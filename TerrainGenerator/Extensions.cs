using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
