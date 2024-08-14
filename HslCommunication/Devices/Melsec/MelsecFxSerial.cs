using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;
using HslCommunication.Serial;

namespace HslCommunication.Devices.Melsec;

/// <summary>
/// 三菱的串口通信的对象，适用于读取FX系列的串口数据，支持的类型参考文档说明
/// </summary>
/// <remarks>
/// 字读写地址支持的列表如下：
/// <list type="table">
///   <listheader>
///     <term>地址名称</term>
///     <term>地址代号</term>
///     <term>示例</term>
///     <term>地址范围</term>
///     <term>地址进制</term>
///     <term>备注</term>
///   </listheader>
///   <item>
///     <term>数据寄存器</term>
///     <term>D</term>
///     <term>D100,D200</term>
///     <term>D0-D511,D8000-D8255</term>
///     <term>10</term>
///     <term></term>
///   </item>
///   <item>
///     <term>定时器的值</term>
///     <term>TN</term>
///     <term>TN10,TN20</term>
///     <term>TN0-TN255</term>
///     <term>10</term>
///     <term></term>
///   </item>
///   <item>
///     <term>计数器的值</term>
///     <term>CN</term>
///     <term>CN10,CN20</term>
///     <term>CN0-CN199,CN200-CN255</term>
///     <term>10</term>
///     <term></term>
///   </item>
/// </list>
/// 位地址支持的列表如下：
/// <list type="table">
///   <listheader>
///     <term>地址名称</term>
///     <term>地址代号</term>
///     <term>示例</term>
///     <term>地址范围</term>
///     <term>地址进制</term>
///     <term>备注</term>
///   </listheader>
///   <item>
///     <term>内部继电器</term>
///     <term>M</term>
///     <term>M100,M200</term>
///     <term>M0-M1023,M8000-M8255</term>
///     <term>10</term>
///     <term></term>
///   </item>
///   <item>
///     <term>输入继电器</term>
///     <term>X</term>
///     <term>X1,X20</term>
///     <term>X0-X177</term>
///     <term>8</term>
///     <term></term>
///   </item>
///   <item>
///     <term>输出继电器</term>
///     <term>Y</term>
///     <term>Y10,Y20</term>
///     <term>Y0-Y177</term>
///     <term>8</term>
///     <term></term>
///   </item>
///   <item>
///     <term>步进继电器</term>
///     <term>S</term>
///     <term>S100,S200</term>
///     <term>S0-S999</term>
///     <term>10</term>
///     <term></term>
///   </item>
///   <item>
///     <term>定时器触点</term>
///     <term>TS</term>
///     <term>TS10,TS20</term>
///     <term>TS0-TS255</term>
///     <term>10</term>
///     <term></term>
///   </item>
///   <item>
///     <term>定时器线圈</term>
///     <term>TC</term>
///     <term>TC10,TC20</term>
///     <term>TC0-TC255</term>
///     <term>10</term>
///     <term></term>
///   </item>
///   <item>
///     <term>计数器触点</term>
///     <term>CS</term>
///     <term>CS10,CS20</term>
///     <term>CS0-CS255</term>
///     <term>10</term>
///     <term></term>
///   </item>
///   <item>
///     <term>计数器线圈</term>
///     <term>CC</term>
///     <term>CC10,CC20</term>
///     <term>CC0-CC255</term>
///     <term>10</term>
///     <term></term>
///   </item>
/// </list>
/// </remarks>
/// <example>
/// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Devices\MelsecFxSerial.cs" region="Usage" title="简单的使用" />
/// </example>
public class MelsecFxSerial : SerialDeviceBase<RegularByteTransform> {
    /// <summary>
    /// 实例化三菱的串口协议的通讯对象
    /// </summary>
    public MelsecFxSerial() {
        this.WordLength = 1;
    }

    /*
     * Control Codes:
         STX 0x2 Start of Text        LF  0xA Line Feed
         ETX 0x3 End of Text          CL  0xC Clear
         EOT 0x4 End of Transmission  CR  0xD Carriage Return
         ENQ 0x5 Enquiry              NAK 0x15 Negative Acknowledge
         ACK 0x6 Acknowledge
     */
    
