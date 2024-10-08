﻿using System.Text;

namespace HslCommunication.Core.Transfer;

/// <summary>
/// 按照字节错位的数据转换类
/// </summary>
public class ReverseWordTransform : ByteTransformBase {
    /// <summary>
    /// 实例化一个默认的对象
    /// </summary>
    public ReverseWordTransform() {
        this.DataFormat = DataFormat.ABCD;
    }

    /// <summary>
    /// 使用指定的数据解析来实例化对象
    /// </summary>
    /// <param name="dataFormat">数据规则</param>
    public ReverseWordTransform(DataFormat dataFormat) : base(dataFormat) {
    }

    /// <summary>
    /// 按照字节错位的方法
    /// </summary>
    /// <param name="buffer">实际的字节数据</param>
    /// <param name="index">起始字节位置</param>
    /// <param name="length">数据长度</param>
    /// <returns>处理过的数据信息</returns>
    private byte[] ReverseBytesByWord(byte[] buffer, int index, int length) {
        if (buffer == null)
            return null;

        // copy data
        byte[] tmp = new byte[length];
        for (int i = 0; i < length; i++) {
            tmp[i] = buffer[index + i];
        }

        // change
        for (int i = 0; i < length / 2; i++) {
            byte b = tmp[i * 2 + 0];
            tmp[i * 2 + 0] = tmp[i * 2 + 1];
            tmp[i * 2 + 1] = b;
        }

        return tmp;
    }

    private byte[] ReverseBytesByWord(byte[] buffer) {
        return this.ReverseBytesByWord(buffer, 0, buffer.Length);
    }

    /// <summary>
    /// 字符串数据是否按照字来反转
    /// </summary>
    public bool IsStringReverse { get; set; }

    /// <summary>
    /// 从缓存中提取short结果
    /// </summary>
    /// <param name="buffer">缓存数据</param>
    /// <param name="index">索引位置</param>
    /// <returns>short对象</returns>
    public override short TransInt16(byte[] buffer, int index) {
        return base.TransInt16(this.ReverseBytesByWord(buffer, index, 2), 0);
    }


    /// <summary>
    /// 从缓存中提取ushort结果
    /// </summary>
    /// <param name="buffer">缓存数据</param>
    /// <param name="index">索引位置</param>
    /// <returns>ushort对象</returns>
    public override ushort TransUInt16(byte[] buffer, int index) {
        return base.TransUInt16(this.ReverseBytesByWord(buffer, index, 2), 0);
    }


    /// <summary>
    /// 从缓存中提取string结果，使用指定的编码
    /// </summary>
    /// <param name="buffer">缓存对象</param>
    /// <param name="index">索引位置</param>
    /// <param name="length">byte数组长度</param>
    /// <param name="encoding">字符串的编码</param>
    /// <returns>string对象</returns>
    public override string TransString(byte[] buffer, int index, int length, Encoding encoding) {
        byte[] tmp = this.TransByte(buffer, index, length);

        if (this.IsStringReverse) {
            return encoding.GetString(this.ReverseBytesByWord(tmp));
        }
        else {
            return encoding.GetString(tmp);
        }
    }

    /// <summary>
    /// short数组变量转化缓存数据
    /// </summary>
    /// <param name="values">等待转化的数组</param>
    /// <returns>buffer数据</returns>
    public override byte[] TransByte(short[] values) {
        byte[] buffer = base.TransByte(values);
        return this.ReverseBytesByWord(buffer);
    }


    /// <summary>
    /// ushort数组变量转化缓存数据
    /// </summary>
    /// <param name="values">等待转化的数组</param>
    /// <returns>buffer数据</returns>
    public override byte[] TransByte(ushort[] values) {
        byte[] buffer = base.TransByte(values);
        return this.ReverseBytesByWord(buffer);
    }


    /// <summary>
    /// 使用指定的编码字符串转化缓存数据
    /// </summary>
    /// <param name="value">等待转化的数据</param>
    /// <param name="encoding">字符串的编码方式</param>
    /// <returns>buffer数据</returns>
    public override byte[] TransByte(string value, Encoding encoding) {
        if (value == null)
            return null;
        byte[] buffer = encoding.GetBytes(value);
        buffer = BasicFramework.SoftBasic.ArrayExpandToLengthEven(buffer);
        if (this.IsStringReverse) {
            return this.ReverseBytesByWord(buffer);
        }
        else {
            return buffer;
        }
    }
}