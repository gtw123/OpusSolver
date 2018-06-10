using System;
using System.Windows.Forms;
using Opus.Solution.Solver;
using Opus.UI.Analysis;
using Opus.UI.Rendering;

namespace Opus
{
    public partial class Form1 : Form
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ProgramMain));

        private HotKeyHandler m_hotKeyHandler;

        public Form1()
        {
            InitializeComponent();

            Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_hotKeyHandler = new HotKeyHandler(this);
            m_hotKeyHandler.RegisterHotKey(Keys.A, HotKeyHandler.ModifierKeys.Win | HotKeyHandler.ModifierKeys.Shift, HandleHotKey);
        }

        private void HandleHotKey(Keys key, HotKeyHandler.ModifierKeys modifiers)
        {
            var analyzer = CreateAnalyzer();
            if (analyzer != null)
            {
                SolvePuzzle(analyzer);
            }
        }

        private ScreenAnalyzer CreateAnalyzer()
        {
            try
            {
                // We do this separately just so we can give a better error message if it fails.
                return new ScreenAnalyzer();
            }
            catch (AnalysisException e)
            {
                HandleError(e.Message, e);
            }
            catch (Exception e)
            {
                HandleError("Internal error: " + e.ToString(), e);
            }

            return null;
        }

        private void SolvePuzzle(ScreenAnalyzer analyzer)
        {
            try
            {
                var screen = analyzer.Analyze();
                var solver = new PuzzleSolver(screen.GetPuzzle());
                var solution = solver.Solve();
                new SolutionRenderer(solution, screen).Render();
            }
            catch (AbortException)
            {
                sm_log.Info("Solution aborted");
            }
            catch (AnalysisException e)
            {
                HandleError("A problem occurred while analyzing the screen. Please ensure a puzzle is open." +
                    Environment.NewLine + Environment.NewLine + "Error message: " + e.Message, e);
            }
            catch (SolverException e)
            {
                HandleError("Unable to solve this puzzle: " + e.Message, e);
            }
            catch (RenderException e)
            {
                HandleError("A problem occurred while rendering the solution: " + e.Message, e);
            }
            catch (Exception e)
            {
                HandleError("Internal error: " + e.ToString(), e);
            }
        }

        private void HandleError(string message, Exception e)
        {
            sm_log.Error(e);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        protected override void WndProc(ref Message m)
        {
            if (m_hotKeyHandler != null)
            {
                m_hotKeyHandler.WndProc(m);
            }

            base.WndProc(ref m);
        }
    }
}
