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

        private Queue<string> loggerQueue = new Queue<string>();
        private volatile bool IsRunning = true;
        private ModbusTcpSlave modeSlave;
        private ModbusIpMaster modeMaster;

        private void btnStart_Click(object sender, EventArgs e)
        {
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

                modeSlave.ModbusSlaveRequestReceived += (s, ev) =>
                {

                    //ev.Message.MessageFrame[0] <--功能吗
                    //ev.Message.MessageFrame[1]*256 + ev.Message.MessageFrame[2] <--起始地址
                    //ev.Message.MessageFrame[3]*256 + ev.Message.MessageFrame[4] <--长度

                };


            }
            catch (Exception ex)
            {
                loggerQueue.Enqueue("程序界面线程出错，" + ex.Message + ",程序界面线程已经退出!"); 
            }
        }



        private void btnStop_Click(object sender, EventArgs e)
        {
            IsRunning = false;
            modeSlave.Dispose();
        }
    }
}
