using System.Drawing;
using System.Drawing.Imaging;

namespace TerrainGenerator
{
    public partial class Form1 : Form
    {
        public Dictionary<float, Color> colors = new Dictionary<float, Color>() { { 0.3f, Color.FromArgb(2, 60, 100) }, { 0.35f, Color.FromArgb(197, 151, 86) }, { 0.55f, Color.FromArgb(34, 139, 34) }, { 0.7f, Color.FromArgb(85,85,85) }, { 1f, Color.FromArgb(223, 227, 220) } };

        BMP snow = new(Properties.Resources.snow.Clone(new Rectangle(0,0,Properties.Resources.snow.Width, Properties.Resources.snow.Height), PixelFormat.Format32bppArgb));

        Bitmap result;
        public Form1()
        {
            InitializeComponent();
        }
        int size = 1;
        int zoom = 1;
        float blend = 0;
        private void RefreshTerrain()
        {
            List<float> total = new List<float>();

            result = new Bitmap(Width*size, Height*size);
            var perlin = PerlinNoise.GeneratePerlinNoise(Width*size, Height*size, zoom);

            var max = perlin.Select(p => p.Max()).Max();
            var min = perlin.Select(p => p.Min()).Min();
            var scalar = max - min;

            var ordered_colors = colors.OrderBy(c => c.Key).Select(c=>c.Key).ToList();

            using (var bmp = new BMP(result))
            {
                for (int x = 0; x < Width*size; ++x)
                {
                    for (int y = 0; y < Height*size; ++y)
                    {
                        float adjustment = perlin[x][y] * (1/(max-min));
                        total.Add(adjustment);

                        float lastheight = 0;
                        int idx = 0;
                        foreach (var color in colors)
                        {
                            if (adjustment > lastheight && adjustment < color.Key)
                            {
                                var currentcolor = color.Value;

                                if (color.Value == Color.FromArgb(223, 227, 220))
                                {
                                    //Doing snow
                                    var adjusted_x = x;
                                    var adjusted_y = y;
                                    while (adjusted_x >= snow.Width)
                                    {
                                        adjusted_x -= snow.Width;
                                    }
                                    while (adjusted_y >= snow.Height)
                                    {
                                        adjusted_y -= snow.Height;
                                    }
                                    currentcolor = snow.GetPixel(adjusted_x, adjusted_y);
                                }

                                currentcolor = ChangeColorBrightness(currentcolor, adjustment - lastheight);

                                //Blend stuff to the lower "1/blend" of the color for the previous one

                                //Like this, if the lower color starts at 0.1, and thie color is from 0.1,0.9 with a blend of (1/4)
                                //0.1 18% blended towards lower color
                                //0.2 9% blurred towards lower color
                                //0.3 no blur
                                //0.4 no blur
                                //...


                                if (idx != 0)
                                {
                                    float below_space = color.Key - lastheight;

                                    if (adjustment < (lastheight + below_space/(1/blend)))
                                    {
                                        //We are in the lower quartile, so adjust the color towards the lower color
                                        var lowercolor = colors[ordered_colors[idx-1]];

                                        //The closer we are to the lower color, the more we should blend
                                        float amount = 8 * (((below_space / (1/blend)) + lastheight) - adjustment);
                                        currentcolor = Blend(currentcolor, lowercolor, amount);
                                    }
                                }

                                bmp.SetPixel(x, y, currentcolor);
                            }
                            lastheight = color.Key;
                            ++idx;
                        }
                    }
                }
            }
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
            if (amount <= 0)
            {

            }

            byte red   = (byte)(((color.R * (1 - amount)) + (backColor.R * amount)));
            byte green = (byte)(((color.G * (1 - amount)) + (backColor.G * amount)));
            byte blue  = (byte)(((color.B * (1 - amount)) + (backColor.B * amount)));

            return Color.FromArgb(red, green, blue);
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
            blend = float.Parse(textBox3.Text);
            RefreshTerrain();
            Invalidate();
        }
    }
}