using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Types;
using HslCommunication.Enthernet.SimplifyNet;
using HslCommunication.LogNet.Core;
#if !NET35
#endif

/*************************************************************************************
 *
 *    说明：
 *    本组件的所有网络类的基类。提供了一些基础的操作实现，部分实现需要集成实现
 *
 *    重构日期：2018年3月8日 21:22:05
 *
 *************************************************************************************/

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 本系统所有网络类的基类，该类为抽象类，无法进行实例化
/// </summary>
/// <remarks>
/// network base class, support basic operation with socket
/// </remarks>
public abstract class NetworkBase {
    /// <summary>
    /// 实例化一个NetworkBase对象
    /// </summary>
    /// <remarks>
    /// 令牌的默认值为空，都是0x00
    /// </remarks>
    public NetworkBase() {
        this.Token = Guid.Empty;
    }

    /// <summary>
    /// 组件的日志工具，支持日志记录
    /// </summary>
    /// <remarks>
    /// 只要实例化即可以记录日志，实例化的对象需要实现接口 <see cref="ILogNet"/> ，本组件提供了三个日志记录类，你可以实现基于 <see cref="ILogNet"/>  的对象。</remarks>
    /// <example>
    /// 如下的实例化适用于所有的Network及其派生类，以下举两个例子，三菱的设备类及服务器类
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkBase.cs" region="LogNetExample1" title="LogNet示例" />
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkBase.cs" region="LogNetExample2" title="LogNet示例" />
    /// </example>
    public ILogNet LogNet { get; set; }

    /// <summary>
    /// 网络类的身份令牌
    /// </summary>
    /// <remarks>
    /// 适用于Hsl协议相关的网络通信类，不适用于设备交互类。
    /// </remarks>
    /// <example>
    /// 此处以 <see cref="NetSimplifyServer"/> 服务器类及 <see cref="NetSimplifyClient"/> 客户端类的令牌设置举例
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkBase.cs" region="TokenClientExample" title="Client示例" />
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkBase.cs" region="TokenServerExample" title="Server示例" />
    /// </example>
    public Guid Token { get; set; }

    /// <summary>
    /// 是否使用同步的网络通讯
    /// </summary>
    public bool UseSynchronousNet { get; set; } = false;

    /// <summary>
    /// 通讯类的核心套接字
    /// </summary>
    protected Socket CoreSocket = null;

    /// <summary>
    /// 检查网络套接字是否操作超时，需要对套接字进行封装
    /// </summary>
    /// <param name="obj">通常是 <see cref="HslTimeOut"/> 对象 </param>
    protected void ThreadPoolCheckTimeOut(object obj) {
        if (obj is HslTimeOut timeout) {
            while (!timeout.IsSuccessful) {
                System.Threading.Thread.Sleep(100);
                if ((DateTime.Now - timeout.StartTime).TotalMilliseconds > timeout.DelayTime) {
                    // 连接超时或是验证超时
                    if (!timeout.IsSuccessful) {
                        this.LogNet?.WriteWarn(this.ToString(), "Wait Time Out : " + timeout.DelayTime);
                        timeout.Operator?.Invoke();
                        timeout.WorkSocket?.Close();
                    }

                    break;
                }
            }
        }
    }

    /*****************************************************************************
     *
     *    说明：
     *    下面的三个模块代码指示了如何接收数据，如何发送数据，如何连接网络
     *
     ********************************************************************************/

