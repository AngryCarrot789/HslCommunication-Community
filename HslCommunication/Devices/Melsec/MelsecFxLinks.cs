﻿using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;
using HslCommunication.Serial;

namespace HslCommunication.Devices.Melsec;

/// <summary>
/// 三菱PLC的计算机链接协议，适用的PLC型号参考备注
/// </summary>
/// <remarks>
/// 支持的通讯的系列如下参考
/// <list type="table">
///     <listheader>
///         <term>系列</term>
///         <term>是否支持</term>
///         <term>备注</term>
///     </listheader>
///     <item>
///         <description>FX3UC系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX3U系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX3GC系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX3G系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX3S系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX2NC系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX2N系列</description>
///         <description>部分支持(v1.06+)</description>
///         <description>通过监控D8001来确认版本号</description>
///     </item>
///     <item>
///         <description>FX1NC系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX1N系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX1S系列</description>
///         <description>支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX0N系列</description>
///         <description>部分支持(v1.20+)</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX0S系列</description>
///         <description>不支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX0系列</description>
///         <description>不支持</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX2C系列</description>
///         <description>部分支持(v3.30+)</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX2(FX)系列</description>
///         <description>部分支持(v3.30+)</description>
///         <description></description>
///     </item>
///     <item>
///         <description>FX1系列</description>
///         <description>不支持</description>
///         <description></description>
///     </item>
/// </list>
/// 数据地址支持的格式如下：
/// <list type="table">
///   <listheader>
///     <term>地址名称</term>
///     <term>地址代号</term>
///     <term>示例</term>
///     <term>地址进制</term>
///     <term>字操作</term>
///     <term>位操作</term>
///     <term>备注</term>
///   </listheader>
///   <item>
///     <term>内部继电器</term>
///     <term>M</term>
///     <term>M100,M200</term>
///     <term>10</term>
///     <term>√</term>
///     <term>√</term>
///     <term></term>
///   </item>
///   <item>
///     <term>输入继电器</term>
///     <term>X</term>
///     <term>X10,X20</term>
///     <term>8</term>
///     <term>√</term>
///     <term>√</term>
///     <term></term>
///   </item>
///   <item>
///     <term>输出继电器</term>
///     <term>Y</term>
///     <term>Y10,Y20</term>
///     <term>8</term>
///     <term>√</term>
///     <term>√</term>
///     <term></term>
///   </item>
///   <item>
///     <term>步进继电器</term>
///     <term>S</term>
///     <term>S100,S200</term>
///     <term>10</term>
///     <term>√</term>
///     <term>√</term>
///     <term></term>
///   </item>
///   <item>
///     <term>定时器的触点</term>
///     <term>TS</term>
///     <term>TS100,TS200</term>
///     <term>10</term>
///     <term>√</term>
///     <term>√</term>
///     <term></term>
///   </item>
///   <item>
///     <term>定时器的当前值</term>
///     <term>TN</term>
///     <term>TN100,TN200</term>
///     <term>10</term>
///     <term>√</term>
///     <term>×</term>
///     <term></term>
///   </item>
///   <item>
///     <term>计数器的触点</term>
///     <term>CS</term>
///     <term>CS100,CS200</term>
///     <term>10</term>
///     <term>√</term>
///     <term>√</term>
///     <term></term>
///   </item>
///   <item>
///     <term>计数器的当前</term>
///     <term>CN</term>
///     <term>CN100,CN200</term>
///     <term>10</term>
///     <term>√</term>
///     <term>×</term>
///     <term></term>
///   </item>
///   <item>
///     <term>数据寄存器</term>
///     <term>D</term>
///     <term>D1000,D2000</term>
///     <term>10</term>
///     <term>√</term>
///     <term>×</term>
///     <term></term>
///   </item>
///   <item>
///     <term>文件寄存器</term>
///     <term>R</term>
///     <term>R100,R200</term>
///     <term>10</term>
///     <term>√</term>
///     <term>×</term>
///     <term></term>
///   </item>
/// </list>
/// </remarks>
public class MelsecFxLinks : SerialDeviceBase<RegularByteTransform> {
    /// <summary>
    /// 实例化默认的构造方法
    /// </summary>
    public MelsecFxLinks() {
        this.WordLength = 1;
    }

