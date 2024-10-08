﻿using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Reflection;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;
#if !NET35
#endif

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 设备类的基类，提供了基础的字节读写方法
/// </summary>
/// <typeparam name="TNetMessage">指定了消息的解析规则</typeparam>
/// <typeparam name="TTransform">指定了数据转换的规则</typeparam>
/// <remarks>需要继承实现采用使用。</remarks>
public class NetworkDeviceBase<TNetMessage, TTransform> : NetworkDoubleBase<TNetMessage, TTransform>, IReadWriteNet where TNetMessage : INetMessage, new() where TTransform : IByteTransform, new() {
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
    /// 单个数据字节的长度，西门子为2，三菱，欧姆龙，modbusTcp就为1，AB PLC无效
    /// </summary>
    /// <remarks>对设备来说，一个地址的数据对应的字节数，或是1个字节或是2个字节</remarks>
    protected ushort WordLength { get; set; } = 1;

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
    /// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
    /// </summary>
    /// <typeparam name="T">自定义的数据类型对象</typeparam>
    /// <returns>包含是否成功的结果对象</returns>
    /// <example>
    /// 此处演示西门子的读取示例，先定义一个类，重点是将需要读取的数据，写入到属性的特性中去。
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ObjectDefineExample" title="特性实现示例" />
    /// 接下来就可以实现数据的读取了
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadObjectExample" title="ReadObject示例" />
    /// </example>
    public OperateResult<T> Read<T>() where T : class, new() {
        return HslReflectionHelper.Read<T>(this);
    }

    /// <summary>
    /// 从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
    /// </summary>
    /// <typeparam name="T">自定义的数据类型对象</typeparam>
    /// <returns>包含是否成功的结果对象</returns>
    /// <example>
    /// 此处演示西门子的读取示例，先定义一个类，重点是将需要读取的数据，写入到属性的特性中去。
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ObjectDefineExample" title="特性实现示例" />
    /// 接下来就可以实现数据的写入了
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteObjectExample" title="WriteObject示例" />
    /// </example>
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
    /// 读取设备的字符串数据，编码为指定的编码信息
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">地址长度</param>
    /// <param name="encoding">编码机制</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadString" title="String类型示例" />
    /// </example>
    public OperateResult<string> ReadString(string address, ushort length, Encoding encoding) {
        return ByteTransformHelper.GetResultFromBytes(this.Read(address, length), m => this.ByteTransform.TransString(m, 0, m.Length, encoding));
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

#if !NET35

    /// <summary>
    /// 批量读取底层的数据信息，需要指定地址和长度，具体的结果取决于实现
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="length">数据长度</param>
    /// <returns>带有成功标识的bool[]数组</returns>
    public Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length) {
        return Task.Run(() => new OperateResult<bool[]>(StringResources.Language.NotSupportedFunction));
    }

    /// <summary>
    /// 读取底层的bool数据信息，具体的结果取决于实现
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <returns>带有成功标识的bool数组</returns>
    public Task<OperateResult<bool>> ReadBoolAsync(string address) {
        return Task.Run(() => new OperateResult<bool>(StringResources.Language.NotSupportedFunction));
    }

    /// <summary>
    /// 写入bool数组数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="value">写入值</param>
    /// <returns>带有成功标识的结果类对象</returns>
    public Task<OperateResult> WriteAsync(string address, bool[] value) {
        return Task.Run(() => new OperateResult(StringResources.Language.NotSupportedFunction));
    }

    /// <summary>
    /// 写入bool数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="value">写入值</param>
    /// <returns>带有成功标识的结果类对象</returns>
    public Task<OperateResult> WriteAsync(string address, bool value) {
        return Task.Run(() => new OperateResult(StringResources.Language.NotSupportedFunction));
    }

    /// <summary>
    /// 使用异步的操作从原始的设备中读取数据信息
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">地址长度</param>
    /// <returns>带有成功标识的结果对象</returns>
    public Task<OperateResult<byte[]>> ReadAsync(string address, ushort length) {
        return Task.Run(() => this.Read(address, length));
    }

    /// <summary>
    /// 异步读取设备的short类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt16Async" title="Int16类型示例" />
    /// </example>
    public Task<OperateResult<short>> ReadInt16Async(string address) {
        return Task.Run(() => this.ReadInt16(address));
    }

