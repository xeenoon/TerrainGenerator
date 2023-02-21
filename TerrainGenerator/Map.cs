using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGenerator
{
    public enum BiomeType
    {
        Desert,
        Grassland,
        Lake,
    }
    internal class Map
    {
        public Biome[] biomes;
        public int width;
        public int height;

        public Map(Biome[] biomes, int width, int height)
        {
            this.biomes = biomes;
            this.width = width;
            this.height = height;
        }

        public Biome FromPoint(Point point) //Find the biome that the point is in. It will be the closest biome
        {
            Biome closestbiome = null;
            double lastdistance = double.MaxValue;
            
            for (int i = 0; i < biomes.Length; ++i)
            {
                double distance = DistanceBetweenPoints(point, biomes[i].point);
                if (distance < lastdistance) //Closest to this biome
                {
                    closestbiome = biomes[i];
                    lastdistance = distance;
                }
            }
            return closestbiome;
        }
        double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
    }
    public class Biome
    {
        public BiomeType biomeType;
        public Point point;

        public Biome(BiomeType biomeType, Point point)
        {
            this.biomeType = biomeType;
            this.point = point;
        }
    }
}