    /// <summary>
    /// 接收固定长度的字节数组
    /// </summary>
    /// <remarks>
    /// Receive Special Length Bytes
    /// </remarks>
    /// <param name="socket">网络通讯的套接字</param>
    /// <param name="length">准备接收的数据长度</param>
    /// <returns>包含了字节数据的结果类</returns>
    protected OperateResult<byte[]> Receive(Socket socket, int length) {
        if (length == 0)
            return OperateResult.CreateSuccessResult(new byte[0]);

        if (this.UseSynchronousNet) {
            try {
                byte[] data = NetSupport.ReadBytesFromSocket(socket, length);
                return OperateResult.CreateSuccessResult(data);
            }
            catch (Exception ex) {
                socket?.Close();
                this.LogNet?.WriteException(this.ToString(), "Receive", ex);
                return new OperateResult<byte[]>(ex.Message);
            }
        }

        OperateResult<byte[]> result = new OperateResult<byte[]>();
        ManualResetEvent receiveDone = null;
        StateObject state = null;
        try {
            receiveDone = new ManualResetEvent(false);
            state = new StateObject(length);
        }
        catch (Exception ex) {
            return new OperateResult<byte[]>(ex.Message);
        }


        try {
            state.WaitDone = receiveDone;
            state.WorkSocket = socket;

            // Begin receiving the data from the remote device.
            socket.BeginReceive(state.Buffer, state.AlreadyDealLength,
                state.DataLength - state.AlreadyDealLength, SocketFlags.None,
                new AsyncCallback(this.ReceiveCallback), state);
        }
        catch (Exception ex) {
            // 发生了错误，直接返回
            this.LogNet?.WriteException(this.ToString(), ex);
            result.Message = ex.Message;
            receiveDone.Close();
            socket?.Close();
            return result;
        }


        // 等待接收完成，或是发生异常
        receiveDone.WaitOne();
        receiveDone.Close();


        // 接收数据失败
        if (state.IsError) {
            socket?.Close();
            result.Message = state.ErrerMsg;
            return result;
        }


        // 远程关闭了连接
        if (state.IsClose) {
            // result.IsSuccess = true;
            result.Message = StringResources.Language.RemoteClosedConnection;
            socket?.Close();
            return result;
        }


        // 正常接收到数据
        result.Content = state.Buffer;
        result.IsSuccess = true;
        state.Clear();
        state = null;
        return result;
    }


    private void ReceiveCallback(IAsyncResult ar) {
        if (ar.AsyncState is StateObject state) {
            try {
                Socket client = state.WorkSocket;
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0) {
                    // 接收到了数据
                    state.AlreadyDealLength += bytesRead;
                    if (state.AlreadyDealLength < state.DataLength) {
                        // 获取接下来的所有的数据
                        client.BeginReceive(state.Buffer, state.AlreadyDealLength,
                            state.DataLength - state.AlreadyDealLength, SocketFlags.None,
                            new AsyncCallback(this.ReceiveCallback), state);
                    }
                    else {
                        // 接收到了所有的数据，通知接收数据的线程继续
                        state.WaitDone.Set();
                    }
                }
                else {
                    // 对方关闭了网络通讯
                    state.IsClose = true;
                    state.WaitDone.Set();
                }
            }
            catch (Exception ex) {
                state.IsError = true;
                this.LogNet?.WriteException(this.ToString(), "ReceiveCallback", ex);
                state.ErrerMsg = ex.Message;
                state.WaitDone.Set();
            }
        }
    }

#if !NET35

    /// <summary>
    /// 接收固定长度的字节数组
    /// </summary>
    /// <remarks>
    /// Receive Special Length Bytes
    /// </remarks>
    /// <param name="socket">网络通讯的套接字</param>
    /// <param name="length">准备接收的数据长度</param>
    /// <returns>包含了字节数据的结果类</returns>
    protected OperateResult<byte[]> ReceiveAsync(Socket socket, int length) {
        if (length <= 0)
            return OperateResult.CreateSuccessResult(new byte[0]);

        StateObjectAsync<byte[]>? state = new StateObjectAsync<byte[]>(length);
        state.Tcs = new TaskCompletionSource<byte[]>();
        state.WorkSocket = socket;

        try {
            socket.BeginReceive(state.Buffer, state.AlreadyDealLength, state.DataLength - state.AlreadyDealLength,
                SocketFlags.None, new AsyncCallback(this.ReceiveAsyncCallback), state);
            byte[] byteResult = state.Tcs.Task.Result;
            if (byteResult == null) {
                socket?.Close();
                return new OperateResult<byte[]>(StringResources.Language.RemoteClosedConnection);
            }

            state.Clear();
            state = null;
            return OperateResult.CreateSuccessResult(byteResult);
        }
        catch (Exception ex) {
            return new OperateResult<byte[]>(ex.Message);
        }
    }

    private void ReceiveAsyncCallback(IAsyncResult ar) {
        if (ar.AsyncState is StateObjectAsync<byte[]> state) {
            try {
                Socket socket = state.WorkSocket;
                int bytesRead = socket.EndReceive(ar);

                if (bytesRead > 0) {
                    // 接收到了数据
                    state.AlreadyDealLength += bytesRead;
                    if (state.AlreadyDealLength < state.DataLength) {
                        // 获取接下来的所有的数据
                        socket.BeginReceive(state.Buffer, state.AlreadyDealLength, state.DataLength - state.AlreadyDealLength,
                            SocketFlags.None, new AsyncCallback(this.ReceiveAsyncCallback), state);
                    }
                    else {
                        // 接收到了所有的数据，通知接收数据的线程继续
                        state.Tcs.SetResult(state.Buffer);
                    }
                }
                else {
                    // 对方关闭了网络通讯
                    state.IsClose = true;
                    state.Tcs.SetResult(null);
                }
            }
            catch (Exception ex) {
                state.IsError = true;
                this.LogNet?.WriteException(this.ToString(), "ReceiveAsyncCallback", ex);
                state.Tcs.SetException(ex);
            }
        }
    }

