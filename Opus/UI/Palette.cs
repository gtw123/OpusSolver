using System.Collections.Generic;
using System.Drawing;

namespace Opus.UI
{
    /// <summary>
    /// Represents a tool palette in the sidebar on the left of the game window.
    /// </summary>
    public class Palette
    {
        /// <summary>
        /// Position to which the sidebar must be scrolled for the palette (or at least the top part of it)
        /// to be visible on the screen.
        /// </summary>
        public Point ScrollPosition { get; private set; }

        /// <summary>
        /// Bounds of the palette, relative to the top left of the sidebar.
        /// </summary>
        public Rectangle Rect { get; private set; }

        public Palette(Point scrollPosition, Rectangle rect)
        {
            ScrollPosition = scrollPosition;
            Rect = rect;
        }
    }

    public class Palette<TToolKey, TToolItem> : Palette
    {
        private Dictionary<TToolKey, Tool<TToolItem>> m_tools = new Dictionary<TToolKey, Tool<TToolItem>>();

        public Palette(Point scrollPosition, Rectangle rect)
            : base (scrollPosition, rect)
        {
        }

        public void AddTool(TToolKey key, Tool<TToolItem> tool)
        {
            m_tools[key] = tool;
        }

        public IEnumerable<Tool<TToolItem>> Tools
        {
            get { return m_tools.Values; }
        }

        public Tool<TToolItem> this[TToolKey key]
        {
            get { return m_tools[key]; }
        }
    }
}
