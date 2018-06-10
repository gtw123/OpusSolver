using System.Drawing;

namespace Opus.UI
{
    /// <summary>
    /// Represents a draggable tool in a palette in the sidebar on the left of the screen.
    /// </summary>
    public class Tool
    {
        /// <summary>
        /// Location of this tool, relative to the location of the palette it's on.
        /// </summary>
        public Point Location { get; private set; }

        public Tool(Point location)
        {
            Location = location;
        }
    }

    public class Tool<TToolItem> : Tool
    {
        public TToolItem Item { get; private set; }

        public Tool(TToolItem item, Point location)
            : base(location)
        {
            Item = item;
        }
    }
}
