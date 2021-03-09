using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Toub.Sound.Midi;

namespace Pic_Analyzator
{
    public partial class Form1 : Form
    {
        private bool _stopPlay;

        private List<List<Pixel>> stars;

        public Form1()
        {
            InitializeComponent();
        }

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

        // menu OPEN button
        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) // choose picture
            {
                await Task.Run(() =>
                {
                    pictureBox2.Image = null;
                    pictureBox1.Image = new Bitmap(openFileDialog1.FileName);
                });
                OriginBitmapInit(openFileDialog1.FileName);
            }
            else // cancel choosing
            {
                return;
            }

            FillPixelsArray();
            await Task.Run(() => TakeStarPixels());
            await Task.Run(() => FindMin());
        }

        // method to add all picture pixels into _pixels array
        private void FillPixelsArray()
        {
            var max = int.MinValue;
            Origin.Pixels = new List<Pixel>(Origin.W * Origin.H);
            var arr = BitmapToByteRgb(Origin.Bitmap);
            for (var y = 0; y < Origin.H; y++)
                for (var x = 0; x < Origin.W; x++)
                {
                    var color = Color.FromArgb(arr[0, y, x], arr[1, y, x], arr[2, y, x], arr[3, y, x]);
                    Origin.Pixels.Add(new Pixel { Point = new Point(x, y), Color = color });
                    if ((int)(color.GetBrightness() * 1000) > max)
                        max = (int)(color.GetBrightness() * 1000);
                }

            Origin.Max = max;
            TextLog("PixelParse done");
        }

        // find pixels which brigthness more then 3/4 of max brigthness
        private void TakeStarPixels()
        {
            var starPixels = new List<Pixel>(Origin.W * Origin.H);
            var startedMax = Origin.Max / 4 * 3;

            var bitmap = new Bitmap(Origin.W, Origin.H);
            foreach (var pixel in Origin.Pixels)
                if ((int)(pixel.Color.GetBrightness() * 1000) > startedMax)
                {
                    bitmap.SetPixel(pixel.Point.X, pixel.Point.Y, pixel.Color);
                    starPixels.Add(new Pixel { Point = new Point(pixel.Point.X, pixel.Point.Y), Color = pixel.Color });
                }

            Stars.Pixels = starPixels;
            pictureBox2.Image = bitmap;
            Stars.Bitmap = bitmap;
            TextLog("Analyze done");
        }

        private void FindMin()
        {
            var min = int.MaxValue;
            foreach (var pixel in Stars.Pixels) // method to fill array and find min
            {
                if ((int)(pixel.Color.GetBrightness() * 1000) < min)
                    min = (int)(pixel.Color.GetBrightness() * 1000);
            }

            Origin.Min = min;
        }

        private Color GetColor(int x, int y)
        {
            //return Stars.Pixels.Find(m => m.Point == new Point(x, y)).Color;
            return Stars.Bitmap.GetPixel(x, y);
        }

        private void FindStars()
        {
            stars = new List<List<Pixel>>();
            var visitedNodes = new int[Origin.W, Origin.H];

            for (var w = 0; w < Origin.W; w++)
                for (var h = 0; h < Origin.H; h++)
                {
                    var aloneStarPixels = new List<Pixel>();
                    var queue = new Queue<Pixel>();
                    queue.Enqueue(new Pixel { Point = new Point(w, h), Color = GetColor(w, h) });
                    visitedNodes[w, h] = 1; // заменить на структуру которая считает 1 как true для сравнения
                    while (queue.Count != 0)
                    {
                        var node = queue.Dequeue();
                        var x = node.Point.X;
                        var y = node.Point.Y;

                        if (GetColor(x, y).Name != "0")
                        {
                            aloneStarPixels.Add(new Pixel { Point = new Point(x, y), Color = GetColor(x, y) });
                            if (y - 1 >= 0)
                                if (visitedNodes[x, y - 1] == 0) //todo
                                {
                                    queue.Enqueue(new Pixel { Point = new Point(x, y - 1), Color = GetColor(x, y - 1) });
                                    visitedNodes[x, y - 1] = 1; // todo
                                }

                            if (y + 1 < Origin.H)
                                if (visitedNodes[x, y + 1] == 0) // todo
                                {
                                    queue.Enqueue(new Pixel { Point = new Point(x, y + 1), Color = GetColor(x, y + 1) });
                                    visitedNodes[x, y + 1] = 1; // todo
                                }

                            if (x - 1 >= 0)
                                if (visitedNodes[x - 1, y] == 0) // todo
                                {
                                    queue.Enqueue(new Pixel { Point = new Point(x - 1, y), Color = GetColor(x - 1, y) });
                                    visitedNodes[x - 1, y] = 1; // todo
                                }

                            if (x + 1 < Origin.W)
                                if (visitedNodes[x + 1, y] == 0) // todo
                                {
                                    queue.Enqueue(new Pixel { Point = new Point(x + 1, y), Color = GetColor(x + 1, y) });
                                    visitedNodes[x + 1, y] = 1; // todo
                                }
                        }
                    }

                    if (aloneStarPixels.Count != 0 && aloneStarPixels.Count != 1)
                        stars.Add(aloneStarPixels);
                }
        }

        private void ColorizeStars()
        {
            var bitmap = new Bitmap(Origin.W, Origin.H);
            foreach (var arrays in stars)
                foreach (var pixels in arrays)
                    bitmap.SetPixel(pixels.Point.X, pixels.Point.Y, pixels.Color);

            pictureBox1.Image = bitmap;
        }

        // method to log caption
        private void TextLog(string text)
        {
            Invoke(new Action(() => { Text = text; }));
        }

        // menu PLAY SOUND button
        private async void playSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() => PlayMusic());
        }

        // method to play music
        private void PlayMusic()
        {
            _stopPlay = false;

            MidiPlayer.OpenMidi();
            var starFlag = true;
            var oct = (Origin.Max - Origin.Min) / 6; // 6 - max num of octaves

            Bitmap redColumn;

            for (var w = 0; w < Origin.W; w++)
            {
                redColumn = new Bitmap(Stars.Bitmap);
                for (var i = 0; i < Origin.H; i++) redColumn.SetPixel(w, i, Color.IndianRed);
                pictureBox2.Image = redColumn;
                for (var h = 0; h < Origin.H; h++)
                    if (!_stopPlay)
                    {
                        if (GetColor(w, h).Name != "0" && starFlag)
                        {
                            var brightLevel = (int)(GetColor(w, h).GetBrightness() * 1000); // get bright level form pixel
                            var octave = 3; // stock octave
                            for (var i = 2; i < 6; i++)
                                if (brightLevel > Origin.Min + oct * i && brightLevel <= Origin.Min + oct * (i + 1)
                                ) // find octave num
                                {
                                    octave = i + 1;
                                    break;
                                }

                            starFlag = false;
                            var oct7 = oct / 7; // find button num in octave
                            var t = Origin.Min + oct * (octave - 1);
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
                            Invoke(new Action(() => // delay between piano button push
                            {
                                Thread.Sleep(trackBar1.Value);
                            }));
                        }

                        if (GetColor(w, h).Name == "0")
                            starFlag = true;
                    }

                Thread.Sleep(10); // delay between pixel column go
            }

            pictureBox2.Image = Stars.Bitmap;
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

        void OriginBitmapInit(string fileName)
        {
            var bitmap = LoadBitmap(openFileDialog1.FileName);
            Origin.Bitmap = bitmap;
            Origin.H = bitmap.Height;
            Origin.W = bitmap.Width;
        }
    }
}