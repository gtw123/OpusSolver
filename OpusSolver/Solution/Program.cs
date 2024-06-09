using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.FormattableString;

namespace OpusSolver
{
    public class Program
    {
        public Dictionary<Arm, List<Instruction>> Instructions { get; private set; }

        private int m_time = 0;
        public int CurrentTime => m_time;

        public Program()
        {
            Instructions = new Dictionary<Arm, List<Instruction>>();
        }

        public List<Instruction> GetArmInstructions(Arm arm)
        {
            if (!Instructions.TryGetValue(arm, out var armInstructions))
            {
                armInstructions = new List<Instruction>();
                Instructions[arm] = armInstructions;
            }

            return armInstructions;
        }

        public void AddInstructions(IEnumerable<Arm> arms, IEnumerable<Instruction> instructions, bool updateTime)
        {
            foreach (var arm in arms)
            {
                int time = m_time;
                var armInstructions = GetArmInstructions(arm);

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
                foreach (var instructions in Instructions.Values)
                {
                    instructions.InsertRange(0, padding);
                }

                m_time = 0;
            }
        }

        public override string ToString()
        {
            var str = new StringBuilder();

            foreach (var arm in Instructions.Keys.OrderBy(arm => arm.UniqueID))
            {
                str.Append(Invariant($"Arm {arm.UniqueID, 2}: "));
                foreach (var instruction in Instructions[arm])
                {
                    str.Append(instruction.ToDebugString());
                }

                str.AppendLine();
            }

            return str.ToString();
        }
    }
}
