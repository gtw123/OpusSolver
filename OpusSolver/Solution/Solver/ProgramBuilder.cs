using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace Opus.Solution.Solver
{
    /// <summary>
    /// Converts a series of program fragments into a complete program. The fragments will
    /// always be positioned in the same relative order but they will be shifted forward or
    /// backward so that there is the minimum amount of time between them without any
    /// instructions overlapping on any arms.
    /// </summary>
    public class ProgramBuilder
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramBuilder));

        private IEnumerable<Program> m_fragments;
        private Program m_program = new Program();

        public ProgramBuilder(IEnumerable<Program> fragments)
        {
            m_fragments = fragments;
        }

        public Program Build()
        {
            sm_log.Debug("Building program");
            foreach (var fragment in m_fragments.Reverse())
            {
                AddFragment(fragment);
            }

            AddPeriodOverride();
            AddRepeats();

            sm_log.Debug("Final program:" + Environment.NewLine + m_program.ToString());
            return m_program;
        }

        /// <summary>
        /// Adds the specified program fragment at the start of the program constructed so far,
        /// shifting existing instructions forward if necessary.
        /// </summary>
        private void AddFragment(Program fragment)
        {
            int timeShift = CalculateTimeShift(fragment);
            if (timeShift > 0)
            {
                var padding = Enumerable.Repeat(Instruction.None, timeShift);
                foreach (var instructions in m_program.Instructions.Values)
                {
                    instructions.InsertRange(0, padding);
                }
            }

            int startTime = (timeShift < 0) ? -timeShift : 0;

            foreach (var (arm, fragmentInstructions) in fragment.Instructions)
            {
                if (arm.Parent == null)
                {
                    sm_log.Debug("Ignoring instructions for removed arm " + arm.UniqueID);
                    continue;
                }

                var programInstructions = m_program.GetArmInstructions(arm);
                if (startTime > programInstructions.Count)
                {
                    var padding = Enumerable.Repeat(Instruction.None, startTime - programInstructions.Count);
                    programInstructions.AddRange(padding);
                }

                for (int i = 0; i < fragmentInstructions.Count; i++)
                {
                    var instruction = fragmentInstructions[i];
                    int time = startTime + i;
                    if (time < programInstructions.Count)
                    {
                        programInstructions[time] = instruction;
                    }
                    else
                    {
                        programInstructions.Add(instruction);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates how many cycles the existing program will need to be shifted by in order
        /// to insert a fragment at the start and avoiding any instructions overlapping.
        /// </summary>
        /// <returns>Positive means shift to the right; negative means shift to the left (or equivalently, shift the fragment to the right)</returns>
        private int CalculateTimeShift(Program fragment)
        {
            int? maxTimeShift = null;
            foreach (var (arm, fragmentInstructions) in fragment.Instructions)
            {
                int lastIndex = fragmentInstructions.FindLastIndex(i => i != Instruction.None);
                if (lastIndex >= 0)
                {
                    if (fragmentInstructions[lastIndex] == Instruction.Reset)
                    {
                        lastIndex += CalculateResetTime(fragment, arm, lastIndex) - 1;
                    }

                    int firstIndex = m_program.GetArmInstructions(arm).FindIndex(i => i != Instruction.None);
                    if (firstIndex >= 0)
                    {
                        maxTimeShift = Max(maxTimeShift, lastIndex + 1 - firstIndex);
                    }
                }
            }

            return maxTimeShift ?? 0;
        }

        private static int Max(int? v1, int v2)
        {
            return v1.HasValue ? Math.Max(v1.Value, v2) : v2;
        }

        /// <summary>
        /// Calculates how many cycles will be required to execute the reset instruction
        /// for a particular arm at the specified index of a fragment.
        /// </summary>
        private int CalculateResetTime(Program fragment, Arm arm, int index)
        {
            // Make a list of all instructions that will execute on this arm before this fragment
            var instructions = new List<Instruction>();
            foreach (var priorFragment in m_fragments)
            {
                if (priorFragment == fragment)
                {
                    break;
                }

                if (priorFragment.Instructions.TryGetValue(arm, out var priorInstructions))
                {
                    instructions.AddRange(priorInstructions);
                }
            }

            // Add all the instructions that will execute before the reset in the current fragment
            instructions.AddRange(fragment.Instructions[arm].GetRange(0, index));

            // Skip everything up to the last reset, since it doesn't matter. Note if there are no
            // resets then LastIndexOf returns -1 and startIndex conveniently becomes 0.
            int startIndex = instructions.LastIndexOf(Instruction.Reset) + 1;

            return CalculateResetTime(instructions.Skip(startIndex), arm.Extension);
        }

        /// <summary>
        /// Calculates how many cycles will be required to execute a reset instruction after
        /// the specified instructions are executed. Assumes the arm will be at its initial
        /// position/rotation/extension.
        /// </summary>
        private static int CalculateResetTime(IEnumerable<Instruction> instructions, int initialExtension)
        {
            bool isGrabbing = false;
            int extension = initialExtension;
            int deltaRotation = 0;
            int deltaPosition = 0;

            foreach (var instruction in instructions)
            {
                switch (instruction)
                {
                    case Instruction.Grab: isGrabbing = true; break;
                    case Instruction.Drop: isGrabbing = false; break;
                    case Instruction.RotateClockwise: deltaRotation = DirectionUtil.Rotate60Clockwise(deltaRotation); break;
                    case Instruction.RotateCounterclockwise: deltaRotation = DirectionUtil.Rotate60Counterclockwise(deltaRotation); break;
                    case Instruction.Extend: extension = Math.Min(extension + 1, 3); break;
                    case Instruction.Retract: extension = Math.Max(extension - 1, 0); break;
                    case Instruction.MovePositive: deltaPosition++; break;  // TODO: Handle cyclical tracks, where it may reset by moving in the same direction instead of opposite
                    case Instruction.MoveNegative: deltaPosition--; break;  // TODO: Handle moving beyond the end of a track
                    case Instruction.None:
                    case Instruction.Wait:
                    case Instruction.PivotClockwise:
                    case Instruction.PivotCounterclockwise:
                        break;
                    default:
                        throw new ArgumentException(Invariant($"Unexpected instruction: '{instruction}'."));
                }
            }

            int resetTime = isGrabbing ? 1 : 0;
            resetTime += Math.Abs(extension - initialExtension);
            resetTime += 3 - Math.Abs(deltaRotation - 3);
            resetTime += Math.Abs(deltaPosition);

            // Even if there is nothing to reset, the instruction will still take one cycle
            return Math.Max(resetTime, 1);
        }

        /// <summary>
        /// Converts repeated instructions to use the "Repeat" instruction where possible
        /// (to reduce rendering time).
        /// </summary>
        private void AddRepeats()
        {
            foreach (var instructions in m_program.Instructions.Values)
            {
                // For simplicity, just find the first sequence that ends with a reset. This is good enough for most programs.
                int start = instructions.FindIndex(i => i != Instruction.None);
                if (start >= 0)
                {
                    int end = instructions.IndexOf(Instruction.Reset, start);
                    if (end >= 0)
                    {
                        var sequence = instructions.GetRange(start, end - start + 1);
                        ReplaceSequenceWithRepeat(instructions, sequence, end + 1);
                    }
                }
            }
        }

        private static void ReplaceSequenceWithRepeat(List<Instruction> instructions, List<Instruction> sequence, int startIndex)
        {
            for (int i = startIndex; i < instructions.Count; i++)
            {
                if (instructions[i] == Instruction.None)
                {
                    continue;
                }

                if (!sequence.SequenceEqual(instructions.Skip(i).Take(sequence.Count)))
                {
                    // If the sequences don't match, stop replacement. This is because the "Repeat" instruction
                    // always repeats from the start of the prograrm (or the last repeat).
                    return;
                }

                instructions[i] = Instruction.Repeat;
                for (int j = 1; j < sequence.Count; j++)
                {
                    instructions[i + j] = Instruction.None;
                }

                i += sequence.Count;
            } 
        }

        /// <summary>
        /// Finds the longest instruction sequence that ends with a Wait, and replaces the Wait
        /// with a period override instruction. This ensures atoms won't collide when the second
        /// or subsequent iterations of the program are run.
        /// </summary>
        private void AddPeriodOverride()
        {
            int? maxLength = null;
            List<Instruction> maxInstructions = null;

            // Find the longest instruction sequence that ends with a Wait
            foreach (var instructions in m_program.Instructions.Values)
            {
                int lastIndex = instructions.FindLastIndex(i => i != Instruction.None);
                if (lastIndex >= 0 && instructions[lastIndex] == Instruction.Wait)
                {
                    // Find the length of this instruction sequence
                    int length = lastIndex - instructions.FindIndex(i => i != Instruction.None);
                    if (maxLength == null || length > maxLength.Value)
                    {
                        maxLength = length;
                        maxInstructions = instructions;
                    }
                }
            }

            if (maxInstructions != null)
            {
                int lastIndex = maxInstructions.FindLastIndex(i => i != Instruction.None);
                maxInstructions[lastIndex] = Instruction.PeriodOverride;
            }
        }
    }
}
