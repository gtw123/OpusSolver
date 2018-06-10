using System.Drawing;

namespace Opus
{
    public static class RectangleExtensions
    {
        public static Rectangle Add(this Rectangle rect, Point point)
        {
            return new Rectangle(rect.Location.Add(point), rect.Size);
        }

        public static Rectangle Subtract(this Rectangle rect, Point point)
        {
            return new Rectangle(rect.Location.Subtract(point), rect.Size);
        }
    }
}
