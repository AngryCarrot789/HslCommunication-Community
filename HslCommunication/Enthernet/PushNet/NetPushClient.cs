using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Types;

namespace HslCommunication.Enthernet.PushNet;

/// <summary>
/// 发布订阅类的客户端，使用指定的关键订阅相关的数据推送信息
/// </summary>
/// <remarks>
/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/8992315.html">http://www.cnblogs.com/dathlin/p/8992315.html</a>
/// </remarks>
/// <example>
/// 此处贴上了Demo项目的服务器配置的示例代码
/// <code lang="cs" source="TestProject\HslCommunicationDemo\FormPushNet.cs" region="FormPushNet" title="NetPushClient示例" />
/// </example>
public class NetPushClient : NetworkXBase {
    /// <summary>
    /// 实例化一个发布订阅类的客户端，需要指定ip地址，端口，及订阅关键字
    /// </summary>
    /// <param name="ipAddress">服务器的IP地址</param>
    /// <param name="port">服务器的端口号</param>
    /// <param name="key">订阅关键字</param>
    public NetPushClient(string ipAddress, int port, string key) {
        this.endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        this.keyWord = key;

        if (string.IsNullOrEmpty(key)) {
            throw new Exception(StringResources.Language.KeyIsNotAllowedNull);
        }
    }

    internal override void DataProcessingCenter(AppSession session, int protocol, int customer, byte[] content) {
        if (protocol == HslProtocol.ProtocolUserString) {
            this.action?.Invoke(this, Encoding.Unicode.GetString(content));
            this.OnReceived?.Invoke(this, Encoding.Unicode.GetString(content));
        }
    }

    internal override void SocketReceiveException(AppSession session, Exception ex) {
        // 发生异常的时候需要进行重新连接
        while (true) {
            Console.WriteLine(ex);
            Console.WriteLine(StringResources.Language.ReConnectServerAfterTenSeconds);
            Thread.Sleep(this.reconnectTime);

            if (this.CreatePush().IsSuccess) {
                Console.WriteLine(StringResources.Language.ReConnectServerSuccess);
                break;
            }
        }
    }

    /// <summary>
    /// 创建数据推送服务
    /// </summary>
    /// <param name="pushCallBack">触发数据推送的委托</param>
    /// <returns>是否创建成功</returns>
    public OperateResult CreatePush(Action<NetPushClient, string> pushCallBack) {
        this.action = pushCallBack;
        return this.CreatePush();
    }

    /// <summary>
    /// 创建数据推送服务，使用事件绑定的机制实现
    /// </summary>
    /// <returns>是否创建成功</returns>
    public OperateResult CreatePush() {
        this.CoreSocket?.Close();

        // 连接服务器
        OperateResult<Socket> connect = this.CreateSocketAndConnect(this.endPoint, 5000);
        if (!connect.IsSuccess)
            return connect;

        // 发送订阅的关键字
        OperateResult send = this.SendStringAndCheckReceive(connect.Content, 0, this.keyWord);
        if (!send.IsSuccess)
            return send;

        // 确认服务器的反馈
        OperateResult<int, string> receive = this.ReceiveStringContentFromSocket(connect.Content);
        if (!receive.IsSuccess)
            return receive;

        // 订阅不存在
        if (receive.Content1 != 0) {
            connect.Content?.Close();
            return new OperateResult(receive.Content2);
        }

        // 异步接收
        AppSession appSession = new AppSession();
        this.CoreSocket = connect.Content;
        appSession.WorkSocket = connect.Content;
        this.ReBeginReceiveHead(appSession, false);

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 关闭消息推送的界面
    /// </summary>
    public void ClosePush() {
        this.action = null;
        if (this.CoreSocket != null && this.CoreSocket.Connected)
            this.CoreSocket?.Send(BitConverter.GetBytes(100));
        Thread.Sleep(20);
        this.CoreSocket?.Close();
    }

    /// <summary>
    /// 本客户端的关键字
    /// </summary>
    public string KeyWord => this.keyWord;

    /// <summary>
    /// 获取或设置重连服务器的间隔时间
    /// </summary>
    public int ReConnectTime { set => this.reconnectTime = value; get => this.reconnectTime; }

    /// <summary>
    /// 当接收到数据的事件信息，接收到数据的时候触发。
    /// </summary>
    public event Action<NetPushClient, string> OnReceived;

    private IPEndPoint endPoint; // 服务器的地址及端口信息
    private string keyWord = string.Empty; // 缓存的订阅关键字
    private Action<NetPushClient, string> action; // 服务器推送后的回调方法
    private int reconnectTime = 10000; // 重连服务器的时间

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串</returns>
    public override string ToString() {
        return $"NetPushClient[{this.endPoint}]";
    }
}