    /// <summary>
    /// 异步读取设备的ushort类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt16ArrayAsync" title="Int16类型示例" />
    /// </example>
    public Task<OperateResult<short[]>> ReadInt16Async(string address, ushort length) {
        return Task.Run(() => this.ReadInt16(address, length));
    }


    /// <summary>
    /// 异步读取设备的ushort数据类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt16Async" title="UInt16类型示例" />
    /// </example>
    public Task<OperateResult<ushort>> ReadUInt16Async(string address) {
        return Task.Run(() => this.ReadUInt16(address));
    }

    /// <summary>
    /// 异步读取设备的ushort类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt16ArrayAsync" title="UInt16类型示例" />
    /// </example>
    public Task<OperateResult<ushort[]>> ReadUInt16Async(string address, ushort length) {
        return Task.Run(() => this.ReadUInt16(address, length));
    }

    /// <summary>
    /// 异步读取设备的int类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt32Async" title="Int32类型示例" />
    /// </example>
    public Task<OperateResult<int>> ReadInt32Async(string address) {
        return Task.Run(() => this.ReadInt32(address));
    }

    /// <summary>
    /// 异步读取设备的int类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt32ArrayAsync" title="Int32类型示例" />
    /// </example>
    public Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length) {
        return Task.Run(() => this.ReadInt32(address, length));
    }

    /// <summary>
    /// 异步读取设备的uint类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt32Async" title="UInt32类型示例" />
    /// </example>
    public Task<OperateResult<uint>> ReadUInt32Async(string address) {
        return Task.Run(() => this.ReadUInt32(address));
    }

    /// <summary>
    /// 异步读取设备的uint类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt32ArrayAsync" title="UInt32类型示例" />
    /// </example>
    public Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length) {
        return Task.Run(() => this.ReadUInt32(address, length));
    }

    /// <summary>
    /// 异步读取设备的float类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadFloatAsync" title="Float类型示例" />
    /// </example>
    public Task<OperateResult<float>> ReadFloatAsync(string address) {
        return Task.Run(() => this.ReadFloat(address));
    }

    /// <summary>
    /// 异步读取设备的float类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadFloatArrayAsync" title="Float类型示例" />
    /// </example>
    public Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length) {
        return Task.Run(() => this.ReadFloat(address, length));
    }

    /// <summary>
    /// 异步读取设备的long类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt64Async" title="Int64类型示例" />
    /// </example>
    public Task<OperateResult<long>> ReadInt64Async(string address) {
        return Task.Run(() => this.ReadInt64(address));
    }

    /// <summary>
    /// 异步读取设备的long类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadInt64ArrayAsync" title="Int64类型示例" />
    /// </example>
    public Task<OperateResult<long[]>> ReadInt64Async(string address, ushort length) {
        return Task.Run(() => this.ReadInt64(address, length));
    }

    /// <summary>
    /// 异步读取设备的ulong类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt64Async" title="UInt64类型示例" />
    /// </example>
    public Task<OperateResult<ulong>> ReadUInt64Async(string address) {
        return Task.Run(() => this.ReadUInt64(address));
    }

    /// <summary>
    /// 异步读取设备的ulong类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadUInt64ArrayAsync" title="UInt64类型示例" />
    /// </example>
    public Task<OperateResult<ulong[]>> ReadUInt64Async(string address, ushort length) {
        return Task.Run(() => this.ReadUInt64(address, length));
    }

    /// <summary>
    /// 异步读取设备的double类型的数据
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadDoubleAsync" title="Double类型示例" />
    /// </example>
    public Task<OperateResult<double>> ReadDoubleAsync(string address) {
        return Task.Run(() => this.ReadDouble(address));
    }

    /// <summary>
    /// 异步读取设备的double类型的数组
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">数组长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadDoubleArrayAsync" title="Double类型示例" />
    /// </example>
    public Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length) {
        return Task.Run(() => this.ReadDouble(address, length));
    }

    /// <summary>
    /// 异步读取设备的字符串数据，编码为ASCII
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">地址长度</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadStringAsync" title="String类型示例" />
    /// </example>
    public Task<OperateResult<string>> ReadStringAsync(string address, ushort length) {
        return Task.Run(() => this.ReadString(address, length));
    }

    /// <summary>
    /// 读取设备的字符串数据，编码为指定的编码信息
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="length">地址长度</param>
    /// <param name="encoding">编码机制</param>
    /// <returns>带成功标志的结果数据对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadStringAsync" title="String类型示例" />
    /// </example>
    public Task<OperateResult<string>> ReadStringAsync(string address, ushort length, Encoding encoding) {
        return Task.Run(() => this.ReadString(address, length, encoding));
    }

    /// <summary>
    /// 异步将原始数据写入设备
    /// </summary>
    /// <param name="address">起始地址</param>
    /// <param name="value">原始数据</param>
    /// <returns>带有成功标识的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteAsync" title="bytes类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, byte[] value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入short数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt16ArrayAsync" title="Int16类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, short[] values) {
        return Task.Run(() => this.Write(address, values));
    }

    /// <summary>
    /// 异步向设备中写入short数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt16Async" title="Int16类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, short value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入ushort数组，返回是否写入成功
    /// </summary>
    /// <param name="address">要写入的数据地址</param>
    /// <param name="values">要写入的实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt16ArrayAsync" title="UInt16类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, ushort[] values) {
        return Task.Run(() => this.Write(address, values));
    }


    /// <summary>
    /// 异步向设备中写入ushort数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt16Async" title="UInt16类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, ushort value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入int数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt32ArrayAsync" title="Int32类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, int[] values) {
        return Task.Run(() => this.Write(address, values));
    }

    /// <summary>
    /// 异步向设备中写入int数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt32Async" title="Int32类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, int value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入uint数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt32ArrayAsync" title="UInt32类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, uint[] values) {
        return Task.Run(() => this.Write(address, values));
    }

    /// <summary>
    /// 异步向设备中写入uint数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt32Async" title="UInt32类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, uint value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入float数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>返回写入结果</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteFloatArrayAsync" title="Float类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, float[] values) {
        return Task.Run(() => this.Write(address, values));
    }

    /// <summary>
    /// 异步向设备中写入float数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>返回写入结果</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteFloatAsync" title="Float类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, float value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入long数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt64ArrayAsync" title="Int64类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, long[] values) {
        return Task.Run(() => this.Write(address, values));
    }

    /// <summary>
    /// 异步向设备中写入long数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteInt64Async" title="Int64类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, long value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向P设备中写入ulong数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt64ArrayAsync" title="UInt64类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, ulong[] values) {
        return Task.Run(() => this.Write(address, values));
    }

    /// <summary>
    /// 异步向设备中写入ulong数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteUInt64Async" title="UInt64类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, ulong value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入double数组，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="values">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteDoubleArrayAsync" title="Double类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, double[] values) {
        return Task.Run(() => this.Write(address, values));
    }

    /// <summary>
    /// 异步向设备中写入double数据，返回是否写入成功
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">实际数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteDoubleAsync" title="Double类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, double value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入字符串，编码格式为ASCII
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteStringAsync" title="String类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, string value) {
        return Task.Run(() => this.Write(address, value));
    }

    /// <summary>
    /// 异步向设备中写入字符串，使用指定的字符编码
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="encoding">字符编码</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteStringAsync" title="String类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, string value, Encoding encoding) {
        return Task.Run(() => this.Write(address, value, encoding));
    }

    /// <summary>
    /// 异步向设备中写入指定长度的字符串,超出截断，不够补0，编码格式为ASCII
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="length">指定的字符串长度，必须大于0</param>
    /// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteString2Async" title="String类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, string value, int length) {
        return Task.Run(() => this.Write(address, value, length));
    }

    /// <summary>
    /// 异步向设备中写入指定长度的字符串,超出截断，不够补0，指定的编码格式
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="length">指定的字符串长度，必须大于0</param>
    /// <param name="encoding">指定的编码格式</param>
    /// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteString2Async" title="String类型示例" />
    /// </example>
    public Task<OperateResult> WriteAsync(string address, string value, int length, Encoding encoding) {
        return Task.Run(() => this.Write(address, value, length, encoding));
    }

    /// <summary>
    /// 异步向设备中写入字符串，编码格式为Unicode
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <returns>是否写入成功的结果对象</returns>
    public Task<OperateResult> WriteUnicodeStringAsync(string address, string value) {
        return Task.Run(() => this.WriteUnicodeString(address, value));
    }

    /// <summary>
    /// 异步向设备中写入指定长度的字符串,超出截断，不够补0，编码格式为Unicode
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="length">指定的字符串长度，必须大于0</param>
    /// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
    public Task<OperateResult> WriteUnicodeStringAsync(string address, string value, int length) {
        return Task.Run(() => this.WriteUnicodeString(address, value, length));
    }

    /// <summary>
    /// 异步读取自定义类型的数据，需要规定解析规则
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
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadCustomerAsyncExample" title="ReadCustomerAsync示例" />
    /// </example>
    public Task<OperateResult<T>> ReadCustomerAsync<T>(string address) where T : IDataTransfer, new() {
        return Task.Run(() => this.ReadCustomer<T>(address));
    }

    /// <summary>
    /// 异步写入自定义类型的数据到设备去，需要规定生成字节的方法
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
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteCustomerAsyncExample" title="WriteCustomerAsync示例" />
    /// </example>
    public Task<OperateResult> WriteCustomerAsync<T>(string address, T data) where T : IDataTransfer, new() {
        return Task.Run(() => this.WriteCustomer(address, data));
    }

    /// <summary>
    /// 异步从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
    /// </summary>
    /// <typeparam name="T">自定义的数据类型对象</typeparam>
    /// <returns>包含是否成功的结果对象</returns>
    /// <example>
    /// 此处演示西门子的读取示例，先定义一个类，重点是将需要读取的数据，写入到属性的特性中去。
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ObjectDefineExample" title="特性实现示例" />
    /// 接下来就可以实现数据的读取了
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadObjectAsyncExample" title="ReadObjectAsync示例" />
    /// </example>
    public Task<OperateResult<T>> ReadAsync<T>() where T : class, new() {
        return Task.Run(() => HslReflectionHelper.Read<T>(this));
    }

    /// <summary>
    /// 异步从设备里读取支持Hsl特性的数据内容，该特性为<see cref="HslDeviceAddressAttribute"/>，详细参考论坛的操作说明。
    /// </summary>
    /// <typeparam name="T">自定义的数据类型对象</typeparam>
    /// <returns>包含是否成功的结果对象</returns>
    /// <example>
    /// 此处演示西门子的读取示例，先定义一个类，重点是将需要读取的数据，写入到属性的特性中去。
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ObjectDefineExample" title="特性实现示例" />
    /// 接下来就可以实现数据的写入了
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteObjectAsyncExample" title="WriteObjectAsync示例" />
    /// </example>
    /// <exception cref="ArgumentNullException"></exception>
    public Task<OperateResult> WriteAsync<T>(T data) where T : class, new() {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        return Task.Run(() => HslReflectionHelper.Write<T>(data, this));
    }
#endif

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
        return this.Write(address, value, Encoding.ASCII);
    }

    /// <summary>
    /// 向设备中写入指定编码的字符串
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="encoding">字节编码</param>
    /// <returns>是否写入成功的结果对象</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteString" title="String类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, string value, Encoding encoding) {
        byte[] temp = this.ByteTransform.TransByte(value, encoding);
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
        return this.Write(address, value, length, Encoding.ASCII);
    }

    /// <summary>
    /// 向设备中写入指定长度并且指定编码的字符串,超出截断，不够补0
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <param name="value">字符串数据</param>
    /// <param name="length">指定的长度，按照转换后的字节计算</param>
    /// <param name="encoding">字符编码</param>
    /// <returns>是否写入成功的结果对象 -> Whether to write a successful result object</returns>
    /// <example>
    /// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
    /// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="WriteString2" title="String类型示例" />
    /// </example>
    public virtual OperateResult Write(string address, string value, int length, Encoding encoding) {
        byte[] temp = this.ByteTransform.TransByte(value, encoding);
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
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串数据</returns>
    public override string ToString() {
        return $"NetworkDeviceBase<{typeof(TNetMessage)}, {typeof(TTransform)}>[{this.IpAddress}:{this.Port}]";
    }
}