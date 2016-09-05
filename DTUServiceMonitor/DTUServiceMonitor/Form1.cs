using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Modbus.Device;

namespace DTUServiceMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private volatile bool IsRunning = true;
        private ModbusTcpSlave modeSlave;
        private ModbusIpMaster modeMaster;
        private delegate void ShowText(RichTextBox rb,string str);

        private ShowText showText;

        private void ShowTxt(RichTextBox rb, string str)
        {
            rb.AppendText(str + "\r\n");
        }





        private void btnStart_Click(object sender, EventArgs e)
        {
            showText = ShowTxt;
            try
            {
                //if (DTUWrapper.DSStartService(ushort.Parse(txtDTUPort.Text)) != 0)
                //{

                //    loggerQueue.Enqueue("启动DTU服务成功");
                //}
                //else
                //{
                //    loggerQueue.Enqueue("启动DTU服务失败");
                //}


                modeSlave = ModbusTcpSlave.CreateTcp(1,
                    new TcpListener(IPAddress.Any, int.Parse(txtServerPort.Text)));
                //modeMaster = ModbusIpMaster.CreateIp(new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 502)));
                
                modeSlave.Listen();
                DTUAdapter.slaveDataStore = modeSlave.DataStore;
                DTUAdapter.StartServer(int.Parse(txtDTUPort.Text));

                ThreadPool.QueueUserWorkItem(o =>
                {
                    while (IsRunning)
                    {
                        if (Logger.Count > 0)
                        {
                            var str = Logger.Dequeue();
                            Invoke(showText, richTextBox1, str);
                        }
                        Thread.Sleep(100);
                    }
                });



            }
            catch (Exception ex)
            {
                Logger.Enqueue("程序界面线程出错，" + ex.Message + ",程序界面线程已经退出!"); 
            }
        }



        private void btnStop_Click(object sender, EventArgs e)
        {
            IsRunning = false;
            modeSlave.Dispose();
            DTUAdapter.Dispose();
        }
    }
}
