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

namespace DTUServiceMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Queue<string> loggerQueue = new Queue<string>();
        private List<Socket> clientPool = new List<Socket>(2);
        private volatile bool IsRunning = true;


        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (DTUWrapper.DSStartService(ushort.Parse(txtDTUPort.Text)) != 0)
                {

                    loggerQueue.Enqueue("启动DTU服务成功");
                }
                else
                {
                    loggerQueue.Enqueue("启动DTU服务失败");
                }

                IPEndPoint serverIP = new IPEndPoint(IPAddress.Any, int.Parse(txtServerPort.Text)); //本机预使用的IP和端口
                Socket skServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                skServer.Bind(serverIP);
                skServer.Listen(100);

                var mainThread = new Thread(() =>
                {
                    try
                    {
                        while (IsRunning)
                        {
                            Socket clientSocket;
                            try
                            {
                                //当有可用的客户端连接尝试时执行，并返回一个新的socket,用于与客户端之间的通信
                                clientSocket = skServer.Accept();
                                clientPool.Add(clientSocket);
                                var execThread = new Thread(o =>
                                {
                                    var sock = o as Socket;
                                    var readEvent = new SocketAsyncEventArgs();
                                    readEvent.Completed += (readSock, eventArgs) =>
                                    {
                                        var currentsock = readSock as Socket;
                                        if (eventArgs.BytesTransferred > 0 &&
                                            eventArgs.SocketError == SocketError.Success)
                                        {
                                            //切换数据

                                            //发送数据给currentsock
                                        }
                                        else
                                        {
                                            currentsock.Close();
                                        }
                                    };
                                    sock.ReceiveAsync(readEvent);

                                    var sendEvent = new SocketAsyncEventArgs();
                                    sendEvent.Completed += (sendSock, eventArgs) =>
                                    {
                                        var currentsock = sendSock as Socket;
                                        if (eventArgs.BytesTransferred > 0 &&
                                            eventArgs.SocketError == SocketError.Success)
                                        {
                                            //切换数据

                                            //查找到对应的DTU

                                            //发送数据给DTU
                                        }
                                        else
                                        {
                                            currentsock.Close();
                                        }
                                    };
                                    sock.SendAsync(sendEvent);
                                });
                                execThread.Start(clientSocket);
                            }
                            catch (Exception ex)
                            {
                                loggerQueue.Enqueue("接收用户连接失败" + ex.Message);
                                continue;
                            }
                            Thread.Sleep(10);
                        }
                    }
                    catch (Exception ex)
                    {
                        loggerQueue.Enqueue("主线程内发生错误" + ex.Message + ",主线程已经退出!");
                    }

                });
                mainThread.Start();

            }
            catch (Exception ex)
            {
                loggerQueue.Enqueue("程序界面线程出错，" + ex.Message + ",程序界面线程已经退出!"); 
            }
        }



        private void btnStop_Click(object sender, EventArgs e)
        {
            IsRunning = false;
            foreach (var client in clientPool)
            {
                client.Close(1);
            }
            DTUWrapper.DSStopService();
        }
    }
}
