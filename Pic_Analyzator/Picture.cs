using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Pic_Analyzator
{
    class Picture
    {
        // method to get the picture and not to occupy it
        public static Bitmap LoadBitmap(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return new Bitmap(fs);
            }
        }

        // UNSAFE METHOD
        // add all pixels from _bitmap to array that includes ARGB values
        public static unsafe byte[,,] BitmapToByteRgb(Bitmap bmp)
        {
            int width = bmp.Width,
                height = bmp.Height;
            var res = new byte[4, height, width];
            var bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                byte* curpos;
                for (var h = 0; h < height; h++)
                {
                    curpos = (byte*)bd.Scan0 + h * bd.Stride;
                    for (var w = 0; w < width; w++)
                    {
                        res[3, h, w] = *curpos++; // B
                        res[2, h, w] = *curpos++; // G
                        res[1, h, w] = *curpos++; // R
                        res[0, h, w] = *curpos++; // A
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }

            return res;
        }

        public static void OriginBitmapInit(OpenFileDialog openFileDialog)
        {
            var bitmap = Picture.LoadBitmap(openFileDialog.FileName);
            Origin.Bitmap = bitmap;
            Origin.H = bitmap.Height;
            Origin.W = bitmap.Width;
        }
    }
}
