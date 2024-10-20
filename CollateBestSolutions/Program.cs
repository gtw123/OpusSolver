using OpusSolver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CollateBestSolutions
{
    internal class Program
    {
        private class Solution
        {
            public string SolutionFilePath;
            public string SolutionName;
            public string PuzzleFileName;
            public Metrics Metrics;
        }

        static void Main(string[] args)
        {
            string sourceDir = Path.GetFullPath(args[0]);
            string destDir = Path.GetFullPath(args[1]);

            var solutions = ReadSolutions(sourceDir);
            CopyBestSolutions(solutions, destDir);
        }

        private static List<Solution> ReadSolutions(string sourceDir)
        {
            var solution = new List<Solution>();
            foreach (string file in Directory.GetFiles(sourceDir, "*.solution", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(Path.GetDirectoryName(file)) == "Working")
                {
                    continue;
                }

                try
                {
                    solution.Add(ReadSolutionFile(file));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading solution file \"{file}\": {e.Message}");
                }
            }

            return solution;
        }

        private static Solution ReadSolutionFile(string file)
        {
            using var reader = new BinaryReader(File.OpenRead(file));

            var version = reader.ReadInt32();
            if (version != 7)
            {
                throw new Exception($"Unsupported solution file version: {version}");
            }

            string puzzleFileName = reader.ReadString();
            string solutionName = reader.ReadString();

            int numMetrics = reader.ReadInt32();
            if (numMetrics != 4)
            {
                throw new Exception("Solution has no metrics");
            }

            var metrics = new Metrics();

            int metricType = reader.ReadInt32();
            if (metricType != 0)
            {
                throw new Exception($"Expected cycles metric but found metric type {metricType}.");
            }

            metrics.Cycles = reader.ReadInt32();

            metricType = reader.ReadInt32();
            if (metricType != 1)
            {
                throw new Exception($"Expected cost metric but found metric type {metricType}.");
            }

            metrics.Cost = reader.ReadInt32();

            metricType = reader.ReadInt32();
            if (metricType != 2)
            {
                throw new Exception($"Expected area metric but found metric type {metricType}.");
            }

            metrics.Area = reader.ReadInt32();


            metricType = reader.ReadInt32();
            if (metricType != 3)
            {
                throw new Exception($"Expected instruction metric but found metric type {metricType}.");
            }

            metrics.Instructions = reader.ReadInt32();

            return new Solution { SolutionFilePath = file, SolutionName = solutionName, PuzzleFileName = puzzleFileName, Metrics = metrics };
        }

        private static void CopyBestSolutions(List<Solution> solutions, string destDir)
        {
            if (Directory.Exists(destDir))
            {
                foreach (string file in Directory.GetFiles(destDir, "*.solution"))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(destDir);
            }

            void CopySolutionToOutputDir(Solution solution, string suffix)
            {
                string solutionFile = Path.Combine(destDir, Path.GetFileNameWithoutExtension(solution.PuzzleFileName) + $"_{suffix}.solution");
                File.Copy(solution.SolutionFilePath, solutionFile, overwrite: true);
            }

            using var reportWriter = new StreamWriter(Path.Combine(destDir, "report.csv"));

            foreach (var puzzleSolutions in solutions.GroupBy(s => s.PuzzleFileName).OrderBy(g => g.Key))
            {
                var bestCost = puzzleSolutions.OrderBy(s => s.Metrics.Cost).ThenBy(s => s.Metrics.Cycles).ThenBy(s => s.Metrics.Area).First();
                CopySolutionToOutputDir(bestCost, "Cost");

                var bestCycles = puzzleSolutions.OrderBy(s => s.Metrics.Cycles).ThenBy(s => s.Metrics.Area).ThenBy(s => s.Metrics.Cost).First();
                CopySolutionToOutputDir(bestCycles, "Cycles");

                var bestArea = puzzleSolutions.OrderBy(s => s.Metrics.Area).ThenBy(s => s.Metrics.Cost).ThenBy(s => s.Metrics.Cycles).First();
                CopySolutionToOutputDir(bestArea, "Area");

                reportWriter.WriteLine($"{puzzleSolutions.Key},{bestCost.Metrics.Cost},{bestCycles.Metrics.Cycles},{bestArea.Metrics.Area}");
            }
        }
    }
}
