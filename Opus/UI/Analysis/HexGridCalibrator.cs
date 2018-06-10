using System.Drawing;
using System.Windows.Forms;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Scrolls the hex grid so that the center-most hex is at the same position where it would be
    /// when opening a new solution for the first time. This helps ensure element analysis works
    /// as expected.
    /// </summary>
    public class HexGridCalibrator
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(HexGridCalibrator));

        private const int SearchWidth = 6;
        private const int SearchHeight = 16;

        // This is the offset from the center of the grid (on the screen) to the center of the center-most
        // hex when opening a new solution on a 2560x1440 screen.
        private static readonly Point CenterOffset = new Point(27, 6);

        HexGrid m_grid;
        Sidebar m_sidebar;

        public HexGridCalibrator(HexGrid grid, Sidebar sidebar)
        {
            m_grid = grid;
            m_sidebar = sidebar;
        }

        public void Calibrate()
        {
            sm_log.Info("Calibrating center of hex grid");

            // Drag a glyph of equilibrium onto the center of the grid
            var toolLocation = m_sidebar.ScrollTo(m_sidebar.Glyphs, m_sidebar.Glyphs[GlyphType.Equilibrium]);
            var gridLocation = m_grid.GetScreenLocationForCell(new Vector2(0, 0));
            MouseUtils.LeftDrag(toolLocation, gridLocation);

            // Find where the glyph actually ended up on the screen - this will be the exact center of a hex
            // near the center of the screen.
            var actualCenter = FindGlyph(m_grid.CenterLocation);
            sm_log.Info(Invariant($"Actual hex center is {actualCenter}"));

            // Scroll the grid so this actual center is exactly where we want it
            var desiredCenter = m_grid.Rect.Location.Add(new Point(m_grid.Rect.Width / 2, m_grid.Rect.Height / 2)).Add(CenterOffset);
            MouseUtils.RightDrag(actualCenter, desiredCenter);
            m_grid.CenterLocation = desiredCenter;

            // Delete the glyph from the grid
            KeyboardUtils.KeyPress(Keys.Z);
        }

        private static Point FindGlyph(Point center)
        {
            var rect = new Rectangle(center.X - HexGrid.HexWidth, center.Y - HexGrid.HexHeight, HexGrid.HexWidth * 2, HexGrid.HexHeight * 2);
            using (var capture = new ScreenCapture(rect))
            {
                var analyzer = new GlyphAnalyzer(capture);
                var startPoint = new Point(rect.Width / 2 - SearchWidth / 2, rect.Height / 2 - SearchHeight / 2);
                for (int y = 0; y < SearchHeight; y++)
                {
                    for (int x = 0; x< SearchWidth; x++)
                    {
                        var location = startPoint.Add(new Point(x, y));
                        if (analyzer.IsMatch(location, GlyphType.Equilibrium))
                        {
                            if (ScreenCapture.LoggingEnabled)
                            {
                                capture.Bitmap.SetPixel(location.X, location.Y, Color.Red);
                                capture.Save();
                            }

                            return location.Add(capture.Rect.Location);
                        }
                    }
                }
            }

            throw new AnalysisException(Invariant($"Cannot find a glyph in {rect}."));
        }
    }
}
