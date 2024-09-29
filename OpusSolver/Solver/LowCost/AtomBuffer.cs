using OpusSolver.Solver.ElementGenerators;
using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Temporarily stores atoms that aren't currently needed.
    /// </summary>
    public class AtomBuffer : LowCostAtomGenerator
    {
        private ElementBuffer.BufferInfo m_bufferInfo;
        private Arm m_arm;

        private static readonly Transform2D GrabPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [GrabPosition];

        public AtomBuffer(ProgramWriter writer, ArmArea armArea, ElementBuffer.BufferInfo bufferInfo)
            : base(writer, armArea)
        {
            m_bufferInfo = bufferInfo;

            m_arm = new Arm(this, new(1, 0), HexRotation.R180, ArmType.Arm1, extension: 1);
            new Glyph(this, new(1, 1), HexRotation.R180, GlyphType.Bonding);
        }

        public override void BeginSolution()
        {
            // Register dummy atoms where the waste chain will be so the solver will know to avoid them.
            for (int i = 1; i <= 6; i++)
            {
                GridState.RegisterAtom(new(i, 1), Element.Salt, this);
            }
        }

        public override void Consume(Element element, int id)
        {
            ArmArea.MoveGrabberTo(GrabPosition, this);
            ArmArea.DropAtoms(addToGrid: false);

            // Bond the atom to the waste chain
            Writer.AdjustTime(-1);
            Writer.WriteGrabResetAction(m_arm, [Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise]);
        }
    }
}
