using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using OpusSolver.IO;

namespace OpusSolver.Verifier
{
    public sealed class SolutionVerifier
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(SolutionVerifier));
        private static readonly Regex sm_solutionNameRegex = new Regex(@"^SOLUTION: (.*)", RegexOptions.Compiled);

        private bool m_logErrorsToConsole;

        public SolutionVerifier(bool logErrorsToConsole)
        {
            m_logErrorsToConsole = logErrorsToConsole;
        }

        public void Verify(List<GeneratedSolution> generatedSolutions)
        {
            var runners = new List<VerifierRunner>();

            try
            {
                var batches = CreateBatches(generatedSolutions);
                foreach (var batch in batches)
                {
                    runners.Add(new VerifierRunner(batch));
                }

                var combinedOutput = new StringBuilder();
                foreach (var runner in runners)
                {
                    combinedOutput.Append(runner.WaitForExit());
                }

                Console.WriteLine();

                sm_log.Info("Updating solution files...");
                ProcessOutput(combinedOutput.ToString(), generatedSolutions);
            }
            finally
            {
                foreach (var runner in runners)
                {
                    runner.Dispose();
                }
            }
        }

        private IEnumerable<List<GeneratedSolution>> CreateBatches(List<GeneratedSolution> generatedSolutions)
        {
            int numBatches = Environment.ProcessorCount;
            int firstIndex = 0;
            for (int i = 0; i < numBatches; i++)
            {
                // Calculate the index so that the batches are approximately equal size (or as close as we can get)
                int lastIndex = (i + 1) * generatedSolutions.Count / numBatches;
                yield return generatedSolutions.GetRange(firstIndex, lastIndex - firstIndex);
                firstIndex = lastIndex;
            }
        }

        private class VerifierRunner : IDisposable
        {
            private StringBuilder m_output = new StringBuilder();
            private string m_inputFile = Path.GetTempFileName();
            private Process m_process;

            public VerifierRunner(IEnumerable<GeneratedSolution> generatedSolutions)
            {
                string verifierDir = AppDomain.CurrentDomain.BaseDirectory;
                string verifierExe = Path.Combine(verifierDir, "SolutionVerifier.exe");

                string args = $"--batch \"{m_inputFile}\"";
                using (var writer = new StreamWriter(m_inputFile))
                {
                    foreach (var solution in generatedSolutions)
                    {
                        writer.WriteLine($"{solution.PuzzleFile};{solution.SolutionFile}");
                    }
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = verifierExe,
                    WorkingDirectory = verifierDir,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                };

                m_process = new Process();
                m_process.StartInfo = startInfo;
                m_process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        m_output.AppendLine(e.Data);
                        if (sm_solutionNameRegex.IsMatch(e.Data))
                        {
                            Console.Write(".");
                        }
                    }
                };

                sm_log.Debug($"Running verifier: {verifierExe} {args}");
                m_process.Start();
                m_process.BeginOutputReadLine();
            }

            public string WaitForExit()
            {
                m_process.WaitForExit();

                int exitCode = m_process.ExitCode;
                sm_log.Debug($"Verifier {m_process.Id} exited with code {exitCode}");
                if (exitCode != 0)
                {
                    sm_log.Debug("Output:" + Environment.NewLine + m_output.ToString());
                    throw new Exception($"Error verifying solutions: SolutionVerifier.exe exited with code {exitCode}. See log file for output.");
                }

                return m_output.ToString();
            }

            public void Dispose()
            {
                m_process?.Dispose();
                m_process = null;

                File.Delete(m_inputFile);
            }
        }

        private void ProcessOutput(string output, IEnumerable<GeneratedSolution> generatedSolutions)
        {
            var solutionDict = generatedSolutions.ToDictionary(s => s.SolutionFile, s => s, StringComparer.OrdinalIgnoreCase);
            var remainingSolutions = new HashSet<string>(solutionDict.Keys, StringComparer.OrdinalIgnoreCase);

            using var reader = new StringReader(output.ToString());
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Match match;
                if (!(match = sm_solutionNameRegex.Match(line)).Success)
                {
                    throw new Exception("Error parsing verifier output. Expected solution file name but was: " + line);
                }

                string solutionFile = match.Groups[1].Value;
                if (!solutionDict.TryGetValue(solutionFile, out var generatedSolution))
                {
                    throw new Exception("Unexpected solution file returned by verifier: " + solutionFile);
                }
                remainingSolutions.Remove(solutionFile);

                if ((line = reader.ReadLine()) == null)
                {
                    throw new Exception($"Error parsing verifier output for solution \"{solutionFile}\". No SUCCESS/ERROR status was generated.");
                }

                if ((match = Regex.Match(line, @"^SUCCESS: (\d+)/(\d+)/(\d+)/(\d+)$")).Success)
                {
                    var metrics = new Metrics
                    {
                        Cost = Int32.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                        Cycles = Int32.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                        Area = Int32.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                        Instructions = Int32.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
                    };

                    sm_log.Debug($"Solution \"{generatedSolution.SolutionFile}\" verified successfully. ");
                    sm_log.Debug($"Cost/cycles/area/instructions: {metrics.Cost}/{metrics.Cycles}/{metrics.Area}/{metrics.Instructions}");
                    generatedSolution.PassedVerification = true;

                    sm_log.Debug($"Writing metrics to solution file");
                    generatedSolution.Solution.Metrics = metrics;
                    SolutionWriter.WriteSolution(generatedSolution.Solution, generatedSolution.SolutionFile);
                }
                else if ((match = Regex.Match(line, @"ERROR: (.*)")).Success)
                {
                    sm_log.Debug($"Solution \"{generatedSolution.SolutionFile}\" failed verification.");
                    string errorMessage = $"Error verifying solution for puzzle {generatedSolution.Solution.Puzzle.Name} from \"{generatedSolution.PuzzleFile}\": {match.Groups[1].Value}";
                    if (m_logErrorsToConsole)
                    {
                        sm_log.Error(errorMessage);
                    }
                    else
                    {
                        sm_log.Debug(errorMessage);
                    }

                    generatedSolution.PassedVerification = false;
                }
                else
                {
                    throw new Exception($"Error parsing output of verifier for solution \"{solutionFile}\". Invalid result: {line}");
                }
            }

            if (remainingSolutions.Count != 0)
            {
                string missingMessage = string.Join(Environment.NewLine, remainingSolutions.OrderBy(s => s));
                throw new Exception("Error parsing verifier output: no results received for: " + Environment.NewLine + missingMessage);
            }
        }
    }
}
