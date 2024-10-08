﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 所有虚拟的数据服务器的基类
/// </summary>
public class NetworkDataServerBase : NetworkAuthenticationServerBase, IDisposable {
    /// <summary>
    /// 实例化一个默认的数据服务器的对象
    /// </summary>
    public NetworkDataServerBase() {
        this.lock_trusted_clients = new SimpleHybirdLock();


        this.lockOnlineClient = new SimpleHybirdLock();
        this.listsOnlineClient = new List<AppSession>();
    }

    /// <summary>
    /// 从设备读取原始数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">地址长度</param>
    /// <returns>带有成功标识的结果对象</returns>
    /// <remarks>需要在继承类中重写实现，并且实现地址解析操作</remarks>
    public virtual OperateResult<byte[]> Read(string address, ushort length) {
        return new OperateResult<byte[]>();
    }

    /// <summary>
    /// 将原始数据写入设备
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="value">原始数据</param>
    /// <returns>带有成功标识的结果对象</returns>
    /// <remarks>需要在继承类中重写实现，并且实现地址解析操作</remarks>
    public virtual OperateResult Write(string address, byte[] value) {
        return new OperateResult();
    }

    /// <summary>
    /// 从字节数据加载数据信息
    /// </summary>
    /// <param name="content">字节数据</param>
    protected virtual void LoadFromBytes(byte[] content) {
    }

    /// <summary>
    /// 将数据信息存储到字节数组去
    /// </summary>
    /// <returns>所有的内容</returns>
    protected virtual byte[] SaveToBytes() {
        return Array.Empty<byte>();
    }

    /// <summary>
    /// 将本系统的数据池数据存储到指定的文件
    /// </summary>
    /// <param name="path">指定文件的路径</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="System.IO.PathTooLongException"></exception>
    /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
    /// <exception cref="System.IO.IOException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="System.Security.SecurityException"></exception>
    public void SaveDataPool(string path) {
        byte[] content = this.SaveToBytes();
        File.WriteAllBytes(path, content);
    }

    /// <summary>
    /// 从文件加载数据池信息
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="System.IO.PathTooLongException"></exception>
    /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
    /// <exception cref="System.IO.IOException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="System.Security.SecurityException"></exception>
    /// <exception cref="System.IO.FileNotFoundException"></exception>
    public void LoadDataPool(string path) {
        if (File.Exists(path)) {
            byte[] buffer = File.ReadAllBytes(path);
            this.LoadFromBytes(buffer);
        }
    }

    /// <summary>
    /// 系统的数据转换接口
    /// </summary>
    public IByteTransform ByteTransform { get; set; }

    /// <summary>
    /// 当接收到来自客户的数据信息时触发的对象，该数据可能来自tcp或是串口
    /// </summary>
    /// <param name="sender">本服务器对象</param>
    /// <param name="data">实际的数据信息</param>
    public delegate void DataReceivedDelegate(object sender, byte[] data);

    /// <summary>
    /// 接收到数据的时候就行触发
    /// </summary>
    public event DataReceivedDelegate OnDataReceived;

    /// <summary>
    /// 触发一个数据接收的事件信息
    /// </summary>
    /// <param name="receive">接收数据信息</param>
    protected void RaiseDataReceived(byte[] receive) {
        this.OnDataReceived?.Invoke(this, receive);
    }

    /// <summary>
    /// Show DataSend To PLC
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    public delegate void DataSendDelegate(object sender, byte[] data);

    /// <summary>
    /// OnDataSend
    /// </summary>
    public event DataSendDelegate OnDataSend;

    /// <summary>
    /// RaiseDataSend
    /// </summary>
    /// <param name="receive"></param>
    protected void RaiseDataSend(byte[] receive) {
        this.OnDataSend?.Invoke(this, receive);
    }

    /// <summary>
    /// 单个数据字节的长度，西门子为2，三菱，欧姆龙，modbusTcp就为1，AB PLC无效
    /// </summary>
    /// <remarks>对设备来说，一个地址的数据对应的字节数，或是1个字节或是2个字节</remarks>
    protected ushort WordLength { get; set; } = 1;

