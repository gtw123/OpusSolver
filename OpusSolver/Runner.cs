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

            foreach (var puzzleFile in m_args.PuzzleFiles)
            {
                string solutionFile = Path.Combine(m_args.OutputDir, Path.GetFileNameWithoutExtension(puzzleFile) + ".solution");
                if (SolvePuzzle(puzzleFile, solutionFile))
                {
                    totalPuzzlesSolved++;
                }                   
            }

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
            try
            {
                sm_log.Info($"Loading puzzle file \"{puzzleFile}\"");
                var puzzle = PuzzleReader.ReadPuzzle(puzzleFile);

                sm_log.Info($"Puzzle name: {puzzle.Name}");

                var solver = new PuzzleSolver(puzzle);
                var solution = solver.Solve();

                sm_log.Info($"Writing solution to \"{solutionFile}\"");
                // It might be more efficient to write the solution to a byte array first and pass that to the verifier,
                // rather than writing it to disk twice. But it's also convenient having a copy on disk even if the
                // verification fails so that we can debug it in the game.
                SolutionWriter.WriteSolution(solution, solutionFile);

                if (!m_args.SkipVerification)
                {
                    sm_log.Debug("Verifying solution");
                    using var verifier = new SolutionVerifier(puzzleFile, solutionFile);
                    var metrics = verifier.CalculateMetrics();
                    sm_log.Info($"Cost/cycles/area/instructions: {metrics.Cost}/{metrics.Cycles}/{metrics.Area}/{metrics.Instructions}");
                    m_reportWriter?.WriteLine($"{puzzle.Name},\"{puzzleFile}\",{metrics.Cost},{metrics.Cycles},{metrics.Area},{metrics.Instructions}");

                    sm_log.Debug($"Writing metrics to solution file");
                    solution.Metrics = metrics;
                    SolutionWriter.WriteSolution(solution, solutionFile);
                }

                return true;
            }
            catch (ParseException e)
            {
                sm_log.Error($"Error loading puzzle file \"{puzzleFile}\": {e.Message}");
                return false;
            }
            catch (SolverException e)
            {
                sm_log.Error($"Error solving puzzle \"{puzzleFile}\": {e.Message}");
                return false;
            }
            catch (VerifierException e)
            {
                sm_log.Error($"Error verifying solution to puzzle \"{puzzleFile}\": {e.Message}");
                return false;
            }
            catch (Exception e)
            {
                sm_log.Error($"Internal error while solving puzzle \"{puzzleFile}\"" , e);
                return false;
            }
        }
    }
}
