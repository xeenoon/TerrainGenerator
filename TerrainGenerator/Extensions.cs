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
    }
}
