using System.Net.Sockets;

namespace HslCommunication.Core.Net;

/// <summary>
/// 网络中的异步对象
/// </summary>
internal class StateObject : StateOneBase {
    #region Constructor

    /// <summary>
    /// 实例化一个对象
    /// </summary>
    public StateObject() {
    }

    /// <summary>
    /// 实例化一个对象，指定接收或是发送的数据长度
    /// </summary>
    /// <param name="length">数据长度</param>
    public StateObject(int length) {
        this.DataLength = length;
        this.Buffer = new byte[length];
    }

    #endregion

    #region Public Member

    /// <summary>
    /// 唯一的一串信息
    /// </summary>
    public string UniqueId { get; set; }

    /// <summary>
    /// 网络套接字
    /// </summary>
    public Socket WorkSocket { get; set; }

    /// <summary>
    /// 是否关闭了通道
    /// </summary>
    public bool IsClose { get; set; }

    #endregion

    #region Public Method

    /// <summary>
    /// 清空旧的数据
    /// </summary>
    public void Clear() {
        this.IsError = false;
        this.IsClose = false;
        this.AlreadyDealLength = 0;
        this.Buffer = null;
    }

    #endregion
}

#if !NET35

/// <summary>
/// 携带TaskCompletionSource属性的异步对象
/// </summary>
/// <typeparam name="T">类型</typeparam>
internal class StateObjectAsync<T> : StateObject {
    #region Constructor

    /// <summary>
    /// 实例化一个对象
    /// </summary>
    public StateObjectAsync() : base() {
    }

    /// <summary>
    /// 实例化一个对象，指定接收或是发送的数据长度
    /// </summary>
    /// <param name="length">数据长度</param>
    public StateObjectAsync(int length) : base(length) {
    }

    #endregion

    public System.Threading.Tasks.TaskCompletionSource<T> Tcs { get; set; }
}

#endif