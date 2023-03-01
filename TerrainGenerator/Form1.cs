using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace TerrainGenerator
{
    public partial class Form1 : Form
    {
        Bitmap snow = Properties.Resources.snow;
        Bitmap rock = Properties.Resources.rock;
        Bitmap grass = Properties.Resources.grass;
        Bitmap sand = Properties.Resources.sand;
        Bitmap water = Properties.Resources.water;

        Dictionary<float, BMP> colors = new Dictionary<float, BMP>();
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

            result = new Bitmap(Width * size, Height * size);
            var perlin = PerlinNoise.GeneratePerlinNoise(Width * size, Height * size, zoom);

            var max = perlin.Select(p => p.Max()).Max();
            var min = perlin.Select(p => p.Min()).Min();
            var scalar = max - min;

            var ordered_colors = colors.OrderBy(c => c.Key).Select(c => c.Key).ToList();

            using (var bmp = new BMP(result))
            {
                for (int x = 0; x < Width * size; ++x)
                {
                    for (int y = 0; y < Height * size; ++y)
                    {
                        float adjustment = perlin[x][y] * (1 / (max - min));
                        total.Add(adjustment);

                        float lastheight = 0;
                        int idx = 0;
                        foreach (var color in colors)
                        {
                            if (adjustment > lastheight && adjustment < color.Key)
                            {
                                //Sample color texture
                                Color currentcolor = SampleColor(x, y, color.Value);

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

                                    if (adjustment < (lastheight + below_space / (1 / blend)))
                                    {
                                        //We are in the lower quartile, so adjust the color towards the lower color
                                        var lowercolor = colors[ordered_colors[idx - 1]];

                                        //The closer we are to the lower color, the more we should blend
                                        float amount = 8 * (((below_space / (1 / blend)) + lastheight) - adjustment);
                                        currentcolor = Blend(currentcolor, SampleColor(x, y, lowercolor), amount);
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

        private Color SampleColor(int x, int y, BMP bmp)
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

            byte red = (byte)(((color.R * (1 - amount)) + (backColor.R * amount)));
            byte green = (byte)(((color.G * (1 - amount)) + (backColor.G * amount)));
            byte blue = (byte)(((color.B * (1 - amount)) + (backColor.B * amount)));

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
                red = 255f;
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
            colors.Clear();
            if (panel1.Visible)
            {
                foreach (var layer in layerChoosers)
                {
                    string[] points = layer.imagesize_textbox.Text.Split(",");
                    var newsize = new Bitmap(layer.image, new Size(int.Parse(points[0]), int.Parse(points[1])));
                    colors.Add(float.Parse(layer.upperbound_textbox.Text), new BMP(newsize.Clone(new Rectangle(0,0,newsize.Width, newsize.Height), PixelFormat.Format32bppArgb)));
                }
            }
            else
            {
                colors.Add(0.3f, new BMP(water.Clone(new Rectangle(0, 0, water.Width, water.Height), PixelFormat.Format32bppArgb)));
                colors.Add(0.35f, new BMP(sand.Clone(new Rectangle(0, 0, sand.Width, sand.Height), PixelFormat.Format32bppArgb)));
                colors.Add(0.55f, new BMP(grass.Clone(new Rectangle(0, 0, grass.Width, grass.Height), PixelFormat.Format32bppArgb)));
                colors.Add(0.7f, new BMP(rock.Clone(new Rectangle(0, 0, rock.Width, rock.Height), PixelFormat.Format32bppArgb)));
                colors.Add(1f, new BMP(snow.Clone(new Rectangle(0, 0, snow.Width, snow.Height), PixelFormat.Format32bppArgb)));
            }

            RefreshTerrain();
            Invalidate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            panel1.Visible = !panel1.Visible;
        }

        public bool Reload()
        {
            y_pos = 40;
            foreach (var layer in layerChoosers)
            {
                layer.Reset(ref y_pos);
            }

            return true;
        }

        int y_pos = 40;
        public static List<LayerChooser> layerChoosers = new List<LayerChooser>();
        private void AddLayer(object sender, EventArgs e)
        {
            LayerChooser layerChooser = new LayerChooser(ref y_pos, Reload);
            layerChoosers.Add(layerChooser);
            layerChooser.AddTo(panel1);
        }
        public class LayerChooser
        {
            Label upperbound_label = new Label();
            public TextBox upperbound_textbox = new TextBox();
            public TextBox imagesize_textbox = new TextBox();
            Button uploadImageButton = new Button();
            Button deleteButton = new Button();

            public Bitmap image;
            Func<bool> Reload;

            public LayerChooser(ref int y_pos, Func<bool> Reload)
            {
                this.Reload = Reload;

                upperbound_label.Text = "Upper Bound:";
                upperbound_label.Location = new Point(3, y_pos + 3);
                upperbound_label.AutoSize = true;

                upperbound_textbox.Size = new Size(30, 23);
                upperbound_textbox.Location = new Point(86, y_pos);

                imagesize_textbox.Size = new Size(70, 23);
                imagesize_textbox.Location = new Point(120, y_pos);


                uploadImageButton.Size = new Size(95, 23);
                uploadImageButton.Location = new Point(192, y_pos);
                uploadImageButton.Text = "Upload image";
                uploadImageButton.Click += new EventHandler(AddImage);

                deleteButton.Size = new Size(19, 23);
                deleteButton.Location = new Point(289, y_pos);
                deleteButton.Text = "X";
                deleteButton.Click += new EventHandler(Delete);

                y_pos += 30;
            }

            public void Reset(ref int y_pos)
            {
                upperbound_label.Text = "Upper Bound:";
                upperbound_label.Location = new Point(3, y_pos + 3);
                upperbound_label.AutoSize = true;

                upperbound_textbox.Size = new Size(100, 23);
                upperbound_textbox.Location = new Point(86, y_pos);

                imagesize_textbox.Size = new Size(70, 23);
                imagesize_textbox.Location = new Point(120, y_pos);

                uploadImageButton.Size = new Size(95, 23);
                uploadImageButton.Location = new Point(192, y_pos);
                uploadImageButton.Text = "Upload image";

                deleteButton.Size = new Size(19, 23);
                deleteButton.Location = new Point(289, y_pos);
                deleteButton.Text = "X";

                y_pos += 30;
            }

            public void Delete(object sender, EventArgs e)
            {
                Form1.layerChoosers.Remove(this);

                parent.Controls.Remove(upperbound_label);
                parent.Controls.Remove(upperbound_textbox);
                parent.Controls.Remove(imagesize_textbox);
                parent.Controls.Remove(uploadImageButton);
                parent.Controls.Remove(deleteButton);

                Reload();
            }

            public void AddImage(object sender, EventArgs e)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                var result = dialog.ShowDialog();
                if (result != DialogResult.Cancel)
                {
                    var filetype = dialog.FileName.Split(".")[1];
                    if (filetype != "jpg")
                    {
                        MessageBox.Show("Change filetype to .jpg");
                        return;
                    }

                    image = (Bitmap)Image.FromFile(dialog.FileName);
                    //Image should now be successfully uploaded
                    uploadImageButton.Text = dialog.FileName.Split("\\").Last();

                    imagesize_textbox.Text = String.Format("{0},{1}", image.Width, image.Height);
                }
            }
            Panel parent;

            public void AddTo(Panel panel)
            {
                parent = panel;
                panel.Controls.Add(upperbound_label);
                panel.Controls.Add(upperbound_textbox);
                panel.Controls.Add(imagesize_textbox);
                panel.Controls.Add(uploadImageButton);
                panel.Controls.Add(deleteButton);
            }
        }
    }
}