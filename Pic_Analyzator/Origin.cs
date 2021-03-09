using System.Collections.Generic;
using System.Drawing;

namespace Pic_Analyzator
{
    static class Origin
    {
        public static Bitmap Bitmap { get; set; } // original picture
        public static int H { get; set; } // height [y]
        public static int W { get; set; } // width [x]
        public static int Max { get; set; } // max brightness of all pixels
        public static int Min { get; set; } // min brightness level    
        public static List<Pixel> Pixels { get; set; } // pixels array
    }
}
