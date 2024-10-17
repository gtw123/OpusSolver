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

        private record class ArmState(int TrackIndex, HexRotation ArmRotation, Transform2D MoleculeTransform, bool IsHoldingMolecule);

        private record class SearchParams(
            Func<ArmState, bool> IsAtTarget,
            Func<ArmState, int> CalculateHeuristic,
            bool AllowPivot,
            bool AllowGrabDrop
        );

        public record class PathResult(bool Success, IEnumerable<Instruction> Instructions, Transform2D FinalArmTransform);

        public PathResult FindArmPath(Transform2D startTransform, Transform2D endTransform, AtomCollection moleculeToMove, ArmMovementOptions options)
        {
            if (!m_trackCellsToIndexes.TryGetValue(startTransform.Position, out var startTrackIndex))
            {
                throw new SolverException($"Starting position {startTransform.Position} is not on the track.");
            }

            if (!m_trackCellsToIndexes.TryGetValue(endTransform.Position, out var endTrackIndex))
            {
                throw new SolverException($"Ending position {endTransform.Position} is not on the track.");
            }

            var startState = new ArmState(startTrackIndex, startTransform.Rotation, moleculeToMove?.WorldTransform ?? new Transform2D(), moleculeToMove != null);

            bool IsAtTarget(ArmState state) => state.TrackIndex == endTrackIndex && state.ArmRotation == endTransform.Rotation && state.IsHoldingMolecule == (moleculeToMove != null);

            int CalculateTrackDistance(int index1, int index2)
            {
                int distance = Math.Abs(index1 - index2);
                if (m_isLoopingTrack)
                {
                    // On a looping track it may be shorter to go backwards and wraparound
                    distance = Math.Min(distance, m_trackCells.Count - distance);
                }
                return distance;
            }

            int CalculateDistanceHeuristic(ArmState state) => CalculateTrackDistance(endTrackIndex, state.TrackIndex) + endTransform.Rotation.DistanceTo(state.ArmRotation);

            var searchParams = new SearchParams(IsAtTarget, CalculateDistanceHeuristic, AllowPivot: false, AllowGrabDrop: false);
            var path = FindShortestPath(startState, moleculeToMove, searchParams, options);
            if (path == null)
            {
                return new PathResult(Success: false, null, new());
            }

            return new PathResult(Success: true, GetInstructionsForPath(startState, path), endTransform);
        }

        public PathResult FindMoleculePath(Transform2D startArmTransform, Transform2D endMoleculeTransform, AtomCollection moleculeToMove, bool isHoldingMolecule, ArmMovementOptions options)
        {
            if (moleculeToMove == null)
            {
                throw new ArgumentNullException(nameof(moleculeToMove));
            }

            if (!m_trackCellsToIndexes.TryGetValue(startArmTransform.Position, out var startTrackIndex))
            {
                throw new SolverException($"Starting position {startArmTransform.Position} is not on the track.");
            }

            var startState = new ArmState(startTrackIndex, startArmTransform.Rotation, moleculeToMove.WorldTransform, isHoldingMolecule);

            bool IsAtTarget(ArmState state)
            {
                if (!state.IsHoldingMolecule)
                {
                    return false;
                }

                if (moleculeToMove.Atoms.Count > 1)
                {
                    return state.MoleculeTransform == endMoleculeTransform;
                }
                else
                {
                    // Ignore rotation for monoatomic molecules since they're completely symmetrical
                    return state.MoleculeTransform.Position == endMoleculeTransform.Position;
                }
            }

            int CalculateDistanceHeuristic(ArmState state)
            {
                int result;
                if (moleculeToMove.Atoms.Count > 1)
                {
                    // Logically we'd include both the distance and rotation here, but empirical testing shows that
                    // using rotation only gives a shorter path (although it means we end up checking more states).
                    // TODO: Figure out why this is and if there's a better heuristic we could use. (It could be
                    // because the arm can only move along the track, not in arbitrary directions, but we don't
                    // know in advance which track location will end up being closest to the end molecule position.)
                    result = endMoleculeTransform.Rotation.DistanceTo(state.MoleculeTransform.Rotation);
                }
                else
                {
                    result = endMoleculeTransform.Position.DistanceBetween(state.MoleculeTransform.Position);
                }

                if (!state.IsHoldingMolecule)
                {
                    result++;
                }

                return result;
            }

            var searchParams = new SearchParams(IsAtTarget, CalculateDistanceHeuristic, AllowPivot: true, AllowGrabDrop: true);
            var path = FindShortestPath(startState, moleculeToMove, searchParams, options);
            if (path == null)
            {
                return new PathResult(Success: false, null, new());
            }

            return new PathResult(Success: true, GetInstructionsForPath(startState, path), GetArmTransform(path.LastOrDefault() ?? startState));
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
        private IEnumerable<ArmState> FindShortestPath(ArmState startState, AtomCollection moleculeToMove, SearchParams searchParams, ArmMovementOptions options)
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
                    if (IsMovementAllowed(currentState, neighbor, moleculeToMove, searchParams.IsAtTarget(neighbor), options))
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

                if (currentState.IsHoldingMolecule)
                {
                    var currentArmPos = m_trackCells[currentState.TrackIndex];
                    if (currentState.TrackIndex < m_trackCells.Count - 1)
                    {
                        var newArmPos = m_trackCells[currentState.TrackIndex + 1];
                        AddNeighbor(new ArmState(currentState.TrackIndex + 1, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos), currentState.IsHoldingMolecule));
                    }
                    else if (m_isLoopingTrack)
                    {
                        var newArmPos = m_trackCells[0];
                        AddNeighbor(new ArmState(0, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos), currentState.IsHoldingMolecule));
                    }

                    if (currentState.TrackIndex > 0)
                    {
                        var newArmPos = m_trackCells[currentState.TrackIndex - 1];
                        AddNeighbor(new ArmState(currentState.TrackIndex - 1, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos), currentState.IsHoldingMolecule));
                    }
                    else if (m_isLoopingTrack)
                    {
                        var newArmPos = m_trackCells[m_trackCells.Count - 1];
                        AddNeighbor(new ArmState(m_trackCells.Count - 1, currentState.ArmRotation, currentState.MoleculeTransform.OffsetBy(newArmPos - currentArmPos), currentState.IsHoldingMolecule));
                    }

                    // Rotate
                    if (moleculeToMove.Atoms.Count > 1)
                    {
                        AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Counterclockwise(), currentState.MoleculeTransform.RotateAbout(currentArmPos, HexRotation.R60), currentState.IsHoldingMolecule));
                        AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Clockwise(), currentState.MoleculeTransform.RotateAbout(currentArmPos, -HexRotation.R60), currentState.IsHoldingMolecule));
                    }
                    else
                    {
                        // Adjust only the position of the molecule transform, not the rotation too, because the molecule (if any) is symmetric
                        // and so we don't need to consider two different molecule rotations as different states.
                        var newTransform = new Transform2D(currentState.MoleculeTransform.Position.RotateAbout(currentArmPos, HexRotation.R60), currentState.MoleculeTransform.Rotation);
                        AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Counterclockwise(), newTransform, currentState.IsHoldingMolecule));

                        newTransform = new Transform2D(currentState.MoleculeTransform.Position.RotateAbout(currentArmPos, -HexRotation.R60), currentState.MoleculeTransform.Rotation);
                        AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Clockwise(), newTransform, currentState.IsHoldingMolecule));
                    }

                    // Pivot
                    if (searchParams.AllowPivot && moleculeToMove.Atoms.Count > 1)
                    {
                        var grabberPosition = GetGrabberPosition(currentState);
                        AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation, currentState.MoleculeTransform.RotateAbout(grabberPosition, HexRotation.R60), currentState.IsHoldingMolecule));
                        AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation, currentState.MoleculeTransform.RotateAbout(grabberPosition, -HexRotation.R60), currentState.IsHoldingMolecule));
                    }

                    // Drop the molecule
                    if (searchParams.AllowGrabDrop)
                    {
                        AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation, currentState.MoleculeTransform, IsHoldingMolecule: false));
                    }
                }
                else
                {
                    // No molecule is currently being held, so don't update any molecule transforms
                    if (currentState.TrackIndex < m_trackCells.Count - 1)
                    {
                        AddNeighbor(new ArmState(currentState.TrackIndex + 1, currentState.ArmRotation, currentState.MoleculeTransform, currentState.IsHoldingMolecule));
                    }
                    else if (m_isLoopingTrack)
                    {
                        AddNeighbor(new ArmState(0, currentState.ArmRotation, currentState.MoleculeTransform, currentState.IsHoldingMolecule));
                    }

                    if (currentState.TrackIndex > 0)
                    {
                        AddNeighbor(new ArmState(currentState.TrackIndex - 1, currentState.ArmRotation, currentState.MoleculeTransform, currentState.IsHoldingMolecule));
                    }
                    else if (m_isLoopingTrack)
                    {
                        AddNeighbor(new ArmState(m_trackCells.Count - 1, currentState.ArmRotation, currentState.MoleculeTransform, currentState.IsHoldingMolecule));
                    }

                    // Rotate
                    AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Counterclockwise(), currentState.MoleculeTransform, currentState.IsHoldingMolecule));
                    AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation.Rotate60Clockwise(), currentState.MoleculeTransform, currentState.IsHoldingMolecule));

                    // Grab
                    if (searchParams.AllowGrabDrop && moleculeToMove != null)
                    {
                        // Check if there's an atom of the molecule to pick up here
                        var grabberPosition = GetGrabberPosition(currentState);
                        if (moleculeToMove.GetAtomAtTransformedPosition(currentState.MoleculeTransform, grabberPosition) != null)
                        {
                            AddNeighbor(new ArmState(currentState.TrackIndex, currentState.ArmRotation, currentState.MoleculeTransform, IsHoldingMolecule: true));
                        }
                    }
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

        private bool IsMovementAllowed(ArmState currentState, ArmState targetState, AtomCollection moleculeToMove, bool isAtTargetState, ArmMovementOptions options)
        {
            if (currentState.TrackIndex == targetState.TrackIndex && currentState.MoleculeTransform == targetState.MoleculeTransform)
            {
                // If the arm and atoms aren't moving, there's nothing to check. This includes the case where we're picking up
                // or dropping atoms.
                return true;
            }

            // Check if the arm base will collide with a static atom while moving along the track
            var targetArmPos = m_trackCells[targetState.TrackIndex];
            if (m_gridState.GetAtom(targetArmPos) != null)
            {
                return false;
            }

            if (moleculeToMove == null)
            {
                return true;
            }

            if (!targetState.IsHoldingMolecule)
            {
                // Check if the arm base with collide with an atom of the molecule while it's dropped
                if (moleculeToMove.GetTransformedAtomPositions(targetState.MoleculeTransform).Any(p => p.position == targetArmPos))
                {
                    return false;
                }

                // There's nothing else to check if we're not currently holding the molecule
                return true;
            }

            var collidableAtomPositions = new HashSet<Vector2>(m_gridState.GetAllCollidableAtomPositions(moleculeToMove.GetTransformedAtomPositions(currentState.MoleculeTransform).Select(p => p.position)));

            foreach (var (atom, pos) in moleculeToMove.GetTransformedAtomPositions(targetState.MoleculeTransform))
            {
                if (collidableAtomPositions.Contains(pos))
                {
                    return false;
                }

                if (m_gridState.GetStaticArm(pos) != null)
                {
                    return false;
                }

                var glyph = m_gridState.GetGlyph(pos);
                if (glyph != null)
                {
                    bool isMovementAllowed = glyph.Type switch
                    {
                        GlyphType.Calcification => IsMovementAllowedOverCalcification(atom, pos, glyph, options),
                        GlyphType.Duplication => IsMovementAllowedOverDuplication(atom, pos, glyph, options),
                        GlyphType.Bonding => IsMovementAllowedOverBonder(targetState, moleculeToMove, atom, pos, glyph, options),
                        GlyphType.Unbonding => IsMovementAllowedOverUnbonder(targetState, moleculeToMove, atom, pos, glyph, isAtTargetState, options),
                        _ => true
                    };
                    if (!isMovementAllowed)
                    {
                        return false;
                    }
                }
            }

            var deltaRot = targetState.ArmRotation - currentState.ArmRotation;
            if (deltaRot != HexRotation.R0)
            {
                if (currentState.TrackIndex != targetState.TrackIndex)
                {
                    throw new SolverException("Cannot move and rotate an arm at the same time.");
                }

                var armPos = m_trackCells[currentState.TrackIndex];
                return !m_collisionDetector.WillAtomsCollideWhileRotating(moleculeToMove, currentState.MoleculeTransform, armPos, deltaRot);
            }

            deltaRot = targetState.MoleculeTransform.Rotation - currentState.MoleculeTransform.Rotation;
            if (deltaRot != HexRotation.R0)
            {
                if (currentState.TrackIndex != targetState.TrackIndex)
                {
                    throw new SolverException("Cannot move and pivot an arm at the same time.");
                }

                var armPos = m_trackCells[currentState.TrackIndex];
                var grabberPos = GetGrabberPosition(currentState);
                return !m_collisionDetector.WillAtomsCollideWhilePivoting(moleculeToMove, currentState.MoleculeTransform, armPos, grabberPos, deltaRot);
            }

            return true;
        }

        private bool IsMovementAllowedOverCalcification(Atom atom, Vector2 atomWorldPos, Glyph glyph, ArmMovementOptions options)
        {
            return options.AllowCalcification || !PeriodicTable.Cardinals.Contains(atom.Element);
        }

        private bool IsMovementAllowedOverDuplication(Atom atom, Vector2 atomWorldPos, Glyph glyph, ArmMovementOptions options)
        {
            if (!options.AllowDuplication && atom.Element == Element.Salt)
            {
                var glyphCells = glyph.GetWorldCells();
                if (atomWorldPos == glyphCells[1])
                {
                    var otherAtom = m_gridState.GetAtom(glyphCells[0]);
                    if (otherAtom != Element.Salt)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsMovementAllowedOverBonder(ArmState targetState, AtomCollection moleculeToMove, Atom atom, Vector2 atomWorldPos, Glyph glyph, ArmMovementOptions options)
        {
            // Check what's on top of the other cell of the bonder (if anything)
            var bonderCells = glyph.GetWorldCells();
            var otherPos = bonderCells[0] != atomWorldPos ? bonderCells[0] : bonderCells[1];
            if (m_gridState.GetAtom(otherPos) != null)
            {
                // A static atom is on the other cell of the bonder
                if (!options.AllowExternalBonds)
                {
                    return false;
                }
            }
            else
            {
                var moleculeInverse = targetState.MoleculeTransform.Inverse();
                var otherAtomLocalPos = moleculeInverse.Apply(otherPos);
                var otherAtom = moleculeToMove.GetAtom(otherAtomLocalPos);
                if (otherAtom != null)
                {
                    // Another atom within the molecule is on the other cell of the bonder.
                    // Check if these atoms are *not* already bonded.
                    var currentAtomLocalPos = moleculeInverse.Apply(atomWorldPos);
                    var bondDir = (otherAtomLocalPos - currentAtomLocalPos).ToRotation() ?? throw new InvalidOperationException($"Expected bonder cells {atomWorldPos} and {otherPos} to be adjacent.");
                    if (atom.Bonds[bondDir] == BondType.None)
                    {
                        // Disallow this bond unless the target molecule has it
                        if (moleculeToMove.TargetMolecule == null || moleculeToMove.TargetMolecule.GetAtom(currentAtomLocalPos).Bonds[bondDir] == BondType.None)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool IsMovementAllowedOverUnbonder(ArmState targetState, AtomCollection moleculeToMove, Atom atom, Vector2 atomWorldPos, Glyph glyph, bool isAtTargetState, ArmMovementOptions options)
        {
            // Check what's on top of the other cell of the unbonder (if anything)
            var unbonderCells = glyph.GetWorldCells();
            var otherPos = unbonderCells[0] != atomWorldPos ? unbonderCells[0] : unbonderCells[1];

            var moleculeInverse = targetState.MoleculeTransform.Inverse();
            var otherAtomLocalPos = moleculeInverse.Apply(otherPos);
            var otherAtom = moleculeToMove.GetAtom(otherAtomLocalPos);
            if (otherAtom != null)
            {
                // Another atom within the molecule is on the other cell of the bonder.
                // Check if these atoms are already bonded.
                var currentAtomLocalPos = moleculeInverse.Apply(atomWorldPos);
                var bondDir = (otherAtomLocalPos - currentAtomLocalPos).ToRotation() ?? throw new InvalidOperationException($"Expected unbonder cells {atomWorldPos} and {otherPos} to be adjacent.");
                if (atom.Bonds[bondDir] != BondType.None)
                {
                    if (!options.AllowUnbonding)
                    {
                        return false;
                    }

                    // Disallow removing this bond unless the target molecule doesn't have it, in which case we can remove it too.
                    // But also disallow removing the bond if the molecule isn't yet the target state - otherwise we'll need to track
                    // both the removed atoms and the remaining atoms separately, and this class isn't sophisticated enough to do that yet.
                    if (moleculeToMove.TargetMolecule == null || moleculeToMove.TargetMolecule.GetAtom(currentAtomLocalPos).Bonds[bondDir] != BondType.None || !isAtTargetState)
                    {
                        return false;
                    }
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
                else if (state.IsHoldingMolecule != previousState.IsHoldingMolecule)
                {
                    instructions.Add(state.IsHoldingMolecule ? Instruction.Grab : Instruction.Drop);
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
