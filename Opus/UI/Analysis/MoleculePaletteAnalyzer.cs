using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Analyzes the products or reagents palette in the sidebar.
    /// </summary>
    public class MoleculePaletteAnalyzer : Analyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(MoleculePaletteAnalyzer));

        private const float BrightnessThreshold = 0.2f;
        private const int MaxMolecules = 100; // Just so we don't hang if something goes wrong

        private Palette<int, Molecule> m_palette;
        private HexGrid m_grid;
        private MoleculeType m_type;
        private Sidebar m_sidebar;

        public MoleculePaletteAnalyzer(Palette<int, Molecule> palette, Sidebar sidebar, HexGrid grid, MoleculeType type, ScreenCapture capture)
            : base(capture)
        {
            m_palette = palette;
            m_grid = grid;
            m_sidebar = sidebar;
            m_type = type;
        }

        public void Analyze()
        {
            var prevLocation = new Point(0, 0);
            while (m_palette.Tools.Count() < MaxMolecules)
            {
                // Look for a pixel which is not part of the background of the palette, and not part of a molecule we've
                // already analyzed.
                var searchRect = new Rectangle(0, prevLocation.Y, Capture.Bitmap.Width, Capture.Bitmap.Height - prevLocation.Y);
                var location = BitmapUtils.FindFirstPoint(Capture.Bitmap, searchRect, col => col.GetBrightness() > BrightnessThreshold);
                if (location == null)
                {
                    break;
                }

                sm_log.Info(Invariant($"Found molecule at {location}"));

                // The sidebar does not scroll very accurately - sometimes the molecules move by 0 pixels and sometimes by 1 or 2.
                // Because of this we set the location to the pixel below, under the assumption that a molecule won't
                // have an isolated pixel at the top.
                var realLoc = location.Value;
                realLoc.Y++;

                var molecule = AnalyzeMolecule(realLoc);
                molecule.ID = m_palette.Tools.Count();
                m_palette.AddTool(molecule.ID, new Tool<Molecule>(molecule, realLoc));

                prevLocation = realLoc;
            }
        }

        private Molecule AnalyzeMolecule(Point location)
        {
            // Make sure the molecule is visible in the sidebar
            var screenLocation = m_sidebar.ScrollTo(m_palette, location);

            Molecule molecule;
            bool edgeChanged;

            using (var captures = new DisposableList<ScreenCapture>())
            {
                var sidebarCapture1 = captures.Add(new ScreenCapture(m_sidebar.Rect));

                // To analyze the molecule, drag it onto the grid. This will expand it to full size and make it much easier to analyze.
                sm_log.Info("Centering grid");
                m_grid.ScrollTo(new Vector2(0, 0));

                sm_log.Info("Dragging molecule onto grid");
                MouseUtils.LeftDrag(screenLocation, m_grid.GetScreenLocationForCell(new Vector2(0, 0)));
                molecule = new MoleculeAnalyzer(m_grid, m_type).Analyze();
                sm_log.Info("Analyzed molecule:" + Environment.NewLine + molecule.ToString());

                var sidebarCapture2 = captures.Add(new ScreenCapture(m_sidebar.Rect));
                edgeChanged = ExcludeChangedPixels(sidebarCapture1, sidebarCapture2);
                sm_log.Info("edgeChanged: " + edgeChanged);
            }

            using (var captures = new DisposableList<ScreenCapture>())
            {
                if (edgeChanged)
                {
                    // A pixel on the bottom edge of the visible part of the sidebar changed, which means
                    // the molecule probably extends onto the next page. So we scroll down and then exclude
                    // any pixels that change there when we delete the molecule.
                    m_sidebar.Area.ScrollBy(new Point(0, m_sidebar.Rect.Height));
                    var sidebarCapture1 = captures.Add(new ScreenCapture(m_sidebar.Rect));

                    // Delete the molecule from the grid
                    KeyboardUtils.KeyPress(Keys.Z);

                    var sidebarCapture2 = captures.Add(new ScreenCapture(m_sidebar.Rect));
                    ExcludeChangedPixels(sidebarCapture1, sidebarCapture2);

                    // Technically the molecule could overlap a third page but we'll ignore that for now.
                }
                else
                {
                    // Delete the molecule from the grid
                    KeyboardUtils.KeyPress(Keys.Z);
                }
            }

            return molecule;
        }

        /// <summary>
        /// Blackens any pixels that have changed as a result of moving the molecule onto the grid.
        /// This is so we can determine if there are any more molecules to analyze.
        /// </summary>
        private bool ExcludeChangedPixels(ScreenCapture capture1, ScreenCapture capture2)
        {
            bool edgeChanged = false;
            using (var data1 = new LockedBitmapData(capture1.Bitmap))
            using (var data2 = new LockedBitmapData(capture2.Bitmap))
            using (var outputData = new LockedBitmapData(Capture.Bitmap, writeable: true))
            {
                for (int y = 0; y < capture1.Rect.Height; y++)
                {
                    for (int x = 0; x < capture1.Rect.Width; x++)
                    {
                        if (data1.GetPixel(x, y) != data2.GetPixel(x, y))
                        {
                            edgeChanged |= (y == capture1.Rect.Height - 1);

                            // Transform to screen coordinates
                            var location = new Point(x, y).Add(capture1.Rect.Location);

                            // Transform to palette coordinates
                            location = location.Add(m_sidebar.Area.ScrollPosition).Subtract(m_palette.ScrollPosition);

                            // Due to inaccuracies in the scrolling of the sidebar we might be off by 1 in Y. So we blacken the pixels above/below too.
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                var location2 = location.Add(new Point(0, dy));
                                if (Capture.Rect.Contains(location2))
                                {
                                    location2 = location2.Subtract(Capture.Rect.Location);
                                    outputData.SetPixel(location2.X, location2.Y, Color.Black);
                                }
                            }
                        }
                    }
                }
            }

            return edgeChanged;
        }
    }
}