#endif

    /// <summary>
    /// 接收一条完整的 <seealso cref="INetMessage"/> 数据内容 ->
    /// Receive a complete <seealso cref="INetMessage"/> data content
    /// </summary>
    /// <param name="socket">网络的套接字</param>
    /// <param name="timeOut">超时时间</param>
    /// <param name="netMessage">消息的格式定义</param>
    /// <returns>带有是否成功的byte数组对象</returns>
    protected OperateResult<byte[]> ReceiveByMessage(Socket socket, int timeOut, INetMessage netMessage) {
        HslTimeOut hslTimeOut = new HslTimeOut() {
            DelayTime = timeOut,
            WorkSocket = socket,
        };
        if (timeOut > 0)
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ThreadPoolCheckTimeOut), hslTimeOut);

        // 接收指令头
        OperateResult<byte[]> headResult = this.Receive(socket, netMessage.ProtocolHeadBytesLength);
        if (!headResult.IsSuccess) {
            hslTimeOut.IsSuccessful = true;
            return headResult;
        }

        netMessage.HeadBytes = headResult.Content;
        int contentLength = netMessage.GetContentLengthByHeadBytes();
        if (contentLength <= 0) {
            hslTimeOut.IsSuccessful = true;
            return headResult;
        }

        OperateResult<byte[]> contentResult = this.Receive(socket, contentLength);
        if (!contentResult.IsSuccess) {
            hslTimeOut.IsSuccessful = true;
            return contentResult;
        }

        hslTimeOut.IsSuccessful = true;
        netMessage.ContentBytes = contentResult.Content;
        return OperateResult.CreateSuccessResult(SoftBasic.SpliceTwoByteArray(headResult.Content, contentResult.Content));
    }

    /// <summary>
    /// 发送消息给套接字，直到完成的时候返回
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="data">字节数据</param>
    /// <returns>发送是否成功的结果</returns>
    protected OperateResult Send(Socket socket, byte[] data) {
        if (data == null)
            return OperateResult.CreateSuccessResult();

        if (this.UseSynchronousNet) {
            try {
                socket.Send(data);
                return OperateResult.CreateSuccessResult();
            }
            catch (Exception ex) {
                socket?.Close();
                this.LogNet?.WriteException("Send", ex);
                return new OperateResult<byte[]>(ex.Message);
            }
        }

        OperateResult result = new OperateResult();
        ManualResetEvent sendDone = null;
        StateObject state = null;
        try {
            sendDone = new ManualResetEvent(false);
            state = new StateObject(data.Length);
        }
        catch (Exception ex) {
            return new OperateResult(ex.Message);
        }

        try {
            state.WaitDone = sendDone;
            state.WorkSocket = socket;
            state.Buffer = data;

            socket.BeginSend(state.Buffer, state.AlreadyDealLength, state.DataLength - state.AlreadyDealLength,
                SocketFlags.None, new AsyncCallback(this.SendCallBack), state);
        }
        catch (Exception ex) {
            // 发生了错误，直接返回
            this.LogNet?.WriteException(this.ToString(), ex);
            result.Message = ex.Message;
            socket?.Close();
            sendDone.Close();
            return result;
        }

        // 等待发送完成
        sendDone.WaitOne();
        sendDone.Close();

        if (state.IsError) {
            socket.Close();
            result.Message = state.ErrerMsg;
            return result;
        }

        state.Clear();
        state = null;
        result.IsSuccess = true;
        result.Message = StringResources.Language.SuccessText;

        return result;
    }

    /// <summary>
    /// 发送数据异步返回的方法
    /// </summary>
    /// <param name="ar">异步对象</param>
    private void SendCallBack(IAsyncResult ar) {
        if (ar.AsyncState is StateObject state) {
            try {
                Socket socket = state.WorkSocket;
                int byteSend = socket.EndSend(ar);
                state.AlreadyDealLength += byteSend;

                if (state.AlreadyDealLength < state.DataLength) {
                    // 继续发送数据
                    socket.BeginSend(state.Buffer, state.AlreadyDealLength, state.DataLength - state.AlreadyDealLength,
                        SocketFlags.None, new AsyncCallback(this.SendCallBack), state);
                }
                else {
                    // 发送完成
                    state.WaitDone.Set();
                }
            }
            catch (Exception ex) {
                // 发生了异常
                state.IsError = true;
                this.LogNet?.WriteException(this.ToString(), "SendCallBack", ex);
                state.ErrerMsg = ex.Message;
                state.WaitDone.Set();
            }
        }
    }

