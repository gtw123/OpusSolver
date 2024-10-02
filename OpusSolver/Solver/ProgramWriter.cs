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
        private Program m_currentFragment;

        public ProgramWriter()
        {
            NewFragment();
        }

        public void NewFragment()
        {
            m_currentFragment = new Program();
            m_fragments.Add(m_currentFragment);
        }

        public Program GetLastFragmentForArm(Arm arm)
        {
            return m_fragments.LastOrDefault(f => f.Instructions.ContainsKey(arm));
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
            Write([arm], [instruction], updateTime);
        }

        public void Write(Arm arm, IEnumerable<Instruction> instructions, bool updateTime = true)
        {
            Write([arm], instructions, updateTime);
        }

        public void Write(IEnumerable<Arm> arms, Instruction instruction, bool updateTime = true)
        {
            Write(arms, [instruction], updateTime);
        }

        public void Write(IEnumerable<Arm> arms, IEnumerable<Instruction> instructions, bool updateTime = true)
        {
            m_currentFragment.AddInstructions(arms, instructions, updateTime);
        }

        public void AdjustTime(int deltaTime)
        {
            m_currentFragment.AdjustTime(deltaTime);
        }

        /// <summary>
        /// Appends another fragment to the current one, at the current writer position.
        /// </summary>
        public void AppendFragment(Program fragment, bool updateTime = true)
        {
            foreach (var (arm, instructions) in fragment.Instructions)
            {
                m_currentFragment.AddInstructions([arm], instructions, updateTime: false);
            }

            if (updateTime)
            {
                m_currentFragment.AdjustTime(fragment.CurrentTime);
            }
        }
    }
}
