using System;
using static System.FormattableString;

namespace OpusSolver
{
    public struct Vector2
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

        public override bool Equals(object obj)
        {
            if (!(obj is Vector2))
            {
                return false;
            }

            Vector2 v = (Vector2)obj;
            return X == v.X && Y == v.Y;
        }

        public override int GetHashCode()
        {
            return X * 65536 + Y;
        }

        public static bool operator ==(Vector2 v1, Vector2 v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(Vector2 v1, Vector2 v2)
        {
            return !v1.Equals(v2);
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return v1.Add(v2);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return v1.Subtract(v2);
        }

        public static Vector2 operator *(Vector2 v, int scale)
        {
            return v.Multiply(scale);
        }

        public Vector2 Add(Vector2 other)
        {
            return new Vector2(X + other.X, Y + other.Y);
        }

        public Vector2 Add(int x, int y)
        {
            return new Vector2(X + x, Y + y);
        }

        public Vector2 Subtract(Vector2 other)
        {
            return new Vector2(X - other.X, Y - other.Y);
        }

        public Vector2 Subtract(int x, int y)
        {
            return new Vector2(X - x, Y - y);
        }

        public Vector2 Multiply(int scale)
        {
            return new Vector2(X * scale, Y * scale);
        }

        public Vector2 Rotate60Counterclockwise()
        {
            return new Vector2(-Y, X + Y);
        }

        public Vector2 Rotate60Clockwise()
        {
            return new Vector2(X + Y, -X);
        }

        public Vector2 OffsetInDirection(int direction, int length)
        {
            switch (direction)
            {
                case Direction.E:  return new Vector2(X + length, Y);
                case Direction.NE: return new Vector2(X, Y + length);
                case Direction.NW: return new Vector2(X - length, Y + length);
                case Direction.W:  return new Vector2(X - length, Y);
                case Direction.SW: return new Vector2(X, Y - length);
                case Direction.SE: return new Vector2(X + length, Y - length);
                default:
                    throw new ArgumentException(Invariant($"Invalid direction {direction}."));
            }
        }

        public float DistanceToSquared(Vector2 other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        public override string ToString()
        {
            return Invariant($"({X}, {Y})");
        }
    }
}
