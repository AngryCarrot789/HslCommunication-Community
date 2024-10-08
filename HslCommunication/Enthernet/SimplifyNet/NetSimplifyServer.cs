﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Net.StateOne;

namespace HslCommunication.Enthernet.SimplifyNet;

/// <summary>
/// 异步消息处理服务器，主要用来实现接收客户端信息并进行消息反馈的操作，适用于客户端进行远程的调用，要求服务器反馈数据。
/// </summary>
/// <remarks>
/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/7697782.html">http://www.cnblogs.com/dathlin/p/7697782.html</a>
/// </remarks>
/// <example>
/// 此处贴上了Demo项目的服务器配置的示例代码
/// <code lang="cs" source="TestProject\SimplifyNetTest\FormServer.cs" region="Simplify Net" title="NetSimplifyServer示例" />
/// </example>
public class NetSimplifyServer : NetworkAuthenticationServerBase {
    /// <summary>
    /// 实例化一个服务器消息请求的信息
    /// </summary>
    public NetSimplifyServer() {
    }

    /// <summary>
    /// 接收字符串信息的事件
    /// </summary>
    public event Action<AppSession, NetHandle, string> ReceiveStringEvent;

    /// <summary>
    /// 接收字符串数组信息的事件
    /// </summary>
    public event Action<AppSession, NetHandle, string[]> ReceiveStringArrayEvent;

    /// <summary>
    /// 接收字节信息的事件
    /// </summary>
    public event Action<AppSession, NetHandle, byte[]> ReceivedBytesEvent;


    private void OnReceiveStringEvent(AppSession session, int customer, string str) {
        this.ReceiveStringEvent?.Invoke(session, customer, str);
    }

    private void OnReceivedBytesEvent(AppSession session, int customer, byte[] temp) {
        this.ReceivedBytesEvent?.Invoke(session, customer, temp);
    }

    private void OnReceiveStringArrayEvent(AppSession session, int customer, string[] str) {
        this.ReceiveStringArrayEvent?.Invoke(session, customer, str);
    }

    /// <summary>
    /// 向指定的通信对象发送字符串数据
    /// </summary>
    /// <param name="session">通信对象</param>
    /// <param name="customer">用户的指令头</param>
    /// <param name="str">实际发送的字符串数据</param>
    public void SendMessage(AppSession session, int customer, string str) {
        this.SendBytesAsync(session, HslProtocol.CommandBytes(customer, this.Token, str));
    }

    /// <summary>
    /// 向指定的通信对象发送字符串数组
    /// </summary>
    /// <param name="session">通信对象</param>
    /// <param name="customer">用户的指令头</param>
    /// <param name="str">实际发送的字符串数组</param>
    public void SendMessage(AppSession session, int customer, string[] str) {
        this.SendBytesAsync(session, HslProtocol.CommandBytes(customer, this.Token, str));
    }

    /// <summary>
    /// 向指定的通信对象发送字节数据
    /// </summary>
    /// <param name="session">连接对象</param>
    /// <param name="customer">用户的指令头</param>
    /// <param name="bytes">实际的数据</param>
    public void SendMessage(AppSession session, int customer, byte[] bytes) {
        this.SendBytesAsync(session, HslProtocol.CommandBytes(customer, this.Token, bytes));
    }

    /// <summary>
    /// 关闭网络的操作
    /// </summary>
    protected override void CloseAction() {
        this.ReceivedBytesEvent = null;
        this.ReceiveStringEvent = null;
        base.CloseAction();
    }


    /// <summary>
    /// 当接收到了新的请求的时候执行的操作
    /// </summary>
    /// <param name="socket">异步对象</param>
    /// <param name="endPoint">终结点</param>
    protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint) {
        AppSession session = new AppSession();

        session.WorkSocket = socket;
        try {
            session.IpEndPoint = endPoint;
            session.IpAddress = session.IpEndPoint.Address.ToString();
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), StringResources.Language.GetClientIpaddressFailed, ex);
        }

        this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientOnlineInfo, session.IpEndPoint));
        Interlocked.Increment(ref this.clientCount);
        this.ReBeginReceiveHead(session, false);
    }

    /// <summary>
    /// 处理异常的方法
    /// </summary>
    /// <param name="session">会话</param>
    /// <param name="ex">异常信息</param>
    internal override void SocketReceiveException(AppSession session, Exception ex) {
        session.WorkSocket?.Close();
        Interlocked.Decrement(ref this.clientCount);
        this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientOfflineInfo, session.IpEndPoint));
    }

    /// <summary>
    /// 正常下线
    /// </summary>
    /// <param name="session">会话</param>
    internal override void AppSessionRemoteClose(AppSession session) {
        session.WorkSocket?.Close();
        Interlocked.Decrement(ref this.clientCount);
        this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientOfflineInfo, session.IpEndPoint));
    }

    /// <summary>
    /// 数据处理中心
    /// </summary>
    /// <param name="session">当前的会话</param>
    /// <param name="protocol">协议指令头</param>
    /// <param name="customer">客户端信号</param>
    /// <param name="content">触发的消息内容</param>
    internal override void DataProcessingCenter(AppSession session, int protocol, int customer, byte[] content) {
        //接收数据完成，进行事件通知，优先进行解密操作
        if (protocol == HslProtocol.ProtocolCheckSecends) {
            // 初始化时候的测试消息
            session.HeartTime = DateTime.Now;
            this.SendMessage(session, customer, content);
        }
        else if (protocol == HslProtocol.ProtocolUserBytes) {
            // 字节数据
            this.OnReceivedBytesEvent(session, customer, content);
        }
        else if (protocol == HslProtocol.ProtocolUserString) {
            // 字符串数据
            this.OnReceiveStringEvent(session, customer, Encoding.Unicode.GetString(content));
        }
        else if (protocol == HslProtocol.ProtocolUserStringArray) {
            // 字符串数组
            this.OnReceiveStringArrayEvent(session, customer, HslProtocol.UnPackStringArrayFromByte(content));
        }
        else {
            // 数据异常
            this.AppSessionRemoteClose(session);
        }
    }

    /// <summary>
    /// 当前在线的客户端数量
    /// </summary>
    public int ClientCount => this.clientCount;

    private int clientCount = 0; // 在线客户端的数量

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "NetSimplifyServer";
    }
}