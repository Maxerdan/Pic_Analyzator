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

        public static void FindStars()
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

            ListToBitmap();

            FindMaxAndMinAverageBrightness();
        }

        public static void ListToBitmap()
        {
            //load list of pixels to bitmap
            var bitmap = new Bitmap(Origin.W, Origin.H);
            foreach (var oneStar in Stars.ListOfStars)
                foreach (var pixels in oneStar.StarPixels)
                    bitmap.SetPixel(pixels.Point.X, pixels.Point.Y, pixels.Color);

            Stars.Bitmap = bitmap;
        }

        private static Point TakeStarCenter(List<Pixel> starPixels)
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

        private static void FindMaxAndMinAverageBrightness()
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

        private static Color GetColor(int x, int y)
        {
            return Stars.Bitmap.GetPixel(x, y);
        }

        private static int TakeAverageBrightness(List<Pixel> starPixels)
        {
            var brightnessSum = 0;
            foreach (var pixel in starPixels)
            {
                brightnessSum += (int)(pixel.Color.GetBrightness() * 1000);
            }

            return brightnessSum / starPixels.Count;
        }
    }

    class OneStar
    {
        public List<Pixel> StarPixels { get; set; }

        public Point StarCenter { get; set; }

        public int AverageBrightness { get; set; }
    }
}
