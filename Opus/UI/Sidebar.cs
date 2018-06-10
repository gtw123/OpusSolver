using System.Drawing;

namespace Opus.UI
{
    /// <summary>
    /// The sidebar on the left of the game window containing the tools.
    /// </summary>
    public class Sidebar
    {
        /// <summary>
        /// The height of the rectangle to make visible when scrolling to a molecule.
        /// Although we technically only need 1 pixel, we use a higher number to allow
        /// for inaccuracies in the scrolling.
        /// </summary>
        public const int MoleculeScrollHeight = 2;

        public Palette<int, Molecule> Products { get; set; }
        public Palette<int, Molecule> Reagents { get; set; }
        public Palette<MechanismType, MechanismType> Mechanisms { get; set; }
        public Palette<GlyphType, GlyphType> Glyphs { get; set; }

        public ScrollableArea Area { get; private set; }

        public Rectangle Rect => Area.Rect;

        /// <summary>
        /// If true, the palettes scroll together as one unit. If false, they scroll separately
        /// and overlap each other.
        /// </summary>
        public bool ContinuousScrolling { get; private set; }

        public Sidebar(Rectangle rect, int scrollableHeight, int initialScrollPosition, bool continuousScrolling)
        {
            Area = new ScrollableArea(rect, new Point(0, initialScrollPosition));
            Area.ScrollableAreaHeight = scrollableHeight;
            ContinuousScrolling = continuousScrolling;
        }

        /// <summary>
        /// Scrolls the sidebar so that the specified tool is visible.
        /// </summary>
        /// <returns>The screen location of the tool</returns>
        public Point ScrollTo(Palette palette, Tool tool)
        {
            return ScrollTo(palette, tool.Location);
        }

        /// <summary>
        /// Scrolls the sidebar so that a tool at the specified location on a palette is visible.
        /// </summary>
        /// <returns>The screen location of the tool</returns>
        public Point ScrollTo(Palette palette, Point toolLocation)
        {
            var location = toolLocation.Add(palette.Rect.Location);

            if (ContinuousScrolling)
            {
                Area.ScrollToTopLeftIfNecessary(new Rectangle(location, new Size(1, MoleculeScrollHeight)));
            }
            else
            {
                Area.ScrollTo(palette.ScrollPosition);
            }

            return Area.GetScreenLocation(location.Add(palette.ScrollPosition));
        }
    }
}
