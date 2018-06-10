using System;
using System.Collections.Generic;
using System.Drawing;

namespace Opus
{
    public static class LineLocator
    {
        /// <summary>
        /// Finds all vertical lines within a bitmap that match the specified conditions.
        /// </summary>
        /// <param name="bitmap">The bitmap to search</param>
        /// <param name="searchRect">The rectangle within the bitmap to search</param>
        /// <param name="minLength">The minimum length of lines to find</param>
        /// <param name="predicate">A function that returns true if a pixel is considered to be part of a line</param>
        /// <returns>A list of all vertical lines found</returns>
        public static IEnumerable<Rectangle> FindVerticalLines(Bitmap bitmap, Rectangle searchRect, int minLength, Func<Color, bool> predicate)
        {
            using (var data = new LockedBitmapData(bitmap))
            {
                for (int x = searchRect.Left; x < searchRect.Right; x++)
                {
                    for (int y = searchRect.Top; y < searchRect.Bottom; y++)
                    {
                        int length = GetVerticalLineLength(data, x, y, predicate);
                        if (length >= minLength)
                        {
                            yield return new Rectangle(x, y, 1, length);

                            // Jump past this line to avoid returning duplicates
                            y += length;
                        }
                    }
                }
            }
        }

        private static int GetVerticalLineLength(LockedBitmapData data, int x, int startY, Func<Color, bool> predicate)
        {
            int y;
            for (y = startY; y < data.Bitmap.Height; y++)
            {
                if (!predicate(data.GetPixel(x, y)))
                {
                    break;
                }
            }

            return y - startY;
        }

        /// <summary>
        /// Finds the first row within a bitmap for which all the pixels are the specified color.
        /// The search begins at the specified row and contains downwards until a match is found.
        /// </summary>
        /// <param name="bitmap">The bitmap to check</param>
        /// <param name="startY">The first row of the bitmap to check</param>
        /// <param name="color">The color to check rows against</param>
        /// <param name="comparer">The comparer to use to compare the pixels against the specified color</param>
        /// <returns>The Y coordinate of the first row which is the specified color, or null if none were found</returns>
        public static int? FindHorizontalLineWithColor(Bitmap bitmap, int startY, Color color, IColorComparer comparer)
        {
            using (var data = new LockedBitmapData(bitmap))
            {
                for (int y = startY; y < bitmap.Height; y++)
                {
                    if (IsHorizontalLineWithColor(data, y, color, comparer))
                    {
                        return y;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks whether all pixels within a row of a bitmap are the specified color.
        /// </summary>
        /// <param name="bitmap">The bitmap to check</param>
        /// <param name="y">The row of the bitmap to check</param>
        /// <param name="color">The color to check the row against</param>
        /// <param name="comparer">The comparer to use to compare the pixels against the specified color</param>
        /// <returns>Whether all the pixels within the row are the specified color</returns>
        public static bool IsHorizontalLineWithColor(Bitmap bitmap, int y, Color color, IColorComparer comparer)
        {
            using (var data = new LockedBitmapData(bitmap))
            {
                return IsHorizontalLineWithColor(data, y, color, comparer);
            }
        }

        private static bool IsHorizontalLineWithColor(LockedBitmapData data, int y, Color color, IColorComparer comparer)
        {
            for (int x = 0; x < data.Bitmap.Width; x++)
            {
                if (!comparer.Compare(data.GetPixel(x, y), color))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
