using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace apq2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            KSetDialog ksd = new KSetDialog(this.plc);
            ksd.ShowDialog();

            // 刷新homeVel
            refreshHomeVel();
        }

        KPlc plc = new KPlc();

        /// <summary>
        /// UI刷新 通信查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = plc.Connected ? "已连接" : "未连接";
            toolStripStatusLabel2.BackColor = plc.Connected ? Color.LightGreen : Color.LightGray;
            
            if (plc.Connected)
            {
                plc.req();

                // 刷新UI
                string spos1 = string.Format("{0:F2}", plc.Axes_pos[0]);
                string spos2 = string.Format("{0:F2}", plc.Axes_pos[1]);
                string svel1 = string.Format("{0:F2}", plc.Axes_vel[0]);
                string svel2 = string.Format("{0:F2}", plc.Axes_vel[1]);

                textBox1.Text = spos1;
                textBox2.Text = spos2;
                textBox4.Text = svel1;
                textBox3.Text = svel2;

                // 限位、原点
                label23.BackColor = plc.Axes_limit[0] ? Color.Red : Color.LightGreen;
                label24.BackColor = plc.Axes_homeSignal[0] ? Color.LightGreen : Color.LightGray;
                label25.BackColor = plc.Axes_limit[1] ? Color.Red : Color.LightGreen;

                label28.BackColor = plc.Axes_limit[2] ? Color.Red : Color.LightGreen;
                label27.BackColor = plc.Axes_homeSignal[1] ? Color.LightGreen : Color.LightGray;
                label26.BackColor = plc.Axes_limit[3] ? Color.Red : Color.LightGreen;

                label9.BackColor = plc.Axes_onMoving[0] ? Color.LightGreen : Color.LightGray;
                label12.BackColor = plc.Axes_onMoving[1] ? Color.LightGreen : Color.LightGray;

                // 已回零
                label30.BackColor = plc.Axes_homeDone[0] ? Color.LightGreen : Color.LightGray;
                label29.BackColor = plc.Axes_homeDone[1] ? Color.LightGreen : Color.LightGray;
                label30.Text = plc.Axes_homeDone[0] ? "已回零" : "未回零";
                label29.Text = plc.Axes_homeDone[1] ? "已回零" : "未回零";

                // 未完成回零的情况下除回零外其他按钮无法操作，测试期间可以屏蔽以下9行
                button1.Enabled = plc.Axes_homeDone[0];
                button2.Enabled = plc.Axes_homeDone[0];
                button5.Enabled = plc.Axes_homeDone[0];
                button6.Enabled = plc.Axes_homeDone[0];

                button8.Enabled = plc.Axes_homeDone[1];
                button7.Enabled = plc.Axes_homeDone[1];
                button10.Enabled = plc.Axes_homeDone[1];
                button11.Enabled = plc.Axes_homeDone[1];

                // 回零提示信息
                label31.Visible = !plc.Axes_homeDone[0];
                label32.Visible = !plc.Axes_homeDone[1];  
            }
            else
            {
            }
        }
        
        // 选择的模式发生变化
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshHomeVel();
        }

        private bool zeroCheck(double fml_v)
        {
            bool ret = KPlc.checkRealZero(fml_v);
            if (!ret)
            {
                MessageBox.Show("速度不能近似或等于0");
            }

            return ret;
        }
        // a1 negative press
        private void button1_MouseDown(object sender, MouseEventArgs e)
        {
            double vel;
            try
            {
                vel = Convert.ToDouble(textBox5.Text);
                if (zeroCheck(vel)) return;
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A1 jog速度格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }

            plc.move_jog(0, KPlc.Direction.negative, true, vel);
        }

        // a1 negative release
        private void button1_MouseUp(object sender, MouseEventArgs e)
        {
            plc.move_jog(0, KPlc.Direction.negative, false);
        }

        // a1 jog positive
        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            double vel;
            try
            {
                vel = Convert.ToDouble(textBox5.Text);
                if (zeroCheck(vel)) return;
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A1 jog速度格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }

            plc.move_jog(0, KPlc.Direction.positive, true, vel);
        }

        private void button2_MouseUp(object sender, MouseEventArgs e)
        {
            plc.move_jog(0, KPlc.Direction.positive, false);
        }

        // 连接到plc
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            bool ok = plc.connect2device();
            if(!ok)
            {
                MessageBox.Show("连接到PLC失败");
            }
        }

        // 断开plc连接
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            plc.disconnectFromDevice();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            plc.disconnectFromDevice();
        }

        // a2 positive
        private void button7_MouseDown(object sender, MouseEventArgs e)
        {
            double vel;
            try
            {
                vel = Convert.ToDouble(textBox6.Text);
                if (zeroCheck(vel)) return;
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A2 jog速度格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }
            plc.move_jog(1, KPlc.Direction.positive, true, vel);
        }

        private void button7_MouseUp(object sender, MouseEventArgs e)
        {
            plc.move_jog(1, KPlc.Direction.positive, false);
        }

        // a2 negative
        private void button8_MouseDown(object sender, MouseEventArgs e)
        {
            double vel;
            try
            {
                vel = Convert.ToDouble(textBox6.Text);
                if (zeroCheck(vel)) return;
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A2 jog速度格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }
            plc.move_jog(1, KPlc.Direction.negative, true, vel);
        }

        private void button8_MouseUp(object sender, MouseEventArgs e)
        {
            plc.move_jog(1, KPlc.Direction.negative, false);
        }

        // a1回零
        private void button4_Click(object sender, EventArgs e)
        {
            double vel;
            try
            {
                vel = Convert.ToDouble(textBox10.Text);
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A1 回零速度格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }
            plc.move(0, KPlc.MoveMode.Home, vel);
        }

        // a2回零
        private void button9_Click(object sender, EventArgs e)
        {
            double vel;
            try
            {
                vel = Convert.ToDouble(textBox8.Text);
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A1 回零速度格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }
            plc.move(1, KPlc.MoveMode.Home, vel);
        }

        // a1绝对运动
        private void button5_Click(object sender, EventArgs e)
        {
            double vel, target;
            try
            {
                vel = Convert.ToDouble(textBox12.Text);
                if (zeroCheck(vel)) return;
                target = Convert.ToDouble(textBox11.Text);
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A1 绝对运动数据格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }

            plc.move(0, KPlc.MoveMode.Absolute, vel, target);
        }

        // a2绝对运动
        private void button10_Click(object sender, EventArgs e)
        {
            double vel, target;
            try
            {
                vel = Convert.ToDouble(textBox16.Text);
                if (zeroCheck(vel)) return;
                target = Convert.ToDouble(textBox15.Text);
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A2 绝对运动数据格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }

            plc.move(1, KPlc.MoveMode.Absolute, vel, target);
        }

        // a1相对运动
        private void button6_Click(object sender, EventArgs e)
        {
            double vel, target;
            try
            {
                vel = Convert.ToDouble(textBox14.Text);
                if (zeroCheck(vel)) return;
                target = Convert.ToDouble(textBox13.Text);
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A1 绝对运动数据格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }

            plc.move(0, KPlc.MoveMode.Relative, vel, target);
        }

        // a2相对运动
        private void button11_Click(object sender, EventArgs e)
        {
            double vel, target;
            try
            {
                vel = Convert.ToDouble(textBox18.Text);
                if (zeroCheck(vel)) return;
                target = Convert.ToDouble(textBox17.Text);
            }
            catch (Exception ex)
            {
                string info = string.Format("{0}:{1}", "A2 绝对运动数据格式错误", ex.Message);
                MessageBox.Show(info);
                return;
            }

            plc.move(1, KPlc.MoveMode.Relative, vel, target);
        }

        // A1停止运动
        private void button3_Click(object sender, EventArgs e)
        {
            plc.move_stop(0);
        }

        // A2停止
        private void button12_Click(object sender, EventArgs e)
        {
            plc.move_stop(1);
        }

        private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
        {
            refreshHomeVel();
        }

        private void refreshHomeVel()
        {
            textBox10.Text = string.Format("{0}", plc.HomeVel[0]);
            textBox8.Text = string.Format("{0}", plc.HomeVel[1]);
        }
    }
}
