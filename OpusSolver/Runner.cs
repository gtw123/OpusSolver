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

        private record class PuzzleSolutions(string Name, string PuzzleFile, List<GeneratedSolution> AllSolutions)
        {
            public GeneratedSolution BestCost, BestCycles, BestArea;
            public IEnumerable<GeneratedSolution> ValidSolutions => AllSolutions.Where(s => s.PassedVerification);
            public bool IsSolved => ValidSolutions.Any();
        }

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
            var puzzleSolutions = GenerateSolutions();
            VerifySolutions(puzzleSolutions);
            FindBestSolutions(puzzleSolutions);
            GenerateReport(puzzleSolutions);

            if (m_args.GenerateMultipleSolutions)
            {
                // Explicitly log errors for puzzles that had no valid solutions since we suppress verification errors
                // when generating multiple solutions. Note that if we couldn't generate any solutions at all, we already
                // log an error for that elsewhere.
                var failedPuzzles = puzzleSolutions.Where(s => s.AllSolutions.Any() && !s.IsSolved);
                foreach (var puzzle in failedPuzzles)
                {
                    var error = new SolverException($"Could not generate any valid solutions. See log file for details.");
                    LogUtils.LogSolverException(puzzle.Name, puzzle.PuzzleFile, error, logToConsole: true);
                }
            }

            int totalSolvedPuzzles = puzzleSolutions.Where(p => p.IsSolved).Count();
            sm_log.Info($"Successfully generated solutions for {totalSolvedPuzzles}/{m_args.PuzzleFiles.Count} puzzles.");

            int totalUnsolved = m_args.PuzzleFiles.Count - totalSolvedPuzzles;
            if (totalUnsolved > 0)
            {
                sm_log.Error($"Could not generate valid solutions for {totalUnsolved} puzzles.");
            }

            if (m_reportWriter != null)
            {
                sm_log.Info($"Report saved to \"{m_args.ReportFile}\"");
                m_reportWriter.WriteLine($"Solved puzzles: {totalSolvedPuzzles}/{m_args.PuzzleFiles.Count}");
            }
        }

        private List<PuzzleSolutions> GenerateSolutions()
        {
            sm_log.Info($"Generating solutions to \"{m_args.OutputDir}\"");

            var puzzleSolutions = new List<PuzzleSolutions>();
            foreach (var puzzleFile in m_args.PuzzleFiles)
            {
                puzzleSolutions.Add(GenerateSolutionsForPuzzle(puzzleFile));
                Console.Write(".");
            }

            Console.WriteLine();

            return puzzleSolutions;
        }

        private PuzzleSolutions GenerateSolutionsForPuzzle(string puzzleFile)
        {
            string puzzleName = null;

            try
            {
                sm_log.Debug($"Loading puzzle file \"{puzzleFile}\"");
                var puzzle = PuzzleReader.ReadPuzzle(puzzleFile);

                puzzleName = puzzle.Name;
                sm_log.Debug($"Puzzle: " + Environment.NewLine + puzzle.ToString());

                var solver = new PuzzleSolver(puzzle, m_args.SolutionType);
                var solutions = solver.Solve(generateMultipleSolutions: m_args.GenerateMultipleSolutions);

                int solutionIndex = 0;
                var generatedSolutions = new List<GeneratedSolution>();
                foreach (var solution in solutions)
                {
                    string suffix = m_args.GenerateMultipleSolutions ? $"_{m_args.SolutionType}_{solutionIndex}.solution" : $"_{m_args.SolutionType}.solution";
                    string outputDir = m_args.GenerateMultipleSolutions ? Path.Combine(m_args.OutputDir, "Working") : m_args.OutputDir;
                    string solutionFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(puzzleFile) + suffix);
                    sm_log.Debug($"Writing solution to \"{solutionFile}\"");
                    SolutionWriter.WriteSolution(solution, solutionFile);

                    if (solution.Exception != null)
                    {
                        // Don't add the solution to the list as we don't want to try to verify the solution and generate
                        // another error.
                        if (!m_args.GenerateMultipleSolutions)
                        {
                            LogUtils.LogSolverException(puzzleName, puzzleFile, solution.Exception, logToConsole: true);
                        }
                    }
                    else
                    {
                        generatedSolutions.Add(new GeneratedSolution { PuzzleFile = puzzleFile, SolutionFile = solutionFile, Solution = solution });
                    }

                    solutionIndex++;
                }

                return new PuzzleSolutions(puzzleName, puzzleFile, generatedSolutions);
            }
            catch (Exception e)
            {
                LogUtils.LogSolverException(puzzleName, puzzleFile, e, logToConsole: true);
                return new PuzzleSolutions(puzzleName, puzzleFile, []);
            }
        }

        private void VerifySolutions(List<PuzzleSolutions> puzzleSolutions)
        {
            sm_log.Info("Verifying solutions...");

            var verifier = new SolutionVerifier(logErrorsToConsole: !m_args.GenerateMultipleSolutions);
            verifier.Verify(puzzleSolutions.SelectMany(p => p.AllSolutions).ToList());
        }

        private void FindBestSolutions(IEnumerable<PuzzleSolutions> puzzleSolutions)
        {
            foreach (var puzzle in puzzleSolutions.Where(p => p.IsSolved))
            {
                puzzle.BestCost = puzzle.ValidSolutions.MinBy(s => s.Solution.Metrics.Cost);
                puzzle.BestCycles = puzzle.ValidSolutions.MinBy(s => s.Solution.Metrics.Cycles);
                puzzle.BestArea = puzzle.ValidSolutions.MinBy(s => s.Solution.Metrics.Area);
            }
        }

        private void GenerateReport(IEnumerable<PuzzleSolutions> puzzleSolutions)
        {
            var metricSums = new Metrics();
            int totalPuzzles = 0;

            foreach (var puzzle in puzzleSolutions.Where(p => p.IsSolved))
            {
                var metrics = new Metrics
                {
                    Cost = puzzle.BestCost.Solution.Metrics.Cost,
                    Cycles = puzzle.BestCycles.Solution.Metrics.Cycles,
                    Area = puzzle.BestArea.Solution.Metrics.Area,
                };

                m_reportWriter?.WriteLine($"{puzzle.Name},\"{puzzle.PuzzleFile}\",{metrics.Cost},{metrics.Cycles},{metrics.Area}");

                metricSums.Add(metrics);
                totalPuzzles++;
            }

            double total = totalPuzzles;
            m_reportWriter?.WriteLine($"Average,,{metricSums.Cost / total:.00},{metricSums.Cycles / total:.00},{metricSums.Area / total:.00}");
        }
    }
}
