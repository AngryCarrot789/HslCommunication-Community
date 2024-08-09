﻿using System.Net.Sockets;
using System.Text;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;


/*********************************************************************************************
 *
 *    thanks: 江阴-  ∮溪风-⊙_⌒ 提供了测试的PLC
 *    thanks:
 *
 *    感谢一个开源的java项目支持才使得本项目顺利开发：https://github.com/Tulioh/Ethernetip4j
 *
 ***********************************************************************************************/

namespace HslCommunication.Devices.AllenBradley;

/// <summary>
/// AB PLC Data communication class, support read and write PLC node data
/// </summary>
/// <remarks>
/// thanks 江阴-  ∮溪风-⊙_⌒ help test the dll
/// thanks 上海-null 测试了这个dll
/// </remarks>
public class AllenBradleyNet : NetworkDeviceBase<AllenBradleyMessage, RegularByteTransform> {
    /// <summary>
    /// Instantiate a communication object for a Allenbradley PLC protocol
    /// </summary>
    public AllenBradleyNet() {
        this.WordLength = 2;
    }

    /// <summary>
    /// Instantiate a communication object for a Allenbradley PLC protocol
    /// </summary>
    /// <param name="ipAddress">PLC IpAddress</param>
    /// <param name="port">PLC Port</param>
    public AllenBradleyNet(string ipAddress, int port = 44818) {
        this.WordLength = 2;
        this.IpAddress = ipAddress;
        this.Port = port;
    }

    /// <summary>
    /// The current session handle, which is determined by the PLC when communicating with the PLC handshake
    /// </summary>
    public uint SessionHandle { get; private set; }

    /// <summary>
    /// Gets or sets the slot number information for the current plc, which should be set before connections
    /// </summary>
    public byte Slot { get; set; } = 0;

    /// <summary>
    /// when read array type, this means the segment length. when data type is 8-byte data, it should set to be 50
    /// </summary>
    public int ArraySegment { get; set; } = 100;

