using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Opus
{
    public static class BitmapUtils
    {
        /// <summary>
        /// Applies a transformation to each pixel of a bitmap.
        /// </summary>
        public static void TransformPixels(Bitmap bitmap, Func<Color, Color> transformFunc)
        {
            using (var data = new LockedBitmapData(bitmap, writeable: true))
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        data.SetPixel(x, y, transformFunc(data.GetPixel(x, y)));
                    }
                }
            }
        }

        /// <summary>
        /// Finds the first point in a bitmap for which the specified predicate is true.
        /// The search begins at the top left of the rectangle and proceeds by row.
        /// </summary>
        /// <param name="bitmap">The bitmap to search</param>
        /// <param name="searchRect">The rectangle within the bitmap to search</param>
        /// <param name="predicate">A function which will be evaluate for each pixel until it
        /// returns true, or all the pixels in the rectangle have been tested</param>
        /// <returns>The first point for which the predicate is true, or null if none were true</returns>
        public static Point? FindFirstPoint(Bitmap bitmap, Rectangle searchRect, Func<Color, bool> predicate)
        {
            using (var data = new LockedBitmapData(bitmap))
            {
                for (int y = searchRect.Top; y < searchRect.Bottom; y++)
                {
                    for (int x = searchRect.Left; x < searchRect.Right; x++)
                    {
                        if (predicate(data.GetPixel(x, y)))
                        {
                            return new Point(x, y);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Same as FindFirstPoint but the search begins at the bottom right and proceeds backwards.
        /// </summary>
        public static Point? FindLastPoint(Bitmap bitmap, Rectangle searchRect, Func<Color, bool> predicate)
        {
            using (var data = new LockedBitmapData(bitmap))
            {
                for (int y = searchRect.Bottom - 1; y >= searchRect.Top; y--)
                {
                    for (int x = searchRect.Right - 1; x >= searchRect.Left; x--)
                    {
                        if (predicate(data.GetPixel(x, y)))
                        {
                            return new Point(x, y);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new bitmap consisting of all the specified bitmaps joined vertically togeether.
        /// The bitmaps must be all the same width.
        /// </summary>
        public static Bitmap JoinBitmapsVertically(IEnumerable<Bitmap> bitmaps)
        {
            int width = bitmaps.First().Width;
            if (!bitmaps.All(bitmap => bitmap.Width == width))
            {
                throw new ArgumentException("Bitmaps must all be the same width.");
            }

            int height = bitmaps.Sum(bitmap => bitmap.Height);
            var joinedBitmap = new Bitmap(width, height);
            var pos = new Point(0, 0);
            using (var graphics = Graphics.FromImage(joinedBitmap))
            {
                foreach (var bitmap in bitmaps)
                {
                    graphics.DrawImageUnscaled(bitmap, pos);
                    pos.Y += bitmap.Height;
                }
            }

            return joinedBitmap;
        }
    }
}
