using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpusSolver
{
    /// <summary>
    /// Represents a rotation on the hex grid, where a positive rotation represents a counterclockwise rotation.
    /// When interpreted as a direction, a rotation of 0 is in the direction of the positive X axis.
    /// </summary>
    public readonly struct HexRotation : IEquatable<HexRotation>, IComparable<HexRotation>
    {
        private readonly int m_angle;

        public HexRotation(int angle)
        {
            m_angle = angle;
        }

        public static readonly HexRotation R0 = new(0);
        public static readonly HexRotation R60 = new(1);
        public static readonly HexRotation R120 = new(2);
        public static readonly HexRotation R180 = new(3);
        public static readonly HexRotation R240 = new(4);
        public static readonly HexRotation R300 = new(5);

        public static readonly HexRotation[] All = [R0, R60, R120, R180, R240, R300];

        public const int Count = 6;

        public int IntValue => m_angle;

        public HexRotation Rotate60Counterclockwise()
        {
            return new HexRotation((m_angle + 1) % Count);
        }

        public HexRotation Rotate60Clockwise()
        {
            return new HexRotation((m_angle - 1 + Count) % Count);
        }

        public HexRotation Rotate180()
        {
            return new HexRotation((m_angle + Count / 2) % Count);
        }

        public override bool Equals(object obj)
        {
            return obj is HexRotation rotation && Equals(rotation);
        }

        public bool Equals(HexRotation other)
        {
            return m_angle == other.m_angle;
        }

        public override int GetHashCode()
        {
            return m_angle.GetHashCode();
        }

        public static bool operator ==(HexRotation left, HexRotation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexRotation left, HexRotation right)
        {
            return !(left == right);
        }

        public static HexRotation operator +(HexRotation left, HexRotation right)
        {
            return new HexRotation((left.IntValue + right.IntValue) % Count);
        }

        public static HexRotation operator -(HexRotation left, HexRotation right)
        {
            return new HexRotation((left.IntValue - right.IntValue + Count) % Count);
        }

        public static HexRotation operator -(HexRotation rot)
        {
            return new HexRotation((-rot.IntValue + Count) % Count);
        }

        public override string ToString()
        {
            return m_angle.ToString(CultureInfo.InvariantCulture);
        }

        public int CompareTo(HexRotation other)
        {
            return this.IntValue - other.IntValue;
        }

        /// <summary>
        /// Calculates the shortest sequence of rotations to get from the current rotation to targetRot.
        /// </summary>
        /// <param name="targetRot">The target rotation</param>
        /// <param name="rotateClockwiseIf180Degrees">If true, then use clockwise rotations if the difference between the
        /// current rotation and the target is exactly 180 degrees; if false, use counterclockwise rotations</param>
        /// <returns>A list of the intermediate rotations, including the target rotation.
        /// Returns an empty list if no rotations are required.</returns>
        public IEnumerable<HexRotation> CalculateRotationsTo(HexRotation targetRot, bool rotateClockwiseIf180Degrees = false)
        {
            var numRotations = (targetRot - this).IntValue;
            var rotationDir = R60;
            if (numRotations > 3 || (numRotations == 3 && rotateClockwiseIf180Degrees))
            {
                numRotations = Count - numRotations;
                rotationDir = -R60;
            }

            var rot = this;
            for (int i = 0; i < numRotations; i++)
            {
                rot += rotationDir;
                yield return rot;
            }
        }
    }
}