#if !NET35

    /// <summary>
    /// 发送一个异步的数据信息，该方式在NET35里是不可用的。
    /// </summary>
    /// <param name="socket">网络的套接字</param>
    /// <param name="data">数据内容</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendAsync(Socket socket, byte[] data) {
        if (data == null)
            return OperateResult.CreateSuccessResult();
        if (data.Length == 0)
            return OperateResult.CreateSuccessResult();

        StateObjectAsync<bool> state = new StateObjectAsync<bool>(data.Length);
        state.Tcs = new TaskCompletionSource<bool>();
        state.WorkSocket = socket;
        state.Buffer = data;

        try {
            socket.BeginSend(state.Buffer, state.AlreadyDealLength, state.DataLength - state.AlreadyDealLength,
                SocketFlags.None, new AsyncCallback(this.SendAsyncCallBack), state);
            bool boolResult = state.Tcs.Task.Result;
            return OperateResult.CreateSuccessResult();
        }
        catch (Exception ex) {
            return new OperateResult(ex.Message);
        }
    }

    private void SendAsyncCallBack(IAsyncResult ar) {
        if (ar.AsyncState is StateObjectAsync<bool> state) {
            try {
                Socket socket = state.WorkSocket;
                state.AlreadyDealLength += socket.EndSend(ar);

                if (state.AlreadyDealLength < state.DataLength) {
                    // 继续发送数据
                    socket.BeginSend(state.Buffer, state.AlreadyDealLength, state.DataLength - state.AlreadyDealLength,
                        SocketFlags.None, new AsyncCallback(this.SendAsyncCallBack), state);
                }
                else {
                    // 发送完成
                    state.Tcs.SetResult(true);
                }
            }
            catch (Exception ex) {
                state.IsError = true;
                this.LogNet?.WriteException("SendAsyncCallBack", ex);
                state.Tcs.SetException(ex);
            }
        }
    }

