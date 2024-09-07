using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

        public static HexRotation operator *(HexRotation left, int right)
        {
            return new HexRotation((left.IntValue * right) % Count);
        }

        public override string ToString()
        {
            return (m_angle * 60).ToString(CultureInfo.InvariantCulture);
        }

        public int CompareTo(HexRotation other)
        {
            return this.IntValue - other.IntValue;
        }

        /// <summary>
        /// Returns the smallest number of rotations required to get from this rotation to another one.
        /// This will be either 0, 1, 2 or 3 depending on the difference between the two rotations.
        /// </summary>
        public int DistanceTo(HexRotation other)
        {
            return 3 - Math.Abs((other - this).IntValue - 3);
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
            var rot = this;
            var deltaRotations = CalculateDeltaRotationsTo(targetRot, rotateClockwiseIf180Degrees);
            foreach (var delta in deltaRotations)
            {
                rot += delta;
                yield return rot;
            }
        }

        /// <summary>
        /// Calculates the shortest sequence of rotations to get from the current rotation to targetRot.
        /// </summary>
        /// <param name="targetRot">The target rotation</param>
        /// <param name="rotateClockwiseIf180Degrees">If true, then use clockwise rotations if the difference between the
        /// current rotation and the target is exactly 180 degrees; if false, use counterclockwise rotations</param>
        /// <returns>A list of the rotation deltas required to get to the target rotation. These will be a sequence of
        /// either HexRotation.R60 or -HexRotation.R60 depending on the direction.
        /// Returns an empty list if no rotations are required.</returns>
        public IEnumerable<HexRotation> CalculateDeltaRotationsTo(HexRotation targetRot, bool rotateClockwiseIf180Degrees = false)
        {
            var numRotations = (targetRot - this).IntValue;
            if (numRotations > 3 || (numRotations == 3 && rotateClockwiseIf180Degrees))
            {
                return Enumerable.Repeat(-R60, Count - numRotations);
            }
            else
            {
                return Enumerable.Repeat(R60, numRotations);
            }
        }
    }
}
