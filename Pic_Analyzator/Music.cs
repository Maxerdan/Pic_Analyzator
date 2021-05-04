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
                MidiPlayer.Play(new ProgramChange(0, 2, GeneralMidiInstruments.Goblin));
            }));
            var nebulaMinBrightness = Nebula.NebulaAverageBrightness.Min(p => p.Value);
            var nebulaMaxBrightness = Nebula.NebulaAverageBrightness.Max(p => p.Value);
            var cursor = new Cursor();

            for (var x = 0; x < Origin.W; x++)
            {
                if (form._stopPlay)
                {
                    break;
                }

                cursor.DrawCursor(x);
                form.pictureBox2.Image = cursor.cursorLine;

                var octaveLengthInPixels = Origin.H / 2; // 2 - octaves number
                var starsOnLine = Stars.ListOfStars.FindAll(p => p.StarCenter.X == x);
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
                        MessageBox.Show($"Y-starCenter: {star.StarCenter.Y}\nOctaveLength: {octSumLength}\nMaxLegth: {octSumLength + buttonLength * 7}", "Star can't be interpolated");
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

                    var nebulaNote = "";
                    byte nebulaVolume = 60;
                    int nebulaOctaveNum = 3;
                    if (Nebula.NebulaAverageBrightness.Any(p => p.Key == x))
                    {
                        var brightness = Nebula.NebulaAverageBrightness[x];
                        var nebulaBrightInterval = (nebulaMaxBrightness - nebulaMinBrightness) / 7.0;

                        if (brightness >= nebulaMinBrightness && brightness <= nebulaMinBrightness + nebulaBrightInterval)
                            nebulaNote = $"C{nebulaOctaveNum}";
                        else if (brightness >= nebulaMinBrightness + nebulaBrightInterval && brightness <= nebulaMinBrightness + nebulaBrightInterval * 2)
                            nebulaNote = $"D{nebulaOctaveNum}";
                        else if (brightness >= nebulaMinBrightness + nebulaBrightInterval * 2 && brightness <= nebulaMinBrightness + nebulaBrightInterval * 3)
                            nebulaNote = $"E{nebulaOctaveNum}";
                        else if (brightness >= nebulaMinBrightness + nebulaBrightInterval * 3 && brightness <= nebulaMinBrightness + nebulaBrightInterval * 4)
                            nebulaNote = $"F{nebulaOctaveNum}";
                        else if (brightness >= nebulaMinBrightness + nebulaBrightInterval * 4 && brightness <= nebulaMinBrightness + nebulaBrightInterval * 5)
                            nebulaNote = $"B{nebulaOctaveNum}";
                        else if (brightness >= nebulaMinBrightness + nebulaBrightInterval * 5 && brightness <= nebulaMinBrightness + nebulaBrightInterval * 6)
                            nebulaNote = $"A{nebulaOctaveNum}";
                        else if (brightness >= nebulaMinBrightness + nebulaBrightInterval * 6 && brightness <= nebulaMinBrightness + nebulaBrightInterval * 7)
                            nebulaNote = $"G{nebulaOctaveNum}";
                        else
                        {
                            MessageBox.Show($"NebulaBrightness: {brightness}\nNebulaBrightInterval: {nebulaBrightInterval}\nMaxLegth: {nebulaMinBrightness + nebulaBrightInterval * 7}", "Nebula can't be interpolated");
                        }
                    }


                    PlaySound((byte)volume, button, 1);
                    PlaySound(nebulaVolume, nebulaNote, 2);
                    //form.Invoke(new Action(() => // delay between piano button push
                    //{
                    //    Thread.Sleep(speedValue);
                    //}));
                }

                Thread.Sleep(int.Parse(form.columnSpeed.Text)); // delay between pixel column go
            }

            form.pictureBox2.Image = Stars.Bitmap;
        }

        private static void PlaySound(byte volume, string note, byte channel)
        {
            if (!string.IsNullOrEmpty(note))
                MidiPlayer.Play(new NoteOn(0, channel, note, volume));
        }
    }
}
