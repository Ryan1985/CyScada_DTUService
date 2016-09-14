﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private  volatile uint SiteOnline = 0;
        private  object m_lock = new object();
        private  List<Hashtable> SiteInfoList = new List<Hashtable>();
        private  Dictionary<string, Hashtable> SiteList = new Dictionary<string, Hashtable>();

        public void StartServer(int port)
        {
            if (DTUWrapper.DSStartService((ushort) port) == 0)
            {
                var exception=string.Empty;
                DTUWrapper.DSGetLastError(exception, 500);
                throw new Exception("启动DTU监听失败" + exception);
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
                    rc = DTUWrapper.DSGetNextData(ref dds, 1);
                    if (rc != 0)
                    {
                        Logger.Enqueue("DTU服务收到数据");
                        var phoneNo = Encoding.Default.GetString(dds.m_phoneno);
                        var configModel = ConfigurationAdapter.GetConfigTable()[phoneNo];
                        switch (configModel.ServerFunctionCode)
                        {
                            case 1:
                            case 2:
                                break;
                            case 4:
                                {
                                    //判断站号
                                    if (dds.m_data_buf[0] != configModel.DTUDeviceId)
                                    {
                                        Logger.Enqueue("DTU服务收到错误DEVICEID：" + dds.m_data_buf[0]);
                                    }
                                    if (dds.m_data_buf[1] != 4)
                                    {
                                        Logger.Enqueue("DTU服务收到错误FunctionCode：" + dds.m_data_buf[1]);
                                    }

                                    //验证CRC
                                    if (!ValidateCrc(dds.m_data_buf))
                                    {
                                        Logger.Enqueue("DTU服务收到错误CRC：验证未通过");
                                    }

                                    //解析读协议
                                    var length = dds.m_data_buf[2];
                                    var strBuf = new StringBuilder();
                                    for (var i = 3; i < length; i++)
                                    {
                                        slaveDataStore.InputRegisters[configModel.ServerAddressStart + i - 3] =
                                            dds.m_data_buf[i];
                                        strBuf.Append(dds.m_data_buf[i].ToString());
                                    }
                                    File.AppendAllText(
                                        configModel.PhoneNo + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt",
                                        "[" + DateTime.Now + "]" + strBuf.ToString() + "\r\n");

                                }
                                break;
                            case 3:
                                {
                                    //判断站号
                                    if (dds.m_data_buf[0] != configModel.DTUDeviceId)
                                    {
                                        Logger.Enqueue("DTU服务收到错误DEVICEID：" + dds.m_data_buf[0]);

                                    }
                                    if (dds.m_data_buf[1] != 3)
                                    {
                                        Logger.Enqueue("DTU服务收到错误FunctionCode：" + dds.m_data_buf[1]);

                                    }
                                    //验证CRC
                                    if (!ValidateCrc(dds.m_data_buf))
                                    {
                                        Logger.Enqueue("DTU服务收到错误CRC：验证未通过");
                                    }

                                    //解析读协议
                                    var length = dds.m_data_buf[2];
                                    var strBuf = new StringBuilder();
                                    for (var i = 3; i < length; i++)
                                    {
                                        slaveDataStore.HoldingRegisters[configModel.ServerAddressStart + i - 3] =
                                            dds.m_data_buf[i];
                                    }
                                    File.AppendAllText(
                                        configModel.PhoneNo + "_" + DateTime.Now.Date.ToString("yyyyMMdd") + ".txt",
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
                            SiteList.Add(siteinfo["SIM"].ToString(), siteinfo);
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
                            var result = DTUWrapper.DSSendData(Encoding.Default.GetBytes(kv.Value.PhoneNo), (ushort)sendBytes.Length, sendBytes);
                            Logger.Enqueue("发送数据到" + kv.Value.PhoneNo+":"+result.ToString());
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
                    rc = DTUWrapper.DSGetDtuCount();
                    if (rc != SiteOnline)
                    {
                        Logger.Enqueue("刷新DTU列表");
                        lock (m_lock)
                        {
                            SiteInfoList.Clear();
                            for (uint i = 0; i < rc; i++)
                            {
                                DtuInfoStruct dis = new DtuInfoStruct();
                                if (DTUWrapper.DSGetDtuByPosition(i, ref dis) > 0)
                                {
                                    string id = Encoding.Default.GetString(dis.m_dtuId, 0, 9);
                                    string sim = Encoding.Default.GetString(dis.m_phoneno, 0, 11);
                                    string ip = StIPtoString(dis.m_dynip);
                                    string sitename = string.Format("{0}:{1}", id, sim);
                                    SiteInfoList.Add(new Hashtable
                                    {
                                        {"SiteName", sitename},
                                        {"ID", id},
                                        {"SIM", sim},
                                        {"IP", ip},
                                        {"ConnTime", UxToDateTime(dis.m_conn_time)},
                                        {"LastTime", UxToDateTime(dis.m_refresh_time)},

                                    });
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
            DTUWrapper.DSStopService();
        }

    }
}
