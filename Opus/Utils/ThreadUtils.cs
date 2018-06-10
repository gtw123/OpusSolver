using System.Threading;
using System.Windows.Forms;

namespace Opus
{
    public static class ThreadUtils
    {
        public static void SleepOrAbort(int milliseconds)
        {
            Thread.Sleep(milliseconds);
            if (KeyboardUtils.IsKeyDown(Keys.Escape))
            {
                MouseUtils.SendMouseEvent(MouseEvent.LeftUp);
                MouseUtils.SendMouseEvent(MouseEvent.RightUp);
                KeyboardUtils.ClearKeysDown();
                throw new AbortException();
            }
        }
    }
}
