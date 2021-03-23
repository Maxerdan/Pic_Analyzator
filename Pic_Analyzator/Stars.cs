using System.Collections.Generic;
using System.Drawing;

namespace Pic_Analyzator
{
    static class Stars
    {
        public static Bitmap Bitmap { get; set; }

        public static List<OneStar> ListOfStars { get; set; }

        public static int MaxBrightness { get; set; }

        public static int MinBrightness { get; set; }
    }

    class OneStar
    {
        public List<Pixel> StarPixels { get; set; }

        public Point StarCenter { get; set; }

        public int AverageBrightness { get; set; }
    }
}
