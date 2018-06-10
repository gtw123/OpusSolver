using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using static System.FormattableString;

namespace Opus.UI.Analysis
{
    public class ScreenAnalyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ScreenAnalyzer));

        public ScreenAnalyzer()
        {
            ScreenCapture.ScreenBounds = GetGameWindowBounds();
        }

        private Rectangle GetGameWindowBounds()
        {
            sm_log.Info("Checking if window is completely on the screen");
            var window = GetGameWindow();
            if (!WindowUtils.IsWindowCompletelyVisible(window))
            {
                throw new AnalysisException("Please ensure the Opus Magnum window is completely visible and on screen.");
            }

            sm_log.Info("Finding window bounds");
            var rect = WindowUtils.GetWindowScreenRect(window);
            sm_log.Info(Invariant($"Bounds: {rect}"));

            return rect;
        }

        private IntPtr GetGameWindow()
        {
            sm_log.Info("Finding Opus Magnum process");
            var process = Process.GetProcessesByName("Lightning").FirstOrDefault(p => p.MainWindowHandle != null && p.MainWindowTitle == "Opus Magnum");
            if (process == null)
            {
                throw new AnalysisException("Cannot find the Opus Magnum window. Please make sure it is running.");
            }

            return process.MainWindowHandle;
        }

        public GameScreen Analyze()
        {
            sm_log.Info("Analyzing game window");
            using (var capture = new ScreenCapture())
            {
                var grid = new HexGridAnalyzer(capture).Analyze();
                var sidebar = new SidebarAnalyzer(grid).Analyze();
                var programGrid = new ProgramGridAnalyzer(capture).Analyze();

                return new GameScreen(grid, sidebar, programGrid);
            }
        }
    }
}
