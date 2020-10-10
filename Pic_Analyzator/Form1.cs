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
using Toub.Sound.Midi;

namespace Pic_Analyzator
{
    public partial class Form1 : Form
    {
        Bitmap _bitmap; // original picture
        List<Pixel> _pixel; // pixels array from original picture
        List<Pixel> _starPixel;
        int _max = int.MinValue; // max brightness of all pixels

        public Form1()
        {
            InitializeComponent();
        }

        // method to get the picture and not to occupy it
        public static Bitmap LoadBitmap(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                return new Bitmap(fs);
        }


        // UNSAFE METHOD
        // add all pixels from _bitmap to array that includes ARGB values
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
                        res[3, h, w] = *(curpos++); // B
                        res[2, h, w] = *(curpos++); // G
                        res[1, h, w] = *(curpos++); // R
                        res[0, h, w] = *(curpos++); // A

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
            if (openFileDialog1.ShowDialog() == DialogResult.OK) // choose picture
            {
                await Task.Run(new Action(() =>
                {
                    pictureBox2.Image = null;
                    pictureBox1.Image = new Bitmap(openFileDialog1.FileName);
                }));
                _bitmap = LoadBitmap(openFileDialog1.FileName);
            }
            else // cancel choosing
            {
                return;
            }

            FillPixelsArray();
            //await Task.Run(new Action(() => PixelParse())); // doesn't work cause it runs not in UI
            await Task.Run(new Action(() => PixelAnalysing()));
        }

        // find pixels which brigthness more then 3/4 of max brigthness
        private void PixelAnalysing()
        {
            _starPixel = new List<Pixel>(_bitmap.Width * _bitmap.Height);
            int startedMax = _max / 4 * 3;
            Bitmap newBitmap = new Bitmap(_bitmap.Width, _bitmap.Height);
            var counter = 0;
            foreach (var pixel in _pixel)
            {
                counter++;
                if ((int)(pixel.color.GetBrightness() * 1000) > startedMax)
                {
                    newBitmap.SetPixel(pixel.point.X, pixel.point.Y, pixel.color);
                    _starPixel.Add(new Pixel() { point = new Point(pixel.point.X, pixel.point.Y), color = pixel.color });
                }
            }
            TextLog("Analyze done");
            pictureBox2.Image = newBitmap;
        }

        // method to add all picture pixels into _pixels array
        private void FillPixelsArray()
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

        // method to log caption
        private void TextLog(string text)
        {
            this.Invoke(new Action(() =>
            {
                this.Text = text;
            }));
        }

        private async void playSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(new Action(() => PlayMusic()));
        }

        private void PlayMusic()
        {
            int W = _bitmap.Width;
            int H = _bitmap.Height;
            Color[,] arr = new Color[W, H];
            foreach (var pixel in _starPixel)
            {
                arr[pixel.point.X, pixel.point.Y] = pixel.color;
            }
            // добавить количество октав как переменную извне + классификацию по нотам внутри октавы и
            // ОБЯЗАТЕЛЬНО написать алгоритм определения звезд и составления списка со звездами

            MidiPlayer.OpenMidi();
            int oct = H / 7;
            for (var w = 0; w < W; w++)
            {
                for (var h = 0; h < H; h++)
                {
                    if (arr[w, h].Name != "0")
                    {
                        int octave = 1;
                        for (var i = 0; i < 7; i++)
                        {
                            if (h > oct * i && h <= oct * (i + 1))
                            {
                                octave = i + 1;
                                break;
                            }
                        }

                        PlaySound(100, $"C{octave}");
                        Thread.Sleep(3000);

                    }
                }
                        Thread.Sleep(100);
            }
        }

        private void PlaySound(byte volume, string note)
        {
            MidiPlayer.Play(new NoteOn(0, 1, note, volume));
        }
    }
}
