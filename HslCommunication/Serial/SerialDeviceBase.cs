﻿using HslCommunication.BasicFramework;
using System.Text;
using HslCommunication.Core.Net;
using HslCommunication.Core.Reflection;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;
#if !NET35
#endif

namespace HslCommunication.Serial;

/// <summary>
/// Base class for specialised serial devices with custom word lengths, byte transformets, etc.
/// </summary>
/// <typeparam name="TTransform">数据解析的规则泛型</typeparam>
public class SerialDeviceBase<TTransform> : SerialBase, IReadWriteNet where TTransform : IByteTransform, new() {
    /// <summary>
    /// 单个数据字节的长度，西门子为2，三菱，欧姆龙，modbusTcp就为1
    /// </summary>
    /// <remarks>对设备来说，一个地址的数据对应的字节数，或是1个字节或是2个字节</remarks>
    protected ushort WordLength { get; set; } = 1;

    /// <summary>
    /// 当前客户端的数据变换机制，当你需要从字节数据转换类型数据的时候需要。
    /// </summary>
    /// <example>
    /// 主要是用来转换数据类型的，下面仅仅演示了2个方法，其他的类型转换，类似处理。
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDoubleBase.cs" region="ByteTransform" title="ByteTransform示例" />
    /// </example>
    public TTransform ByteTransform { get; set; }

    /// <summary>
    /// 当前连接的唯一ID号，默认为长度20的guid码加随机数组成，方便列表管理，也可以自己指定
    /// </summary>
    /// <remarks>
    /// Current Connection ID, conclude guid and random data, also, you can spcified
    /// </remarks>
    public string ConnectionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 默认的构造方法实现的设备信息
    /// </summary>
    public SerialDeviceBase() {
        this.ByteTransform = new TTransform(); // 实例化数据转换规则
    }

    /**************************************************************************************************
     *
     *    说明：子类中需要重写基础的读取和写入方法，来支持不同的数据访问规则
     *
     *    此处没有将读写位纳入进来，因为各种设备的支持不尽相同，比较麻烦
     *
     **************************************************************************************************/

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
    /// 读取自定义类型的数据，需要规定解析规则
    /// </summary>
    /// <typeparam name="T">类型名称</typeparam>
    /// <param name="address">起始地址</param>
    /// <returns>带有成功标识的结果对象</returns>
    /// <remarks>
    /// 需要是定义一个类，选择好相对于的ByteTransform实例，才能调用该方法。
    /// </remarks>
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
    public OperateResult WriteCustomer<T>(string address, T data) where T : IDataTransfer, new() {
        return this.Write(address, data.ToSource());
    }

    /// <summary>
    /// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
    /// </summary>
    /// <typeparam name="T">自定义的数据类型对象</typeparam>
    /// <returns>包含是否成功的结果对象</returns>
    public OperateResult<T> Read<T>() where T : class, new() {
        return HslReflectionHelper.Read<T>(this);
    }

