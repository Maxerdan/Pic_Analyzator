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
        Bitmap newBitmap;
        List<Pixel> _starPixel;
        int _max; // max brightness of all pixels
        int _min; // min brightness level
        Color[,] arr; // array contains analyzed pixel (star pixel)
        bool _stopPlay = false;

        List<List<Pixel>> stars;

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

        // menu OPEN button
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
            await Task.Run(new Action(() => PixelAnalysing()));
            await Task.Run(new Action(() => FillArrayAndFindMin()));
        }

        // find pixels which brigthness more then 3/4 of max brigthness
        private void PixelAnalysing()
        {
            _starPixel = new List<Pixel>(_bitmap.Width * _bitmap.Height);
            int startedMax = _max / 4 * 3;
            newBitmap = new Bitmap(_bitmap.Width, _bitmap.Height);
            foreach (var pixel in _pixel)
            {
                if ((int)(pixel.Color.GetBrightness() * 1000) > startedMax)
                {
                    newBitmap.SetPixel(pixel.Point.X, pixel.Point.Y, pixel.Color);
                    _starPixel.Add(new Pixel() { Point = new Point(pixel.Point.X, pixel.Point.Y), Color = pixel.Color });
                }
            }
            TextLog("Analyze done");
            pictureBox2.Image = newBitmap;
        }

        // method to add all picture pixels into _pixels array
        private void FillPixelsArray()
        {
            _max = int.MinValue;
            _pixel = new List<Pixel>(_bitmap.Width * _bitmap.Height);
            var arr = BitmapToByteRgb(_bitmap);
            for (var y = 0; y < _bitmap.Height; y++)
            {
                for (var x = 0; x < _bitmap.Width; x++)
                {
                    var color = Color.FromArgb(arr[0, y, x], arr[1, y, x], arr[2, y, x], arr[3, y, x]);
                    _pixel.Add(new Pixel() { Point = new Point(x, y), Color = color });
                    if ((int)(color.GetBrightness() * 1000) > _max)
                        _max = (int)(color.GetBrightness() * 1000);
                }
            }
            TextLog("PixelParse done");
        }

        private void FillArrayAndFindMin()
        {
            _min = int.MaxValue;
            arr = new Color[_bitmap.Width, _bitmap.Height]; // array contains analyzed pixel (star pixel)
            foreach (var pixel in _starPixel) // method to fill array and find min
            {
                arr[pixel.Point.X, pixel.Point.Y] = pixel.Color;
                if ((int)(pixel.Color.GetBrightness() * 1000) < _min)
                    _min = (int)(pixel.Color.GetBrightness() * 1000);
            }
        }

        private void FindStars()
        {
            stars = new List<List<Pixel>>();
            int W = _bitmap.Width;
            int H = _bitmap.Height;
            var visitedNodes = new int[W, H];
            for (var w = 0; w < W; w++)
            {
                for (var h = 0; h < H; h++)
                {
                    List<Pixel> aloneStarPixels = new List<Pixel>();
                    var queue = new Queue<Pixel>();
                    queue.Enqueue(new Pixel() { Point = new Point(w, h), Color = arr[w, h] });
                    visitedNodes[w, h] = 1; // заменить на структуру которая считает 1 как true для сравнения
                    while (queue.Count != 0)
                    {
                        var node = queue.Dequeue();
                        var x = node.Point.X;
                        var y = node.Point.Y;

                        if (arr[x, y].Name != "0")
                        {
                            aloneStarPixels.Add(new Pixel() { Point = new Point(x, y), Color = arr[x, y] });
                            if (y - 1 >= 0)
                                if (visitedNodes[x, y - 1] == 0) //todo
                                {
                                    queue.Enqueue(new Pixel() { Point = new Point(x, y - 1), Color = arr[x, y - 1] });
                                    visitedNodes[x, y - 1] = 1; // todo
                                }
                            if (y + 1 < H)
                                if (visitedNodes[x, y + 1] == 0) // todo
                                {
                                    queue.Enqueue(new Pixel() { Point = new Point(x, y + 1), Color = arr[x, y + 1] });
                                    visitedNodes[x, y + 1] = 1; // todo
                                }
                            if (x - 1 >= 0)
                                if (visitedNodes[x - 1, y] == 0)// todo
                                {
                                    queue.Enqueue(new Pixel() { Point = new Point(x - 1, y), Color = arr[x - 1, y] });
                                    visitedNodes[x - 1, y] = 1; // todo
                                }
                            if (x + 1 < W)
                                if (visitedNodes[x + 1, y] == 0) // todo
                                {
                                    queue.Enqueue(new Pixel() { Point = new Point(x + 1, y), Color = arr[x + 1, y] });
                                    visitedNodes[x + 1, y] = 1; // todo
                                }
                        }
                    }
                    if (aloneStarPixels.Count != 0 && aloneStarPixels.Count != 1)
                        stars.Add(aloneStarPixels);
                }
            }
        }

        private void ColorizeStars()
        {
            var bitmap = new Bitmap(_bitmap.Width, _bitmap.Height);
            foreach(var arrays in stars)
            {
                foreach(var pixels in arrays)
                {
                    bitmap.SetPixel(pixels.Point.X, pixels.Point.Y, pixels.Color);
                }
            }

            pictureBox1.Image = bitmap;
        }

        // method to log caption
        private void TextLog(string text)
        {
            this.Invoke(new Action(() =>
            {
                this.Text = text;
            }));
        }

        // menu PLAY SOUND button
        private async void playSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(new Action(() => PlayMusic()));
        }

        // method to play music
        private void PlayMusic()
        {
            _stopPlay = false;

            int W = _bitmap.Width;
            int H = _bitmap.Height;

            MidiPlayer.OpenMidi();
            bool starFlag = true;
            int oct = (_max - _min) / 6; // 6 - max num of octaves

            Bitmap redColumn;

            for (var w = 0; w < W; w++)
            {
                redColumn = new Bitmap(newBitmap);
                for (var i = 0; i < H; i++)
                {
                    redColumn.SetPixel(w, i, Color.IndianRed);
                }
                pictureBox2.Image = redColumn;
                for (var h = 0; h < H; h++)
                {
                    if (!_stopPlay)
                    {
                        if (arr[w, h].Name != "0" && starFlag)
                        {
                            var brightLevel = (int)(arr[w, h].GetBrightness() * 1000); // get bright level form pixel
                            int octave = 3; // stock octave
                            for (var i = 2; i < 6; i++)
                            {
                                if (brightLevel > _min + oct * i && brightLevel <= _min + oct * (i + 1)) // find octave num
                                {
                                    octave = i + 1;
                                    break;
                                }
                            }

                            starFlag = false;
                            int oct7 = oct / 7; // find button num in octave
                            var t = _min + (oct * (octave - 1));
                            if (brightLevel > t + oct7 && brightLevel <= t + oct7 * 2)
                                PlaySound(100, $"C{octave}");
                            else if (brightLevel > t + oct7 * 2 && brightLevel <= t + oct7 * 3)
                                PlaySound(100, $"D{octave}");
                            else if (brightLevel > t + oct7 * 3 && brightLevel <= t + oct7 * 4)
                                PlaySound(100, $"E{octave}");
                            else if (brightLevel > t + oct7 * 4 && brightLevel <= t + oct7 * 5)
                                PlaySound(100, $"F{octave}");
                            else if (brightLevel > t + oct7 * 5 && brightLevel <= t + oct7 * 6)
                                PlaySound(100, $"G{octave}");
                            else if (brightLevel > t + oct7 * 6 && brightLevel <= t + oct7 * 7)
                                PlaySound(100, $"A{octave}");
                            else if (brightLevel > t + oct7 * 7 && brightLevel <= t + oct7 * 8)
                                PlaySound(100, $"B{octave}");
                            this.Invoke(new Action(() => // delay between piano button push
                            {
                                Thread.Sleep(trackBar1.Value);
                            }));
                        }
                        if (arr[w, h].Name == "0")
                            starFlag = true;
                    }
                }
                Thread.Sleep(10); // delay between pixel column go
            }
            pictureBox2.Image = newBitmap;
        }

        private void PlaySound(byte volume, string note)
        {
            MidiPlayer.Play(new NoteOn(0, 1, note, volume));
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _stopPlay = true;
        }

        private void findStarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FindStars();
            ColorizeStars();
        }
    }
}
