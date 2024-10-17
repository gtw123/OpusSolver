using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.LowCost.Input
{
    /// <summary>
    /// A disassembler used when all reagents have atoms in a straight line.
    /// </summary>
    public class LinearDisassembler : LowCostAtomGenerator
    {
        private MoleculeInput m_input;
        private AtomCollection m_pendingAtoms;

        public const int MaxReagents = 1;

        private static readonly Transform2D InnerUnbonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D OuterUnbonderPosition = new Transform2D(new Vector2(1, 0), HexRotation.R0);
        private static readonly Transform2D ReagentPosition = new Transform2D(new Vector2(1, -1), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [ReagentPosition, InnerUnbonderPosition, OuterUnbonderPosition];

        public LinearDisassembler(ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> reagents)
            : base(writer, armArea)
        {
            if (reagents.Any(r => r.Height > 1))
            {
                throw new ArgumentException($"{nameof(LinearDisassembler)} can't handle non-linear reagents.");
            }

            if (reagents.Count() > MaxReagents)
            {
                throw new ArgumentException(Invariant($"{nameof(LinearDisassembler)} can't handle more than {MaxReagents} distinct reagents."));
            }

            new Glyph(this, InnerUnbonderPosition.Position, HexRotation.R0, GlyphType.Unbonding);
            m_input = new MoleculeInput(this, Writer, ArmArea, ReagentPosition, reagents.First(), new Transform2D());
        }

        public override void Generate(Element element, int id)
        {
            var targetPosition = InnerUnbonderPosition.Position;
            AtomCollection molecule;
            Atom atomToUnbond;
            Atom atomToUnbondFrom;

            if (m_pendingAtoms == null)
            {
                molecule = m_input.GrabMolecule();
                atomToUnbond = molecule.GetAtom(new Vector2(0, 0));
                atomToUnbondFrom = molecule.GetAtom(new Vector2(1, 0));
            }
            else if (m_pendingAtoms.Atoms.Count == 1)
            {
                // Recreate the atom collection so we can be sure it's at (0, 0) and with no bonds
                var lastAtom = new AtomCollection(m_pendingAtoms.Atoms[0].Element, GetWorldTransform().Apply(OuterUnbonderPosition));
                ArmController.SetMoleculeToGrab(lastAtom);
                m_pendingAtoms = null;
                return;
            }
            else
            {
                molecule = m_pendingAtoms;
                ArmController.SetMoleculeToGrab(m_pendingAtoms);
                atomToUnbond = m_pendingAtoms.GetAtomAtWorldPosition(OuterUnbonderPosition.Position, this);
                targetPosition -= atomToUnbond.Position;

                atomToUnbondFrom = m_pendingAtoms.GetAtomAtWorldPosition(OuterUnbonderPosition.Position + new Vector2(1, 0), this);
            }

            molecule.TargetMolecule = molecule.Copy();
            molecule.TargetMolecule.RemoveBond(atomToUnbond.Position, atomToUnbondFrom.Position);
            ArmController.MoveMoleculeTo(new Transform2D(targetPosition, HexRotation.R0), this, options: new ArmMovementOptions { AllowUnbonding = true });

            m_pendingAtoms = ArmController.RemoveAllExceptGrabbedAtom();
        }
    }
}
