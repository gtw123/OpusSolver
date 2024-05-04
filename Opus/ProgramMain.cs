using Opus.Solution.Solver;
using System;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]

namespace Opus
{
    static class ProgramMain
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramMain));

        static void Main()
        {
            sm_log.Info("Starting up");

            try
            {
                var puzzle = new Puzzle(null, null, null, null);
                var solver = new PuzzleSolver(puzzle);
                var solution = solver.Solve();
                //new SolutionRenderer(solution, screen).Render();
            }
            catch (SolverException e)
            {
                sm_log.Error("Unable to solve this puzzle: " + e.Message, e);
            }
            catch (Exception e)
            {
                sm_log.Error("Internal error: " + e.ToString(), e);
            }
        }
    }
}