    /// <summary>
    /// After connecting the Allenbradley plc, a next step handshake protocol is required
    /// </summary>
    /// <param name="socket">socket after connectting sucessful</param>
    /// <returns>Success of initialization</returns>
    protected override OperateResult InitializationOnConnect(Socket socket) {
        // Registering Session Information
        OperateResult<byte[]> read = this.ReadFromCoreServer(socket, this.RegisterSessionHandle());
        if (!read.IsSuccess)
            return read;

        // Check the returned status
        OperateResult check = this.CheckResponse(read.Content);
        if (!check.IsSuccess)
            return check;

        // Extract session ID
        this.SessionHandle = this.ByteTransform.TransUInt32(read.Content, 4);

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// A next step handshake agreement is required before disconnecting the Allenbradley plc
    /// </summary>
    /// <param name="socket">socket befor connection close </param>
    /// <returns>Whether the disconnect operation was successful</returns>
    protected override OperateResult ExtraOnDisconnect(Socket socket) {
        // Unregister session Information
        OperateResult<byte[]> read = this.ReadFromCoreServer(socket, this.UnRegisterSessionHandle());
        if (!read.IsSuccess)
            return read;

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// Build a read command bytes
    /// </summary>
    /// <param name="address">the address of the tag name</param>
    /// <param name="length">Array information, if not arrays, is 1 </param>
    /// <returns>Message information that contains the result object </returns>
    public OperateResult<byte[]> BuildReadCommand(string[] address, int[] length) {
        if (address == null || length == null)
            return new OperateResult<byte[]>("address or length is null");
        if (address.Length != length.Length)
            return new OperateResult<byte[]>("address and length is not same array");

        try {
            List<byte[]> cips = new List<byte[]>();
            for (int i = 0; i < address.Length; i++) {
                cips.Add(AllenBradleyHelper.PackRequsetRead(address[i], length[i]));
            }

            byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData(this.Slot, cips.ToArray());

            return OperateResult.CreateSuccessResult(AllenBradleyHelper.PackRequestHeader(0x6F, this.SessionHandle, commandSpecificData));
        }
        catch (Exception ex) {
            return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
        }
    }

    /// <summary>
    /// Build a read command bytes
    /// </summary>
    /// <param name="address">The address of the tag name </param>
    /// <returns>Message information that contains the result object </returns>
    public OperateResult<byte[]> BuildReadCommand(string[] address) {
        if (address == null)
            return new OperateResult<byte[]>("address or length is null");

        int[] length = new int[address.Length];
        for (int i = 0; i < address.Length; i++) {
            length[i] = 1;
        }

        return this.BuildReadCommand(address, length);
    }

    /// <summary>
    /// Create a written message instruction
    /// </summary>
    /// <param name="address">The address of the tag name </param>
    /// <param name="typeCode">Data type</param>
    /// <param name="data">Source Data </param>
    /// <param name="length">In the case of arrays, the length of the array </param>
    /// <returns>Message information that contains the result object</returns>
    public OperateResult<byte[]> BuildWriteCommand(string address, ushort typeCode, byte[] data, int length = 1) {
        try {
            byte[] cip = AllenBradleyHelper.PackRequestWrite(address, typeCode, data, length);
            byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData(this.Slot, cip);

            return OperateResult.CreateSuccessResult(AllenBradleyHelper.PackRequestHeader(0x6F, this.SessionHandle, commandSpecificData));
        }
        catch (Exception ex) {
            return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
        }
    }

    /// <summary>
    /// Read data information, data length for read array length information
    /// </summary>
    /// <param name="address">Address format of the node</param>
    /// <param name="length">In the case of arrays, the length of the array </param>
    /// <returns>Result data with result object </returns>
    public override OperateResult<byte[]> Read(string address, ushort length) {
        if (length > 1) {
            return this.ReadSegment(address, 0, length);
        }
        else {
            return this.Read(new string[] { address }, new int[] { length });
        }
    }

    /// <summary>
    /// Bulk read Data information
    /// </summary>
    /// <param name="address">Name of the node </param>
    /// <returns>Result data with result object </returns>
    public OperateResult<byte[]> Read(string[] address) {
        if (address == null)
            return new OperateResult<byte[]>("address can not be null");

        int[] length = new int[address.Length];
        for (int i = 0; i < length.Length; i++) {
            length[i] = 1;
        }

        return this.Read(address, length);
    }

    /// <summary>
    /// 批量读取数据信息，数据长度为读取的数组长度信息 -> Bulk read data information, data length for read array length information
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">如果是数组，就为数组长度 -> In the case of arrays, the length of the array </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result object </returns>
    public OperateResult<byte[]> Read(string[] address, int[] length) {
        // 指令生成 -> Instruction Generation
        OperateResult<byte[]> command = this.BuildReadCommand(address, length);
        if (!command.IsSuccess)
            return command;

        // 核心交互 -> Core Interactions
        OperateResult<byte[]> read = this.ReadFromCoreServer(command.Content);
        if (!read.IsSuccess)
            return read;

        // 检查反馈 -> Check Feedback
        OperateResult check = this.CheckResponse(read.Content);
        if (!check.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(check);

        // 提取数据 -> Extracting data
        return AllenBradleyHelper.ExtractActualData(read.Content, true);
    }

    /// <summary>
    /// Read Segment Data Array form plc, use address tag name
    /// </summary>
    /// <param name="address">Tag name in plc</param>
    /// <param name="startIndex">array start index</param>
    /// <param name="length">array length</param>
    /// <returns>Results Bytes</returns>
    public OperateResult<byte[]> ReadSegment(string address, int startIndex, int length) {
        try {
            List<byte> bytesContent = new List<byte>();
            ushort alreadyFinished = 0;
            while (alreadyFinished < length) {
                ushort readLength = (ushort) Math.Min(length - alreadyFinished, 100);
                OperateResult<byte[]> read = this.ReadByCips(AllenBradleyHelper.PackRequestReadSegment(address, startIndex + alreadyFinished, readLength));
                if (!read.IsSuccess)
                    return read;

                bytesContent.AddRange(read.Content);
                alreadyFinished += readLength;
            }

            return OperateResult.CreateSuccessResult(bytesContent.ToArray());
        }
        catch (Exception ex) {
            return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
        }
    }


    private OperateResult<byte[]> ReadByCips(params byte[][] cips) {
        OperateResult<byte[]> read = this.ReadCipFromServer(cips);
        if (!read.IsSuccess)
            return read;

        // 提取数据 -> Extracting data
        return AllenBradleyHelper.ExtractActualData(read.Content, true);
    }

    /// <summary>
    /// 使用CIP报文和服务器进行核心的数据交换
    /// </summary>
    /// <param name="cips">Cip commands</param>
    /// <returns>Results Bytes</returns>
    public OperateResult<byte[]> ReadCipFromServer(params byte[][] cips) {
        byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData(this.Slot, cips);
        byte[] command = AllenBradleyHelper.PackRequestHeader(0x6F, this.SessionHandle, commandSpecificData);

        // 核心交互 -> Core Interactions
        OperateResult<byte[]> read = this.ReadFromCoreServer(command);
        if (!read.IsSuccess)
            return read;

        // 检查反馈 -> Check Feedback
        OperateResult check = this.CheckResponse(read.Content);
        if (!check.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(check);

        return OperateResult.CreateSuccessResult(read.Content);
    }

    /// <summary>
    /// 读取单个的bool数据信息 -> Read a single BOOL data information
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    public override OperateResult<bool> ReadBool(string address) {
        OperateResult<byte[]> read = this.Read(address, 1);
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<bool>(read);

        return OperateResult.CreateSuccessResult(this.ByteTransform.TransBool(read.Content, 0));
    }

    /// <summary>
    /// 批量读取的bool数组信息 -> Bulk read of bool array information
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    public OperateResult<bool[]> ReadBoolArray(string address) {
        OperateResult<byte[]> read = this.Read(address, 1);
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<bool[]>(read);

        return OperateResult.CreateSuccessResult(this.ByteTransform.TransBool(read.Content, 0, read.Content.Length));
    }

    /// <summary>
    /// 读取PLC的byte类型的数据 -> Read the byte type of PLC data
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    public OperateResult<byte> ReadByte(string address) {
        OperateResult<byte[]> read = this.Read(address, 1);
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<byte>(read);

        return OperateResult.CreateSuccessResult(this.ByteTransform.TransByte(read.Content, 0));
    }

    /// <summary>
    /// 读取PLC的short类型的数组 -> Read an array of the short type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt16Array" title="Int16类型示例" />
    /// </example>
    public override OperateResult<short[]> ReadInt16(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransInt16(m, 0, length));
    }

    /// <summary>
    /// 读取PLC的ushort类型的数组 -> An array that reads the ushort type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt16Array" title="UInt16类型示例" />
    /// </example>
    public override OperateResult<ushort[]> ReadUInt16(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransUInt16(m, 0, length));
    }

    /// <summary>
    /// 读取PLC的int类型的数组 -> An array that reads the int type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt32Array" title="Int32类型示例" />
    /// </example>
    public override OperateResult<int[]> ReadInt32(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransInt32(m, 0, length));
    }

    /// <summary>
    /// 读取PLC的uint类型的数组 -> An array that reads the UINT type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt32Array" title="UInt32类型示例" />
    /// </example>
    public override OperateResult<uint[]> ReadUInt32(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransUInt32(m, 0, length));
    }

