using System.Drawing;

namespace Opus.UI.Analysis
{
    public static class SidebarUtil
    {
        private const int MaxHeightFromBottom = 270;
        public static int MaxHeight => ScreenCapture.ScreenBounds.Height - MaxHeightFromBottom;

        private const int LeftBorderX = 9;
        private const float SideLeftBorderBrightness = 0.09f;

        private static readonly Color sm_paletteHeaderColor1 = Color.FromArgb(157, 152, 151);
        private static readonly Color sm_paletteHeaderColor2 = Color.FromArgb(49, 43, 41);
        private static IColorComparer sm_lineComparer = new ToleranceColorComparer(9);

        public static int FindVisibleSidebarHeight()
        {
            var rect = new Rectangle(ScreenCapture.ScreenBounds.Left + LeftBorderX, ScreenCapture.ScreenBounds.Top, 1, ScreenCapture.ScreenBounds.Height);
            using (var capture = new ScreenCapture(rect))
            {
                var searchRect = new Rectangle(0, 0, 1, capture.Bitmap.Height);
                var location = BitmapUtils.FindLastPoint(capture.Bitmap, searchRect, col => col.GetBrightness() > SideLeftBorderBrightness);
                if (location == null)
                {
                    throw new AnalysisException("Could not find the bottom of the left side of the sidebar.");
                }

                return location.Value.Y + 1;
            }
        }
        
        public static int? FindPaletteHeader(Bitmap bitmap, int startY)
        {
            int y = startY;
            while (y < bitmap.Height)
            {
                int? lineY = LineLocator.FindHorizontalLineWithColor(bitmap, y, sm_paletteHeaderColor1, sm_lineComparer);
                if (!lineY.HasValue || lineY + 1 >= bitmap.Height)
                {
                    break;
                }

                if (LineLocator.IsHorizontalLineWithColor(bitmap, lineY.Value + 1, sm_paletteHeaderColor2, sm_lineComparer))
                {
                    return lineY;
                }

                y = lineY.Value + 1;
            }

            return null;
        }
    }
}
