using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WindowsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Int32 V1, V2, V3, V4;
        Double Id1, Id2;
        Int32 Index; //
        Int64[] R = new Int64[7]; //限流电阻R1, R2, R3, R4, R5, R6, R7
        NumericUpDown[] numericUpDown = new NumericUpDown[7];
        String[] str1 ={ "R1", "R2", "R3", "R4", "R5", "R6", "R7" };    //电阻的编号数组
        Decimal[] str2 ={ 4700.0M, 1000.0M, 330.0M, 100.0M, 33.0M, 10.0M, 10000M }; //电阻初值数组
        Int32 T=25;  //温度
        Double IS = 171.34388778833204;   //饱和电流
        Double RD = 160.40886801322117;  //零电阻
        Double N = 1.0693751430533303;   //理想因子
        Double VT = 0.025679647254953728;     //热电压
        Double[] set1 ={ 1.0, 1.0, 1.0, 1.0 };   //IS,RD,N,VT
        Double[] set2 ={ 1.0, 1.0, 1.0, 1.0 };
        Double[] set3 ={ 1.0, 1.0, 1.0, 1.0 };
        Double[] set4 ={ 1.0, 1.0, 1.0, 1.0 };
        Double[] set5 ={ 1.0, 1.0, 1.0, 1.0 };  
        Label[] lb1 = new Label[7];
        Label[] lb2 = new Label[7];
        Label[] lb3 = new Label[6];
        Bitmap tup1;
        Graphics g1;
        private void Form1_Load(object sender, EventArgs e)
        {
            this.listBox1.LostFocus += new System.EventHandler(this.listBox1_LostFocus); //添加失去焦点事件
            this.Controls.Add(panel1);
            panel1.BringToFront();
            panel1.Left = 522;
            panel1.Top = 270;
            panel1.Visible = false;
            for (int i = 0; i < 7; i++)
            {
                numericUpDown[i] = new NumericUpDown();
                numericUpDown[i].Cursor = Cursors.Hand;
                numericUpDown[i].Width = 57;
                numericUpDown[i].Height = 21;
                numericUpDown[i].Top = 33 + 27 * i;
                numericUpDown[i].Left = 102;
                numericUpDown[i].Minimum = 0M;
                if (i == 6)
                {
                    numericUpDown[i].DecimalPlaces = 0;
                    numericUpDown[i].Maximum = 20000M;
                    numericUpDown[i].Increment = 1M;
                }
                else
                {
                    numericUpDown[i].DecimalPlaces = 1;
                    numericUpDown[i].Maximum =10000M;
                    numericUpDown[i].Increment = 0.1M;
                }                  
                this.panel1.Controls.Add(numericUpDown[i]); //添加到控件集合
                numericUpDown[i].BringToFront();
            }
            //读取注册表保存的设置
            if (null == Registry.GetValue(@"HKEY_CURRENT_USER\Software\二极管参数计算\Settings", "R1", ""))
            {
                //第1次创建注册表子项,并将初值数据写入内存和注册表
                for (Int32 i = 0; i < 7; i++)
                {
                    R[i] = Convert.ToInt64(str2[i]);
                    numericUpDown[i].Value = str2[i];
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\二极管参数计算\Settings", str1[i], str2[i]);
                }
            }
            else
            {
                //读取注册表保存的子项内数据,并写入界面
                for (Int32 i = 0; i < 7; i++)
                {
                    numericUpDown[i].Value = Convert.ToDecimal(Registry.GetValue(@"HKEY_CURRENT_USER\Software\二极管参数计算\Settings", str1[i], ""));
                }
            }
            Index = 3;
            comboBox1.SelectedIndex = Index;
            label6.Text = "电阻" + str1[Index] + ":";
            listBox1.SelectedIndex = 1;
            listBox1.Visible = false;
            Drawing_Transfer_Characteristic(); //绘制曲线图形
            System.IO.Directory.CreateDirectory("C:\\Documents and Settings\\User\\My Documents\\二极管参数表"); //创建保存数据的文件夹
        }
        /// <summary>
        /// 计算Is,N,Rd
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            V1 = Convert.ToInt32(1000 * numericUpDown1.Value);
            V2 = Convert.ToInt32(1000 * numericUpDown2.Value);
            V3 = Convert.ToInt32(1000 * numericUpDown3.Value);
            V4 = Convert.ToInt32(1000 * numericUpDown4.Value);
            for (Int32 i = 0; i < 7; i++)
            {
                  R[i] = Convert.ToInt64(1000 * numericUpDown[i].Value);
            }
            for (Int32 i = 0; i < 6; i++)
            {
                R[i] = (R[i] * R[6]) / (R[i] + R[6]);  //并联10M电阻后，修正DVM带来的误差
            }
            T = Convert.ToInt32(numericUpDown5.Value);
            VT = ((273 + T) * 1.380649 / 1.602177) / 10000; //计算热电压
            Id1 = 1000.0 * V2 / R[comboBox1.SelectedIndex] - 1000.0 * V1 / R[6]; //计算二极管电流（单位nA）
            Id2 = 1000.0 * V4 / R[comboBox1.SelectedIndex] - 1000.0 * V3 / R[6];
            Int32 n = 0;
            Double Is1=0, Is2=0;
            for (Int32 i = 0; i < 16; i++)			//16位逐次逼近法
            {
                n |= (1 << (15 - i));				//预置转换位
                N = 0.5 + n / 21845.0;   //N(0.5~3.5)
                Is1 = Id1 / (Math.Exp(V1 / (VT * 1000000 * N)) - 1);
                Is2 = Id2 / (Math.Exp(V3 / (VT * 1000000 * N)) - 1);
                if (Is1 > Is2)						//
                {
                    n &= ~(1 << (15 - i));			//清零该位
                    N = 0.5 + n / 21845.0;
                }				
            }
            if (Is1 > Is2)
            {
                if ((Is1 - Is2)/Is1 > 0.001)
                    MessageBox.Show("方程无解！"); //误差超过0.1%，表示方程无法解出
            }
            else
            {
                if ((Is2 - Is1) /Is2> 0.001)
                    MessageBox.Show("方程无解！"); //显示错误
            }
            IS = (Is1 + Is2) / 2;
            RD=1000000 * (VT * N / IS);
            label26.Text = Convert.ToString(Convert.ToUInt32(IS));
            label27.Text = Convert.ToString(Convert.ToUInt32(RD));
            label28.Text = String.Format("{0:0.00}", N);
            label16.Text = String.Format("{0:样本数据1: Vd1=0.0 mV}", Convert.ToDouble(numericUpDown1.Value));
            label38.Text = String.Format("{0:Id1=0 nA}", Id1);
            label17.Text = String.Format("{0:样本数据2: Vd2=0.0 mV}", Convert.ToDouble(numericUpDown3.Value));
            label39.Text = String.Format("{0:Id2=0 nA}", Id2);
            label18.Text = String.Format("{0:热电压:    VT=0.0 mV}", VT * 1000);
            label29.Text = Convert.ToString(numericUpDown5.Value) + "℃";
            Drawing_Transfer_Characteristic(); //绘制转移特性曲线
        }
       /// <summary>
       /// 绘制二极管转移特性函数
       /// </summary>
       /// <param name="x">电压</param>
       /// <param name="y">电流</param>
        private void Drawing_Transfer_Characteristic()
        {
            tup1 = new Bitmap(201, 201);
            g1 = Graphics.FromImage(tup1);
            String[] str = new String[] { "正", "向", "电", "流", "IF", " " };
            //写垂直数字刻度
            for (int i = 0; i < 6; i++)
            {
                lb1[i] = new Label();
                lb1[i].BackColor = this.tabPage5.BackColor; //背景颜色(取底色)
                lb1[i].Width = 30;
                lb1[i].Height = 15;
                lb1[i].TextAlign = ContentAlignment.MiddleRight;
                if (i == 5)
                    lb1[i].Top = 20 + (40 * i - 3);
                else 
                    lb1[i].Top = 20 + 40 * i;
                lb1[i].Left = pictureBox1.Left - lb1[i].Width;
                lb1[i].Font = new Font("宋体", 9, FontStyle.Regular);
                lb1[i].Text = Convert.ToString((5 - i) * Convert.ToUInt32(numericUpDown6.Value) / 5);
                this.tabPage5.Controls.Add(lb1[i]); //添加到控件集合
                lb1[i].BringToFront();
            }
            for (int i = 0; i < 6; i++)
            {
                lb3[i] = new Label();
                lb3[i].BackColor = this.tabPage5.BackColor; //背景颜色(取底色)
                lb3[i].Width = 35;
                lb3[i].Height = 15;
                lb3[i].TextAlign = ContentAlignment.MiddleCenter;
                lb3[i].ForeColor = Color.Green;
                lb3[i].Top = 80 + 15 * i;
                lb3[i].Left = 0;
                lb3[i].Font = new Font("宋体", 9, FontStyle.Bold);
                if (i == 5)
                    lb3[i].Text = "(" + button2.Text.Substring(0, 2) + ")";
                else
                    lb3[i].Text = str[i];
                this.tabPage5.Controls.Add(lb3[i]); //添加到控件集合
                lb3[i].BringToFront();
            }
            //写水平数字刻度
            for (int i = 0; i < 7; i++)
            {
                lb2[i] = new Label();
                lb2[i].BackColor = this.tabPage5.BackColor; //背景颜色(取底色)
                if (i != 6)
                {
                    lb2[i].Width = 30;
                    lb2[i].Height = 15;
                    lb2[i].Top = 235;
                    lb2[i].Left = 47 + 40 * i;  
                    lb2[i].Text = Convert.ToString(i *Convert.ToUInt32(numericUpDown7.Value) / 5);
                    lb2[i].Font = new Font("宋体", 9, FontStyle.Regular);
                    lb2[i].ForeColor = Color.Black;
                }
                else
                {
                    lb2[i].Width = 100;
                    lb2[i].Height = 15;
                    lb2[i].Top = 255;
                    lb2[i].Left =120;
                    lb2[i].Text = "正向电压VF(mV)";
                    lb2[i].Font = new Font("宋体", 9, FontStyle.Bold);
                    lb2[i].ForeColor = Color.Green;
                }
                lb2[i].TextAlign = ContentAlignment.MiddleCenter;              
                this.tabPage5.Controls.Add(lb2[i]); //添加到控件集合
                lb2[i].BringToFront();
            }
            g1.Clear(Color.AliceBlue);
            //横线
            for (int j = 0; j <= 200; j += 20)
            {
                g1.DrawLine(new Pen(Color.Gainsboro), 0, j, 200, j);
            }
            //竖线
            for (int j = 0; j <= 200; j += 20)
            {
                g1.DrawLine(new Pen(Color.Gainsboro), j, 0, j, 200);
            }
            if (checkBox1.Checked == true)   //绘制叠加1
            {
                Draw_Line(set1[0], set1[2], set1[3], button5.BackColor);
            }
            if (checkBox2.Checked == true)   //绘制叠加2
            {
                Draw_Line(set2[0], set2[2], set2[3], button6.BackColor);
            }
            if (checkBox3.Checked == true)   //绘制叠加3
            {
                Draw_Line(set3[0], set3[2], set3[3], button7.BackColor);
            }
            if (checkBox4.Checked == true)   //绘制叠加4
            {
                Draw_Line(set4[0], set4[2], set4[3], button8.BackColor);
            }
            if (checkBox5.Checked == true)   //绘制叠加5
            {
                Draw_Line(set5[0], set5[2], set5[3], button9.BackColor);
            }
            Draw_Line(IS, N, VT, Color.Green);  //绘制当前二极管伏安曲线
            pictureBox1.Image = tup1;
        }
        private void Draw_Line(Double Is, Double n, Double vt, Color color) //绘制二极管伏安线函数
        {
            g1.DrawImage(tup1, 0, 0);
            Double I1, I2, Vd; ;
            Int32 k = Convert.ToInt32(Convert.ToString(listBox1.SelectedItem).Substring(4));
            for (int V = 0; V < 200; V++)
            {
                Vd = (V / 200.0) * Convert.ToUInt32(numericUpDown7.Value);   //mV             
                I1 = 200 * (Is * (Math.Exp(Vd / (1000 * vt * n)) - 1)) / (Convert.ToInt32(numericUpDown6.Value) * k); //nA
                if (I1 > 200)
                    I1 = 200;
                Vd = ((V + 1) / 200.0) * Convert.ToUInt32(numericUpDown7.Value);  //mV
                I2 = 200 * (Is * (Math.Exp(Vd / (1000 * vt * n)) - 1)) / (Convert.ToInt32(numericUpDown6.Value) * k); //nA
                if (I2 > 200)
                    I2 = 200;
                g1.DrawLine(new Pen(color), (float)V, (float)(200 - I1), (float)(V + 1), (float)(200 - I2));
                if ((I1 == 200) | (I2 == 200))
                    break;
            }
        }
        /// <summary>
        /// 绘图XY坐标单位调整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e) 
        {
            listBox1.Visible = true; //显示电流单位列表
            listBox1.Focus();  //设置listBox1控件焦点
        }
        private void listBox1_LostFocus(object sender, EventArgs e)
        {
            listBox1.Visible = false;  //失去焦点隐藏
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Text = listBox1.Text;
            listBox1.Visible = false;
            Drawing_Transfer_Characteristic();
        }
        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();
        }
        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();
        }
        /// <summary>
        /// 保存二极管参数文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "文本文件（*.txt）|*.txt|全部文件|*.*";
            saveFileDialog1.FilterIndex = 1; //指定第1个过滤器（默认的打开的方法）
            saveFileDialog1.InitialDirectory = "C:\\Documents and Settings\\User\\My Documents\\二极管参数表";  //打开起始目录
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.Delete(saveFileDialog1.FileName); //首先删除文件
                StreamWriter MyWriter = new StreamWriter(new FileStream(saveFileDialog1.FileName, FileMode.Append, FileAccess.Write)); //若无则创建新文件
                MyWriter.WriteLine(Convert.ToString(numericUpDown5.Value));  //写入温度
                MyWriter.WriteLine(Convert.ToString(IS));  //写入饱和电流IS
                MyWriter.WriteLine(Convert.ToString(RD));    //写入RD
                MyWriter.WriteLine(Convert.ToString(N));    //写入N
                MyWriter.WriteLine(Convert.ToString(VT));    //写入VT
                MyWriter.WriteLine("存放顺序第一行开始；温度T，饱和电流IS，零电阻RD，理想因子N，热电压VT");    //写入注解
                MyWriter.Close(); //关闭文件
            }
        }
        /// <summary>
        /// 读取二极管数据文件到内存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog(1);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog(2);
        }
        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog(3);
        }
        private void button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog(4);
        }
        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog(5);
        }
        private void OpenFileDialog(Int32 k)   //读数据文件到数组
        {
            openFileDialog1.Filter = "文本文件（*.txt）|*.txt|全部文件|*.*";
            openFileDialog1.FilterIndex = 1; //指定第1个过滤器（默认的打开的方法）
            openFileDialog1.InitialDirectory = "C:\\Documents and Settings\\User\\My Documents\\二极管参数表";  //打开起始目录
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamReader MyWriter = new StreamReader(new FileStream(openFileDialog1.FileName, FileMode.OpenOrCreate, FileAccess.Read)); //若无则创建新文件
                String n0 = Path.GetFileNameWithoutExtension(openFileDialog1.FileName) + "(" + MyWriter.ReadLine() + "℃)"; //读出文件名和温度
                Double n1 = Convert.ToDouble(MyWriter.ReadLine());  //读IS饱和电流
                Double n2 = Convert.ToDouble(MyWriter.ReadLine());  //读RD
                Double n3 = Convert.ToDouble(MyWriter.ReadLine());  //读N
                Double n4 = Convert.ToDouble(MyWriter.ReadLine());  //读VT
                MyWriter.Close(); //关闭文件 
                if ((n1 == 0.0) | (n3 == 0.0) | (n4 == 0.0))
                {
                    MessageBox.Show("二极管数据文件格式不正确", "警告！");
                }
                else
                {
                    switch (k)
                    {
                        case 1:
                            checkBox1.Text = n0;
                            set1[0] = n1;
                            set1[1] = n2;
                            set1[2] = n3;
                            set1[3] = n4;
                            checkBox1.Enabled = true;
                            break;
                        case 2:
                            checkBox2.Text = n0;
                            set2[0] = n1;
                            set2[1] = n2;
                            set2[2] = n3;
                            set2[3] = n4;
                            checkBox2.Enabled = true;
                            break;
                        case 3:
                            checkBox3.Text = n0;
                            set3[0] = n1;
                            set3[1] = n2;
                            set3[2] = n3;
                            set3[3] = n4;
                            checkBox3.Enabled = true;
                            break;
                        case 4:
                            checkBox4.Text = n0;
                            set4[0] = n1;
                            set4[1] = n2;
                            set4[2] = n3;
                            set4[3] = n4;
                            checkBox4.Enabled = true;
                            break;
                        case 5:
                            checkBox5.Text = n0;
                            set5[0] = n1;
                            set5[1] = n2;
                            set5[2] = n3;
                            set5[3] = n4;
                            checkBox5.Enabled = true;
                            break;
                    }
                }   
            }
        }
        /// <summary>
        /// 复选框叠加绘图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();
            if (checkBox1.Checked == true)
                radioButton3.Enabled = true;
            else
            {
                radioButton3.Enabled = false;
                if (radioButton3.Checked == true)
                    radioButton8.Checked = true;
            }
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();
            if (checkBox2.Checked == true)
                radioButton4.Enabled = true;
            else
            {
                radioButton4.Enabled = false;
                if (radioButton4.Checked == true)
                    radioButton8.Checked = true;
            }
        }
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();
            if (checkBox3.Checked == true)
                radioButton5.Enabled = true;
            else
            {
                radioButton5.Enabled = false;
                if (radioButton5.Checked == true)
                    radioButton8.Checked = true;
            }
        }
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();
            if (checkBox4.Checked == true)
                radioButton6.Enabled = true;
            else
            {
                radioButton6.Enabled = false;
                if (radioButton6.Checked == true)
                    radioButton8.Checked = true;
            }
        }
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();
            if (checkBox5.Checked == true)
                radioButton7.Enabled = true;
            else
            {
                radioButton7.Enabled = false;
                if (radioButton7.Checked == true)
                    radioButton8.Checked = true;
            }
        }
        /// <summary>
        /// 鼠标指针即时Rd显示数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {          
            Int32 Rd=0;
            Double Is,n,vt;
            Bitmap tup2 = new Bitmap(201, 201);
            Graphics g2 = Graphics.FromImage(tup2);
            Pen pen;
            Brush brush1;
            Brush brush2 = new SolidBrush(pictureBox1.BackColor);
            //判断跟踪曲线
            if (radioButton3.Enabled & radioButton3.Checked)
            {
                pen = new Pen(button5.BackColor);
                brush1 = new SolidBrush(button5.BackColor);
                Is = set1[0];
                n = set1[2];
                vt = set1[3];
            }
            else if (radioButton4.Enabled & radioButton4.Checked)
            {
                pen = new Pen(button6.BackColor);
                brush1 = new SolidBrush(button6.BackColor);
                Is = set2[0];
                n = set2[2];
                vt = set2[3];
            }
            else if (radioButton5.Enabled & radioButton5.Checked)
            {
                pen = new Pen(button7.BackColor);
                brush1 = new SolidBrush(button7.BackColor);
                Is = set3[0];
                n = set3[2];
                vt = set3[3];
            }
            else if (radioButton6.Enabled & radioButton6.Checked)
            {
                pen = new Pen(button8.BackColor);
                brush1 = new SolidBrush(button8.BackColor);
                Is = set4[0];
                n = set4[2];
                vt = set4[3];
            }
            else if (radioButton7.Enabled & radioButton7.Checked)
            {
                pen = new Pen(button9.BackColor);
                brush1 = new SolidBrush(button9.BackColor);
                Is = set5[0];
                n = set5[2];
                vt = set5[3];
            }
            else
            {
                pen = new Pen(Color.Green);
                brush1 = new SolidBrush(Color.Green);
                Is = IS;
                n = N;
                vt = VT;
            }
            Double Vd=(e.X / 200.0) * Convert.ToUInt32(numericUpDown7.Value);    //正向电压mV
            Double Id = Is * (Math.Exp(Vd / (1000 * vt * n)) - 1); //正向电压对于的正向电流nA
            Int32 k = Convert.ToInt32(Convert.ToString(listBox1.SelectedItem).Substring(4)); //纵坐标单位
            Int32 I = (int)(200 * (Id / (Convert.ToInt32(numericUpDown6.Value) * k))); //纵坐标
            if (I > 200) I = 200;
            if (Id == 0.0) Id = 1;
            //鼠标靠近曲线悬停字符串显示
            if (((e.Y - (200 - I)) < 5)&(( (200 - I)- e.Y) <5))
            {
                Rd = (Convert.ToInt32(1000000000 * ((n * vt / Id) * Math.Log((Id / Is) + 1, Math.E))));    //计算动态电阻Rd
                String str;
                if (Rd < 1000)
                    str = "Rd=" + Rd.ToString() + "Ω";
                else
                {
                    Rd = Rd / 1000;
                    str = "Rd=" + Rd.ToString() + "kΩ";
                }
                //绘制悬停字符串
                g2.DrawImage(tup1, 0, 0);  //复制
                int x1 = 0;
                int y1 = 0;
                SizeF sizeF = g2.MeasureString(str, new Font("宋体", 9, FontStyle.Regular));//测量字符串长度
                if (e.X > (int)sizeF.Width) //区分在鼠标左或右显示字符串
                    x1 = e.X - (int)sizeF.Width;
                else
                    x1 = e.X + 5;
                if (e.Y < (int)sizeF.Height)//区分在鼠标上或下显示字符串
                    y1 = e.Y;
                else
                    y1 = e.Y - (int)sizeF.Height;
                g2.DrawString(str, new Font("宋体", 9, FontStyle.Regular),brush1, x1, y1);
                g2.FillEllipse(brush2, e.X - 2, (200 - I) - 2, 4, 4);  //绘制实心圆
                g2.DrawEllipse(pen, e.X - 2, (200 - I) - 2, 4, 4); //绘制圆圈
                
                pictureBox1.Image = tup2;
            }
            else
            {
                pictureBox1.Image = tup1;//回复原图
            }
        }
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.Image = tup1;
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text == "校电阻")
            {
                panel1.Visible = true;
                panel1.Focus();
                for (Int32 i = 0; i < 7; i++)
                {
                    numericUpDown[i].Value = Convert.ToDecimal(Registry.GetValue(@"HKEY_CURRENT_USER\Software\二极管参数计算\Settings", str1[i], ""));
                }
                groupBox1.Enabled = false;
            }
            else
            {
                Index = comboBox1.SelectedIndex;
                label6.Text = "电阻" + str1[Index] + ":";
            }
        }
        /// <summary>
        /// 保存退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button10_Click(object sender, EventArgs e)
        {         
            //数据写入注册表子项
            for (Int32 i = 0; i < 7; i++)
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\二极管参数计算\Settings", str1[i], numericUpDown[i].Value);
            }
            panel1.Visible = false;
            groupBox1.Enabled = true;
            comboBox1.SelectedIndex = Index;
            label6.Text = "电阻" + str1[Index] + ":"; //恢复原来的comboBox1选项
        }
        /// <summary>
        /// 放弃
        /// </summary>
        /// <param name="sender"></param> 
        /// <param name="e"></param>
        private void button11_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            groupBox1.Enabled = true;
            comboBox1.SelectedIndex = Index;
            label6.Text = "电阻" + str1[Index] + ":";
        }
    }
}