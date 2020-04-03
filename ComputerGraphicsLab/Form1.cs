using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ComputerGraphicsLab.Form1.Filters;
namespace ComputerGraphicsLab
{
    public partial class Form1 : Form
    {
        private Bitmap old_image;
        private Bitmap image;

        private int mWidth = 3;
        private int mHeight = 3;
        private int[,] mMatrix = { { 1, 1, 1 }, { 1, 1, 1 }, { 1, 1, 1 } };
        public Form1()
        {
            InitializeComponent();
        }
        public void ChangeMatrix(int [,]matrix)
        {
            mMatrix = matrix;
        }
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void ОткрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files|*.png;*.jpg;*.bmp|All files(*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                image = new Bitmap(dialog.FileName);
                old_image = image;
                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }
        }
        static double  Intensity(Color color)
        {
            return 0.36 * color.R + 0.53 * color.G + 0.11 * color.B;
        }

        private void СохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null) //если в pictureBox есть изображение
            {
                SaveFileDialog savedialog = new SaveFileDialog();
                savedialog.Title = "Сохранить картинку как...";
                //отображать ли предупреждение, если пользователь указывает имя уже существующего файла
                savedialog.OverwritePrompt = true;
                //отображать ли предупреждение, если пользователь указывает несуществующий путь
                savedialog.CheckPathExists = true;
                //список форматов файла, отображаемый в поле "Тип файла"
                savedialog.Filter = "Image Files(*.BMP)|*.BMP|Image Files(*.JPG)|*.JPG|Image Files(*.GIF)|*.GIF|Image Files(*.PNG)|*.PNG|All files (*.*)|*.*";
                //отображается ли кнопка "Справка" в диалоговом окне
                savedialog.ShowHelp = true;
                if (savedialog.ShowDialog() == DialogResult.OK) //если в диалоговом окне нажата кнопка "ОК"
                {
                    try
                    {
                        image.Save(savedialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    catch
                    {
                        MessageBox.Show("Невозможно сохранить изображение", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        internal abstract class Filters
        {
            public virtual Bitmap processImage(Bitmap img, BackgroundWorker worker)
            {

                Bitmap result = new Bitmap(img.Width, img.Height);
                for (int i = 0; i < img.Width; i++)
                {
                    worker.ReportProgress((int)((float)i / result.Width * 100));
                    if (worker.CancellationPending)
                        return null;
                    for (int j = 0; j < img.Height; j++)
                    {
                        result.SetPixel(i, j, calculateNewPixelColor(img, i, j));
                    }

                }
                return result;
            }
            public int Clamp(int value, int min, int max)
            {
                if (value > max)
                    return max;
                if (value < min)
                    return min;
                return value;
            }
            protected abstract Color calculateNewPixelColor(Bitmap img, int x, int y);
        }
        class LinearRastig : Filters
        {
            private double intensityMax = -100000;
            private double intensityMin = 1000000;

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color curColor = img.GetPixel(x, y);
                double intensity = curColor.R * 0.36 + 0.53 * curColor.G + 0.11 * curColor.B;
                double newIntensity = (intensity - intensityMin) * (255.0 / (intensityMax - intensityMin));
                double scale = newIntensity / intensity;
                return Color.FromArgb(
                    Clamp((int)(scale * curColor.R), 0, 255),
                    Clamp((int)(scale * curColor.G), 0, 255),
                    Clamp((int)(scale * curColor.B), 0, 255)
                );
            }

            public override Bitmap processImage(Bitmap img, BackgroundWorker bgWorker)
            {
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        Color curColor = img.GetPixel(i, j);
                        double intensity = curColor.R * 0.36 + 0.53 * curColor.G + 0.11 * curColor.B;
                        if (intensity > intensityMax)
                            intensityMax = intensity;
                        if (intensity < intensityMin)
                            intensityMin = intensity;
                    }
                }
                return base.processImage(img, bgWorker);
            }
        }
        class InvertFilter : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color sourceColor = img.GetPixel(x, y);
                Color resultColor = Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
                return resultColor;
            }
        }
        class GrayScaleFilter : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color sourceColor = img.GetPixel(x, y);
                var intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.144 * sourceColor.B;
                Color resultColor = Color.FromArgb(Clamp((int)intensity, 0, 255), Clamp((int)intensity, 0, 255), Clamp((int)intensity, 0, 255));
                return resultColor;
            }
        }
        class GreyWorld: Filters
        {
            public double avgR;
            public double avgG;
            public double avgB;
            public double avgAll;
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color color = img.GetPixel(x, y);
                return Color.FromArgb(
                    Clamp((int)(color.R * avgAll / avgR), 0, 255),
                    Clamp((int)(color.G * avgAll / avgG), 0, 255),
                    Clamp((int)(color.B * avgAll / avgB), 0, 255)
                );
            }
            public override Bitmap processImage(Bitmap img, BackgroundWorker bgWorker)
            {
                double sumR = 0;
                double sumG = 0;
                double sumB = 0;
                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        Color curColor = img.GetPixel(i, j);
                        sumR += curColor.R;
                        sumG += curColor.G;
                        sumB += curColor.G;
                    }
                }

                avgR = sumR / (img.Width * img.Height);
                avgG = sumG / (img.Width * img.Height);
                avgB = sumB / (img.Width * img.Height);

                avgAll = (avgR + avgB + avgG) / 3;

                return base.processImage(img, bgWorker);
            }
        }
        class Shift : Filters
        {
            double x0, y0;
            public Shift(double x00, double y00)
            {
                x0 = x00; y0 = y00;
            }
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                // Color sourceColor = img.GetPixel(x, y);
                int nX = (int)(x + x0);
                int nY = (int)(y + y0);
                if (nX > img.Width - 1 || nY > img.Height - 1)
                    return Color.Black;
                return img.GetPixel(nX, nY);
            }
        };
        class Turn : Filters
        {
            double alfa;
            public Turn(double _alfa)
            {
                alfa = _alfa;
            }
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {

                double x0 = Convert.ToInt32(img.Width / 2), y0 = Convert.ToInt32(img.Height / 2);
                int nX = Clamp((int)((x - x0) * Math.Cos(alfa) - (y - y0) * Math.Sin(alfa) + x0), 0, img.Width - 1);
                int nY = Clamp((int)((x - x0) * Math.Sin(alfa) + (y - y0) * Math.Cos(alfa) + y0), 0, img.Height - 1);
                if (img.Height > img.Width)
                {
                    int area = (img.Height - img.Width) / 2;
                    if (y < area || y > area + img.Width)
                        return Color.Black;
                }
                else
                {
                    int area = (img.Width - img.Height) / 2;
                    if (x < area || x > area + img.Height)
                        return Color.Black;
                }
                return img.GetPixel(nX, nY);
            }
        }
        class Waves1 : Filters
        {

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color sourceColor = img.GetPixel(x, y);
                int nX = Clamp((int)(x + 20 * Math.Sin((2 * Math.PI * y) / 60)), 0, img.Width - 1);
                int nY = y;
                return img.GetPixel(nX, nY);
            }
        }
        class Waves2 : Filters
        {

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color sourceColor = img.GetPixel(x, y);
                int nX = Clamp((int)(x + 20 * Math.Sin((2 * Math.PI * y) / 30)), 0, img.Width - 1);
                int nY = y;
                return img.GetPixel(nX, nY);
            }
        }
        class Glass : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Random random = new Random();
                double num = random.NextDouble();
                int nX = Clamp((int)(x + (num - 0.5) * 10), 0, img.Width - 1);
                int nY = Clamp((int)(y + (num - 0.5) * 10), 0, img.Height - 1);
                return img.GetPixel(nX, nY);
            }
        }
        class SepiaFilter : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color sourceColor = img.GetPixel(x, y);
                var intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.144 * sourceColor.B;
                double k = 30;
                int R, G, B;
                R = Clamp((int)(intensity + 2 * k), 0, 255);
                G = Clamp((int)(intensity + 0.5 * k), 0, 255);
                B = Clamp((int)(intensity - 1 * k), 0, 255);
                Color resultColor = Color.FromArgb(R, G, B);
                return resultColor;
            }
        }

        class BrightnessFilter : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color sourceColor = img.GetPixel(x, y);
                //int R, G, B;
                int k = 20;
                Color resultColor = Color.FromArgb(Clamp(sourceColor.R + k, 0, 255), Clamp(sourceColor.G + k, 0, 255), Clamp(sourceColor.B + k, 0, 255));
                return resultColor;
            }
        }
        class MatrixFilter : Filters
        {
            protected float[,] kernel = null;
            protected MatrixFilter() { }
            public MatrixFilter(float[,] _kernel)
            {
                kernel = _kernel;
            }
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                int radiusX = kernel.GetLength(0) / 2;
                int radiusY = kernel.GetLength(1) / 2;
                float resR = 0;
                float resG = 0;
                float resB = 0;
                for (int l = -radiusY; l <= radiusY; l++)
                {
                    for (int k = -radiusX; k <= radiusX; k++)
                    {
                        int idX = Clamp(x + k, 0, img.Width - 1);
                        int idY = Clamp(y + l, 0, img.Height - 1);
                        Color neighborColor = img.GetPixel(idX, idY);
                        resR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                        resG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                        resB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                    }
                }
                return Color.FromArgb(Clamp((int)resR, 0, 255), Clamp((int)resG, 0, 255), Clamp((int)resB, 0, 255));
            }

        }
        class MedianFilter : Filters
        {
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                int rad = 2;
                if (x < rad || x >= img.Width - 1 - rad || y < rad || y >= img.Height - 1 - rad)
                    return img.GetPixel(x, y);
                double[] arrR = new double[(rad * 2 + 1) * (rad * 2 + 1)];
                double[] arrB = new double[(rad * 2 + 1) * (rad * 2 + 1)];
                double[] arrG = new double[(rad * 2 + 1) * (rad * 2 + 1)];
                int count = 0;
                for (int i = -rad; i <= rad; i++)
                    for (int j = -rad; j <= rad; j++, count++)
                    {
                        int idX = Clamp(x + i, 0, img.Width - 1);
                        int idY = Clamp(y + j, 0, img.Height - 1);
                        Color nearColor = img.GetPixel(idX, idY);
                        arrR[count] = nearColor.R;
                        arrG[count] = nearColor.G;
                        arrB[count] = nearColor.B;
                    }
                Array.Sort(arrR);
                Array.Sort(arrG);
                Array.Sort(arrB);
                return Color.FromArgb(Clamp((int)arrR[4], 0, 255), Clamp((int)arrG[4], 0, 255), Clamp((int)arrG[4], 0, 255)); ;
            }
        }
        class BlurFilter : MatrixFilter
        {
            public BlurFilter()
            {
                int sizeX = 3;
                int sizeY = 3;
                kernel = new float[sizeX, sizeY];
                for (int i = 0; i < sizeX; i++)
                    for (int j = 0; j < sizeY; j++)
                        kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
            }
        }
        class MotionBlur : MatrixFilter
        {
            public MotionBlur(int n)
            {
                kernel = new float[n, n];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j)
                            kernel[i, j] = 1.0f / n;
                        else
                            kernel[i, j] = 0;
                    }
                }

            }
        }
        class SobelsFilter : MatrixFilter
        {
            private float[,] kerX = null;
            private float[,] kerY = null;
            public SobelsFilter()
            {
                kerY = new float[3, 3]{ { -1f, -2f, -1f },
                                        { 0f, 0f, 0f },
                                        { 1f, 2f, 1f } };
                kerX = new float[3, 3]{ { -1f, 0f, 1f },
                                        { -2f, 0f, 2f },
                                        { -1f, 0f, 1f } };
            }
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                float R, G, B;
                kernel = kerX;
                Color gradX = base.calculateNewPixelColor(img, x, y);
                kernel = kerY;
                Color gradY = base.calculateNewPixelColor(img, x, y);
                R = (float)Math.Sqrt(gradX.R * gradX.R + gradY.R * gradY.R);
                G = (float)Math.Sqrt(gradX.G * gradX.G + gradY.G * gradY.G);
                B = (float)Math.Sqrt(gradX.B * gradX.B + gradY.B * gradY.B);
                return Color.FromArgb(Clamp((int)R, 0, 255), Clamp((int)G, 0, 255), Clamp((int)B, 0, 255));

            }
        }
        class Shara : MatrixFilter
        {
            private float[,] kerX = null;
            private float[,] kerY = null;
            public Shara()
            {
                kerX = new float[3, 3] { { 3f, 0f, -3f },
                                         { 10f, 0f, -10f },
                                         { 3f, 0f, -3f } };
                kerY = new float[3, 3] { { 3f, 10f, 3f },
                                         { 0f, 0f, 0f },
                                         { -3f, -10f, -3f } };
            }
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                float R, G, B;
                kernel = kerX;
                Color gradX = base.calculateNewPixelColor(img, x, y);
                kernel = kerY;
                Color gradY = base.calculateNewPixelColor(img, x, y);
                R = (float)Math.Sqrt(gradX.R * gradX.R + gradY.R * gradY.R);
                G = (float)Math.Sqrt(gradX.G * gradX.G + gradY.G * gradY.G);
                B = (float)Math.Sqrt(gradX.B * gradX.B + gradY.B * gradY.B);
                return Color.FromArgb(Clamp((int)R, 0, 255), Clamp((int)G, 0, 255), Clamp((int)B, 0, 255));
            }
        }
        class Pruitt : MatrixFilter
        {
            private float[,] kerX = null;
            private float[,] kerY = null;
            public Pruitt()
            {
                kerX = new float[3, 3] { { -1f, 0f, 1f }, 
                                         { -1f, 0f, 1f }, 
                                         { -1f, 0f, 1f} };
                kerY = new float[3, 3] { { -1f, -1f, -1f }, 
                                         { 0f, 0f, 0f }, 
                                         { 1f, 1f, 1f } };
            }
            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                float R, G, B;
                kernel = kerX;
                Color gradX = base.calculateNewPixelColor(img, x, y);
                kernel = kerY;
                Color gradY = base.calculateNewPixelColor(img, x, y);
                R = (float)Math.Sqrt(gradX.R * gradX.R + gradY.R * gradY.R);
                G = (float)Math.Sqrt(gradX.G * gradX.G + gradY.G * gradY.G);
                B = (float)Math.Sqrt(gradX.B * gradX.B + gradY.B * gradY.B);
                return Color.FromArgb(Clamp((int)R, 0, 255), Clamp((int)G, 0, 255), Clamp((int)B, 0, 255));
            }
        }
        class SharpnessFilter : MatrixFilter
        {
            public SharpnessFilter()
            {
                kernel = new float[3, 3] { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
            }
        }
        class PrecisionFilter : MatrixFilter
        {
            public PrecisionFilter()
            {
                kernel = new float[3, 3] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };
            }
        }
        class Stamping : MatrixFilter
        {
            public Stamping()
            {
                kernel = new float[3, 3] { { 0, 1, 0 }, { 1, 0, -1 }, { 0, -1, 0 } };
            }
        }
        class GaussianFilter : MatrixFilter
        {
            public void createGaussianKernel(int radius, float sigma)
            {
                int size = 2 * radius + 1;
                kernel = new float[size, size];
                float norm = 0;
                for (int i = -radius; i <= radius; i++)
                    for (int j = -radius; j <= radius; j++)
                    {
                        kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                        norm += kernel[i + radius, j + radius];
                    }
                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                        kernel[i, j] = kernel[i, j] / norm;
            }
            public GaussianFilter()
            {
                createGaussianKernel(7, 2);
            }
        }
        class Dilation : Filters
        {
            private int _kwidth;
            private int _kheight;
            private int[,] _kmatrix;

            public Dilation(int w, int h, int[,] k)
            {
                _kwidth = w;
                _kheight = h;
                _kmatrix = k;
            }

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {

                Color max = Color.Black;
                double maxIntensity = -100000;
                for (int j = -_kheight / 2; j <= _kheight / 2; j++)
                {
                    for (int i = -_kwidth / 2; i <= _kwidth / 2; i++)
                    {
                        int nx = Clamp(x + i, 0, img.Width - 1);
                        int ny = Clamp(y + j, 0, img.Height - 1);

                        if ((_kmatrix[_kwidth / 2 + i, (_kheight / 2) + j] != 0) && ((Intensity(img.GetPixel(nx, ny))) > maxIntensity))
                        {
                            max = img.GetPixel(nx, ny);
                            maxIntensity = Intensity(max);
                        }
                    }
                }

                return max;
            }
        }
        class Erosion : Filters
        {
            private int _kwidth;
            private int _kheight;
            private int[,] _kmatrix;

            public Erosion(int w, int h, int[,] k)
            {
                _kwidth = w;
                _kheight = h;
                _kmatrix = k;
            }

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color min = Color.Black;
                double minIntensity = 100000;
                for (int j = -_kheight / 2; j <= _kheight / 2; j++)
                {
                    for (int i = -_kwidth / 2; i <= _kwidth / 2; i++)
                    {
                        int nx = Clamp(x + i, 0, img.Width - 1);
                        int ny = Clamp(y + j, 0, img.Height - 1);
                        if ((_kmatrix[_kwidth / 2 + i, _kheight / 2 + j] != 0) &&
                            (Intensity(img.GetPixel(nx, ny)) < minIntensity))
                        {
                            min = img.GetPixel(nx, ny);
                            minIntensity = Intensity(min);
                        }
                    }
                }
                return min;
            }
        }
        class Opening : Filters
        {
            private Dilation dilationFilter;
            private Erosion erosionFilter;

            public Opening(int w, int h, int[,] k)
            {

                dilationFilter = new Dilation(w, h, k);
                erosionFilter = new Erosion(w, h, k);
            }

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                if (img == null)
                {
                    throw new ArgumentNullException(nameof(img));
                }

                return Color.White;
            }

            public override Bitmap processImage(Bitmap img, BackgroundWorker worker)
            {
                Bitmap res = erosionFilter.processImage(img, worker);
                Bitmap finalRes = dilationFilter.processImage(res, worker);

                return finalRes;
            }
        }
        class Closings : Filters
        {
            private Dilation dilationFilter;
            private Erosion erosionFilter;

            public Closings(int w, int h, int[,] k)
            {

                dilationFilter = new Dilation(w, h, k);
                erosionFilter = new Erosion(w, h, k);
            }

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                return Color.White;
            }

            public override Bitmap processImage(Bitmap img, BackgroundWorker bgWorker)
            {
                Bitmap res = dilationFilter.processImage(img, bgWorker);
                Bitmap finalRes = erosionFilter.processImage(res, bgWorker);

                return finalRes;
            }
        }
        class TopHat : Filters
        {
            private Bitmap openedImage;

            private int _kwidth;
            private int _kheight;
            private int[,] _kmatrix;

            public TopHat(int w, int h, int[,] k)
            {
                _kwidth = w;
                _kheight = h;
                _kmatrix = k;
            }

            protected override Color calculateNewPixelColor(Bitmap img, int x, int y)
            {
                Color color = openedImage.GetPixel(x, y);
                if (color.R >= 250 && color.G >= 250 && color.B >= 250)
                {
                    return Color.Black;
                }

                return img.GetPixel(x, y);
            }


            public override Bitmap processImage(Bitmap img, BackgroundWorker bgWorker)
            {
                Opening of = new Opening(_kwidth, _kheight, _kmatrix);
                openedImage = of.processImage(img, bgWorker);

                return base.processImage(img, bgWorker);
            }
        }
        private void PictureBox1_Click(object sender, EventArgs e)
        {

        }
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap newImage = ((Filters)e.Argument).processImage(image, backgroundWorker1);
            if (backgroundWorker1.CancellationPending != true)
                image = newImage;
        }
        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }
            progressBar1.Value = 0;
        }
        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }
        private void ИнверсияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new InvertFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }
        private void ОттенокСерогоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GrayScaleFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }
        private void РазмытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new BlurFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void РазмытиеПоГToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GaussianFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }
        private void СепияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new SepiaFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void УвеличитьЯркостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new BrightnessFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void ФильтрСобеляToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filterX = new SobelsFilter();
            backgroundWorker1.RunWorkerAsync(filterX);
        }

        private void РезкостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new SharpnessFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void ТиснениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new Stamping();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void ДругаяРезкостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new PrecisionFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void MotionBlurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new MotionBlur(5);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void ПереносToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new Shift(50, 0);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void ПоворотНа90ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new Turn(Math.PI / 2);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void Волны1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new Waves1();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void Волны2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new Waves2();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void СтеклоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new Glass();
            backgroundWorker1.RunWorkerAsync(filter);
        }
        private void ОператорЩарраToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Filters filter1 = new Shara();
            backgroundWorker1.RunWorkerAsync(filter1);
        }

        private void ОператорПрюиттаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter1 = new Pruitt();
            backgroundWorker1.RunWorkerAsync(filter1);
        }


        private void МедианныйФильтрToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter1 = new MedianFilter();
            backgroundWorker1.RunWorkerAsync(filter1);
        }

        private void СерыйМирToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GreyWorld();
            backgroundWorker1.RunWorkerAsync(filter);
        }
        private void ЛинейноеРастяжениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new LinearRastig();
            backgroundWorker1.RunWorkerAsync(filter);
        }
        private void НазадToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = old_image;
        }

        private void ВпередToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = image;
        }

        private void РасширениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dilation filter = new Dilation(mWidth, mHeight, mMatrix);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void СужениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Erosion filter = new Erosion(mWidth, mHeight, mMatrix);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void ОткрытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Opening filter = new Opening(mWidth, mHeight, mMatrix);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void ЗакрытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Closings filter = new Closings(mWidth, mHeight, mMatrix);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void TopHatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TopHat filter = new TopHat(mWidth, mHeight, mMatrix);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void СтруктурныйЭлементToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
        }
    }
}
