//来源：https://blog.csdn.net/qq_33022911/article/details/82432778

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using KRPCController;

namespace SocketUtil
{
    public class SocketServer
    {
        public bool listening = false;
        private List<Socket> clients = new List<Socket>();

        private object locker = new object();

        private string _ip = string.Empty;
        private int _port = 0;
        private Socket _socket = null;
        private byte[] buffer = new byte[1024 * 1024 * 2];
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ip">监听的IP</param>
        /// <param name="port">监听的端口</param>
        public SocketServer(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
        }
        public SocketServer(int port)
        {
            this._ip = "0.0.0.0";
            this._port = port;
        }

        public void StartListen()
        {
            try
            {
                //1.0 实例化套接字(IP4寻找协议,流式协议,TCP协议)
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //2.0 创建IP对象
                IPAddress address = IPAddress.Parse(_ip);
                //3.0 创建网络端口,包括ip和端口
                IPEndPoint endPoint = new IPEndPoint(address, _port);
                //4.0 绑定套接字
                _socket.Bind(endPoint);
                //5.0 设置最大连接数
                _socket.Listen(int.MaxValue);
                ConnectionInitializer.Log(string.Format("监听{0}消息成功", _socket.LocalEndPoint.ToString()));
                //6.0 开始监听
                Thread thread = new Thread(ListenClientConnect);
                thread.Start();
                listening = true;
            }
            catch (Exception ex)
            {
                ConnectionInitializer.Log(ex.Message);
            }
        }

        public void Update()
        {
            if (listening)
            {
                lock (locker)
                {
                    foreach (var clientSocket in clients)
                    {
                        if (clientSocket != null)
                        {
                            try
                            {
                                //获取从客户端发来的数据
                                if (clientSocket.Available > 0)
                                {
                                    int length = clientSocket.Receive(buffer);
                                    //var msg = Encoding.ASCII.GetString(buffer, 0, length);//no other char
                                    var bytes = new byte[length];
                                    Array.ConstrainedCopy(buffer, 0, bytes, 0, length);
                                    //ConnectionInitializer.Log(msg);
                                    Console.WriteLine("接收客户端{0},长度{2},消息{1}", clientSocket.RemoteEndPoint.ToString(), bytes.ToString(), bytes.Length);
                                    ConnectionInitializer.HandleSocketData(bytes);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                try
                                {
                                    clientSocket.Shutdown(SocketShutdown.Both);
                                    clientSocket.Close();
                                }catch(Exception e) { }
                                //break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 监听客户端连接
        /// </summary>
        private void ListenClientConnect()
        {
            try
            {
                while (true)
                {
                    //Socket创建的新连接
                    Socket clientSocket = _socket.Accept();//阻塞直到连接
                    //clientSocket.Send(Encoding.UTF8.GetBytes("服务端发送消息:"));
                    Console.WriteLine("连接到新客户端" + clientSocket.RemoteEndPoint.ToString());
                    //Thread thread = new Thread(ReceiveMessage);
                    //thread.Start(clientSocket);
                    lock (locker)
                    {
                        clients.Add(clientSocket);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 接收客户端消息
        /// </summary>
        /// <param name="socket">来自客户端的socket</param>
        private void ReceiveMessage(object socket)
        {
            Socket clientSocket = (Socket)socket;
            while (true)
            {
                try
                {
                    //获取从客户端发来的数据
                    int length = clientSocket.Receive(buffer);
                    var msg = Encoding.UTF8.GetString(buffer, 0, length);
                    //ConnectionInitializer.Log(msg);
                    Console.WriteLine("接收客户端{0},长度{2},消息{1}", clientSocket.RemoteEndPoint.ToString(), msg, msg.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    break;
                }
            }
        }
    }
}