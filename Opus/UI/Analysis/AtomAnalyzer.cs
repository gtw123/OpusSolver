using static System.FormattableString;

namespace Opus.UI.Analysis
{
    /// <summary>
    /// Analyzes atoms on the hex grid.
    /// </summary>
    public class AtomAnalyzer : Analyzer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(AtomAnalyzer));

        private HexGrid m_grid;
        private ElementAnalyzer m_elementAnalyzer;
        private BondAnalyzer m_bondAnalyzer;
        private MoleculeType m_type;

        public AtomAnalyzer(ScreenCapture capture, HexGrid grid, MoleculeType type)
            : base(capture)
        {
            m_grid = grid;
            m_type = type;

            m_elementAnalyzer = new ElementAnalyzer(capture, m_type);
            m_bondAnalyzer = new BondAnalyzer(capture, m_type);
        }

        /// <summary>
        /// Analyzes the atom at the specified position on the hex grid.
        /// </summary>
        public Atom Analyze(Vector2 position)
        {
            var location = m_grid.GetScreenLocationForCell(position).Subtract(Capture.Rect.Location);
            var element = m_elementAnalyzer.Analyze(location);
            if (element != null)
            {
                sm_log.Info(Invariant($"Found {m_type} {element} atom at position {position} and screen location {location}"));
                var bonds = m_bondAnalyzer.Analyze(location);
                return new Atom(element.Value, bonds, position);
            }

            sm_log.Info(Invariant($"Found no {m_type} atom at position {position} and screen location {location}"));

            return null;
        }
    }
}
