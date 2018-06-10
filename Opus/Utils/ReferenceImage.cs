using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using static System.FormattableString;

namespace Opus
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
    public class ThresholdData
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public float Lower;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public float Upper;
    }

    public class ReferenceImage : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public IColorComparer ColorComparer { get; private set; }
        private int m_maxDifference;

        public static ReferenceImage CreateToleranceImage(string file, int tolerance, int maxDifference)
        {
            var image = new ReferenceImage(file, maxDifference);
            image.ColorComparer = new ToleranceColorComparer(tolerance);

            return image;
        }

        public static ReferenceImage CreateBrightnessThresholdedImage(string file, ThresholdData thresholds, int maxDifference)
        {
            var image = new ReferenceImage(file, maxDifference);
            image.ColorComparer = new BrightnessThresholdComparer(thresholds.Lower, thresholds.Upper);

            BitmapUtils.TransformPixels(image.Bitmap, col => col.ApplyBrightnessThreshold(thresholds.Lower, thresholds.Upper));

            return image;
        }

        public static ReferenceImage CreateHueThresholdedImage(string file, ThresholdData thresholds, int maxDifference)
        {
            var image = new ReferenceImage(file, maxDifference);
            image.ColorComparer = new HueThresholdComparer(thresholds.Lower, thresholds.Upper);

            BitmapUtils.TransformPixels(image.Bitmap, col => col.ApplyHueThreshold(thresholds.Lower, thresholds.Upper));

            return image;
        }

        private ReferenceImage(string file, int maxDifference)
        {
            var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(file);
            if (stream == null)
            {
                throw new FileNotFoundException(Invariant($"Can't find image resource {file}"));
            }

            Bitmap = new Bitmap(stream);
            m_maxDifference = maxDifference;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Bitmap.Dispose();
            }
        }

        public bool IsMatch(Bitmap bitmap, Point centerLocation)
        {
            return BitmapComparer.CalculateDifference(bitmap, GetCompareLocation(centerLocation), Bitmap, ColorComparer, m_maxDifference) <= m_maxDifference;
        }

        public int CalculateDifference(Bitmap bitmap, Point centerLocation)
        {
            return BitmapComparer.CalculateDifference(bitmap, GetCompareLocation(centerLocation), Bitmap, ColorComparer);
        }

        private Point GetCompareLocation(Point centerLocation)
        {
            return new Point(centerLocation.X - Bitmap.Width / 2, centerLocation.Y - Bitmap.Height / 2);
        }
    }
}