#endif

    /// <summary>
    /// 创建一个新的socket对象并连接到远程的地址，默认超时时间为10秒钟
    /// </summary>
    /// <param name="ipAddress">Ip地址</param>
    /// <param name="port">端口号</param>
    /// <returns>返回套接字的封装结果对象</returns>
    /// <example>
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
    /// </example>
    protected OperateResult<Socket> CreateSocketAndConnect(string ipAddress, int port) {
        return this.CreateSocketAndConnect(new IPEndPoint(IPAddress.Parse(ipAddress), port), 10000);
    }


    /// <summary>
    /// 创建一个新的socket对象并连接到远程的地址
    /// </summary>
    /// <param name="ipAddress">Ip地址</param>
    /// <param name="port">端口号</param>
    /// <param name="timeOut">连接的超时时间</param>
    /// <returns>返回套接字的封装结果对象</returns>
    /// <example>
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
    /// </example>
    protected OperateResult<Socket> CreateSocketAndConnect(string ipAddress, int port, int timeOut) {
        return this.CreateSocketAndConnect(new IPEndPoint(IPAddress.Parse(ipAddress), port), timeOut);
    }


    /// <summary>
    /// 创建一个新的socket对象并连接到远程的地址
    /// </summary>
    /// <param name="endPoint">连接的目标终结点</param>
    /// <param name="timeOut">连接的超时时间</param>
    /// <returns>返回套接字的封装结果对象</returns>
    /// <example>
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
    /// </example>
    protected OperateResult<Socket> CreateSocketAndConnect(IPEndPoint endPoint, int timeOut) {
        if (this.UseSynchronousNet) {
            Socket? socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try {
                HslTimeOut connectTimeout = new HslTimeOut() {
                    WorkSocket = socket,
                    DelayTime = timeOut
                };
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.ThreadPoolCheckTimeOut), connectTimeout);
                socket.Connect(endPoint);
                connectTimeout.IsSuccessful = true;

                return OperateResult.CreateSuccessResult(socket);
            }
            catch (Exception ex) {
                socket?.Close();
                this.LogNet?.WriteException("CreateSocketAndConnect", ex);
                return new OperateResult<Socket>(ex.Message);
            }
        }
        else {
            OperateResult<Socket> result = new OperateResult<Socket>();
            ManualResetEvent connectDone = null;
            StateObject state = null;
            try {
                connectDone = new ManualResetEvent(false);
                state = new StateObject();
            }
            catch (Exception ex) {
                return new OperateResult<Socket>(ex.Message);
            }


            Socket? socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 超时验证的信息
            HslTimeOut connectTimeout = new HslTimeOut() {
                WorkSocket = socket,
                DelayTime = timeOut
            };
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ThreadPoolCheckTimeOut), connectTimeout);

            try {
                state.WaitDone = connectDone;
                state.WorkSocket = socket;
                socket.BeginConnect(endPoint, new AsyncCallback(this.ConnectCallBack), state);
            }
            catch (Exception ex) {
                // 直接失败
                connectTimeout.IsSuccessful = true; // 退出线程池的超时检查
                this.LogNet?.WriteException(this.ToString(), ex); // 记录错误日志
                socket.Close(); // 关闭网络信息
                connectDone.Close(); // 释放等待资源
                result.Message = StringResources.Language.ConnectedFailed + ex.Message; // 传递错误消息
                return result;
            }


            // 等待连接完成
            connectDone.WaitOne();
            connectDone.Close();
            connectTimeout.IsSuccessful = true;

            if (state.IsError) {
                // 连接失败
                result.Message = StringResources.Language.ConnectedFailed + state.ErrerMsg;
                socket?.Close();
                return result;
            }


            result.Content = socket;
            result.IsSuccess = true;
            state.Clear();
            state = null;
            return result;
        }
    }


    /// <summary>
    /// 当连接的结果返回
    /// </summary>
    /// <param name="ar">异步对象</param>
    private void ConnectCallBack(IAsyncResult ar) {
        if (ar.AsyncState is StateObject state) {
            try {
                Socket socket = state.WorkSocket;
                socket.EndConnect(ar);
                state.WaitDone.Set();
            }
            catch (Exception ex) {
                // 发生了异常
                state.IsError = true;
                this.LogNet?.WriteException(this.ToString(), "ConnectCallBack", ex);
                state.ErrerMsg = ex.Message;
                state.WaitDone.Set();
            }
        }
    }

#if !NET35

    private OperateResult<Socket> ConnectAsync(IPEndPoint endPoint, int timeOut) {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        StateObjectAsync<Socket> state = new StateObjectAsync<Socket>();
        state.Tcs = new TaskCompletionSource<Socket>();
        state.WorkSocket = socket;

        // timeout check
        HslTimeOut connectTimeout = new HslTimeOut() {
            WorkSocket = socket,
            DelayTime = timeOut
        };
        ThreadPool.QueueUserWorkItem(new WaitCallback(this.ThreadPoolCheckTimeOut), connectTimeout);

        try {
            socket.BeginConnect(endPoint, new AsyncCallback(this.ConnectAsyncCallBack), state);
            socket = state.Tcs.Task.Result;
            return OperateResult.CreateSuccessResult(socket);
        }
        catch (Exception ex) {
            return new OperateResult<Socket>(ex.Message);
        }
    }

    private void ConnectAsyncCallBack(IAsyncResult ar) {
        if (ar.AsyncState is StateObjectAsync<Socket> state) {
            try {
                Socket socket = state.WorkSocket;
                socket.EndConnect(ar);
                state.Tcs.SetResult(socket);
            }
            catch (Exception ex) {
                // 发生了异常
                state.IsError = true;
                this.LogNet?.WriteException("ConnectAsyncCallBack", ex);
                state.ErrerMsg = ex.Message;
                state.Tcs.SetException(ex);
            }
        }
    }

