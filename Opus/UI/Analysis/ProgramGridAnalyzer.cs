using System.Drawing;

namespace Opus.UI.Analysis
{
    public class ProgramGridAnalyzer : Analyzer
    {
        private const int InstructionGridLeft = 345;
        private const int InstructionGridRightMargin = 43;
        private const int InstructionGridHeight = 228;
        private const int InstructionGridBottomMargin = 21;

        public ProgramGridAnalyzer(ScreenCapture capture)
            : base(capture)
        {
        }

        public ProgramGrid Analyze()
        {
            var rect = new Rectangle(InstructionGridLeft, Capture.Rect.Height - InstructionGridHeight - InstructionGridBottomMargin,
                Capture.Rect.Width - InstructionGridLeft - InstructionGridRightMargin, InstructionGridHeight);
            return new ProgramGrid(rect.Add(Capture.Rect.Location));
        }
    }
}