    /// <summary>
    /// PLC的站号信息
    /// </summary>
    public byte Station { get => this.station; set => this.station = value; }

    /// <summary>
    /// 报文等待时间，单位10ms，设置范围为0-15
    /// </summary>
    public byte WaittingTime {
        get => this.watiingTime;
        set {
            if (this.watiingTime > 0x0F) {
                this.watiingTime = 0x0F;
            }
            else {
                this.watiingTime = value;
            }
        }
    }

    /// <summary>
    /// 是否启动和校验
    /// </summary>
    public bool SumCheck { get => this.sumCheck; set => this.sumCheck = value; }

    /// <summary>
    /// 批量读取PLC的数据，以字为单位，支持读取X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认
    /// </summary>
    /// <param name="address">地址信息</param>
    /// <param name="length">数据长度</param>
    /// <returns>读取结果信息</returns>
    public override OperateResult<byte[]> Read(string address, ushort length) {
        // 解析指令
        LightOperationResult<byte[]> command = BuildReadCommand(this.station, address, length, false, this.sumCheck, this.watiingTime);
        if (!command.IsSuccess)
            return new OperateResult<byte[]>(command.ErrorCode, command.Message);

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 结果验证
        if (read.Content[0] != 0x02)
            return new OperateResult<byte[]>(read.Content[0], "Read Faild:" + SoftBasic.ByteToHexString(read.Content, ' '));

        // 提取结果
        byte[] Content = new byte[length * 2];
        for (int i = 0; i < Content.Length / 2; i++) {
            ushort tmp = Convert.ToUInt16(Encoding.ASCII.GetString(read.Content, i * 4 + 5, 4), 16);
            BitConverter.GetBytes(tmp).CopyTo(Content, i * 2);
        }

        return OperateResult.CreateSuccessResult(Content);
    }

