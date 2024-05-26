using OpusSolver.IO;
using OpusSolver.Solver;
using OpusSolver.Verifier;
using System;
using System.IO;

namespace OpusSolver
{
    public class Runner : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Runner));

        private CommandLineArguments m_args;
        private StreamWriter m_reportWriter;

        public Runner(CommandLineArguments args)
        {
            m_args = args;
            if (m_args.ReportFile != null)
            {
                m_reportWriter = new StreamWriter(m_args.ReportFile);
                m_reportWriter.WriteLine("Name,File,Cost,Cycles,Area,Instructions");
            }
        }

        public void Dispose()
        {
            m_reportWriter?.Dispose();
            m_reportWriter = null;
        }

        public void Run()
        {
            int totalPuzzlesSolved = 0;

            sm_log.Info($"Generating solutions to \"{m_args.OutputDir}\"");

            foreach (var puzzleFile in m_args.PuzzleFiles)
            {
                string solutionFile = Path.Combine(m_args.OutputDir, Path.GetFileNameWithoutExtension(puzzleFile) + ".solution");
                if (SolvePuzzle(puzzleFile, solutionFile))
                {
                    totalPuzzlesSolved++;
                }

                Console.Write(".");
            }

            Console.WriteLine();

            string verifyMessage = m_args.SkipVerification ? "" : "and verified ";
            sm_log.Info($"Successfully generated {verifyMessage}solutions for {totalPuzzlesSolved}/{m_args.PuzzleFiles.Count} puzzles.");

            if (m_reportWriter != null)
            {
                sm_log.Info($"Report saved to \"{m_args.ReportFile}\"");
                m_reportWriter.WriteLine($"Successful solutions: {totalPuzzlesSolved}/{m_args.PuzzleFiles.Count}");
            }
        }

        private bool SolvePuzzle(string puzzleFile, string solutionFile)
        {
            string puzzleName = null;

            try
            {
                sm_log.Debug($"Loading puzzle file \"{puzzleFile}\"");
                var puzzle = PuzzleReader.ReadPuzzle(puzzleFile);

                puzzleName = puzzle.Name;
                sm_log.Debug($"Puzzle name: {puzzle.Name}");

                var solver = new PuzzleSolver(puzzle);
                var solution = solver.Solve();

                sm_log.Debug($"Writing solution to \"{solutionFile}\"");
                // It might be more efficient to write the solution to a byte array first and pass that to the verifier,
                // rather than writing it to disk twice. But it's also convenient having a copy on disk even if the
                // verification fails so that we can debug it in the game.
                SolutionWriter.WriteSolution(solution, solutionFile);

                if (!m_args.SkipVerification)
                {
                    sm_log.Debug("Verifying solution");
                    using var verifier = new SolutionVerifier(puzzleFile, solutionFile);
                    var metrics = verifier.CalculateMetrics();
                    sm_log.Debug($"Cost/cycles/area/instructions: {metrics.Cost}/{metrics.Cycles}/{metrics.Area}/{metrics.Instructions}");
                    m_reportWriter?.WriteLine($"{puzzle.Name},\"{puzzleFile}\",{metrics.Cost},{metrics.Cycles},{metrics.Area},{metrics.Instructions}");

                    sm_log.Debug($"Writing metrics to solution file");
                    solution.Metrics = metrics;
                    SolutionWriter.WriteSolution(solution, solutionFile);
                }

                return true;
            }
            catch (Exception e)
            {
                string exceptionDetail = e.Message;
                string message;
                switch (e)
                {
                    case ParseException:
                        message = "Error loading puzzle file";
                        break;
                    case SolverException:
                        message = "Error solving puzzle";
                        break;
                    case VerifierException:
                        message = "Error verifying solution to puzzle";
                        break;
                    default:
                        message = "Internal error while solving puzzle";
                        exceptionDetail = e.ToString();
                        break;
                };

                if (puzzleName != null)
                {
                    message += $" \"{puzzleName}\" from";
                }
                message += $" \"{puzzleFile}\": {exceptionDetail}";

                // Write a new line to first because there may be progress dots on the current line
                Console.WriteLine();
                sm_log.Error(message);
                return false;
            }
        }
    }
}
