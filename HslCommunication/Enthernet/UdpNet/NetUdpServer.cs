﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Net.StateOne;

namespace HslCommunication.Enthernet.UdpNet;

/// <summary>
/// Udp网络的服务器端类
/// </summary>
public class NetUdpServer : NetworkServerBase {
    /// <summary>
    /// 获取或设置一次接收时的数据长度，默认2KB数据长度
    /// </summary>
    public int ReceiveCacheLength { get; set; } = 2048;


    /// <summary>
    /// 根据指定的端口启动Upd侦听
    /// </summary>
    /// <param name="port">端口号信息</param>
    public override void ServerStart(int port) {
        if (!this.IsStarted) {
            this.CoreSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //绑定网络地址
            this.CoreSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            this.RefreshReceive();
            this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.NetEngineStart);
            this.IsStarted = true;
        }
    }

    /// <summary>
    /// 关闭引擎的操作
    /// </summary>
    protected override void CloseAction() {
        this.AcceptString = null;
        this.AcceptByte = null;
        base.CloseAction();
    }

    /// <summary>
    /// 重新开始接收数据
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    private void RefreshReceive() {
        AppSession session = new AppSession();
        session.WorkSocket = this.CoreSocket;
        session.UdpEndPoint = new IPEndPoint(IPAddress.Any, 0);
        session.BytesContent = new byte[this.ReceiveCacheLength];
        // WorkSocket.BeginReceiveFrom(state.BytesHead, 0, 8, SocketFlags.None, ref state.UdpEndPoint, new AsyncCallback(ReceiveAsyncCallback), state);
        this.CoreSocket.BeginReceiveFrom(session.BytesContent, 0, this.ReceiveCacheLength, SocketFlags.None, ref session.UdpEndPoint, new AsyncCallback(this.AsyncCallback), session);
    }

    private void AsyncCallback(IAsyncResult ar) {
        if (ar.AsyncState is AppSession session) {
            try {
                int received = session.WorkSocket.EndReceiveFrom(ar, ref session.UdpEndPoint);
                // 释放连接关联
                // session.WorkSocket = null;
                // 马上开始重新接收，提供性能保障
                this.RefreshReceive();
                // 处理数据
                if (received >= HslProtocol.HeadByteLength) {
                    // 检测令牌
                    if (this.CheckRemoteToken(session.BytesContent)) {
                        session.IpEndPoint = (IPEndPoint) session.UdpEndPoint;
                        int contentLength = BitConverter.ToInt32(session.BytesContent, HslProtocol.HeadByteLength - 4);
                        if (contentLength == received - HslProtocol.HeadByteLength) {
                            byte[] head = new byte[HslProtocol.HeadByteLength];
                            byte[] content = new byte[contentLength];

                            Array.Copy(session.BytesContent, 0, head, 0, HslProtocol.HeadByteLength);
                            if (contentLength > 0) {
                                Array.Copy(session.BytesContent, 32, content, 0, contentLength);
                            }

                            // 解析内容
                            content = HslProtocol.CommandAnalysis(head, content);

                            int protocol = BitConverter.ToInt32(head, 0);
                            int customer = BitConverter.ToInt32(head, 4);
                            // 丢给数据中心处理
                            this.DataProcessingCenter(session, protocol, customer, content);
                        }
                        else {
                            // 否则记录到日志
                            this.LogNet?.WriteWarn(this.ToString(), $"Should Rece：{(BitConverter.ToInt32(session.BytesContent, 4) + 8)} Actual：{received}");
                        }
                    }
                    else {
                        this.LogNet?.WriteWarn(this.ToString(), StringResources.Language.TokenCheckFailed);
                    }
                }
                else {
                    this.LogNet?.WriteWarn(this.ToString(), $"Receive error, Actual：{received}");
                }
            }
            catch (ObjectDisposedException) {
                //主程序退出的时候触发
            }
            catch (Exception ex) {
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.SocketEndReceiveException, ex);
                //重新接收，此处已经排除掉了对象释放的异常
                this.RefreshReceive();
            }
            finally {
                //state = null;
            }
        }
    }

    /***********************************************************************************************************
     *
     *    无法使用如下的字节头接收来确认网络传输，总是报错为最小
     *
     ***********************************************************************************************************/

    //private void ReceiveAsyncCallback(IAsyncResult ar)
    //{
    //    if (ar.AsyncState is AsyncStateOne state)
    //    {
    //        try
    //        {
    //            state.AlreadyReceivedHead += state.WorkSocket.EndReceiveFrom(ar, ref state.UdpEndPoint);
    //            if (state.AlreadyReceivedHead < state.HeadLength)
    //            {
    //                //接续接收头数据
    //                WorkSocket.BeginReceiveFrom(state.BytesHead, state.AlreadyReceivedHead, state.HeadLength - state.AlreadyReceivedHead, SocketFlags.None,
    //                    ref state.UdpEndPoint, new AsyncCallback(ReceiveAsyncCallback), state);
    //            }
    //            else
    //            {
    //                //开始接收内容
    //                int ReceiveLenght = BitConverter.ToInt32(state.BytesHead, 4);
    //                if (ReceiveLenght > 0)
    //                {
    //                    state.BytesContent = new byte[ReceiveLenght];
    //                    WorkSocket.BeginReceiveFrom(state.BytesContent, state.AlreadyReceivedContent, state.BytesContent.Length - state.AlreadyReceivedContent, 
    //                        SocketFlags.None, ref state.UdpEndPoint, new AsyncCallback(ContentReceiveAsyncCallback), state);
    //                }
    //                else
    //                {
    //                    //没有内容了
    //                    ThreadDealWithReveice(state, BitConverter.ToInt32(state.BytesHead, 0), state.BytesContent);
    //                    state = null;
    //                    RefreshReceive();
    //                }
    //            }
    //        }
    //        catch(Exception ex)
    //        {
    //            LogHelper.SaveError(StringResources.Language.异步数据结束挂起发送出错, ex);
    //        }


    //    }
    //}

    //private void ContentReceiveAsyncCallback(IAsyncResult ar)
    //{
    //    if (ar.AsyncState is AsyncStateOne state)
    //    {
    //        try
    //        {
    //            state.AlreadyReceivedContent += state.WorkSocket.EndReceiveFrom(ar, ref state.UdpEndPoint);
    //            if (state.AlreadyReceivedContent < state.BytesContent.Length)
    //            {
    //                //还需要继续接收
    //                WorkSocket.BeginReceiveFrom(state.BytesContent, state.AlreadyReceivedContent, state.BytesContent.Length - state.AlreadyReceivedContent,
    //                        SocketFlags.None, ref state.UdpEndPoint, new AsyncCallback(ContentReceiveAsyncCallback), state);
    //            }
    //            else
    //            {
    //                //接收完成了
    //                ThreadDealWithReveice(state, BitConverter.ToInt32(state.BytesHead, 0), new byte[0]);
    //                state = null;
    //                RefreshReceive();
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            LogHelper.SaveError(StringResources.Language.异步数据结束挂起发送出错, ex);
    //        }


    //    }
    //}

    /// <summary>
    /// 数据处理中心
    /// </summary>
    /// <param name="receive"></param>
    /// <param name="protocol"></param>
    /// <param name="customer"></param>
    /// <param name="content"></param>
    internal override void DataProcessingCenter(AppSession receive, int protocol, int customer, byte[] content) {
        if (protocol == HslProtocol.ProtocolUserBytes) {
            this.AcceptByte?.Invoke(receive, customer, content);
        }
        else if (protocol == HslProtocol.ProtocolUserString) {
            // 接收到文本数据
            string str = Encoding.Unicode.GetString(content);
            this.AcceptString?.Invoke(receive, customer, str);
        }
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
    /// 向指定的通信对象发送字节数据
    /// </summary>
    /// <param name="session">连接对象</param>
    /// <param name="customer">用户的指令头</param>
    /// <param name="bytes">实际的数据</param>
    public void SendMessage(AppSession session, int customer, byte[] bytes) {
        this.SendBytesAsync(session, HslProtocol.CommandBytes(customer, this.Token, bytes));
    }

    private new void SendBytesAsync(AppSession session, byte[] data) {
        try {
            session.WorkSocket.SendTo(data, data.Length, SocketFlags.None, session.UdpEndPoint);
        }
        catch (Exception ex) {
            this.LogNet?.WriteException("SendMessage", ex);
        }
    }

    /// <summary>
    /// 当接收到文本数据的时候,触发此事件
    /// </summary>
    public event Action<AppSession, NetHandle, string> AcceptString;


    /// <summary>
    /// 当接收到字节数据的时候,触发此事件
    /// </summary>
    public event Action<AppSession, NetHandle, byte[]> AcceptByte;

    /// <summary>
    /// 获取本对象的字符串表示形式
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "NetUdpServer";
    }
}