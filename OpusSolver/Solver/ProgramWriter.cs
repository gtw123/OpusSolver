using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    /// <summary>
    /// Provides a convenient way to generate a program. The "program" consists of a number of
    /// fragments, each one representing a sequence of instructions that must be run exactly
    /// as specified. The distance between two fragments will automatically be determined later
    /// on so that the fragments are as close as possible with no instructions overlapping.
    /// </summary>
    public class ProgramWriter
    {
        public IEnumerable<Program> Fragments => m_fragments;

        private List<Program> m_fragments = new List<Program>();
        private InstructionWriter m_writer;

        public ProgramWriter()
        {
            NewFragment();
        }

        public void NewFragment()
        {
            var fragment = new Program();
            m_fragments.Add(fragment);
            m_writer = new InstructionWriter(fragment);
        }

        /// <summary>
        /// Adds a grab instruction, then the specified instruction, then a reset instruction.
        /// </summary>
        /// <param name="updateTime">If true, the current write position will be updated so that it's
        /// at the position of the reset instruction. This is convenient for chaining a number of
        /// grab/reset instructions across a number of arms.</param>
        public void WriteGrabResetAction(Arm arm, Instruction instruction, bool updateTime = true)
        {
            WriteGrabResetAction(new[] { arm }, new[] { instruction }, updateTime);
        }

        public void WriteGrabResetAction(Arm arm, IEnumerable<Instruction> instructions, bool updateTime = true)
        {
            WriteGrabResetAction(new[] { arm }, instructions, updateTime);
        }

        public void WriteGrabResetAction(IEnumerable<Arm> arms, Instruction instruction, bool updateTime = true)
        {
            WriteGrabResetAction(arms, new[] { instruction }, updateTime);
        }

        public void WriteGrabResetAction(IEnumerable<Arm> arms, IEnumerable<Instruction> instructions, bool updateTime = true)
        {
            Write(arms, new[] { Instruction.Grab }.Concat(instructions).Concat(new[] { Instruction.Reset }), updateTime);
            if (updateTime)
            {
                AdjustTime(-1);
            }
        }

        /// <summary>
        /// Adds an instruction for the specified arm to the current program fragment.
        /// </summary>
        /// <param name="updateTime">If true, the current write position will be updated so that it's just
        /// after the added instruction. If false, it will be left where it was before the instruction
        /// was added.</param>
        public void Write(Arm arm, Instruction instruction, bool updateTime = true)
        {
            Write(new[] { arm }, new[] { instruction }, updateTime);
        }

        public void Write(Arm arm, IEnumerable<Instruction> instructions, bool updateTime = true)
        {
            Write(new[] { arm }, instructions, updateTime);
        }

        public void Write(IEnumerable<Arm> arms, Instruction instruction, bool updateTime = true)
        {
            Write(arms, new[] { instruction }, updateTime);
        }

        public void Write(IEnumerable<Arm> arms, IEnumerable<Instruction> instructions, bool updateTime = true)
        {
            m_writer.AddInstructions(arms, instructions, updateTime);
        }

        public void AdjustTime(int deltaTime)
        {
            m_writer.AdjustTime(deltaTime);
        }
    }
}
