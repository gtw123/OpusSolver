using System.Collections.Generic;
using System.Drawing;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Analyzes the sidebar on the left side of the game window.
    /// </summary>
    public class SidebarAnalyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(SidebarAnalyzer));

        private const int PaletteLeft = 32;
        private const int PaletteWidth = 250;

        private HexGrid m_grid;
        private Rectangle m_sidebarRect;
        private bool m_isScrollable = false;
        private bool m_isContinuousScrolling;

        public SidebarAnalyzer(HexGrid grid)
        {
            m_grid = grid;
        }

        public Sidebar Analyze()
        {
            ScrollToTop();
            AnalyzeSidebar();

            sm_log.Info("Finding palettes");
            var finder = CreatePaletteFinder();
            using (var palettes = finder.Analyze())
            {
                return CreateSidebar(palettes, finder.TotalScrollableHeight);
            }
        }

        private void AnalyzeSidebar()
        {
            sm_log.Info("Analyzing sidebar");

            int height = SidebarUtil.FindVisibleSidebarHeight();
            if (height > SidebarUtil.MaxHeight)
            {
                m_isScrollable = true;
                height = SidebarUtil.MaxHeight;
            }

            if (height < 100)
            {
                throw new AnalysisException("Cannot find the sidebar.");
            }

            m_sidebarRect = new Rectangle(ScreenCapture.ScreenBounds.Left + PaletteLeft, ScreenCapture.ScreenBounds.Top, PaletteWidth, height);
            sm_log.Info(Invariant($"Sidebar screen rect: {m_sidebarRect}"));
            sm_log.Info(Invariant($"Sidebar is scrollable: {m_isScrollable}"));

            m_isContinuousScrolling = m_isScrollable && DetermineContinuousScrolling();
            sm_log.Info(Invariant($"Sidebar is continuous scrolling: {m_isContinuousScrolling}"));
        }

        private static void ScrollToTop()
        {
            sm_log.Info("Scrolling sidebar to top");

            var sidebarRect = new Rectangle(ScreenCapture.ScreenBounds.Location, new Size(ScreenLayout.SidebarWidth, SidebarUtil.MaxHeight));
            const int maxIterations = 100;
            for (int i = 0; i < maxIterations; i++)
            {
                // Drag upwards as much as we can
                MouseUtils.RightDrag(sidebarRect.Location, new Point(sidebarRect.Left, ScreenCapture.ScreenBounds.Height - 1));

                // To check if we've reached the top, try to drag upwards one more pixel. If nothing changes,
                // we're almost definitely at the top!
                using (var capture1 = new ScreenCapture(sidebarRect))
                {
                    MouseUtils.RightDrag(sidebarRect.Location, sidebarRect.Location.Add(new Point(0, 1)));
                    using (var capture2 = new ScreenCapture(sidebarRect))
                    {
                        if (BitmapComparer.AreBitmapsIdentical(capture1.Bitmap, capture2.Bitmap))
                        {
                            return;
                        }
                    }
                }
            }

            throw new AnalysisException(Invariant($"Failed to scroll to the top of the sidebar after {maxIterations} attempts."));
        }

        private bool DetermineContinuousScrolling()
        {
            // Scroll the sidebar up one pixel. If the top header moves, it's continuously scrolling.
            using (var capture1 = new ScreenCapture(m_sidebarRect))
            {
                MouseUtils.RightDrag(m_sidebarRect.Location.Add(new Point(0, 1)), m_sidebarRect.Location);
                using (var capture2 = new ScreenCapture(m_sidebarRect))
                {
                    MouseUtils.RightDrag(m_sidebarRect.Location, m_sidebarRect.Location.Add(new Point(0, 1)));

                    var height1 = SidebarUtil.FindPaletteHeader(capture1.Bitmap, 0);
                    var height2 = SidebarUtil.FindPaletteHeader(capture2.Bitmap, 0);
                    if (!height1.HasValue || !height2.HasValue)
                    {
                        throw new AnalysisException("Could not find any palette headers in the sidebar.");
                    }

                    return height1.Value != height2.Value;
                }
            }
        }

        private PaletteFinder CreatePaletteFinder()
        {
            if (m_isContinuousScrolling)
            {
                return new ContinuousScrollingPaletteFinder(m_sidebarRect);
            }
            else
            {
                return new OverlappingPaletteFinder(m_sidebarRect, m_isScrollable);
            }
        }

        private Sidebar CreateSidebar(List<PaletteInfo> palettes, int totalHeight)
        {
            var sidebar = new Sidebar(m_sidebarRect, totalHeight, totalHeight - m_sidebarRect.Height, m_isContinuousScrolling);
            
            sm_log.Info("Analyzing glyphs");
            var palette = palettes[3];
            sidebar.Glyphs = new Palette<GlyphType, GlyphType>(palette.ScrollPosition, palette.Capture.Rect.Subtract(m_sidebarRect.Location));
            new GlyphPaletteAnalyzer(sidebar.Glyphs, palette.Capture).Analyze();

            // Calibrate the hex grid - note we can't do this until we've analyzed the glyphs
            new HexGridCalibrator(m_grid, sidebar).Calibrate();

            sm_log.Info("Analyzing mechanisms");
            palette = palettes[2];
            sidebar.Mechanisms = new Palette<MechanismType, MechanismType>(palette.ScrollPosition, palette.Capture.Rect.Subtract(m_sidebarRect.Location));
            new MechanismPaletteAnalyzer(sidebar.Mechanisms, palette.Capture).Analyze();

            sm_log.Info("Analyzing products");
            palette = palettes[0];
            sidebar.Products = new Palette<int, Molecule>(palette.ScrollPosition, palette.Capture.Rect.Subtract(m_sidebarRect.Location));
            new MoleculePaletteAnalyzer(sidebar.Products, sidebar, m_grid, MoleculeType.Product, palette.Capture).Analyze();

            sm_log.Info("Analyzing reagants");
            palette = palettes[1];
            sidebar.Reagents = new Palette<int, Molecule>(palette.ScrollPosition, palette.Capture.Rect.Subtract(m_sidebarRect.Location));
            new MoleculePaletteAnalyzer(sidebar.Reagents, sidebar, m_grid, MoleculeType.Reagent, palette.Capture).Analyze();

            return sidebar;
        }
    }
}