using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Windows.Media.Media3D;
using System.Security.Cryptography.X509Certificates;
using System.Drawing;
using System.Windows.Media.TextFormatting;

namespace TerrainGenerator
{
    internal class Map
    {
        public Biome[] biomes;
        public int width;
        public int height;
        public Bitmap map;

        Circle circle;

        public Map(List<Biome> biomes, int width, int height)
        {
            this.width = width;
            this.height = height;
            map = new Bitmap(width, height);
            circle = new Circle(width < height ? width / 2 : height / 2);
            PlaceBiomes(biomes);
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

        Random random = new Random();
        public void PlaceBiomes(List<Biome> inputbiomes)
        {
            this.biomes = new Biome[inputbiomes.Count()];
            for (int i = 0; i < biomes.Count(); ++i)
            {
                Point point;
                do
                {
                    point = new Point(random.Next(0, width), random.Next(0, height));
                } while (!circle.PointInCircle(point));
                biomes[i] = inputbiomes[i];
                biomes[i].point = point;
            }
        }

        public BMP Draw()
        {
            var bmp = new BMP(map);

            List<float> total = new List<float>();

            var perlin = PerlinNoise.GeneratePerlinNoise(width, height, Form1.zoom);

            var max = perlin.Select(p => p.Max()).Max();
            var min = perlin.Select(p => p.Min()).Min();
            var scalar = max - min;


            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    if (circle.PointInCircle(new Point(x, y)))
                    {
                        float adjustment = perlin[x][y] * (1 / (max - min));
                        total.Add(adjustment);
                        var currentBiome = FromPoint(new Point(x, y));
                        float lastheight = 0;
                        int idx = 0;
                        if (adjustment >= 1)
                        {
                            adjustment = 1;
                        }
                        foreach (var color in currentBiome.colors)
                        {
                            if (adjustment > lastheight && adjustment <= color.upperbound)
                            {
                                //Sample color texture
                                Color currentcolor = color.bitmap.SampleColor(x, y);

                                if (idx != 0)
                                {
                                    float upperbound = color.upperbound;
                                    float lowerbound = currentBiome.colors[idx - 1].upperbound;
                                    float size = upperbound - lowerbound;

                                    float above_space = upperbound - adjustment;

                                    if (above_space < size)
                                    {
                                        //We are in the lower quartile, so adjust the color towards the lower color
                                        var lowercolor = currentBiome.colors[idx - 1].bitmap;

                                        //The closer we are to the lower color, the more we should blend
                                        float amount = ((above_space) / (Form1.blend * size));
                                        currentcolor = Blend(currentcolor, lowercolor.SampleColor(x, y), amount);
                                    }
                                }

                                bmp.SetPixel(x, y, currentcolor);
                            }
                            lastheight = color.upperbound;
                            ++idx;
                        }
                    }
                    else if (circle.InCircleBorder(new Point(x, y)))
                    {
                        bmp.SetPixel(x, y, Color.FromArgb(150, 75, 0));
                    }
                }
            }
            return bmp;
        }
        public static Color Blend(Color color, Color backColor, double amount)
        {
            if (amount <= 0)
            {

            }

            byte red = (byte)(((color.R * (1 - amount)) + (backColor.R * amount)));
            byte green = (byte)(((color.G * (1 - amount)) + (backColor.G * amount)));
            byte blue = (byte)(((color.B * (1 - amount)) + (backColor.B * amount)));

            return Color.FromArgb(red, green, blue);
        }
        const float softcap = 0.2f;
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            if (correctionFactor >= softcap) //Soft cap brightness modifier at 0.3f
            {
                correctionFactor = softcap + ((correctionFactor - softcap) / 1.1f*correctionFactor);
            }

            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }
            if (green > 255 || red > 255 || blue > 255)
            {
                green = 255f;
                red = 255f;
                blue = 255f;
            }
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }
    }
    public class Biome
    {
        public string biomeType;
        public Point point;

        public List<BiomeLayerData> colors = new List<BiomeLayerData>();
        public Biome(Point point)
        {
            this.point = point;
        }
        public Biome(string biomeType, Point point, List<BiomeLayerData> colors)
        {
            this.biomeType = biomeType;
            this.point = point;

            this.colors = colors.Copy();
        }

        public void Save(string folderpath)
        {
            //Will put images in a folder named with their coresponding upperbound values
            if (!Directory.Exists(folderpath)) //If the folder doesn't exist, create it
            {
                Directory.CreateDirectory(folderpath);
            }
            foreach (var color in colors)
            {
                string formattedbounds = color.upperbound.ToString().Replace(".", "_"); //Use underscores to avoid file extension mishaps
                color.bitmap.wrappedBitmap.Save(Path.Combine(folderpath, (formattedbounds + ".png")), ImageFormat.Png);
            }
        }
        private Biome() { }
        public static Biome FromFolder(string folderpath)
        {
            Biome result = new Biome();
            result.biomeType = (folderpath.Split("\\").Where(s => s != "").Last());


            foreach (var file in Directory.EnumerateFiles(folderpath))
            {
                string v = file.Split(@"\").Last();
                string s = v.Split(".")[0].Replace("_", ".");
                var upperbound = float.Parse(s);
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

    public class Circle
    {
        int radius;

        public Circle(int radius)
        {
            this.radius = radius;
        }

        public bool PointInCircle(Point point)
        {
            return DistanceBetweenPoints(point, new Point(radius, radius)) < radius;
        }
        public bool InCircleBorder(Point point)
        {
            return DistanceBetweenPoints(point, new Point(radius, radius)) < (radius + 10);
        }
        double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
    }

    public class Circle_DetailArea
    {
        public Point location;
        public int lowestarea;
        public int highestarea;

        public float xstretch;
        public float ystretch;

        public PointF[] points = new PointF[720]; //2 points per degrees

        public Circle_DetailArea(int lowestarea, int highestarea, float xstretch, float ystretch)
        {
            this.lowestarea = lowestarea;
            this.highestarea = highestarea;
            this.xstretch = xstretch;
            this.ystretch = ystretch;
        }
        Random random = new Random();
        public void Generate()
        {
            double startradius = Math.Sqrt(((lowestarea+highestarea)/2)/Math.PI); //Circle area = pi*r^2, r = sqrt(area/pi)
            double nextradius = startradius;
            double lastradius = startradius;
            for (double degrees = 0; degrees < 360; degrees+=(0.5))
            {
                do
                {
                    double v = random.NextDouble();
                    nextradius = lastradius * (((v - 0.5) / 25) + 1); //Add some vibration to the startradius
                } while (nextradius < startradius * 0.9 || nextradius > startradius * 1.1);
                
                if (startradius == 0)
                {

                }
                //Do a right angle triangle, where degrees = theta, and c = startradius

                //For x coordinate, we use adjacent side
                //a/h=cos(theta)
                //a=h*cos(theta)
                double radians = DegToRad(degrees);
                double a = nextradius* Math.Cos(radians);

                //For y coordinate, we use opposite side
                //o/h=sin(theta)
                //o=h*sin(theta)
                double o = nextradius * Math.Sin(radians);

                if (double.IsNaN(a) || double.IsNaN(o))
                {

                }
                
                points[(int)(degrees*2)] = new PointF(((float)a)*xstretch + location.X,((float)o)*ystretch + location.Y);
                lastradius = nextradius;
            }
        }

        private static double DegToRad(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
    public class Direction_DetailArea
    {
        public List<Point> visitedpoints;
        private List<Vector> placementVectors = new List<Vector>();

        public float radius;
        public List<PointF> points = new List<PointF>();

        public float[,] heightmap;

        Random random = new Random();

        public Direction_DetailArea(List<Point> visitedpoints, float radius, int maxwidth, int maxheight)
        {
            this.visitedpoints = visitedpoints.Copy();
            this.radius = radius;
            heightmap = new float[maxwidth,maxheight];
        }

        public void Generate()
        {
            //Generate vectors
            for (int i = 1; i < visitedpoints.Count; ++i)
            {
                placementVectors.Add(new Vector(visitedpoints[i-1], visitedpoints[i]));
            }

            List<PointF> leftpoints = new List<PointF>();
            List<PointF> rightpoints = new List<PointF>();


            PointF location = new PointF(visitedpoints.OrderBy(v => v.X).First().X, visitedpoints.OrderBy(v => v.Y).First().Y);
            var bounds = new RectangleF(location, new SizeF(visitedpoints.OrderByDescending(v => v.X).First().X-location.X, visitedpoints.OrderByDescending(v => v.Y).First().Y-location.Y));
            double lastradius = radius;
            double nextradius = lastradius;

            foreach (var vector in placementVectors)
            {
                points.AddRange(GetPoints(ref lastradius, ref nextradius, vector, true));
            }
            //Now do the same thing, but backwards
            placementVectors.Reverse();
            foreach (var vector in placementVectors)
            {
                points.AddRange(GetPoints(ref lastradius, ref nextradius, vector, false));
            }
            placementVectors.Reverse();

            
            //points = leftpoints.OrderBy(l=>l.Y).Concat(rightpoints.OrderByDescending(l=>l.Y)).ToList();
        }
        public void DrawPoints(Graphics g)
        {
            Generate();

            Pen vectorpen = new Pen(Color.Red);
            Pen dotpen = new Pen(Color.Green);
            Pen greyscalepen = new Pen(Color.Black);

            foreach (var vector in placementVectors)
            {
                g.DrawLine(vectorpen, vector.A, vector.B);
            }
            foreach (var point in points)
            {
                g.FillRectangle(dotpen.Brush, point.X, point.Y, 1, 1);
            }

            int highestx = (int)points.OrderByDescending(p => p.X).FirstOrDefault().X;
            int highesty = (int)points.OrderByDescending(p => p.X).FirstOrDefault().Y;

            for (int x = 0; x < highestx; ++x)
            {
                for (int y = 0; y < highesty; ++y)
                {
                    if (heightmap[x,y] == 1)
                    {
                        g.FillRectangle(greyscalepen.Brush, x, y, 1, 1);
                    }
                }
            }
        }

        private List<PointF> GetPoints(ref double lastradius, ref double nextradius, Vector vector, bool fowards)
        {
            double start;
            double end;

            if (fowards) //Moving fowards?
            {
                start = vector.A.X;
                end = vector.B.X;
            }
            else
            {
                start = vector.B.X;
                end = vector.A.X;
            }
            List<PointF> result = new List<PointF>();
            double change = start < end ? 0.5 : -0.5;

            for (double x = start; start < end ? x <= end : x >= end; x += change)
            {
                //Go left to right on the vector and add points for this vector
                double y = vector.GetPoint(x);

                do
                {
                    double v = random.NextDouble();
                    nextradius = lastradius * (((v - 0.5) / 25) + 1); //Add some vibration to the startradius
                } while (nextradius < radius * 0.9 || nextradius > radius * 1.1);

                //Get the vector perpindicular to this vector

                var scaled = vector.GetPerpindicular();
                scaled = scaled.GetUnitVector();
                scaled = scaled * nextradius;

                PointF p_start = new PointF((float)(x + scaled.i), (float)(y + scaled.j));
                PointF p_end = new PointF((float)(x - scaled.i), (float)(y - scaled.j));

                var start2 = x + scaled.i;
                var end2   = x - scaled.i;


                double change2 = start2 < end2 ? 1 : -1;

                Vector vector2 = new Vector(p_start, p_end);

                for (double x2 = start2; start2 < end2 ? x2 < end2 : x2 > end2; x2 += change2)
                {
                    //Add the depth of the vector to the mountain
                    var y2 = vector2.GetPoint(x2);

                    heightmap[(int)x2, (int)y2] = 1;
                }

                if (fowards)
                {
                    result.Add(new PointF((float)(x + scaled.i), (float)(y + scaled.j)));
                }
                else
                {
                    result.Add(new PointF((float)(x - scaled.i), (float)(y - scaled.j)));
                }
                lastradius = nextradius;
            }
            return result;
        }
    }

    public class Vector
    {
        public double i;
        public double j;

        public PointF A;
        public PointF B; //Where vector = ->
        internal double angle //Is in radians
        {
            //theta = arctan(j/i)
            get
            {
                return Math.Atan(j/i);
            }
        }

        //               AB
        private YMC_VectorLine vectorLine;
        public Vector(double i, double j)
        {
            this.i = i;
            this.j = j;
        }

        public static Vector operator *(Vector a, double s)
        {
            return new Vector(a.i*s, a.j*s);
        }
        public static Vector operator /(Vector a, double s)
        {
            return new Vector(a.i / s, a.j / s);
        }

        public Vector(PointF A, PointF B)
        {
            this.A = A;
            this.B = B;

            i = B.X - A.X;
            j = B.Y - A.Y;

            //Generate ymc vectorline
            //Convert into a y = mx + c equation
            //Step one, convert into ai + bj + t(ci + dj)
            //AB = A + t(b-a)
            var b_minus_a = new PointF(B.X - A.X, B.Y - A.Y);
            //r = (a+ct)i + (b+dt)j

            //a+ct = x
            //x - a = ct
            //t = x-a/c

            //y = b + d((x-a)/c)
            //y = d (x-a)/c
            
            vectorLine = new YMC_VectorLine(A.X, A.Y, b_minus_a.X, b_minus_a.Y);
        }

        public Vector GetPerpindicular()
        {
            //Find a new vector where i*a + j*b == 0
            return new Vector(-j, i);
        }
        public Vector GetUnitVector()
        {
            //Unit vector = v/|v|
            double magnitude = Math.Sqrt(i * i + j * j);
            return new Vector(i/magnitude,j/magnitude);
        }

        public bool PointOnLine(PointF point)
        {
            //Convert into a y = mx + c equation
            //Step one, convert into ai + bj + t(ci + dj)

            //Can only be done if two seperate points are given, where our 'point' is within the bounds
            RectangleF bounds = new RectangleF(new PointF(A.X < B.X ? A.X : B.X, A.Y < B.Y ? A.Y : B.Y), new SizeF(Math.Abs(B.X-A.X), Math.Abs(B.Y-A.Y)));
            if (A == B || !bounds.Contains(point)) //A, B start as 0,0 so if no points are given still returns false
            {
                return false;
            }

            return vectorLine.PointOnLine(point.X, point.Y);
        }

        internal double GetPoint(double x)
        {
            return vectorLine.Substitute(x);
        }
    }

    public class YMC_VectorLine
    {
        public double a;
        public double b;
        public double c;
        public double d; //y = b + d((x-a)/c)

        public YMC_VectorLine(double a, double b, double c, double d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public bool PointOnLine(double x, double y)
        {
            //y = b + d((x-a)/c)
            return b + d * ((x - a) / c) == y;
        }
        public double Substitute(double x)
        {
            return b + d * ((x - a) / c);
        }
    }
}