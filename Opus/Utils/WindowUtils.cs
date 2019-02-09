using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using static System.FormattableString;

namespace Opus
{
    public static class WindowUtils
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(WindowUtils));

        public static Rectangle GetWindowScreenRect(IntPtr window)
        {
            if (!NativeMethods.GetClientRect(window, out NativeMethods.RECT rect))
            {
                throw new InvalidOperationException(Invariant($"GetClientRect: error {Marshal.GetLastWin32Error()}"));
            }

            sm_log.Info(Invariant($"GetClientRect: {rect}"));

            var topLeft = new NativeMethods.POINT { x = rect.Left, y = rect.Top };
            if (!NativeMethods.ClientToScreen(window, ref topLeft))
            {
                throw new InvalidOperationException(Invariant($"ClientToScreen: error {Marshal.GetLastWin32Error()}"));
            }

            sm_log.Info(Invariant($"ClientToScreen(topLeft): {topLeft.x}, {topLeft.y}"));

            var bottomRight = new NativeMethods.POINT { x = rect.Right, y = rect.Bottom };
            if (!NativeMethods.ClientToScreen(window, ref bottomRight))
            {
                throw new InvalidOperationException(Invariant($"ClientToScreen: error {Marshal.GetLastWin32Error()}"));
            }

            sm_log.Info(Invariant($"ClientToScreen(bottomRight): {bottomRight.x}, {bottomRight.y}"));

            return new Rectangle(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
        }

        public static bool IsWindowCompletelyVisible(IntPtr window)
        {
            if (NativeMethods.IsIconic(window))
            {
                sm_log.Info("Window is minimized");
                return false;
            }

            if (IsOverlapped(window))
            {
                sm_log.Info("Another window overlaps the window");
                return false;
            }

            if (!IsOnScreen(window))
            {
                sm_log.Info("The window is not completely on the screen");
                return false;
            }

            return true;
        }

        private static bool IsOverlapped(IntPtr window)
        {
            var visited = new HashSet<IntPtr> { window };

            NativeMethods.GetWindowRect(window, out var windowRect);
            sm_log.Info(Invariant($"GetWindowRect: {windowRect}"));

            while ((window = NativeMethods.GetWindow(window, NativeMethods.GetWindowCommand.GW_HWNDPREV)) != IntPtr.Zero && !visited.Contains(window))
            {
                visited.Add(window);
                NativeMethods.RECT testRect, intersection;
                if (NativeMethods.IsWindowVisible(window) && NativeMethods.GetWindowRect(window, out testRect) && NativeMethods.IntersectRect(out intersection, ref windowRect, ref testRect))
                {
                    if (testRect.Bottom - testRect.Top <= 1 && testRect.Right - testRect.Left <= 1)
                    {
                        sm_log.Info(Invariant($"Ignoring overlapping window \"{GetWindowName(window)}\" because its rect is trivially small: {testRect}"));
                    }
                    else
                    {
                        sm_log.Info(Invariant($"Found overlapping window \"{GetWindowName(window)}\": {testRect}"));
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsOnScreen(IntPtr window)
        {
            var rect = GetWindowScreenRect(window);

            // Check if each of the corners of the window is on a monitor. Technically this doesn't guarantee the whole window
            // is on a monitor but it's good enough for most cases.
            return IsPointOnScreen(rect.Left, rect.Top) && IsPointOnScreen(rect.Right - 1, rect.Top) &&
                IsPointOnScreen(rect.Left, rect.Bottom - 1) && IsPointOnScreen(rect.Right - 1, rect.Bottom - 1);

            bool IsPointOnScreen(int x, int y) => NativeMethods.MonitorFromPoint(new NativeMethods.POINT { x = x, y = y },
                NativeMethods.MonitorOptions.MONITOR_DEFAULTTONULL) != IntPtr.Zero;
        }

        private static string GetWindowName(IntPtr window)
        {
            int length = NativeMethods.GetWindowTextLength(window);
            if (length == 0)
            {
                sm_log.Warn(Invariant($"GetWindowTextLength: error {Marshal.GetLastWin32Error()}"));
                return String.Empty;
            }

            var builder = new StringBuilder(length + 1);
            if (NativeMethods.GetWindowText(window, builder, builder.Capacity) == 0)
            {
                sm_log.Warn(Invariant($"GetWindowText: error {Marshal.GetLastWin32Error()}"));
                return String.Empty;
            }

            return builder.ToString();
        }
    }
}