    protected override bool IsReceivedMessageComplete(byte[] buffer, int count) {
        switch (count) {
            case 0: return false;
            case 1:
                // Check response is ACK or NAK
                return buffer[0] == 0x6 || buffer[0] == 0x15;
            default:
                // Check for STX, check buffer is big enough has contains ETX,
                // then verify checksum
                return buffer[0] == 0x2 && count >= 5 && buffer[count - 3] == 0x3 && MelsecHelper.CheckCRC(buffer, count);
        }
    }

    private LightOperationResult CheckPlcReadResponse(byte[] ack) {
        if (ack.Length == 0)
            return new LightOperationResult(StringResources.Language.MelsecFxReceiveZore);
        if (ack[0] == 0x15) // Received NAK
            return new LightOperationResult(StringResources.Language.MelsecFxAckNagative + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
        if (ack[0] != 0x02) // 
            return new LightOperationResult(StringResources.Language.MelsecFxAckWrong + ack[0] + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
        if (!MelsecHelper.CheckCRC(ack))
            return new LightOperationResult(StringResources.Language.MelsecFxCrcCheckFailed);

        return LightOperationResult.CreateSuccessResult();
    }

    private LightOperationResult CheckPlcWriteResponse(byte[] ack) {
        if (ack.Length == 0)
            return new LightOperationResult(StringResources.Language.MelsecFxReceiveZore);
        if (ack[0] == 0x15)
            return new LightOperationResult(StringResources.Language.MelsecFxAckNagative + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
        if (ack[0] != 0x06)
            return new LightOperationResult(StringResources.Language.MelsecFxAckWrong + ack[0] + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));

        return LightOperationResult.CreateSuccessResult();
    }

    /// <summary>
    /// 从三菱PLC中读取想要的数据，返回读取结果
    /// </summary>
    /// <param name="address">读取地址，，支持的类型参考文档说明</param>
    /// <param name="length">读取的数据长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 假设起始地址为D100，D100存储了温度，100.6℃值为1006，D101存储了压力，1.23Mpa值为123，D102，D103存储了产量计数，读取如下：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Devices\MelsecFxSerial.cs" region="ReadExample2" title="Read示例" />
    /// 以下是读取不同类型数据的示例
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Devices\MelsecFxSerial.cs" region="ReadExample1" title="Read示例" />
    /// </example>
    public override OperateResult<byte[]> Read(string address, ushort length) {
        // 获取指令
        LightOperationResult<byte[]> command = BuildReadWordCommand(address, length);
        if (!command.IsSuccess)
            return new OperateResult<byte[]>(command.ErrorCode, command.Message);

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 反馈检查
        LightOperationResult ackResult = this.CheckPlcReadResponse(read.Content);
        if (!ackResult.IsSuccess)
            return new OperateResult<byte[]>(ackResult.ErrorCode, ackResult.Message);

        // 数据提炼
        return ExtractActualData(read.Content).ToOperateResult();
    }


    /// <summary>
    /// 从三菱PLC中批量读取位软元件，返回读取结果，该读取地址最好从0，16，32...等开始读取，这样可以读取比较长得数据数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">读取的长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    ///  <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Devices\MelsecFxSerial.cs" region="ReadBool" title="Bool类型示例" />
    /// </example>
    public override OperateResult<bool[]> ReadBool(string address, ushort length) {
        LightOperationResult<byte[], int> command = BuildReadBoolCommand(address, length);
        if (!command.IsSuccess)
            return new OperateResult<bool[]>(command.ErrorCode, command.Message);

        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content1);
        if (!read.IsSuccess)
            return new OperateResult<bool[]>(read.ErrorCode, read.Message);

        LightOperationResult ackResult = this.CheckPlcReadResponse(read.Content);
        if (!ackResult.IsSuccess)
            return new OperateResult<bool[]>(ackResult.ErrorCode, ackResult.Message);

        // 提取真实的数据
        return ExtractActualBoolData(read.Content, command.Content2, length).ToOperateResult();
    }
    
    public virtual LightOperationResult<bool[]> ReadBoolLight(string address, ushort length) {
        LightOperationResult<byte[], int> command = BuildReadBoolCommand(address, length);
        if (!command.IsSuccess)
            return new LightOperationResult<bool[]>(command.ErrorCode, command.Message);

        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content1);
        if (!read.IsSuccess)
            return new LightOperationResult<bool[]>(read.ErrorCode, read.Message);

        LightOperationResult ackResult = this.CheckPlcReadResponse(read.Content);
        if (!ackResult.IsSuccess)
            return new LightOperationResult<bool[]>(ackResult.ErrorCode, ackResult.Message);

        // 提取真实的数据
        return ExtractActualBoolData(read.Content, command.Content2, length);
    }

