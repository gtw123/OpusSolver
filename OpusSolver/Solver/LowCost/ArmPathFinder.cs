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
        private readonly bool m_isLoopingTrack;
        private readonly GridState m_gridState;
        private readonly RotationalCollisionDetector m_collisionDetector;

        public ArmPathFinder(int armLength, Track track, GridState gridState, RotationalCollisionDetector collisionDetector)
        {
            m_armLength = armLength;
            m_trackCells = track.GetAllPathCells().ToList();
            m_trackCellsToIndexes = m_trackCells.Select((pos, index) => (pos, index)).ToDictionary(pair => pair.pos, pair => pair.index);
            m_isLoopingTrack = track.IsLooping;
            m_gridState = gridState;
            m_collisionDetector = collisionDetector;
        }

        private record class ArmPosition(int TrackIndex, HexRotation Rotation);

        public IEnumerable<Instruction> FindPath(Transform2D startTransform, Transform2D endTransform, AtomCollection grabbedAtoms, bool allowCalcification)
        {
            if (!m_trackCellsToIndexes.TryGetValue(startTransform.Position, out var startTrackIndex))
            {
                throw new SolverException($"Starting position {startTransform.Position} is not on the track.");
            }

            if (!m_trackCellsToIndexes.TryGetValue(endTransform.Position, out var endTrackIndex))
            {
                throw new SolverException($"Ending position {endTransform.Position} is not on the track.");
            }

            var startPosition = new ArmPosition(startTrackIndex, startTransform.Rotation);
            var endPosition = new ArmPosition(endTrackIndex, endTransform.Rotation);
            var path = FindShortestPath(startPosition, endPosition, grabbedAtoms, allowCalcification);

            return GetInstructionsForPath(startPosition, path);
        }

        private Transform2D ArmPositionToGrabberTransform(ArmPosition armPos)
        {
            var armGridPos = m_trackCells[armPos.TrackIndex];
            return new Transform2D(armGridPos + new Vector2(m_armLength, 0).RotateBy(armPos.Rotation), armPos.Rotation);
        }

        /// <summary>
        /// Finds the shortest "path" to move/rotate an arm from one position to another.
        /// Uses the Uniform Cost Search algorithm.
        /// </summary>
        private IEnumerable<ArmPosition> FindShortestPath(ArmPosition startPosition, ArmPosition endPosition, AtomCollection grabbedAtoms, bool allowCalcification)
        {
            var previousPosition = new Dictionary<ArmPosition, ArmPosition> { { startPosition, null } };
            var costs = new Dictionary<ArmPosition, int> { { startPosition, 0 } };
            var queue = new PriorityQueue<ArmPosition, int>();
            queue.Enqueue(startPosition, 0);

            var startGrabberTransform = ArmPositionToGrabberTransform(startPosition);
            var grabberToAtomsTransform = startGrabberTransform.Inverse().Apply(grabbedAtoms?.WorldTransform ?? new Transform2D());

            ArmPosition currentPosition = null;
            void AddNeighbor(ArmPosition neighbor)
            {
                int newCost = costs[currentPosition] + 1;
                if (!costs.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
                {
                    if (grabbedAtoms == null || IsMovementAllowed(currentPosition, neighbor, grabbedAtoms, grabberToAtomsTransform, allowCalcification))
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
                else if (m_isLoopingTrack)
                {
                    AddNeighbor(new ArmPosition(0, currentPosition.Rotation));
                }

                if (currentPosition.TrackIndex > 0)
                {
                    AddNeighbor(new ArmPosition(currentPosition.TrackIndex - 1, currentPosition.Rotation));
                }
                else if (m_isLoopingTrack)
                {
                    AddNeighbor(new ArmPosition(m_trackCells.Count - 1, currentPosition.Rotation));
                }

                AddNeighbor(new ArmPosition(currentPosition.TrackIndex, currentPosition.Rotation.Rotate60Counterclockwise()));
                AddNeighbor(new ArmPosition(currentPosition.TrackIndex, currentPosition.Rotation.Rotate60Clockwise()));
            }

            if (currentPosition != endPosition)
            {
                throw new SolverException($"Cannot find path from {startPosition} to {endPosition}.");
            }

            var path = new List<ArmPosition>();
            while (currentPosition != startPosition)
            {
                path.Add(currentPosition);
                currentPosition = previousPosition[currentPosition];
            }

            return Enumerable.Reverse(path);
        }

        private bool IsMovementAllowed(ArmPosition currentPosition, ArmPosition targetPosition, AtomCollection grabbedAtoms,
            Transform2D grabberToAtomsTransform, bool allowCalcification)
        {
            var targetAtomsTransform = ArmPositionToGrabberTransform(targetPosition).Apply(grabberToAtomsTransform);
            foreach (var (atom, pos) in grabbedAtoms.GetTransformedAtomPositions(targetAtomsTransform))
            {
                if (m_gridState.GetAtom(pos) != null)
                {
                    return false;
                }

                if (!allowCalcification && PeriodicTable.Cardinals.Contains(atom.Element) && m_gridState.GetGlyph(pos) == GlyphType.Calcification)
                {
                    return false;
                }
            }

            var deltaRot = targetPosition.Rotation - currentPosition.Rotation;
            if (deltaRot != HexRotation.R0)
            {
                if (currentPosition.TrackIndex != targetPosition.TrackIndex)
                {
                    throw new SolverException("Cannot move and rotate atoms at the same time.");
                }

                var currentAtomsTransform = ArmPositionToGrabberTransform(currentPosition).Apply(grabberToAtomsTransform);
                var armPos = m_trackCells[currentPosition.TrackIndex];
                if (m_collisionDetector.WillAtomsCollideWhileRotating(grabbedAtoms, currentAtomsTransform, armPos, deltaRot))
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<Instruction> GetInstructionsForPath(ArmPosition startPosition, IEnumerable<ArmPosition> path)
        {
            var instructions = new List<Instruction>();

            var previousPos = startPosition;
            foreach (var pos in path)
            {
                int trackDelta = pos.TrackIndex - previousPos.TrackIndex;
                if (trackDelta != 0)
                {
                    if (m_isLoopingTrack)
                    {
                        if (pos.TrackIndex == 0 && previousPos.TrackIndex == m_trackCells.Count - 1)
                        {
                            trackDelta = 1;
                        }
                        else if (pos.TrackIndex == m_trackCells.Count - 1 && previousPos.TrackIndex == 0)
                        {
                            trackDelta = -1;
                        }
                    }

                    instructions.Add(trackDelta > 0 ? Instruction.MovePositive : Instruction.MoveNegative);
                }
                else
                {
                    var deltaRots = previousPos.Rotation.CalculateDeltaRotationsTo(pos.Rotation);
                    instructions.AddRange(deltaRots.Select(rot => rot == HexRotation.R60 ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise));
                }

                previousPos = pos;
            }

            return instructions;
        }
    }
}