    /// <summary>
    /// 批量写入PLC的数据，以字为单位，也就是说最少2个字节信息，支持X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认
    /// </summary>
    /// <param name="address">地址信息</param>
    /// <param name="value">数据值</param>
    /// <returns>是否写入成功</returns>
    public override OperateResult Write(string address, byte[] value) {
        // 解析指令
        OperateResult<byte[]> command = BuildWriteByteCommand(this.station, address, value, this.sumCheck, this.watiingTime);
        if (!command.IsSuccess)
            return command;

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 结果验证
        if (read.Content[0] != 0x06)
            return new OperateResult(read.Content[0], "Write Faild:" + SoftBasic.ByteToHexString(read.Content, ' '));

        // 提取结果
        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 批量读取bool类型数据，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型
    /// </summary>
    /// <param name="address">地址信息，比如X10,Y17，注意X，Y的地址是8进制的</param>
    /// <param name="length">读取的长度</param>
    /// <returns>读取结果信息</returns>
    public override OperateResult<bool[]> ReadBool(string address, ushort length) {
        // 解析指令
        LightOperationResult<byte[]> command = BuildReadCommand(this.station, address, length, true, this.sumCheck, this.watiingTime);
        if (!command.IsSuccess)
            return new OperateResult<bool[]>(command.ErrorCode, command.Message);

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<bool[]>(read.ErrorCode, read.Message);

        // 结果验证
        if (read.Content[0] != 0x02)
            return new OperateResult<bool[]>(read.Content[0], "Read Faild:" + SoftBasic.ByteToHexString(read.Content, ' '));

        // 提取结果
        byte[] buffer = new byte[length];
        Array.Copy(read.Content, 5, buffer, 0, length);
        return OperateResult.CreateSuccessResult(buffer.Select(m => m == 0x31).ToArray());
    }

    /// <summary>
    /// 批量写入bool类型的数组，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型
    /// </summary>
    /// <param name="address">PLC的地址信息</param>
    /// <param name="value">数据信息</param>
    /// <returns>是否写入成功</returns>
    public override OperateResult Write(string address, bool[] value) {
        // 解析指令
        OperateResult<byte[]> command = BuildWriteBoolCommand(this.station, address, value, this.sumCheck, this.watiingTime);
        if (!command.IsSuccess)
            return command;

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 结果验证
        if (read.Content[0] != 0x06)
            return new OperateResult(read.Content[0], "Write Faild:" + SoftBasic.ByteToHexString(read.Content, ' '));

        // 提取结果
        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 启动PLC
    /// </summary>
    /// <returns>是否启动成功</returns>
    public OperateResult StartPLC() {
        // 解析指令
        OperateResult<byte[]> command = BuildStart(this.station, this.sumCheck, this.watiingTime);
        if (!command.IsSuccess)
            return command;

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 结果验证
        if (read.Content[0] != 0x06)
            return new OperateResult(read.Content[0], "Start Faild:" + SoftBasic.ByteToHexString(read.Content, ' '));

        // 提取结果
        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 停止PLC
    /// </summary>
    /// <returns>是否停止成功</returns>
    public OperateResult StopPLC() {
        // 解析指令
        OperateResult<byte[]> command = BuildStop(this.station, this.sumCheck, this.watiingTime);
        if (!command.IsSuccess)
            return command;

        // 核心交互
        LightOperationResult<byte[]> read = this.SendMessageAndGetResponce(command.Content);
        if (!read.IsSuccess)
            return new OperateResult<byte[]>(read.ErrorCode, read.Message);

        // 结果验证
        if (read.Content[0] != 0x06)
            return new OperateResult(read.Content[0], "Stop Faild:" + SoftBasic.ByteToHexString(read.Content, ' '));

        // 提取结果
        return OperateResult.CreateSuccessResult();
    }

    private byte station = 0x00; // PLC的站号信息
    private byte watiingTime = 0x00; // 报文的等待时间，设置为0-15
    private bool sumCheck = true; // 是否启用和校验

    /// <summary>
    /// 解析数据地址成不同的三菱地址类型
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <returns>地址结果对象</returns>
    private static OperateResult<string> FxAnalysisAddress(string address) {
        OperateResult<string> result = new OperateResult<string>();
        try {
            switch (address[0]) {
                case 'X':
                case 'x': {
                    result.Content = "X" + Convert.ToUInt16(address.Substring(1), 10).ToString("D4");
                    break;
                }
                case 'Y':
                case 'y': {
                    result.Content = "Y" + Convert.ToUInt16(address.Substring(1), 10).ToString("D4");
                    break;
                }
                case 'M':
                case 'm': {
                    result.Content = "M" + Convert.ToUInt16(address.Substring(1), 10).ToString("D4");
                    break;
                }
                case 'S':
                case 's': {
                    result.Content = "S" + Convert.ToUInt16(address.Substring(1), 10).ToString("D4");
                    break;
                }
                case 'T':
                case 't': {
                    if (address[1] == 'S' || address[1] == 's') {
                        result.Content = "TS" + Convert.ToUInt16(address.Substring(1), 10).ToString("D3");
                        break;
                    }
                    else if (address[1] == 'N' || address[1] == 'n') {
                        result.Content = "TN" + Convert.ToUInt16(address.Substring(1), 10).ToString("D3");
                        break;
                    }
                    else {
                        throw new Exception(StringResources.Language.NotSupportedDataType);
                    }
                }
                case 'C':
                case 'c': {
                    if (address[1] == 'S' || address[1] == 's') {
                        result.Content = "CS" + Convert.ToUInt16(address.Substring(1), 10).ToString("D3");
                        break;
                    }
                    else if (address[1] == 'N' || address[1] == 'n') {
                        result.Content = "CN" + Convert.ToUInt16(address.Substring(1), 10).ToString("D3");
                        break;
                    }
                    else {
                        throw new Exception(StringResources.Language.NotSupportedDataType);
                    }
                }
                case 'D':
                case 'd': {
                    result.Content = "D" + Convert.ToUInt16(address.Substring(1), 10).ToString("D4");
                    break;
                }
                case 'R':
                case 'r': {
                    result.Content = "R" + Convert.ToUInt16(address.Substring(1), 10).ToString("D4");
                    break;
                }
                default: throw new Exception(StringResources.Language.NotSupportedDataType);
            }
        }
        catch (Exception ex) {
            result.Message = ex.Message;
            return result;
        }

        result.IsSuccess = true;
        return result;
    }

    /// <summary>
    /// 计算指令的和校验码
    /// </summary>
    /// <param name="data">指令</param>
    /// <returns>校验之后的信息</returns>
    public static string CalculateAcc(string data) {
        byte[] buffer = Encoding.ASCII.GetBytes(data);

        int count = 0;
        for (int i = 0; i < buffer.Length; i++) {
            count += buffer[i];
        }

        return data + count.ToString("X4").Substring(2);
    }

    /// <summary>
    /// 创建一条读取的指令信息，需要指定一些参数
    /// </summary>
    /// <param name="station">PLCd的站号</param>
    /// <param name="address">地址信息</param>
    /// <param name="length">数据长度</param>
    /// <param name="isBool">是否位读取</param>
    /// <param name="sumCheck">是否和校验</param>
    /// <param name="waitTime">等待时间</param>
    /// <returns>是否成功的结果对象</returns>
    public static LightOperationResult<byte[]> BuildReadCommand(byte station, string address, ushort length, bool isBool, bool sumCheck = true, byte waitTime = 0x00) {
        OperateResult<string> addressAnalysis = FxAnalysisAddress(address);
        if (!addressAnalysis.IsSuccess)
            return new LightOperationResult<byte[]>(addressAnalysis.ErrorCode, addressAnalysis.Message);

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(station.ToString("D2"));
        stringBuilder.Append("FF");

        if (isBool)
            stringBuilder.Append("BR");
        else
            stringBuilder.Append("WR");

        stringBuilder.Append(waitTime.ToString("X"));
        stringBuilder.Append(addressAnalysis.Content);
        stringBuilder.Append(length.ToString("D2"));

        byte[] core = Encoding.ASCII.GetBytes(sumCheck ? CalculateAcc(stringBuilder.ToString()) : stringBuilder.ToString());
        core = SoftBasic.SpliceTwoByteArray(new byte[] { 0x05 }, core);
        return LightOperationResult.CreateSuccessResult(core);
    }

    /// <summary>
    /// 创建一条别入bool数据的指令信息，需要指定一些参数
    /// </summary>
    /// <param name="station">站号</param>
    /// <param name="address">地址</param>
    /// <param name="value">数组值</param>
    /// <param name="sumCheck">是否和校验</param>
    /// <param name="waitTime">等待时间</param>
    /// <returns>是否创建成功</returns>
    public static OperateResult<byte[]> BuildWriteBoolCommand(byte station, string address, bool[] value, bool sumCheck = true, byte waitTime = 0x00) {
        OperateResult<string> addressAnalysis = FxAnalysisAddress(address);
        if (!addressAnalysis.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(addressAnalysis);

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(station.ToString("D2"));
        stringBuilder.Append("FF");
        stringBuilder.Append("BW");
        stringBuilder.Append(waitTime.ToString("X"));
        stringBuilder.Append(addressAnalysis.Content);
        stringBuilder.Append(value.Length.ToString("D2"));
        for (int i = 0; i < value.Length; i++) {
            stringBuilder.Append(value[i] ? "1" : "0");
        }

        byte[] core = null;
        if (sumCheck)
            core = Encoding.ASCII.GetBytes(CalculateAcc(stringBuilder.ToString()));
        else
            core = Encoding.ASCII.GetBytes(stringBuilder.ToString());

        core = SoftBasic.SpliceTwoByteArray(new byte[] { 0x05 }, core);

        return OperateResult.CreateSuccessResult(core);
    }

    /// <summary>
    /// 创建一条别入byte数据的指令信息，需要指定一些参数，按照字单位
    /// </summary>
    /// <param name="station">站号</param>
    /// <param name="address">地址</param>
    /// <param name="value">数组值</param>
    /// <param name="sumCheck">是否和校验</param>
    /// <param name="waitTime">等待时间</param>
    /// <returns>是否创建成功</returns>
    public static OperateResult<byte[]> BuildWriteByteCommand(byte station, string address, byte[] value, bool sumCheck = true, byte waitTime = 0x00) {
        OperateResult<string> addressAnalysis = FxAnalysisAddress(address);
        if (!addressAnalysis.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(addressAnalysis);

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(station.ToString("D2"));
        stringBuilder.Append("FF");
        stringBuilder.Append("WW");
        stringBuilder.Append(waitTime.ToString("X"));
        stringBuilder.Append(addressAnalysis.Content);
        stringBuilder.Append((value.Length / 2).ToString("D2"));

        // 字写入
        byte[] buffer = new byte[value.Length * 2];
        for (int i = 0; i < value.Length / 2; i++) {
            SoftBasic.BuildAsciiBytesFrom(BitConverter.ToUInt16(value, i * 2)).CopyTo(buffer, 4 * i);
        }

        stringBuilder.Append(Encoding.ASCII.GetString(buffer));


        byte[] core = null;
        if (sumCheck)
            core = Encoding.ASCII.GetBytes(CalculateAcc(stringBuilder.ToString()));
        else
            core = Encoding.ASCII.GetBytes(stringBuilder.ToString());

        core = SoftBasic.SpliceTwoByteArray(new byte[] { 0x05 }, core);

        return OperateResult.CreateSuccessResult(core);
    }

    /// <summary>
    /// 创建启动PLC的报文信息
    /// </summary>
    /// <param name="station">站号信息</param>
    /// <param name="sumCheck">是否和校验</param>
    /// <param name="waitTime">等待时间</param>
    /// <returns>是否创建成功</returns>
    public static OperateResult<byte[]> BuildStart(byte station, bool sumCheck = true, byte waitTime = 0x00) {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(station.ToString("D2"));
        stringBuilder.Append("FF");
        stringBuilder.Append("RR");
        stringBuilder.Append(waitTime.ToString("X"));

        byte[] core = null;
        if (sumCheck)
            core = Encoding.ASCII.GetBytes(CalculateAcc(stringBuilder.ToString()));
        else
            core = Encoding.ASCII.GetBytes(stringBuilder.ToString());

        core = SoftBasic.SpliceTwoByteArray(new byte[] { 0x05 }, core);

        return OperateResult.CreateSuccessResult(core);
    }

    /// <summary>
    /// 创建启动PLC的报文信息
    /// </summary>
    /// <param name="station">站号信息</param>
    /// <param name="sumCheck">是否和校验</param>
    /// <param name="waitTime">等待时间</param>
    /// <returns>是否创建成功</returns>
    public static OperateResult<byte[]> BuildStop(byte station, bool sumCheck = true, byte waitTime = 0x00) {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(station.ToString("D2"));
        stringBuilder.Append("FF");
        stringBuilder.Append("RS");
        stringBuilder.Append(waitTime.ToString("X"));

        byte[] core = null;
        if (sumCheck)
            core = Encoding.ASCII.GetBytes(CalculateAcc(stringBuilder.ToString()));
        else
            core = Encoding.ASCII.GetBytes(stringBuilder.ToString());

        core = SoftBasic.SpliceTwoByteArray(new byte[] { 0x05 }, core);

        return OperateResult.CreateSuccessResult(core);
    }
}