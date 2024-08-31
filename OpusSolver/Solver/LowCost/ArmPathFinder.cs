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

        public ArmPathFinder(int armLength, IEnumerable<Vector2> trackCells)
        {
            m_armLength = armLength;
            m_trackCells = trackCells.ToList();
            m_trackCellsToIndexes = m_trackCells.Select((pos, index) => (pos, index)).ToDictionary(pair => pair.pos, pair => pair.index);
        }

        private record class ArmPosition(int TrackIndex, HexRotation Rotation);

        public IEnumerable<Instruction> FindPath(Transform2D startTransform, Transform2D endTransform)
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
            var path = FindShortestPath(startPosition, endPosition);

            return GetInstructionsForPath(startPosition, path);
        }

        /// <summary>
        /// Finds the shortest "path" to move/rotate an arm from one position to another.
        /// Uses the Uniform Cost Search algorithm.
        /// </summary>
        private IEnumerable<ArmPosition> FindShortestPath(ArmPosition startPosition, ArmPosition endPosition)
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
                    costs[neighbor] = newCost;
                    int heuristic = Math.Abs(endPosition.TrackIndex - neighbor.TrackIndex) + endPosition.Rotation.DistanceTo(neighbor.Rotation);
                    queue.Enqueue(neighbor, newCost + heuristic);
                    previousPosition[neighbor] = currentPosition;
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
