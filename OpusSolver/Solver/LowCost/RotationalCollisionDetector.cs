using System;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    public class RotationalCollisionDetector
    {
        private readonly GridState m_gridState;

        private const float HexSizeX = 82;
        private const float HexSizeY = 71;
        private const float AtomRadius = 29;
        private const float ProducedAtomRadius = 15;
        private const float ArmBaseRadius = 20;

        private const float SixtyDegrees = MathF.PI / 3.0f;

        private struct RectVector2
        {
            public float X;
            public float Y;

            public RectVector2(float x, float y)
            {
                X = x;
                Y = y;
            }

            public RectVector2(Vector2 v)
            {
                X = (v.X + v.Y / 2.0f) * HexSizeX;
                Y = v.Y * HexSizeY;
            }

            public readonly float Length => MathF.Sqrt(X * X + Y * Y);

            /// <summary>
            /// Calculates the 2D cross product of this vector with another. The result will be negative if this vector
            /// points in a counterclockwise direction compared to the other, zero if they are parallel, or positive
            /// if this vector points in a clockwise direction compared to the other.
            /// </summary>
            public readonly float CrossProduct(RectVector2 other)
            {
                return X * other.Y - Y * other.X;
            }
        }

        public RotationalCollisionDetector(GridState gridState)
        {
            m_gridState = gridState;
        }

        /// <summary>
        /// Checks if any atoms will collide with any other atoms/arms in the grid when an arm rotates. Currently assumes
        /// all other atoms/arms are stationary.
        /// </summary>
        /// <param name="atoms">The atoms to check for collisions</param>
        /// <param name="currentAtomsTransform">The current transform of these atoms (overrides atoms.WorldTransform)</param>
        /// <param name="armTransform">The current transform of the arm that will rotate the atoms</param>
        /// <param name="deltaRotation">The direction the arm is rotating</param>
        /// <returns>True if any of the atoms will collide; false otherwise</returns>
        public bool WillAtomsCollide(AtomCollection atoms, Transform2D currentAtomsTransform, Transform2D armTransform, HexRotation deltaRotation)
        {
            if (deltaRotation != HexRotation.R60 && deltaRotation != HexRotation.R300)
            {
                throw new ArgumentException($"{nameof(WillAtomsCollide)} only supports rotations by +/- 60 degrees but was given {deltaRotation}.");
            }

            return atoms.GetTransformedAtomPositions(currentAtomsTransform).Any(p => WillAtomCollide(p.position, armTransform, deltaRotation));
        }

        private bool WillAtomCollide(Vector2 atom1Pos, Transform2D armTransform, HexRotation deltaRotation)
        {
            // Assuming atom1 is moving in a circular arc and the other atom is stationary, the minimum distance
            // between the two atoms will occur when they are collinear with the center of rotation. Therefore
            // we can simply calculate the distance of each atom from the center and compare it against the
            // radii of the atoms.
            var atom1Offset = new RectVector2(atom1Pos - armTransform.Position);
            float atom1DistanceFromCenter = atom1Offset.Length;

            foreach (var atom2Pos in m_gridState.GetAllAtomPositions())
            {
                if (atom2Pos == atom1Pos)
                {
                    // This can happen when rotating an atom out of an input glyph
                    continue;
                }

                var atom2Offset = new RectVector2(atom2Pos - armTransform.Position);
                float atom2DistanceFromCenter = atom2Offset.Length;

                float minSeparation = Math.Abs(atom2DistanceFromCenter - atom1DistanceFromCenter);
                if (minSeparation < 2 * AtomRadius)
                {
                    // Check that the atoms are in the same sextant
                    float cross = atom1Offset.CrossProduct(atom2Offset);
                    if (deltaRotation == HexRotation.R60 && cross > 0 || deltaRotation == HexRotation.R300 && cross < 0)
                    {
                        cross = new RectVector2((atom1Pos - armTransform.Position).RotateBy(deltaRotation)).CrossProduct(atom2Offset);
                        if (deltaRotation == HexRotation.R60 && cross < 0 || deltaRotation == HexRotation.R300 && cross > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            // TODO: Check for arm collisions too
            return false;
        }
    }
}