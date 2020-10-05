using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using System.Drawing.Imaging;

namespace Pic_Analyzator
{
    public partial class Form1 : Form
    {
        Bitmap _bitmap;
        List<Pixel> _pixel;
        int _max = int.MinValue;

        public Form1()
        {
            InitializeComponent();
        }

        public static Bitmap LoadBitmap(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return new Bitmap(fs);
        }

        public unsafe static byte[,,] BitmapToByteRgb(Bitmap bmp)
        {
            int width = bmp.Width,
                height = bmp.Height;
            byte[,,] res = new byte[4, height, width];
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                byte* curpos;
                for (int h = 0; h < height; h++)
                {
                    curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                    for (int w = 0; w < width; w++)
                    {
                        res[3, h, w] = *(curpos++);
                        res[2, h, w] = *(curpos++);
                        res[1, h, w] = *(curpos++);
                        res[0, h, w] = *(curpos++);

                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
            return res;
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                await Task.Run(new Action(() =>
                {
                    pictureBox2.Image = null;
                    pictureBox1.Image = new Bitmap(openFileDialog1.FileName);
                }));
                _bitmap = LoadBitmap(openFileDialog1.FileName);
            }
            else
            {
                return;
            }

            PixelParse();
            //await Task.Run(new Action(() => PixelParse()));
            await Task.Run(new Action(() => PixelAnalysing()));
        }

        private void PixelAnalysing()
        {
            int startedMax = _max / 4 * 3;
            Bitmap newBitmap = new Bitmap(_bitmap.Width, _bitmap.Height);
            var counter = 0;
            foreach (var pixel in _pixel)
            {
                counter++;
                if ((int)(pixel.color.GetBrightness() * 1000) > startedMax)
                    newBitmap.SetPixel(pixel.point.X, pixel.point.Y, pixel.color);
            }
            TextLog("Analyzee done");
            pictureBox2.Image = newBitmap;
        }

        private void PixelParse()
        {
            _pixel = new List<Pixel>(_bitmap.Width * _bitmap.Height);
            var arr = BitmapToByteRgb(_bitmap);
            for (var y = 0; y < _bitmap.Height; y++)
            {
                for (var x = 0; x < _bitmap.Width; x++)
                {
                    var color = Color.FromArgb(arr[0, y, x], arr[1, y, x], arr[2, y, x], arr[3, y, x]);
                    _pixel.Add(new Pixel() { point = new Point(x, y), color = color });
                    if ((int)(color.GetBrightness() * 1000) > _max)
                        _max = (int)(color.GetBrightness() * 1000);
                }
            }
            TextLog("PixelParse done");
        }

        private void TextLog(string text)
        {
            this.Invoke(new Action(() =>
            {
                this.Text = text;
            }));
        }
    }
}
