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
                throw new ArgumentException($"{nameof(LinearDisassembler)} can't handle reagents with more than two atoms.");
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
            if (m_pendingAtoms == null)
            {
                m_input.GrabMolecule();
            }
            else
            {
                ArmController.SetMoleculeToGrab(m_pendingAtoms);
                var nextAtom = m_pendingAtoms.GetAtomAtWorldPosition(OuterUnbonderPosition.Position, this);
                targetPosition -= nextAtom.Position;
            }

            ArmController.MoveMoleculeTo(new Transform2D(targetPosition, HexRotation.R0), this);

            m_pendingAtoms = ArmController.RemoveAllExceptGrabbedAtom();
            if (m_pendingAtoms.Atoms.Count == 0)
            {
                m_pendingAtoms = null;
            }
        }
    }
}