    /// <summary>
    /// 向PLC写入数据，数据格式为原始的字节类型
    /// </summary>
    /// <param name="address">初始地址，支持的类型参考文档说明</param>
    /// <param name="value">原始的字节数据</param>
    /// <example>
    /// 假设起始地址为D100，D100存储了温度，100.6℃值为1006，D101存储了压力，1.23Mpa值为123，D102，D103存储了产量计数，写入如下：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Devices\MelsecFxSerial.cs" region="WriteExample2" title="Write示例" />
    /// 以下是读取不同类型数据的示例
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Devices\MelsecFxSerial.cs" region="WriteExample1" title="Write示例" />
    /// </example>
    /// <returns>是否写入成功的结果对象</returns>
    public override OperateResult Write(string address, byte[] value) {
        // 获取写入
        LightOperationResult<byte[]> command = BuildWriteWordCommand(address, value);
        if (!command.IsSuccess)
            return command.ToOperateResult();

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 结果验证
        LightOperationResult checkResult = this.CheckPlcWriteResponse(read.Content);
        if (!checkResult.IsSuccess)
            return checkResult.ToOperateResult();

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 强制写入位数据的通断，支持的类型参考文档说明
    /// </summary>
    /// <param name="address">地址信息</param>
    /// <param name="value">是否为通</param>
    /// <returns>是否写入成功的结果对象</returns>
    public override OperateResult Write(string address, bool value) {
        // 先获取指令
        LightOperationResult<byte[]> command = BuildWriteBoolPacket(address, value);
        if (!command.IsSuccess)
            return command.ToOperateResult();

        // 和串口进行核心的数据交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 检查结果是否正确
        LightOperationResult checkResult = this.CheckPlcWriteResponse(read.Content);
        if (!checkResult.IsSuccess)
            return checkResult.ToOperateResult();

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 获取当前对象的字符串标识形式
    /// </summary>
    /// <returns>字符串信息</returns>
    public override string ToString() {
        return "MelsecFxSerial";
    }

    /// <summary>
    /// 生成位写入的数据报文信息，该报文可直接用于发送串口给PLC
    /// </summary>
    /// <param name="address">地址信息，每个地址存在一定的范围，需要谨慎传入数据。举例：M10,S10,X5,Y10,C10,T10</param>
    /// <param name="value"><c>True</c>或是<c>False</c></param>
    /// <returns>带报文信息的结果对象</returns>
    public static LightOperationResult<byte[]> BuildWriteBoolPacket(string address, bool value) {
        LightOperationResult<MelsecMcDataType, ushort> analysis = FxAnalysisAddress(address);
        if (!analysis.IsSuccess)
            return new LightOperationResult<byte[]>(analysis.ErrorCode, analysis.Message);

        // 二次运算起始地址偏移量，根据类型的不同，地址的计算方式不同
        ushort startAddress = analysis.Content2;
        if (analysis.Content1 == MelsecMcDataType.M) {
            if (startAddress >= 8000) {
                startAddress = (ushort) (startAddress - 8000 + 0x0F00);
            }
            else {
                startAddress = (ushort) (startAddress + 0x0800);
            }
        }
        else if (analysis.Content1 == MelsecMcDataType.S) {
            startAddress = (ushort) (startAddress + 0x0000);
        }
        else if (analysis.Content1 == MelsecMcDataType.X) {
            startAddress = (ushort) (startAddress + 0x0400);
        }
        else if (analysis.Content1 == MelsecMcDataType.Y) {
            startAddress = (ushort) (startAddress + 0x0500);
        }
        else if (analysis.Content1 == MelsecMcDataType.CS) {
            startAddress += (ushort) (startAddress + 0x01C0);
        }
        else if (analysis.Content1 == MelsecMcDataType.CC) {
            startAddress += (ushort) (startAddress + 0x03C0);
        }
        else if (analysis.Content1 == MelsecMcDataType.CN) {
            startAddress += (ushort) (startAddress + 0x0E00);
        }
        else if (analysis.Content1 == MelsecMcDataType.TS) {
            startAddress += (ushort) (startAddress + 0x00C0);
        }
        else if (analysis.Content1 == MelsecMcDataType.TC) {
            startAddress += (ushort) (startAddress + 0x02C0);
        }
        else if (analysis.Content1 == MelsecMcDataType.TN) {
            startAddress += (ushort) (startAddress + 0x0600);
        }
        else {
            return new LightOperationResult<byte[]>(StringResources.Language.MelsecCurrentTypeNotSupportedBitOperate);
        }

        byte[] _PLCCommand = new byte[9];
        _PLCCommand[0] = 0x02; // STX
        _PLCCommand[1] = value ? (byte) 0x37 : (byte) 0x38; // Read
        _PLCCommand[2] = SoftBasic.BuildAsciiBytesFrom(startAddress)[2]; // 偏移地址
        _PLCCommand[3] = SoftBasic.BuildAsciiBytesFrom(startAddress)[3];
        _PLCCommand[4] = SoftBasic.BuildAsciiBytesFrom(startAddress)[0];
        _PLCCommand[5] = SoftBasic.BuildAsciiBytesFrom(startAddress)[1];
        _PLCCommand[6] = 0x03; // ETX
        MelsecHelper.FxCalculateCRC(_PLCCommand).CopyTo(_PLCCommand, 7); // CRC

        return LightOperationResult.CreateSuccessResult(_PLCCommand);
    }

    /// <summary>
    /// 根据类型地址长度确认需要读取的指令头
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">长度</param>
    /// <returns>带有成功标志的指令数据</returns>
    public static LightOperationResult<byte[]> BuildReadWordCommand(string address, ushort length) {
        LightOperationResult<ushort> addressResult = FxCalculateWordStartAddress(address);
        if (!addressResult.IsSuccess)
            return new LightOperationResult<byte[]>(addressResult.ErrorCode, addressResult.Message);

        length = (ushort) (length * 2);
        ushort startAddress = addressResult.Content;

        byte[] _PLCCommand = new byte[11];
        _PLCCommand[0] = 0x02; // STX
        _PLCCommand[1] = 0x30; // Read
        _PLCCommand[2] = SoftBasic.BuildAsciiBytesFrom(startAddress)[0]; // 偏移地址
        _PLCCommand[3] = SoftBasic.BuildAsciiBytesFrom(startAddress)[1];
        _PLCCommand[4] = SoftBasic.BuildAsciiBytesFrom(startAddress)[2];
        _PLCCommand[5] = SoftBasic.BuildAsciiBytesFrom(startAddress)[3];
        _PLCCommand[6] = SoftBasic.BuildAsciiBytesFrom((byte) length)[0]; // 读取长度
        _PLCCommand[7] = SoftBasic.BuildAsciiBytesFrom((byte) length)[1];
        _PLCCommand[8] = 0x03; // ETX
        MelsecHelper.FxCalculateCRC(_PLCCommand).CopyTo(_PLCCommand, 9); // CRC

        return LightOperationResult.CreateSuccessResult(_PLCCommand); // Return
    }

    /// <summary>
    /// 根据类型地址长度确认需要读取的指令头
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">bool数组长度</param>
    /// <returns>带有成功标志的指令数据</returns>
    public static LightOperationResult<byte[], int> BuildReadBoolCommand(string address, ushort length) {
        LightOperationResult<ushort, ushort, ushort> addressResult = FxCalculateBoolStartAddress(address);
        if (!addressResult.IsSuccess)
            return new LightOperationResult<byte[], int>(addressResult.ErrorCode, addressResult.Message);

        // 计算下实际需要读取的数据长度
        ushort length2 = (ushort) ((addressResult.Content2 + length - 1) / 8 - (addressResult.Content2 / 8) + 1);

        ushort startAddress = addressResult.Content1;
        byte[] _PLCCommand = new byte[11];
        _PLCCommand[0] = 0x02; // STX
        _PLCCommand[1] = 0x30; // Read
        _PLCCommand[2] = SoftBasic.BuildAsciiBytesFrom(startAddress)[0]; // 偏移地址
        _PLCCommand[3] = SoftBasic.BuildAsciiBytesFrom(startAddress)[1];
        _PLCCommand[4] = SoftBasic.BuildAsciiBytesFrom(startAddress)[2];
        _PLCCommand[5] = SoftBasic.BuildAsciiBytesFrom(startAddress)[3];
        _PLCCommand[6] = SoftBasic.BuildAsciiBytesFrom((byte) length2)[0]; // 读取长度
        _PLCCommand[7] = SoftBasic.BuildAsciiBytesFrom((byte) length2)[1];
        _PLCCommand[8] = 0x03; // ETX
        MelsecHelper.FxCalculateCRC(_PLCCommand).CopyTo(_PLCCommand, 9); // CRC

        return LightOperationResult.CreateSuccessResult(_PLCCommand, (int) addressResult.Content3);
    }

    /// <summary>
    /// 根据类型地址以及需要写入的数据来生成指令头
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="value">实际的数据信息</param>
    /// <returns>带有成功标志的指令数据</returns>
    public static LightOperationResult<byte[]> BuildWriteWordCommand(string address, byte[] value) {
        LightOperationResult<ushort> addressResult = FxCalculateWordStartAddress(address);
        if (!addressResult.IsSuccess)
            return new LightOperationResult<byte[]>(addressResult.ErrorCode, addressResult.Message);

        // 字节数据转换成ASCII格式
        if (value != null)
            value = SoftBasic.BuildAsciiBytesFrom(value);

        ushort startAddress = addressResult.Content;
        byte[] _PLCCommand = new byte[11 + value.Length];
        _PLCCommand[0] = 0x02; // STX
        _PLCCommand[1] = 0x31; // Read
        _PLCCommand[2] = SoftBasic.BuildAsciiBytesFrom(startAddress)[0]; // Offect Address
        _PLCCommand[3] = SoftBasic.BuildAsciiBytesFrom(startAddress)[1];
        _PLCCommand[4] = SoftBasic.BuildAsciiBytesFrom(startAddress)[2];
        _PLCCommand[5] = SoftBasic.BuildAsciiBytesFrom(startAddress)[3];
        _PLCCommand[6] = SoftBasic.BuildAsciiBytesFrom((byte) (value.Length / 2))[0]; // Read Length
        _PLCCommand[7] = SoftBasic.BuildAsciiBytesFrom((byte) (value.Length / 2))[1];
        Array.Copy(value, 0, _PLCCommand, 8, value.Length);
        _PLCCommand[_PLCCommand.Length - 3] = 0x03; // ETX
        MelsecHelper.FxCalculateCRC(_PLCCommand).CopyTo(_PLCCommand, _PLCCommand.Length - 2); // CRC

        return LightOperationResult.CreateSuccessResult(_PLCCommand);
    }


    /// <summary>
    /// 从PLC反馈的数据进行提炼操作
    /// </summary>
    /// <param name="response">PLC反馈的真实数据</param>
    /// <returns>数据提炼后的真实数据</returns>
    public static LightOperationResult<byte[]> ExtractActualData(byte[] response) {
        try {
            byte[] data = new byte[(response.Length - 4) / 2];
            for (int i = 0; i < data.Length; i++) {
                byte[] buffer = new byte[2];
                buffer[0] = response[i * 2 + 1];
                buffer[1] = response[i * 2 + 2];

                data[i] = Convert.ToByte(Encoding.ASCII.GetString(buffer), 16);
            }

            return LightOperationResult.CreateSuccessResult(data);
        }
        catch (Exception ex) {
            return new LightOperationResult<byte[]>("Extract Msg：" + ex.Message + Environment.NewLine + "Data: " + SoftBasic.ByteToHexString(response));
        }
    }


    /// <summary>
    /// 从PLC反馈的数据进行提炼bool数组操作
    /// </summary>
    /// <param name="response">PLC反馈的真实数据</param>
    /// <param name="start">起始提取的点信息</param>
    /// <param name="length">bool数组的长度</param>
    /// <returns>数据提炼后的真实数据</returns>
    public static LightOperationResult<bool[]> ExtractActualBoolData(byte[] response, int start, int length) {
        LightOperationResult<byte[]> extraResult = ExtractActualData(response);
        if (!extraResult.IsSuccess)
            return new LightOperationResult<bool[]>(extraResult.ErrorCode, extraResult.Message);

        try {
            // DateTime startTime = DateTime.Now;
            bool[] data = new bool[length];
            bool[] array = SoftBasic.ByteToBoolArray(extraResult.Content, extraResult.Content.Length * 8);
            
            // Although I profiled this in debug mode, manual array copy is
            // still faster. BlockCopy is a bit slower, and CopyBlockUnaligned
            // is also slower than BlockCopy
            
            for (int i = 0; i < length; i++) {
                data[i] = array[i + start];
            }
            
            // Buffer.BlockCopy(array, start, data, 0, length);
            // Unsafe.CopyBlockUnaligned(ref Unsafe.As<bool, byte>(ref data[0]), ref Unsafe.As<bool, byte>(ref array[start]), (uint) length);
            // double millisTaken = (DateTime.Now - startTime).TotalMilliseconds;

            return LightOperationResult.CreateSuccessResult(data);
        }
        catch (Exception ex) {
            return new LightOperationResult<bool[]>("Extract Msg：" + ex.Message + Environment.NewLine + "Data: " + SoftBasic.ByteToHexString(response));
        }
    }

    /// <summary>
    /// Parse data addresses into different Mitsubishi address types
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <returns>地址结果对象</returns>
    public static LightOperationResult<MelsecMcDataType, ushort> FxAnalysisAddress(string address) {
        LightOperationResult<MelsecMcDataType, ushort> result;
        try {
            switch (address[0]) {
                case 'M':
                case 'm': {
                    result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.M, Convert.ToUInt16(address.Substring(1), MelsecMcDataType.M.FromBase));
                    break;
                }
                case 'X':
                case 'x': {
                    result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.X, Convert.ToUInt16(address.Substring(1), 8));
                    break;
                }
                case 'Y':
                case 'y': {
                    result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.Y, Convert.ToUInt16(address.Substring(1), 8));
                    break;
                }
                case 'D':
                case 'd': {
                    result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.D, Convert.ToUInt16(address.Substring(1), MelsecMcDataType.D.FromBase));
                    break;
                }
                case 'S':
                case 's': {
                    result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.S, Convert.ToUInt16(address.Substring(1), MelsecMcDataType.S.FromBase));
                    break;
                }
                case 'T':
                case 't': {
                    if (address[1] == 'N' || address[1] == 'n') {
                        result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.TN, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TN.FromBase));
                        break;
                    }
                    else if (address[1] == 'S' || address[1] == 's') {
                        result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.TS, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TS.FromBase));
                        break;
                    }
                    else if (address[1] == 'C' || address[1] == 'c') {
                        result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.TC, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TC.FromBase));
                        break;
                    }
                    else {
                        throw new Exception(StringResources.Language.NotSupportedDataType);
                    }
                }
                case 'C':
                case 'c': {
                    if (address[1] == 'N' || address[1] == 'n') {
                        result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.CN, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CN.FromBase));
                        break;
                    }
                    else if (address[1] == 'S' || address[1] == 's') {
                        result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.CS, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CS.FromBase));
                        break;
                    }
                    else if (address[1] == 'C' || address[1] == 'c') {
                        result = new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.CC, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CC.FromBase));
                        break;
                    }
                    else {
                        throw new Exception(StringResources.Language.NotSupportedDataType);
                    }
                }
                default: throw new Exception(StringResources.Language.NotSupportedDataType);
            }
        }
        catch (Exception ex) {
            return new LightOperationResult<MelsecMcDataType, ushort>(ex.Message);
        }

        return result;
    }

    /// <summary>
    /// 返回读取的地址及长度信息
    /// </summary>
    /// <param name="address">读取的地址信息</param>
    /// <returns>带起始地址的结果对象</returns>
    public static LightOperationResult<ushort> FxCalculateWordStartAddress(string address) {
        // 初步解析，失败就返回
        LightOperationResult<MelsecMcDataType, ushort> analysis = FxAnalysisAddress(address);
        if (analysis.IsSuccess)
            return FxCalculateWordStartAddress(analysis.Content1, analysis.Content2);

        return new LightOperationResult<ushort>(analysis.ErrorCode, analysis.Message);

    }
    
    public static LightOperationResult<ushort> FxCalculateWordStartAddress(MelsecMcDataType dataType, ushort startAddress) {
        // 二次解析
        if (dataType == MelsecMcDataType.D) {
            if (startAddress >= 8000) {
                startAddress = (ushort) ((startAddress - 8000) * 2 + 0x0E00);
            }
            else {
                startAddress = (ushort) (startAddress * 2 + 0x1000);
            }
        }
        else if (dataType == MelsecMcDataType.CN) {
            if (startAddress >= 200) {
                startAddress = (ushort) ((startAddress - 200) * 4 + 0x0C00);
            }
            else {
                startAddress = (ushort) (startAddress * 2 + 0x0A00);
            }
        }
        else if (dataType == MelsecMcDataType.TN) {
            startAddress = (ushort) (startAddress * 2 + 0x0800);
        }
        else {
            return new LightOperationResult<ushort>(StringResources.Language.MelsecCurrentTypeNotSupportedWordOperate);
        }

        return LightOperationResult.CreateSuccessResult(startAddress);
    }

    /// <summary>
    /// 返回读取的地址及长度信息，以及当前的偏置信息
    /// </summary><param name="address">读取的地址信息</param>
    /// <returns>带起始地址的结果对象</returns>
    public static LightOperationResult<ushort, ushort, ushort> FxCalculateBoolStartAddress(string address) {
        // 初步解析
        LightOperationResult<MelsecMcDataType, ushort> analysis = FxAnalysisAddress(address);
        if (analysis.IsSuccess)
            return FxCalculateBoolStartAddress(analysis.Content1, analysis.Content2);

        return new LightOperationResult<ushort, ushort, ushort>(analysis.ErrorCode, analysis.Message);
    }
    
    public static LightOperationResult<ushort, ushort, ushort> FxCalculateBoolStartAddress(MelsecMcDataType dataType, ushort startAddress) {
        // 二次解析
        
        ushort originalAddress = startAddress;
        if (dataType == MelsecMcDataType.M) {
            if (startAddress >= 8000) {
                startAddress = (ushort) ((startAddress - 8000) / 8 + 0x01E0);
            }
            else {
                startAddress = (ushort) (startAddress / 8 + 0x0100);
            }
        }
        else if (dataType == MelsecMcDataType.X) {
            startAddress = (ushort) (startAddress / 8 + 0x0080);
        }
        else if (dataType == MelsecMcDataType.Y) {
            startAddress = (ushort) (startAddress / 8 + 0x00A0);
        }
        else if (dataType == MelsecMcDataType.S) {
            startAddress = (ushort) (startAddress / 8 + 0x0000);
        }
        else if (dataType == MelsecMcDataType.CS) {
            startAddress += (ushort) (startAddress / 8 + 0x01C0);
        }
        else if (dataType == MelsecMcDataType.CC) {
            startAddress += (ushort) (startAddress / 8 + 0x03C0);
        }
        else if (dataType == MelsecMcDataType.TS) {
            startAddress += (ushort) (startAddress / 8 + 0x00C0);
        }
        else if (dataType == MelsecMcDataType.TC) {
            startAddress += (ushort) (startAddress / 8 + 0x02C0);
        }
        else {
            return new LightOperationResult<ushort, ushort, ushort>(StringResources.Language.MelsecCurrentTypeNotSupportedBitOperate);
        }

        return LightOperationResult.CreateSuccessResult(startAddress, originalAddress, (ushort) (originalAddress % 8));
    }
}