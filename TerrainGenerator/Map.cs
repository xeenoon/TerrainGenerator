using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Windows.Media.Media3D;

namespace TerrainGenerator
{
    public enum BiomeType
    {
        Forest,
        Desert,
        Tundra,
        Wasteland,
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

        public List<BiomeLayerData> colors = new List<BiomeLayerData>();

        public Biome(BiomeType biomeType, Point point, List<BiomeLayerData> colors)
        {
            this.biomeType = biomeType;
            this.point = point;

            this.colors = colors.Copy();
        }

        public void Write(string filepath)
        {
            //Will put images in a folder named with their coresponding upperbound values
            string folderpath = Path.Combine(filepath, biomeType.ToString());
            if (!Directory.Exists(folderpath)) //If the folder doesn't exist, create it
            {
                Directory.CreateDirectory(folderpath);
            }
            foreach (var color in colors)
            {
                string formattedbounds = color.upperbound.ToString().Replace(".","_"); //Use underscores to avoid file extension mishaps
                color.bitmap.wrappedBitmap.Save(Path.Combine(folderpath,(formattedbounds + ".png")), ImageFormat.Png);
            }
        }
        private Biome(){}
        public static Biome FromFolder(string folderpath)
        {
            Biome result = new Biome();
            try
            {
                result.biomeType = Enum.Parse<BiomeType>(folderpath.Split("\\").Where(s => s != "").Last());
            }
            catch
            {
                MessageBox.Show("Invalid folder name");
                return null;
            }

            foreach (var file in Directory.EnumerateFiles(folderpath))
            {
                var upperbound = float.Parse(file.Split(@"\\").Last().Split(".")[0]);
                result.colors.Add(new BiomeLayerData(upperbound, new BMP(new Bitmap(Image.FromFile(file)))));
            }
            return result;
        }
    }
    public class BiomeLayerData
    {
        public float upperbound;
        public BMP bitmap;

        public BiomeLayerData(float upperbound, BMP bitmap)
        {
            this.upperbound = upperbound;
            this.bitmap = bitmap;
        }
    }
}