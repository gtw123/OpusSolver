using System;
using System.Linq;
using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public class ArmPathFinder
    {
        private readonly int m_armLength;
        private readonly List<Vector2> m_trackCells;
        private readonly Dictionary<Vector2, int> m_trackCellsToIndexes;
        private readonly GridState m_gridState;

        public ArmPathFinder(int armLength, IEnumerable<Vector2> trackCells, GridState gridState)
        {
            m_armLength = armLength;
            m_trackCells = trackCells.ToList();
            m_trackCellsToIndexes = m_trackCells.Select((pos, index) => (pos, index)).ToDictionary(pair => pair.pos, pair => pair.index);
            m_gridState = gridState;
        }

        private record class ArmPosition(int TrackIndex, HexRotation Rotation);

        public IEnumerable<Instruction> FindPath(Transform2D startTransform, Transform2D endTransform, Element? grabbedElement, HashSet<GlyphType> disallowedGlyphs)
        {
            if (!m_trackCellsToIndexes.TryGetValue(startTransform.Position, out var startTrackIndex))
            {
                throw new InvalidOperationException($"Starting position {startTransform.Position} is not on the track.");
            }

            if (!m_trackCellsToIndexes.TryGetValue(endTransform.Position, out var endTrackIndex))
            {
                throw new InvalidOperationException($"Ending position {endTransform.Position} is not on the track.");
            }

            var startPosition = new ArmPosition(startTrackIndex, startTransform.Rotation);
            var endPosition = new ArmPosition(endTrackIndex, endTransform.Rotation);
            var path = FindShortestPath(startPosition, endPosition, grabbedElement, disallowedGlyphs);

            return GetInstructionsForPath(startPosition, path);
        }

        private Vector2 ArmPositionToGrabberPosition(ArmPosition armPos)
        {
            var armGridPos = m_trackCells[armPos.TrackIndex];
            return armGridPos + new Vector2(m_armLength, 0).RotateBy(armPos.Rotation);
        }

        /// <summary>
        /// Finds the shortest "path" to move/rotate an arm from one position to another.
        /// Uses the Uniform Cost Search algorithm.
        /// </summary>
        private IEnumerable<ArmPosition> FindShortestPath(ArmPosition startPosition, ArmPosition endPosition, Element? grabbedElement, HashSet<GlyphType> disallowedGlyphs)
        {
            var previousPosition = new Dictionary<ArmPosition, ArmPosition> { { startPosition, null } };
            var costs = new Dictionary<ArmPosition, int> { { startPosition, 0 } };
            var queue = new PriorityQueue<ArmPosition, int>();
            queue.Enqueue(startPosition, 0);

            ArmPosition currentPosition = null;
            void AddNeighbor(ArmPosition neighbor)
            {
                int newCost = costs[currentPosition] + 1;
                if (!costs.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
                {
                    if (grabbedElement == null || IsMovementAllowed(currentPosition, neighbor, disallowedGlyphs))
                    {
                        costs[neighbor] = newCost;
                        int heuristic = Math.Abs(endPosition.TrackIndex - neighbor.TrackIndex) + endPosition.Rotation.DistanceTo(neighbor.Rotation);
                        queue.Enqueue(neighbor, newCost + heuristic);
                        previousPosition[neighbor] = currentPosition;
                    }
                }
            }

            while (queue.Count > 0)
            {
                currentPosition = queue.Dequeue();
                if (currentPosition == endPosition)
                {
                    break;
                }

                if (currentPosition.TrackIndex < m_trackCells.Count - 1)
                {
                    AddNeighbor(new ArmPosition(currentPosition.TrackIndex + 1, currentPosition.Rotation));
                }
                if (currentPosition.TrackIndex > 0)
                {
                    AddNeighbor(new ArmPosition(currentPosition.TrackIndex - 1, currentPosition.Rotation));
                }

                AddNeighbor(new ArmPosition(currentPosition.TrackIndex, currentPosition.Rotation.Rotate60Counterclockwise()));
                AddNeighbor(new ArmPosition(currentPosition.TrackIndex, currentPosition.Rotation.Rotate60Clockwise()));
            }

            if (currentPosition != endPosition)
            {
                throw new InvalidOperationException($"Cannot find path from {startPosition} to {endPosition}.");
            }

            var path = new List<ArmPosition>();
            while (currentPosition != startPosition)
            {
                path.Add(currentPosition);
                currentPosition = previousPosition[currentPosition];
            }

            return Enumerable.Reverse(path);
        }

        private bool IsMovementAllowed(ArmPosition currentPosition, ArmPosition targetPosition, HashSet<GlyphType> disallowedGlyphs)
        {
            var targetGripperPosition = ArmPositionToGrabberPosition(targetPosition);
            if (m_gridState.GetAtom(targetGripperPosition) != null)
            {
                return false;
            }

            // Disallow movement over certain glyphs (e.g. calcification)
            var glyph = m_gridState.GetGlyph(targetGripperPosition);
            if (glyph != null && disallowedGlyphs.Contains(glyph.Value))
            {
                return false;
            }

            if (m_armLength == 1)
            {
                // Nothing to do here: length-1 arms don't collide with anything while rotating
                return false;
            }

            var deltaRot = targetPosition.Rotation - currentPosition.Rotation;
            if (deltaRot != HexRotation.R0)
            {
                // These are the locations where atoms will cause a collision with the atom held by a
                // gripper when the arm rotates CCW from R300 to R0 or CW from R60 to R0. These are
                // offsets from the target position.
                Vector2[] offsets;
                if (m_armLength == 2)
                {
                    if (deltaRot == HexRotation.R60)
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
                    if (deltaRot == HexRotation.R60)
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
                    var checkPos = targetGripperPosition + offset.RotateBy(targetPosition.Rotation);
                    if (m_gridState.GetAtom(checkPos) != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static IEnumerable<Instruction> GetInstructionsForPath(ArmPosition startPosition, IEnumerable<ArmPosition> path)
        {
            var instructions = new List<Instruction>();

            var previousPos = startPosition;
            foreach (var pos in path)
            {
                int trackDelta = pos.TrackIndex - previousPos.TrackIndex;
                if (trackDelta != 0)
                {
                    instructions.Add(trackDelta > 0 ? Instruction.MovePositive : Instruction.MoveNegative);
                }
                else
                {
                    var rotDelta = pos.Rotation - previousPos.Rotation;
                    instructions.Add(rotDelta.IntValue <= 3 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise);
                }

                previousPos = pos;
            }

            return instructions;
        }
    }
}
