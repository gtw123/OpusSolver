using System;

namespace OpusSolver
{
    public static class MathUtils
    {
        public static int? Min(int? x, int? y)
        {
            if (x.HasValue && y.HasValue)
            {
                return Math.Min(x.Value, y.Value);
            }
            else
            {
                return x ?? y;
            }
        }
    }
}
