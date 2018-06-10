using System.Collections.Generic;
using System.Windows.Forms;
using static Opus.KeyboardUtils;

namespace Opus.UI.Rendering
{
    public class InstructionRenderer
    {
        private ProgramGrid m_grid;

        private Dictionary<Instruction, Keys> m_instructionKeys = new Dictionary<Instruction, Keys>()
        {
            { Instruction.PivotCounterclockwise,    Keys.Q },
            { Instruction.Extend,                   Keys.W },
            { Instruction.PivotClockwise,           Keys.E },
            { Instruction.Drop,                     Keys.R },
            { Instruction.MoveNegative,             Keys.T },
            { Instruction.RotateCounterclockwise,   Keys.A },
            { Instruction.Retract,                  Keys.S },
            { Instruction.RotateClockwise,          Keys.D },
            { Instruction.Grab,                     Keys.F },
            { Instruction.MovePositive,             Keys.G },
            { Instruction.PeriodOverride,           Keys.X },
            { Instruction.Reset,                    Keys.C },
            { Instruction.Repeat,                   Keys.V }
        };

        public InstructionRenderer(ProgramGrid grid)
        {
            m_grid = grid;
        }

        /// <summary>
        /// Renders an instruction onto the program grid. Assumes the grid is already scrolled to make the instruction
        /// location visible.
        /// </summary>
        public void Render(Vector2 position, Instruction instruction, int delay = 0)
        {
            if (instruction.IsRenderable())
            {
                var key = m_instructionKeys[instruction];
                var gridLocation = m_grid.GetCellLocation(position);

                int keyTime = delay;
                int clickTime = delay +  50;

                KeyDown(key);
                MouseUtils.SetCursorPosition(gridLocation);
                ThreadUtils.SleepOrAbort(keyTime);
                MouseUtils.LeftClick(clickTime);

                KeyUp(key);
                ThreadUtils.SleepOrAbort(keyTime);
            }
        }
    }
}
