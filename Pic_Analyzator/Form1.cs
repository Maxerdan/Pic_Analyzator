using System;
using System.Linq;
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
        private Logger logger;

        public Form1()
        {
            InitializeComponent();
            soundType.DataSource = Enum.GetValues(typeof(GeneralMidiInstruments));
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
                logger = new Logger(this);
                Picture.OriginBitmapInit(openFileDialog1);
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
            var arr = Picture.BitmapToByteRgb(Origin.Bitmap);
            for (var y = 0; y < Origin.H; y++)
                for (var x = 0; x < Origin.W; x++)
                {
                    var color = Color.FromArgb(arr[0, y, x], arr[1, y, x], arr[2, y, x], arr[3, y, x]);
                    Origin.Pixels.Add(new Pixel { Point = new Point(x, y), Color = color });
                    if ((int)(color.GetBrightness() * 1000) > max)
                        max = (int)(color.GetBrightness() * 1000);
                }

            Origin.Max = max;
            logger.Log("Find Max");
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
            logger.Log("NewBitmap 3/4 of max brightness");

            Stars.FindStars();
            pictureBox2.Image = Stars.Bitmap;
            logger.Log("Stars Done");
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
            logger.Log("Nebula Done");

            Nebula.NebulaAverageBrightness = new Dictionary<int, int>();
            var xs = Nebula.Pixels.GroupBy(x => x.Point.X).Select(x => x.Key);
            foreach (var x in xs)
            {
                var columnPixels = Nebula.Pixels.FindAll(el => el.Point.X == x);

                double brightness = 0;
                foreach (var pixel in columnPixels)
                {
                    brightness += pixel.Color.GetBrightness() * 1000;
                }

                Nebula.NebulaAverageBrightness.Add(x, (int)brightness / columnPixels.Count);
            }

            logger.Log("Done");
        }

        // menu PLAY SOUND button
        private async void playSoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await Task.Run(() => PlayMusic());
        }

        private void PlayMusic()
        {
            _stopPlay = false;
            var speedValue = 100;

            MidiPlayer.OpenMidi();

            Invoke(new Action(() =>
            {
                MidiPlayer.Play(new ProgramChange(0, 1, (GeneralMidiInstruments)soundType.SelectedItem));
            }));
            var cursor = new Cursor();

            for (var w = 0; w < Origin.W; w++)
            {
                if (_stopPlay)
                {
                    break;
                }

                cursor.DrawCursor(w);
                pictureBox2.Image = cursor.cursorLine;

                var octaveLengthInPixels = Origin.H / 2; // 2 - octaves number
                var starsOnLine = Stars.ListOfStars.FindAll(x => x.StarCenter.X == w);
                foreach (var star in starsOnLine)
                {
                    int octave = 3; // stock octave
                    if (star.StarCenter.Y < octaveLengthInPixels)
                        octave++;

                    // tone
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

                    // volume
                    var volume = 0;
                    var intervalNumber = 20;
                    var intervalMin = 40;
                    var intervalMax = 120;
                    var intervalLength = (intervalMax - intervalMin) / intervalNumber;
                    double brightnessIntervalLength = (Stars.MaxBrightness - Stars.MinBrightness) / (double)intervalNumber;
                    for (var i = 0; i < intervalNumber; i++)
                    {
                        if (star.AverageBrightness >= Stars.MinBrightness + brightnessIntervalLength * i &&
                            star.AverageBrightness <= Stars.MinBrightness + brightnessIntervalLength * (i + 1))
                            volume = intervalMin + intervalLength * i;
                        if (star.AverageBrightness == Stars.MaxBrightness)
                            volume = intervalMax;
                    }

                    PlaySound((byte)volume, button);
                    //Invoke(new Action(() => // delay between piano button push
                    //{
                    //    Thread.Sleep(speedValue);
                    //}));
                }

                Thread.Sleep(int.Parse(columnSpeed.Text)); // delay between pixel column go
            }

            pictureBox2.Image = Stars.Bitmap;
        }

        private void PlaySound(byte volume, string note)
        {
            if (!string.IsNullOrEmpty(note))
                MidiPlayer.Play(new NoteOn(0, 1, note, volume));
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _stopPlay = true;
            string[] allButtons = { "A3", "B3", "C3", "D3", "E3", "F3", "G3", "A4", "B4", "C4", "D4", "E4", "F4", "G4", };
            foreach (var button in allButtons)
            {
                MidiPlayer.Play(new NoteOff(0, 1, button, 100));
            }
        }

        private void findStarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = Stars.Bitmap;
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
            pictureBox2.Image = bitmap;
        }
    }
}