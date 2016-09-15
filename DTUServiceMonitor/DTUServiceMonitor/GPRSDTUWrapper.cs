using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DTUServiceMonitor
{
    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
    public struct DtuInfoStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_dtuId;                                    //DTU模块的ID号，9个ASCII码，必须以‘\0'字符结尾
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] m_dynip;                                  //DTU的4位动态IP地址（为IP绑定新增）
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] m_phoneno;                                //DTU的11位电话号码，必须以'\0'字符结尾 
        public ulong m_conn_time;                               //DTU模块最后一次建立TCP连接的时间
        public ulong m_refresh_time;                            //DTU模块最后一次收发数据的时间
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
    public struct DtuDataStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] m_dtuId;                                  //DTU模块的ID号，9个ASCII码，必须以‘\0'字符结尾
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] m_phoneno;                                //DTU的11位电话号码，必须以'\0'字符结尾
        public ulong m_recv_time;                               //接收到数据包的时间
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1451)]
        public byte[] m_data_buf;                               //存储接收到的数据
        public ushort m_data_len;                               //接收到的数据包长度
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

    public class GPRSDTUWrapper
    {
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSStartService(ushort uiListenPort);
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSStopService();
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSGetNextData(ref DtuDataStruct pDataStruct, ushort waitseconds);
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSSendData(byte[] pPhone, ushort len, byte[] buf);
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern uint DSGetModemCount();
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSGetModemByPosition(uint pos, ref DtuInfoStruct pDtuInfo);
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern void DSGetLastError( IntPtr str, int nMaxStrSize);
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSDisconnect(byte[] pPhone);

        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSSendRtuReadCmd(byte[] pPhone);
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSSendRtuRelayCmd(byte[] pPhone, byte Relay, byte Value, ushort Timeout);
        [DllImport("gprsdll.dll", CharSet = CharSet.Ansi)]
        public static extern int DSSendRtuSetTime(byte[] pPhone);
        [DllImport("gprsdll.dll")]
        public static extern int DSGetRtuData(byte[] pPhone, ref RtuDataStruct rtu, ushort waitseconds);
    }
}
