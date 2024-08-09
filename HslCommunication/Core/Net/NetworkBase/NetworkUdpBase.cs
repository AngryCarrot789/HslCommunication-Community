using System.Net;
using System.Net.Sockets;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Types;

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 基础的Udp的通信对象
/// </summary>
public class NetworkUdpBase : NetworkBase {
    /// <summary>
    /// 实例化一个默认的方法
    /// </summary>
    public NetworkUdpBase() {
        this.hybirdLock = new SimpleHybirdLock();
        this.ReceiveTimeout = 5000;
    }

    /// <summary>
    /// Ip地址
    /// </summary>
    public virtual string IpAddress { get; set; }

    /// <summary>
    /// 端口号信息
    /// </summary>
    public virtual int Port { get; set; }

    /// <summary>
    /// 接收反馈的超时时间
    /// </summary>
    public int ReceiveTimeout { get; set; }

    /// <summary>
    /// 获取或设置一次接收时的数据长度，默认2KB数据长度，特殊情况的时候需要调整
    /// </summary>
    public int ReceiveCacheLength { get; set; } = 2048;

    /// <summary>
    /// 核心的数据交互读取
    /// </summary>
    /// <param name="value">完整的报文内容</param>
    /// <returns>是否成功的结果对象</returns>
    public virtual OperateResult<byte[]> ReadFromCoreServer(byte[] value) {
        this.hybirdLock.Enter();
        try {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(this.IpAddress), this.Port);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            server.SendTo(value, value.Length, SocketFlags.None, endPoint);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint) sender;

            // 对于不存在的IP地址，加入此行代码后，可以在指定时间内解除阻塞模式限制
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, this.ReceiveTimeout);
            byte[] buffer = new byte[this.ReceiveCacheLength];
            int recv = server.ReceiveFrom(buffer, ref Remote);

            this.hybirdLock.Leave();
            return OperateResult.CreateSuccessResult(buffer.Take(recv).ToArray());
        }
        catch (Exception ex) {
            this.hybirdLock.Leave();
            return new OperateResult<byte[]>(ex.Message);
        }
    }


    private SimpleHybirdLock hybirdLock = null; // 数据锁
}