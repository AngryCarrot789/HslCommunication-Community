using System.IO.Ports;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Types;
using HslCommunication.LogNet.Core;

namespace HslCommunication.Serial;

/// <summary>
/// 所有串行通信类的基类，提供了一些基础的服务
/// </summary>
public class SerialBase : IDisposable {
    private readonly SerialPort serialPort;
    private SimpleHybirdLock hybirdLock; // Guard against multiple threads sending and receiving data at the same time
    private ILogNet? logNet;
    private int receiveTimeout = 5000;
    private int sleepTime;
    private bool clearReadBufferBeforeSend;
    private bool disposedValue;
    
    /// <summary>
    /// 当前的日志情况
    /// </summary>
    public ILogNet? LogNet {
        get { return this.logNet; }
        set { this.logNet = value; }
    }

    /// <summary>
    /// The maximum amount of time to wait for a message to be received from the serial port, in milliseconds. Default value of 5000.
    /// Setting this value to 0 results in an infinite timeout for receiving messages, and could result in the application freezing if the
    /// PLC disconnects and then reconnects but then never sends a response message.
    /// </summary>
    public int ReceiveTimeout {
        get => this.receiveTimeout;
        set => this.receiveTimeout = Math.Max(value, 0);
    }

    /// <summary>
    /// The amount of time to sleep before attempting to read a message from the PLC, in milliseconds. Defaults to 0,
    /// meaning no sleeping. Beware of the fact that <see cref="Thread.Sleep(int)"/> is used, therefore, on platforms
    /// such as windows, the actual sleep duration tends to be a minimum of 16ms which is around about the interval between
    /// thread time-slices during normal operating system levels (may increase when new threads spawn with much higher priority) 
    /// </summary>
    public int SleepTime {
        get => this.sleepTime;
        set => this.sleepTime = Math.Max(value, 0);
    }

    /// <summary>
    /// Gets or sets whether to clear the read buffer before sending data
    /// </summary>
    public bool ClearReadBufferBeforeSend {
        get => this.clearReadBufferBeforeSend;
        set => this.clearReadBufferBeforeSend = value;
    }

    /// <summary>
    /// Gets the current port name
    /// </summary>
    public string PortName { get; private set; }

    /// <summary>
    /// Gets the current baud rate
    /// </summary>
    public int BaudRate { get; private set; }

    /// <summary>
    /// 实例化一个无参的构造方法
    /// </summary>
    public SerialBase() {
        this.serialPort = new SerialPort();
        this.hybirdLock = new SimpleHybirdLock();
    }

    /// <summary>
    /// Initialise serial port variable
    /// </summary>
    /// <param name="portName">端口号信息，例如"COM3"</param>
    /// <param name="baudRate">波特率</param>
    /// <param name="dataBits">数据位</param>
    /// <param name="stopBits">停止位</param>
    /// <param name="parity">奇偶校验</param>
    public void SerialPortInni(string portName, int baudRate = 38400, int dataBits = 8, StopBits stopBits = StopBits.One, Parity parity = Parity.None) {
        if (this.serialPort.IsOpen) {
            throw new InvalidOperationException("Cannot initialise serial port variables because it is currently open");
        }

        this.serialPort.PortName = portName; // 串口
        this.serialPort.BaudRate = baudRate; // 波特率
        this.serialPort.DataBits = dataBits; // 数据位
        this.serialPort.StopBits = stopBits; // 停止位
        this.serialPort.Parity = parity; // 奇偶校验
        this.PortName = this.serialPort.PortName;
        this.BaudRate = this.serialPort.BaudRate;
    }

    /// <summary>
    /// 根据自定义初始化方法进行初始化串口信息
    /// </summary>
    /// <param name="initi">初始化的委托方法</param>
    public void SerialPortInni(Action<SerialPort> initi) {
        if (this.serialPort.IsOpen) {
            throw new InvalidOperationException("Cannot initialise serial port variables because it is currently open");
        }

        this.serialPort.PortName = "COM5";
        this.serialPort.BaudRate = 9600;
        this.serialPort.DataBits = 8;
        this.serialPort.StopBits = StopBits.One;
        this.serialPort.Parity = Parity.None;

        initi.Invoke(this.serialPort);

        this.PortName = this.serialPort.PortName;
        this.BaudRate = this.serialPort.BaudRate;
    }

    /// <summary>
    /// 打开一个新的串行端口连接
    /// </summary>
    public void Open() {
        if (!this.serialPort.IsOpen) {
            this.serialPort.Open();
            this.InitializationOnOpen();
        }
    }

    /// <summary>
    /// 获取一个值，指示串口是否处于打开状态
    /// </summary>
    /// <returns>是或否</returns>
    public bool IsOpen() => this.serialPort.IsOpen;

    /// <summary>
    /// 关闭端口连接
    /// </summary>
    public void Close() {
        if (this.serialPort.IsOpen) {
            this.ExtraOnClose();
            this.serialPort.Close();
        }
    }

