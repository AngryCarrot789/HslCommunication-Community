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
    public const int CODE_STX = 0x02;
    public const int CODE_ETX = 0x03;
    public const int CODE_EOT = 0x04;
    public const int CODE_ENQ = 0x05;
    public const int CODE_ACK = 0x06;
    public const int CODE_LF = 0x0A;
    public const int CODE_CL = 0x0C;
    public const int CODE_CR = 0x0D;
    public const int CODE_NAK = 0x15;
    
    public const int CMD_BASIC_READ = 0x30;
    public const int CMD_BASIC_WRITE = 0x31;
    public const int CMD_FORCE_SET_BIT = 0x37;
    public const int CMD_FORCE_RESET_BIT = 0x38;
    public const int CMD_EXT_PREFIX = 0x45;
    public const int CMD_EXT_READ_PREFIX = 0x30;
    public const int CMD_EXT_WRITE_PREFIX = 0x31;
    public const int CMD_EXT_CONFIG = 0x30;
    public const int CMD_EXT_PLC_CODE = 0x31;
    
    /// <summary>
    /// 实例化三菱的串口协议的通讯对象
    /// </summary>
    public MelsecFxSerial() {
        this.WordLength = 1;
    }

    protected override OperateResult InitializationOnOpen() {
        LightOperationResult<byte[]> responce = this.SendMessageAndGetResponce(new byte[] { 0x05 });
        if (responce.IsSuccess && responce.Content[0] == CODE_ACK) {
            Console.WriteLine("Successfully connected to PLC!");
        }
        
        return base.InitializationOnOpen();
    }

    /*
        For more info: https://github.com/KunYi/FX3U_Simulation
        BASIC READ:                 0x30
        BASIC WRITE:                0x31
        FORCE SET BIT:              0x37
        FORCE RESET BIT:            0x38
        EXTENSION READ PLC CONFIG:  0x45 0x30 0x30
        EXTENSION READ PLC CODE:    0x45 0x30 0x31
        EXTENSION WRITE PLC CONFIG: 0x45 0x31 0x30
        EXTENSION WRITE PLC CODE:   0x45 0x31 0x31
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
                return buffer[0] == CODE_STX && count >= 5 && buffer[count - 3] == CODE_ETX && MelsecHelper.CheckCRC(buffer, count);
        }
    }

    private LightOperationResult CheckPlcReadResponse(byte[] ack) {
        if (ack.Length == 0)
            return new LightOperationResult(StringResources.Language.MelsecFxReceiveZore);
        if (ack[0] == CODE_NAK) // Received NAK
            return new LightOperationResult(StringResources.Language.MelsecFxAckNagative + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
        if (ack[0] != CODE_STX) // 
            return new LightOperationResult(StringResources.Language.MelsecFxAckWrong + ack[0] + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
        if (!MelsecHelper.CheckCRC(ack))
            return new LightOperationResult(StringResources.Language.MelsecFxCrcCheckFailed);

        return LightOperationResult.CreateSuccessResult();
    }

    private LightOperationResult CheckPlcWriteResponse(byte[] ack) {
        if (ack.Length == 0)
            return new LightOperationResult(StringResources.Language.MelsecFxReceiveZore);
        if (ack[0] == CODE_NAK)
            return new LightOperationResult(StringResources.Language.MelsecFxAckNagative + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
        if (ack[0] != CODE_ACK)
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
        return ConvertResponse(read.Content).ToOperateResult();
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
        return ExtractBoolArrayFromResponce(read.Content, command.Content2, length).ToOperateResult();
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

    // public override OperateResult Write(string address, bool[] value) {
    //     LightOperationResult<byte[]> command = BuildWriteBoolPacket(address, value);
    //     if (!command.IsSuccess)
    //         return command.ToOperateResult();
    //
    //     // 和串口进行核心的数据交互
    //     LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
    //     if (!read.IsSuccess)
    //         return new OperateResult<byte[]>(read.ErrorCode, read.Message);
    //
    //     // 检查结果是否正确
    //     LightOperationResult checkResult = this.CheckPlcWriteResponse(read.Content);
    //     if (!checkResult.IsSuccess)
    //         return checkResult.ToOperateResult();
    //
    //     return OperateResult.CreateSuccessResult();
    // }

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
        LightOperationResult<MelsecMcDataType, ushort> analysis = FxParseAddress(address);
        if (!analysis.IsSuccess)
            return new LightOperationResult<byte[]>(analysis.ErrorCode, analysis.Message);

        // The offset of the starting address of the secondary operation.
        // The address is calculated differently depending on the type.
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

        byte[] asciiAddress = SoftBasic.BuildAsciiBytesFrom(startAddress);
        byte[] cmd = new byte[9];
        cmd[0] = CODE_STX;
        cmd[1] = value ? (byte) CMD_FORCE_SET_BIT : (byte) CMD_FORCE_RESET_BIT;
        cmd[2] = asciiAddress[2];
        cmd[3] = asciiAddress[3];
        cmd[4] = asciiAddress[0];
        cmd[5] = asciiAddress[1];
        cmd[6] = CODE_ETX;
        MelsecHelper.FxCalculateCRC(cmd).CopyTo(cmd, 7);

        return LightOperationResult.CreateSuccessResult(cmd);
    }

    // public static LightOperationResult<byte[]> BuildWriteBoolPacket(string address, bool[] values) {
    //     LightOperationResult<MelsecMcDataType, ushort> analysis = FxParseAddress(address);
    //     if (!analysis.IsSuccess)
    //         return new LightOperationResult<byte[]>(analysis.ErrorCode, analysis.Message);
    //
    //     // The offset of the starting address of the secondary operation.
    //     // The address is calculated differently depending on the type.
    //     ushort startAddress = analysis.Content2;
    //     if (analysis.Content1 == MelsecMcDataType.M) {
    //         if (startAddress >= 8000) {
    //             startAddress = (ushort) (startAddress - 8000 + 0x0F00);
    //         }
    //         else {
    //             startAddress = (ushort) (startAddress + 0x0800);
    //         }
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.S) {
    //         startAddress = (ushort) (startAddress + 0x0000);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.X) {
    //         startAddress = (ushort) (startAddress + 0x0400);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.Y) {
    //         startAddress = (ushort) (startAddress + 0x0500);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.CS) {
    //         startAddress += (ushort) (startAddress + 0x01C0);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.CC) {
    //         startAddress += (ushort) (startAddress + 0x03C0);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.CN) {
    //         startAddress += (ushort) (startAddress + 0x0E00);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.TS) {
    //         startAddress += (ushort) (startAddress + 0x00C0);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.TC) {
    //         startAddress += (ushort) (startAddress + 0x02C0);
    //     }
    //     else if (analysis.Content1 == MelsecMcDataType.TN) {
    //         startAddress += (ushort) (startAddress + 0x0600);
    //     }
    //     else {
    //         return new LightOperationResult<byte[]>(StringResources.Language.MelsecCurrentTypeNotSupportedBitOperate);
    //     }
    //
    //     // byte[] rawArray = new byte[] { 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, };
    //     // byte[] dataBytes = SoftBasic.BuildAsciiBytesFrom(SoftBasic.BoolArrayToByte(values));
    //     // byte[] asciiAddress = SoftBasic.BuildAsciiBytesFrom(startAddress);
    //     // byte[] lengthBytes = Encoding.ASCII.GetBytes((dataBytes.Length).ToString("D2").Substring(0, 2));
    //     // byte[] cmd = new byte[11 + dataBytes.Length];
    //     // cmd[0] = CODE_STX;
    //     // cmd[1] = CMD_BASIC_WRITE;
    //     // cmd[2] = asciiAddress[2];
    //     // cmd[3] = asciiAddress[3];
    //     // cmd[4] = asciiAddress[0];
    //     // cmd[5] = asciiAddress[1];
    //     // cmd[6] = lengthBytes[0];
    //     // cmd[7] = lengthBytes[1];
    //     // Array.Copy(dataBytes, 0, cmd, 8, dataBytes.Length);
    //     // cmd[cmd.Length - 3] = CODE_ETX;
    //     // MelsecHelper.FxCalculateCRC(cmd).CopyTo(cmd, cmd.Length - 2);
    //     // return LightOperationResult.CreateSuccessResult(cmd);
    //
    //     // Doesn't work, even if the raw address is used without additional processing above
    //     // Maybe FX3U uses an older protocol? This should work according to the manuals
    //     byte[] array = SoftBasic.BoolArrayToByte(values);
    //     byte[] asciiAddress = SoftBasic.BuildAsciiBytesFrom(startAddress);
    //     byte[] lengthBytes = Encoding.ASCII.GetBytes(array.Length.ToString("D2").Substring(0, 2));
    //     byte[] cmd = new byte[17 + array.Length];
    //     cmd[0] = 0x05; // ENQ
    //     cmd[1] = 0x30; // STATION BYTE 0
    //     cmd[2] = 0x30; // STATION BYTE 1
    //     cmd[3] = 0x30; // PC NUMBER BYTE 0 
    //     cmd[4] = 0x30; // PC NUMBER BYTE 1
    //     cmd[5] = 0x42; // 'B'
    //     cmd[6] = 0x57; // 'W'
    //     cmd[7] = 0x30;
    //     cmd[8] = Encoding.ASCII.GetBytes(char.ToUpperInvariant(address[0]).ToString())[0]; // Y
    //     cmd[9] = asciiAddress[0];
    //     cmd[10] = asciiAddress[1];
    //     cmd[11] = asciiAddress[2];
    //     cmd[12] = asciiAddress[3];
    //     cmd[13] = lengthBytes[0];
    //     cmd[14] = lengthBytes[1];
    //     array.CopyTo(cmd, 15); // Copy data
    //     MelsecHelper.FxCalculateCRC(cmd).CopyTo(cmd, cmd.Length - 2);
    //     return LightOperationResult.CreateSuccessResult(cmd);
    // }

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

        byte[] asciiAddress = SoftBasic.BuildAsciiBytesFrom(addressResult.Content);
        byte[] asciiLength = SoftBasic.BuildAsciiBytesFrom((byte) (ushort) (length * 2));

        byte[] cmd = new byte[11];
        cmd[0] = CODE_STX;
        cmd[1] = CMD_BASIC_READ;
        cmd[2] = asciiAddress[0];
        cmd[3] = asciiAddress[1];
        cmd[4] = asciiAddress[2];
        cmd[5] = asciiAddress[3];
        cmd[6] = asciiLength[0];
        cmd[7] = asciiLength[1];
        cmd[8] = CODE_ETX;
        MelsecHelper.FxCalculateCRC(cmd).CopyTo(cmd, 9);

        return LightOperationResult.CreateSuccessResult(cmd); // Return
    }

    /// <summary>
    /// 根据类型地址长度确认需要读取的指令头
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">bool数组长度</param>
    /// <returns>带有成功标志的指令数据</returns>
    public static LightOperationResult<byte[], int> BuildReadBoolCommand(string address, ushort length) {
        LightOperationResult<ushort, ushort, ushort> startAddress = FxCalculateReadBoolStartAddress(address);
        if (!startAddress.IsSuccess)
            return new LightOperationResult<byte[], int>(startAddress.ErrorCode, startAddress.Message);

        byte[] asciiAddress = SoftBasic.BuildAsciiBytesFrom(startAddress.Content1);
        byte[] asciiLength = SoftBasic.BuildAsciiBytesFrom((byte) (ushort) ((startAddress.Content2 + length - 1) / 8 - (startAddress.Content2 / 8) + 1));

        byte[] cmd = new byte[11];
        cmd[0] = CODE_STX;
        cmd[1] = CMD_BASIC_READ;
        cmd[2] = asciiAddress[0];
        cmd[3] = asciiAddress[1];
        cmd[4] = asciiAddress[2];
        cmd[5] = asciiAddress[3];
        cmd[6] = asciiLength[0];
        cmd[7] = asciiLength[1];
        cmd[8] = CODE_ETX;
        MelsecHelper.FxCalculateCRC(cmd).CopyTo(cmd, 9);

        return LightOperationResult.CreateSuccessResult(cmd, (int) startAddress.Content3);
    }

    /// <summary>
    /// Generates a command header based on the address and the data to be written
    /// </summary>
    /// <param name="address">Start address</param>
    /// <param name="data">Data to be written</param>
    /// <returns>Command data</returns>
    public static LightOperationResult<byte[]> BuildWriteWordCommand(string address, byte[] data) {
        LightOperationResult<ushort> startAddress = FxCalculateWordStartAddress(address);
        if (!startAddress.IsSuccess)
            return new LightOperationResult<byte[]>(startAddress.ErrorCode, startAddress.Message);

        // Convert bytes to ASCII
        byte[] dataBytes = SoftBasic.BuildAsciiBytesFrom(data);
        byte[] asciiAddress = SoftBasic.BuildAsciiBytesFrom(startAddress.Content);
        byte[] asciiLength = SoftBasic.BuildAsciiBytesFrom((byte) (dataBytes.Length / 2));

        byte[] cmd = new byte[11 + dataBytes.Length];
        cmd[0] = CODE_STX;
        cmd[1] = CMD_BASIC_WRITE;
        cmd[2] = asciiAddress[0];
        cmd[3] = asciiAddress[1];
        cmd[4] = asciiAddress[2];
        cmd[5] = asciiAddress[3];
        cmd[6] = asciiLength[0];
        cmd[7] = asciiLength[1];
        Array.Copy(dataBytes, 0, cmd, 8, dataBytes.Length);
        cmd[cmd.Length - 3] = CODE_ETX;
        MelsecHelper.FxCalculateCRC(cmd).CopyTo(cmd, cmd.Length - 2);

        return LightOperationResult.CreateSuccessResult(cmd);
    }

    /// <summary>
    /// 从PLC反馈的数据进行提炼操作
    /// </summary>
    /// <param name="response">PLC反馈的真实数据</param>
    /// <returns>数据提炼后的真实数据</returns>
    public static LightOperationResult<byte[]> ConvertResponse(byte[] response) {
        try {
            // I wonder if this is slower or faster than using a traditional byte[]
            // stackalloc is 6.67 microseconds, byte array is about 5.5 micros.
            // But i tested this in debug mode so maybe release is faster?
            byte[] buffer = new byte[2];
            byte[] data = new byte[(response.Length - 4) / 2];
            for (int i = 0; i < data.Length; i++) {
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

    public static LightOperationResult<bool[]> ExtractBoolArrayFromResponce(byte[] response, int start, int length) {
        LightOperationResult<byte[]> realResponse = ConvertResponse(response);
        if (!realResponse.IsSuccess)
            return new LightOperationResult<bool[]>(realResponse.ErrorCode, realResponse.Message);

        try {
            // DateTime startTime = DateTime.Now;
            bool[] data = new bool[length];
            bool[] array = SoftBasic.ByteToBoolArray(realResponse.Content, realResponse.Content.Length * 8);

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

    internal static LightOperationResult<ushort> FxCalculateWordStartAddress(string address) {
        LightOperationResult<MelsecMcDataType, ushort> analysis = FxParseAddress(address);
        if (!analysis.IsSuccess)
            return new LightOperationResult<ushort>(analysis.ErrorCode, analysis.Message);

        return FxCalculateWordStartAddress(analysis.Content1, analysis.Content2);
    }
    
    internal static LightOperationResult<ushort, ushort, ushort> FxCalculateReadBoolStartAddress(string address) {
        LightOperationResult<MelsecMcDataType, ushort> analysis = FxParseAddress(address);
        if (!analysis.IsSuccess) {
            return new LightOperationResult<ushort, ushort, ushort>(analysis.ErrorCode, analysis.Message);
        }

        return FxCalculateReadBoolStartAddress(analysis.Content1, analysis.Content2);
    }

    internal static LightOperationResult<ushort> FxCalculateWordStartAddress(MelsecMcDataType dataType, ushort startAddress) {
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

    internal static LightOperationResult<ushort, ushort, ushort> FxCalculateReadBoolStartAddress(MelsecMcDataType dataType, ushort startAddress) {
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
    
    /// <summary>
    /// Parse data addresses into different Mitsubishi address types
    /// </summary>
    public static LightOperationResult<MelsecMcDataType, ushort> FxParseAddress(string address) {
        try {
            switch (address[0]) {
                case 'M':
                case 'm':
                    return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.M, Convert.ToUInt16(address.Substring(1), MelsecMcDataType.M.FromBase));
                case 'X':
                case 'x':
                    return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.X, Convert.ToUInt16(address.Substring(1), 8));
                case 'Y':
                case 'y':
                    return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.Y, Convert.ToUInt16(address.Substring(1), 8));
                case 'D':
                case 'd':
                    return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.D, Convert.ToUInt16(address.Substring(1), MelsecMcDataType.D.FromBase));
                case 'S':
                case 's':
                    return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.S, Convert.ToUInt16(address.Substring(1), MelsecMcDataType.S.FromBase));
                case 'T':
                case 't': {
                    switch (address[1]) {
                        case 'N':
                        case 'n':
                            return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.TN, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TN.FromBase));
                        case 'S':
                        case 's':
                            return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.TS, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TS.FromBase));
                        case 'C':
                        case 'c':
                            return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.TC, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TC.FromBase));
                        default: throw new Exception(StringResources.Language.NotSupportedDataType + ": " + address);
                    }
                }
                case 'C':
                case 'c': {
                    switch (address[1]) {
                        case 'N':
                        case 'n':
                            return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.CN, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CN.FromBase));
                        case 'S':
                        case 's':
                            return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.CS, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CS.FromBase));
                        case 'C':
                        case 'c':
                            return new LightOperationResult<MelsecMcDataType, ushort>(MelsecMcDataType.CC, Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CC.FromBase));
                        default: throw new Exception(StringResources.Language.NotSupportedDataType + ": " + address);
                    }
                }
                default: throw new Exception(StringResources.Language.NotSupportedDataType + ": " + address);
            }
        }
        catch (Exception ex) {
            return new LightOperationResult<MelsecMcDataType, ushort>(ex.Message);
        }
    }
}