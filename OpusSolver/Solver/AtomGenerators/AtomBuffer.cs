using System.Collections.Generic;

namespace OpusSolver.Solver.AtomGenerators
{
    /// <summary>
    /// Temporarily stores atoms that aren't currently needed.
    /// </summary>
    public class AtomBuffer : AtomGenerator
    {
        /// <summary>
        /// Specified various properties of a stack, used for optimising the mechanisms needed by the stack.
        /// </summary>
        public class StackInfo
        {
            /// <summary>
            /// Indicates whether the stack ever needs to store more than one atom at atime.
            /// </summary>
            public bool MultiAtom { get; set; }

            /// <summary>
            /// Indicates whether atoms are ever restored from the stack.
            /// </summary>
            public bool UsesRestore { get; set; }

            /// <summary>
            /// Indicates whether the stack has leftover atoms at the end of the solution.
            /// </summary>
            public bool WastesAtoms { get; set; }
        }

        private class AtomStack
        {
            public StackInfo Info;
            public int Index;
            public Arm PushArm;
            public Arm PopArm;
            public Arm OutputArm;
            public int AtomCount;
        }

        private List<AtomStack> m_stacks = new List<AtomStack>();

        public AtomBuffer(ProgramWriter writer, IEnumerable<StackInfo> stackInfo)
            : base(writer)
        {
            int i = 0;
            foreach (var info in stackInfo)
            {
                AddStack(i++, info);
            }

            OutputArm = new Arm(this, new Vector2(m_stacks.Count * 2 + 3, 0), Direction.W, MechanismType.Arm1, extension: 3);
        }

        private void AddStack(int index, StackInfo info)
        {              
            var stack = new AtomStack { Info = info, Index = index };
            var pos = new Vector2(index * 2, 0);

            stack.PushArm = new Arm(this, pos.Add(-1, 2), Direction.SE, MechanismType.Arm1);
            stack.PopArm = new Arm(this, pos.Add(-1, 4), Direction.SE, MechanismType.Arm1);
            stack.OutputArm = new Arm(this, pos.Add(2, -2), Direction.NW, MechanismType.Arm1, extension: 2);

            new Track(this, pos.Add(-1, 0), Direction.NE, 4);
            if (info.UsesRestore && (info.MultiAtom || info.WastesAtoms))
            {
                new Glyph(this, pos.Add(0, -1), Direction.NE, GlyphType.Unbonding);
            }

            if (info.MultiAtom || info.WastesAtoms)
            {
                new Glyph(this, pos.Add(0, 1), Direction.NE, GlyphType.Bonding);
            }

            m_stacks.Add(stack);
        }

        public override void Consume(Element element, int id)
        {
            var stack = m_stacks[id];
            stack.AtomCount++;

            MoveAtomThroughStacks(0, stack.Index);
            Writer.AdjustTime(-1);
            Writer.Write(stack.PushArm, new[] { Instruction.MoveNegative, Instruction.Grab, Instruction.MovePositive, Instruction.MovePositive, Instruction.Reset });
        }

        public override void Generate(Element element, int id)
        {
            Writer.NewFragment();

            var stack = m_stacks[id];
            if (stack.AtomCount > 1 || stack.Info.WastesAtoms)
            {
                Writer.Write(stack.PushArm, Instruction.MovePositive);
                Writer.Write(new[] { stack.PushArm, stack.PopArm }, new[] { Instruction.Grab, Instruction.MoveNegative, Instruction.MoveNegative, Instruction.MoveNegative, Instruction.MovePositive });
                Writer.Write(stack.PushArm, Instruction.Reset, updateTime: false);
                Writer.Write(stack.PopArm, new[] { Instruction.MovePositive, Instruction.Drop, Instruction.MovePositive }, updateTime: false);
                // If another atom is moving through the stacks 4 cycles before we restore the current atom, they will collide.
                // To avoid this we add a "wait" to the output arm. This will force the restore to be delayed by one cycle if the
                // output arm is already busy.
                Writer.AdjustTime(-1);
                Writer.Write(stack.OutputArm, Instruction.Wait);
            }
            else
            {
                Writer.Write(stack.PushArm, new[] { Instruction.MovePositive, Instruction.Grab, Instruction.MoveNegative, Instruction.MoveNegative, Instruction.Reset });
            }

            MoveAtomThroughStacks(stack.Index, m_stacks.Count);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);

            stack.AtomCount--;
        }

        public override void PassThrough(Element element)
        {
            MoveAtomThroughStacks(0, m_stacks.Count);
            Writer.WriteGrabResetAction(OutputArm, Instruction.RotateCounterclockwise);
        }

        private void MoveAtomThroughStacks(int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                Writer.WriteGrabResetAction(m_stacks[i].OutputArm, Instruction.RotateClockwise);
            }
        }
    }
}