    /// <summary>
    /// 清除串口缓冲区的数据，并返回该数据，如果缓冲区没有数据，返回的字节数组长度为0
    /// </summary>
    /// <returns>是否操作成功的方法</returns>
    public LightOperationResult<byte[]> ClearSerialCache() {
        return this.ReadSerialData(false);
    }

    /// <summary>
    /// 在打开端口时的初始化方法，按照协议的需求进行必要的重写
    /// </summary>
    /// <returns>是否初始化成功</returns>
    protected virtual OperateResult InitializationOnOpen() {
        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 在将要和服务器进行断开的情况下额外的操作，需要根据对应协议进行重写
    /// </summary>
    /// <returns>当断开连接时额外的操作结果</returns>
    protected virtual OperateResult ExtraOnClose() {
        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// Returns true when this device uses a protocol when sending data to and from devices, and the protocol can
    /// be identified in the received data. This is required for <see cref="IsReceivedMessageComplete"/> to be called
    /// </summary>
    /// <returns>True or false</returns>
    protected virtual bool HasProtocol() {
        return true;
    }

    /// <summary>
    /// Checks if the received bytes account to a valid message from the PLC.
    /// Do not use the receive array's Length property but instead use receivedCount,
    /// because the receive array will almost always be larger than what has actually been processed
    /// </summary>
    /// <param name="received">An array containing the bytes that have been received back so far</param>
    /// <param name="receivedCount">The number of bytes received so far. This may differ from the length of the received array's Length</param>
    /// <returns>True if the message is complete and the received array can be processed, otherwise, keep waiting for data to be received</returns>
    protected virtual bool IsReceivedMessageComplete(byte[] received, int receivedCount) {
        return true;
    }
    
    /// <summary>
    /// Sends the given array of bytes (if non-null) and then reads a response
    /// </summary>
    /// <param name="send">发送的原始字节数据</param>
    /// <returns>带接收字节的结果对象</returns>
    public OperateResult<byte[]> SendMessageAndGetResponce(byte[] send) {
        this.hybirdLock.Enter();

        if (this.ClearReadBufferBeforeSend)
            this.ClearSerialCache();

        LightOperationResult sendResult = this.WriteSerialData(send);
        if (!sendResult.IsSuccess) {
            this.hybirdLock.Leave();
            return sendResult.ToFailedResult<byte[]>();
        }

        LightOperationResult<byte[]> receiveResult = this.ReadSerialData(true);
        this.hybirdLock.Leave();

        return receiveResult.ToOperateResult();
    }

    protected virtual LightOperationResult<byte[]> ReadSerialData(bool waitForData) {
        byte[] buffer;
        try {
            buffer = new byte[64];
        }
        catch (Exception ex) {
            return new LightOperationResult<byte[]>(ex.Message);
        }

        using MemoryStream ms = new MemoryStream(64);
        
        DateTime now = DateTime.Now;
        int iterations = 0;

        LoopStart:
        
        if (++iterations > 1 && this.sleepTime >= 0)
            Thread.Sleep(this.sleepTime);

        try {
            if (this.serialPort.BytesToRead > 0) {
                int received = this.serialPort.Read(buffer, 0, buffer.Length);
                if (received > 0)
                    ms.Write(buffer, 0, received);

                if (this.HasProtocol() && this.IsReceivedMessageComplete(ms.GetBuffer(), (int) ms.Length))
                    goto SuccessResult;

                if (this.ReceiveTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > this.ReceiveTimeout)
                    goto TimeoutResult;

                goto LoopStart;
            }
            else if (iterations != 1) {
                if ((DateTime.Now - now).TotalMilliseconds > this.ReceiveTimeout)
                    goto TimeoutResult;

                if (ms.Length > 0 || waitForData)
                    goto LoopStart;
            }
            else {
                goto LoopStart;
            }
        }
        catch (Exception ex) {
            return new LightOperationResult<byte[]>(ex.Message);
        }

        SuccessResult:
        return LightOperationResult.CreateSuccessResult(ms.ToArray());
        
        TimeoutResult:
        return new LightOperationResult<byte[]>($"Time out: {this.ReceiveTimeout}");
    }
    
    /// <summary>
    /// Writes serial data to our serial port
    /// </summary>
    /// <param name="data">字节数据</param>
    /// <returns>是否发送成功</returns>
    protected virtual LightOperationResult WriteSerialData(byte[] data) {
        if (data == null! || data.Length == 0) {
            return LightOperationResult.CreateSuccessResult();
        }

        try {
            this.serialPort.Write(data, 0, data.Length);
            return LightOperationResult.CreateSuccessResult();
        }
        catch (Exception ex) {
            return new LightOperationResult(ex.Message);
        }
    }

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串</returns>
    public override string ToString() {
        return "SerialBase";
    }

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    /// <param name="disposing">是否在</param>
    protected virtual void Dispose(bool disposing) {
        if (!this.disposedValue) {
            if (disposing) {
                this.hybirdLock?.Dispose();
                this.serialPort?.Dispose();
            }

            this.disposedValue = true;
        }
    }

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    public void Dispose() => this.Dispose(true);
}