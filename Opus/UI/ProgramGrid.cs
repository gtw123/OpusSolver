using System.Drawing;

namespace Opus.UI
{
    /// <summary>
    /// Represents the program grid at the bottom of the game window.
    /// </summary>
    public class ProgramGrid
    {
        private ScrollableArea m_area;
        private static readonly Size CellSize = new Size(41, 38);

        public ProgramGrid(Rectangle rect)
        {
            m_area = new ScrollableArea(rect);
            m_area.DragOffsetTopLeft = new Point(10, 0); // Don't drag from the left as it may cause the arm label to be highlighted, confusing the scroll detection
            m_area.DragOffsetBottomRight = new Point(0, 10);

            // To work out if the grid scrolled, we look at the number of the first visible arm
            m_area.VerticalCheckRect = new Rectangle(rect.Left - 30, rect.Top + 10, 15, 15);
        }

        public Vector2 GetNumVisibleCells()
        {
            return new Vector2(m_area.Rect.Width / CellSize.Width, m_area.Rect.Height / CellSize.Height);
        }

        /// <summary>
        /// Gets the screen location of the center of a cell.
        /// </summary>
        public Point GetCellLocation(Vector2 position)
        {
            var point = new Point(CellSize.Width * position.X + CellSize.Width / 2,
                                  CellSize.Height * position.Y + CellSize.Height / 2);

            return m_area.GetScreenLocation(point);
        }

        public Rectangle GetCellScreenBounds(Bounds cells)
        {
            var topLeft = new Point(cells.Min.X * CellSize.Width, cells.Min.Y * CellSize.Height);
            var size = new Size((cells.Max.X - cells.Min.X + 1) * CellSize.Width, (cells.Max.Y - cells.Min.Y + 1) * CellSize.Height);
            return new Rectangle(m_area.GetScreenLocation(topLeft), size);
        }

        public void SetMaxY(int maxY)
        {
            m_area.ScrollableAreaHeight = maxY * CellSize.Height;
        }

        /// <summary>
        /// Scrolls the grid if necessary so that the cell at the specified position
        /// is completely visible.
        /// </summary>
        public void EnsureCellVisible(Vector2 position)
        {
            EnsureCellsVisible(position, new Vector2(1, 1));
        }

        /// <summary>
        /// Scrolls the grid if necessary so that the cells in the coordinates given by rect
        /// are completely visible.
        /// </summary>
        public void EnsureCellsVisible(Vector2 position, Vector2 size)
        {
            var startLocation = new Point(position.X * CellSize.Width, position.Y * CellSize.Height);
            var screenSize = new Size(size.X * CellSize.Width, size.Y * CellSize.Height);
            m_area.ScrollToTopLeftIfNecessary(new Rectangle(startLocation, screenSize));
        }
    }
}
