using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Identifies program instructions on the screen.
    /// </summary>
    public class InstructionAnalyzer : Analyzer
    {
        private static Dictionary<Instruction, ReferenceImage> sm_referenceImages = new Dictionary<Instruction, ReferenceImage>();

        static InstructionAnalyzer()
        {
            foreach (Instruction instruction in Enum.GetValues(typeof(Instruction)))
            {
                // We only consider renderable instructions because there are several different ways to represent "none"
                // (e.g. the arrow that comes after a reset or repeat).
                if (instruction.IsRenderable())
                {
                    string file = Invariant($"Opus.Images.Instructions.{instruction}.png");
                    sm_referenceImages[instruction] = ReferenceImage.CreateHueThresholdedImage(file, new ThresholdData { Lower = 30, Upper = 50 }, 7);
                }
            }
        }

        public InstructionAnalyzer(ScreenCapture capture)
            : base(capture)
        {
        }

        public bool IsMatch(Point location, Instruction instruction)
        {
            return sm_referenceImages[instruction].IsMatch(Capture.Bitmap, location);
        }

        public (int smallest, int nextSmallest) CalculateDifferences(Point location)
        {
            var diffs = sm_referenceImages.Values.Select(image => image.CalculateDifference(Capture.Bitmap, location));
            var sorted = diffs.OrderBy(x => x).ToList();

            return (sorted[0], sorted[1]);
        }
    }
}
