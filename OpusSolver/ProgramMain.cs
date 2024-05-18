using OpusSolver.IO;
using OpusSolver.Solver;
using OpusSolver.Verifier;
using System;
using System.Collections.Generic;
using System.IO;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]

namespace OpusSolver
{
    public static class ProgramMain
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramMain));
        
        private class CommandLineArguments
        {
            public List<string> PuzzleFiles = new();
            public string OutputDir;
            public bool SkipVerification = false;
        }

        public static int Main(string[] args)
        {
            sm_log.Debug($"Starting up");

            CommandLineArguments commandArgs;
            try
            {
                commandArgs = ParseArguments(args);
            }
            catch (Exception e)
            {
                sm_log.Error($"Error parsing command line args: {e.Message}");
                ShowUsage();
                return 1;
            }

            int totalPuzzlesSolved = 0;
            try
            {
                foreach (var puzzleFile in commandArgs.PuzzleFiles)
                {
                    string solutionFile = Path.Combine(commandArgs.OutputDir, Path.GetFileNameWithoutExtension(puzzleFile) + ".solution");
                    if (SolvePuzzle(puzzleFile, solutionFile, commandArgs.SkipVerification))
                    {
                        totalPuzzlesSolved++;
                    }                   
                }

                string verifyMessage = commandArgs.SkipVerification ? "" : "and verified ";
                sm_log.Info($"Successfully generated {verifyMessage}solutions for {totalPuzzlesSolved}/{commandArgs.PuzzleFiles.Count} puzzles.");
            }
            catch (Exception e)
            {
                sm_log.Error("Internal error", e);
                return 1;
            }

            return 0;
        }

        private static CommandLineArguments ParseArguments(string[] args)
        {
            var commandArgs = new CommandLineArguments
            {
                OutputDir = Directory.GetCurrentDirectory()
            };
            var excludedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var puzzlePaths = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--output":
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("Missing directory for '--output' argument.");
                        }
                        commandArgs.OutputDir = Path.GetFullPath(args[++i]);
                        break;
                    }
                    case "--exclude":
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("Missing file name for '--exclude' argument.");
                        }
                        excludedFiles.Add(args[++i]);
                        break;
                    }
                    case "--noverify":
                        commandArgs.SkipVerification = true;
                        break;
                    default:
                        puzzlePaths.Add(args[i]);
                        break;
                }
            }

            if (puzzlePaths.Count == 0)
            {
                throw new ArgumentException("No puzzle files or directories specified.");
            }

            foreach (string puzzlePath in puzzlePaths)
            {
                if (Directory.Exists(puzzlePath))
                {
                    foreach (string file in Directory.GetFiles(puzzlePath, "*.puzzle", SearchOption.AllDirectories))
                    {
                        if (!excludedFiles.Contains(Path.GetFileName(file)))
                        {
                            commandArgs.PuzzleFiles.Add(Path.GetFullPath(file));
                        }
                    }
                }
                else if (File.Exists(puzzlePath))
                {
                    commandArgs.PuzzleFiles.Add(Path.GetFullPath(puzzlePath));
                }
                else
                {
                    throw new ArgumentException($"File or directory \"{puzzlePath}\" does not exist.");
                }
            }

            return commandArgs;
        }

        private static void ShowUsage()
        {
            sm_log.Error("Usage: OpusSolver.exe [<options>] <puzzle file/dir>...");
            sm_log.Error("");
            sm_log.Error("Options:");
            sm_log.Error("    --output <dir>        Directory to write solutions to (default is current dir)");
            sm_log.Error("    --exclude <file name> Name of a puzzle file to skip");
            sm_log.Error("    --noverify            Skip solution verification (useful if you don't have a copy of libverify)");
        }

        private static bool SolvePuzzle(string puzzleFile, string solutionFile, bool skipVerification)
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

                if (!skipVerification)
                {
                    sm_log.Debug("Verifying solution");
                    using var verifier = new SolutionVerifier(puzzleFile, solutionFile);
                    var metrics = verifier.CalculateMetrics();
                    sm_log.Info($"Cost/cycles/area/instructions: {metrics.Cost}/{metrics.Cycles}/{metrics.Area}/{metrics.Instructions}");

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
