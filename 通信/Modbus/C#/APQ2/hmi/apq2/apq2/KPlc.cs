using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace apq2
{
    /// <summary>
    /// 在KModbusTcpMaster的基础上进行读写数据，对外开放的是操作接口
    /// </summary>
    public class KPlc : KModbusTcpMaster
    {
        public KPlc()
        {
            loadConfig();
        }

        #region 轴数据
        const int max_motor = 2;
        private double[] _axes_pos = new double[max_motor];
        private double[] _axes_vel = new double[max_motor];
        private ushort[] _axes_errId = new ushort[max_motor];
        private bool[] _axes_onMoving = new bool[max_motor];
        private bool[] _axes_error = new bool[max_motor];
        private bool[] _axes_homeSignal = new bool[max_motor];
        private bool[] _axes_homeDone = new bool[max_motor];
        private bool[] _axes_limit = new bool[max_motor * 2];


        public bool[] Axes_limit
        {
            get { return _axes_limit; }
            set { _axes_limit = value; }
        }

        public double[] Axes_pos
        {
            get
            {
                return _axes_pos;
            }

            set
            {
                _axes_pos = value;
            }
        }

        public double[] Axes_vel
        {
            get
            {
                return _axes_vel;
            }

            set
            {
                _axes_vel = value;
            }
        }

        public ushort[] Axes_errId
        {
            get
            {
                return _axes_errId;
            }

            set
            {
                _axes_errId = value;
            }
        }

        public bool[] Axes_onMoving
        {
            get
            {
                return _axes_onMoving;
            }

            set
            {
                _axes_onMoving = value;
            }
        }

        public bool[] Axes_error
        {
            get
            {
                return _axes_error;
            }

            set
            {
                _axes_error = value;
            }
        }
        public bool[] Axes_homeSignal
        {
            get
            {
                return _axes_homeSignal;
            }

            set
            {
                _axes_homeSignal = value;
            }
        }

        public bool[] Axes_homeDone
        {
            get
            {
                return _axes_homeDone;
            }

            set
            {
                _axes_homeDone = value;
            }
        }


        #endregion

        public enum MoveMode
        {
            Stop = 0,
            Absolute = 1,
            Relative = 2,
            Jog = 3,
            Home = 4
        }

        public enum Direction
        {
            positive = 0,
            negative = 1
        }

        // 当前选择的运动模式
        private MoveMode[] _moveMode = new MoveMode[2];

        //PLC操作、状态变量区域
        ushort[] _readArea = new ushort[100];  //对应DT100~DT199
        ushort[] _writeArea = new ushort[100];  //对应DT200~DT299
        bool[] _operaArea = new bool[32];  //对应WR10~WR11
        bool[] _statusArea = new bool[32];  //对应WR12~WR13
        double[] _ppu = new double[] { 1.0, 1.0 };  //pulse per unit
        double[] _softLimit = new double[] { 0, 1500 , 0, 1500  };  // 0~1 ax1 2~3 ax2
        ushort[] _accTime = new ushort[] { 200, 200, 200, 200 };  //加减速时间 [0]-acc [1]-dec
        private double[] _homeOffset = new double[2];  //零点偏置
        private double[] _homeVel = new double[] { 1, 1 };  //回零速度

        public double[] Ppu
        {
            get
            {
                return _ppu;
            }

            set
            {
                _ppu = value;
            }
        }

        public double[] SoftLimit
        {
            get
            {
                return _softLimit;
            }

            set
            {
                _softLimit = value;
            }
        }

        public ushort[] AccTime
        {
            get
            {
                return _accTime;
            }

            set
            {
                _accTime = value;
            }
        }

        public double[] HomeOffset
        {
            get
            {
                return _homeOffset;
            }

            set
            {
                _homeOffset = value;
            }
        }

        public double[] HomeVel
        {
            get
            {
                return _homeVel;
            }

            set
            {
                _homeVel = value;
            }
        }



        /// <summary>
        /// 指定轴正向点动
        /// </summary>
        /// <param name="fml_axis">轴编号 1-2</param>
        /// <param name="fml_dir">true-正方向， false-负方向</param>
        /// <param name="fml_state">保持型操作，在按下持续运动，弹起停止运动</param>
        /// <param name="fml_vel">jog速度</param>
        public void move_jog(uint fml_axis, Direction fml_dir, bool fml_state, double fml_vel = 10.0)
        {
            if (fml_axis >= 2) return;

            // 切换模式
            _moveMode[fml_axis] = MoveMode.Jog;

            // 转换为脉冲单位
            try
            {
                if(fml_state)
                {
                    uint pulse_vel = Convert.ToUInt32(fml_vel * _ppu[fml_axis]);
                    byte[] byt = BitConverter.GetBytes(pulse_vel);
                    _writeArea[06 + fml_axis * 4] = (ushort)(byt[0] << 8 | byt[1] << 0);
                    _writeArea[07 + fml_axis * 4] = (ushort)(byt[2] << 8 | byt[3] << 0);
                }
                if(fml_dir == Direction.positive)
                {
                    _operaArea[6 + fml_axis] = fml_state;
                }
                else if(fml_dir == Direction.negative)
                {
                    _operaArea[8 + fml_axis] = fml_state;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[KPlc.move_jog] 检查数据是否超出范围");
            }
        }
        

        /// <summary>
        /// 绝对、相对、回零运动
        /// </summary>
        /// <param name="fml_axis"></param>
        /// <param name="fml_mode"></param>
        /// <param name="fml_velocity"></param>
        /// <param name="fml_target"></param>
        public void move(uint fml_axis, MoveMode fml_mode, double fml_velocity = 10.0, double fml_target = .0)
        {
            if (fml_axis >= 2) return;
            try
            {
                // 切换模式
                _moveMode[fml_axis] = fml_mode;

                //根据相对绝对模式选择不同的操作变量
                if (fml_mode == MoveMode.Relative)
                {
                    _operaArea[2+ fml_axis] = true;
                }
                else if (fml_mode == MoveMode.Absolute)
                {
                    _operaArea[4 + fml_axis] = true;
                }
                else if (fml_mode == MoveMode.Home)
                {
                    _operaArea[10 + fml_axis] = true;
                }
                else return;

                int pulse_target = Convert.ToInt32(fml_target * _ppu[fml_axis]);
                int pulse_vel = Convert.ToInt32(fml_velocity * _ppu[fml_axis]);
                uint index1, index2;  //目标位置、速度寄存器索引
                index1 = 4 + fml_axis * 4;  //DDT104 DDT108
                index2 = 6 + fml_axis * 4;  //DDT106 DDT110
                byte[] byt_pos = BitConverter.GetBytes(pulse_target);
                byte[] byt_vel = BitConverter.GetBytes(pulse_vel);
                // 交换字的高低字节
                _writeArea[index1] = (ushort)(byt_pos[0] << 8 | byt_pos[1]);
                _writeArea[index1+1] = (ushort)(byt_pos[2] << 8 | byt_pos[3]);

                _writeArea[index2] = (ushort)(byt_vel[0] << 8 | byt_vel[1]);
                _writeArea[index2 + 1] = (ushort)(byt_vel[2] << 8 | byt_vel[3]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[KPlc.move_rel] {0} 检查数据是否超出范围", ex.Message);
            }
        }

        public void move_stop(uint fml_axis = 255)
        {
            _moveMode[0] = MoveMode.Stop;
            _moveMode[1] = MoveMode.Stop;

            if (fml_axis == 255) //所有轴停止
            {
                _operaArea[0] = true;
                _operaArea[1] = true;
            }

            if (fml_axis >= 2) return;
            _operaArea[fml_axis] = true;
        }

        /// <summary>
        /// 与PLC进行数据交互 读状态，写操作
        /// </summary>
        public override void req() 
        {
            if (!Connected) return;

            try
            {
                // 读取状态数据 12word
                ushort[] rcv_word = new ushort[12];
                bool ok = read_holdRegister(200, 12, ref rcv_word);
                if (!ok)
                {
                    // 检查通信状态

                    return;
                }

                /* *********************************************************
                 * 从获取的数据中解析
                 * 1.将对应的状态变量从DDT200中提取处理，并存入对应的变量
                 * 2.将数据变量解析并提取
                 * *********************************************************/
                for (int i = 0; i < 16; i++)
                {
                    _statusArea[i] = Convert.ToBoolean(rcv_word[0] & (0x01 << i));
                    _statusArea[i + 16] = Convert.ToBoolean(rcv_word[1] & (0x01 << i));
                }

                for (int i = 0; i < max_motor; i++)
                {
                    _axes_onMoving[i] = _statusArea[i];
                    _axes_error[i] = _statusArea[2 + i];
                    _axes_homeSignal[i] = _statusArea[4 + i];
                    _axes_homeDone[i] = _statusArea[6 + i];
                    _axes_limit[i * 2] = _statusArea[8 + i * 2];
                    _axes_limit[i * 2 + 1] = _statusArea[9 + i * 2];

                    _axes_errId[i] = rcv_word[2 + i];
                    _axes_pos[i] = ((int)(rcv_word[4 + i * 2]) + (int)(rcv_word[5 + i * 2] << 16)) / _ppu[i];

                    _axes_vel[i] = ((int)(rcv_word[8 + i * 2]) + (int)(rcv_word[9 + i * 2] << 16)) / _ppu[i];
                }

                /**************************************************************
                 *  将操作变量赋值到对应的寄存器区域
                 *  1.将bool操作变量存入_writeArea区域。调换了高低字节。
                 *  2.将_writeArea需要发送的数据拷贝到一个临时区域
                 *  3.将临时区域数据发送
                 *  4.接收结果，如果接收了12个字符说明正确
                 * 
                 * ************************************************************/
                ushort[] opr_word = new ushort[2];
                for (int i = 0; i < 16; i++)
                {
                    if (_operaArea[i])
                    {
                        opr_word[0] |= (ushort)(0x01 << i);
                    }
                    else
                    {
                        opr_word[0] &= (ushort)(~(0x01 << i));
                    }
                    if (_operaArea[i + 16])
                    {
                        opr_word[1] |= (ushort)(0x01 << i);
                    }
                    else
                    {
                        opr_word[1] &= (ushort)(~(0x01 << i));
                    }
                }
                // 交换高低字节
                _writeArea[0] = (ushort)(opr_word[0] >> 8 | opr_word[0] << 8);
                _writeArea[1] = (ushort)(opr_word[1] >> 8 | opr_word[1] << 8);


                // 拷贝操作模式, 交换字的高低字节
                _writeArea[2] = (ushort)(Convert.ToUInt16(_moveMode[0]) << 8);
                _writeArea[3] = (ushort)(Convert.ToUInt16(_moveMode[1]) << 8);

                // 拷贝限位数据
                int[] softLimit_pulse = new int[4];
                for (int i = 0; i < 2; i++)
                {
                    softLimit_pulse[i] = Convert.ToInt32(_softLimit[i] * _ppu[0]);
                    softLimit_pulse[i + 2] = Convert.ToInt32(_softLimit[i + 2] * _ppu[1]);
                }
                for (int i = 0; i < 4; i++)
                {
                    byte[] limit_byt = BitConverter.GetBytes(softLimit_pulse[i]);
                    _writeArea[12 + i * 2] = (ushort)(limit_byt[0] << 8 | limit_byt[1]);
                    _writeArea[13 + i * 2] = (ushort)(limit_byt[2] << 8 | limit_byt[3]);
                }

                // 拷贝加减速时间, 交换字的高低字节
                _writeArea[20] = (ushort)(AccTime[0] << 8 | AccTime[0] >> 8);
                _writeArea[21] = (ushort)(AccTime[1] << 8 | AccTime[1] >> 8);
                _writeArea[22] = (ushort)(AccTime[2] << 8 | AccTime[2] >> 8);
                _writeArea[23] = (ushort)(AccTime[3] << 8 | AccTime[3] >> 8);

                // 拷贝回零偏置，回零速度
                uint[] homeOffset_pulse = new uint[2];
                homeOffset_pulse[0] = (uint)(_homeOffset[0] * _ppu[0]);
                homeOffset_pulse[1] = (uint)(_homeOffset[1] * _ppu[1]);
                uint[] homeVel_pulse = new uint[2];
                homeVel_pulse[0] = (uint)(_homeVel[0] * _ppu[0]);
                homeVel_pulse[1] = (uint)(_homeVel[1] * _ppu[1]);
                for (int i = 0; i < 2; i++)
                {
                    byte[] homeOffset_byt = BitConverter.GetBytes(homeOffset_pulse[i]);
                    _writeArea[24 + i * 2] = (ushort)(homeOffset_byt[0] << 8 | homeOffset_byt[1]);
                    _writeArea[25 + i * 2] = (ushort)(homeOffset_byt[2] << 8 | homeOffset_byt[3]);

                    byte[] homeVel_byt = BitConverter.GetBytes(homeVel_pulse[i]);
                    _writeArea[28 + i * 2] = (ushort)(homeVel_byt[0] << 8 | homeVel_byt[1]);
                    _writeArea[29 + i * 2] = (ushort)(homeVel_byt[2] << 8 | homeVel_byt[3]);

                }

                // 写入操作数据 20word
                const int write_len = 31;
                ushort[] wrtArea = new ushort[write_len];
                Array.Copy(_writeArea, 0, wrtArea, 0, write_len);
                bool ok2 = write_holdRegister(100, write_len, wrtArea);
                if (!ok2)
                {
                    // 检查通信状态

                    return;
                }

                // 变量自复位
                if (oprAutoReset)
                {
                    // 对以下索引的操作变量进行复位
                    uint[] resetIndex = { 0, 1, 2, 3, 4, 5, 10, 11 };
                    foreach (uint index in resetIndex)
                    {
                        _operaArea[index] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("[KPlc.Req] 发生错误：");
                Console.WriteLine(ex.Message);
                Connected = false;
                disconnectFromDevice();  //断开连接
            }
        }

        public bool oprAutoReset = true;  //操作变量是否进行自动复位


        #region 参数读写
        /* 参数包括
         * ip port modbusAdress ppu softLimit
         * */

        /// <summary>
        /// 存储参数
        /// </summary>
        SerializableDictionary<string, string> _param = new SerializableDictionary<string, string>();

        /// <summary>
        /// 保存参数
        /// 参数-参数字典-文件
        /// </summary>
        /// <returns></returns>
        public bool saveConfig()
        {
            // 
            _param.Clear();
            _param.Add("ip", Ip);
            _param.Add("port", string.Format("{0}", Port));
            _param.Add("slaveAdress", string.Format("{0}", SlaveAdress));
            _param.Add("A1_softLimitN", string.Format("{0}", _softLimit[0]));
            _param.Add("A1_softLimitP", string.Format("{0}", _softLimit[1]));
            _param.Add("A2_softLimitN", string.Format("{0}", _softLimit[2]));
            _param.Add("A2_softLimitP", string.Format("{0}", _softLimit[3]));
            _param.Add("A1_accTime", string.Format("{0}", _accTime[0]));
            _param.Add("A1_decTime", string.Format("{0}", _accTime[1]));
            _param.Add("A2_accTime", string.Format("{0}", _accTime[2]));
            _param.Add("A2_decTime", string.Format("{0}", _accTime[3]));
            _param.Add("A1_homeOffset", string.Format("{0}", _homeOffset[0]));
            _param.Add("A2_homeOffset", string.Format("{0}", _homeOffset[1]));
            _param.Add("A1_homeVel", string.Format("{0}", _homeVel[0]));
            _param.Add("A2_homeVel", string.Format("{0}", _homeVel[1]));
            _param.Add("A1_ppu", string.Format("{0}", _ppu[0]));
            _param.Add("A2_ppu", string.Format("{0}", _ppu[1]));

            bool ret = false;
            try
            {
                using (FileStream fileStream = new FileStream("plcParam.xml", FileMode.Create))
                {
                    XmlSerializer xmlFormatter = new XmlSerializer(typeof(SerializableDictionary<string, string>));
                    xmlFormatter.Serialize(fileStream, this._param);
                    ret = true;
                    fileStream.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[KPlc.saveConfig] 写参数错误 ");
                Console.WriteLine(ex);
            }
            finally
            {
            }

            return ret;
        }

        public bool loadConfig()
        {
            bool ret = false;

            if(!System.IO.File.Exists("plcParam.xml"))
            {
                Console.WriteLine("[KPlc.loadConfig] 默认配置文件不存在，使用默认参数");
                return ret;
            }

            try
            {
                using (FileStream fileStream = new FileStream("plcParam.xml", FileMode.Open))
                {
                    XmlSerializer xmlFormatter = new XmlSerializer(typeof(SerializableDictionary<string, string>));
                    this._param = (SerializableDictionary<string, string>)xmlFormatter.Deserialize(fileStream);

                    // 将数据存入变量
                    Ip = _param["ip"];
                    Port = Convert.ToUInt32(_param["port"]);
                    SlaveAdress = Convert.ToByte(_param["slaveAdress"]);
                    _softLimit[0] = Convert.ToDouble(_param["A1_softLimitN"]);
                    _softLimit[1] = Convert.ToDouble(_param["A1_softLimitP"]);
                    _softLimit[2] = Convert.ToDouble(_param["A2_softLimitN"]);
                    _softLimit[3] = Convert.ToDouble(_param["A2_softLimitP"]);
                    _accTime[0] = Convert.ToUInt16(_param["A1_accTime"]);
                    _accTime[1] = Convert.ToUInt16(_param["A1_decTime"]);
                    _accTime[2] = Convert.ToUInt16(_param["A2_accTime"]);
                    _accTime[3] = Convert.ToUInt16(_param["A2_decTime"]);
                    _homeOffset[0] = Convert.ToDouble(_param["A1_homeOffset"]);
                    _homeOffset[1] = Convert.ToDouble(_param["A2_homeOffset"]); 
                    _homeVel[0] = Convert.ToDouble(_param["A1_homeVel"]);
                    _homeVel[1] = Convert.ToDouble(_param["A2_homeVel"]);
                    _ppu[0] = Convert.ToDouble(_param["A1_ppu"]);
                    _ppu[1] = Convert.ToDouble(_param["A2_ppu"]);

                    ret = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[KPlc.saveConfig] 读参数错误 ");
                Console.WriteLine(ex);
                ret = false;
            }
            finally
            {
            }

            return ret;
        }
        #endregion


        #region 辅助功能
        public static bool checkRealZero(double fml_v)
        {
            if(Math.Abs(fml_v) < 1e-6 )
            { return false; }

            return true;
        }
        #endregion
    }
}
