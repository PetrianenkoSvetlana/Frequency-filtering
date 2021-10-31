using System;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using AForge.Imaging;

namespace Lab4
{
    public partial class Form1 : Form
    {
        Image<Bgr, byte> inputImage = null;
        int imageWidth = 0, imageHeight = 0;

        Label[] labels = new Label[30];
        PictureBox[] pictureBoxes = new PictureBox[30];
        Label[] labels2 = new Label[30];
        PictureBox[] pictureBoxes2 = new PictureBox[30];

        public Form1()
        {
            InitializeComponent();
        }

        private void btnReview_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK)
                {
                    tbPath.Text = openFileDialog1.FileName;
                    btnCalculate_Click(this, null);
                }
                else
                    MessageBox.Show("Файл не выбран", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < pictureBoxes.Length; i++)
            {
                PictureBox pictureBox = new PictureBox
                {
                    Size = new Size(273, 135),
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                pictureBoxes[i] = pictureBox;

                Label label = new Label
                {
                    AutoSize = true,
                    Font = new Font("Microsoft Sans Serif", 8f),
                    TextAlign = ContentAlignment.TopCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                labels[i] = label;
                tableLayoutPanel1.Controls.Add(pictureBoxes[i], i % 10, i / 10 * 2);
                tableLayoutPanel1.Controls.Add(labels[i], i % 10, i / 10 * 2 + 1);
            }

            for (int i = 0; i < pictureBoxes2.Length; i++)
            {
                PictureBox pictureBox2 = new PictureBox
                {
                    Size = new Size(273, 135),
                    SizeMode = PictureBoxSizeMode.Zoom
                };
                pictureBoxes2[i] = pictureBox2;

                Label label2 = new Label
                {
                    AutoSize = true,
                    Font = new Font("Microsoft Sans Serif", 8f),
                    TextAlign = ContentAlignment.TopCenter,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                labels2[i] = label2;
                tableLayoutPanel2.Controls.Add(pictureBoxes2[i], i % 10, i / 10 * 2);
                tableLayoutPanel2.Controls.Add(labels2[i], i % 10, i / 10 * 2 + 1);
            }
        }

        private double Distance(int i, int j)
        {
            return Math.Sqrt(Math.Pow(imageWidth / 2 - i, 2) + Math.Pow(imageHeight / 2 - j, 2));
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            /* Первоначальные настройки изображения */
            Bitmap bitmapTemp = new Bitmap(tbPath.Text);
            imageWidth = (int)Math.Pow(2, (int)Math.Log(bitmapTemp.Width, 2)); //ширина и высота картинки должны быть ровны 2 в степени для БПФ
            imageHeight = (int)Math.Pow(2, (int)Math.Log(bitmapTemp.Height, 2));
            inputImage = new Image<Bgr, byte>(new Bitmap(bitmapTemp, imageWidth, imageHeight));

            /* Исходное изображение */
            pictureBox1.Image = inputImage.Bitmap;

            /* Оттенки серого*/
            Image<Gray, float> imageGray = new Image<Gray, float>(inputImage.Bitmap);
            pictureBox2.Image = imageGray.ToBitmap();

            /* Спектр изображения */
            ComplexImage complexImage = ComplexImage.FromBitmap(imageGray.ToBitmap());
            complexImage.ForwardFourierTransform();
            pictureBox3.Image = complexImage.ToBitmap();

            /* Логарифрированный спектор изображения */
            Image<Gray, byte> imageComplexImage = new Image<Gray, byte>(complexImage.ToBitmap());
            for (int i = 0; i < imageWidth; i++)
                for (int j = 0; j < imageHeight; j++)
                    imageComplexImage[j, i] = new Gray(Math.Log(1 + imageComplexImage[j, i].Intensity));
            CvInvoke.Normalize(imageComplexImage, imageComplexImage, 0, 255, NormType.MinMax);
            pictureBox4.Image = imageComplexImage.Bitmap;

            /* Низкие частоты */
            for (int circleIgeal = 0; circleIgeal <= 4; circleIgeal++)
            {
                /* Спектор изображения c идеальным фильтром */
                ComplexImage complexImageIgeal = (ComplexImage)complexImage.Clone();
                for (int i = 0; i < imageWidth; i++)
                    for (int j = 0; j < imageHeight; j++)
                        if (Distance(i, j) > (circleIgeal * 10 + 5))
                           complexImageIgeal.Data[j, i] *= 0;
                pictureBoxes[circleIgeal * 2].Image = complexImageIgeal.ToBitmap();
                labels[circleIgeal * 2].Text = $"Спектор изображения c идеальным фильтром = {circleIgeal * 10 + 5}";

                /* Изображение с идеальным фильтром */
                complexImageIgeal.BackwardFourierTransform();//обратное БПФ
                pictureBoxes[circleIgeal * 2 + 1].Image = complexImageIgeal.ToBitmap();
                labels[circleIgeal * 2 + 1].Text = $"Изображение с идеальным фильтром = {circleIgeal * 10 + 5}";
            }

            for (int circleButterworth = 0; circleButterworth <= 4; circleButterworth++)
            {
                /* Спектор изображения c фильтром Баттервотта */
                int n = 2;
                ComplexImage complexImageButterworth = (ComplexImage)complexImage.Clone();
                for (int i = 0; i < imageWidth; i++)
                    for (int j = 0; j < imageHeight; j++)
                        complexImageButterworth.Data[j, i] *= 1 / (1 + Math.Pow(Distance(i, j) / (circleButterworth * 10 + 5), 2 * n));
                pictureBoxes[circleButterworth * 2 + 10].Image = complexImageButterworth.ToBitmap();
                labels[circleButterworth * 2 + 10].Text = $"Спектор изображения c фильтром Баттервотта = {circleButterworth * 10 + 5}";

                /* Изображение с фильтром Баттервотта */
                complexImageButterworth.BackwardFourierTransform();
                pictureBoxes[circleButterworth * 2 + 11].Image = complexImageButterworth.ToBitmap();
                labels[circleButterworth * 2 + 11].Text = $"Изображение c фильтром Баттервотта = {circleButterworth * 10 + 5}";
            }

            for (int circleGaussian = 0; circleGaussian <= 4; circleGaussian++)
            {
                /* Спектор изображения c Гауссовским фильтром */
                ComplexImage complexImageGaussian = (ComplexImage)complexImage.Clone();
                for (int i = 0; i < imageWidth; i++)
                    for (int j = 0; j < imageHeight; j++)
                        complexImageGaussian.Data[j, i] *= Math.Exp(-Math.Pow(Distance(i, j), 2) / 2 / Math.Pow(circleGaussian * 10 + 5, 2));
                pictureBoxes[circleGaussian * 2 + 20].Image = complexImageGaussian.ToBitmap();
                labels[circleGaussian * 2 + 20].Text = $"Спектор изображения c Гауссовским фильтром = {circleGaussian * 10 + 5}";

                /* Изображение с Гауссовским фильтром */
                complexImageGaussian.BackwardFourierTransform();
                pictureBoxes[circleGaussian * 2 + 21].Image = complexImageGaussian.ToBitmap();
                labels[circleGaussian * 2 + 21].Text = $"Изображение с Гауссовским фильтром = {circleGaussian * 10 + 5}";
            }


            /* Высокие частоты */
            for (int circleIgeal2 = 0; circleIgeal2 <= 4; circleIgeal2++)
            {
                /* Спектор изображения c идеальным фильтром */
                ComplexImage complexImageIgeal2 = (ComplexImage)complexImage.Clone();
                for (int i = 0; i < imageWidth; i++)
                    for (int j = 0; j < imageHeight; j++)
                        if (Distance(i, j) <= circleIgeal2 * 10 + 5)
                            complexImageIgeal2.Data[j, i] *= 0;
                pictureBoxes2[circleIgeal2 * 2].Image = complexImageIgeal2.ToBitmap();
                labels2[circleIgeal2 * 2].Text = $"Спектор изображения c идеальным фильтром = {circleIgeal2 * 10 + 5}";

                /* Изображение с идеальным фильтром */
                complexImageIgeal2.BackwardFourierTransform();
                pictureBoxes2[circleIgeal2 * 2 + 1].Image = complexImageIgeal2.ToBitmap();
                labels2[circleIgeal2 * 2 + 1].Text = $"Изображение с идеальным фильтром = {circleIgeal2 * 10 + 5}";
            }

            for (int circleButterworth2 = 0; circleButterworth2 <= 4; circleButterworth2++)
            {
                /* Спектор изображения c фильтром Баттервотта */
                int n2 = 2;
                ComplexImage complexImageButterworth2 = (ComplexImage)complexImage.Clone();
                for (int i = 0; i < imageWidth; i++)
                    for (int j = 0; j < imageHeight; j++)
                        complexImageButterworth2.Data[j, i] *= 1 - (1 / (1 + Math.Pow(Distance(i, j) / (circleButterworth2 * 10 + 5), 2 * n2)));
                pictureBoxes2[circleButterworth2 * 2 + 10].Image = complexImageButterworth2.ToBitmap();
                labels2[circleButterworth2 * 2 + 10].Text = $"Спектор изображения c фильтром Баттервотта = {circleButterworth2 * 10 + 5}";

                /* Изображение с фильтром Баттервотта */
                complexImageButterworth2.BackwardFourierTransform();
                pictureBoxes2[circleButterworth2 * 2 + 11].Image = complexImageButterworth2.ToBitmap();
                labels2[circleButterworth2 * 2 + 11].Text = $"Изображение c фильтром Баттервотта = {circleButterworth2 * 10 + 5}";
            }

            for (int circleGaussian2 = 0; circleGaussian2 <= 4; circleGaussian2++)
            {
                /* Спектор изображения c Гауссовским фильтром */
                ComplexImage complexImageGaussian2 = (ComplexImage)complexImage.Clone();
                for (int i = 0; i < imageWidth; i++)
                    for (int j = 0; j < imageHeight; j++)
                        complexImageGaussian2.Data[j, i] *= 1 - Math.Exp(-Math.Pow(Distance(i, j), 2) / 2 / Math.Pow(circleGaussian2 * 10 + 5, 2));
                pictureBoxes2[circleGaussian2 * 2 + 20].Image = complexImageGaussian2.ToBitmap();
                labels2[circleGaussian2 * 2 + 20].Text = $"Спектор изображения c Гауссовским фильтром = {circleGaussian2 * 10 + 5}";

                /* Изображение с Гауссовским фильтром */
                complexImageGaussian2.BackwardFourierTransform();
                pictureBoxes2[circleGaussian2 * 2 + 21].Image = complexImageGaussian2.ToBitmap();
                labels2[circleGaussian2 * 2 + 21].Text = $"Изображение с Гауссовским фильтром = {circleGaussian2 * 10 + 5}";
            }
        }
    }
}