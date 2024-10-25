﻿using System.Collections.Generic;
using System.Linq;
using System;

namespace OpusSolver.Solver.Standard.Output.Hex3
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

        private List<Element> m_elementBuildOrder;
        private HexRotation m_outputRotation;
        private bool m_needsAllBonds;

        public MissingCenterAtomMoleculeBuilder(Molecule product)
            : base(product)
        {
            m_centerAtomPosition = FindCenterPosition();

            DetermineBuildOrder();
        }

        private void DetermineBuildOrder()
        {
            // Start with the first atom that doesn't have a bond in a clockwise direction.
            var atoms = Product.GetAdjacentAtoms(CenterAtomPosition);
            var startDir = atoms.Keys.FirstOrDefault(dir => atoms[dir].Bonds[dir - HexRotation.R120] == BondType.None);
            var orderedAtoms = atoms.EnumerateCounterclockwise(startFrom: startDir);

            m_elementBuildOrder = orderedAtoms.Select(a => a.Value.Element).ToList();

            // For convenience, the first two atoms are swapped
            (m_elementBuildOrder[0], m_elementBuildOrder[1]) = (m_elementBuildOrder[1], m_elementBuildOrder[0]);

            m_outputRotation = HexRotation.R180 - orderedAtoms.Last().Key;
            m_needsAllBonds = atoms.Keys.All(dir => atoms[dir].Bonds[dir - HexRotation.R120] != BondType.None);
            if (m_needsAllBonds)
            {
                m_outputRotation = m_outputRotation.Rotate60Clockwise();
            }
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
                throw new SolverException($"Expected to find a center position but instead found {commonPositions.Count}.");
            }

            return commonPositions.First();
        }

        public override IEnumerable<Program> GenerateFragments(AssemblyArea assemblyArea)
        {
            var writer = new ProgramWriter();

            // Move the assembly arm so the product doesn't collide with it while being assembled
            writer.Write(assemblyArea.AssemblyArm, Instruction.MovePositive);

            // Move the first atom to the RHS of the bonder
            writer.Write(assemblyArea.HorizontalArm, [Instruction.Grab, Instruction.MoveNegative]);

            // Grab the second atom and rotate it so the two atoms are now facing away from the bonder
            writer.NewFragment();
            writer.Write(assemblyArea.HorizontalArm, [Instruction.PivotCounterclockwise, Instruction.PivotCounterclockwise, Instruction.Reset]);

            int atomCount = Product.Atoms.Count();
            for (int index = 2; index < atomCount; index++)
            {
                writer.NewFragment();

                if (index < atomCount - 1)
                {
                    writer.WriteGrabResetAction(assemblyArea.HorizontalArm, [Instruction.MoveNegative, Instruction.PivotClockwise]);
                }
            }

            if (m_needsAllBonds)
            {
                writer.WriteGrabResetAction(assemblyArea.HorizontalArm, [Instruction.MoveNegative, Instruction.PivotClockwise]);
                writer.AdjustTime(2); // Move past the reset
            }

            // Move the assembled molecule to the output area
            writer.WriteGrabResetAction(assemblyArea.HorizontalArm, Instruction.RotateCounterclockwise);

            writer.Write(assemblyArea.AssemblyArm, Instruction.Reset);
            writer.AdjustTime(-1);

            return writer.Fragments;
        }
    }
}
