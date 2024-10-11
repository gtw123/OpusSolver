using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
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
        /// Checks if any atoms will collide with any other atoms in the grid when they are rotated by an arm.
        /// Assumes all other atoms/arms are stationary.
        /// </summary>
        /// <param name="atoms">The atoms to check for collisions</param>
        /// <param name="currentAtomsTransform">The current transform of these atoms (overrides atoms.WorldTransform)</param>
        /// <param name="armPosition">The position of the base of the arm</param>
        /// <param name="deltaRotation">The direction of rotation</param>
        /// <returns>True if any of the atoms will collide; false otherwise</returns>
        public bool WillAtomsCollideWhileRotating(AtomCollection atoms, Transform2D currentAtomsTransform, Vector2 armPosition, HexRotation deltaRotation)
        {
            return WillAtomsCollide(atoms.GetTransformedAtomPositions(currentAtomsTransform), armPosition, armPosition, deltaRotation);
        }

        /// <summary>
        /// Checks if any atoms will collide with any other atoms in the grid when they are pivoted by an arm.
        /// Assumes all other atoms/arms are stationary.
        /// </summary>
        /// <param name="atoms">The atoms to check for collisions</param>
        /// <param name="currentAtomsTransform">The current transform of these atoms (overrides atoms.WorldTransform)</param>
        /// <param name="armPosition">The position of the base of the arm</param>
        /// <param name="grabberPosition">The position of the arm's grabber</param>
        /// <param name="deltaRotation">The direction of rotation</param>
        /// <returns>True if any of the atoms will collide; false otherwise</returns>
        public bool WillAtomsCollideWhilePivoting(AtomCollection atoms, Transform2D currentAtomsTransform, Vector2 armPosition, Vector2 grabberPosition, HexRotation deltaRotation)
        {
            return WillAtomsCollide(atoms.GetTransformedAtomPositions(currentAtomsTransform), armPosition, grabberPosition, deltaRotation);
        }

        private bool WillAtomsCollide(IEnumerable<(Atom atom, Vector2 position)> atomPositions, Vector2 armPosition, Vector2 rotationCenter, HexRotation deltaRotation)
        {
            var collidableAtomPositions = m_gridState.GetAllCollidableAtomPositions(atomPositions.Select(p => p.position)).ToArray();
            return atomPositions.Any(p => WillAtomCollide(p.position, armPosition, rotationCenter, deltaRotation, collidableAtomPositions));
        }

        private bool WillAtomCollide(Vector2 atomPos, Vector2 armPosition, Vector2 rotationCenter, HexRotation deltaRotation, IEnumerable<Vector2> collidableAtomPositions)
        {
            if (deltaRotation != HexRotation.R60 && deltaRotation != HexRotation.R300)
            {
                throw new ArgumentException($"{nameof(WillAtomCollide)} only supports rotations by +/- 60 degrees but was given {deltaRotation}.");
            }

            // Assuming the atom is moving in a circular arc and the other object is stationary, the minimum distance
            // between the two objects will occur when they are collinear with the center of rotation. Therefore we can
            // simply calculate the distance of each object from the center and compare it against the radii of the
            // objects.
            var atomOffset = new RectVector2(atomPos - rotationCenter);
            float atomDistanceFromCenter = atomOffset.Length;

            bool WillAtomCollideWithObject(Vector2 objectPos, float objectRadius)
            {
                if (objectPos == atomPos)
                {
                    // This can happen when rotating an atom out of an input glyph
                    return false;
                }

                var objectOffset = new RectVector2(objectPos - rotationCenter);
                float objectDistanceFromCenter = objectOffset.Length;

                float minSeparation = Math.Abs(objectDistanceFromCenter - atomDistanceFromCenter);
                if (minSeparation < AtomRadius + objectRadius)
                {
                    // Check that the atoms are in the same sextant
                    float cross = atomOffset.CrossProduct(objectOffset);
                    if (deltaRotation == HexRotation.R60 && cross >= 0 || deltaRotation == HexRotation.R300 && cross <= 0)
                    {
                        cross = new RectVector2((atomPos - rotationCenter).RotateBy(deltaRotation)).CrossProduct(objectOffset);
                        if (deltaRotation == HexRotation.R60 && cross <= 0 || deltaRotation == HexRotation.R300 && cross >= 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            if (collidableAtomPositions.Any(pos => WillAtomCollideWithObject(pos, AtomRadius)))
            {
                return true;
            }

            if (WillAtomCollideWithObject(armPosition, ArmBaseRadius))
            {
                return true;
            }

            return false;
        }
    }
}