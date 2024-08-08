using HslCommunication.Core;
using HslCommunication.Core.Net;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HslCommunication.Enthernet;

/// <summary>
/// 高性能的异步网络服务器类，适合搭建局域网聊天程序，消息推送程序
/// </summary>
/// <remarks>
/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/8097897.html">http://www.cnblogs.com/dathlin/p/8097897.html</a>
/// </remarks>
/// <example>
/// 此处贴上了Demo项目的服务器配置的示例代码
/// <code lang="cs" source="TestProject\ComplexNetServer\FormServer.cs" region="NetComplexServer" title="NetComplexServer示例" />
/// </example>
public class NetComplexServer : NetworkServerBase {
    #region Constructor

    /// <summary>
    /// 实例化一个网络服务器类对象
    /// </summary>
    public NetComplexServer() {
        this.appSessions = new List<AppSession>();
        this.lockSessions = new SimpleHybirdLock();
    }

    #endregion

    #region Private Member

    private int connectMaxClient = 1000; // 允许同时登录的最大客户端数量
    private List<AppSession> appSessions = null; // 所有客户端连接的对象信息   
    private SimpleHybirdLock lockSessions = null; // 对象列表操作的锁

    #endregion

    #region Public Properties

    /// <summary>
    /// 所支持的同时在线客户端的最大数量，商用限制1000个，最小10个
    /// </summary>
    public int ConnectMax {
        get { return this.connectMaxClient; }
        set {
            if (value >= 10 && value < 1001) {
                this.connectMaxClient = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置服务器是否记录客户端上下线信息
    /// </summary>
    public bool IsSaveLogClientLineChange { get; set; } = true;

    /// <summary>
    /// 所有在线客户端的数量
    /// </summary>
    public int ClientCount => this.appSessions.Count;

    #endregion

    #region NetworkServerBase Override

    /// <summary>
    /// 初始化操作
    /// </summary>
    protected override void StartInitialization() {
        this.Thread_heart_check = new Thread(new ThreadStart(this.ThreadHeartCheck)) {
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        this.Thread_heart_check.Start();
        base.StartInitialization();
    }

    /// <summary>
    /// 关闭网络时的操作
    /// </summary>
    protected override void CloseAction() {
        this.Thread_heart_check?.Abort();
        this.ClientOffline = null;
        this.ClientOnline = null;
        this.AcceptString = null;
        this.AcceptByte = null;

        //关闭所有的网络
        this.appSessions.ForEach(m => m.WorkSocket?.Close());
        base.CloseAction();
    }


    /// <summary>
    /// 异常下线
    /// </summary>
    /// <param name="session">会话信息</param>
    /// <param name="ex">异常</param>
    internal override void SocketReceiveException(AppSession session, Exception ex) {
        if (ex.Message.Contains(StringResources.Language.SocketRemoteCloseException)) {
            //异常掉线
            this.TcpStateDownLine(session, false);
        }
    }


    /// <summary>
    /// 正常下线
    /// </summary>
    /// <param name="session">会话信息</param>
    internal override void AppSessionRemoteClose(AppSession session) {
        this.TcpStateDownLine(session, true);
    }

    #endregion

    #region Client Online Offline

    private void TcpStateUpLine(AppSession state) {
        this.lockSessions.Enter();
        this.appSessions.Add(state);
        this.lockSessions.Leave();

        // 提示上线
        this.ClientOnline?.Invoke(state);

        this.AllClientsStatusChange?.Invoke(this.ClientCount);
        // 是否保存上线信息
        if (this.IsSaveLogClientLineChange) {
            this.LogNet?.WriteInfo(this.ToString(), $"[{state.IpEndPoint}] Name:{state?.LoginAlias} {StringResources.Language.NetClientOnline}");
        }
    }

    private void TcpStateClose(AppSession state) {
        state?.WorkSocket?.Close();
    }

    private void TcpStateDownLine(AppSession state, bool is_regular, bool logSave = true) {
        this.lockSessions.Enter();
        bool success = this.appSessions.Remove(state);
        this.lockSessions.Leave();

        if (!success)
            return;
        // 关闭连接
        this.TcpStateClose(state);
        // 判断是否正常下线
        string str = is_regular ? StringResources.Language.NetClientOffline : StringResources.Language.NetClientBreak;
        this.ClientOffline?.Invoke(state, str);
        this.AllClientsStatusChange?.Invoke(this.ClientCount);
        // 是否保存上线信息
        if (this.IsSaveLogClientLineChange && logSave) {
            this.LogNet?.WriteInfo(this.ToString(), $"[{state.IpEndPoint}] Name:{state?.LoginAlias} {str}");
        }
    }

    #endregion

    #region Event Handle

    /// <summary>
    /// 客户端的上下限状态变更时触发，仅作为在线客户端识别
    /// </summary>
    public event Action<int> AllClientsStatusChange;

    /// <summary>
    /// 当客户端上线的时候，触发此事件
    /// </summary>
    public event Action<AppSession> ClientOnline;

    /// <summary>
    /// 当客户端下线的时候，触发此事件
    /// </summary>
    public event Action<AppSession, string> ClientOffline;

    /// <summary>
    /// 当接收到文本数据的时候,触发此事件
    /// </summary>
    public event Action<AppSession, NetHandle, string> AcceptString;

    /// <summary>
    /// 当接收到字节数据的时候,触发此事件
    /// </summary>
    public event Action<AppSession, NetHandle, byte[]> AcceptByte;

    #endregion

    #region Login Server

    /// <summary>
    /// 当接收到了新的请求的时候执行的操作
    /// </summary>
    /// <param name="socket">异步对象</param>
    /// <param name="endPoint">终结点</param>
    protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint) {
        // 判断连接数是否超出规定
        if (this.appSessions.Count > this.ConnectMax) {
            socket?.Close();
            this.LogNet?.WriteWarn(this.ToString(), StringResources.Language.NetClientFull);
            return;
        }

        // 接收用户别名并验证令牌
        OperateResult result = new OperateResult();
        OperateResult<int, string> readResult = this.ReceiveStringContentFromSocket(socket);
        if (!readResult.IsSuccess)
            return;

        // 登录成功
        AppSession session = new AppSession() {
            WorkSocket = socket,
            LoginAlias = readResult.Content2,
        };


        try {
            session.IpEndPoint = (IPEndPoint) socket.RemoteEndPoint;
            session.IpAddress = ((IPEndPoint) socket.RemoteEndPoint).Address.ToString();
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), StringResources.Language.GetClientIpaddressFailed, ex);
        }

        if (readResult.Content1 == 1) {
            // 电脑端客户端
            session.ClientType = "Windows";
        }
        else if (readResult.Content1 == 2) {
            // Android 客户端
            session.ClientType = "Android";
        }


        try {
            session.WorkSocket.BeginReceive(session.BytesHead, session.AlreadyReceivedHead,
                session.BytesHead.Length - session.AlreadyReceivedHead, SocketFlags.None,
                new AsyncCallback(this.HeadBytesReceiveCallback), session);
            this.TcpStateUpLine(session);
            Thread.Sleep(100); // 留下一些时间进行反应
        }
        catch (Exception ex) {
            // 登录前已经出错
            this.TcpStateClose(session);
            this.LogNet?.WriteException(this.ToString(), StringResources.Language.NetClientLoginFailed, ex);
        }
    }

    #endregion

    #region SendAsync Support

    /// <summary>
    /// 服务器端用于数据发送文本的方法
    /// </summary>
    /// <param name="session">数据发送对象</param>
    /// <param name="customer">用户自定义的数据对象，如不需要，赋值为0</param>
    /// <param name="str">发送的文本</param>
    public void Send(AppSession session, NetHandle customer, string str) {
        this.SendBytes(session, HslProtocol.CommandBytes(customer, this.Token, str));
    }

    /// <summary>
    /// 服务器端用于发送字节的方法
    /// </summary>
    /// <param name="session">数据发送对象</param>
    /// <param name="customer">用户自定义的数据对象，如不需要，赋值为0</param>
    /// <param name="bytes">实际发送的数据</param>
    public void Send(AppSession session, NetHandle customer, byte[] bytes) {
        this.SendBytes(session, HslProtocol.CommandBytes(customer, this.Token, bytes));
    }

    private void SendBytes(AppSession session, byte[] content) {
        this.SendBytesAsync(session, content);
    }


    /// <summary>
    /// 服务端用于发送所有数据到所有的客户端
    /// </summary>
    /// <param name="customer">用户自定义的命令头</param>
    /// <param name="str">需要传送的实际的数据</param>
    public void SendAllClients(NetHandle customer, string str) {
        for (int i = 0; i < this.appSessions.Count; i++) {
            this.Send(this.appSessions[i], customer, str);
        }
    }

    /// <summary>
    /// 服务端用于发送所有数据到所有的客户端
    /// </summary>
    /// <param name="customer">用户自定义的命令头</param>
    /// <param name="data">需要群发客户端的字节数据</param>
    public void SendAllClients(NetHandle customer, byte[] data) {
        for (int i = 0; i < this.appSessions.Count; i++) {
            this.Send(this.appSessions[i], customer, data);
        }
    }

    /// <summary>
    /// 根据客户端设置的别名进行发送消息
    /// </summary>
    /// <param name="Alias">客户端上线的别名</param>
    /// <param name="customer">用户自定义的命令头</param>
    /// <param name="str">需要传送的实际的数据</param>
    public void SendClientByAlias(string Alias, NetHandle customer, string str) {
        for (int i = 0; i < this.appSessions.Count; i++) {
            if (this.appSessions[i].LoginAlias == Alias) {
                this.Send(this.appSessions[i], customer, str);
            }
        }
    }


    /// <summary>
    /// 根据客户端设置的别名进行发送消息
    /// </summary>
    /// <param name="Alias">客户端上线的别名</param>
    /// <param name="customer">用户自定义的命令头</param>
    /// <param name="data">需要传送的实际的数据</param>
    public void SendClientByAlias(string Alias, NetHandle customer, byte[] data) {
        for (int i = 0; i < this.appSessions.Count; i++) {
            if (this.appSessions[i].LoginAlias == Alias) {
                this.Send(this.appSessions[i], customer, data);
            }
        }
    }

    #endregion

    #region DataProcessingCenter

    /// <summary>
    /// 数据处理中心
    /// </summary>
    /// <param name="session">会话对象</param>
    /// <param name="protocol">消息的代码</param>
    /// <param name="customer">用户消息</param>
    /// <param name="content">数据内容</param>
    internal override void DataProcessingCenter(AppSession session, int protocol, int customer, byte[] content) {
        if (protocol == HslProtocol.ProtocolCheckSecends) {
            BitConverter.GetBytes(DateTime.Now.Ticks).CopyTo(content, 8);
            this.SendBytes(session, HslProtocol.CommandBytes(HslProtocol.ProtocolCheckSecends, customer, this.Token, content));
            session.HeartTime = DateTime.Now;
        }
        else if (protocol == HslProtocol.ProtocolClientQuit) {
            this.TcpStateDownLine(session, true);
        }
        else if (protocol == HslProtocol.ProtocolUserBytes) {
            //接收到字节数据
            this.AcceptByte?.Invoke(session, customer, content);
        }
        else if (protocol == HslProtocol.ProtocolUserString) {
            //接收到文本数据
            string str = Encoding.Unicode.GetString(content);
            this.AcceptString?.Invoke(session, customer, str);
        }
        else {
            // 其他一概不处理
        }
    }

    #endregion

    #region Heart Check

    private Thread Thread_heart_check { get; set; } = null;

    private void ThreadHeartCheck() {
        while (true) {
            Thread.Sleep(2000);

            try {
                for (int i = this.appSessions.Count - 1; i >= 0; i--) {
                    if (this.appSessions[i] == null) {
                        this.appSessions.RemoveAt(i);
                        continue;
                    }

                    if ((DateTime.Now - this.appSessions[i].HeartTime).TotalSeconds > 1 * 8) //8次没有收到失去联系
                    {
                        this.LogNet?.WriteWarn(this.ToString(), StringResources.Language.NetHeartCheckTimeout + this.appSessions[i].IpAddress.ToString());
                        this.TcpStateDownLine(this.appSessions[i], false, false);
                        continue;
                    }
                }
            }
            catch (Exception ex) {
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.NetHeartCheckFailed, ex);
            }


            if (!this.IsStarted)
                break;
        }
    }

    #endregion

    #region Object Override

    /// <summary>
    /// 获取本对象的字符串表示形式
    /// </summary>
    /// <returns>字符串</returns>
    public override string ToString() {
        return "NetComplexServer";
    }

    #endregion
}