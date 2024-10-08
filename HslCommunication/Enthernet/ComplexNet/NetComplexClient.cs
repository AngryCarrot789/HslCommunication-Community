﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Types;

namespace HslCommunication.Enthernet.ComplexNet;

/// <summary>
/// 一个基于异步高性能的客户端网络类，支持主动接收服务器的消息
/// </summary>
/// <remarks>
/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/7697782.html">http://www.cnblogs.com/dathlin/p/7697782.html</a>
/// </remarks>
/// <example>
/// 此处贴上了Demo项目的服务器配置的示例代码
/// <code lang="cs" source="TestProject\HslCommunicationDemo\FormComplexNet.cs" region="NetComplexClient" title="NetComplexClient示例" />
/// </example>
public class NetComplexClient : NetworkXBase {
    /// <summary>
    /// 实例化一个对象
    /// </summary>
    public NetComplexClient() {
        this.session = new AppSession();
        this.ServerTime = DateTime.Now;
        this.EndPointServer = new IPEndPoint(IPAddress.Any, 0);
    }

    private AppSession session; // 客户端的核心连接对象
    private int isConnecting = 0; // 指示客户端是否处于连接服务器中，0代表未连接，1代表连接中
    private bool IsQuie = false; // 指示系统是否准备退出
    private Thread? thread_heart_check = null; // 心跳线程

    /// <summary>
    /// 客户端系统是否启动
    /// </summary>
    public bool IsClientStart { get; set; }

    /// <summary>
    /// 重连接失败的次数
    /// </summary>
    public int ConnectFailedCount { get; private set; }

    /// <summary>
    /// 客户端登录的标识名称，可以为ID号，也可以为登录名
    /// </summary>
    public string ClientAlias { get; set; } = string.Empty;

    /// <summary>
    /// 远程服务器的IP地址和端口
    /// </summary>
    public IPEndPoint EndPointServer { get; set; }

    /// <summary>
    /// 服务器的时间，自动实现和服务器同步
    /// </summary>
    public DateTime ServerTime { get; private set; }

    /// <summary>
    /// 系统与服务器的延时时间，单位毫秒
    /// </summary>
    public int DelayTime { get; private set; }

    /// <summary>
    /// 客户端启动成功的事件，重连成功也将触发此事件
    /// </summary>
    public event Action LoginSuccess;

    /// <summary>
    /// 连接失败时触发的事件
    /// </summary>
    public event Action<int> LoginFailed;

    /// <summary>
    /// 服务器的异常，启动，等等一般消息产生的时候，出发此事件
    /// </summary>
    public event Action<string> MessageAlerts;

    /// <summary>
    /// 在客户端断开后并在重连服务器之前触发，用于清理系统资源
    /// </summary>
    public event Action BeforReConnected;

    /// <summary>
    /// 当接收到文本数据的时候,触发此事件
    /// </summary>
    public event Action<AppSession, NetHandle, string> AcceptString;

    /// <summary>
    /// 当接收到字节数据的时候,触发此事件
    /// </summary>
    public event Action<AppSession, NetHandle, byte[]> AcceptByte;

    /// <summary>
    /// 关闭该客户端引擎
    /// </summary>
    public void ClientClose() {
        this.IsQuie = true;
        if (this.IsClientStart)
            this.SendBytes(this.session, HslProtocol.CommandBytes(HslProtocol.ProtocolClientQuit, 0, this.Token, null));

        this.IsClientStart = false; // 关闭客户端
        this.thread_heart_check = null;

        this.LoginSuccess = null; // 清空所有的事件
        this.LoginFailed = null;
        this.MessageAlerts = null;
        this.AcceptByte = null;
        this.AcceptString = null;
        try {
            this.session.WorkSocket?.Shutdown(SocketShutdown.Both);
            this.session.WorkSocket?.Close();
        }
        catch {
        }

        this.LogNet?.WriteDebug(this.ToString(), "Client Close.");
    }


