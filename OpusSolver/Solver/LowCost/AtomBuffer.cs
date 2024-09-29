using OpusSolver.Solver.ElementGenerators;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Stores atoms that are waste or aren't currently needed.
    /// </summary>
    public class AtomBuffer : LowCostAtomGenerator
    {
        private ElementBuffer.BufferInfo m_bufferInfo;
        private Arm m_arm;

        private class AtomStack
        {
            public ElementBuffer.StackInfo Info;
            public int AtomCount;
        }

        private AtomStack m_atomStack;

        private static readonly Transform2D GrabPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [GrabPosition];

        public AtomBuffer(ProgramWriter writer, ArmArea armArea, ElementBuffer.BufferInfo bufferInfo)
            : base(writer, armArea)
        {
            m_bufferInfo = bufferInfo;

            if (bufferInfo.Stacks.Count > 1)
            {
                throw new UnsupportedException($"LowCost {nameof(AtomBuffer)} currently only supports one stack.");
            }

            m_arm = new Arm(this, new(1, 0), HexRotation.R180, ArmType.Arm1, extension: 1);

            if (bufferInfo.Stacks.Any(s => s.UsesRestore && (s.MultiAtom || s.WastesAtoms)))
            {
                // It would be a bit more efficient to have the unbonder closer to the bonder but then we risk
                // the atom chain colliding with glyphs that are clockwise from this atom buffer, or with the
                // product being assembled.
                new Glyph(this, new(2, -1), HexRotation.R180, GlyphType.Unbonding);
            }

            if (bufferInfo.Stacks.Any(s => s.MultiAtom || s.WastesAtoms))
            {
                new Glyph(this, new(1, 1), HexRotation.R180, GlyphType.Bonding);
            }

            m_atomStack = new AtomStack { Info = bufferInfo.Stacks.First() };
        }

        public override void BeginSolution()
        {
            // Register dummy atoms where the atom chain will be so the solver will know to avoid them.
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

            m_atomStack.AtomCount++;
        }

        public override void Generate(Element element, int id)
        {
            ArmArea.MoveGrabberTo(GrabPosition, this);

            // Create a new fragment so that the drop instructions for the buffer arm will automatically line up with
            // the grab for the main arm if possible.
            Writer.NewFragment();

            if (m_atomStack.AtomCount == 1 && !m_atomStack.Info.WastesAtoms)
            {
                Writer.Write(m_arm, [Instruction.RotateClockwise, Instruction.RotateClockwise]);
                Writer.WriteGrabResetAction(m_arm, [Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise]);
                ArmArea.GrabAtoms(new AtomCollection(element, GrabPosition, this));
            }
            else
            {
                Writer.Write(m_arm, [Instruction.RotateClockwise, Instruction.RotateClockwise, Instruction.Grab, Instruction.RotateClockwise, Instruction.PivotCounterclockwise,
                    Instruction.RotateClockwise, Instruction.PivotCounterclockwise, Instruction.RotateClockwise, Instruction.PivotCounterclockwise]);

                // Move the unbonded atom to the grab position
                Writer.Write(m_arm, [Instruction.RotateClockwise, Instruction.Drop]);

                // Move the remaining chain back onto the bonder
                Writer.Write(m_arm, [Instruction.RotateCounterclockwise, Instruction.RotateCounterclockwise, Instruction.Grab,
                    Instruction.PivotClockwise, Instruction.RotateCounterclockwise, Instruction.PivotClockwise, Instruction.RotateCounterclockwise, Instruction.Reset], updateTime: false);

                Writer.AdjustTime(-1);
                ArmArea.GrabAtoms(new AtomCollection(element, GrabPosition, this));
            }

            m_atomStack.AtomCount--;
        }
    }
}
