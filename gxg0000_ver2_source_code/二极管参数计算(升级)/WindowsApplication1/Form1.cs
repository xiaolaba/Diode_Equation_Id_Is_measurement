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
using System.Threading;
using System.Drawing.Drawing2D;

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
        Int32 Index;
        Int64[] R = new Int64[7]; //限流电阻R1, R2, R3, R4, R5, R6, R7
        NumericUpDown[] numericUpDown = new NumericUpDown[7]; //限流电阻数字输入框控件
        String[] str1 ={ "R1", "R2", "R3", "R4", "R5", "R6", "R7" };    //电阻的编号数组
        Decimal[] str2 ={ 4700.0M, 1000.0M, 330.0M, 100.0M, 33.0M, 10.0M, 10000M }; //电阻初值数组
        String[] str3 = { "正", "向", "电", "流", "IF", " " };
        String[] str4 = { "动", "态", "电", "阻", "Rd", "(Ω)" };
        String[] str5 = { "10M", "1M", "100K", "10K", "1K", "100","10" };
        Int32 T = 25;  //温度
        Double IS = 171.34388778833204;   //饱和电流
        Double RD = 160.40886801322117;  //零电阻
        Double N = 1.0693751430533303;   //理想因子
        Double VT = 0.025679647254953728;   //热电压
        Double[] set ={ 1.0, 1.0, 1.0 }; //暂存
        Double[] set1 ={ 1.0, 1.0, 1.0, 1.0 };   //叠加数据(IS,RD,N,VT)
        Double[] set2 ={ 1.0, 1.0, 1.0, 1.0 };
        Double[] set3 ={ 1.0, 1.0, 1.0, 1.0 };
        Double[] set4 ={ 1.0, 1.0, 1.0, 1.0 };
        Double[] set5 ={ 1.0, 1.0, 1.0, 1.0 };
        Label[] lb1 = new Label[6]; //转移特性标签
        Label[] lb2 = new Label[8];
        Label[] lb3 = new Label[6];
        Label[] lb4 = new Label[7]; //灵敏度预估标签
        Label[] lb5 = new Label[8];
        Label[] lb6 = new Label[6];
        Bitmap tup1, tup2, tup3, tup4;
        Graphics g1, g2, g3, g4;
        Pen pen1, pen2;
        SolidBrush brush1, brush2;
        private void Form1_Load(object sender, EventArgs e)
        {
            pen1 = new Pen(Color.Gainsboro);
            pen2 = new Pen(Color.White);
            brush1 = new SolidBrush(Color.White); ;
            brush2 = new SolidBrush(pictureBox1.BackColor);
            tup1 = new Bitmap(301, 251);   //IV曲线画布
            g1 = Graphics.FromImage(tup1);
            g1.SmoothingMode = SmoothingMode.AntiAlias;  //消除锯齿, 使绘图质量最高
            g1.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g1.CompositingQuality = CompositingQuality.HighQuality;
            tup2 = new Bitmap(301, 265);   //RdV曲线画布
            g2 = Graphics.FromImage(tup2);
            g2.SmoothingMode = SmoothingMode.AntiAlias;  //消除锯齿, 使绘图质量最高
            g2.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g2.CompositingQuality = CompositingQuality.HighQuality;
            tup3 = new Bitmap(301, 251);    //跟踪IV曲线临时画布
            g3 = Graphics.FromImage(tup3);
            tup4 = new Bitmap(301, 265);    //跟踪RdV曲线临时画布
            g4 = Graphics.FromImage(tup4);
            this.listBox1.LostFocus += new System.EventHandler(this.listBox1_LostFocus); //添加失去焦点事件
            //校正电阻设置panel控件
            this.Controls.Add(panel1);
            panel1.BringToFront();
            panel1.Left = 652;
            panel1.Top = 306;
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
                    numericUpDown[i].Maximum = 10000M;
                    numericUpDown[i].Increment = 0.1M;
                }
                this.panel1.Controls.Add(numericUpDown[i]); //添加到控件集合
                numericUpDown[i].BringToFront();
            }
            //读取注册表保存的设置
            if (null == Registry.GetValue(@"HKEY_CURRENT_USER\Software\检波二极管灵敏度分析器\Settings", "R1", ""))
            {
                //第1次创建注册表子项,并将初值数据写入内存和注册表
                for (Int32 i = 0; i < 7; i++)
                {
                    R[i] = Convert.ToInt64(str2[i]);
                    numericUpDown[i].Value = str2[i];
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\检波二极管灵敏度分析器\Settings", str1[i], str2[i]);
                }
            }
            else
            {
                //读取注册表保存的子项内数据,并写入界面
                for (Int32 i = 0; i < 7; i++)
                {
                    numericUpDown[i].Value = Convert.ToDecimal(Registry.GetValue(@"HKEY_CURRENT_USER\Software\检波二极管灵敏度分析器\Settings", str1[i], ""));
                }
            }
            //System.IO.Directory.CreateDirectory(Application.StartupPath + "\\Diode"); //创建保存数据的文件夹          
            //设置转移特性垂直刻度
            for (int i = 0; i < 6; i++)
             {
                 lb1[i] = new Label();
                 lb1[i].BackColor = this.tabPage5.BackColor; //背景颜色(取底色)
                 lb1[i].Width = 30;
                 lb1[i].Height = 15;
                 lb1[i].TextAlign = ContentAlignment.MiddleRight;
                 if (i == 5)
                     lb1[i].Top = 25 + (50 * i - 3);
                 else
                     lb1[i].Top = 25 + 50 * i;
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
                 lb3[i].ForeColor = Color.Teal;
                 lb3[i].Top = 102 + 15 * i;
                 lb3[i].Left = 6;
                 lb3[i].Font = new Font("宋体", 9, FontStyle.Bold);
                 if (i == 5)
                     lb3[i].Text = "(" + button2.Text.Substring(0, 2) + ")";
                 else
                     lb3[i].Text = str3[i]; //正向电流IF
                 this.tabPage5.Controls.Add(lb3[i]); //添加到控件集合
                 lb3[i].BringToFront();
             }
             //设置转移特性水平刻度
             for (int i = 0; i < 8; i++)
             {
                 lb2[i] = new Label();
                 lb2[i].BackColor = this.tabPage5.BackColor; //背景颜色(取底色)
                 if (i != 7)
                 {
                     lb2[i].Width = 30;
                     lb2[i].Height = 15;
                     lb2[i].Top = 285;
                     lb2[i].Left = 57 + 50 * i;
                     lb2[i].Text = Convert.ToString(i * Convert.ToUInt32(numericUpDown7.Value) / 6);
                     lb2[i].Font = new Font("宋体", 9, FontStyle.Regular);
                     lb2[i].ForeColor = Color.Black;
                 }
                 else
                 {
                     lb2[i].Width = 100;
                     lb2[i].Height = 15;
                     lb2[i].Top = 305;
                     lb2[i].Left = 170;
                     lb2[i].Text = "正向电压VF(mV)";
                     lb2[i].Font = new Font("宋体", 9, FontStyle.Bold);
                     lb2[i].ForeColor = Color.Teal;
                 }
                 lb2[i].TextAlign = ContentAlignment.MiddleCenter;
                 this.tabPage5.Controls.Add(lb2[i]); //添加到控件集合
                 lb2[i].BringToFront();
             }
             //设置灵敏度预估特性垂直刻度
             for (int i = 0; i < 7; i++)
             {
                 lb4[i] = new Label();
                 lb4[i].BackColor = this.tabPage6.BackColor; //背景颜色(取底色)
                 lb4[i].Width = 30;
                 lb4[i].Height = 15;
                 lb4[i].TextAlign = ContentAlignment.MiddleRight;
                 if (i == 6)
                     lb4[i].Top = 12 + (44 * i - 3);
                 else
                     lb4[i].Top = 12 + 44 * i;
                 lb4[i].Left = pictureBox1.Left - lb4[i].Width;
                 lb4[i].Font = new Font("宋体", 9, FontStyle.Regular);
                 lb4[i].Text = str5[i];
                 this.tabPage6.Controls.Add(lb4[i]); //添加到控件集合
                 lb4[i].BringToFront();
             }
             for (int i = 0; i < 6; i++)
             {
                 lb6[i] = new Label();
                 lb6[i].BackColor = this.tabPage6.BackColor; //背景颜色(取底色)
                 lb6[i].Width = 35;
                 lb6[i].Height = 15;
                 lb6[i].TextAlign = ContentAlignment.MiddleCenter;
                 lb6[i].ForeColor = Color.Teal;
                 lb6[i].Top = 102 + 15 * i;
                 lb6[i].Left = 6;
                 lb6[i].Font = new Font("宋体", 9, FontStyle.Bold);
                 lb6[i].Text = str4[i]; //
                 this.tabPage6.Controls.Add(lb6[i]); //添加到控件集合
                 lb6[i].BringToFront();
             }
             //设置灵敏度预估特性水平刻度
             for (int i = 0; i < 8; i++)
             {
                 lb5[i] = new Label();
                 lb5[i].BackColor = this.tabPage6.BackColor; //背景颜色(取底色)
                 if (i != 7)
                 {
                     lb5[i].Width = 30;
                     lb5[i].Height = 15;
                     lb5[i].Top = 285;
                     lb5[i].Left = 57 + 50 * i;
                     lb5[i].Text = Convert.ToString(i * Convert.ToUInt32(numericUpDown8.Value) / 6);
                     lb5[i].Font = new Font("宋体", 9, FontStyle.Regular);
                     lb5[i].ForeColor = Color.Black;
                 }
                 else
                 {
                     lb5[i].Width = 100;
                     lb5[i].Height = 15;
                     lb5[i].Top = 305;
                     lb5[i].Left = 170;
                     lb5[i].Text = "正向电压VF(mV)";
                     lb5[i].Font = new Font("宋体", 9, FontStyle.Bold);
                     lb5[i].ForeColor = Color.Teal;
                 }
                 lb5[i].TextAlign = ContentAlignment.MiddleCenter;
                 this.tabPage6.Controls.Add(lb5[i]); //添加到控件集合
                 lb5[i].BringToFront();
             }
             Index = 3;
             comboBox1.SelectedIndex = Index;
             label6.Text = "电阻" + str1[Index] + ":";
             listBox1.SelectedIndex = 1;
             listBox1.Visible = false; 
             Drawing_Transfer_Characteristic(); //绘制转移特性曲线
             Drawing_Rd_V_sensibility(); //绘制灵敏度预估曲线
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
         Double Is1 = 0, Is2 = 0;
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
             if ((Is1 - Is2) / Is1 > 0.001)
             {
                 MessageBox.Show("方程无解！"); //误差超过0.1%，表示方程无法解出
                 return;
             }
         }
         else
         {
             if ((Is2 - Is1) / Is2 > 0.001)
             {
                 MessageBox.Show("方程无解！"); //显示错误
                 return;
             }
         }
         IS = (Is1 + Is2) / 2;
         RD = 1000000 * (VT * N / IS);
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
        Drawing_Rd_V_sensibility(); //绘制灵敏度预估曲线
     }    
        /// <summary>
        /// 绘制二极管转移特性界面函数
        /// </summary>
         private void Drawing_Transfer_Characteristic()
          {
             g1.Clear(Color.AliceBlue);
             //绘制IF刻度和横格子线
             for (int i = 0; i < 6; i++)
              {
                  lb1[i].Text = Convert.ToString((5 - i) * Convert.ToUInt32(numericUpDown6.Value) / 5); //IF刻度
              }
              lb3[5].Text = "(" + button2.Text.Substring(0, 2) + ")"; //IF电流单位
              for (int j = 0; j <= 300; j += 25)
              {
                  g1.DrawLine(pen1, 0, j, 300, j);
              }
              //绘制VF刻度和竖格子线
              for (int i = 0; i < 7; i++)
              {
                  lb2[i].Text = Convert.ToString(i * Convert.ToUInt32(numericUpDown7.Value) / 6); //VF刻度
              }
             for (int j = 0; j <= 300; j += 25)
              {
                  g1.DrawLine(pen1, j, 0, j, 300);
              }
            if (checkBox1.Checked == true)   //绘制叠加1
            {
                pen2.Color = button5.BackColor;
                Draw_Line_IV(set1[0], set1[2], set1[3], pen2);
            }
            if (checkBox2.Checked == true)   //绘制叠加2
            {
                pen2.Color = button6.BackColor;
                Draw_Line_IV(set2[0], set2[2], set2[3], pen2);
            }
            if (checkBox3.Checked == true)   //绘制叠加3
            {
                pen2.Color = button7.BackColor;
                Draw_Line_IV(set3[0], set3[2], set3[3], pen2);
            }
            if (checkBox4.Checked == true)   //绘制叠加4
            {
                pen2.Color = button8.BackColor;
                Draw_Line_IV(set4[0], set4[2], set4[3], pen2);
            }
            if (checkBox5.Checked == true)   //绘制叠加5
            {
                pen2.Color = button9.BackColor;
                Draw_Line_IV(set5[0], set5[2], set5[3], pen2);
            }
            pen2.Color = Color.Teal;
            Draw_Line_IV(IS, N, VT, pen2);  //绘制当前二极管伏安曲线
            pictureBox1.Image = tup1;           
        }
        /// <summary>
        /// 绘制二极管IV曲线函数
        /// </summary>
        /// <param name="Is"></param>
        /// <param name="n"></param>
        /// <param name="vt"></param>
        /// <param name="pen"></param>
        private void Draw_Line_IV(Double Is, Double n, Double vt, Pen pen)
        {
            Double I1, I2, Vd; ;
            Int32 k = Convert.ToInt32(numericUpDown6.Value) * Convert.ToInt32(Convert.ToString(listBox1.SelectedItem).Substring(4));
            for (int V = 0; V < 300; V++)
            {
                Vd = Convert.ToUInt32(numericUpDown7.Value) * V / 300.0;//mV             
                I1 = 250 * (Is * (Math.Exp(Vd / (1000 * vt * n)) - 1)) / k; //nA
                if (I1 > 250) I1 = 250;
                Vd = Convert.ToUInt32(numericUpDown7.Value) * (V + 1) / 300.0;  //mV
                I2 = 250 * (Is * (Math.Exp(Vd / (1000 * vt * n)) - 1)) / k; //nA
                if (I2 > 250) I2 = 250;   
                if(!((I1==250)&(I2==250)))
                    g1.DrawLine(pen, (float)V, (float)(250 - I1), (float)(V + 1), (float)(250 - I2));
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
            listBox1.Visible = false;  //失去列表焦点隐藏列表
        }
        private void listBox1_MouseLeave(object sender, EventArgs e)
        {
            listBox1.Visible = false; //移出列表隐藏列表
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button2.Text = listBox1.Text;
            listBox1.Visible = false;
            Drawing_Transfer_Characteristic();
        }
        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();  //调整数字输入框击发重绘曲线
        }
        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic();  //调整数字输入框击发重绘曲线
        }
       /// <summary>
        /// 绘制Rd-V灵敏度预估特性界面函数
       /// </summary>
        private void Drawing_Rd_V_sensibility()
        {
            g2.Clear(Color.AliceBlue);
            //绘制VF刻度和竖格子线
            for (int i = 0; i < 7; i++)
            {
                lb5[i].Text = Convert.ToString(i * Convert.ToUInt32(numericUpDown8.Value) / 6); //VF刻度
            }
            //横线
            for (int j = 0; j <= 200; j++)
            {
                int y = (j / 10) * 44 + Convert.ToInt32(44 * (2 - Math.Log10((j % 10 + 1) * 10))); //水平对数分布
                g2.DrawLine(pen1, 0, y, 300, y);
            }
            //竖线
            for (int j = 0; j <= 300; j += 25)
            {
                g2.DrawLine(pen1, j, 0, j, 264);
            }
            if (checkBox1.Checked == true)   //绘制叠加1
            {
                pen2.Color = button5.BackColor;
                Draw_Line_Rd_V(set1[0], set1[2], set1[3], pen2);
            }
            if (checkBox2.Checked == true)   //绘制叠加2
            {
                pen2.Color = button6.BackColor;
                Draw_Line_Rd_V(set2[0], set2[2], set2[3], pen2);
            }
            if (checkBox3.Checked == true)   //绘制叠加3
            {
                pen2.Color = button7.BackColor;
                Draw_Line_Rd_V(set3[0], set3[2], set3[3], pen2);
            }
            if (checkBox4.Checked == true)   //绘制叠加4
            {
                pen2.Color = button8.BackColor;
                Draw_Line_Rd_V(set4[0], set4[2], set4[3], pen2);
            }
            if (checkBox5.Checked == true)   //绘制叠加5
            {
                pen2.Color = button9.BackColor;
                Draw_Line_Rd_V(set5[0], set5[2], set5[3], pen2);
            }
            pen2.Color = Color.Teal;
            Draw_Line_Rd_V(IS, N, VT,pen2 );  //绘制当前二极管Rd_V曲线
            pictureBox2.Image = tup2;
        }
       /// <summary>
        /// 绘制二极管Rd_V曲线函数
       /// </summary>
       /// <param name="Is"></param>
       /// <param name="n"></param>
       /// <param name="vt"></param>
       /// <param name="pen"></param>
        private void Draw_Line_Rd_V(Double Is, Double n, Double vt, Pen pen)
        {
            Double Rd1, Rd2, Vd, Id;
            for (int V = 0; V < 300; V++)
            {
                Vd = (V / 300.0) * Convert.ToUInt32(numericUpDown8.Value);   //mV             
                Id = Is * (Math.Exp(Vd / (1000 * vt * n)) - 1); //正向电压对应的正向电流nA
                if (Id == 0.0) Id = 1;
                Rd1 = (Convert.ToInt32(1000000000 * ((n * vt / Id) * Math.Log((Id / Is) + 1, Math.E))));    //计算动态电阻Rd1
                if (Rd1 == 0) Rd1 = 1;
                Rd1 = 308 * (Math.Log10(Rd1) / 7.0);
                Vd = ((V + 1) / 300.0) * Convert.ToUInt32(numericUpDown8.Value);  //mV
                Id = Is * (Math.Exp(Vd / (1000 * vt * n)) - 1); //正向电压对应的正向电流nA
                Rd2 = (Convert.ToInt32(1000000000 * ((n * vt / Id) * Math.Log((Id / Is) + 1, Math.E))));    //计算动态电阻Rd2
                if (Rd2 == 0) Rd2 = 1;
                Rd2 = 308 * (Math.Log10(Rd2) / 7.0);
                if (!((Rd1 > 308) | (Rd2 > 308))) //超出范围不绘制线条
                    g2.DrawLine(pen, (float)V, (float)(308 - Rd1), (float)(V + 1), (float)(308 - Rd2));
            }
        }
        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            Drawing_Rd_V_sensibility();  //调整数字输入框击发重绘曲线
        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            label46.Visible = false;
        }
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            label46.Visible = true;
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
            saveFileDialog1.InitialDirectory = Application.StartupPath + "\\Diode"; //打开起始目录(跟随安装文件)
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
        /// 读取二极管数据文件到内存并重绘曲线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog(1);
            if (checkBox1.Checked == true)
            {
                Drawing_Transfer_Characteristic(); //绘制转移特性曲线
                Drawing_Rd_V_sensibility(); //绘制灵敏度预估曲线
            }
        }
        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog(2);
            if (checkBox2.Checked == true)
            {
                Drawing_Transfer_Characteristic();
                Drawing_Rd_V_sensibility(); 
            }
        }
        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog(3);
            if (checkBox3.Checked == true)
            {
                Drawing_Transfer_Characteristic(); 
                Drawing_Rd_V_sensibility(); 
            }
        }
        private void button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog(4);
            if (checkBox4.Checked == true)
            {
                Drawing_Transfer_Characteristic(); 
                Drawing_Rd_V_sensibility(); 
            }
        }
        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog(5);
            if (checkBox5.Checked == true)
            {
                Drawing_Transfer_Characteristic(); 
                Drawing_Rd_V_sensibility(); 
            }
        }
        /// <summary>
        /// 读文件数据到数组
        /// </summary>
        /// <param name="k"></param>
        private void OpenFileDialog(Int32 k)
        {
            openFileDialog1.Filter = "文本文件（*.txt）|*.txt|全部文件|*.*";
            openFileDialog1.FilterIndex = 1; //指定第1个过滤器（默认的打开的方法）
            openFileDialog1.InitialDirectory = Application.StartupPath + "\\Diode";  //打开起始目录(跟随安装文件)
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
        /// 复选框勾选击发叠加重绘
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Drawing_Transfer_Characteristic(); //绘制转移特性曲线
            Drawing_Rd_V_sensibility(); //绘制灵敏度预估曲线
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
            Drawing_Rd_V_sensibility();
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
            Drawing_Rd_V_sensibility();
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
            Drawing_Rd_V_sensibility();
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
            Drawing_Rd_V_sensibility();
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
        /// 选项卡切换击发重绘
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl2_Selected(object sender, TabControlEventArgs e)
        {
            if (tabControl2.SelectedTab == tabPage4)//也可以判断tabControl1.SelectedTab.Text的值
            {
                tabControl1.SelectedIndex = 0; //切换到测量程序说明
            }
            if (tabControl2.SelectedTab == tabPage5)
            {
                this.tabPage5.Controls.Add(this.groupBox8); //添加到曲线叠加控件集合
                tabControl1.SelectedIndex = 0;
                Drawing_Transfer_Characteristic();
            }
            else if (tabControl2.SelectedTab == tabPage6)
            {
                this.tabPage6.Controls.Add(this.groupBox8); //添加到曲线叠加控件集合
                tabControl1.SelectedIndex = 3; //切换到灵敏度判断标准说明
                Drawing_Rd_V_sensibility();
            }
        }
        /// <summary>
        /// 鼠标指针即时显示曲线处动态电阻
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Double Vd;  //正向电压mV
            Double Id;   //正向电压对于的正向电流nA
            Int32 k;   //纵坐标单位
            Int32 I; //纵坐标
            String str;
            Int32 Rd = 0;
            Tracking_curve();  //获取当前的跟踪曲线
            Double Is = set[0];
            Double n = set[1];
            Double vt = set[2];
            ///IV转移特性曲线跟踪
            Vd = (e.X / 300.0) * Convert.ToUInt32(numericUpDown7.Value);    //正向电压mV
                Id = Is * (Math.Exp(Vd / (1000 * vt * n)) - 1); //正向电压对于的正向电流nA
                k = Convert.ToInt32(Convert.ToString(listBox1.SelectedItem).Substring(4)); //纵坐标单位
                I = (int)(250 * (Id / (Convert.ToInt32(numericUpDown6.Value) * k))); //纵坐标
                if (Id == 0.0) Id = 1;
                //鼠标靠近曲线悬停字符串显示
                if (((e.Y - (250 - I)) < 5) & (((250 - I) - e.Y) < 5))
                {
                    Rd = (Convert.ToInt32(1000000000 * ((n * vt / Id) * Math.Log((Id / Is) + 1, Math.E))));    //计算动态电阻Rd
                    if (Rd < 1000)
                        str = "Rd=" + String.Format("{0:0Ω}", Rd);
                    else
                    {
                        Rd = Rd / 1000;
                        str = "Rd=" + String.Format("{0:0KΩ}", Rd);
                    }
                    //绘制悬停字符串
                    g3.DrawImage(tup1, 0, 0);  //复制
                    int x1 = 0;
                    int y1 = 0;
                    SizeF sizeF = g3.MeasureString(str, new Font("Arial", 9, FontStyle.Regular)); //测量字符串长度
                    if (e.X > (int)sizeF.Width) //区分在鼠标左或右显示字符串
                        x1 = e.X - (int)sizeF.Width;
                    else
                        x1 = e.X + 5;
                    if (e.Y < (int)sizeF.Height) //区分在鼠标上或下显示字符串
                        y1 = e.Y;
                    else
                        y1 = e.Y - (int)sizeF.Height;
                    g3.DrawString(str, new Font("Arial", 9, FontStyle.Regular), brush1, x1, y1);
                    g3.FillEllipse(brush2, e.X - 2, (250 - I) - 2, 4, 4);  //绘制实心圆
                    g3.DrawEllipse(pen2, e.X - 2, (250 - I) - 2, 4, 4); //绘制圆圈
                    pictureBox1.Image = tup3;
                }
                else
                {
                    pictureBox1.Image = tup1; //离开网格区恢复原图
                }
            }
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.Image = tup1;
        }
        /// <summary>
        /// 鼠标指针即时显示曲线处斜率或动态电阻
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            Double Vd;  //正向电压mV
            Double Id1, Id2;   //正向电压对于的正向电流nA
            Double Rd1,Rd2;
            Int32 Rd_Log; //转成对数值
            String str="";
            Tracking_curve();  //获取当前的跟踪曲线
            Double Is = set[0];
            Double n = set[1];
            Double vt = set[2];
            ///RdV灵敏度预估跟踪曲线
            Vd = (e.X / 300.0) * Convert.ToUInt32(numericUpDown8.Value);   //mV             
            Id1 = Is * (Math.Exp(Vd / (1000 * vt * n)) - 1); //正向电压对应的正向电流nA
            if (Id1 == 0.0) Id1 = 1;
            Rd1 = 1000000000 * ((n * vt / Id1) * Math.Log((Id1 / Is) + 1, Math.E));    //计算动态电阻Rd1
            if (Rd1 == 0) Rd1 = 1;
            Rd_Log = (int)(308 * (Math.Log10(Rd1) / 7.0)); //转成对数值
            //鼠标靠近曲线悬停字符串显示
            if (((e.Y - (308 - Rd_Log)) < 5) & (((308 - Rd_Log) - e.Y) < 5))
            {
                if (radioButton1.Checked == true) //跟踪显示斜率
                {
                    Id2 = Is * (Math.Exp((Vd+1) / (1000 * vt * n)) - 1);
                    if (Id2 == 0.0) Id2 = 1;
                    Rd2 = (Convert.ToInt32(1000000000 * ((n * vt / Id2) * Math.Log((Id2 / Is) + 1, Math.E))));    //计算动态电阻Rd2
                    Double k =( Rd1-Rd2) / Rd1;
                    str = String.Format("{0:0.00%/mV}", k);
                }
                else //跟踪显示Rd
                {
                    if (Rd1 < 1000)
                        str = "Rd="+String.Format("{0:0Ω}", Rd1);
                    else
                    {
                        Rd1 = Rd1 / 1000;
                        str = "Rd=" + String.Format("{0:0KΩ}", Rd1);
                    }
                }
                //绘制悬停字符串
                g4.DrawImage(tup2, 0, 0);  //复制
                int x1 = 0;
                int y1 = 0;
                SizeF sizeF = g4.MeasureString(str, new Font("Arial", 9, FontStyle.Regular)); //测量字符串长度
                if (e.X > (300-(int)sizeF.Width)) //区分在鼠标左或右显示字符串
                    x1 = e.X - (int)sizeF.Width;
                else
                    x1 = e.X + 5;
                if (e.Y < (int)sizeF.Height) //区分在鼠标上或下显示字符串
                    y1 = e.Y;
                else
                    y1 = e.Y - (int)sizeF.Height;
                g4.DrawString(str, new Font("Arial", 9, FontStyle.Regular), brush1, x1, y1);
                g4.FillEllipse(brush2, e.X - 2, (308 - Rd_Log) - 2, 4, 4);  //绘制实心圆
                g4.DrawEllipse(pen2, e.X - 2, (308 - Rd_Log) - 2, 4, 4); //绘制圆圈
                pictureBox2.Image = tup4;
            }
            else
            {
                pictureBox2.Image = tup2; //离开网格区恢复原图
            }
        }
        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            pictureBox2.Image = tup2;
        }
        /// <summary>
        /// 判断跟踪曲线和颜色函数
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void Tracking_curve()
        {
            if (radioButton3.Enabled & radioButton3.Checked)
            {
                pen2.Color = button5.BackColor;
                brush1.Color = button5.BackColor;
                set[0] = set1[0];
                set[1] = set1[2];
                set[2] = set1[3];
            }
          else if (radioButton4.Enabled & radioButton4.Checked)
            {
                pen2.Color = button6.BackColor;
                brush1.Color = button6.BackColor;
                set[0] = set2[0];
                set[1] = set2[2];
                set[2] = set2[3];
            }
            else if (radioButton5.Enabled & radioButton5.Checked)
            {
                pen2.Color = button7.BackColor;
                brush1.Color = button7.BackColor;
                set[0] = set3[0];
                set[1] = set3[2];
                set[2] = set3[3];
            }
            else if (radioButton6.Enabled & radioButton6.Checked)
            {
                pen2.Color = button8.BackColor;
                brush1.Color = button8.BackColor;
                set[0] = set4[0];
                set[1] = set4[2];
                set[2] = set4[3];
            }
            else if (radioButton7.Enabled & radioButton7.Checked)
            {
                pen2.Color = button9.BackColor;
                brush1.Color = button9.BackColor;
                set[0] = set5[0];
                set[1] = set5[2];
                set[2] = set5[3];
            }
            else
            {
                pen2.Color = Color.Teal;
                brush1.Color = Color.Teal;
                set[0] = IS;
                set[1] = N;
                set[2] = VT;
            }
        }
        /// <summary>
        /// 校正电阻
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.Text == "校电阻")
            {
                panel1.Visible = true;
                panel1.Focus();
                for (Int32 i = 0; i < 7; i++)
                {
                    numericUpDown[i].Value = Convert.ToDecimal(Registry.GetValue(@"HKEY_CURRENT_USER\Software\检波二极管灵敏度分析器\Settings", str1[i], ""));
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
            for (Int32 i = 0; i < 7; i++) //数据写入注册表子项
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\检波二极管灵敏度分析器\Settings", str1[i], numericUpDown[i].Value);
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
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabel1.Text); //超链接
        }
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabel2.Text);
        }
    }
}