﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]

namespace OpusSolver
{
    public static class ProgramMain
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramMain));

        private static Stopwatch m_timer;

        public static int Main(string[] args)
        {
            m_timer = Stopwatch.StartNew();
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

            try
            {
                if (commandArgs.AnalyzeOnly)
                {
                    using var analyzer = new PuzzleAnalyzer(commandArgs);
                    analyzer.Analyze();
                }
                else
                {
                    using var runner = new Runner(commandArgs);
                    runner.Run();
                }
                sm_log.Info($"Total elapsed time: {m_timer.Elapsed.TotalSeconds:0.00} s");
                return 0;
            }
            catch (Exception e)
            {
                sm_log.Error("Internal error", e);
                return 1;
            }
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
                    case "--solver":
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("Missing file for '--solver' argument.");
                        }
                        string type = args[++i];
                        if (!Enum.TryParse(type, true, out commandArgs.SolutionType))
                        {
                            throw new ArgumentException($"Unknown solver {type}.");
                        }
                        break;
                    }
                    case "--report":
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("Missing file for '--report' argument.");
                        }
                        commandArgs.ReportFile = args[++i];
                        break;
                    }
                    case "--optimize":
                        commandArgs.GenerateMultipleSolutions = true;
                        break;
                    case "--analyze":
                        commandArgs.AnalyzeOnly = true;
                        break;
                    case "--maxparallelverifiers":
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("Missing file for '--maxparallelverifiers' argument.");
                        }
                        commandArgs.MaxParallelVerifiers = int.Parse(args[++i]);
                        break;
                    }
                    default:
                        puzzlePaths.Add(args[i]);
                        break;
                }
            }

            if (commandArgs.AnalyzeOnly && string.IsNullOrEmpty(commandArgs.ReportFile))
            {
                throw new ArgumentException("--analyze requires --report");
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

            commandArgs.PuzzleFiles.Sort(StringComparer.OrdinalIgnoreCase);

            return commandArgs;
        }

        private static void ShowUsage()
        {
            sm_log.Error("Usage: OpusSolver.exe [<options>] <puzzle file/dir>...");
            sm_log.Error("");
            sm_log.Error("Options:");
            sm_log.Error("    --output <dir>        Directory to write solutions to (default is current dir)");
            sm_log.Error("    --exclude <file name> Name of a puzzle file to skip");
            string solutionTypes = string.Join(", ", Enum.GetNames(typeof(Solver.SolutionType)));
            sm_log.Error($"    --solver <solver>    Generates solutions using this solver. Valid solvers: {solutionTypes}");
            sm_log.Error("    --optimize            Generate multiple solutions for each puzzle and keep those with the best metrics.");
            sm_log.Error("                          Note: Using this option will greatly increase run time.");
            sm_log.Error("    --analyze             Analyze puzzles instead of solving them. Output will be written to the report file");
            sm_log.Error("    --report <file>       Generate a report file summarizing the solutions and their metrics (default is report.csv)");
            sm_log.Error("    --maxparallelverifiers <number> Maximum number of processes to spawn in parallel when verifying solutions. Defaults to current device's number of logical CPU cores.");
        }
    }
}