    /// <summary>
    /// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
    /// </summary>
    /// <typeparam name="T">自定义的数据类型对象</typeparam>
    /// <returns>包含是否成功的结果对象</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public OperateResult Write<T>(T data) where T : class, new() {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        return HslReflectionHelper.Write<T>(data, this);
    }

    /// <summary>
    /// 读取设备的short类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<short> ReadInt16(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadInt16(address, 1));
    }

    /// <summary>
    /// 读取设备的short类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<short[]> ReadInt16(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength)), m => this.ByteTransform.TransInt16(m, 0, length));
    }

    /// <summary>
    /// 读取设备的ushort数据类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<ushort> ReadUInt16(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadUInt16(address, 1));
    }

    /// <summary>
    /// 读取设备的ushort类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<ushort[]> ReadUInt16(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength)), m => this.ByteTransform.TransUInt16(m, 0, length));
    }

    /// <summary>
    /// 读取设备的int类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<int> ReadInt32(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadInt32(address, 1));
    }

    /// <summary>
    /// 读取设备的int类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<int[]> ReadInt32(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 2)), m => this.ByteTransform.TransInt32(m, 0, length));
    }

    /// <summary>
    /// 读取设备的uint类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<uint> ReadUInt32(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadUInt32(address, 1));
    }

    /// <summary>
    /// 读取设备的uint类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<uint[]> ReadUInt32(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 2)), m => this.ByteTransform.TransUInt32(m, 0, length));
    }

    /// <summary>
    /// 读取设备的float类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<float> ReadFloat(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadFloat(address, 1));
    }


    /// <summary>
    /// 读取设备的float类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<float[]> ReadFloat(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 2)), m => this.ByteTransform.TransSingle(m, 0, length));
    }

    /// <summary>
    /// 读取设备的long类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<long> ReadInt64(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadInt64(address, 1));
    }

    /// <summary>
    /// 读取设备的long类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<long[]> ReadInt64(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 4)), m => this.ByteTransform.TransInt64(m, 0, length));
    }

    /// <summary>
    /// 读取设备的ulong类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<ulong> ReadUInt64(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadUInt64(address, 1));
    }

    /// <summary>
    /// 读取设备的ulong类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<ulong[]> ReadUInt64(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 4)), m => this.ByteTransform.TransUInt64(m, 0, length));
    }

    /// <summary>
    /// 读取设备的double类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<double> ReadDouble(string address) {
        return ByteTransformHelper.GetResultFromArray(this.ReadDouble(address, 1));
    }

    /// <summary>
    /// 读取设备的double类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<double[]> ReadDouble(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, (ushort) (length * this.WordLength * 4)), m => this.ByteTransform.TransDouble(m, 0, length));
    }

    /// <summary>
    /// 读取设备的字符串数据，编码为ASCII
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">地址长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    public OperateResult<string> ReadString(string address, ushort length) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransString(m, 0, m.Length, Encoding.ASCII));
    }

    // Bool类型的读写，不一定所有的设备都实现，比如西门子，就没有实现bool[]的读写，Siemens的fetch/write没有实现bool操作

    /// <summary>
    /// 批量读取底层的数据信息，需要指定地址和长度，具体的结果取决于实现
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="length">数据长度</param>
    /// <returns>带有成功标识的bool[]数组</returns>
    public virtual OperateResult<bool[]> ReadBool(string address, ushort length) {
        return new OperateResult<bool[]>(StringResources.Language.NotSupportedFunction);
    }

    /// <summary>
    /// 读取底层的bool数据信息，具体的结果取决于实现
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <returns>带有成功标识的bool数组</returns>
    public virtual OperateResult<bool> ReadBool(string address) {
        OperateResult<bool[]> read = this.ReadBool(address, 1);
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<bool>(read);

        return OperateResult.CreateSuccessResult(read.Content[0]);
    }

    /// <summary>
    /// 写入bool数组数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="value">写入值</param>
    /// <returns>带有成功标识的结果类对象</returns>
    public virtual OperateResult Write(string address, bool[] value) {
        return new OperateResult(StringResources.Language.NotSupportedFunction);
    }

    /// <summary>
    /// 写入bool数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="value">写入值</param>
    /// <returns>带有成功标识的结果类对象</returns>
    public virtual OperateResult Write(string address, bool value) {
        return this.Write(address, new bool[] { value });
    }

    /// <summary>
    /// 向设备中写入short数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, short[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入short数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, short value) {
        return this.Write(address, new short[] { value });
    }

    /// <summary>
    /// 向设备中写入ushort数组，返回是否写入成功
    /// </summary>
    /// <param name="address">要写入的数据地址</param>
    /// <param name="values">要写入的实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, ushort[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }


    /// <summary>
    /// 向设备中写入ushort数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, ushort value) {
        return this.Write(address, new ushort[] { value });
    }

    /// <summary>
    /// 向设备中写入int数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, int[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入int数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, int value) {
        return this.Write(address, new int[] { value });
    }

    /// <summary>
    /// 向设备中写入uint数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, uint[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入uint数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, uint value) {
        return this.Write(address, new uint[] { value });
    }

    /// <summary>
    /// 向设备中写入float数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, float[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入float数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, float value) {
        return this.Write(address, new float[] { value });
    }

    /// <summary>
    /// 向设备中写入long数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, long[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入long数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, long value) {
        return this.Write(address, new long[] { value });
    }

    /// <summary>
    /// 向P设备中写入ulong数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, ulong[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入ulong数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, ulong value) {
        return this.Write(address, new ulong[] { value });
    }

    /// <summary>
    /// 向设备中写入double数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, double[] values) {
        return this.Write(address, this.ByteTransform.TransByte(values));
    }

    /// <summary>
    /// 向设备中写入double数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    public virtual OperateResult Write(string address, double value) {
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
}