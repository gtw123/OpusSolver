using System.Linq;

namespace Opus.UI
{
    public class GameScreen
    {
        public HexGrid Grid { get; private set; }
        public Sidebar Sidebar { get; private set; }
        public ProgramGrid ProgramGrid { get; private set; }

        public GameScreen(HexGrid grid, Sidebar sidebar, ProgramGrid programGrid)
        {
            Grid = grid;
            Sidebar = sidebar;
            ProgramGrid = programGrid;
        }

        public Puzzle GetPuzzle()
        {
            return new Puzzle(
                Sidebar.Products.Tools.Select(m => m.Item),
                Sidebar.Reagents.Tools.Select(m => m.Item),
                Sidebar.Mechanisms.Tools.Select(m => m.Item),
                Sidebar.Glyphs.Tools.Select(m => m.Item));
        }
    }
}
