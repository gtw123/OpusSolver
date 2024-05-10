using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver
{
    /// <summary>
    /// Optimizes the cost of a solution by removing unused arms and tracks.
    /// </summary>
    public class CostOptimizer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramBuilder));

        private PuzzleSolution m_solution;

        public CostOptimizer(PuzzleSolution solution)
        {
            m_solution = solution;
        }

        public void Optimize()
        {
            RemoveUnusedArms();
            RemoveUnusedTracks();
        }

        private void RemoveUnusedArms()
        {
            RemoveArms(m_solution.GetObjects<Arm>().Except(m_solution.Program.Instructions.Keys));
            RemoveArms(m_solution.Program.Instructions
                .Where(pair => !pair.Value.Any(instruction => instruction == Instruction.Grab))
                .Select(pair => pair.Key));

            void RemoveArms(IEnumerable<Arm> arms)
            {
                foreach (var arm in arms.ToList().Where(arm => arm.Type != MechanismType.VanBerlo))
                {
                    sm_log.Debug(Invariant($"Removing unused arm {arm.UniqueID}"));
                    m_solution.Program.Instructions.Remove(arm);
                    m_solution.Objects.Remove(arm);
                }
            }
        }

        private void RemoveUnusedTracks()
        {
            foreach (var track in m_solution.GetObjects<Track>().ToList())
            {
                var cells = track.GetAllPathCells().ToList();
                var usedCells = new SortedSet<int>();
                foreach (var arm in m_solution.GetObjects<Arm>())
                {
                    usedCells.UnionWith(GetTrackCellsUsedByArm(cells, arm));
                }

                if (!usedCells.Any())
                {
                    m_solution.Objects.Remove(track);
                }
                else
                {
                    track.TrimPath(usedCells.First(), usedCells.Last());
                }
            }
        }

        private IEnumerable<int> GetTrackCellsUsedByArm(List<Vector2> trackCells, Arm arm)
        {
            int startIndex = trackCells.IndexOf(arm.GetWorldPosition());
            if (startIndex < 0)
            {
                return new int[0];
            }

            int minIndex = startIndex;
            int maxIndex = startIndex;
            int index = startIndex;

            foreach (var instruction in m_solution.Program.GetArmInstructions(arm))
            {
                switch (instruction)
                {
                    case Instruction.MovePositive:
                        if (index < trackCells.Count - 1)
                        {
                            index++;
                        }
                        break;
                    case Instruction.MoveNegative:
                        if (index > 0)
                        {
                            index--;
                        }
                        break;
                    case Instruction.Reset:
                        index = startIndex;
                        break;
                        // We ignore Instruction.Repeat because we assume all repeats end with a reset

                }

                minIndex = Math.Min(minIndex, index);
                maxIndex = Math.Max(maxIndex, index);
            }

            if (maxIndex > minIndex)
            {
                return Enumerable.Range(minIndex, maxIndex - minIndex + 1);
            }

            // Arm didn't actually move on the track, so don't count it
            return new int[0];
        }
    }
}