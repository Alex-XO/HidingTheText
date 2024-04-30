using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HidingTheText
{
    public partial class Form1 : Form
    {
        int count_wr = 0;
        int W;
        int H;
        string code = "55555";

        private void button2_Click(object sender, EventArgs e)
        {
            count_wr = 0;
            Bitmap bmp = new Bitmap(label1.Text);
            H = bmp.Height / 8;
            W = bmp.Width / 8;
            byte[] m = Encoding.GetEncoding(1251).GetBytes(code);
            for (int i = 0; i < m.Length; i++)
                if (!SaveByte(bmp, m[i]))
                {
                    MessageBox.Show("Ошибка сохранения метки");
                    return;
                }
            m = BitConverter.GetBytes(textBox1.Text.Length);
            for (int i = 0; i < m.Length; i++)
                if (!SaveByte(bmp, m[i]))
                {
                    MessageBox.Show("Ошибка сохранения метки");
                    return;
                }
            m = Encoding.GetEncoding(1251).GetBytes(textBox1.Text);
            for (int i = 0; i < m.Length; i++)
                if (!SaveByte(bmp, m[i]))
                {
                    MessageBox.Show("Ошибка сохранения метки");
                    return;
                }
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            { 
                bmp.Save(saveFileDialog1.FileName); 
            }
               
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            pictureBox1.Load(openFileDialog1.FileName);
            label1.Text = openFileDialog1.FileName;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            count_wr = 0;
            Bitmap bmp = new Bitmap(label1.Text);
            H = bmp.Height / 8;
            W = bmp.Width / 8;
            byte[] m = new byte[5];
            for (int i = 0; i < 5; i++)
            {
                ReadByte(bmp, out m[i]);
            }
            string s = Encoding.GetEncoding(1251).GetString(m, 0, m.Length);
            for (int i = 0; i < 4; i++)
            {
                ReadByte(bmp, out m[i]);
            }
            int count_text = BitConverter.ToInt32(m, 0);
            m = new byte[count_text];
            for (int i = 0; i < count_text; i++)
            {
                ReadByte(bmp, out m[i]);
            }
            s = Encoding.GetEncoding(1251).GetString(m, 0, m.Length);
            MessageBox.Show(s + "-" + count_text.ToString());
            textBox1.Text = s;
        }

        bool GetCoor(out int row, out int col)
        {
            row = (count_wr / H) * 8;
            col = (count_wr % W) * 8;
            return row < H;
        }
        bool SaveByte(Bitmap bmp, byte b)
        {
            for (int i = 0; i < 8; i++)
            {
                int bit = (b >> i) & 1;
                if (!SaveBit(bmp, (byte)(bit)))
                {
                    return false;
                }
            }
            return true;
        }
        bool SaveBit(Bitmap bmp, byte bit)
        {
            int P = 450;
            byte[,] S_matr = new byte[8, 8];
            int row, col;
            if (!GetCoor(out row, out col))
            {
                return false;
            }
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    S_matr[i, j] = bmp.GetPixel(j + col, i + row).B;
            double[,] d_matr = new double[8, 8];
            DCT(S_matr, d_matr);
            double d43 = d_matr[4, 3];
            double d34 = d_matr[3, 4];
            double d43_a = Math.Abs(d43);
            double d34_a = Math.Abs(d34);
            if (bit == 0 && ((d43_a - d34_a) <= P))
            {
                d43_a = d34_a + P + 1;
            }
            if (bit == 1 && ((d43_a - d34_a) >= -P))
            {
                d34_a = d43_a + P + 1;
            }
            if (d43 < 0)
                d43_a = -d43_a;
            if (d34 < 0)
                d34_a = -d34_a;
            d_matr[4, 3] = d43_a;
            d_matr[3, 4] = d34_a;
            CDT(d_matr, S_matr);
            FillImage(S_matr, bmp);
            count_wr++;

            return true;
        }
        void FillImage(byte[,] bl, Bitmap bmp)
        {
            int row, col;
            GetCoor(out row, out col);
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    Color a = bmp.GetPixel(j + col, i + row);
                    Color new_a = Color.FromArgb(a.A, a.R, a.G, bl[i, j]);
                    bmp.SetPixel(j + col, i + row, new_a);
                }
        }
        void CDT(double[,] dd, byte[,] bl)
        {
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                {
                    int r = GetCoeffCDT(i, j, dd);
                    if (r < 0)
                        r = 0;
                    if (r > 255)
                        r = 255;
                    bl[i, j] = (byte)(r);
                }
        }
        int GetCoeffCDT(int x, int y, double[,] dd)
        {

            double d43 = 0.25;
            double s = 0.0;
            double d2 = 1 / Math.Sqrt(2.0);
            for (int v = 0; v < 8; v++)
                for (int u = 0; u < 8; u++)
                {
                    double k = 1.0;
                    if (v == 0)
                        k = k * d2;
                    if (u == 0)
                        k = k * d2;
                    s = s + k * dd[v, u] * Math.Cos(Math.PI * v * (2 * x + 1) / 16.0) * Math.Cos(Math.PI * u * (2 * y + 1) / 16.0);
                }

            return (int)(s * d43);
        }
        void DCT(byte[,] bl, double[,] dd)
        {
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    dd[i, j] = GetCoeffDCT(i, j, bl);
        }
        double GetCoeffDCT(int v, int u, byte[,] bl)
        {

            double d43 = 0.25;
            double s = 0.0;
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                {
                    s = s + bl[x, y] * Math.Cos(Math.PI * v * (2 * x + 1) / 16) * Math.Cos(Math.PI * u * (2 * y + 1) / 16);
                }
            double d2 = 1 / Math.Sqrt(2.0);
            if (v == 0)
                s = s * d2;
            if (u == 0)
                s = s * d2;
            return s * d43;
        }
        bool ReadByte(Bitmap bmp, out byte b)
        {
            byte res = 0;
            byte bit = 0;
            b = 0;
            for (int i = 0; i < 8; i++)
            {
                if (!ReadBit(bmp, out bit))
                    return false;
                res = (byte)(res | (bit << i));

            }
            b = res;
            return true;
        }
        bool ReadBit(Bitmap bmp, out byte bit)
        {
            bit = 1;
            byte[,] S_matr = new byte[8, 8];
            int row, col;
            if (!GetCoor(out row, out col))
            {
                return false;
            }
            for (int i = 0; i < 8; i++)
                for (int j = 0; j < 8; j++)
                    S_matr[i, j] = bmp.GetPixel(j + col, i + row).B;
            double[,] d_matr = new double[8, 8];
            DCT(S_matr, d_matr);
            double d43 = d_matr[4, 3];
            double d34 = d_matr[3, 4];
            double d43_a = Math.Abs(d43);
            double d34_a = Math.Abs(d34);
            if (d43_a > d34_a)
                bit = 0;
            else
                bit = 1;
            count_wr++;
            return true;
        }
    }
}
