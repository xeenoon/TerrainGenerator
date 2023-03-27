using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TerrainGenerator.Map;

namespace TerrainGenerator
{
    internal static class Extensions
    {
        public static List<BiomeLayerData> Copy(this List<BiomeLayerData> _m)
        {
            List<BiomeLayerData> result = new List<BiomeLayerData>();
            foreach (var item in _m)
            {
                result.Add(new BiomeLayerData(item.upperbound, new BMP((Bitmap)item.bitmap.wrappedBitmap.Clone())));
            }
            return result;
        }
        public static List<T> Copy<T>(this List<T> _m)
        {
            List<T> result = new List<T>();
            foreach (var item in _m)
            {
                result.Add(item);
            }
            return result;
        }


        public static Color SampleColor(this BMP bmp, int x, int y)
        {
            var adjusted_x = x;
            var adjusted_y = y;
            while (adjusted_x >= bmp.Width)
            {
                adjusted_x -= bmp.Width;
            }
            while (adjusted_y >= bmp.Height)
            {
                adjusted_y -= bmp.Height;
            }
            return bmp.GetPixel(adjusted_x, adjusted_y);
        }

        public static double DistanceTo(this PointF p1, PointF p2)
        {
            return Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        public static double DistanceTo(this Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        public static float[][] Average(this float[][] arry, float[][] comparer, int maxx, int maxy)
        {
            float[][] result = PerlinNoise.GetEmptyArray<float>(maxx, maxy);
            for (int x = 0; x < maxx; ++x)
            {
                for (int y = 0; y < maxy; ++y)
                {
                    result[x][y] = (arry[x][y] + comparer[x][y])/2f;
                }
            }
            return result;
        }
    }
}
