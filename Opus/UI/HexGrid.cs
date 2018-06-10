using System;
using System.Drawing;

namespace Opus.UI
{
    /// <summary>
    /// Represents a hexagonal grid.
    /// </summary>
    public class HexGrid
    {
        /// <summary>
        /// Total width of one hexagon.
        /// </summary>
        public const int HexWidth = 82;

        /// <summary>
        /// Total height of one hexagon.
        /// </summary>
        public const int HexHeight = 96;

        /// <summary>
        /// Height of just the vertical edge of one hexagon.
        /// </summary>
        public const int HexEdgeHeight = 46;

        /// <summary>
        /// Vertical distance between two rows of hexagons (this is less than HexHeight because they overlap).
        /// </summary>
        public const int RowHeight = (HexHeight + HexEdgeHeight) / 2;

        /// <summary>
        /// The location of the center of the cell at coordinates (0, 0), in screen coordinates.
        /// </summary>
        public Point CenterLocation { get; set; }

        public Rectangle Rect => m_area.Rect;

        private ScrollableArea m_area;

        /// <summary>
        /// HexGrid constructor.
        /// </summary>
        /// <param name="rect">The rectangle on the screen encompassing the grid</param>
        /// <param name="centerLocation">The location of the center of the center cell, in screen coordinates</param>
        public HexGrid(Rectangle rect, Point centerLocation)
        {
            m_area = new ScrollableArea(rect);
            m_area.DragOffsetTopLeft = new Point(80, 80);
            m_area.DragOffsetBottomRight = new Point(80, 80);

            CenterLocation = centerLocation;
        }

        /// <summary>
        /// Gets the screen location of the center of a cell.
        /// </summary>
        public Point GetScreenLocationForCell(Vector2 position)
        {
            return m_area.GetScreenLocation(GetCellLocationLocal(position));
        }

        /// <summary>
        /// Gets the location of the center of a cell relative to the top left of the grid.
        /// </summary>
        private Point GetCellLocationLocal(Vector2 position)
        {
            return GetCellLocationLocalCenter(position).Add(CenterLocation).Subtract(m_area.Rect.Location);
        }

        /// <summary>
        /// Gets the location of the center of a cell relative to the center of the grid.
        /// </summary>
        private static Point GetCellLocationLocalCenter(Vector2 position)
        {
            return new Point(HexWidth * position.X + HexWidth / 2 * position.Y, -RowHeight * position.Y);
        }

        /// <summary>
        /// Gets the coordinate of the cell at the specified screen location.
        /// The location may not be accurate when the screen location is near a diagonal edge of a cell.
        /// </summary>
        public Vector2 GetCellFromScreenLocation(Point screenLocation)
        {
            var scrollLocation = m_area.GetScrollableAreaLocation(screenLocation);
            var gridLocation = scrollLocation.Add(m_area.Rect.Location).Subtract(CenterLocation);
            double x = (gridLocation.X + gridLocation.Y / 2.0) / HexWidth;
            double y = -gridLocation.Y / (double)RowHeight;

            return new Vector2((int)Math.Round(x), (int)Math.Round(y));
        }

        /// <summary>
        /// Gets the approximate range of cells currently visible in the grid. Note that the "rectangle"
        /// represented by min and max actually corresponds to a parallelogram on the screen. Therefore,
        /// some of the cells within the rectangle from min to max will not actually be visible on the
        /// screen.
        /// </summary>
        public Bounds GetVisibleCells()
        {
            Vector2 cell1 = GetCellFromScreenLocation(Rect.Location);
            Vector2 cell2 = GetCellFromScreenLocation(new Point(Rect.Right - 1, Rect.Bottom - 1));

            return new Bounds
            {
                Min = new Vector2(cell1.X, cell2.Y),
                Max = new Vector2(cell2.X, cell1.Y)
            };
        }

        /// <summary>
        /// Scrolls the grid if necessary so that the cell at the specified coordinates
        /// is completely visible.
        /// </summary>
        public void EnsureCellVisible(Vector2 position, bool scrollToCenter = false)
        {
            EnsureCellsVisible(new Bounds(position, position), scrollToCenter);
        }

        /// <summary>
        /// Scrolls the grid if necessary so that the cells in the coordinates given by bounds
        /// are completely visible.
        /// </summary>
        public void EnsureCellsVisible(Bounds bounds, bool scrollToCenter = false)
        {
            var bottomLeft = GetCellLocationLocal(bounds.Min);
            var topRight = GetCellLocationLocal(bounds.Max.Add(new Vector2(0, 1))); // Add (0, 1) so we avoid the exit button in the top-right corner of the screen
            var rect = new Rectangle(bottomLeft.X - HexWidth / 2, topRight.Y - HexHeight / 2,
                topRight.X - bottomLeft.X + HexWidth, bottomLeft.Y - topRight.Y + HexHeight);
            if (scrollToCenter)
            {
                m_area.ScrollToCenterIfNecessary(rect);
            }
            else
            {
                m_area.ScrollMinimalIfNecessary(rect);
            }
        }

        /// <summary>
        /// Scrolls the grid so that the center of the cell at the specified coordinates is in the middle of the grid.
        /// </summary>
        public void ScrollTo(Vector2 position)
        {
            m_area.ScrollTo(GetCellLocationLocalCenter(position));
        }
    }
}
