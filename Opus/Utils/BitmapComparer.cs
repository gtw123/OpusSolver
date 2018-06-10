using System;
using System.Drawing;

namespace Opus
{
    public static class BitmapComparer
    {
        public static bool AreBitmapsIdentical(Bitmap bitmap1, Bitmap bitmap2)
        {
            if (bitmap1.Size != bitmap2.Size)
            {
                return false;
            }

            using (var data1 = new LockedBitmapData(bitmap1))
            using (var data2 = new LockedBitmapData(bitmap2))
            {
                int len = data1.Data.Stride * data1.Data.Height;
                return NativeMethods.memcmp(data1.Data.Scan0, data2.Data.Scan0, new UIntPtr((uint)len)) == 0;
            }
        }

        /// <summary>
        /// Calculates the number of pixels that are different between a region of a test bitmap and a reference bitmap.
        /// </summary>
        /// <param name="bitmap">The test bitmap</param>
        /// <param name="offset">The offset from the top left of the test bitmap of the region to be compared
        /// against the reference bitmap</param>
        /// <param name="refBitmap">The reference bitmap</param>
        /// <param name="comparer">The comparer to use to compare pixels of the two bitmaps</param>
        /// <param name="maxDifference">The maximum number of different pixels before the comparision will stop early.
        /// If null, all pixels will be compared.</param>
        /// <returns>The number of different pixels</returns>
        public static int CalculateDifference(Bitmap bitmap, Point offset, Bitmap refBitmap, IColorComparer comparer, int? maxDifference = null)
        {
            if (!IsRectWithinRect(bitmap.Size, offset, refBitmap.Size))
            {
                return int.MaxValue;
            }

            using (var data1 = new LockedBitmapData(bitmap))
            using (var data2 = new LockedBitmapData(refBitmap))
            {
                return CalculateBitmapRectDifference(data1, offset, data2, new Point(0, 0), refBitmap.Size, comparer, maxDifference);
            }
        }

        /// <summary>
        /// Calculates how much of the bottom of bitmap1 is the same as the top of bitmap2.
        /// </summary>
        public static int CalculateVerticalOverlap(Bitmap bitmap1, Bitmap bitmap2, IColorComparer comparer, int maxDifference)
        {
            if (bitmap1.Width != bitmap2.Width)
            {
                return 0;
            }

            using (var data1 = new LockedBitmapData(bitmap1))
            using (var data2 = new LockedBitmapData(bitmap2))
            {
                // If bitmap2 is shorter than bitmap1, start further down, otherwise start at 0.
                int startY = Math.Max(0, bitmap1.Height - bitmap2.Height);
                for (int y1 = startY; y1 < bitmap1.Height; y1++)
                {
                    if (CalculateBitmapRectDifference(data1, new Point(0, y1), data2, new Point(0, 0), new Size(bitmap1.Width, bitmap1.Height - y1), comparer, maxDifference) <= maxDifference)
                    {
                        return bitmap1.Height - y1;
                    }
                }

                return 0;
            }
        }

        private static int CalculateBitmapRectDifference(LockedBitmapData data1, Point offset1, LockedBitmapData data2, Point offset2, Size size, IColorComparer comparer, int? maxDifference = null)
        {
            int diffs = 0;
            for (int y = 0; y < size.Height; y++)
            {
                for (int x = 0; x < size.Width; x++)
                {
                    var col1 = data1.GetPixel(x + offset1.X, y + offset1.Y);
                    var col2 = data2.GetPixel(x + offset2.X, y + offset2.Y);
                    if (!comparer.Compare(col1, col2))
                    {
                        diffs++;
                        if (maxDifference.HasValue && diffs > maxDifference.Value)
                        {
                            return diffs;
                        }
                    }
                }
            }

            return diffs;
        }

        private static bool IsRectWithinRect(Size sourceRect, Point offset, Size otherRect)
        {
            if (offset.X < 0 || offset.Y < 0)
            {
                return false;
            }

            if (offset.X + otherRect.Width > sourceRect.Width || offset.Y + otherRect.Height > sourceRect.Height)
            {
                return false;
            }

            return true;
        }
    }
}
