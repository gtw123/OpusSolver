using System;

namespace OpusSolver.Solver.LowCost
{
    public class RotationalCollisionDetector
    {
        private readonly GridState m_gridState;

        public RotationalCollisionDetector(GridState gridState)
        {
            m_gridState = gridState;
        }

        public bool WillAtomsCollide(int armLength, Transform2D targetGrabberTransform, HexRotation rotation)
        {
            if (rotation != HexRotation.R60 && rotation != HexRotation.R300)
            {
                throw new ArgumentException($"{nameof(WillAtomsCollide)} only supports rotations by 60 degrees but was given {rotation}.");
            }

            // TODO: Check all atoms

            // These are the locations where atoms will cause a collision with the atom held by a
            // gripper when the arm rotates CCW from R300 to R0 or CW from R60 to R0. These are
            // offsets from the target position.

            Vector2[] offsets;
            if (armLength == 1)
            {
                return false;
            }
            else if (armLength == 2)
            {
                if (rotation == HexRotation.R60)
                {
                    offsets = [new Vector2(0, -1), new Vector2(1, -1), new Vector2(1, -2)];
                }
                else
                {
                    offsets = [new Vector2(0, 1), new Vector2(-1, 1), new Vector2(-1, 2)];
                }
            }
            else // length 3
            {
                if (rotation == HexRotation.R60)
                {
                    offsets = [new Vector2(0, -1), new Vector2(0, -2), new Vector2(1, -1), new Vector2(1, -2), new Vector2(1, -3)];
                }
                else
                {
                    offsets = [new Vector2(-1, 1), new Vector2(-2, 2), new Vector2(0, 1), new Vector2(-1, 2), new Vector2(-2, 3)];
                }
            }

            foreach (var offset in offsets)
            {
                var checkPos = targetGrabberTransform.Position + offset.RotateBy(targetGrabberTransform.Rotation);
                if (m_gridState.GetAtom(checkPos) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