    /// <summary>
    /// 启动客户端引擎，连接服务器系统
    /// </summary>
    public void ClientStart() {
        // 如果处于连接中就退出
        if (Interlocked.CompareExchange(ref this.isConnecting, 1, 0) != 0)
            return;

        // 启动后台线程连接
        new Thread(new ThreadStart(this.ThreadLogin)) { IsBackground = true }.Start();

        // 启动心跳线程，在第一次Start的时候
        if (this.thread_heart_check == null) {
            this.thread_heart_check = new Thread(new ThreadStart(this.ThreadHeartCheck)) {
                Priority = ThreadPriority.AboveNormal,
                IsBackground = true
            };
            this.thread_heart_check.Start();
        }
    }

    /// <summary>
    /// 连接服务器之前的消息提示，如果是重连的话，就提示10秒等待信息
    /// </summary>
    private void AwaitToConnect() {
        if (this.ConnectFailedCount == 0) {
            this.MessageAlerts?.Invoke(StringResources.Language.ConnectingServer);
        }
        else {
            int count = 10;
            while (count > 0) {
                if (this.IsQuie)
                    return;
                count--;
                this.MessageAlerts?.Invoke(string.Format(StringResources.Language.ConnectFailedAndWait, count));
                Thread.Sleep(1000);
            }

            this.MessageAlerts?.Invoke(string.Format(StringResources.Language.AttemptConnectServer, this.ConnectFailedCount));
        }
    }

    private void ConnectFailed() {
        this.ConnectFailedCount++;
        Interlocked.Exchange(ref this.isConnecting, 0);
        this.LoginFailed?.Invoke(this.ConnectFailedCount);
        this.LogNet?.WriteDebug(this.ToString(), "Connected Failed, Times: " + this.ConnectFailedCount);
    }

    private OperateResult<Socket> ConnectServer() {
        OperateResult<Socket> connectResult = this.CreateSocketAndConnect(this.EndPointServer, 10000);
        if (!connectResult.IsSuccess) {
            return connectResult;
        }

        // 连接成功，发送数据信息
        OperateResult sendResult = this.SendStringAndCheckReceive(connectResult.Content, 1, this.ClientAlias);
        if (!sendResult.IsSuccess) {
            return OperateResult.CreateFailedResult<Socket>(sendResult);
        }

        this.MessageAlerts?.Invoke(StringResources.Language.ConnectServerSuccess);
        return connectResult;
    }

