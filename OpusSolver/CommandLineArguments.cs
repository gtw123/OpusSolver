using System.Collections.Generic;

namespace OpusSolver
{
    public class CommandLineArguments
    {
        public List<string> PuzzleFiles = new();
        public string OutputDir;
        public bool SkipVerification = false;
        public string ReportFile;
        public bool AnalyzeOnly = false;
    }
}
