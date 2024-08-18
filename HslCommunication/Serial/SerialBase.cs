using System.Diagnostics;
using System.IO.Ports;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Types;

namespace HslCommunication.Serial;

/// <summary>
/// The base class for all types of serial devices. There may be a similarly named class for a PLC that can communicate via serial, ethernet, etc.
/// </summary>
public class SerialBase : IDisposable {
    public readonly SerialPort serialPort;
    private readonly byte[] rxBuffer;
    private readonly SimpleHybirdLock hybirdLock; // Guard against multiple threads sending and receiving data at the same time
    private readonly Stopwatch readTimer = new Stopwatch();
    private int receiveTimeout = 5000;
    private int sleepTime;
    private bool disposedValue;

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
    public bool ClearReadBufferBeforeSend { get; set; }

    /// <summary>
    /// Gets the current port name
    /// </summary>
    public string? PortName { get; private set; }

    /// <summary>
    /// Gets the current baud rate
    /// </summary>
    public int BaudRate { get; private set; }
    
    /// <summary>
    /// 获取一个值，指示串口是否处于打开状态
    /// </summary>
    /// <value>是或否</value>
    public bool IsOpen => this.serialPort.IsOpen;

    /// <summary>
    /// 实例化一个无参的构造方法
    /// </summary>
    public SerialBase() : this(128) {
    }

    public SerialBase(int readBufferSize) {
        this.serialPort = new SerialPort();
        this.hybirdLock = new SimpleHybirdLock();
        this.rxBuffer = new byte[readBufferSize];
    }

    /// <summary>
    /// Initialise serial port variables
    /// </summary>
    public void SetupSerial(string portName, int baudRate = 38400, int dataBits = 8, StopBits stopBits = StopBits.One, Parity parity = Parity.None) {
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

    public void SetupSerial(Action<SerialPort> initi) {
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
    public LightOperationResult Open() {
        if (!this.serialPort.IsOpen) {
            this.serialPort.ReadTimeout = this.ReceiveTimeout;
            this.serialPort.WriteTimeout = 5000;
            this.serialPort.Open();

            OperateResult init = this.InitializationOnOpen();
            if (!init.IsSuccess) {
                this.serialPort.Close();
            }

            return init.IsSuccess ? new LightOperationResult() : new LightOperationResult(init.ErrorCode, init.Message);
        }

        return new LightOperationResult();
    }

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
    /// Clears the serial port's receive buffer
    /// </summary>
    /// <returns>是否操作成功的方法</returns>
    public LightOperationResult ClearSerialCache() {
        if (this.serialPort.IsOpen) {
            try {
                this.serialPort.DiscardInBuffer();
            }
            catch (Exception e) {
                return new LightOperationResult(e.Message);
            }
        }

        return LightOperationResult.CreateSuccessResult();
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
    /// Checks if the received bytes make up a valid message from the PLC. Do not use the
    /// receive array's Length property but instead use receivedCount, because the receive
    /// array will almost always be larger than what has actually been processed
    /// </summary>
    /// <param name="buffer">An array that contains the bytes that have been received back so far</param>
    /// <param name="count">The number of bytes received so far. This may be less than the buffer's Length, so use this if iterating the buffer</param>
    /// <returns>True if the message is complete and the received array can be processed, otherwise, keep waiting for data to be received</returns>
    protected virtual bool IsReceivedMessageComplete(byte[] buffer, int count) {
        return true;
    }

    /// <summary>
    /// Sends the given array of bytes (if non-null) and then reads a response
    /// </summary>
    /// <param name="send">The raw bytes to send</param>
    /// <returns>An operation result containing the response, if data was sent and a response was received in time</returns>
    public LightOperationResult<byte[]> SendMessageAndGetResponce(byte[] send) {
        if (send == null! || send.Length < 1) {
            return LightOperationResult.CreateSuccessResult(Array.Empty<byte>());
        }

        this.hybirdLock.Enter();

        if (this.ClearReadBufferBeforeSend)
            this.ClearSerialCache();

        try {
            this.serialPort.Write(send, 0, send.Length);
        }
        catch (Exception ex) {
            this.hybirdLock.Leave();
            return new LightOperationResult<byte[]>(ex.Message);
        }

        LightOperationResult<byte[]> receiveResult = this.ReadMessageInternal();
        this.hybirdLock.Leave();

        return receiveResult;
    }

    private static void FastArrayCopy(byte[] src, byte[] dst, int dstoffset, int count) {
        if (count > 512) // is 512 a good value for this?
            Buffer.BlockCopy(src, 0, dst, dstoffset, count);
        else
            for (int i = 0; i < count; i++)
                dst[i + dstoffset] = src[i];
    }

    private static void AppendBuffer(ref byte[] dst, ref int dstCount, byte[] src, int count) {
        // sinkCount: 4
        // sink.Length: 8
        // count: 6
        // Debug.Assert(src.Length >= count, "Source buffer length is too small to copy count bytes from");

        if (count > (dst.Length - dstCount)) {
            int newCount = dst.Length;
            do {
                newCount <<= 1;
            } while (newCount < (dstCount + count));
            
            byte[] newSink = new byte[newCount];
            FastArrayCopy(dst, newSink, 0, dstCount);
            dst = newSink;
        }

        FastArrayCopy(src, dst, dstCount, count);
        dstCount += count;
    }

    private static byte[] Subarray(byte[] src, int count) {
        if (src.Length == count)
            return src;
        if (count > src.Length)
            throw new InvalidOperationException("Cannot sub-array more than the length of the array");

        byte[] output = new byte[count];
        FastArrayCopy(src, output, 0, count);
        return output;
    }

    private LightOperationResult<byte[]> ReadMessageInternal() {
        if (!this.serialPort.IsOpen)
            return new LightOperationResult<byte[]>("Serial port is closed");


        byte[] buffer = new byte[32];
        int bufferCount = 0;

        this.readTimer.Restart();
        for (int iterations = 1;; iterations++) {
            // This function appears to be ever slightly faster when sleeping
            // on the 2nd iteration at the start instead of at the end of the 2nd iteration
            try {
                // We rely on the SerialPort timeout values to block until we receive data
                int received = this.serialPort.BaseStream.Read(this.rxBuffer, 0, this.rxBuffer.Length);
                if (received > 0) {
                    AppendBuffer(ref buffer, ref bufferCount, this.rxBuffer, received);
                    if (this.IsReceivedMessageComplete(buffer, bufferCount)) {
#if DEBUG
                        double millis = this.readTimer.ElapsedMilliseconds;
#endif
                        return LightOperationResult.CreateSuccessResult(Subarray(buffer, bufferCount));
                    }
                }

                if (iterations != 1 && this.ReceiveTimeout > 0 && this.readTimer.ElapsedMilliseconds > this.ReceiveTimeout)
                    return new LightOperationResult<byte[]>($"Time out while waiting for completed message: {this.ReceiveTimeout}");
            }
            catch (Exception ex) {
                return new LightOperationResult<byte[]>(ex.Message);
            }
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