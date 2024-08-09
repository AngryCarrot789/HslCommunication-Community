using System.Net.Sockets;

namespace HslCommunication.Core.Net.StateOne;

/// <summary>
/// 异形客户端的异步对象
/// </summary>
public class AlienSession {
    /// <summary>
    /// 实例化一个默认的参数
    /// </summary>
    public AlienSession() {
        this.IsStatusOk = true;
    }


    /// <summary>
    /// 网络套接字
    /// </summary>
    public Socket Socket { get; set; }

    /// <summary>
    /// 唯一的标识
    /// </summary>
    public string DTU { get; set; }

    /// <summary>
    /// 指示当前的网络状态
    /// </summary>
    public bool IsStatusOk { get; set; }
}