    /// <summary>
    /// 当客户端登录后，进行Ip信息的过滤，然后触发本方法，也就是说之后的客户端需要
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="endPoint">终端节点</param>
    protected virtual void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint) {
    }

    /// <summary>
    /// 当接收到了新的请求的时候执行的操作
    /// </summary>
    /// <param name="socket">异步对象</param>
    /// <param name="endPoint">终结点</param>
    protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint) {
        // 为了提高系统的响应能力，采用异步来实现，即时有数万台设备接入也能应付
        string ipAddress = endPoint.Address.ToString();

        if (this.IsTrustedClientsOnly) {
            // 检查受信任的情况
            if (!this.CheckIpAddressTrusted(ipAddress)) {
                // 客户端不被信任，退出
                this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientDisableLogin, endPoint));
                socket.Close();
                return;
            }
        }

        if (!this.IsUseAccountCertificate)
            this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientOnlineInfo, endPoint));
        this.ThreadPoolLoginAfterClientCheck(socket, endPoint);
    }


    private List<string> TrustedClients = null; // 受信任的客户端
    private bool IsTrustedClientsOnly = false; // 是否启用仅仅受信任的客户端登录
    private SimpleHybirdLock lock_trusted_clients; // 受信任的客户端的列表

    /// <summary>
    /// 设置并启动受信任的客户端登录并读写，如果为null，将关闭对客户端的ip验证
    /// </summary>
    /// <param name="clients">受信任的客户端列表</param>
    public void SetTrustedIpAddress(List<string> clients) {
        this.lock_trusted_clients.Enter();
        if (clients != null) {
            this.TrustedClients = clients.Select(m => {
                IPAddress iPAddress = IPAddress.Parse(m);
                return iPAddress.ToString();
            }).ToList();
            this.IsTrustedClientsOnly = true;
        }
        else {
            this.TrustedClients = new List<string>();
            this.IsTrustedClientsOnly = false;
        }

        this.lock_trusted_clients.Leave();
    }

    /// <summary>
    /// 检查该Ip地址是否是受信任的
    /// </summary>
    /// <param name="ipAddress">Ip地址信息</param>
    /// <returns>是受信任的返回<c>True</c>，否则返回<c>False</c></returns>
    private bool CheckIpAddressTrusted(string ipAddress) {
        if (this.IsTrustedClientsOnly) {
            bool result = false;
            this.lock_trusted_clients.Enter();
            for (int i = 0; i < this.TrustedClients.Count; i++) {
                if (this.TrustedClients[i] == ipAddress) {
                    result = true;
                    break;
                }
            }

            this.lock_trusted_clients.Leave();
            return result;
        }
        else {
            return false;
        }
    }

    /// <summary>
    /// 获取受信任的客户端列表
    /// </summary>
    /// <returns>字符串数据信息</returns>
    public string[] GetTrustedClients() {
        string[] result = Array.Empty<string>();
        this.lock_trusted_clients.Enter();
        if (this.TrustedClients != null) {
            result = this.TrustedClients.ToArray();
        }

        this.lock_trusted_clients.Leave();
        return result;
    }

    /// <summary>
    /// 在线的客户端的数量
    /// </summary>
    public int OnlineCount => this.onlineCount;

    private List<AppSession> listsOnlineClient;
    private SimpleHybirdLock lockOnlineClient;
    private int onlineCount = 0; // 在线的客户端的数量

    /// <summary>
    /// 新增一个在线的客户端信息
    /// </summary>
    /// <param name="session">会话内容</param>
    protected void AddClient(AppSession session) {
        this.lockOnlineClient.Enter();
        this.listsOnlineClient.Add(session);
        this.onlineCount++;
        this.lockOnlineClient.Leave();
    }

    /// <summary>
    /// 移除在线的客户端信息
    /// </summary>
    /// <param name="session">会话内容</param>
    protected void RemoveClient(AppSession session) {
        this.lockOnlineClient.Enter();
        if (this.listsOnlineClient.Remove(session)) {
            this.onlineCount--;
        }

        this.lockOnlineClient.Leave();
    }

    /// <summary>
    /// 关闭之后进行的操作
    /// </summary>
    protected override void CloseAction() {
        base.CloseAction();

        this.lockOnlineClient.Enter();
        for (int i = 0; i < this.listsOnlineClient.Count; i++) {
            this.listsOnlineClient[i]?.WorkSocket?.Close();
        }

        this.listsOnlineClient.Clear();
        this.lockOnlineClient.Leave();
    }

    /// <summary>
    /// 读取自定义类型的数据，需要规定解析规则
    /// </summary>
    /// <typeparam name="T">类型名称</typeparam>
    /// <param name="address">起始地址</param>
    /// <returns>带有成功标识的结果对象</returns>
    /// <remarks>
    /// 需要是定义一个类，选择好相对于的ByteTransform实例，才能调用该方法。
    /// </remarks>
    /// <example>
    /// 此处演示三菱的读取示例，先定义一个类，实现<see cref="IDataTransfer"/>接口
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="IDataTransfer Example" title="DataMy示例" />
    /// 接下来就可以实现数据的读取了
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadCustomerExample" title="ReadCustomer示例" />
    /// </example>
    public OperateResult<T> ReadCustomer<T>(string address) where T : IDataTransfer, new() {
        OperateResult<T> result = new OperateResult<T>();
        T Content = new T();
        OperateResult<byte[]> read = this.Read(address, Content.ReadCount);
        if (read.IsSuccess) {
            Content.ParseSource(read.Content);
            result.Content = Content;
            result.IsSuccess = true;
        }
        else {
            result.ErrorCode = read.ErrorCode;
            result.Message = read.Message;
        }

        return result;
    }

    /// <summary>
    /// 写入自定义类型的数据到设备去，需要规定生成字节的方法
    /// </summary>
    /// <typeparam name="T">自定义类型</typeparam>
    /// <param name="address">起始地址</param>
    /// <param name="data">实例对象</param>
    /// <returns>带有成功标识的结果对象</returns>
    /// <remarks>
    /// 需要是定义一个类，选择好相对于的<see cref="IDataTransfer"/>实例，才能调用该方法。
    /// </remarks>
    /// <example>
    /// 此处演示三菱的读取示例，先定义一个类，实现<see cref="IDataTransfer"/>接口
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="IDataTransfer Example" title="DataMy示例" />
    /// 接下来就可以实现数据的读取了
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteCustomerExample" title="WriteCustomer示例" />
    /// </example>
    public OperateResult WriteCustomer<T>(string address, T data) where T : IDataTransfer, new() {
        return this.Write(address, data.ToSource());
    }

    /// <summary>
    /// 读取设备的short类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt16" title="Int16类型示例" />
    /// </example>
    public OperateResult<short> ReadInt16(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadInt16(address, 1));
    }


    /// <summary>
    /// 读取设备的short类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt16Array" title="Int16类型示例" />
    /// </example>
    public virtual OperateResult<short[]> ReadInt16(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength)), m => this.ByteTransform.TransInt16(m, 0, length));
    }

    /// <summary>
    /// 读取设备的ushort数据类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt16" title="UInt16类型示例" />
    /// </example>
    public OperateResult<ushort> ReadUInt16(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadUInt16(address, 1));
    }


    /// <summary>
    /// 读取设备的ushort类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt16Array" title="UInt16类型示例" />
    /// </example>
    public virtual OperateResult<ushort[]> ReadUInt16(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength)), m => this.ByteTransform.TransUInt16(m, 0, length));
    }


    /// <summary>
    /// 读取设备的int类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt32" title="Int32类型示例" />
    /// </example>
    public OperateResult<int> ReadInt32(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadInt32(address, 1));
    }

    /// <summary>
    /// 读取设备的int类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt32Array" title="Int32类型示例" />
    /// </example>
    public virtual OperateResult<int[]> ReadInt32(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 2)), m => this.ByteTransform.TransInt32(m, 0, length));
    }

    /// <summary>
    /// 读取设备的uint类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt32" title="UInt32类型示例" />
    /// </example>
    public OperateResult<uint> ReadUInt32(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadUInt32(address, 1));
    }

    /// <summary>
    /// 读取设备的uint类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt32Array" title="UInt32类型示例" />
    /// </example>
    public virtual OperateResult<uint[]> ReadUInt32(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 2)), m => this.ByteTransform.TransUInt32(m, 0, length));
    }

    /// <summary>
    /// 读取设备的float类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadFloat" title="Float类型示例" />
    /// </example>
    public OperateResult<float> ReadFloat(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadFloat(address, 1));
    }


    /// <summary>
    /// 读取设备的float类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadFloatArray" title="Float类型示例" />
    /// </example>
    public virtual OperateResult<float[]> ReadFloat(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 2)), m => this.ByteTransform.TransSingle(m, 0, length));
    }

    /// <summary>
    /// 读取设备的long类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt64" title="Int64类型示例" />
    /// </example>
    public OperateResult<long> ReadInt64(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadInt64(address, 1));
    }

    /// <summary>
    /// 读取设备的long类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt64Array" title="Int64类型示例" />
    /// </example>
    public virtual OperateResult<long[]> ReadInt64(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 4)), m => this.ByteTransform.TransInt64(m, 0, length));
    }

    /// <summary>
    /// 读取设备的ulong类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt64" title="UInt64类型示例" />
    /// </example>
    public OperateResult<ulong> ReadUInt64(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadUInt64(address, 1));
    }

    /// <summary>
    /// 读取设备的ulong类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt64Array" title="UInt64类型示例" />
    /// </example>
    public virtual OperateResult<ulong[]> ReadUInt64(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 4)), m => this.ByteTransform.TransUInt64(m, 0, length));
    }

    /// <summary>
    /// 读取设备的double类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadDouble" title="Double类型示例" />
    /// </example>
    public OperateResult<double> ReadDouble(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadDouble(address, 1));
    }

    /// <summary>
    /// 读取设备的double类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadDoubleArray" title="Double类型示例" />
    /// </example>
    public virtual OperateResult<double[]> ReadDouble(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 4)), m => this.ByteTransform.TransDouble(m, 0, length));
    }

    /// <summary>
    /// 读取设备的字符串数据，编码为ASCII
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">地址长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadString" title="String类型示例" />
    /// </example>
    public OperateResult<string> ReadString(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransString(m, 0, m.Length, Encoding.ASCII));
    }

    /// <summary>
    /// 向设备中写入short数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt16Array" title="Int16类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, short[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入short数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt16" title="Int16类型示例" />
    /// </example>
    public OperateResult Write(string address, short value) {
        return this.Write(address, new short[] { value });
    }

    /// <summary>
    /// 向设备中写入ushort数组，返回是否写入成功
    /// </summary>
    /// <param name="address">要写入的数据地址</param>
    /// <param name="values">要写入的实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt16Array" title="UInt16类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, ushort[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }


    /// <summary>
    /// 向设备中写入ushort数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt16" title="UInt16类型示例" />
    /// </example>
    public OperateResult Write(string address, ushort value) {
        return this.Write(address, new ushort[] { value });
    }

    /// <summary>
    /// 向设备中写入int数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt32Array" title="Int32类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, int[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入int数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt32" title="Int32类型示例" />
    /// </example>
    public OperateResult Write(string address, int value) {
        return this.Write(address, new int[] { value });
    }

    /// <summary>
    /// 向设备中写入uint数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt32Array" title="UInt32类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, uint[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入uint数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt32" title="UInt32类型示例" />
    /// </example>
    public OperateResult Write(string address, uint value) {
        return this.Write(address, new uint[] { value });
    }

    /// <summary>
    /// 向设备中写入float数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteFloatArray" title="Float类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, float[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入float数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteFloat" title="Float类型示例" />
    /// </example>
    public OperateResult Write(string address, float value) {
        return this.Write(address, new float[] { value });
    }

    /// <summary>
    /// 向设备中写入long数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt64Array" title="Int64类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, long[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入long数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt64" title="Int64类型示例" />
    /// </example>
    public OperateResult Write(string address, long value) {
        return this.Write(address, new long[] { value });
    }

    /// <summary>
    /// 向P设备中写入ulong数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt64Array" title="UInt64类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, ulong[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入ulong数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt64" title="UInt64类型示例" />
    /// </example>
    public OperateResult Write(string address, ulong value) {
        return this.Write(address, new ulong[] { value });
    }

    /// <summary>
    /// 向设备中写入double数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteDoubleArray" title="Double类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, double[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入double数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteDouble" title="Double类型示例" />
    /// </example>
    public OperateResult Write(string address, double value) {
        return this.Write(address, new double[] { value });
    }

    /// <summary>
    /// 向设备中写入字符串，编码格式为ASCII
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteString" title="String类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, string value) {
        byte[] temp = this.ByteTransform.TransByte(value, Encoding.ASCII);
        if (this.WordLength == 1)
            temp = SoftBasic.ArrayExpandToLengthEven(temp);
        return this.Write(address, temp);
    }

    /// <summary>
    /// 向设备中写入指定长度的字符串,超出截断，不够补0，编码格式为ASCII
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="length">指定的字符串长度，必须大于0</param>
    /// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteString2" title="String类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, string value, int length) {
        byte[] temp = this.ByteTransform.TransByte(value, Encoding.ASCII);
        if (this.WordLength == 1)
            temp = SoftBasic.ArrayExpandToLengthEven(temp);
        temp = SoftBasic.ArrayExpandToLength(temp, length);
        return this.Write(address, temp);
    }

    /// <summary>
    /// 向设备中写入字符串，编码格式为Unicode
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    public virtual OperateResult WriteUnicodeString(string address, string value) {
        byte[] temp = this.ByteTransform.TransByte(value, Encoding.Unicode);
        return this.Write(address, temp);
    }

    /// <summary>
    /// 向设备中写入指定长度的字符串,超出截断，不够补0，编码格式为Unicode
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="length">指定的字符串长度，必须大于0</param>
    /// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
    public virtual OperateResult WriteUnicodeString(string address, string value, int length) {
        byte[] temp = this.ByteTransform.TransByte(value, Encoding.Unicode);
        temp = SoftBasic.ArrayExpandToLength(temp, length * 2);
        return this.Write(address, temp);
    }

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    /// <param name="disposing">是否托管对象</param>
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.lock_trusted_clients?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串数据</returns>
    public override string ToString() {
        return "NetworkDataServerBase";
    }
}