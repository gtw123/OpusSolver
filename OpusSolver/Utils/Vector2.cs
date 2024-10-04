using System;
using static System.FormattableString;

namespace OpusSolver
{
    public record struct Vector2
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int X;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public int Y;

        public Vector2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return v1.Add(v2);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return v1.Subtract(v2);
        }

        public static Vector2 operator -(Vector2 v)
        {
            return new Vector2(-v.X, -v.Y);
        }

        public static Vector2 operator *(Vector2 v, int scale)
        {
            return v.Multiply(scale);
        }

        public static Vector2 operator *(int scale, Vector2 v)
        {
            return v.Multiply(scale);
        }

        public readonly Vector2 Add(Vector2 other)
        {
            return new Vector2(X + other.X, Y + other.Y);
        }

        public readonly Vector2 Add(int x, int y)
        {
            return new Vector2(X + x, Y + y);
        }

        public readonly Vector2 Subtract(Vector2 other)
        {
            return new Vector2(X - other.X, Y - other.Y);
        }

        public readonly Vector2 Subtract(int x, int y)
        {
            return new Vector2(X - x, Y - y);
        }

        public readonly Vector2 Multiply(int scale)
        {
            return new Vector2(X * scale, Y * scale);
        }

        public readonly Vector2 RotateBy(HexRotation rotation)
        {
            return rotation.IntValue switch
            {
                0 => new Vector2(X, Y),
                1 => new Vector2(-Y, X + Y),
                2 => new Vector2(-X - Y, X),
                3 => new Vector2(-X, -Y),
                4 => new Vector2(Y, -X - Y),
                5 => new Vector2(X + Y, -X),
                _ => throw new ArgumentException(Invariant($"Invalid rotation {rotation.IntValue}"))
            };
        }

        public readonly Vector2 RotateAbout(Vector2 point, HexRotation rotation)
        {
            return (this - point).RotateBy(rotation) + point;
        }

        public readonly Vector2 Rotate60Counterclockwise()
        {
            return new Vector2(-Y, X + Y);
        }

        public readonly Vector2 Rotate60Clockwise()
        {
            return new Vector2(X + Y, -X);
        }

        public readonly Vector2 Rotate180()
        {
            return new Vector2(-X, -Y);
        }

        public readonly Vector2 OffsetInDirection(HexRotation direction, int length)
        {
            return direction.IntValue switch
            {
                0 => new Vector2(X + length, Y),
                1 => new Vector2(X, Y + length),
                2 => new Vector2(X - length, Y + length),
                3 => new Vector2(X - length, Y),
                4 => new Vector2(X, Y - length),
                5 => new Vector2(X + length, Y - length),
                _ => throw new ArgumentException(Invariant($"Invalid direction {direction}."))
            };
        }

        public readonly HexRotation? ToRotation()
        {
            if (Y == 0)
            {
                return (X > 0) ? HexRotation.R0 : (X < 0) ? HexRotation.R180 : null;
            }
            else if (X == 0)
            {
                return (Y > 0) ? HexRotation.R60 : (Y < 0) ? HexRotation.R240 : null;
            }
            else if (X + Y == 0)
            {
                return (Y > 0) ? HexRotation.R120 : HexRotation.R300;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the hexagonal Manhattan distance between this vector and another vector.
        /// </summary>
        public readonly int DistanceBetween(Vector2 other)
        {
            var diff = other - this;
            if (Math.Sign(diff.X) == Math.Sign(diff.Y))
            {
                return Math.Abs(diff.X + diff.Y);
            }
            else
            {
                return Math.Max(Math.Abs(diff.X), Math.Abs(diff.Y));
            }
        }

        public override readonly string ToString()
        {
            return Invariant($"({X}, {Y})");
        }
    }
}
