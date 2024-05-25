namespace OpusSolver
{
    /// <summary>
    /// These constants represent a direction on the hex grid. We don't use enums here because that
    /// makes it awkward to convert to/from ints.
    /// Technically NE should be "60 degrees CCW from the X axis", but "NE" is easier to visualize!
    /// </summary>
    public static class Direction
    {
        public const int E = 0;
        public const int NE = 1;
        public const int NW = 2;
        public const int W = 3;
        public const int SW = 4;
        public const int SE = 5;
        public const int Count = 6;
    }

    public static class DirectionUtil
    {
        public static int RotateBy(int direction, int rotation)
        {
            return (direction + rotation + Direction.Count) % Direction.Count;
        }

        public static int Rotate60Counterclockwise(int direction)
        {
            return (direction + 1) % Direction.Count;
        }

        public static int Rotate60Clockwise(int direction)
        {
            return (direction - 1 + Direction.Count) % Direction.Count;
        }

        public static int Rotate180(int direction)
        {
            return (direction + Direction.Count / 2) % Direction.Count;
        }
    }
}
