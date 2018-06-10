using System;
using System.Drawing;

namespace Opus.UI.Analysis
{
    public class PaletteInfo : IDisposable
    {
        public ScreenCapture Capture { get; set; }
        public Point ScrollPosition { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Capture.Dispose();
            }
        }
    }
}
