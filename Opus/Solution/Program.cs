using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.FormattableString;

namespace Opus.Solution
{
    public class Program
    {
        public Dictionary<Arm, List<Instruction>> Instructions { get; private set; }

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
