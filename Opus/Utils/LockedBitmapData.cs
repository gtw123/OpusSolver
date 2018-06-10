using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Opus
{
    /// <summary>
    /// Provides fast access to the pixels of a bitmap, by locking the pixels on construction and unlocking
    /// when disposed. 
    /// </summary>
    public class LockedBitmapData : IDisposable
    {
        public Bitmap Bitmap { get; private set; }
        public BitmapData Data { get; private set; }
        private bool m_writeable;

        public LockedBitmapData(Bitmap bitmap, bool writeable = false)
        {
            Bitmap = bitmap;
            Data = bitmap.LockBits(new Rectangle(new Point(0, 0), bitmap.Size),
                writeable ? ImageLockMode.ReadWrite : ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            m_writeable = writeable;
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
                Bitmap.UnlockBits(Data);
            }
        }

        public Color GetPixel(int x, int y)
        {
            unsafe
            {
                IntPtr row = Data.Scan0 + Data.Stride * y;
                int* pixel = (int*)row.ToPointer() + x;
                return Color.FromArgb(*pixel);
            }
        }

        public void SetPixel(int x, int y, Color col)
        {
            if (!m_writeable)
            {
                throw new InvalidOperationException("Can't call SetPixel when bitmap was not locked as writeable.");
            }

            unsafe
            {
                IntPtr row = Data.Scan0 + Data.Stride * y;
                int* pixel = (int*)row.ToPointer() + x;
                *pixel = col.ToArgb();
            }
        }
    }
}
