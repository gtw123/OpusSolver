using System;
using System.Drawing;
using static System.FormattableString;

namespace Opus.UI
{
    /// <summary>
    /// Represents an area of the game window that can be scrolled using the mouse cursor.
    /// </summary>
    public class ScrollableArea
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ScrollableArea));

        /// <summary>
        /// Screen location of the visible part of the scrollable area.
        /// </summary>
        public Rectangle Rect { get; private set; }

        /// <summary>
        /// The current position of the visible scroll area at the top-left corner of Rect.
        /// </summary>
        private Point m_scrollPosition;
        public Point ScrollPosition => m_scrollPosition;

        /// <summary>
        /// The height of the scrollable area. If set, this class will never attempt to scroll past it.
        /// If null, the area can be scrolled indefinitely.
        /// </summary>
        public int? ScrollableAreaHeight { get; set; }

        /// <summary>
        /// Offset from the top left of Rect where the mouse cursor will be placed when scrolling right or down.
        /// </summary>
        public Point DragOffsetTopLeft { get; set; }

        /// <summary>
        /// Offset from the top left of Rect where the mouse cursor will be placed when scrolling left or up.
        /// </summary>
        public Point DragOffsetBottomRight { get; set; }

        /// <summary>
        /// Rectangle used to detect if the area actually scrolled up/down.
        /// </summary>
        public Rectangle? VerticalCheckRect { get; set; }

        /// <summary>
        /// Extra amount of time (in ms) to wait between clicking and dragging when scrolling the area.
        /// </summary>
        public int ScrollDelay { get; set; }

        public ScrollableArea(Rectangle rect)
            : this(rect, new Point(0, 0))
        {
        }

        public ScrollableArea(Rectangle rect, Point initialScrollPosition)
        {
            Rect = rect;
            m_scrollPosition = initialScrollPosition;
        }

        /// <summary>
        /// Gets the screen location of a point in the scrollable area.
        /// Note that this may be off the screen or outside the visible part of the
        /// scrollable area.
        /// </summary>
        public Point GetScreenLocation(Point point)
        {
            return point.Add(Rect.Location).Subtract(m_scrollPosition);
        }

        /// <summary>
        /// Gets the location in the scrollable area corresponding to a point on the screen.
        /// </summary>
        /// <returns></returns>
        public Point GetScrollableAreaLocation(Point screenLocation)
        {
            return screenLocation.Add(m_scrollPosition).Subtract(Rect.Location);
        }

        /// <summary>
        /// Scrolls if necessary so that the target rectangle is completely visible on the screen.
        /// If scrolling is required, the rectangle will be positioned at the top left of the screen.
        /// Doesn't scroll unnecessarily in one direction if it's not required.
        /// </summary>
        public void ScrollToTopLeftIfNecessary(Rectangle targetRect)
        {
            var targetLocation = m_scrollPosition;
            if (targetRect.Left < m_scrollPosition.X || targetRect.Right > m_scrollPosition.X + Rect.Width)
            {
                targetLocation.X = targetRect.Left;
            }

            if (targetRect.Top < m_scrollPosition.Y || targetRect.Bottom > m_scrollPosition.Y + Rect.Height)
            {
                targetLocation.Y = targetRect.Top;
            }

            ScrollTo(targetLocation);
        }

        /// <summary>
        /// Scrolls if necessary so that the target rectangle is completely visible on the screen.
        /// If scrolling is required, the rectangle will be positioned at center screen.
        /// Doesn't scroll unnecessarily in one direction if it's not required.
        /// </summary>
        public void ScrollToCenterIfNecessary(Rectangle targetRect)
        {
            var targetLocation = m_scrollPosition;
            if (targetRect.Left < m_scrollPosition.X || targetRect.Right > m_scrollPosition.X + Rect.Width)
            {
                // If the target rect is wider than the visible width, prefer the left side of the target rect
                int centerX = targetRect.Left + Math.Min(targetRect.Width, Rect.Width) / 2;
                targetLocation.X = centerX - Rect.Width / 2;
            }

            if (targetRect.Top < m_scrollPosition.Y || targetRect.Bottom > m_scrollPosition.Y + Rect.Height)
            {
                // If the target rect is taller than the visible height, prefer the top side of the target rect
                int centerY = targetRect.Top + Math.Min(targetRect.Height, Rect.Height) / 2;
                targetLocation.Y = centerY - Rect.Height / 2;
            }

            ScrollTo(targetLocation);
        }

        /// <summary>
        /// Scrolls as necessary so that the target rectangle is completely visible on the screen.
        /// If scrolling is required, the smallest amount of scrolling will be done to ensure
        /// the rectangle is visible on the screen.
        /// Doesn't scroll unnecessarily in one direction if it's not required.
        /// </summary>
        public void ScrollMinimalIfNecessary(Rectangle targetRect)
        {
            var targetLocation = m_scrollPosition;
            if (targetRect.Left < m_scrollPosition.X)
            {
                targetLocation.X = targetRect.Left;
            }
            else if (targetRect.Right > m_scrollPosition.X + Rect.Width)
            {
                // If the target rect is wider than the visible width, prefer the left side of the target rect
                targetLocation.X = targetRect.Left + Math.Min(targetRect.Width, Rect.Width) - Rect.Width;
            }

            if (targetRect.Top < m_scrollPosition.Y)
            {
                targetLocation.Y = targetRect.Top;
            }
            else if (targetRect.Bottom > m_scrollPosition.Y + Rect.Height)
            {
                // If the target rect is taller than the visible height, prefer the top side of the target rect
                targetLocation.Y = targetRect.Bottom + Math.Min(targetRect.Height, Rect.Height) - Rect.Height;
            }

            ScrollTo(targetLocation);
        }

        /// <summary>
        /// Scrolls the area by the specified amount (relative to the current scroll position).
        /// </summary>
        public void ScrollBy(Point targetDelta)
        {
            ScrollTo(targetDelta.Add(ScrollPosition));
        }

        /// <summary>
        /// Scrolls the area so that the specified location is visible at the top-left corner.
        /// </summary>
        public void ScrollTo(Point targetLocation)
        {
            if (ScrollableAreaHeight != null)
            {
                // Don't scroll too far vertically
                int maxScrollY = ScrollableAreaHeight.Value - Rect.Height;
                targetLocation.Y = Math.Min(targetLocation.Y, maxScrollY);
                targetLocation.Y = Math.Max(targetLocation.Y, 0);
            }

            var delta = targetLocation.Subtract(m_scrollPosition);
            if (VerticalCheckRect.HasValue)
            {
                // Scroll X and Y separately to reduce chances of errors
                InternalScrollBy(new Point(0, delta.Y));
                InternalScrollBy(new Point(delta.X, 0));
            }
            else
            {
                InternalScrollBy(delta);
            }
        }

        /// <summary>
        /// Scrolls the area by the specified amount (relative to the current scroll position).
        /// </summary>
        private void InternalScrollBy(Point targetDelta)
        {
            var start = CalculateDragLocation(targetDelta);

            var delta = targetDelta;
            while (delta.Y != 0 || delta.X != 0)
            {
                var end = start.Subtract(delta);
                end.X = Math.Max(end.X, ScreenCapture.ScreenBounds.Left);
                end.X = Math.Min(end.X, ScreenCapture.ScreenBounds.Right - 1);
                end.Y = Math.Max(end.Y, ScreenCapture.ScreenBounds.Top);
                end.Y = Math.Min(end.Y, ScreenCapture.ScreenBounds.Bottom - 1);

                DragArea(start, end);
                delta = delta.Subtract(start.Subtract(end));
            }

            m_scrollPosition = m_scrollPosition.Add(targetDelta);
        }

        /// <summary>
        /// Calculates the coordinates where the mouse drag will start so as to maximize the
        /// distance that can be scrolled in one go.
        /// </summary>
        private Point CalculateDragLocation(Point delta)
        {
            var location = new Point();
            if (delta.X > 0)
            {
                // Scrolling to the left, so drag from the right hand side
                location.X = Rect.Right - DragOffsetBottomRight.X;
            }
            else
            {
                // Scrolling to the right, so drag from the left hand side
                location.X = Rect.Left + DragOffsetTopLeft.X;
            }

            if (delta.Y > 0)
            {
                location.Y = Rect.Bottom - DragOffsetBottomRight.Y;
            }
            else
            {
                location.Y = Rect.Top + DragOffsetTopLeft.Y;
            }

            return location;
        }

        private void DragArea(Point start, Point end)
        {
            if (start.X != end.X || !VerticalCheckRect.HasValue)
            {
                MouseUtils.RightDrag(start, end, ScrollDelay);
                return;
            }

            const int maxAttempts = 5;
            if (!DragAreaWithVerticalCheck(start, end))
            {
                for (int i = 1; i < maxAttempts; i++)
                {
                    ScrollDelay += 100;
                    sm_log.Info("Increasing scroll delay to " + ScrollDelay);

                    sm_log.Info("Retrying scroll...");
                    if (DragAreaWithVerticalCheck(start, end))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(Invariant($"Attempted to drag from {start} to {end} but no movement detected after {maxAttempts} attempts."));
            }
        }

        private bool DragAreaWithVerticalCheck(Point start, Point end)
        {
            using (var capture1 = new ScreenCapture(VerticalCheckRect.Value))
            {
                MouseUtils.RightDrag(start, end, ScrollDelay);
                using (var capture2 = new ScreenCapture(VerticalCheckRect.Value))
                {
                    if (!BitmapComparer.AreBitmapsIdentical(capture1.Bitmap, capture2.Bitmap))
                    {
                        sm_log.Info("Drag successful");
                        return true;
                    }

                    sm_log.Warn(Invariant($"Attempted to drag from {start} to {end} but detected no vertical movement. Waiting a little while before checking again..."));
                    ThreadUtils.SleepOrAbort(100 + ScrollDelay);

                    using (var capture3 = new ScreenCapture(VerticalCheckRect.Value))
                    {
                        if (!BitmapComparer.AreBitmapsIdentical(capture1.Bitmap, capture3.Bitmap))
                        {
                            sm_log.Info("Drag successful");
                            return true;
                        }

                        sm_log.Warn("Still no vertical movement detected");
                        return false;
                    }
                }
            }
        }
    }
}
