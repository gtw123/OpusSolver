using System.Drawing;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Analyzes the mechanisms palette in the sidebar.
    /// </summary>
    public class MechanismPaletteAnalyzer : Analyzer
    {
        private Palette<MechanismType, MechanismType> m_palette;

        public MechanismPaletteAnalyzer(Palette<MechanismType, MechanismType> palette, ScreenCapture capture)
            : base(capture)
        {
            m_palette = palette;
        }

        public void Analyze()
        {
            if (Capture.Rect.Height < 296)
            {
                throw new AnalysisException(Invariant($"Expected mechanisms palette to be at least 296 pixels high. Dimensions are: {Capture.Rect.Size}."));
            }

            // For simplicity we assume the mechanisms are located at fixed positions.
            // The only exception is Van Berlo's Wheel, which may not be enabled for the current puzzle.
            AddMechanism(MechanismType.Arm1, new Point(51, 67));
            AddMechanism(MechanismType.Arm2, new Point(183, 67));
            AddMechanism(MechanismType.Arm3, new Point(51, 181));
            AddMechanism(MechanismType.Arm6, new Point(183, 176));
            AddMechanism(MechanismType.Piston, new Point(51, 296));
            AddMechanism(MechanismType.Track, new Point(183, 296));

            if (Capture.Rect.Height > 412)
            {
                AddMechanism(MechanismType.VanBerlo, new Point(122, 412));
            }
        }

        private void AddMechanism(MechanismType type, Point location)
        {
            m_palette.AddTool(type, new Tool<MechanismType>(type, location));
        }
    }
}
