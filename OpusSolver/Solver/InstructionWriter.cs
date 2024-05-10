using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    /// <summary>
    /// Writes instructions to a program (or program fragment).
    /// </summary>
    public class InstructionWriter
    {
        private Program m_program;
        private int m_time = 0;

        public InstructionWriter(Program program)
        {
            m_program = program;
        }

        public void AddInstructions(IEnumerable<Arm> arms, IEnumerable<Instruction> instructions, bool updateTime)
        {
            foreach (var arm in arms)
            {
                int time = m_time;
                var armInstructions = m_program.GetArmInstructions(arm);

                while (time + instructions.Count() - 1 >= armInstructions.Count)
                {
                    armInstructions.Add(Instruction.None);
                }

                foreach (var instruction in instructions)
                {
                    armInstructions[time++] = instruction;
                }
            }

            if (updateTime)
            {
                m_time += instructions.Count();
            }
        }

        public void AdjustTime(int deltaTime)
        {
            m_time += deltaTime;
            if (m_time < 0)
            {
                var padding = Enumerable.Repeat(Instruction.None, -m_time);
                foreach (var instructions in m_program.Instructions.Values)
                {
                    instructions.InsertRange(0, padding);
                }

                m_time = 0;
            }
        }
    }
}
