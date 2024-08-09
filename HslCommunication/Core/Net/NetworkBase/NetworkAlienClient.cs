using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Types;

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 异形客户端的基类，提供了基础的异形操作
/// </summary>
public class NetworkAlienClient : NetworkServerBase {
    /// <summary>
    /// 默认的无参构造方法
    /// </summary>
    public NetworkAlienClient() {
        this.password = new byte[6];
        this.alreadyLock = new SimpleHybirdLock();
        this.alreadyOnline = new List<AlienSession>();
        this.trustOnline = new List<string>();
        this.trustLock = new SimpleHybirdLock();
        this.ThreadCheckStart();
    }

    /// <summary>
    /// 当接收到了新的请求的时候执行的操作
    /// </summary>
    /// <param name="socket">异步对象</param>
    /// <param name="endPoint">终结点</param>
    protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint) {
        // 注册包
        // 0x48 0x73 0x6E 0x00 0x17 0x31 0x32 0x33 0x34 0x35 0x36 0x37 0x38 0x39 0x30 0x31 0x00 0x00 0x00 0x00 0x00 0x00 0xC0 0xA8 0x00 0x01 0x17 0x10
        // +------------+ +--+ +--+ +----------------------------------------------------+ +---------------------------+ +-----------------+ +-------+
        // + 固定消息头  +备用 长度           DTU码 12345678901 (唯一标识)                  登录密码(不受信的排除)     Ip:192.168.0.1    端口10000
        // +------------+ +--+ +--+ +----------------------------------------------------+ +---------------------------+ +-----------------+

        // 返回
        // 0x48 0x73 0x6E 0x00 0x01 0x00
        // +------------+ +--+ +--+ +--+
        //   固定消息头  备用 长度 结果代码

        // 结果代码 
        // 0x00: 登录成功 
        // 0x01: DTU重复登录 
        // 0x02: DTU禁止登录
        // 0x03: 密码验证失败 

        OperateResult<byte[]> check = this.ReceiveByMessage(socket, 5000, new AlienMessage());
        if (!check.IsSuccess)
            return;

        if (check.Content[4] != 0x17 || check.Content.Length != 0x1C) {
            socket?.Close();
            this.LogNet?.WriteWarn(this.ToString(), "Length Check Failed");
            return;
        }

        // 密码验证
        bool isPasswrodRight = true;
        for (int i = 0; i < this.password.Length; i++) {
            if (check.Content[16 + i] != this.password[i]) {
                isPasswrodRight = false;
                break;
            }
        }

        string dtu = Encoding.ASCII.GetString(check.Content, 5, 11).Trim();

        // 密码失败的情况
        if (!isPasswrodRight) {
            OperateResult send = this.Send(socket, this.GetResponse(StatusPasswodWrong));
            if (send.IsSuccess)
                socket?.Close();
            this.LogNet?.WriteWarn(this.ToString(), "Login Password Wrong, Id:" + dtu);
            return;
        }

        AlienSession session = new AlienSession() {
            DTU = dtu,
            Socket = socket,
        };

        // 检测是否禁止登录
        if (!this.IsClientPermission(session)) {
            OperateResult send = this.Send(socket, this.GetResponse(StatusLoginForbidden));
            if (send.IsSuccess)
                socket?.Close();
            this.LogNet?.WriteWarn(this.ToString(), "Login Forbidden, Id:" + session.DTU);
            return;
        }

        // 检测是否重复登录，不重复的话，也就是意味着登录成功了
        if (this.IsClientOnline(session)) {
            OperateResult send = this.Send(socket, this.GetResponse(StatusLoginRepeat));
            if (send.IsSuccess)
                socket?.Close();
            this.LogNet?.WriteWarn(this.ToString(), "Login Repeat, Id:" + session.DTU);
            return;
        }
        else {
            OperateResult send = this.Send(socket, this.GetResponse(StatusOk));
            if (!send.IsSuccess)
                return;
        }

        // 触发上线消息
        this.OnClientConnected?.Invoke(this, session);
    }

    /// <summary>
    /// 当有服务器连接上来的时候触发
    /// </summary>
    public event Action<NetworkAlienClient, AlienSession> OnClientConnected = null;

    /// <summary>
    /// 获取返回的命令信息
    /// </summary>
    /// <param name="status">状态</param>
    /// <returns>回发的指令信息</returns>
    private byte[] GetResponse(byte status) {
        return new byte[] {
            0x48, 0x73, 0x6E, 0x00, 0x01, status
        };
    }

    /// <summary>
    /// 状态登录成功
    /// </summary>
    private const byte StatusOk = 0x00;

    /// <summary>
    /// 重复登录
    /// </summary>
    private const byte StatusLoginRepeat = 0x01;

    /// <summary>
    /// 禁止登录
    /// </summary>
    private const byte StatusLoginForbidden = 0x02;

    /// <summary>
    /// 密码错误
    /// </summary>
    private const byte StatusPasswodWrong = 0x03;


    /// <summary>
    /// 检测当前的DTU是否在线
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
    private bool IsClientOnline(AlienSession session) {
        bool result = false;
        this.alreadyLock.Enter();

        for (int i = 0; i < this.alreadyOnline.Count; i++) {
            if (this.alreadyOnline[i].DTU == session.DTU) {
                result = true;
                break;
            }
        }


        if (!result) {
            this.alreadyOnline.Add(session);
        }

        this.alreadyLock.Leave();

        return result;
    }

    /// <summary>
    /// 检测当前的dtu是否允许登录
    /// </summary>
    /// <param name="session"></param>
    /// <returns></returns>
    private bool IsClientPermission(AlienSession session) {
        bool result = false;

        this.trustLock.Enter();

        if (this.trustOnline.Count == 0) {
            result = true;
        }
        else {
            for (int i = 0; i < this.trustOnline.Count; i++) {
                if (this.trustOnline[i] == session.DTU) {
                    result = true;
                    break;
                }
            }
        }

        this.trustLock.Leave();

        return result;
    }

    /// <summary>
    /// 设置密码，长度为6
    /// </summary>
    /// <param name="password"></param>
    public void SetPassword(byte[] password) {
        if (password?.Length == 6) {
            password.CopyTo(this.password, 0);
        }
    }

    /// <summary>
    /// 设置可信任的客户端列表
    /// </summary>
    /// <param name="clients"></param>
    public void SetTrustClients(string[] clients) {
        this.trustLock.Enter();

        this.trustOnline = new List<string>(clients);

        this.trustLock.Leave();
    }


    /// <summary>
    /// 退出异形客户端
    /// </summary>
    /// <param name="session">异形客户端的会话</param>
    public void AlienSessionLoginOut(AlienSession session) {
        this.alreadyLock.Enter();

        this.alreadyOnline.Remove(session);

        this.alreadyLock.Leave();
    }

    private void ThreadCheckStart() {
        this.threadCheck = new System.Threading.Thread(new ThreadStart(this.ThreadCheckAlienClient));
        this.threadCheck.IsBackground = true;
        this.threadCheck.Priority = ThreadPriority.AboveNormal;
        this.threadCheck.Start();
    }

    private void ThreadCheckAlienClient() {
        System.Threading.Thread.Sleep(1000);
        while (true) {
            System.Threading.Thread.Sleep(1000);

            this.alreadyLock.Enter();

            for (int i = this.alreadyOnline.Count - 1; i >= 0; i--) {
                if (!this.alreadyOnline[i].IsStatusOk) {
                    this.alreadyOnline.RemoveAt(i);
                }
            }


            this.alreadyLock.Leave();
        }
    }

    private byte[] password; // 密码设置
    private List<AlienSession> alreadyOnline; // 所有在线信息
    private SimpleHybirdLock alreadyLock; // 列表的同步锁
    private List<string> trustOnline; // 禁止登录的客户端信息
    private SimpleHybirdLock trustLock; // 禁止登录的锁
    private System.Threading.Thread threadCheck; // 后台检测在线情况的

    /// <summary>
    /// 获取本对象的字符串表示形式
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "NetworkAlienBase";
    }
}