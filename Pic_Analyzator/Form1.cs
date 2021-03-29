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

        public Form1()
        {
            InitializeComponent();
            soundType.DataSource = Enum.GetValues(typeof(GeneralMidiInstruments));
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
            await Task.Run(() => TakeNebulaPixels());
        }

        // method to add all picture pixels into array and find Max
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
            TextLog("Find Max");
        }

        // find pixels which brigthness more then 3/4 of max brigthness
        private void TakeStarPixels()
        {
            var startedMax = Origin.Max / 4 * 3;

            var bitmap = new Bitmap(Origin.W, Origin.H);
            foreach (var pixel in Origin.Pixels)
                if ((int)(pixel.Color.GetBrightness() * 1000) > startedMax)
                {
                    bitmap.SetPixel(pixel.Point.X, pixel.Point.Y, pixel.Color);
                }

            Stars.Bitmap = bitmap;
            TextLog("NewBitmap 3/4 of max brightness");

            FindStars();
            FindMin();
        }

        private void TakeNebulaPixels()
        {
            var nebulaPixels = new List<Pixel>(Origin.W * Origin.H);
            var upperBound = Origin.Max / 4 * 2;
            var lowerBound = Origin.Max / 8 * 1;

            var bitmap = new Bitmap(Origin.W, Origin.H);
            foreach (var pixel in Origin.Pixels)
                if ((int)(pixel.Color.GetBrightness() * 1000) > lowerBound && (int)(pixel.Color.GetBrightness() * 1000) < upperBound)
                {
                    bitmap.SetPixel(pixel.Point.X, pixel.Point.Y, pixel.Color);
                    nebulaPixels.Add(new Pixel { Point = new Point(pixel.Point.X, pixel.Point.Y), Color = pixel.Color });
                }

            Nebula.Pixels = nebulaPixels;
            Nebula.Bitmap = bitmap;
            TextLog("Nebula Done");
        }

        private void FindMin()
        {
            var min = int.MaxValue;
            foreach (var star in Stars.ListOfStars) // method to fill array and find min
                foreach (var pixel in star.StarPixels)
                {
                    if ((int)(pixel.Color.GetBrightness() * 1000) < min)
                        min = (int)(pixel.Color.GetBrightness() * 1000);
                }

            Origin.Min = min;
        }

        private void FindStars()
        {
            Stars.ListOfStars = new List<OneStar>();
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
                        Stars.ListOfStars.Add(new OneStar()
                        {
                            StarPixels = aloneStarPixels,
                            StarCenter = TakeStarCenter(aloneStarPixels),
                            AverageBrightness = TakeAverageBrightness(aloneStarPixels)
                        });
                }

            //load list of pixels to bitmap
            var bitmap = new Bitmap(Origin.W, Origin.H);
            foreach (var oneStar in Stars.ListOfStars)
                foreach (var pixels in oneStar.StarPixels)
                    bitmap.SetPixel(pixels.Point.X, pixels.Point.Y, pixels.Color);

            Stars.Bitmap = bitmap;
            pictureBox2.Image = bitmap;


            FindMaxAndMinAverageBrightness();
            TextLog("Stars Done");
        }

        private void FindMaxAndMinAverageBrightness()
        {
            var min = int.MaxValue;
            var max = int.MinValue;
            foreach (var star in Stars.ListOfStars)
            {
                if (star.AverageBrightness > max)
                    max = star.AverageBrightness;
                if (star.AverageBrightness < min)
                    min = star.AverageBrightness;
            }

            Stars.MaxBrightness = max;
            Stars.MinBrightness = min;
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

        // old method to play music
        private void PlayMusic()
        {
            _stopPlay = false;
            var speedValue = 100;

            MidiPlayer.OpenMidi();

            Invoke(new Action(() =>
            {
                MidiPlayer.Play(new ProgramChange(0, 1, (GeneralMidiInstruments)soundType.SelectedItem));
            }));
            Bitmap redColumn;

            for (var w = 0; w < Origin.W; w++)
            {
                if (_stopPlay)
                {
                    string[] allButtons = { "A3", "B3", "C3", "D3", "E3", "F3", "G3", "A4", "B4", "C4", "D4", "E4", "F4", "G4", };
                    foreach (var button in allButtons)
                    {
                        MidiPlayer.Play(new NoteOff(0, 1, button, 100));
                    }
                    break;
                }

                redColumn = new Bitmap(Stars.Bitmap);
                for (var i = 0; i < Origin.H; i++)
                    redColumn.SetPixel(w, i, Color.IndianRed);
                pictureBox2.Image = redColumn;

                var octaveLengthInPixels = Origin.H / 2; // 2 - octaves number
                var starsOnLine = Stars.ListOfStars.FindAll(x => x.StarCenter.X == w);
                foreach (var star in starsOnLine)
                {
                    int octave = 3; // stock octave
                    if (star.StarCenter.Y < octaveLengthInPixels)
                        octave++;

                    var buttonLength = octaveLengthInPixels / 7;
                    var octSumLength = Math.Abs(octave - 4) * octaveLengthInPixels;
                    var button = "";
                    if (star.StarCenter.Y >= octSumLength && star.StarCenter.Y <= octSumLength + buttonLength)
                        button = $"B{octave}";
                    else if (star.StarCenter.Y > octSumLength + buttonLength && star.StarCenter.Y <= octSumLength + buttonLength * 2)
                        button = $"A{octave}";
                    else if (star.StarCenter.Y > octSumLength + buttonLength * 2 && star.StarCenter.Y <= octSumLength + buttonLength * 3)
                        button = $"G{octave}";
                    else if (star.StarCenter.Y > octSumLength + buttonLength * 3 && star.StarCenter.Y <= octSumLength + buttonLength * 4)
                        button = $"F{octave}";
                    else if (star.StarCenter.Y > octSumLength + buttonLength * 4 && star.StarCenter.Y <= octSumLength + buttonLength * 5)
                        button = $"E{octave}";
                    else if (star.StarCenter.Y > octSumLength + buttonLength * 5 && star.StarCenter.Y <= octSumLength + buttonLength * 6)
                        button = $"D{octave}";
                    else if (star.StarCenter.Y > octSumLength + buttonLength * 6 && star.StarCenter.Y <= octSumLength + buttonLength * 7)
                        button = $"C{octave}";

                    if (button == "")
                    {
                        MessageBox.Show($"{star.StarCenter.Y} {octSumLength} {octSumLength + buttonLength * 7}");
                    }

                    var volume = 0;
                    var intervalNumber = 20;
                    var intervalMin = 60;
                    var intervalMax = 120;
                    var intervalLength = (intervalMax - intervalMin) / intervalNumber;
                    double brightnessIntervalLength = (Stars.MaxBrightness - Stars.MinBrightness) / (double)intervalNumber;
                    for (var i = 0; i < intervalNumber; i++)
                    {
                        if (star.AverageBrightness >= Stars.MinBrightness + brightnessIntervalLength * i && star.AverageBrightness <= Stars.MinBrightness + brightnessIntervalLength * (i + 1))
                            volume = intervalMin + intervalLength * i;
                        if (star.AverageBrightness == Stars.MaxBrightness)
                            volume = intervalMax;
                    }



                    PlaySound((byte)volume, button);
                    Invoke(new Action(() => // delay between piano button push
                    {
                        Thread.Sleep(speedValue);
                    }));
                }


                Thread.Sleep(int.Parse(columnSpeed.Text)); // delay between pixel column go
            }

            pictureBox2.Image = Stars.Bitmap;
        }

        private Point TakeStarCenter(List<Pixel> starPixels)
        {
            var maxX = int.MinValue;
            var minX = int.MaxValue;
            var maxY = int.MinValue;
            var minY = int.MaxValue;
            int centerX;
            int centerY;

            foreach (var pixel in starPixels)
            {
                if (pixel.Point.X > maxX)
                    maxX = pixel.Point.X;
                if (pixel.Point.X < minX)
                    minX = pixel.Point.X;
                if (pixel.Point.Y > maxY)
                    maxY = pixel.Point.Y;
                if (pixel.Point.Y < minY)
                    minY = pixel.Point.Y;
            }

            if (maxX == minX)
                centerX = maxX;
            else
            {
                centerX = ((maxX - minX) / 2) + minX;
            }

            if (maxY == minY)
                centerY = maxY;
            else
            {
                centerY = ((maxY - minY) / 2) + minY;
            }

            return new Point(centerX, centerY);
        }

        // old method to play music
        private void PlayMusicOld()
        {
            _stopPlay = false;
            var speedValue = 100;

            MidiPlayer.OpenMidi();
            MidiPlayer.Play(new ProgramChange(0, 1, GeneralMidiInstruments.SciFi));
            var oct = (Origin.Max - Origin.Min) / 6; // 6 - max num of octaves

            Bitmap redColumn;

            for (var w = 0; w < Origin.W; w++)
            {
                if (_stopPlay)
                {
                    break;
                }

                redColumn = new Bitmap(Stars.Bitmap);
                for (var i = 0; i < Origin.H; i++) redColumn.SetPixel(w, i, Color.IndianRed);
                pictureBox2.Image = redColumn;
                for (var h = 0; h < Origin.H; h++)
                    if (GetColor(w, h).Name != "0")
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
                            Thread.Sleep(speedValue);
                        }));
                    }

                Thread.Sleep(10); // delay between pixel column go
            }

            pictureBox2.Image = Stars.Bitmap;
            MidiPlayer.CloseMidi();
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
            pictureBox2.Image = Stars.Bitmap;
        }

        void OriginBitmapInit(string fileName)
        {
            var bitmap = LoadBitmap(openFileDialog1.FileName);
            Origin.Bitmap = bitmap;
            Origin.H = bitmap.Height;
            Origin.W = bitmap.Width;
        }

        private Color GetColor(int x, int y)
        {
            return Stars.Bitmap.GetPixel(x, y);
        }

        private void findNebulaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = Nebula.Bitmap;
        }

        private void showOriginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //pictureBox1.Image = Origin.Bitmap;
            var bitmap = new Bitmap(Origin.W, Origin.H);
            foreach (var star in Stars.ListOfStars)
            {
                bitmap.SetPixel(star.StarCenter.X, star.StarCenter.Y, Color.IndianRed);
            }
            pictureBox1.Image = bitmap;
        }

        private int TakeAverageBrightness(List<Pixel> starPixels)
        {
            var brightnessSum = 0;
            foreach (var pixel in starPixels)
            {
                brightnessSum += (int)(pixel.Color.GetBrightness() * 1000);
            }

            return brightnessSum / starPixels.Count;
        }
    }
}