    /// <summary>
    /// 读取PLC的float类型的数组 -> An array that reads the float type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadFloatArray" title="Float类型示例" />
    /// </example>
    public override OperateResult<float[]> ReadFloat(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransSingle(m, 0, length));
    }

    /// <summary>
    /// 读取PLC的long类型的数组 -> An array that reads the long type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt64Array" title="Int64类型示例" />
    /// </example>
    public override OperateResult<long[]> ReadInt64(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransInt64(m, 0, length));
    }

    /// <summary>
    /// 读取PLC的ulong类型的数组 -> An array that reads the ULONG type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt64Array" title="UInt64类型示例" />
    /// </example>
    public override OperateResult<ulong[]> ReadUInt64(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransUInt64(m, 0, length));
    }

    /// <summary>
    /// 读取PLC的double类型的数组 -> An array that reads the double type of the PLC
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="length">数组长度 -> Array length </param>
    /// <returns>带有结果对象的结果数据 -> Result data with result info </returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadDoubleArray" title="Double类型示例" />
    /// </example>
    public override OperateResult<double[]> ReadDouble(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransDouble(m, 0, length));
    }

    /// <summary>
    /// 使用指定的类型写入指定的节点数据 -> Writes the specified node data with the specified type
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="typeCode">类型代码，详细参见<see cref="AllenBradleyHelper"/>上的常用字段 ->  Type code, see the commonly used Fields section on the <see cref= "AllenBradleyHelper"/> in detail</param>
    /// <param name="value">实际的数据值 -> The actual data value </param>
    /// <param name="length">如果节点是数组，就是数组长度 -> If the node is an array, it is the array length </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    public OperateResult WriteTag(string address, ushort typeCode, byte[] value, int length = 1) {
        OperateResult<byte[]> command = this.BuildWriteCommand(address, typeCode, value, length);
        if (!command.IsSuccess)
            return command;

        OperateResult<byte[]> read = this.ReadFromCoreServer(command.Content);
        if (!read.IsSuccess)
            return read;

        OperateResult check = this.CheckResponse(read.Content);
        if (!check.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(check);

        return AllenBradleyHelper.ExtractActualData(read.Content, false);
    }

    /// <summary>
    /// 向PLC中写入short数组，返回是否写入成功 -> Writes a short array to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="values">实际数据 -> Actual data </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt16Array" title="Int16类型示例" />
    /// </example>
    public override OperateResult Write(string address, short[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_Word, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// 向PLC中写入ushort数组，返回是否写入成功 -> Writes an array of ushort to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="values">实际数据 -> Actual data </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt16Array" title="UInt16类型示例" />
    /// </example>
    public override OperateResult Write(string address, ushort[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_Word, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// 向PLC中写入int数组，返回是否写入成功 -> Writes an int array to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="values">实际数据 -> Actual data </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt32Array" title="Int32类型示例" />
    /// </example>
    public override OperateResult Write(string address, int[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_DWord, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// Writes an array of UINT to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="values">实际数据 -> Actual data </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt32Array" title="UInt32类型示例" />
    /// </example>
    public override OperateResult Write(string address, uint[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_DWord, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// Writes an array of float to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">Name of the node </param>
    /// <param name="values">Actual data </param>
    /// <returns>Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteFloatArray" title="Float类型示例" />
    /// </example>
    public override OperateResult Write(string address, float[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_Real, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// Writes an array of long to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">Name of the node </param>
    /// <param name="values">Actual data </param>
    /// <returns>Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt64Array" title="Int64类型示例" />
    /// </example>
    public override OperateResult Write(string address, long[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_LInt, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// Writes an array of ulong to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">Name of the node </param>
    /// <param name="values">Actual data </param>
    /// <returns>Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt64Array" title="UInt64类型示例" />
    /// </example>
    public override OperateResult Write(string address, ulong[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_LInt, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// Writes an array of double to the PLC to return whether the write was successful
    /// </summary>
    /// <param name="address">Name of the node </param>
    /// <param name="values">Actual data </param>
    /// <returns>Whether to write successfully</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteDoubleArray" title="Double类型示例" />
    /// </example>
    public override OperateResult Write(string address, double[] values) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_Double, this.ByteTransform.TransByte(values), values.Length);
    }

    /// <summary>
    /// 向PLC中写入string数据，返回是否写入成功，该string类型是针对PLC的DINT类型，长度自动扩充到8
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="value">实际数据 -> Actual data </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    public override OperateResult Write(string address, string value) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_DWord, BasicFramework.SoftBasic.ArrayExpandToLength(this.ByteTransform.TransByte(value, Encoding.ASCII), 8));
    }

    /// <summary>
    /// 向PLC中写入bool数据，返回是否写入成功
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="value">实际数据 -> Actual data </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    public override OperateResult Write(string address, bool value) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_Bool, value ? new byte[] { 0xFF, 0xFF } : new byte[] { 0x00, 0x00 });
    }

    /// <summary>
    /// 向PLC中写入byte数据，返回是否写入成功
    /// </summary>
    /// <param name="address">节点的名称 -> Name of the node </param>
    /// <param name="value">实际数据 -> Actual data </param>
    /// <returns>是否写入成功 -> Whether to write successfully</returns>
    public OperateResult Write(string address, byte value) {
        return this.WriteTag(address, AllenBradleyHelper.CIP_Type_Byte, new byte[] { value, 0x00 });
    }

    /// <summary>
    /// 向PLC注册会话ID的报文 ->
    /// Register a message with the PLC for the session ID
    /// </summary>
    /// <returns>报文信息 -> Message information </returns>
    public byte[] RegisterSessionHandle() {
        byte[] commandSpecificData = new byte[] { 0x01, 0x00, 0x00, 0x00, };
        return AllenBradleyHelper.PackRequestHeader(0x65, 0, commandSpecificData);
    }

    /// <summary>
    /// 获取卸载一个已注册的会话的报文 ->
    /// Get a message to uninstall a registered session
    /// </summary>
    /// <returns>字节报文信息 -> BYTE message information </returns>
    public byte[] UnRegisterSessionHandle() {
        return AllenBradleyHelper.PackRequestHeader(0x66, this.SessionHandle, new byte[0]);
    }

    private OperateResult CheckResponse(byte[] response) {
        try {
            int status = this.ByteTransform.TransInt32(response, 8);
            if (status == 0)
                return OperateResult.CreateSuccessResult();

            string msg = string.Empty;
            switch (status) {
                case 0x01:
                    msg = StringResources.Language.AllenBradleySessionStatus01;
                    break;
                case 0x02:
                    msg = StringResources.Language.AllenBradleySessionStatus02;
                    break;
                case 0x03:
                    msg = StringResources.Language.AllenBradleySessionStatus03;
                    break;
                case 0x64:
                    msg = StringResources.Language.AllenBradleySessionStatus64;
                    break;
                case 0x65:
                    msg = StringResources.Language.AllenBradleySessionStatus65;
                    break;
                case 0x69:
                    msg = StringResources.Language.AllenBradleySessionStatus69;
                    break;
                default:
                    msg = StringResources.Language.UnknownError;
                    break;
            }

            return new OperateResult(status, msg);
        }
        catch (Exception ex) {
            return new OperateResult(ex.Message);
        }
    }

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串信息</returns>
    public override string ToString() {
        return $"AllenBradleyNet[{this.IpAddress}:{this.Port}]";
    }
}