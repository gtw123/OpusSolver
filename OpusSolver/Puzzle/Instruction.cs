using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    public enum Instruction
    {
        None,
        Wait,   // Not a real instruction - this renders as "none" but unlike "none" it affects scheduling of program fragments
        PivotCounterclockwise,
        Extend,
        PivotClockwise,
        Drop,
        MoveNegative,
        RotateCounterclockwise,
        Retract,
        RotateClockwise,
        Grab,
        MovePositive,
        PeriodOverride,
        Reset,
        Repeat
    }

    public static class InstructionExtensions
    {
        public static string ToDebugString(this Instruction instruction)
        {
            string str;
            switch (instruction)
            {
                case Instruction.None:                      str = "."; break;
                case Instruction.Wait:                      str = "-"; break;
                case Instruction.PivotCounterclockwise:     str = "Q"; break;
                case Instruction.Extend:                    str = "W"; break;
                case Instruction.PivotClockwise:            str = "E"; break;
                case Instruction.Drop:                      str = "R"; break;
                case Instruction.MoveNegative:              str = "T"; break;
                case Instruction.RotateCounterclockwise:    str = "A"; break;
                case Instruction.Retract:                   str = "S"; break;
                case Instruction.RotateClockwise:           str = "D"; break;
                case Instruction.Grab:                      str = "F"; break;
                case Instruction.MovePositive:              str = "G"; break;
                case Instruction.PeriodOverride:            str = "X"; break;
                case Instruction.Reset:                     str = "C"; break;
                case Instruction.Repeat:                    str = "V"; break;
                default:                                    str = "?"; break;
            }

            return str;
        }

        public static bool IsRenderable(this Instruction instruction)
        {
            return instruction != Instruction.None && instruction != Instruction.Wait;
        }

        public static IEnumerable<Instruction> ToRotationInstructions(this IEnumerable<HexRotation> rotations)
        {
            return rotations.Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise);
        }
    }
}
