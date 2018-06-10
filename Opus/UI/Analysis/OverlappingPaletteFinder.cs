using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Finds palettes in the sidebar when it has palettes that overlap each other when they scroll
    /// (which happens when none of the palettes are taller than the visible part of the sidebar).
    /// </summary>
    public class OverlappingPaletteFinder : PaletteFinder
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(OverlappingPaletteFinder));

        private bool m_isScrollable;

        public OverlappingPaletteFinder(Rectangle sidebarRect, bool isScrollable)
            : base(sidebarRect)
        {
            m_isScrollable = isScrollable;
        }

        public override DisposableList<PaletteInfo> Analyze()
        {
            sm_log.Info("Finding overlapping palettes");
            var palettes = new DisposableList<PaletteInfo>(FindPalettes());
            TotalScrollableHeight = palettes.Last().ScrollPosition.Y + SidebarRect.Height;

            return palettes;
        }

        private IEnumerable<PaletteInfo> FindPalettes()
        {
            sm_log.Info("Finding palettes");

            int startY = 0;
            int prevNextHeaderY = 0;
            int scrollPosition = 0;
            for (int i = 0; i < NumPalettes; i++)
            {
                using (var capture = new ScreenCapture(SidebarRect))
                {
                    int? headerY = SidebarUtil.FindPaletteHeader(capture.Bitmap, startY);
                    if (headerY == null)
                    {
                        throw new AnalysisException(Invariant($"Expected to find {NumPalettes} palettes but only found {i}."));
                    }

                    if (i > 0)
                    {
                        scrollPosition += prevNextHeaderY - headerY.Value;
                    }

                    int? nextHeaderY = SidebarUtil.FindPaletteHeader(capture.Bitmap, headerY.Value + 1);
                    if (nextHeaderY.HasValue)
                    {
                        // Capture between the two palette headers
                        int y = headerY.Value + PaletteHeaderHeight;
                        yield return new PaletteInfo
                        {
                            Capture = capture.Clone(new Rectangle(0, y, capture.Rect.Width, nextHeaderY.Value - y - PaletteBottomSeparationHeight)),
                            ScrollPosition = new Point(0, scrollPosition)
                        };

                        if (m_isScrollable)
                        {
                            // Scroll the next palette up so it's adjacent to the previous one (if possible)
                            MouseUtils.RightDrag(SidebarRect.Location.Add(new Point(0, nextHeaderY.Value)), SidebarRect.Location.Add(new Point(0, headerY.Value + PaletteHeaderSeparationHeight)));
                        }

                        prevNextHeaderY = nextHeaderY.Value;
                    }
                    else
                    {
                        // This must be the last palette, so capture to the bottom of the sidebar
                        int y = headerY.Value + PaletteHeaderHeight;
                        yield return new PaletteInfo
                        {
                            Capture = capture.Clone(new Rectangle(0, y, capture.Rect.Width, capture.Rect.Height - y - PaletteFooterHeight)),
                            ScrollPosition = new Point(0, scrollPosition)
                        };
                    }

                    // Next iteration, start looking for the palette just below the previous one
                    startY = headerY.Value + 1;
                }
            }
        }
    }
}