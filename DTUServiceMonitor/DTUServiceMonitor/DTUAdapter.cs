using System;
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

        private static bool IsRunning = true;

        public static DataStore slaveDataStore;


        public static void StartServer(int port)
        {
            if (DTUSimWrapper.DSStartService((ushort) port) == 0)
            {
                throw new Exception("启动DTU监听失败");
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
                    rc = DTUSimWrapper.DSGetNextData(ref dds, 1);
                    Logger.Enqueue("DTU服务收到数据");
                    if (rc != 0)
                    {
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
                                    slaveDataStore.HoldingRegisters[configModel.ServerAddressStart + i - 3] =
                                        dds.m_data_buf[i];
                                    strBuf.Append(dds.m_data_buf[i].ToString());
                                }
                                File.AppendAllText(configModel.PhoneNo + ".txt",
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
                                File.AppendAllText(configModel.PhoneNo + ".txt",
                                    "[" + DateTime.Now + "]" + strBuf.ToString() + "\r\n");

                            }break;
                        }
                    }
                    Thread.Sleep(100);
                }
            });
            #endregion 读取线程

            //寻址线程
            ThreadPool.QueueUserWorkItem(o =>
            {
                while (IsRunning)
                {
                    var configTable = ConfigurationAdapter.GetConfigTable();
                    foreach (var kv in configTable)
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
                        DTUSimWrapper.DSSendData(Encoding.Default.GetBytes(kv.Value.PhoneNo), (ushort)sendBytes.Length, sendBytes);
                    }
                    Thread.Sleep(100);
                }
            });


        }

        private static bool ValidateCrc(byte[] buf)
        {
            var datalength = (int)(buf[2]);
            var databuf = new byte[buf.Length - 2];
            Buffer.BlockCopy(buf, 0, databuf, 0, databuf.Length);
            var crc = ModbusUtility.CalculateCrc(databuf);
            return (crc[0] == buf[datalength] && crc[1] == buf[datalength + 1]);
        }






        public static void Dispose()
        {
            DTUSimWrapper.DSStopService();
        }

    }
}
