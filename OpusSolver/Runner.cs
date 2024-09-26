using OpusSolver.IO;
using OpusSolver.Solver;
using OpusSolver.Utils;
using OpusSolver.Verifier;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpusSolver
{
    public class Runner : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Runner));

        private CommandLineArguments m_args;
        private StreamWriter m_reportWriter;

        private List<GeneratedSolution> m_generatedSolutions = new();
        private int m_totalErrors = 0;
        private int m_totalUnsupported = 0;

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
            GenerateSolutions();

            int totalSuccessfulSolutions;
            int totalFailedVerification = 0;
            if (!m_args.SkipVerification)
            {
                VerifySolutions(m_generatedSolutions);
                totalSuccessfulSolutions = m_generatedSolutions.Count(s => s.PassedVerification);
                totalFailedVerification = m_generatedSolutions.Count - totalSuccessfulSolutions;
            }
            else
            {
                totalSuccessfulSolutions = m_generatedSolutions.Count();
            }

            string verifyMessage = m_args.SkipVerification ? "" : "and verified ";
            sm_log.Info($"Successfully generated {verifyMessage}solutions for {totalSuccessfulSolutions}/{m_args.PuzzleFiles.Count} puzzles.");

            if (totalFailedVerification > 0)
            {
                sm_log.Error($"{totalFailedVerification} solutions failed verification.");
            }

            if (m_totalErrors > 0)
            {
                sm_log.Error($"{m_totalErrors} puzzles had unexpected errors.");
            }

            if (m_totalUnsupported > 0)
            {
                sm_log.Warn($"{m_totalUnsupported} puzzles could not be solved due to solver limitations.");
            }

            if (m_reportWriter != null)
            {
                sm_log.Info($"Report saved to \"{m_args.ReportFile}\"");
                m_reportWriter.WriteLine($"Successful solutions: {totalSuccessfulSolutions}/{m_args.PuzzleFiles.Count}");
            }
        }

        private void GenerateSolutions()
        {
            sm_log.Info($"Generating solutions to \"{m_args.OutputDir}\"");

            foreach (var puzzleFile in m_args.PuzzleFiles)
            {
                string solutionFile = Path.Combine(m_args.OutputDir, Path.GetFileNameWithoutExtension(puzzleFile) + $"_{m_args.SolutionType}.solution");
                var generatedSolution = GenerateSolution(puzzleFile, solutionFile);
                if (generatedSolution != null)
                {
                    m_generatedSolutions.Add(generatedSolution);
                }

                Console.Write(".");
            }

            Console.WriteLine();
        }

        private GeneratedSolution GenerateSolution(string puzzleFile, string solutionFile)
        {
            string puzzleName = null;

            try
            {
                sm_log.Debug($"Loading puzzle file \"{puzzleFile}\"");
                var puzzle = PuzzleReader.ReadPuzzle(puzzleFile);

                puzzleName = puzzle.Name;
                sm_log.Debug($"Puzzle: " + Environment.NewLine + puzzle.ToString());

                var solver = new PuzzleSolver(puzzle, m_args.SolutionType);
                var solution = solver.Solve();

                sm_log.Debug($"Writing solution to \"{solutionFile}\"");
                SolutionWriter.WriteSolution(solution, solutionFile);

                if (solution.HasErrors)
                {
                    // In this case an error message will have already been logged, so we don't need to do that again.
                    // But return null so that we don't try to verify the solution and generate another error.
                    m_totalErrors++;
                    return null;
                }

                return new GeneratedSolution { PuzzleFile = puzzleFile, SolutionFile = solutionFile, Solution = solution };
            }
            catch (UnsupportedException e)
            {
                sm_log.Debug(e.Message);
                m_totalUnsupported++;
                return null;
            }
            catch (Exception e)
            {
                LogUtils.LogSolverException(puzzleName, puzzleFile, e);
                m_totalErrors++;
                return null;
            }
        }

        private void VerifySolutions(List<GeneratedSolution> generatedSolutions)
        {
            sm_log.Info("Verifying solutions...");

            var verifier = new SolutionVerifier();
            verifier.Verify(generatedSolutions);

            var metricSums = new Metrics();
            var verifiedSolutions = generatedSolutions.Where(s => s.PassedVerification);

            foreach (var generatedSolution in verifiedSolutions)
            {
                var solution = generatedSolution.Solution;
                var metrics = solution.Metrics;
                m_reportWriter?.WriteLine($"{solution.Puzzle.Name},\"{generatedSolution.PuzzleFile}\",{metrics.Cost},{metrics.Cycles},{metrics.Area},{metrics.Instructions}");

                metricSums.Add(metrics);
            }

            double total = verifiedSolutions.Count();
            m_reportWriter?.WriteLine($"Average,,{metricSums.Cost / total:.00},{metricSums.Cycles / total:.00},{metricSums.Area / total:.00},{metricSums.Instructions / total:.00}");

        }
    }
}
