using System;
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
                using var runner = new Runner(commandArgs);
                runner.Run();
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
                    case "--noverify":
                        commandArgs.SkipVerification = true;
                        break;
                    case "--report":
                    {
                        if (i + 1 >= args.Length)
                        {
                            throw new ArgumentException("Missing file for '--report' argument.");
                        }
                        commandArgs.ReportFile = args[++i];
                        break;
                    }
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
            sm_log.Error("    --report <file>       Generate a report file summarizing the solutions and their metrics");
        }
    }
}
