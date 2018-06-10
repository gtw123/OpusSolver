namespace Opus.UI.Analysis
{
    public class Analyzer
    {
        public ScreenCapture Capture { get; private set; }

        public Analyzer(ScreenCapture capture)
        {
            Capture = capture;
        }
    }
}
