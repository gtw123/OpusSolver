using System;
using System.Collections.Generic;
using OpusSolver.Solver;

namespace OpusSolver
{
    public class CommandLineArguments
    {
        public List<string> PuzzleFiles = new();
        public string OutputDir;
        public SolutionType SolutionType = SolutionType.Standard;
        public string ReportFile;
        public bool GenerateMultipleSolutions = false;
        public bool AnalyzeOnly = false;
        public int MaxParallelVerifiers = Environment.ProcessorCount;
    }
}
