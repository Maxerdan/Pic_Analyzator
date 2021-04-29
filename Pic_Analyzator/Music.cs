using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Toub.Sound.Midi;

namespace Pic_Analyzator
{
    static class Music
    {
        public static void Play(Form1 form)
        {
            form._stopPlay = false;
            var speedValue = 100;

            MidiPlayer.OpenMidi();

            form.Invoke(new Action(() =>
            {
                MidiPlayer.Play(new ProgramChange(0, 1, (GeneralMidiInstruments)form.soundType.SelectedItem));
            }));
            var cursor = new Cursor();

            for (var w = 0; w < Origin.W; w++)
            {
                if (form._stopPlay)
                {
                    break;
                }

                cursor.DrawCursor(w);
                form.pictureBox2.Image = cursor.cursorLine;

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

                Thread.Sleep(int.Parse(form.columnSpeed.Text)); // delay between pixel column go
            }

            form.pictureBox2.Image = Stars.Bitmap;
        }

        private static void PlaySound(byte volume, string note)
        {
            if (!string.IsNullOrEmpty(note))
                MidiPlayer.Play(new NoteOn(0, 1, note, volume));
        }
    }
}
