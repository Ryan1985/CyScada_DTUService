using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;

namespace ModSimWrapper
{
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
    public struct DtuInfoStruct
    {
        public byte[] m_dtuId;                                  //DTU模块的ID号，9个ASCII码，必须以‘\0'字符结尾
        public byte[] m_phoneno;                                //DTU的11位电话号码，必须以'\0'字符结尾
        public byte[] m_dynip;                                  //DTU的4位动态IP地址（为IP绑定新增）
        public ulong m_conn_time;                               //DTU模块最后一次建立TCP连接的时间
        public ulong m_refresh_time;                            //DTU模块最后一次收发数据的时间
    }

    public struct DtuDataStruct
    {
        public byte[] m_dtuId;                                  //DTU模块的ID号，9个ASCII码，必须以‘\0'字符结尾
        public byte[] m_phoneno;                                //DTU的11位电话号码，必须以'\0'字符结尾
        public byte[] m_recv_time;                               //接收到数据包的时间
        public byte[] m_data_buf;                               //存储接收到的数据
        public byte[] m_data_len;                               //接收到的数据包长度
        public byte m_data_type;                                //接收到的数据包类型 0x01用户数据包 0x00不认识的数据
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
    public struct RtuDataStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] m_dtuId;                                  //RTU模块的ID号，9个ASCII码，必须以‘\0'字符结尾
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] m_phoneno;                                //RTU的11位电话号码，必须以'\0'字符结尾
        public ulong m_recv_time;                               //接收到数据包的时间
        public byte m_relay;                                    //8个继电器的状态，位寻址
        public byte m_di;                                       //8个开关量输入的状态，位寻址
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ushort[] m_ai;                                   //16路模拟输入的值
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public ushort[] m_pulse;                                //8路脉冲量输入的值
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] m_date;                                   //RTU时间
        public byte m_csq;                                      //RTU信号量
        public ushort m_dc;                                     //RTU电压值
        public ushort m_rtu_version;                            //RTU版本
    }





    public class DTUSimWrapper
    {
        //private static ModbusSerialMaster modMaster;

        private static SerialPort sp = new SerialPort("COM2", 9600, Parity.Odd, 8,StopBits.One);
        private static volatile bool IsRunning = true;
        private static List<byte[]> DataQueue = new List<byte[]>();
        private static object m_lock = new object();


        public static int DSStartService(ushort uiListenPort)
        {
            sp.DataReceived += sp_DataReceived;
            sp.Open();

            //modMaster =
            //    ModbusSerialMaster.CreateRtu(sp);
            return 1;
        }

        static void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var frame = new byte[sp.BytesToRead];
            var readLength = sp.Read(frame, 0, sp.BytesToRead);
            var remainLength = sp.BytesToRead - readLength;
            while (remainLength > 0)
            {
                var readBytesLength = sp.Read(frame, readLength, remainLength);
                readLength = readBytesLength + readLength;
                remainLength = remainLength - readLength;
            }

            lock (m_lock)
            {
                DataQueue.Add(frame);
            }

        }

        public static int DSStopService()
        {
            IsRunning = false;
            //modMaster.Dispose();
            sp.Close();
            return 1;
        }

        public static int DSGetNextData(ref DtuDataStruct pDataStruct, ushort waitseconds)
        {
            if (IsRunning)
            {
                if (DataQueue.Count == 0)
                {
                    return 0;
                }

                //解析协议
                var frame = new byte[0];
                lock (m_lock)
                {
                    frame = DataQueue[0];
                    DataQueue.RemoveAt(0);
                }

                var result = ExtractFrame(ref frame);

                if (!result)
                {
                    return 0;
                }

                pDataStruct.m_data_buf = frame;
                pDataStruct.m_data_len = BitConverter.GetBytes(frame.Length);
                pDataStruct.m_phoneno = BitConverter.GetBytes(13300000000);
                pDataStruct.m_dtuId = BitConverter.GetBytes(1953719668);
                return 1;
            }
            return 1;
        }

        private static bool ExtractFrame(ref byte[] frame)
        {
            var bufFrame = new byte[frame.Length];
            Buffer.BlockCopy(frame, 0, bufFrame, 0, frame.Length);
            while (IsRunning)
            {
                if (bufFrame.Length <= 3)//数据不全,则准备取下一贞数据
                {
                    if (AppendNextFrame(ref bufFrame) == false)//没有下一帧，返回
                    {
                        lock (m_lock)
                        {
                            DataQueue.Insert(0, bufFrame);
                        }
                        return false;
                    }
                    continue;
                }

                var frameLength = bufFrame[2];
                if (bufFrame.Length < 3 + frameLength + 2)//如果数据不全(协议中的数据长度<头_3B+数据_bufFrame[2]+CRC_2B),则准备取下一帧
                {
                    if (AppendNextFrame(ref bufFrame) == false)//没有下一帧，返回
                    {
                        lock (m_lock)
                        {
                            DataQueue.Insert(0, bufFrame);
                        }
                        return false;
                    }
                    continue;
                }
                //一帧数据完整并且保存在了bufFrame中
                break;
            }

            //切分贞，将多余数据放入DataQueue
            var modFrameLength = 3 + bufFrame[2] + 2;
            frame = new byte[modFrameLength];
            Buffer.BlockCopy(bufFrame, 0, frame, 0, modFrameLength);
            var modFragment = new byte[bufFrame.Length - 3 - bufFrame[2] - 2];
            if (modFragment.Length > 0)
            {
                Buffer.BlockCopy(bufFrame, modFrameLength, modFragment, 0, modFragment.Length);
                DataQueue.Insert(0, modFragment);
            }
            return true;
        }

        private static bool AppendNextFrame(ref byte[] frame)
        {
            if (DataQueue.Count == 0)//没有下一帧则退出
            {
                return false;
            }

            lock (m_lock)
            {
                var section = DataQueue[0];
                DataQueue.RemoveAt(0);
                var bufFrame = new byte[frame.Length + section.Length];
                Buffer.BlockCopy(frame, 0, bufFrame, 0, frame.Length);
                Buffer.BlockCopy(section, 0, bufFrame, frame.Length, section.Length);
                frame = bufFrame;
            }
            return true;
        }

        public static int DSSendData(byte[] pPhone, ushort len, byte[] buf)
        {
            if (IsRunning)
            {
                sp.Write(buf, 0, len);
            }
            return 1;
        }

        //public static uint DSGetDtuCount()
        //{
        //    throw new NotImplementedException();
        //}

        //public static int DSGetDtuByPosition(uint pos, ref DtuInfoStruct pDtuInfo)
        //{
        //    pDtuInfo.m_dtuId
        //}

        public static void DSGetLastError(IntPtr str, int nMaxStrSize)
        {
            
        }

        //public static int DSDisconnect(byte[] pPhone)
        //{
        //    return 1;
        //}

        //public static int DSSendRtuReadCmd(byte[] pPhone)
        //{
        //    throw new NotImplementedException();
        //}

        //public static int DSSendRtuRelayCmd(byte[] pPhone, byte Relay, byte Value, ushort Timeout)
        //{
        //    throw new NotImplementedException();
        //}

        //public static int DSSendRtuSetTime(byte[] pPhone)
        //{
        //    throw new NotImplementedException();
        //}

        //public static int DSGetRtuData(byte[] pPhone, ref RtuDataStruct rtu, ushort waitseconds)
        //{
        //    throw new NotImplementedException();
        //}






        internal static uint DSGetModemCount()
        {
            return 1;
        }

        internal static int DSGetModemByPosition(uint pos, ref DtuInfoStruct infostruct)
        {
            infostruct.m_conn_time = 0;
            infostruct.m_dtuId = BitConverter.GetBytes(1953719668);
            infostruct.m_dynip = BitConverter.GetBytes(0);
            infostruct.m_phoneno=BitConverter.GetBytes(1953719668);
            infostruct.m_refresh_time = 0;
            return 1;
        }
    }
}
