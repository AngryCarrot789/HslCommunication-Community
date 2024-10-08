﻿using HslCommunication.Core.Thread;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;

namespace HslCommunication.BasicFramework;

/// <summary>
/// A thread-safe buffer that supports batch modification and snapshotting
/// </summary>
/// <remarks>
/// What functions can this class achieve? You have a large array as the intermediate
/// data pool of your application, allowing you to store sub-byte[] arrays of specified
/// length in the byte[] array, and also allowing you to get data from it.
/// These operations are thread-safe. Of course, this class extends some additional method
/// support, and can also directly assign or get basic data type objects.
/// </remarks>
public class SoftBuffer : IDisposable {
    /// <summary>
    /// 使用默认的大小初始化缓存空间
    /// </summary>
    public SoftBuffer() {
        this.buffer = new byte[this.capacity];
        this.hybirdLock = new SimpleHybirdLock();
        this.byteTransform = new RegularByteTransform();
    }

    /// <summary>
    /// 使用指定的容量初始化缓存数据块
    /// </summary>
    /// <param name="capacity">初始化的容量</param>
    public SoftBuffer(int capacity) {
        this.buffer = new byte[capacity];
        this.capacity = capacity;
        this.hybirdLock = new SimpleHybirdLock();
        this.byteTransform = new RegularByteTransform();
    }

