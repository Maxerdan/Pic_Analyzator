using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pic_Analyzator
{
    class Cursor
    {
        public Bitmap cursorLine { get; set; }
        private int waveLength { get; set; }

        public Cursor()
        {
            waveLength = 8;
            if (Origin.W > 500)
                waveLength += 8;
            if (Origin.W > 1000)
                waveLength += 8;
            if (Origin.W > 1500)
                waveLength += 8;
        }

        public void DrawCursor(int x)
        {
            cursorLine = new Bitmap(Stars.Bitmap);

            var count = 1;
            var minusCount = false;
            for (var y = 0; y < Origin.H; y++)
            {
                if (Stars.Bitmap.GetPixel(x, y).GetBrightness() != 0)
                {
                    if (count < 0)
                        count = 1;
                    if (!minusCount)
                        count += waveLength;
                    else
                        count -= waveLength;
                    if (Stars.ListOfStars.Any(p => p.StarCenter.Y == y)) // might bug
                    {
                        minusCount = true;
                    }
                    for (var j = 1; j <= count; j++)
                    {
                        if (x + j < Origin.W && y - 1 > 0)
                            if (cursorLine.GetPixel(x + j, y - 1) != Color.FromArgb(251, 206, 177))
                                cursorLine.SetPixel(x + j, y, Color.FromArgb(251, 206, 177));
                    }
                }
                else
                {
                    cursorLine.SetPixel(x, y, Color.FromArgb(251, 206, 177));
                    count = 1;
                    minusCount = false;
                }
            }
        }
    }
}
