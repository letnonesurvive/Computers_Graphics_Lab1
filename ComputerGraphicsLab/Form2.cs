using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComputerGraphicsLab
{
    public partial class Form2 : Form
    {
        private int width;
        private int height;
        private int[,] matrix;


        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            width = Convert.ToInt32(textBox1.Text);
            height= Convert.ToInt32(textBox2.Text);
            dataGridView1.RowCount = height;
            dataGridView1.ColumnCount = width;
            matrix = new int[height,width];
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                    matrix[i, j] = Convert.ToInt32(dataGridView1[j, i].Value);
            }
            Form1 form1=new Form1();
            form1.ChangeMatrix(matrix);
        }
    }
}
