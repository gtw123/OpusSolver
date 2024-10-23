using System;
using System.Collections.Generic;
using OpusSolver.Solver;

namespace OpusSolver
{
    public class CommandLineArguments
    {
        public List<string> PuzzleFiles = new();
        public string OutputDir;
        public SolutionType SolutionType = SolutionType.LowCost;
        public string ReportFile = "report.csv";
        public bool GenerateMultipleSolutions = false;
        public bool AnalyzeOnly = false;
        public int MaxParallelVerifiers = Environment.ProcessorCount;
    }
}
