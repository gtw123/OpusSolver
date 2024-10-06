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

        private record class ArmState(int TrackIndex, HexRotation ArmRotation, Transform2D MoleculeTransform);

        private record class SearchParams(
            Func<ArmState, bool> IsAtTarget,
            Func<ArmState, int> CalculateHeuristic,
            bool AllowPivot,
            bool AllowCalcification
        );

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

            var startState = new ArmState(startTrackIndex, startTransform.Rotation, grabbedAtoms?.WorldTransform ?? new Transform2D());

            bool IsAtTarget(ArmState state) => state.TrackIndex == endTrackIndex && state.ArmRotation == endTransform.Rotation;
            int CalculateDistanceHeuristic(ArmState state) => Math.Abs(endTrackIndex - state.TrackIndex) + endTransform.Rotation.DistanceTo(state.ArmRotation);

            var searchParams = new SearchParams(IsAtTarget, CalculateDistanceHeuristic, false, allowCalcification);
            var path = FindShortestPath(startState, grabbedAtoms, searchParams);
            if (path == null)
            {
                throw new SolverException($"Cannot find path from {startTransform} to {endTransform}.");
            }

            return GetInstructionsForPath(startState, path);
        }

        public (IEnumerable<Instruction> instructions, Transform2D finalArmTransform)
            FindMoleculePath(Transform2D startArmTransform, Transform2D endMoleculeTransform, AtomCollection grabbedAtoms, bool allowCalcification)
        {
            if (grabbedAtoms == null)
            {
                throw new ArgumentNullException(nameof(grabbedAtoms));
            }

            if (!m_trackCellsToIndexes.TryGetValue(startArmTransform.Position, out var startTrackIndex))
            {
                throw new SolverException($"Starting position {startArmTransform.Position} is not on the track.");
            }

            var startState = new ArmState(startTrackIndex, startArmTransform.Rotation, grabbedAtoms.WorldTransform);

            bool IsAtTarget(ArmState state) => state.MoleculeTransform == endMoleculeTransform;
            
            int CalculateDistanceHeuristic(ArmState state)
            {
                // Logically we'd include both the distance and rotation here, but empirical testing shows that
                // using rotation only gives a shorter path (although it means we end up checking more states).
                // TODO: Figure out why this is and if there's a better heuristic we could use. (It could be
                // because the arm can only move along the track, not in arbitrary directions, but we don't
                // know in advance which track location will end up being closest to the end molecule position.)
                return /*endMoleculeTransform.Position.DistanceBetween(neighbor.MoleculeTransform.Position) + */ endMoleculeTransform.Rotation.DistanceTo(state.MoleculeTransform.Rotation);
            }

            var searchParams = new SearchParams(IsAtTarget, CalculateDistanceHeuristic, true, allowCalcification);
            var path = FindShortestPath(startState, grabbedAtoms, searchParams);
            if (path == null)
            {
                throw new SolverException($"Cannot find path from {startArmTransform} to {endMoleculeTransform}.");
            }

            return (GetInstructionsForPath(startState, path), GetArmTransform(path.Last()));
        }

        private Vector2 GetGrabberPosition(ArmState armState)
        {
            return m_trackCells[armState.TrackIndex] + new Vector2(m_armLength, 0).RotateBy(armState.ArmRotation);
        }

        private Transform2D GetArmTransform(ArmState armState)
        {
            return new Transform2D(m_trackCells[armState.TrackIndex], armState.ArmRotation);
        }

        /// <summary>
        /// Finds the shortest "path" to move/rotate an arm from one position to another.
        /// Uses the Uniform Cost Search algorithm.
        /// </summary>
        private IEnumerable<ArmState> FindShortestPath(ArmState startState, AtomCollection grabbedAtoms, SearchParams searchParams)
        {
            var previousState = new Dictionary<ArmState, ArmState> { { startState, null } };
            var costs = new Dictionary<ArmState, int> { { startState, 0 } };
            var queue = new PriorityQueue<ArmState, int>();
            queue.Enqueue(startState, 0);

            ArmState currentState = null;
            void AddNeighbor(ArmState neighbor)
            {
                int newCost = costs[currentState] + 1;
                if (!costs.TryGetValue(neighbor, out int existingCost) || newCost < existingCost)
                {
                    if (grabbedAtoms == null || IsMovementAllowed(currentState, neighbor, grabbedAtoms, searchParams.AllowCalcification))
                    {
                        costs[neighbor] = newCost;
                        queue.Enqueue(neighbor, newCost + searchParams.CalculateHeuristic(neighbor));
                        previousState[neighbor] = currentState;
                    }
                }
            }

            while (queue.Count > 0)
            {
                currentState = queue.Dequeue();
                if (searchParams.IsAtTarget(currentState))
                {
                    break;
                }

                var currentArmPos = m_trackCells[currentState.TrackIndex];
                if (currentState.TrackIndex < m_trackCells.Count - 1)
                {
                    var newArmPos = m_trackCells[currentState.TrackIndex + 1];
                    AddNeighbor(new ArmState(currentState.TrackIndex + 1, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos)));
                }
                else if (m_isLoopingTrack)
                {
                    var newArmPos = m_trackCells[0];
                    AddNeighbor(new ArmState(0, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos)));
                }

                if (currentState.TrackIndex > 0)
                {
                    var newArmPos = m_trackCells[currentState.TrackIndex - 1];
                    AddNeighbor(new ArmState(currentState.TrackIndex - 1, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos)));
                }
                else if (m_isLoopingTrack)
                {
                    var newArmPos = m_trackCells[m_trackCells.Count - 1];
                    AddNeighbor(new ArmState(m_trackCells.Count - 1, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos)));
                }

                // Rotate
                AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Counterclockwise(), currentState.MoleculeTransform.RotateAbout(currentArmPos, HexRotation.R60)));
                AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Clockwise(), currentState.MoleculeTransform.RotateAbout(currentArmPos, -HexRotation.R60)));

                if (searchParams.AllowPivot && grabbedAtoms != null && grabbedAtoms.Atoms.Count > 1)
                {
                    // Pivot
                    var grabberPosition = GetGrabberPosition(currentState);
                    AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation, currentState.MoleculeTransform.RotateAbout(grabberPosition, HexRotation.R60)));
                    AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation, currentState.MoleculeTransform.RotateAbout(grabberPosition, -HexRotation.R60)));
                }
            }

            if (!searchParams.IsAtTarget(currentState))
            {
                return null;
            }

            var path = new List<ArmState>();
            while (currentState != startState)
            {
                path.Add(currentState);
                currentState = previousState[currentState];
            }

            return Enumerable.Reverse(path);
        }

        private bool IsMovementAllowed(ArmState currentState, ArmState targetState, AtomCollection grabbedAtoms, bool allowCalcification)
        {
            foreach (var (atom, pos) in grabbedAtoms.GetTransformedAtomPositions(targetState.MoleculeTransform))
            {
                if (m_gridState.GetAtom(pos) != null)
                {
                    return false;
                }

                if (!allowCalcification && PeriodicTable.Cardinals.Contains(atom.Element) && m_gridState.GetGlyph(pos)?.Type == GlyphType.Calcification)
                {
                    return false;
                }
            }

            var deltaRot = targetState.ArmRotation - currentState.ArmRotation;
            if (deltaRot != HexRotation.R0)
            {
                if (currentState.TrackIndex != targetState.TrackIndex)
                {
                    throw new SolverException("Cannot move and rotate atoms at the same time.");
                }

                var armPos = m_trackCells[currentState.TrackIndex];
                return !m_collisionDetector.WillAtomsCollideWhileRotating(grabbedAtoms, currentState.MoleculeTransform, armPos, deltaRot);
            }

            deltaRot = targetState.MoleculeTransform.Rotation - currentState.MoleculeTransform.Rotation;
            if (deltaRot != HexRotation.R0)
            {
                if (currentState.TrackIndex != targetState.TrackIndex)
                {
                    throw new SolverException("Cannot move and pivot atoms at the same time.");
                }

                var armPos = m_trackCells[currentState.TrackIndex];
                var grabberPos = GetGrabberPosition(currentState);
                return !m_collisionDetector.WillAtomsCollideWhilePivoting(grabbedAtoms, currentState.MoleculeTransform, armPos, grabberPos, deltaRot);
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
                else if (state.ArmRotation != previousState.ArmRotation)
                {
                    var deltaRots = previousState.ArmRotation.CalculateDeltaRotationsTo(state.ArmRotation);
                    instructions.AddRange(deltaRots.ToRotationInstructions());
                }
                else if (state.MoleculeTransform.Rotation != previousState.MoleculeTransform.Rotation)
                {
                    var deltaRots = previousState.MoleculeTransform.Rotation.CalculateDeltaRotationsTo(state.MoleculeTransform.Rotation);
                    instructions.AddRange(deltaRots.ToPivotInstructions());
                }
                else
                {
                    throw new InvalidOperationException($"State {state} is identical to previous state {previousState}.");
                }

                previousState = state;
            }

            return instructions;
        }
    }
}