    private void LoginSuccessMethod(Socket socket) {
        this.ConnectFailedCount = 0;
        try {
            this.session.IpEndPoint = (IPEndPoint) socket.RemoteEndPoint;
            this.session.LoginAlias = this.ClientAlias;
            this.session.WorkSocket = socket;
            this.session.HeartTime = DateTime.Now;
            this.IsClientStart = true;
            this.ReBeginReceiveHead(this.session, false);
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), ex);
        }
    }


    private void ThreadLogin() {
        // 连接的消息等待
        this.AwaitToConnect();

        OperateResult<Socket> connectResult = this.ConnectServer();
        if (!connectResult.IsSuccess) {
            this.ConnectFailed();
            // 连接失败，重新连接服务器
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ReconnectServer), null);
            return;
        }

        // 登录成功
        this.LoginSuccessMethod(connectResult.Content);

        // 登录成功
        this.LoginSuccess?.Invoke();
        Interlocked.Exchange(ref this.isConnecting, 0);
        Thread.Sleep(200);
    }


    private void ReconnectServer(object obj = null) {
        // 是否连接服务器中，已经在连接的话，则不再连接
        if (this.isConnecting == 1)
            return;
        // 是否退出了系统，退出则不再重连
        if (this.IsQuie)
            return;
        // 触发连接失败，重连系统前错误
        this.BeforReConnected?.Invoke();
        this.session?.WorkSocket?.Close();
        // 重新启动客户端
        this.ClientStart();
    }

    /// <summary>
    /// 通信出错后的处理
    /// </summary>
    /// <param name="receive">接收的会话</param>
    /// <param name="ex">异常</param>
    internal override void SocketReceiveException(AppSession receive, Exception ex) {
        if (ex.Message.Contains(StringResources.Language.SocketRemoteCloseException)) {
            // 异常掉线
            this.ReconnectServer();
        }
        else {
            // MessageAlerts?.Invoke("数据接收出错：" + ex.Message);
        }

        this.LogNet?.WriteDebug(this.ToString(), "Socket Excepiton Occured.");
    }


    /// <summary>
    /// 服务器端用于数据发送文本的方法
    /// </summary>
    /// <param name="customer">用户自定义的命令头</param>
    /// <param name="str">发送的文本</param>
    public void Send(NetHandle customer, string str) {
        if (this.IsClientStart) {
            this.SendBytes(this.session, HslProtocol.CommandBytes(customer, this.Token, str));
        }
    }

    /// <summary>
    /// 服务器端用于发送字节的方法
    /// </summary>
    /// <param name="customer">用户自定义的命令头</param>
    /// <param name="bytes">实际发送的数据</param>
    public void Send(NetHandle customer, byte[] bytes) {
        if (this.IsClientStart) {
            this.SendBytes(this.session, HslProtocol.CommandBytes(customer, this.Token, bytes));
        }
    }

    private void SendBytes(AppSession stateone, byte[] content) {
        this.SendBytesAsync(stateone, content);
    }

    /// <summary>
    /// 客户端的数据处理中心
    /// </summary>
    /// <param name="session">会话</param>
    /// <param name="protocol">消息暗号</param>
    /// <param name="customer">用户消息</param>
    /// <param name="content">数据内容</param>
    internal override void DataProcessingCenter(AppSession session, int protocol, int customer, byte[] content) {
        if (protocol == HslProtocol.ProtocolCheckSecends) {
            DateTime dt = new DateTime(BitConverter.ToInt64(content, 0));
            this.ServerTime = new DateTime(BitConverter.ToInt64(content, 8));
            this.DelayTime = (int) (DateTime.Now - dt).TotalMilliseconds;
            this.session.HeartTime = DateTime.Now;
            // MessageAlerts?.Invoke("心跳时间：" + DateTime.Now.ToString());
        }
        else if (protocol == HslProtocol.ProtocolClientQuit) {
            // 申请了退出
        }
        else if (protocol == HslProtocol.ProtocolUserBytes) {
            // 接收到字节数据
            this.AcceptByte?.Invoke(this.session, customer, content);
        }
        else if (protocol == HslProtocol.ProtocolUserString) {
            // 接收到文本数据
            string str = Encoding.Unicode.GetString(content);
            this.AcceptString?.Invoke(this.session, customer, str);
        }
    }

    /// <summary>
    /// 心跳线程的方法
    /// </summary>
    private void ThreadHeartCheck() {
        Thread.Sleep(2000);
        while (true) {
            Thread.Sleep(1000);
            if (!this.IsQuie) {
                byte[] send = new byte[16];
                BitConverter.GetBytes(DateTime.Now.Ticks).CopyTo(send, 0);
                this.SendBytes(this.session, HslProtocol.CommandBytes(HslProtocol.ProtocolCheckSecends, 0, this.Token, send));
                double timeSpan = (DateTime.Now - this.session.HeartTime).TotalSeconds;
                if (timeSpan > 1 * 8) //8次没有收到失去联系
                {
                    if (this.isConnecting == 0) {
                        this.LogNet?.WriteDebug(this.ToString(), $"Heart Check Failed int {timeSpan} Seconds.");
                        this.ReconnectServer();
                    }

                    if (!this.IsQuie)
                        Thread.Sleep(1000);
                }
            }
            else {
                break;
            }
        }
    }

    /// <summary>
    /// 返回对象的字符串表示形式
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "NetComplexClient";
    }
}