using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Finds cells on a grid that contain an atom. Unlike ElementAnalyzer, this works for cells
    /// anywhere on the grid, as it only does a rough check (not robust enough to tell what type
    /// of atom it's found).
    /// </summary>
    public class AtomFinder
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(AtomFinder));

        // Since the grid is mostly the same hue, we look for cells with a totally different hue.
        // In particular, the border around an atom is very similar for all elements so we check that.
        private const float LowerHueThreshold = 200.0f;
        private const float UpperHueThreshold = 60.0f;
        private static readonly Point HueTestOffset = new Point(0, -32);

        private HexGrid m_grid;
        private HashSet<Vector2> m_seenCells = new HashSet<Vector2>();

        public AtomFinder(HexGrid grid)
        {
            m_grid = grid;
        }

        public IEnumerable<Vector2> FindNewAtoms(ScreenCapture capture)
        {
            var cells = FindNewAtomsInternal(capture).ToList();

            if (ScreenCapture.LoggingEnabled)
            {
                using (var capture2 = capture.Clone())
                {
                    using (var graphics = Graphics.FromImage(capture2.Bitmap))
                    {
                        using (var pen = new Pen(Color.Red, 2))
                        {
                            foreach (var cell in cells)
                            {
                                var pos = m_grid.GetScreenLocationForCell(cell).Subtract(m_grid.Rect.Location);
                                int radius = HexGrid.HexWidth / 2 - 4;
                                graphics.DrawEllipse(pen, pos.X - radius, pos.Y - radius, radius * 2, radius * 2);
                            }
                        }
                    }

                    capture2.Save();
                }
            }

            return cells;
        }

        private IEnumerable<Vector2> FindNewAtomsInternal(ScreenCapture capture)
        {
            using (var data = new LockedBitmapData(capture.Bitmap))
            {
                var visibleCells = m_grid.GetVisibleCells();
                for (int y = visibleCells.Min.Y; y <= visibleCells.Max.Y; y++)
                {
                    for (int x = visibleCells.Min.X; x <= visibleCells.Max.X; x++)
                    {
                        var cell = new Vector2(x, y);
                        var screenLocation = m_grid.GetScreenLocationForCell(cell);
                        var testLocation = screenLocation.Subtract(m_grid.Rect.Location).Add(HueTestOffset);
                        if (testLocation.X >= 0 && testLocation.Y >= 0 && testLocation.X < capture.Bitmap.Width && testLocation.Y < capture.Bitmap.Height)
                        {
                            // Only consider cells we haven't already tested
                            if (m_seenCells.Add(cell))
                            {
                                if (data.GetPixel(testLocation.X, testLocation.Y).IsWithinHueThresholds(LowerHueThreshold, UpperHueThreshold))
                                {
                                    sm_log.Info(Invariant($"Found atom at {cell}"));
                                    yield return cell;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
