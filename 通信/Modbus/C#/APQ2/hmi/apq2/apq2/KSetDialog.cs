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
    public partial class KSetDialog : Form
    {
        public KSetDialog(KPlc fml_plc)
        {
            plc = fml_plc;
            InitializeComponent();
        }

        private KPlc plc;

        /// <summary>
        /// 确定按钮
        /// 将界面参数存储到plc对象，并save
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                plc.Ip = textBox7.Text;
                plc.Port = Convert.ToUInt32(textBox6.Text);
                plc.SlaveAdress = Convert.ToByte(textBox8.Text);

                plc.SoftLimit[0] = Convert.ToDouble(textBox2.Text);
                plc.SoftLimit[1] = Convert.ToDouble(textBox1.Text);
                plc.SoftLimit[2] = Convert.ToDouble(textBox11.Text);
                plc.SoftLimit[3] = Convert.ToDouble(textBox4.Text);

                plc.AccTime[0] = Convert.ToUInt16(textBox9.Text);
                plc.AccTime[1] = Convert.ToUInt16(textBox10.Text);
                plc.AccTime[2] = Convert.ToUInt16(textBox13.Text);
                plc.AccTime[3] = Convert.ToUInt16(textBox12.Text);

                plc.Ppu[0] = Convert.ToDouble(textBox5.Text);
                plc.Ppu[1] = Convert.ToDouble(textBox3.Text);

                plc.HomeOffset[0] = Convert.ToDouble(textBox14.Text);
                plc.HomeOffset[1] = Convert.ToDouble(textBox15.Text);

                plc.HomeVel[0] = Convert.ToDouble(textBox16.Text);
                plc.HomeVel[1] = Convert.ToDouble(textBox17.Text);

                plc.saveConfig();

                // 重新载入配置
                plc.loadConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[KSetDialog.button1_Click] 写入参数错误，检查参数格式");
            }
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 使用plc数据初始化窗口显示的参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KSetDialog_Load(object sender, EventArgs e)
        {
            //如果文件不存在则不从文件读取
            if (!System.IO.File.Exists("plcParam.xml"))
            {
                Console.WriteLine("[KSetDialog.KSetDialog_Load] 默认配置文件不存在，使用默认参数");
                return;
            }

            textBox2.Text = string.Format("{0}", plc.SoftLimit[0]);
            textBox1.Text = string.Format("{0}", plc.SoftLimit[1]);
            textBox11.Text = string.Format("{0}", plc.SoftLimit[2]);
            textBox4.Text = string.Format("{0}", plc.SoftLimit[3]);

            textBox7.Text = plc.Ip;
            textBox6.Text = string.Format("{0}", plc.Port);
            textBox8.Text = string.Format("{0}", plc.SlaveAdress);

            textBox9.Text = string.Format("{0}", plc.AccTime[0]);
            textBox10.Text = string.Format("{0}", plc.AccTime[1]);
            textBox13.Text = string.Format("{0}", plc.AccTime[2]);
            textBox12.Text = string.Format("{0}", plc.AccTime[3]);

            textBox5.Text = string.Format("{0}", plc.Ppu[0]);
            textBox3.Text = string.Format("{0}", plc.Ppu[1]);

            textBox14.Text = string.Format("{0}", plc.HomeOffset[0]);
            textBox15.Text = string.Format("{0}", plc.HomeOffset[1]);

            textBox16.Text = string.Format("{0}", plc.HomeVel[0]);
            textBox17.Text = string.Format("{0}", plc.HomeVel[1]);
        }
    }
}
