using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Disposes of waste atoms by bonding them to a long chain.
    /// </summary>
    public class WasteChainDisposer : LowCostAtomGenerator, IWasteDisposer
    {
        private Arm m_arm;

        private static readonly Transform2D GrabPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [GrabPosition];

        public WasteChainDisposer(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
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

        public void Dispose(Element element)
        {
            ArmArea.MoveGrabberTo(GrabPosition, this);
            ArmArea.DropAtoms(addToGrid: false);

            // Bond the atom to the waste chain
            Writer.AdjustTime(-1);
            Writer.WriteGrabResetAction(m_arm, [Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise]);
        }
    }
}
