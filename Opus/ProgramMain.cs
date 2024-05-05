using Opus.IO;
using Opus.Solution.Solver;
using System;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]

namespace Opus
{
    public static class ProgramMain
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramMain));

        public static int Main(string[] args)
        {
            sm_log.Info("Starting up");

            if (args.Length < 1)
            {
                sm_log.Error("Usage: Opus.exe <puzzle file>");
                return 1;
            }

            string puzzleFile = args[0];

            try
            {
                sm_log.Info($"Loading puzzle file \"{puzzleFile}\"");
                var puzzle = PuzzleReader.ReadPuzzle(puzzleFile);

                Console.WriteLine("Reagents:");
                foreach (var m in puzzle.Reagents)
                {
                    Console.WriteLine(m);
                }

                Console.WriteLine("Products:");
                foreach (var m in puzzle.Products)
                {
                    Console.WriteLine(m);
                }

                var solver = new PuzzleSolver(puzzle);
                var solution = solver.Solve();
                //new SolutionRenderer(solution, screen).Render();
            }
            catch (ParseException e)
            {
                sm_log.Error("Error loading puzzle file", e);
                return 1;
            }
            catch (SolverException e)
            {
                sm_log.Error("Unable to solve this puzzle", e);
                return 1;
            }
            catch (Exception e)
            {
                sm_log.Error("Internal error", e);
                return 1;
            }

            return 0;
        }
    }
}
