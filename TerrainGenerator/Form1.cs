using System.Drawing;
using System.Drawing.Imaging;

namespace TerrainGenerator
{
    public partial class Form1 : Form
    {
        private const float OasisLevel    = 0.10f;
        private const float GrassLevel    = 0.45f;
        private const float DesertLevel   = 0.60f;
        private const float MountainLevel = 1f;
        Bitmap result;
        public Form1()
        {
            InitializeComponent();
        }
        int size = 1;
        int zoom = 1;

        int oasispixels = 0;
        int grasspixels = 0;
        int desertpixels = 0;
        int mountainpixels = 0;

        private void RefreshTerrain()
        {
            result = new Bitmap(Width*size, Height*size);
            var perlin = PerlinNoise.GeneratePerlinNoise(Width*size, Height*size, zoom);

            var max = perlin.Select(p => p.Max()).Max();
            var min = perlin.Select(p => p.Min()).Min();
            var scalar = max - min;

            using (var bmp = new BMP(result))
            {
                for (int x = 0; x < Width*size; ++x)
                {
                    for (int y = 0; y < Height*size; ++y)
                    {
                        float adjustment = perlin[x][y] * (1/(max-min));
                        Color oasis = Color.FromArgb(2, 60, 70);
                        Color grass = Color.FromArgb(34, 139, 34);
                        Color desert = Color.FromArgb(197, 151, 86);
                        Color mountain = Color.FromArgb(130, 78, 38);

                        if (adjustment < OasisLevel)
                        {
                            bmp.SetPixel(x, y, ChangeColorBrightness(oasis, (adjustment - 0.2f)/1.1f));
                            ++oasispixels;
                        }
                        else if (adjustment < GrassLevel)
                        {
                            bmp.SetPixel(x, y, ChangeColorBrightness(grass, adjustment));
                            ++grasspixels;
                        }
                        else if (adjustment < DesertLevel)
                        {
                            Color color;
                            if (adjustment < 0.2f) //Scale towards mountains
                            {
                                color = desert;
                                //color = Blend(desert, mountain, (1 - (adjustment - 0.15f)) / 10);
                            }
                            else //Scale towards grass
                            {
                                color = desert;
                                color = Blend(desert, grass, (1 - (adjustment - 0.45f)) / 2);
                            }
                            bmp.SetPixel(x, y, ChangeColorBrightness(color, adjustment));
                            ++desertpixels;
                        }
                        else if(adjustment < MountainLevel)
                        {
                            var colour = Blend(mountain, desert, (1 - (adjustment - 0.05f)) / 2);
                            bmp.SetPixel(x, y, ChangeColorBrightness(colour, adjustment));
                            ++mountainpixels;
                        }
                    }
                }
            }

            //result = Blur(result, 5);
            //MessageBox.Show(string.Format("{4} pixels:  {0}\n{5} pixels:  {1}\n{6} pixels: {2}\n{7} pixels: {3}", oasispixels, grasspixels, desertpixels, mountainpixels, OasisLevel.ToString()));
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.DrawImage(result, 0, 0, Width, Height);
            }
            catch
            {

            }
        }

        public static Color Blend(Color color, Color backColor, double amount)
        {
            byte r = (byte)(color.R * amount + backColor.R * (1 - amount));
            byte g = (byte)(color.G * amount + backColor.G * (1 - amount));
            byte b = (byte)(color.B * amount + backColor.B * (1 - amount));
            return Color.FromArgb(r, g, b);
        }
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
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
                red   = 255f;
                blue = 255f;
            }
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }
        private static Bitmap Blur(Bitmap image, Int32 blurSize)
        {
            return Blur(image, new Rectangle(0, 0, image.Width, image.Height), blurSize);
        }

        private unsafe static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            Bitmap blurred = new Bitmap(image.Width, image.Height);

            // make an exact copy of the bitmap provided
            using (Graphics graphics = Graphics.FromImage(blurred))
                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

            // Lock the bitmap's bits
            BitmapData blurredData = blurred.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, blurred.PixelFormat);

            // Get bits per pixel for current PixelFormat
            int bitsPerPixel = Image.GetPixelFormatSize(blurred.PixelFormat);

            // Get pointer to first line
            byte* scan0 = (byte*)blurredData.Scan0.ToPointer();

            // look at every pixel in the blur rectangle
            for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    int avgR = 0, avgG = 0, avgB = 0;
                    int blurPixelCount = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (int x = xx; (x < xx + blurSize && x < image.Width); x++)
                    {
                        for (int y = yy; (y < yy + blurSize && y < image.Height); y++)
                        {
                            // Get pointer to RGB
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            avgB += data[0]; // Blue
                            avgG += data[1]; // Green
                            avgR += data[2]; // Red

                            blurPixelCount++;
                        }
                    }

                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    // now that we know the average for the blur size, set each pixel to that color
                    for (int x = xx; x < xx + blurSize && x < image.Width && x < rectangle.Width; x++)
                    {
                        for (int y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Height; y++)
                        {
                            // Get pointer to RGB
                            byte* data = scan0 + y * blurredData.Stride + x * bitsPerPixel / 8;

                            // Change values
                            data[0] = (byte)avgB;
                            data[1] = (byte)avgG;
                            data[2] = (byte)avgR;
                        }
                    }
                }
            }

            // Unlock the bits
            blurred.UnlockBits(blurredData);

            return blurred;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            size = int.Parse(textBox2.Text);
            zoom = int.Parse(textBox1.Text);
            RefreshTerrain();
            Invalidate();
        }
    }
}