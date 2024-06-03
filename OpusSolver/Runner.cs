using OpusSolver.IO;
using OpusSolver.Solver;
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
            var generatedSolutions = GenerateSolutions();

            int totalSuccessfulSolutions;
            if (!m_args.SkipVerification)
            {
                VerifySolutions(generatedSolutions);
                totalSuccessfulSolutions = generatedSolutions.Count(s => s.Verified);
            }
            else
            {
                totalSuccessfulSolutions = generatedSolutions.Count();
            }

            string verifyMessage = m_args.SkipVerification ? "" : "and verified ";
            sm_log.Info($"Successfully generated {verifyMessage}solutions for {totalSuccessfulSolutions}/{m_args.PuzzleFiles.Count} puzzles.");

            if (m_reportWriter != null)
            {
                sm_log.Info($"Report saved to \"{m_args.ReportFile}\"");
                m_reportWriter.WriteLine($"Successful solutions: {totalSuccessfulSolutions}/{m_args.PuzzleFiles.Count}");
            }
        }

        private List<GeneratedSolution> GenerateSolutions()
        {
            sm_log.Info($"Generating solutions to \"{m_args.OutputDir}\"");

            var generatedSolutions = new List<GeneratedSolution>();
            foreach (var puzzleFile in m_args.PuzzleFiles)
            {
                string solutionFile = Path.Combine(m_args.OutputDir, Path.GetFileNameWithoutExtension(puzzleFile) + ".solution");
                var generatedSolution = GenerateSolution(puzzleFile, solutionFile);
                if (generatedSolution != null)
                {
                    generatedSolutions.Add(generatedSolution);
                }

                Console.Write(".");
            }

            Console.WriteLine();

            return generatedSolutions;
        }

        private GeneratedSolution GenerateSolution(string puzzleFile, string solutionFile)
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
                SolutionWriter.WriteSolution(solution, solutionFile);

                return new GeneratedSolution { PuzzleFile = puzzleFile, SolutionFile = solutionFile, Solution = solution };
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

                // Write a new line first because there may be progress dots on the current line
                Console.WriteLine();
                sm_log.Error(message);
                return null;
            }
        }

        private void VerifySolutions(List<GeneratedSolution> generatedSolutions)
        {
            sm_log.Info("Verifying solutions...");

            var verifier = new SolutionVerifier();
            verifier.Verify(generatedSolutions);

            foreach (var generatedSolution in generatedSolutions.Where(s => s.Verified))
            {
                var solution = generatedSolution.Solution;
                var metrics = solution.Metrics;
                m_reportWriter?.WriteLine($"{solution.Puzzle.Name},\"{generatedSolution.PuzzleFile}\",{metrics.Cost},{metrics.Cycles},{metrics.Area},{metrics.Instructions}");
            }
        }
    }
}