#endif


    /*****************************************************************************
     *
     *    说明：
     *    下面的两个模块代码指示了如何读写文件
     *
     ********************************************************************************/

    /// <summary>
    /// 读取流中的数据到缓存区
    /// </summary>
    /// <param name="stream">数据流</param>
    /// <param name="buffer">缓冲区</param>
    /// <returns>带有成功标志的读取数据长度</returns>
    protected OperateResult<int> ReadStream(Stream stream, byte[] buffer) {
        ManualResetEvent WaitDone = new ManualResetEvent(false);
        FileStateObject stateObject = new FileStateObject {
            WaitDone = WaitDone,
            Stream = stream,
            DataLength = buffer.Length,
            Buffer = buffer
        };

        try {
            stream.BeginRead(buffer, 0, stateObject.DataLength, new AsyncCallback(this.ReadStreamCallBack), stateObject);
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), ex);
            stateObject = null;
            WaitDone.Close();
            return new OperateResult<int>();
        }

        WaitDone.WaitOne();
        WaitDone.Close();
        if (stateObject.IsError) {
            return new OperateResult<int>() {
                Message = stateObject.ErrerMsg
            };
        }
        else {
            return OperateResult.CreateSuccessResult(stateObject.AlreadyDealLength);
        }
    }


    private void ReadStreamCallBack(IAsyncResult ar) {
        if (ar.AsyncState is FileStateObject stateObject) {
            try {
                stateObject.AlreadyDealLength += stateObject.Stream.EndRead(ar);
                stateObject.WaitDone.Set();
            }
            catch (Exception ex) {
                this.LogNet?.WriteException(this.ToString(), ex);
                stateObject.IsError = true;
                stateObject.ErrerMsg = ex.Message;
                stateObject.WaitDone.Set();
            }
        }
    }

    /// <summary>
    /// 将缓冲区的数据写入到流里面去
    /// </summary>
    /// <param name="stream">数据流</param>
    /// <param name="buffer">缓冲区</param>
    /// <returns>是否写入成功</returns>
    protected OperateResult WriteStream(Stream stream, byte[] buffer) {
        ManualResetEvent WaitDone = new ManualResetEvent(false);
        FileStateObject stateObject = new FileStateObject {
            WaitDone = WaitDone,
            Stream = stream
        };

        try {
            stream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(this.WriteStreamCallBack), stateObject);
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), ex);
            stateObject = null;
            WaitDone.Close();
            return new OperateResult(ex.Message);
        }

        WaitDone.WaitOne();
        WaitDone.Close();
        if (stateObject.IsError) {
            return new OperateResult() {
                Message = stateObject.ErrerMsg
            };
        }
        else {
            return OperateResult.CreateSuccessResult();
        }
    }

    private void WriteStreamCallBack(IAsyncResult ar) {
        if (ar.AsyncState is FileStateObject stateObject) {
            try {
                stateObject.Stream.EndWrite(ar);
            }
            catch (Exception ex) {
                this.LogNet?.WriteException(this.ToString(), ex);
                stateObject.IsError = true;
                stateObject.ErrerMsg = ex.Message;
            }
            finally {
                stateObject.WaitDone.Set();
            }
        }
    }

    /// <summary>
    /// 检查当前的头子节信息的令牌是否是正确的
    /// </summary>
    /// <param name="headBytes">头子节数据</param>
    /// <returns>令牌是验证成功</returns>
    protected bool CheckRemoteToken(byte[] headBytes) {
        return SoftBasic.IsByteTokenEquel(headBytes, this.Token);
    }

    /// <summary>
    /// [自校验] 发送字节数据并确认对方接收完成数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="headcode">头指令</param>
    /// <param name="customer">用户指令</param>
    /// <param name="send">发送的数据</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendBaseAndCheckReceive(Socket socket, int headcode, int customer, byte[] send) {
        // 数据处理
        send = HslProtocol.CommandBytes(headcode, customer, this.Token, send);

        // 发送数据
        OperateResult sendResult = this.Send(socket, send);
        if (!sendResult.IsSuccess)
            return sendResult;

        // 检查对方接收完成
        OperateResult<long> checkResult = this.ReceiveLong(socket);
        if (!checkResult.IsSuccess)
            return checkResult;

        // 检查长度接收
        if (checkResult.Content != send.Length) {
            socket?.Close();
            return new OperateResult(StringResources.Language.CommandLengthCheckFailed);
        }

        return checkResult;
    }


    /// <summary>
    /// [自校验] 发送字节数据并确认对方接收完成数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="customer">用户指令</param>
    /// <param name="send">发送的数据</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendBytesAndCheckReceive(Socket socket, int customer, byte[] send) {
        return this.SendBaseAndCheckReceive(socket, HslProtocol.ProtocolUserBytes, customer, send);
    }


    /// <summary>
    /// [自校验] 直接发送字符串数据并确认对方接收完成数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="customer">用户指令</param>
    /// <param name="send">发送的数据</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendStringAndCheckReceive(Socket socket, int customer, string send) {
        byte[] data = string.IsNullOrEmpty(send) ? null : Encoding.Unicode.GetBytes(send);

        return this.SendBaseAndCheckReceive(socket, HslProtocol.ProtocolUserString, customer, data);
    }

    /// <summary>
    /// [自校验] 直接发送字符串数组并确认对方接收完成数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="customer">用户指令</param>
    /// <param name="sends">发送的字符串数组</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendStringAndCheckReceive(Socket socket, int customer, string[] sends) {
        return this.SendBaseAndCheckReceive(socket, HslProtocol.ProtocolUserStringArray, customer, HslProtocol.PackStringArrayToByte(sends));
    }

    /// <summary>
    /// [自校验] 直接发送字符串数组并确认对方接收完成数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="customer">用户指令</param>
    /// <param name="name">用户名</param>
    /// <param name="pwd">密码</param>
    /// <returns>是否发送成功</returns>
    protected OperateResult SendAccountAndCheckReceive(Socket socket, int customer, string name, string pwd) {
        return this.SendBaseAndCheckReceive(socket, HslProtocol.ProtocolAccountLogin, customer, HslProtocol.PackStringArrayToByte(new string[] { name, pwd }));
    }

    /// <summary>
    /// [自校验] 接收一条完整的同步数据，包含头子节和内容字节，基础的数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="timeout">超时时间设置，如果为负数，则不检查超时</param>
    /// <returns>包含是否成功的结果对象</returns>
    /// <exception cref="ArgumentNullException">result</exception>
    protected OperateResult<byte[], byte[]> ReceiveAndCheckBytes(Socket socket, int timeout) {
        // 30秒超时接收验证
        HslTimeOut hslTimeOut = new HslTimeOut() {
            DelayTime = timeout,
            IsSuccessful = false,
            StartTime = DateTime.Now,
            WorkSocket = socket,
        };

        if (timeout > 0)
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ThreadPoolCheckTimeOut), hslTimeOut);

        // 接收头指令
        OperateResult<byte[]> headResult = this.Receive(socket, HslProtocol.HeadByteLength);
        if (!headResult.IsSuccess) {
            hslTimeOut.IsSuccessful = true;
            return OperateResult.CreateFailedResult<byte[], byte[]>(headResult);
        }

        hslTimeOut.IsSuccessful = true;

        // 检查令牌
        if (!this.CheckRemoteToken(headResult.Content)) {
            socket?.Close();
            return new OperateResult<byte[], byte[]>(StringResources.Language.TokenCheckFailed);
        }

        int contentLength = BitConverter.ToInt32(headResult.Content, HslProtocol.HeadByteLength - 4);
        // 接收内容
        OperateResult<byte[]> contentResult = this.Receive(socket, contentLength);
        if (!contentResult.IsSuccess)
            return OperateResult.CreateFailedResult<byte[], byte[]>(contentResult);

        // 返回成功信息
        OperateResult checkResult = this.SendLong(socket, HslProtocol.HeadByteLength + contentLength);
        if (!checkResult.IsSuccess)
            return OperateResult.CreateFailedResult<byte[], byte[]>(checkResult);

        byte[] head = headResult.Content;
        byte[] content = contentResult.Content;
        content = HslProtocol.CommandAnalysis(head, content);
        return OperateResult.CreateSuccessResult(head, content);
    }

    /// <summary>
    /// [自校验] 从网络中接收一个字符串数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <returns>包含是否成功的结果对象</returns>
    protected OperateResult<int, string> ReceiveStringContentFromSocket(Socket socket) {
        OperateResult<byte[], byte[]> receive = this.ReceiveAndCheckBytes(socket, 10000);
        if (!receive.IsSuccess)
            return OperateResult.CreateFailedResult<int, string>(receive);

        // 检查是否是字符串信息
        if (BitConverter.ToInt32(receive.Content1, 0) != HslProtocol.ProtocolUserString) {
            this.LogNet?.WriteError(this.ToString(), StringResources.Language.CommandHeadCodeCheckFailed);
            socket?.Close();
            return new OperateResult<int, string>(StringResources.Language.CommandHeadCodeCheckFailed);
        }

        if (receive.Content2 == null)
            receive.Content2 = new byte[0];
        // 分析数据
        return OperateResult.CreateSuccessResult(BitConverter.ToInt32(receive.Content1, 4), Encoding.Unicode.GetString(receive.Content2));
    }

    /// <summary>
    /// [自校验] 从网络中接收一个字符串数组，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <returns>包含是否成功的结果对象</returns>
    protected OperateResult<int, string[]> ReceiveStringArrayContentFromSocket(Socket socket) {
        OperateResult<byte[], byte[]> receive = this.ReceiveAndCheckBytes(socket, 10000);
        if (!receive.IsSuccess)
            return OperateResult.CreateFailedResult<int, string[]>(receive);

        // 检查是否是字符串信息
        if (BitConverter.ToInt32(receive.Content1, 0) != HslProtocol.ProtocolUserStringArray) {
            this.LogNet?.WriteError(this.ToString(), StringResources.Language.CommandHeadCodeCheckFailed);
            socket?.Close();
            return new OperateResult<int, string[]>(StringResources.Language.CommandHeadCodeCheckFailed);
        }

        if (receive.Content2 == null)
            receive.Content2 = new byte[4];
        return OperateResult.CreateSuccessResult(BitConverter.ToInt32(receive.Content1, 4), HslProtocol.UnPackStringArrayFromByte(receive.Content2));
    }

    /// <summary>
    /// [自校验] 从网络中接收一串字节数据，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">套接字的网络</param>
    /// <returns>包含是否成功的结果对象</returns>
    protected OperateResult<int, byte[]> ReceiveBytesContentFromSocket(Socket socket) {
        OperateResult<byte[], byte[]> receive = this.ReceiveAndCheckBytes(socket, 10000);
        if (!receive.IsSuccess)
            return OperateResult.CreateFailedResult<int, byte[]>(receive);

        // 检查是否是字节信息
        if (BitConverter.ToInt32(receive.Content1, 0) != HslProtocol.ProtocolUserBytes) {
            this.LogNet?.WriteError(this.ToString(), StringResources.Language.CommandHeadCodeCheckFailed);
            socket?.Close();
            return new OperateResult<int, byte[]>(StringResources.Language.CommandHeadCodeCheckFailed);
        }

        // 分析数据
        return OperateResult.CreateSuccessResult(BitConverter.ToInt32(receive.Content1, 4), receive.Content2);
    }


    /// <summary>
    /// 从网络中接收Long数据
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    private OperateResult<long> ReceiveLong(Socket socket) {
        OperateResult<byte[]> read = this.Receive(socket, 8);
        if (read.IsSuccess) {
            return OperateResult.CreateSuccessResult(BitConverter.ToInt64(read.Content, 0));
        }
        else {
            return new OperateResult<long>() {
                Message = read.Message,
            };
        }
    }

    /// <summary>
    /// 将long数据发送到套接字
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private OperateResult SendLong(Socket socket, long value) {
        return this.Send(socket, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串</returns>
    public override string ToString() {
        return "NetworkBase";
    }
}