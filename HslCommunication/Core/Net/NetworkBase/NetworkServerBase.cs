﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Types;

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 服务器程序的基础类
/// </summary>
public class NetworkServerBase : NetworkXBase {
    /// <summary>
    /// 实例化一个默认的对象
    /// </summary>
    public NetworkServerBase() {
        this.IsStarted = false;
        this.Port = 0;
    }

    /// <summary>
    /// 服务器引擎是否启动
    /// </summary>
    public bool IsStarted { get; protected set; }

    /// <summary>
    /// 服务器的端口号
    /// </summary>
    /// <remarks>需要在服务器启动之前设置为有效</remarks>
    public int Port { get; set; }

    /// <summary>
    /// 异步传入的连接申请请求
    /// </summary>
    /// <param name="iar">异步对象</param>
    protected void AsyncAcceptCallback(IAsyncResult iar) {
        //还原传入的原始套接字
        if (iar.AsyncState is Socket server_socket) {
            Socket client = null;
            try {
                // 在原始套接字上调用EndAccept方法，返回新的套接字
                client = server_socket.EndAccept(iar);
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.ThreadPoolLogin), client);
            }
            catch (ObjectDisposedException) {
                // 服务器关闭时候触发的异常，不进行记录
                return;
            }
            catch (Exception ex) {
                // 有可能刚连接上就断开了，那就不管
                client?.Close();
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketAcceptCallbackException, ex);
            }

            // 如果失败，尝试启动三次
            int i = 0;
            while (i < 3) {
                try {
                    server_socket.BeginAccept(new AsyncCallback(this.AsyncAcceptCallback), server_socket);
                    break;
                }
                catch (Exception ex) {
                    System.Threading.Thread.Sleep(1000);
                    this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketReAcceptCallbackException, ex);
                    i++;
                }
            }

            if (i >= 3) {
                this.LogNet?.WriteError(this.ToString(), StringResources.Language.SocketReAcceptCallbackException);
                // 抛出异常，终止应用程序
                throw new Exception(StringResources.Language.SocketReAcceptCallbackException);
            }
        }
    }

    /// <summary>
    /// 用于登录的回调方法
    /// </summary>
    /// <param name="obj">socket对象</param>
    private void ThreadPoolLogin(object obj) {
        if (obj is Socket socket) {
            IPEndPoint endPoint = (IPEndPoint) socket.RemoteEndPoint;
            OperateResult check = this.SocketAcceptExtraCheck(socket, endPoint);
            if (!check.IsSuccess) {
                this.LogNet?.WriteDebug(this.ToString(), check.Message);
                socket?.Close();
            }
            else {
                this.ThreadPoolLogin(socket, endPoint);
            }
        }
    }

    /// <summary>
    /// 用于登录的回调方法
    /// </summary>
    /// <param name="socket">socket对象</param>
    /// <param name="endPoint">远程的终结点</param>
    protected virtual void ThreadPoolLogin(Socket socket, IPEndPoint endPoint) {
        socket?.Close();
    }

    /// <summary>
    /// 当客户端的socket登录的时候额外检查的信息
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="endPoint">终结点</param>
    /// <returns>验证的结果</returns>
    protected virtual OperateResult SocketAcceptExtraCheck(Socket socket, IPEndPoint endPoint) {
        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 服务器启动时额外的初始化信息
    /// </summary>
    /// <remarks>需要在派生类中重写</remarks>
    protected virtual void StartInitialization() {
    }

    /// <summary>
    /// 启动服务器的引擎
    /// </summary>
    /// <param name="port">指定一个端口号</param>
    public virtual void ServerStart(int port) {
        if (!this.IsStarted) {
            this.StartInitialization();

            this.CoreSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.CoreSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            this.CoreSocket.Listen(500); //单次允许最后请求500个，足够小型系统应用了
            this.CoreSocket.BeginAccept(new AsyncCallback(this.AsyncAcceptCallback), this.CoreSocket);
            this.IsStarted = true;
            this.Port = port;

            this.LogNet?.WriteNewLine();
            this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.NetEngineStart);
        }
    }


    /// <summary>
    /// 使用已经配置好的端口启动服务器的引擎
    /// </summary>
    public void ServerStart() {
        this.ServerStart(this.Port);
    }

    /// <summary>
    /// 服务器关闭的时候需要做的事情
    /// </summary>
    protected virtual void CloseAction() {
    }

    /// <summary>
    /// 关闭服务器的引擎
    /// </summary>
    public virtual void ServerClose() {
        if (this.IsStarted) {
            this.CloseAction();
            this.CoreSocket?.Close();
            this.IsStarted = false;
            this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.NetEngineClose);
        }
    }

    /**************************************************************************************************
     *
     *    此处实现了连接Hsl异形客户端的协议，特殊的协议实现定制请联系作者
     *    QQ群：592132877
     *
     *************************************************************************************************/


    /// <summary>
    /// 创建一个指定的异形客户端连接，使用Hsl协议来发送注册包
    /// </summary>
    /// <param name="ipAddress">Ip地址</param>
    /// <param name="port">端口号</param>
    /// <param name="dtuId">设备唯一ID号，最长11</param>
    /// <returns>是否成功连接</returns>
    public OperateResult ConnectHslAlientClient(string ipAddress, int port, string dtuId) {
        if (dtuId.Length > 11)
            dtuId = dtuId.Substring(11);
        byte[] sendBytes = new byte[28];
        sendBytes[0] = 0x48;
        sendBytes[1] = 0x73;
        sendBytes[2] = 0x6E;
        sendBytes[4] = 0x17;

        Encoding.ASCII.GetBytes(dtuId).CopyTo(sendBytes, 5);

        // 创建连接
        OperateResult<Socket> connect = this.CreateSocketAndConnect(ipAddress, port, 10000);
        if (!connect.IsSuccess)
            return connect;

        // 发送数据
        OperateResult send = this.Send(connect.Content, sendBytes);
        if (!send.IsSuccess)
            return send;

        // 接收数据
        OperateResult<byte[]> receive = this.ReceiveByMessage(connect.Content, 10000, new AlienMessage());
        if (!receive.IsSuccess)
            return receive;

        switch (receive.Content[5]) {
            case 0x01: {
                connect.Content?.Close();
                return new OperateResult(StringResources.Language.DeviceCurrentIsLoginRepeat);
            }
            case 0x02: {
                connect.Content?.Close();
                return new OperateResult(StringResources.Language.DeviceCurrentIsLoginForbidden);
            }
            case 0x03: {
                connect.Content?.Close();
                return new OperateResult(StringResources.Language.PasswordCheckFailed);
            }
        }

        this.ThreadPoolLogin(connect.Content);
        return OperateResult.CreateSuccessResult();
    }
}