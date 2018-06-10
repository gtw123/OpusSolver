using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Finds palettes in the sidebar when the sidebar scrolls continuously (which happens when
    /// one of the palettes is taller than the visible height of the sidebar).
    /// </summary>
    public class ContinuousScrollingPaletteFinder : PaletteFinder
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ContinuousScrollingPaletteFinder));
        private IColorComparer m_overlapComparer = new ToleranceColorComparer(5);

        public ContinuousScrollingPaletteFinder(Rectangle sidebarRect)
            : base(sidebarRect)
        {
        }

        public override DisposableList<PaletteInfo> Analyze()
        {
            sm_log.Info("Analyzing continuously scrolling palettes");
            using (var capture = CaptureWholeSidebar())
            {
                TotalScrollableHeight = capture.Bitmap.Height;
                return new DisposableList<PaletteInfo>(FindPalettes(capture));
            }
        }

        private ScreenCapture CaptureWholeSidebar()
        {
            using (var captures = GetSidebarCaptures())
            {
                // Join everything together into one big bitmap
                sm_log.Info("Joining captures");
                var bitmap = BitmapUtils.JoinBitmapsVertically(captures.Select(capture => capture.Bitmap));
                return new ScreenCapture(bitmap, new Rectangle(SidebarRect.Left, SidebarRect.Top, bitmap.Width, bitmap.Height));
            }
        }

        private DisposableList<ScreenCapture> GetSidebarCaptures()
        {
            // Chop a bit off the bottom of the image because it starts to fade out there, making it more difficult to compare
            var captureRect = new Rectangle(SidebarRect.Left, SidebarRect.Top, SidebarRect.Width, SidebarRect.Height - 70);
            sm_log.Info("Capturing section 0 of the sidebar");
            var prevCapture = new ScreenCapture(captureRect);

            var captures = new DisposableList<ScreenCapture> { prevCapture.Clone() };

            const int maxIterations = 100;
            int i = 0;
            for (; i < maxIterations; i++)
            {
                // Don't scroll too much or it'll be difficult to tell how much of the last image
                // overlaps with the previous one when we get to the bottom of the scollable area.
                // The value below was chosen so that at least some part of the mechanism and glyph
                // palettes will overlap with the second last image.
                // This number also has to be odd because some parts of the sidebar scroll every other pixel
                // while some scroll every pixel.
                const int scrollDistance = 551;

                MouseUtils.RightDrag(SidebarRect.Location.Add(new Point(0, scrollDistance)), SidebarRect.Location);

                // Check if we've reached the bottom
                if (SidebarUtil.FindVisibleSidebarHeight() <= SidebarUtil.MaxHeight)
                {
                    break;
                }

                // Capture the next bit of the sidebar
                prevCapture.Dispose();
                prevCapture = new ScreenCapture(captureRect);
                sm_log.Info(Invariant($"Capturing section {i + 1} of the sidebar"));
                captures.Add(prevCapture.Clone(new Rectangle(0, prevCapture.Rect.Height - scrollDistance, prevCapture.Rect.Width, scrollDistance)));
            }

            if (i >= maxIterations)
            {
                throw new AnalysisException(Invariant($"Couldn't find the bottom of the sidebar after {i} attempts."));
            }

            // Capture the final bit of the sidebar
            var finalCapture = new ScreenCapture(SidebarRect);

            // Work out where it overlaps the previous part, and capture the overlap
            int overlap = BitmapComparer.CalculateVerticalOverlap(prevCapture.Bitmap, finalCapture.Bitmap, m_overlapComparer, 0);
            sm_log.Info("Capturing final section of the sidebar");
            captures.Add(finalCapture.Clone(new Rectangle(0, overlap, finalCapture.Rect.Width, finalCapture.Rect.Height - overlap)));

            prevCapture.Dispose();
            return captures;
        }

        private IEnumerable<PaletteInfo> FindPalettes(ScreenCapture capture)
        {
            sm_log.Info("Finding palettes");

            int prevY = 0;
            for (int i = 0; i < NumPalettes; i++)
            {
                int? paletteY = SidebarUtil.FindPaletteHeader(capture.Bitmap, prevY);
                if (paletteY == null)
                {
                    throw new AnalysisException(Invariant($"Expected to find {NumPalettes} palettes but only found {i}."));
                }

                if (i > 0)
                {
                    int y = prevY + PaletteHeaderHeight;
                    yield return new PaletteInfo
                    {
                        Capture = capture.Clone(new Rectangle(0, y, capture.Rect.Width, paletteY.Value - y - PaletteBottomSeparationHeight)),
                        ScrollPosition = new Point(0, 0)
                    };
                }

                if (i == NumPalettes - 1)
                {
                    int y = paletteY.Value + PaletteHeaderHeight;
                    yield return new PaletteInfo
                    {
                        Capture = capture.Clone(new Rectangle(0, y, capture.Rect.Width, capture.Rect.Height - y - PaletteFooterHeight)),
                        ScrollPosition = new Point(0, 0)
                    };
                }

                prevY = paletteY.Value + 1;
            }
        }
    }
}