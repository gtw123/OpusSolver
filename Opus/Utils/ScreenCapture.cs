using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using static System.FormattableString;

namespace Opus
{
    public class ScreenCapture : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ScreenCapture));

        private static int sm_logCount = 0;

        /// <summary>
        /// Controls whether screen captures are logged to disk for diagnostic purposes.
        /// </summary>
        public static bool LoggingEnabled { get; set; }

        /// <summary>
        /// The captured image.
        /// </summary>
        public Bitmap Bitmap { get; private set; }

        /// <summary>
        /// Original screen location that the capture was taken from.
        /// </summary>
        public Rectangle Rect { get; private set; }

        public static Rectangle ScreenBounds { get; set; }

        static ScreenCapture()
        {
            if (bool.TryParse(ConfigurationManager.AppSettings["ScreenCapture.LoggingEnabled"], out bool enabled))
            {
                LoggingEnabled = enabled;
            }
        }

        public ScreenCapture()
            : this(ScreenBounds)
        {
        }

        public ScreenCapture(Rectangle rect)
        {
            sm_log.Info(Invariant($"Creating bitmap with rect {rect}"));
            Bitmap = new Bitmap(rect.Width, rect.Height);
            Rect = rect;

            using (Graphics g = Graphics.FromImage(Bitmap))
            {
                g.CopyFromScreen(rect.X, rect.Y,
                                 0, 0, Bitmap.Size, CopyPixelOperation.SourceCopy);
            }

            Save();
        }

        public ScreenCapture(Bitmap bitmap, Rectangle rect, bool save = true)
        {
            Bitmap = bitmap;
            Rect = rect;

            if (save)
            {
                Save();
            }
        }

        public ScreenCapture Clone()
        {
            return Clone(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), save: false);
        }

        public ScreenCapture Clone(Rectangle rect, bool save = true)
        {
            sm_log.Info(Invariant($"Cloning rect {rect} from bitmap of size {Bitmap.Size}"));
            var bitmap = Bitmap.Clone(rect, PixelFormat.Format32bppArgb);
            return new ScreenCapture(bitmap, new Rectangle(Rect.Location.Add(rect.Location), rect.Size), save: save);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Bitmap.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void Save()
        {
            if (LoggingEnabled)
            {
                string dir = "screenshots";
                Directory.CreateDirectory(dir);

                string filename = Invariant($"{dir}/{sm_logCount}.png");
                sm_log.Info(Invariant($"Saving screenshot from {Rect} to {filename}"));
                try
                {
                    Bitmap.Save(filename, ImageFormat.Png);
                }
                catch (Exception e)
                {
                    sm_log.Error(Invariant($"Error saving screenshot to {filename}: {e.Message}"));
                }

                sm_logCount++;
            }
        }
    }
}
