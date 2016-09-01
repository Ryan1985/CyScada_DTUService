using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Modbus.Data;

namespace DTUServiceMonitor
{
    public class DTUAdapter
    {

        private static bool IsRunning = true;

        public static DataStore slaveDataStore;





        public static void StartServer(int port)
        {
            if (DTUWrapper.DSStartService((ushort) port) == 0)
            {
                throw new Exception("启动DTU监听失败");
            }

            DtuDataStruct dds = new DtuDataStruct();
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
                        var dtuId = Encoding.Default.GetString(dds.m_dtuId);
                        var configModel = ConfigurationAdapter.GetConfigTable()[dtuId];
                        switch (configModel.ServerFunctionCode)
                        {
                            case 1:
                            case 2:
                            case 4:
                                continue;
                            case 3:
                            {
                                for (var i = 0; i < dds.m_data_len; i++)
                                {
                                    slaveDataStore.HoldingRegisters[configModel.ServerAddressStart + i] =
                                        dds.m_data_buf[i];
                                }
                                continue;
                            }
                        }


                        //if (IsHexShow)
                        //{
                        //    string d = ByteToString(dds.m_data_buf, dds.m_data_len);
                        //    str = string.Format("{0}{1}", str, d);
                        //}
                        //else
                        //{
                        //    str = string.Format("{0}{1}", str,
                        //        Encoding.Default.GetString(dds.m_data_buf, 0, dds.m_data_len));
                        //}
                        //UpdateInfoTxtBox(str);
                    }
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

                        //DTUWrapper.DSSendData(Encoding.Default.GetBytes(kv.Value.PhoneNo), (ushort)rc, data);
                    }

                    Thread.Sleep(100);
                }
            });


        }






        public static void Dispose()
        {
            DTUWrapper.DSStopService();
        }

    }
}
