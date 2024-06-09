using System.Collections.Generic;
using System.Linq;
using System;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers.Hex3
{
    /// <summary>
    /// Generates instructions for assembling a molecule that doesn't contain a center atom.
    /// This means it's one of the following (possibly rotated):
    ///                    O        O - O       O - O
    ///                     \            \     /     \
    /// O       O    O       O    O       O   O       O
    ///  \     /      \     /      \     /     \     /
    ///   O - O        O - O        O - O       O - O
    ///   
    /// It will never have less than 4 atoms, because then it would have a "center" atom and go through CenterAtomMoleculeGenerator.
    /// </summary>
    public class MissingCenterAtomMoleculeBuilder : MoleculeBuilder
    {
        private Vector2 m_centerAtomPosition;
        public override Vector2 CenterAtomPosition => m_centerAtomPosition;

        public override Vector2 OutputPositionOffset => new Vector2(1, 0);
        public override OutputLocation OutputLocation => OutputLocation.RightNoCenter;
        public override HexRotation OutputRotation => m_outputRotation;
        public override bool RequiresRotationsBetweenOutputPositions => false;

        private List<Element> m_elementBuildOrder = new();
        private HexRotation m_outputRotation;

        public MissingCenterAtomMoleculeBuilder(AssemblyArea assemblyArea, Molecule product)
            : base(assemblyArea, product)
        {
            m_centerAtomPosition = FindCenterPosition();
            GenerateInstructions();
        }

        public override IEnumerable<Element> GetElementsInBuildOrder()
        {
            return m_elementBuildOrder;
        }

        private Vector2 FindCenterPosition()
        {
            // For our purposes, the center is the location that's a distance of 1 away from all the atoms in the molecule
            IEnumerable<Vector2> GetSurroundingPositions(Atom atom) => HexRotation.All.Select(dir => atom.Position.OffsetInDirection(dir, 1));

            var commonPositions = new HashSet<Vector2>(GetSurroundingPositions(Product.Atoms.First()));
            foreach (var atom in Product.Atoms.Skip(1))
            {
                commonPositions.IntersectWith(GetSurroundingPositions(atom));
            }

            if (commonPositions.Count != 1)
            {
                throw new InvalidOperationException($"Expected to find a center position but instead found {commonPositions.Count}.");
            }

            return commonPositions.First();
        }

        private void GenerateInstructions()
        {
            var atoms = Product.GetAdjacentAtoms(CenterAtomPosition);

            // Start with the first atom that doesn't have a bond in a clockwise direction.
            var startDir = atoms.Keys.FirstOrDefault(dir => atoms[dir].Bonds[dir - HexRotation.R120] == BondType.None);
            var orderedAtoms = atoms.EnumerateCounterclockwise(startFrom: startDir).ToList();

            // Move the assembly arm so the product doesn't collide with it while being assembled
            Writer.Write(AssemblyArea.AssemblyArm, Instruction.MovePositive);

            // Move the first atom to the RHS of the bonder. For convenience, the first atoms are actually swapped.
            Writer.Write(AssemblyArea.HorizontalArm, [Instruction.Grab, Instruction.MoveNegative]);
            m_elementBuildOrder.Add(orderedAtoms[1].Value.Element);

            // Grab the second atom and rotate it so the two atoms are now facing away from the bonder
            Writer.NewFragment();
            Writer.Write(AssemblyArea.HorizontalArm, [Instruction.PivotCounterclockwise, Instruction.PivotCounterclockwise, Instruction.Reset]);
            m_elementBuildOrder.Add(orderedAtoms[0].Value.Element);

            for (int index = 2; index < orderedAtoms.Count; index++)
            {
                var (dir, atom) = orderedAtoms[index];

                Writer.NewFragment();
                m_elementBuildOrder.Add(atom.Element);

                if (index < atoms.Count - 1)
                {
                    Writer.WriteGrabResetAction(AssemblyArea.HorizontalArm, [Instruction.MoveNegative, Instruction.PivotClockwise]);
                }
            }

            m_outputRotation = HexRotation.R180 - orderedAtoms.Last().Key;

            bool needsAllBonds = atoms.Keys.All(dir => atoms[dir].Bonds[dir - HexRotation.R120] != BondType.None);
            if (needsAllBonds)
            {
                Writer.WriteGrabResetAction(AssemblyArea.HorizontalArm, [Instruction.MoveNegative, Instruction.PivotClockwise]);
                Writer.AdjustTime(2); // Move past the reset

                m_outputRotation = m_outputRotation.Rotate60Clockwise();
            }

            // Move the assembled molecule to the output area
            Writer.WriteGrabResetAction(AssemblyArea.HorizontalArm, Instruction.RotateCounterclockwise);

            Writer.Write(AssemblyArea.AssemblyArm, Instruction.Reset);
            Writer.AdjustTime(-1);
        }
    }
}
