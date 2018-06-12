using System;
using System.Configuration;
using System.Drawing;
using System.Runtime.InteropServices;
using static System.FormattableString;

namespace Opus
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames")]
    [Flags]
    public enum MouseEvent
    {
        LeftDown = 0x00000002,
        LeftUp = 0x00000004,
        MiddleDown = 0x00000020,
        MiddleUp = 0x00000040,
        Move = 0x00000001,
        Absolute = 0x00008000,
        RightDown = 0x00000008,
        RightUp = 0x00000010
    }

    public static class MouseUtils
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(MouseUtils));

        public static int GlobalDragDelay { get; set; } = 0;

        static MouseUtils()
        {
            if (Int32.TryParse(ConfigurationManager.AppSettings["MouseUtils.GlobalDragDelay"], out int delay))
            {
                GlobalDragDelay = delay;
            }
        }

        public static void SetCursorPosition(Point point)
        {
            if (!NativeMethods.SetCursorPos(point.X, point.Y))
            {
                throw new InvalidOperationException(Invariant($"SetCursorPos failed: error {Marshal.GetLastWin32Error()}."));
            }
        }

        public static Point GetCursorPosition()
        {
            if (!NativeMethods.GetCursorPos(out NativeMethods.MousePoint point))
            {
                throw new InvalidOperationException(Invariant($"GetCursorPos failed: error {Marshal.GetLastWin32Error()}."));
            }
            return new Point(point.X, point.Y);
        }

        public static void SendMouseEvent(MouseEvent flags)
        {
            var point = GetCursorPosition();
            NativeMethods.mouse_event((int)flags, point.X, point.Y, 0, IntPtr.Zero);
        }

        public static void LeftClick(int delay)
        {
            SendMouseEvent(MouseEvent.LeftDown);
            ThreadUtils.SleepOrAbort(delay);
            SendMouseEvent(MouseEvent.LeftUp);
        }

        public static void LeftDrag(Point start, Point end, int delay = 0, bool keepMouseDown = false)
        {
            Drag(MouseEvent.LeftDown, MouseEvent.LeftUp, start, end, delay, keepMouseDown);
        }

        public static void RightDrag(Point start, Point end, int delay = 0, bool keepMouseDown = false)
        {
            Drag(MouseEvent.RightDown, MouseEvent.RightUp, start, end, delay, keepMouseDown);
        }

        public static void Drag(MouseEvent startFlags, MouseEvent endFlags, Point start, Point end, int delay = 0, bool keepMouseDown = false)
        {
            sm_log.Info(Invariant($"Dragging from {start} to {end}; startFlags: {startFlags}; endFlags: {endFlags}"));

            int delayAfterMove = GlobalDragDelay + delay;
            int delayAfterMouse = GlobalDragDelay + delay + 50;

            SetCursorPosition(start);
            ThreadUtils.SleepOrAbort(delayAfterMove);

            SendMouseEvent(startFlags);
            ThreadUtils.SleepOrAbort(delayAfterMouse);

            SetCursorPosition(end);
            ThreadUtils.SleepOrAbort(delayAfterMove);

            if (!keepMouseDown)
            {
                SendMouseEvent(endFlags);
                ThreadUtils.SleepOrAbort(delayAfterMouse);
            }
        }
    }
}