    /// <summary>
    /// 设置指定的位置的数据块，如果超出，则丢弃数据
    /// </summary>
    /// <param name="value">bool值</param>
    /// <param name="destIndex">目标存储的索引</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void SetBool(bool value, int destIndex) {
        this.SetBool(new bool[] { value }, destIndex);
    }

    /// <summary>
    /// 设置指定的位置的数据块，如果超出，则丢弃数据
    /// </summary>
    /// <param name="value">bool数组值</param>
    /// <param name="destIndex">目标存储的索引</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void SetBool(bool[] value, int destIndex) {
        if (value != null) {
            try {
                this.hybirdLock.Enter();
                for (int i = 0; i < value.Length; i++) {
                    int byteIndex = (destIndex + i) / 8;
                    int offect = (destIndex + i) % 8;

                    if (value[i]) {
                        this.buffer[byteIndex] = (byte) (this.buffer[byteIndex] | this.getOrByte(offect));
                    }
                    else {
                        this.buffer[byteIndex] = (byte) (this.buffer[byteIndex] & this.getAndByte(offect));
                    }
                }

                this.hybirdLock.Leave();
            }
            catch {
                this.hybirdLock.Leave();
                throw;
            }
        }
    }

    /// <summary>
    /// 获取指定的位置的bool值，如果超出，则引发异常
    /// </summary>
    /// <param name="destIndex">目标存储的索引</param>
    /// <returns>获取索引位置的bool数据值</returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public bool GetBool(int destIndex) {
        return this.GetBool(destIndex, 1)[0];
    }

    /// <summary>
    /// 获取指定位置的bool数组值，如果超过，则引发异常
    /// </summary>
    /// <param name="destIndex">目标存储的索引</param>
    /// <param name="length">读取的数组长度</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    /// <returns>bool数组值</returns>
    public bool[] GetBool(int destIndex, int length) {
        bool[] result = new bool[length];
        try {
            this.hybirdLock.Enter();
            for (int i = 0; i < length; i++) {
                int byteIndex = (destIndex + i) / 8;
                int offect = (destIndex + i) % 8;

                result[i] = (this.buffer[byteIndex] & this.getOrByte(offect)) == this.getOrByte(offect);
            }

            this.hybirdLock.Leave();
        }
        catch {
            this.hybirdLock.Leave();
            throw;
        }

        return result;
    }

    private byte getAndByte(int offect) {
        switch (offect) {
            case 0: return 0xFE;
            case 1: return 0xFD;
            case 2: return 0xFB;
            case 3: return 0xF7;
            case 4: return 0xEF;
            case 5: return 0xDF;
            case 6: return 0xBF;
            case 7: return 0x7F;
            default: return 0xFF;
        }
    }


    private byte getOrByte(int offect) {
        switch (offect) {
            case 0: return 0x01;
            case 1: return 0x02;
            case 2: return 0x04;
            case 3: return 0x08;
            case 4: return 0x10;
            case 5: return 0x20;
            case 6: return 0x40;
            case 7: return 0x80;
            default: return 0x00;
        }
    }

    /// <summary>
    /// 设置指定的位置的数据块，如果超出，则丢弃数据
    /// </summary>
    /// <param name="data">数据块信息</param>
    /// <param name="destIndex">目标存储的索引</param>
    public void SetBytes(byte[] data, int destIndex) {
        if (destIndex < this.capacity && destIndex >= 0 && data != null) {
            this.hybirdLock.Enter();

            if ((data.Length + destIndex) > this.buffer.Length) {
                Array.Copy(data, 0, this.buffer, destIndex, (this.buffer.Length - destIndex));
            }
            else {
                data.CopyTo(this.buffer, destIndex);
            }

            this.hybirdLock.Leave();
        }
    }

    /// <summary>
    /// 设置指定的位置的数据块，如果超出，则丢弃数据
    /// </summary>
    /// <param name="data">数据块信息</param>
    /// <param name="destIndex">目标存储的索引</param>
    /// <param name="length">准备拷贝的数据长度</param>
    public void SetBytes(byte[] data, int destIndex, int length) {
        if (destIndex < this.capacity && destIndex >= 0 && data != null) {
            if (length > data.Length)
                length = data.Length;

            this.hybirdLock.Enter();

            if ((length + destIndex) > this.buffer.Length) {
                Array.Copy(data, 0, this.buffer, destIndex, (this.buffer.Length - destIndex));
            }
            else {
                Array.Copy(data, 0, this.buffer, destIndex, length);
            }

            this.hybirdLock.Leave();
        }
    }

    /// <summary>
    /// 设置指定的位置的数据块，如果超出，则丢弃数据
    /// </summary>
    /// <param name="data">数据块信息</param>
    /// <param name="sourceIndex">Data中的起始位置</param>
    /// <param name="destIndex">目标存储的索引</param>
    /// <param name="length">准备拷贝的数据长度</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void SetBytes(byte[] data, int sourceIndex, int destIndex, int length) {
        if (destIndex < this.capacity && destIndex >= 0 && data != null) {
            if (length > data.Length)
                length = data.Length;

            this.hybirdLock.Enter();

            Array.Copy(data, sourceIndex, this.buffer, destIndex, length);

            this.hybirdLock.Leave();
        }
    }

    /// <summary>
    /// 获取内存指定长度的数据信息
    /// </summary>
    /// <param name="index">起始位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>返回实际的数据信息</returns>
    public byte[] GetBytes(int index, int length) {
        byte[] result = new byte[length];
        if (length > 0) {
            this.hybirdLock.Enter();
            if (index >= 0 && (index + length) <= this.buffer.Length) {
                Array.Copy(this.buffer, index, result, 0, length);
            }

            this.hybirdLock.Leave();
        }

        return result;
    }

    /// <summary>
    /// 获取内存所有的数据信息
    /// </summary>
    /// <returns>实际的数据信息</returns>
    public byte[] GetBytes() {
        return this.GetBytes(0, this.capacity);
    }

    /// <summary>
    /// 设置byte类型的数据到缓存区
    /// </summary>
    /// <param name="value">byte数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(byte value, int index) {
        this.SetBytes(new byte[] { value }, index);
    }

    /// <summary>
    /// 设置short类型的数据到缓存区
    /// </summary>
    /// <param name="values">short数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(short[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置short类型的数据到缓存区
    /// </summary>
    /// <param name="value">short数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(short value, int index) {
        this.SetValue(new short[] { value }, index);
    }

    /// <summary>
    /// 设置ushort类型的数据到缓存区
    /// </summary>
    /// <param name="values">ushort数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(ushort[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置ushort类型的数据到缓存区
    /// </summary>
    /// <param name="value">ushort数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(ushort value, int index) {
        this.SetValue(new ushort[] { value }, index);
    }

    /// <summary>
    /// 设置int类型的数据到缓存区
    /// </summary>
    /// <param name="values">int数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(int[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置int类型的数据到缓存区
    /// </summary>
    /// <param name="value">int数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(int value, int index) {
        this.SetValue(new int[] { value }, index);
    }

    /// <summary>
    /// 设置uint类型的数据到缓存区
    /// </summary>
    /// <param name="values">uint数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(uint[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置uint类型的数据到缓存区
    /// </summary>
    /// <param name="value">uint数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(uint value, int index) {
        this.SetValue(new uint[] { value }, index);
    }

    /// <summary>
    /// 设置float类型的数据到缓存区
    /// </summary>
    /// <param name="values">float数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(float[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置float类型的数据到缓存区
    /// </summary>
    /// <param name="value">float数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(float value, int index) {
        this.SetValue(new float[] { value }, index);
    }

    /// <summary>
    /// 设置long类型的数据到缓存区
    /// </summary>
    /// <param name="values">long数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(long[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置long类型的数据到缓存区
    /// </summary>
    /// <param name="value">long数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(long value, int index) {
        this.SetValue(new long[] { value }, index);
    }

    /// <summary>
    /// 设置ulong类型的数据到缓存区
    /// </summary>
    /// <param name="values">ulong数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(ulong[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置ulong类型的数据到缓存区
    /// </summary>
    /// <param name="value">ulong数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(ulong value, int index) {
        this.SetValue(new ulong[] { value }, index);
    }

    /// <summary>
    /// 设置double类型的数据到缓存区
    /// </summary>
    /// <param name="values">double数组</param>
    /// <param name="index">索引位置</param>
    public void SetValue(double[] values, int index) {
        this.SetBytes(this.byteTransform.TransByte(values), index);
    }

    /// <summary>
    /// 设置double类型的数据到缓存区
    /// </summary>
    /// <param name="value">double数值</param>
    /// <param name="index">索引位置</param>
    public void SetValue(double value, int index) {
        this.SetValue(new double[] { value }, index);
    }

    /// <summary>
    /// 获取byte类型的数据
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>byte数值</returns>
    public byte GetByte(int index) {
        return this.GetBytes(index, 1)[0];
    }

    /// <summary>
    /// 获取short类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>short数组</returns>
    public short[] GetInt16(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 2);
        return this.byteTransform.TransInt16(tmp, 0, length);
    }

    /// <summary>
    /// 获取short类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>short数据</returns>
    public short GetInt16(int index) {
        return this.GetInt16(index, 1)[0];
    }

    /// <summary>
    /// 获取ushort类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>ushort数组</returns>
    public ushort[] GetUInt16(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 2);
        return this.byteTransform.TransUInt16(tmp, 0, length);
    }

    /// <summary>
    /// 获取ushort类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>ushort数据</returns>
    public ushort GetUInt16(int index) {
        return this.GetUInt16(index, 1)[0];
    }

    /// <summary>
    /// 获取int类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>int数组</returns>
    public int[] GetInt32(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 4);
        return this.byteTransform.TransInt32(tmp, 0, length);
    }

    /// <summary>
    /// 获取int类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>int数据</returns>
    public int GetInt32(int index) {
        return this.GetInt32(index, 1)[0];
    }

    /// <summary>
    /// 获取uint类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>uint数组</returns>
    public uint[] GetUInt32(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 4);
        return this.byteTransform.TransUInt32(tmp, 0, length);
    }

    /// <summary>
    /// 获取uint类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>uint数据</returns>
    public uint GetUInt32(int index) {
        return this.GetUInt32(index, 1)[0];
    }

    /// <summary>
    /// 获取float类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>float数组</returns>
    public float[] GetSingle(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 4);
        return this.byteTransform.TransSingle(tmp, 0, length);
    }

    /// <summary>
    /// 获取float类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>float数据</returns>
    public float GetSingle(int index) {
        return this.GetSingle(index, 1)[0];
    }

    /// <summary>
    /// 获取long类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>long数组</returns>
    public long[] GetInt64(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 8);
        return this.byteTransform.TransInt64(tmp, 0, length);
    }

    /// <summary>
    /// 获取long类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>long数据</returns>
    public long GetInt64(int index) {
        return this.GetInt64(index, 1)[0];
    }

    /// <summary>
    /// 获取ulong类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>ulong数组</returns>
    public ulong[] GetUInt64(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 8);
        return this.byteTransform.TransUInt64(tmp, 0, length);
    }

    /// <summary>
    /// 获取ulong类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>ulong数据</returns>
    public ulong GetUInt64(int index) {
        return this.GetUInt64(index, 1)[0];
    }

    /// <summary>
    /// 获取double类型的数组到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <param name="length">数组长度</param>
    /// <returns>ulong数组</returns>
    public double[] GetDouble(int index, int length) {
        byte[] tmp = this.GetBytes(index, length * 8);
        return this.byteTransform.TransDouble(tmp, 0, length);
    }

    /// <summary>
    /// 获取double类型的数据到缓存区
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>double数据</returns>
    public double GetDouble(int index) {
        return this.GetDouble(index, 1)[0];
    }

    /// <summary>
    /// 读取自定义类型的数据，需要规定解析规则
    /// </summary>
    /// <typeparam name="T">类型名称</typeparam>
    /// <param name="index">起始索引</param>
    /// <returns>自定义的数据类型</returns>
    public T GetCustomer<T>(int index) where T : IDataTransfer, new() {
        T Content = new T();
        byte[] read = this.GetBytes(index, Content.ReadCount);
        Content.ParseSource(read);
        return Content;
    }

    /// <summary>
    /// 写入自定义类型的数据到缓存中去，需要规定生成字节的方法
    /// </summary>
    /// <typeparam name="T">自定义类型</typeparam>
    /// <param name="data">实例对象</param>
    /// <param name="index">起始地址</param>
    public void SetCustomer<T>(T data, int index) where T : IDataTransfer, new() {
        this.SetBytes(data.ToSource(), index);
    }

    /// <summary>
    /// 获取或设置当前的数据缓存类的解析规则
    /// </summary>
    public IByteTransform ByteTransform {
        get => this.byteTransform;
        set => this.byteTransform = value;
    }

    private int capacity = 10; // 缓存的容量
    private byte[] buffer; // 缓存的数据
    private SimpleHybirdLock hybirdLock; // 高效的混合锁
    private IByteTransform byteTransform; // 数据转换类

    private bool disposedValue = false; // 要检测冗余调用

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing) {
        if (!this.disposedValue) {
            if (disposing) {
                // TODO: 释放托管状态(托管对象)。
                this.hybirdLock?.Dispose();
                this.buffer = null;
            }

            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
            // TODO: 将大型字段设置为 null。

            this.disposedValue = true;
        }
    }

    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    // ~SoftBuffer()
    // {
    //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //   Dispose(false);
    // }

    // 添加此代码以正确实现可处置模式。

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    public void Dispose() {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        this.Dispose(true);
        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
        // GC.SuppressFinalize(this);
    }
}