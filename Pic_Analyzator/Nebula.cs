using System.Collections.Generic;
using System.Drawing;

namespace Pic_Analyzator
{
    static class Nebula
    {
        public static Bitmap Bitmap { get; set; }
        public static List<Pixel> Pixels { get; set; }
        public static Dictionary<int, int> NebulaAverageBrightness { get; set; }
    }
}
