using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Modbus.Data;
using Modbus.Utility;

namespace DTUServiceMonitor
{
    public class DTUAdapter
    {

        private  bool IsRunning = true;

        public  DataStore slaveDataStore;
        public volatile bool showLog = false;
        private  volatile uint SiteOnline = 0;
        private  object m_lock = new object();
        private  List<Hashtable> SiteInfoList = new List<Hashtable>();
        private  Dictionary<string, Hashtable> SiteList = new Dictionary<string, Hashtable>();

        public void StartServer(int port)
        {
            if (GPRSDTUWrapper.DSStartService((ushort) port) == 0)
            {
                var exception = Marshal.AllocHGlobal(200);
                GPRSDTUWrapper.DSGetLastError(exception, 200);
                throw new Exception("启动DTU监听失败" + Marshal.PtrToStringAnsi(exception));
            }

            Logger.Enqueue("DTU服务启动");

            var dds = new DtuDataStruct();
            //读取线程
            #region 读取线程
            ThreadPool.QueueUserWorkItem(o =>
            {
                int rc = 0;
                while (IsRunning)
                {   
                    rc = GPRSDTUWrapper.DSGetNextData(ref dds, 1);
                    if (rc != 0)
                    {
                        //var phoneNo = Encoding.Default.GetString(dds.m_phoneno);
                        var modId = dds.m_dtuId;
                        var configModel = ConfigurationAdapter.GetConfigTable()[BitConverter.ToInt32(modId,0).ToString()];
                        switch (configModel.ServerFunctionCode)
                        {
                            case 1:
                            case 2:
                                break;
                            case 4:
                                {
                                    if (showLog)
                                    {
                                        Logger.Enqueue("DTU收到数据dtuId：" + BitConverter.ToInt32(dds.m_dtuId, 0));
                                        Logger.Enqueue("DTU收到数据data_len：" + BitConverter.ToInt16(dds.m_data_len, 0));
                                    }
                                    //判断站号
                                    if (dds.m_data_buf[0] != configModel.DTUDeviceId)
                                    {
                                        Logger.Enqueue("DTU服务收到错误DEVICEID：" + dds.m_data_buf[0]);
                                    }
                                    if (dds.m_data_buf[1] != 4)
                                    {
                                        Logger.Enqueue("DTU服务收到错误FunctionCode：" + dds.m_data_buf[1]);
                                    }


                                    var realData = new byte[BitConverter.ToInt16(dds.m_data_len, 0)];
                                    Buffer.BlockCopy(dds.m_data_buf, 0, realData, 0, realData.Length);
                                    //验证CRC
                                    if (!ValidateCrc(realData))
                                    {
                                        Logger.Enqueue("DTU服务收到错误CRC：验证未通过");
                                    }

                                    //解析读协议
                                    var length = dds.m_data_buf[2];

                                    if (showLog)
                                    {
                                        Logger.Enqueue("协议数据包长度：" + length);
                                    }
                                    var strBuf = new StringBuilder();
                                    for (var i = 3; i < length+3; i++)
                                    {
                                        if (showLog)
                                        {
                                            Logger.Enqueue("写入INPUT寄存器：" + (configModel.ServerAddressStart + i - 3) + "|" +
                                                           (dds.m_data_buf[i]));
                                        }
                                        slaveDataStore.InputRegisters[configModel.ServerAddressStart + i - 3] =
                                            dds.m_data_buf[i];
                                        strBuf.Append(dds.m_data_buf[i].ToString());
                                    }
                                    File.AppendAllText(
                                        configModel.DTUId + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt",
                                        "[" + DateTime.Now + "]" + strBuf.ToString() + "\r\n");

                                }
                                break;
                            case 3:
                                {

                                    if (showLog)
                                    {
                                        Logger.Enqueue("DTU收到数据dtuId：" + BitConverter.ToInt32(dds.m_dtuId, 0));
                                        Logger.Enqueue("DTU收到数据data_len：" + BitConverter.ToInt16(dds.m_data_len, 0));
                                    }
                                    //判断站号
                                    if (dds.m_data_buf[0] != configModel.DTUDeviceId)
                                    {
                                        Logger.Enqueue("DTU服务收到错误DEVICEID：" + dds.m_data_buf[0]);

                                    }
                                    if (dds.m_data_buf[1] != 3)
                                    {
                                        Logger.Enqueue("DTU服务收到错误FunctionCode：" + dds.m_data_buf[1]);

                                    }
                                    var realData = new byte[BitConverter.ToInt16(dds.m_data_len, 0)];
                                    Buffer.BlockCopy(dds.m_data_buf, 0, realData, 0, realData.Length);
                                    //验证CRC
                                    if (!ValidateCrc(realData))
                                    {
                                        Logger.Enqueue("DTU服务收到错误CRC：验证未通过");
                                    }

                                    //解析读协议
                                    var length = dds.m_data_buf[2];
                                    if (showLog)
                                    {
                                        Logger.Enqueue("协议数据包长度：" + length);
                                    }
                                    var strBuf = new StringBuilder();
                                    for (var i = 3; i < length+3; i++)
                                    {
                                        if (showLog)
                                        {
                                            Logger.Enqueue("写入INPUT寄存器：" + (configModel.ServerAddressStart + i - 3) + "|" +
                                                           (dds.m_data_buf[i]));
                                        }
                                        slaveDataStore.HoldingRegisters[configModel.ServerAddressStart + i - 3] =
                                            dds.m_data_buf[i];
                                    }
                                    File.AppendAllText(
                                        configModel.DTUId + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt",
                                        "[" + DateTime.Now + "]" + strBuf.ToString() + "\r\n");

                                } break;
                        }
                    }
                    Thread.Sleep(50);
                }
            });
            #endregion 读取线程

            //寻址线程
            #region 寻址线程
            ThreadPool.QueueUserWorkItem(o =>
            {
                while (IsRunning)
                {
                    lock (m_lock)
                    {
                        SiteList = new Dictionary<string, Hashtable>();
                        foreach (var siteinfo in SiteInfoList)
                        {
                            SiteList.Add(siteinfo["ID"].ToString(), siteinfo);
                        }
                    }

                    var configTable = ConfigurationAdapter.GetConfigTable();
                    foreach (var kv in configTable)
                    {
                        if (SiteList.ContainsKey(kv.Key))
                        {
                            var dataByte = new byte[6];
                            dataByte[0] = (byte)kv.Value.DTUDeviceId;
                            dataByte[1] = (byte)kv.Value.DTUFunctionCode;
                            dataByte[2] = (byte)(kv.Value.DTUAddressStart / 256);
                            dataByte[3] = (byte)(kv.Value.DTUAddressStart % 256);
                            dataByte[4] = (byte)(kv.Value.DTUAddressLength / 256);
                            dataByte[5] = (byte)(kv.Value.DTUAddressLength % 256);
                            var crc = ModbusUtility.CalculateCrc(dataByte);
                            var sendBytes = new byte[8];
                            Buffer.BlockCopy(dataByte, 0, sendBytes, 0, 6);
                            Buffer.BlockCopy(crc, 0, sendBytes, 6, 2);
                            var result = GPRSDTUWrapper.DSSendData(uint.Parse(kv.Value.DTUId), (ushort)sendBytes.Length, sendBytes);

                            if (showLog)
                            {
                                Logger.Enqueue("发送数据到" + kv.Value.DTUId + ":" + result.ToString());
                            }
                            if (result == 0)
                            {
                                try
                                {

                                    var strError = Marshal.AllocHGlobal(200);
                                    GPRSDTUWrapper.DSGetLastError(strError, 200);
                                    Logger.Enqueue(Marshal.PtrToStringAnsi(strError));
                                }
                                catch (Exception ex)
                                {
                                    Logger.Enqueue(ex.Message);
                                }
                            }
                            var str = new StringBuilder();
                            for (var i = 0; i < sendBytes.Length; i++)
                            {
                                str.AppendFormat(@"{0} ", sendBytes[i].ToString());
                            }
                            File.AppendAllText(
                                    kv.Key + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + "_Sent.txt",
                                    "[" + DateTime.Now + "]" + str.ToString() + "\r\n");
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
            #endregion

            //接收DTU线程
            #region 接收DTU连接线程
            ThreadPool.QueueUserWorkItem(o =>
            {
                while (IsRunning)
                {
                    uint rc = 0;
                    rc = GPRSDTUWrapper.DSGetModemCount();
                    if (rc != SiteOnline)
                    {
                        Logger.Enqueue("刷新DTU列表");
                        lock (m_lock)
                        {
                            SiteInfoList.Clear();
                            for (uint i = 0; i < rc; i++)
                            {
                                DtuInfoStruct dis = new DtuInfoStruct();
                                if (GPRSDTUWrapper.DSGetModemByPosition(i, ref dis) > 0)
                                {
                                    int id =BitConverter.ToInt32(dis.m_dtuId,0);
                                    string sim = Encoding.Default.GetString(dis.m_phoneno,0,11);
                                    string ip = StIPtoString(dis.m_dynip);
                                    string sitename = string.Format("{0}:{1}", id, sim);
                                    SiteInfoList.Add(new Hashtable
                                    {
                                        {"SiteName", sitename},
                                        {"ID", id},
                                        {"SIM", sim},
                                        {"IP", ip},
                                        //{"ConnTime", UxToDateTime(dis.m_conn_time)},
                                        //{"LastTime", UxToDateTime(dis.m_refresh_time)},

                                    });
                                    Logger.Enqueue("id:"+id);
                                    Logger.Enqueue("sim:"+sim);
                                    Logger.Enqueue("ip:"+ip);
                                    Logger.Enqueue("sitename:"+sitename);
                                }
                            }
                        }
                    }
                    SiteOnline = rc;
                    Thread.Sleep(5000);
                }
            });
            #endregion
        }

        private bool ValidateCrc(byte[] buf)
        {
            var datalength = (int)(buf[2]);
            var databuf = new byte[buf.Length - 2];
            Buffer.BlockCopy(buf, 0, databuf, 0, databuf.Length);
            var crc = ModbusUtility.CalculateCrc(databuf);
            return (crc[0] == buf[datalength + 3] && crc[1] == buf[datalength + 3 + 1]);
        }


        private DateTime UxToDateTime(ulong time)
        {
            DateTime dt = new DateTime(1970, 1, 1);
            dt = dt.AddSeconds(Convert.ToDouble(time));
            dt = dt.AddHours(8);
            return dt;
        }        

        private string StIPtoString(byte[] IPa)
        {
            string ip = "";
            if (IPa != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    ip += string.Format("{0}", IPa[i].ToString());
                    if (i < 3)
                        ip += ".";
                }
            }
            return ip;
        }



        public void Dispose()
        {
            IsRunning = false;
            GPRSDTUWrapper.DSStopService();
        }

    }
}
