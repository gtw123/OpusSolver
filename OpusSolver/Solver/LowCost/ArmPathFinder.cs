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

        private record class ArmState(int TrackIndex, HexRotation Rotation);

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

            var startState = new ArmState(startTrackIndex, startTransform.Rotation);
            var endState = new ArmState(endTrackIndex, endTransform.Rotation);
            var path = FindShortestPath(startState, endState, grabbedAtoms, allowCalcification);

            return GetInstructionsForPath(startState, path);
        }

        private Transform2D GetGrabberTransform(ArmState armState)
        {
            var armGridPos = m_trackCells[armState.TrackIndex];
            return new Transform2D(armGridPos + new Vector2(m_armLength, 0).RotateBy(armState.Rotation), armState.Rotation);
        }

        /// <summary>
        /// Finds the shortest "path" to move/rotate an arm from one position to another.
        /// Uses the Uniform Cost Search algorithm.
        /// </summary>
        private IEnumerable<ArmState> FindShortestPath(ArmState startState, ArmState endState, AtomCollection grabbedAtoms, bool allowCalcification)
        {
            var previousState = new Dictionary<ArmState, ArmState> { { startState, null } };
            var costs = new Dictionary<ArmState, int> { { startState, 0 } };
            var queue = new PriorityQueue<ArmState, int>();
            queue.Enqueue(startState, 0);

            var startGrabberTransform = GetGrabberTransform(startState);
            var grabberToAtomsTransform = startGrabberTransform.Inverse().Apply(grabbedAtoms?.WorldTransform ?? new Transform2D());

            ArmState currentState = null;
            void AddNeighbor(ArmState neighbor)
            {
                int newCost = costs[currentState] + 1;
                if (!costs.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
                {
                    if (grabbedAtoms == null || IsMovementAllowed(currentState, neighbor, grabbedAtoms, grabberToAtomsTransform, allowCalcification))
                    {
                        costs[neighbor] = newCost;
                        int heuristic = Math.Abs(endState.TrackIndex - neighbor.TrackIndex) + endState.Rotation.DistanceTo(neighbor.Rotation);
                        queue.Enqueue(neighbor, newCost + heuristic);
                        previousState[neighbor] = currentState;
                    }
                }
            }

            while (queue.Count > 0)
            {
                currentState = queue.Dequeue();
                if (currentState == endState)
                {
                    break;
                }

                if (currentState.TrackIndex < m_trackCells.Count - 1)
                {
                    AddNeighbor(new ArmState(currentState.TrackIndex + 1, currentState.Rotation));
                }
                else if (m_isLoopingTrack)
                {
                    AddNeighbor(new ArmState(0, currentState.Rotation));
                }

                if (currentState.TrackIndex > 0)
                {
                    AddNeighbor(new ArmState(currentState.TrackIndex - 1, currentState.Rotation));
                }
                else if (m_isLoopingTrack)
                {
                    AddNeighbor(new ArmState(m_trackCells.Count - 1, currentState.Rotation));
                }

                AddNeighbor(new ArmState(currentState.TrackIndex, currentState.Rotation.Rotate60Counterclockwise()));
                AddNeighbor(new ArmState(currentState.TrackIndex, currentState.Rotation.Rotate60Clockwise()));
            }

            if (currentState != endState)
            {
                throw new SolverException($"Cannot find path from {startState} to {endState}.");
            }

            var path = new List<ArmState>();
            while (currentState != startState)
            {
                path.Add(currentState);
                currentState = previousState[currentState];
            }

            return Enumerable.Reverse(path);
        }

        private bool IsMovementAllowed(ArmState currentState, ArmState targetState, AtomCollection grabbedAtoms,
            Transform2D grabberToAtomsTransform, bool allowCalcification)
        {
            var targetAtomsTransform = GetGrabberTransform(targetState).Apply(grabberToAtomsTransform);
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

            var deltaRot = targetState.Rotation - currentState.Rotation;
            if (deltaRot != HexRotation.R0)
            {
                if (currentState.TrackIndex != targetState.TrackIndex)
                {
                    throw new SolverException("Cannot move and rotate atoms at the same time.");
                }

                var currentAtomsTransform = GetGrabberTransform(currentState).Apply(grabberToAtomsTransform);
                var armPos = m_trackCells[currentState.TrackIndex];
                if (m_collisionDetector.WillAtomsCollideWhileRotating(grabbedAtoms, currentAtomsTransform, armPos, deltaRot))
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<Instruction> GetInstructionsForPath(ArmState startState, IEnumerable<ArmState> path)
        {
            var instructions = new List<Instruction>();

            var previousState = startState;
            foreach (var state in path)
            {
                int trackDelta = state.TrackIndex - previousState.TrackIndex;
                if (trackDelta != 0)
                {
                    if (m_isLoopingTrack)
                    {
                        if (state.TrackIndex == 0 && previousState.TrackIndex == m_trackCells.Count - 1)
                        {
                            trackDelta = 1;
                        }
                        else if (state.TrackIndex == m_trackCells.Count - 1 && previousState.TrackIndex == 0)
                        {
                            trackDelta = -1;
                        }
                    }

                    instructions.Add(trackDelta > 0 ? Instruction.MovePositive : Instruction.MoveNegative);
                }
                else
                {
                    var deltaRots = previousState.Rotation.CalculateDeltaRotationsTo(state.Rotation);
                    instructions.AddRange(deltaRots.ToRotationInstructions());
                }

                previousState = state;
            }

            return instructions;
        }
    }
}
