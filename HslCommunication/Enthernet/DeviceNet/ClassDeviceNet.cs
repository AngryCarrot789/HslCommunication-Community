using HslCommunication.Core;
using System.Text;
using System.Net;
using System.Net.Sockets;
using HslCommunication.Core.Net;

namespace HslCommunication.Enthernet;

/// <summary>
/// 通用设备的基础网络信息
/// </summary>
public class DeviceNet : NetworkServerBase {
    #region Constructor

    /// <summary>
    /// 实例化一个通用的设备类
    /// </summary>
    public DeviceNet() {
        this.list = new List<DeviceState>();
        this.lock_list = new SimpleHybirdLock();
    }

    #endregion

    #region Connection Management

    private List<DeviceState> list; // 所有客户端的连接对象
    private SimpleHybirdLock lock_list; // 列表锁

    private void AddClient(DeviceState device) {
        this.lock_list.Enter();
        this.list.Add(device);
        this.lock_list.Leave();

        this.ClientOnline?.Invoke(device);
    }

    private void RemoveClient(DeviceState device) {
        this.lock_list.Enter();
        this.list.Remove(device);
        device.WorkSocket?.Close();
        this.lock_list.Leave();

        this.ClientOffline?.Invoke(device);
    }

    #endregion

    #region Event Handle

    /// <summary>
    /// 当客户端上线的时候，触发此事件
    /// </summary>
    public event Action<DeviceState> ClientOnline;

    /// <summary>
    /// 当客户端下线的时候，触发此事件
    /// </summary>
    public event Action<DeviceState> ClientOffline;


    /// <summary>
    /// 按照ASCII文本的方式进行触发接收的数据
    /// </summary>
    public event Action<DeviceState, string> AcceptString;

    /// <summary>
    /// 按照字节的方式进行触发接收的数据
    /// </summary>
    public event Action<DeviceState, byte[]> AcceptBytes;

    #endregion

    #region Private Member

    private readonly byte endByte = 0x0D; // 结束的指令

    #endregion

    /// <summary>
    /// 当接收到了新的请求的时候执行的操作
    /// </summary>
    /// <param name="socket">异步对象</param>
    /// <param name="endPoint">终结点</param>
    protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint) {
        // 登录成功
        DeviceState stateone = new DeviceState() {
            WorkSocket = socket,
            DeviceEndPoint = (IPEndPoint) socket.RemoteEndPoint,
            IpAddress = ((IPEndPoint) socket.RemoteEndPoint).Address.ToString(),
            ConnectTime = DateTime.Now,
        };

        this.AddClient(stateone);

        try {
            stateone.WorkSocket.BeginReceive(stateone.Buffer, 0, stateone.Buffer.Length, SocketFlags.None,
                new AsyncCallback(this.ContentReceiveCallBack), stateone);
        }
        catch (Exception ex) {
            //登录前已经出错
            this.RemoveClient(stateone);
            this.LogNet?.WriteException(this.ToString(), StringResources.Language.NetClientLoginFailed, ex);
        }
    }


    private void ContentReceiveCallBack(IAsyncResult ar) {
        if (ar.AsyncState is DeviceState stateone) {
            try {
                int count = stateone.WorkSocket.EndReceive(ar);

                if (count > 0) {
                    MemoryStream ms = new MemoryStream();
                    byte next = stateone.Buffer[0];

                    while (next != this.endByte) {
                        ms.WriteByte(next);
                        byte[] buffer = new byte[1];
                        stateone.WorkSocket.Receive(buffer, 0, 1, SocketFlags.None);
                        next = buffer[0];
                    }

                    // 接收完成
                    stateone.WorkSocket.BeginReceive(stateone.Buffer, 0, stateone.Buffer.Length, SocketFlags.None,
                        new AsyncCallback(this.ContentReceiveCallBack), stateone);


                    byte[] receive = ms.ToArray();
                    ms.Dispose();

                    this.lock_list.Enter();
                    stateone.ReceiveTime = DateTime.Now;
                    this.lock_list.Leave();
                    this.AcceptBytes?.Invoke(stateone, receive);
                    this.AcceptString?.Invoke(stateone, Encoding.ASCII.GetString(receive));
                }
                else {
                    this.RemoveClient(stateone);
                    this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.NetClientOffline);
                }
            }
            catch (Exception ex) {
                //登录前已经出错
                this.RemoveClient(stateone);
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.NetClientLoginFailed, ex);
            }
        }
    }
}