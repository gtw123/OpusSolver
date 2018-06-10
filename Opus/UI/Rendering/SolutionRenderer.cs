using System.Linq;
using System.Windows.Forms;
using Opus.Solution;
using static System.FormattableString;

namespace Opus.UI.Rendering
{
    public class SolutionRenderer
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(SolutionRenderer));

        public PuzzleSolution Solution { get; private set; }
        public GameScreen Screen { get; private set; }
        private Sidebar Sidebar { get; }

        public SolutionRenderer(PuzzleSolution solution, GameScreen screen)
        {
            Solution = solution;
            Screen = screen;
            Sidebar = screen.Sidebar;
        }

        public void Render()
        {
            sm_log.Info("Rendering solution");

            RenderProducts();
            RenderReagents();
            RenderMechanisms();
            RenderGlyphs();

            new ProgramRenderer(Screen.ProgramGrid, Solution.Program, Solution.GetObjects<Arm>()).Render();
        }

        private void RenderProducts()
        {
            sm_log.Info("Rendering products");

            var objects = Solution.GetObjects<Product>();
            foreach (var obj in objects)
            {
                RenderObject(obj, Sidebar.Products, Sidebar.Products[obj.ID]);
            }
        }

        private void RenderReagents()
        {
            sm_log.Info("Rendering reagents");

            var objects = Solution.GetObjects<Reagent>();
            foreach (var obj in objects)
            {
                RenderObject(obj, Sidebar.Reagents, Sidebar.Reagents[obj.ID]);
            }
        }

        private void RenderMechanisms()
        {
            // Do tracks before arms because it can be awkward to move tracks when an arm is on top of it
            RenderTracks();
            RenderArms();
        }

        private void RenderTracks()
        {
            sm_log.Info("Rendering tracks");

            var objects = Solution.GetObjects<Track>();
            foreach (var obj in objects)
            {
                var pos = obj.GetWorldPosition();
                Screen.Grid.EnsureCellVisible(pos);

                var toolLocation = Sidebar.ScrollTo(Sidebar.Mechanisms, Sidebar.Mechanisms[obj.Type]);
                var gridLocation = Screen.Grid.GetScreenLocationForCell(pos);
                MouseUtils.LeftDrag(toolLocation, gridLocation);

                RenderTrackPath(obj);
            }
        }

        private void RenderTrackPath(Track track)
        {
            var trackPos = track.GetWorldPosition();
            sm_log.Info(Invariant($"Rendering track at {trackPos}"));

            // To minimize scrollling, first try to get as much as possible of the path visible
            Screen.Grid.EnsureCellsVisible(track.GetBounds());

            var prevPosition = trackPos;
            foreach (var offset in track.Path.Skip(1))
            {
                var position = trackPos.Add(offset);
                Screen.Grid.EnsureCellVisible(position, scrollToCenter: true); // scroll to center to minimise number of scrolls if it's a long track

                // We need to recalculate both locations because the grid may have scrolled
                var location = Screen.Grid.GetScreenLocationForCell(position);
                var prevLocation = Screen.Grid.GetScreenLocationForCell(prevPosition);
                MouseUtils.LeftDrag(prevLocation, location);

                prevPosition = position;
            }
        }

        private void RenderArms()
        {
            sm_log.Info("Rendering arms");

            var objects = Solution.GetObjects<Arm>();
            foreach (var obj in objects)
            {
                RenderObject(obj, Sidebar.Mechanisms, Sidebar.Mechanisms[obj.Type], obj.Extension);
            }
        }

        private void RenderGlyphs()
        {
            sm_log.Info("Rendering glyphs");

            var objects = Solution.GetObjects<Glyph>();
            foreach (var obj in objects)
            {
                RenderObject(obj, Sidebar.Glyphs, Sidebar.Glyphs[obj.Type]);
            }
        }

        private void RenderObject(GameObject obj, Palette palette, Tool tool, int extension = 1)
        {
            var position = obj.GetWorldPosition();
            sm_log.Info(Invariant($"Rendering object {obj.GetType()} at {position}"));

            Screen.Grid.EnsureCellVisible(position);

            var toolLocation = Sidebar.ScrollTo(palette, tool);
            var gridLocation = Screen.Grid.GetScreenLocationForCell(position);
            MouseUtils.LeftDrag(toolLocation, gridLocation, keepMouseDown: true);

            SetDirection(obj.Rotation);
            SetExtension(extension);
            MouseUtils.SendMouseEvent(MouseEvent.LeftUp);
            ThreadUtils.SleepOrAbort(50);
        }

        private static void SetDirection(int direction)
        {
            switch (direction)
            {
                case Direction.W:
                    KeyboardUtils.KeyPress(Keys.A);
                    goto case Direction.NW;
                case Direction.NW:
                    KeyboardUtils.KeyPress(Keys.A);
                    goto case Direction.NE;
                case Direction.NE:
                    KeyboardUtils.KeyPress(Keys.A);
                    break;
                case Direction.SW:
                    KeyboardUtils.KeyPress(Keys.D);
                    goto case Direction.SE;
                case Direction.SE:
                    KeyboardUtils.KeyPress(Keys.D);
                    break;
            }
        }

        private static void SetExtension(int extension)
        {
            if (extension > 1)
            {
                KeyboardUtils.KeyPress(Keys.W);

                if (extension > 2)
                {
                    KeyboardUtils.KeyPress(Keys.W);
                }
            }
        }
    }
}
