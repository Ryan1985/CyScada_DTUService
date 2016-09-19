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

        private DTUAdapter dtuAdapter = new DTUAdapter();


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
                Logger.Enqueue("程序开始启动..."); 
                modeSlave = ModbusTcpSlave.CreateTcp(1,
                    new TcpListener(IPAddress.Any, int.Parse(txtServerPort.Text)));
                modeSlave.Listen();
                Logger.Enqueue("MODBUS服务已经启动完成");
                dtuAdapter.slaveDataStore = modeSlave.DataStore;
                dtuAdapter.StartServer(int.Parse(txtDTUPort.Text));
                ThreadPool.QueueUserWorkItem(o =>
                {
                    while (IsRunning || Logger.Count != 0)
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
            Logger.Enqueue("程序退出..."); 
            modeSlave.Dispose();
            dtuAdapter.Dispose();
            Logger.Enqueue("程序退出完毕"); 
            IsRunning = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (dtuAdapter != null)
            {
                dtuAdapter.showLog = checkBox1.Checked;
            }
        }
    }
}
