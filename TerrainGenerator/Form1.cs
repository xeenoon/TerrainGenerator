using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace TerrainGenerator
{
    public partial class Form1 : Form
    {
        Bitmap snow = Properties.Resources.snow;
        Bitmap rock = Properties.Resources.rock;
        Bitmap grass = Properties.Resources.grass;
        Bitmap sand = Properties.Resources.sand;
        Bitmap water = Properties.Resources.water;

        Biome currentBiome;

        Bitmap result;
        Biome defaultBiome;
        System.Timers.Timer movetimer = new System.Timers.Timer();
        public Form1()
        {
            InitializeComponent();

            stableposition = new Rectangle(Width / 2 - 50, Height / 2 - 50, 100, 100);
            List<BiomeLayerData> colors = new()
            {
                new BiomeLayerData(0.3f, new BMP(water.Clone(new Rectangle(0, 0, water.Width, water.Height), PixelFormat.Format32bppArgb))),
                new BiomeLayerData(0.35f, new BMP(sand.Clone(new Rectangle(0, 0, sand.Width, sand.Height), PixelFormat.Format32bppArgb))),
                new BiomeLayerData(0.55f, new BMP(grass.Clone(new Rectangle(0, 0, grass.Width, grass.Height), PixelFormat.Format32bppArgb))),
                new BiomeLayerData(0.7f, new BMP(rock.Clone(new Rectangle(0, 0, rock.Width, rock.Height), PixelFormat.Format32bppArgb))),
                new BiomeLayerData(1f, new BMP(snow.Clone(new Rectangle(0, 0, snow.Width, snow.Height), PixelFormat.Format32bppArgb)))
            };

            defaultBiome = new Biome("Forest", new Point(0, 0), colors);
            currentBiome = new Biome("Forest", new Point(0, 0), colors);

            movetimer.Interval = 50;
            movetimer.Elapsed += new System.Timers.ElapsedEventHandler(Tick);
            movetimer.Start();
        }
        int size = 1;
        public static int zoom = 1;
        public static float blend = 0;
        private void RefreshTerrain()
        {
            List<float> total = new List<float>();

            result = new Bitmap(Width * size, Height * size);
            var perlin = PerlinNoise.GeneratePerlinNoise(Width * size, Height * size, zoom);

            var max = perlin.Select(p => p.Max()).Max();
            var min = perlin.Select(p => p.Min()).Min();
            var scalar = max - min;

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
                        foreach (var color in currentBiome.colors)
                        {
                            if (adjustment > lastheight && adjustment < color.upperbound)
                            {
                                //Sample color texture
                                Color currentcolor = color.bitmap.SampleColor(x, y);

                                //currentcolor = ChangeColorBrightness(currentcolor, adjustment - lastheight);

                                //Blend stuff to the lower "1/blend" of the color for the previous one

                                //Like this, if the lower color starts at 0.1, and thie color is from 0.1,0.9 with a blend of (1/4)
                                //0.1 18% blended towards lower color
                                //0.2 9% blurred towards lower color
                                //0.3 no blur
                                //0.4 no blur
                                //...


                                if (idx != 0)
                                {
                                    float upperbound = color.upperbound;
                                    float lowerbound = currentBiome.colors[idx - 1].upperbound;
                                    float size = upperbound - lowerbound;

                                    float above_space = upperbound - adjustment;

                                    if (above_space < (size))
                                    {
                                        //We are in the lower quartile, so adjust the color towards the lower color
                                        var lowercolor = currentBiome.colors[idx - 1].bitmap;

                                        //The closer we are to the lower color, the more we should blend
                                        float amount = (above_space) / (2 * size);
                                        currentcolor = Blend(currentcolor, lowercolor.SampleColor(x, y), amount);
                                    }
                                }

                                bmp.SetPixel(x, y, currentcolor);
                            }
                            lastheight = color.upperbound;
                            ++idx;
                        }
                    }
                }
            }
        }


        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (result != null)
            {
                e.Graphics.DrawImage(result, position.X, position.Y, result.Width, result.Height);
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
        private void button1_Click(object sender, EventArgs e)
        {
            result = new Bitmap(Width, Height);
            //Circle_DetailArea detailArea = new Circle_DetailArea(30000,50000, 2, 1);
            //detailArea.location = new Point(Width/2, Height/2);

            //Direction_DetailArea detailArea = new Direction_DetailArea(new List<Point>() {new Point(100,100), new Point(120,150), new Point(100,200), new Point(150, 220), new Point(200, 250), new Point(150, 270), new Point(100, 300) }, 10,Width, Height);
            Direction_DetailArea detailArea = new Direction_DetailArea(new List<Point>() { new Point(100, 100), new Point(120, 150) },50,Width,Height);
            //detailArea.Generate();
            detailArea.DrawPoints(Graphics.FromImage(result));
            //Graphics.FromImage(result).FillPolygon(new Pen(Color.Green).Brush,detailArea.points.ToArray());
            Invalidate();
            return;
            size = int.Parse(textBox2.Text);
            zoom = int.Parse(textBox1.Text);
            blend = float.Parse(textBox3.Text);
            if (textBox4.Text != "")
            {
                PerlinNoise.random = new Random(int.Parse(textBox4.Text));
            }
            else
            {
                PerlinNoise.random = new Random();
            }
            position = new Point(0, 0);

            Map map = new Map(biomeChoosers.Select(b => b.b).ToList(), Width, Height);
            result = (Bitmap)map.Draw().wrappedBitmap.Clone();
            Invalidate();

            return;
            if (textBox4.Text != "")
            {
                PerlinNoise.random = new Random(int.Parse(textBox4.Text));
            }
            else
            {
                PerlinNoise.random = new Random();
            }
            currentBiome.colors.Clear();
            if (panel1.Visible)
            {
                foreach (var layer in layerChoosers)
                {
                    try
                    {
                        string[] points = layer.imagesize_textbox.Text.Split(",");
                        var newsize = new Bitmap(layer.image, new Size(int.Parse(points[0]), int.Parse(points[1])));
                        currentBiome.colors.Add(new BiomeLayerData(float.Parse(layer.upperbound_textbox.Text), new BMP(newsize.Clone(new Rectangle(0, 0, newsize.Width, newsize.Height), PixelFormat.Format32bppArgb))));
                    }
                    catch
                    {
                        MessageBox.Show("Layer of upperbound {0} was formatted incorrectly. Was ignored", layer.upperbound_textbox.Text);
                    }
                }
            }
            else
            {
                currentBiome.colors = defaultBiome.colors.Copy();
            }

            RefreshTerrain();
            Invalidate();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            panel1.Visible = !panel1.Visible;
            panel2.Visible = !panel2.Visible;
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
            public Label upperbound_label = new Label();
            public TextBox upperbound_textbox = new TextBox();
            public TextBox imagesize_textbox = new TextBox();
            public Button uploadImageButton = new Button();
            public Button deleteButton = new Button();

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

                upperbound_textbox.Size = new Size(30, 23);
                upperbound_textbox.Location = new Point(86, y_pos);

                imagesize_textbox.Size = new Size(70, 23);
                imagesize_textbox.Location = new Point(120, y_pos);

                uploadImageButton.Size = new Size(95, 23);
                uploadImageButton.Location = new Point(192, y_pos);
                uploadImageButton.Text = image == null ? "Upload image" : uploadImageButton.Text;

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
                    if (filetype != "jpg" && filetype != "png" && filetype != "jfif" && filetype != "jpeg")
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

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = currentBiome.biomeType;
            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                currentBiome.Save(dialog.FileName);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                currentBiome = Biome.FromFolder(dialog.SelectedPath);
            }

            while (layerChoosers.Count() != 0)
            {
                panel1.Controls.Remove(layerChoosers[0].upperbound_label);
                panel1.Controls.Remove(layerChoosers[0].upperbound_textbox);
                panel1.Controls.Remove(layerChoosers[0].imagesize_textbox);
                panel1.Controls.Remove(layerChoosers[0].uploadImageButton);
                panel1.Controls.Remove(layerChoosers[0].deleteButton);

                Form1.layerChoosers.Remove(layerChoosers[0]);
                y_pos = 40;
            }
            foreach (var layer in currentBiome.colors)
            {
                LayerChooser layerChooser = new LayerChooser(ref y_pos, Reload);
                layerChoosers.Add(layerChooser);
                layerChooser.AddTo(panel1);

                layerChooser.upperbound_textbox.Text = layer.upperbound.ToString();
                layerChooser.image = (Bitmap)layer.bitmap.wrappedBitmap.Clone();
                layerChooser.imagesize_textbox.Text = string.Format("{0},{1}", layerChooser.image.Width, layerChooser.image.Height);
                layerChooser.uploadImageButton.Text = "Auto";
            }
        }

        Rectangle stableposition;
        Point velocity = new Point(0, 0);
        Point position = new Point(0, 0);
        bool changine = false;
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (result != null)
            {
                if (!stableposition.Contains(e.Location))
                {
                    velocity.X = ((Width / 2) - e.Location.X) / 40;
                    velocity.Y = ((Height / 2) - e.Location.Y) / 40;
                }
                else
                {
                    velocity.X = 0;
                    velocity.Y = 0;
                }
                Invalidate();
            }
        }
        private void Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            position = new Point(position.X + velocity.X, position.Y + velocity.Y);
            Invalidate();
        }

        public static List<BiomeChooser> biomeChoosers = new List<BiomeChooser>();
        int yidx = 0;
        private void button8_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            Biome b;
            if (dialog.ShowDialog() != DialogResult.Cancel)
            {
                b = Biome.FromFolder(dialog.SelectedPath);
            }
            else
            {
                return;
            }

            BiomeChooser biomeChooser = new BiomeChooser(yidx, panel2, dialog.SelectedPath.Split("\\").Last(), b, panel1);
            ++yidx;
            biomeChoosers.Add(biomeChooser);
        }

        public class BiomeChooser
        {
            Panel handle;
            Label nameLabel;
            Button deleteButton;
            Panel parent;

            public Biome b;
            List<LayerChooser> layers = new List<LayerChooser>();
            Panel layerPanel;


            public BiomeChooser(int yidx, Panel parent, string name, Biome b, Panel layerpanel)
            {
                handle = new Panel() { Location = new Point(10, 43 + (yidx * 30)), Size = new Size(145, 25), BackColor = Color.FromArgb(100, 100, 100) };
                handle.Click += new EventHandler(BiomeClick);
                nameLabel = new Label() { Location = new Point(0, 5), Text = name, AutoSize = true, ForeColor = Color.White };
                deleteButton = new Button() { Location = new Point(120, 1), Size = new Size(21, 23), Text = "X", ForeColor = parent.ForeColor, BackColor = parent.BackColor };
                handle.Controls.Add(nameLabel);
                handle.Controls.Add(deleteButton);
                parent.Controls.Add(handle);

                this.b = b;
                this.layerPanel = layerpanel;
                this.parent = parent;
            }
            bool showing;
            public void ShowLayers()
            {
                foreach (var biome in Form1.biomeChoosers)
                {
                    if (biome != this)
                    {
                        biome.HideLayers();
                        biome.handle.BackColor = Color.FromArgb(100, 100, 100);
                    }
                }

                showing = true;
                var y_pos = 40;
                foreach (var layer in b.colors)
                {
                    LayerChooser layerChooser = new LayerChooser(ref y_pos, Reload);
                    layers.Add(layerChooser);
                    layerChooser.AddTo(layerPanel);

                    layerChooser.upperbound_textbox.Text = layer.upperbound.ToString();
                    layerChooser.image = (Bitmap)layer.bitmap.wrappedBitmap.Clone();
                    layerChooser.imagesize_textbox.Text = string.Format("{0},{1}", layerChooser.image.Width, layerChooser.image.Height);
                    layerChooser.uploadImageButton.Text = "Auto";
                }
            }
            public void HideLayers()
            {
                if (showing)
                {
                    showing = false;

                    b.colors.Clear();
                    foreach (var layer in layers)
                    {
                        try
                        {
                            string[] points = layer.imagesize_textbox.Text.Split(",");
                            var newsize = new Bitmap(layer.image, new Size(int.Parse(points[0]), int.Parse(points[1])));
                            b.colors.Add(new BiomeLayerData(float.Parse(layer.upperbound_textbox.Text), new BMP(newsize.Clone(new Rectangle(0, 0, newsize.Width, newsize.Height), PixelFormat.Format32bppArgb))));
                        }
                        catch
                        {
                            MessageBox.Show("Layer of upperbound {0} was formatted incorrectly. Was ignored", layer.upperbound_textbox.Text);
                        }
                    }

                    while (layers.Count() != 0)
                    {
                        layerPanel.Controls.Remove(layers[0].upperbound_label);
                        layerPanel.Controls.Remove(layers[0].upperbound_textbox);
                        layerPanel.Controls.Remove(layers[0].imagesize_textbox);
                        layerPanel.Controls.Remove(layers[0].uploadImageButton);
                        layerPanel.Controls.Remove(layers[0].deleteButton);

                        layers.Remove(layers[0]);
                    }
                }
            }

            public void BiomeClick(object sender, EventArgs e)
            {
                if (showing)
                {
                    handle.BackColor = Color.FromArgb(100, 100, 100);
                    HideLayers();
                }
                else
                {
                    handle.BackColor = Color.Blue;
                    ShowLayers();
                }
            }

            public bool Reload()
            {
                var y_pos = 40;
                foreach (var layer in layerChoosers)
                {
                    layer.Reset(ref y_pos);
                }

                return true;
            }
        }
    }
}