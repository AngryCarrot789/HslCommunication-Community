﻿using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using HslCommunication.Core.Types;

namespace HslCommunication.Devices.Melsec;

/// <summary>
/// 所有三菱通讯类的通用辅助工具类，包含了一些通用的静态方法，可以使用本类来获取一些原始的报文信息。详细的操作参见例子
/// </summary>
public static class MelsecHelper {
    /// <summary>
    /// 解析A1E协议数据地址
    /// </summary>
    /// <param name="address">数据地址</param>
    /// <returns></returns>
    public static LightOperationResult<MelsecA1EDataType, ushort> McA1EAnalysisAddress(string address) {
        try {
            switch (address[0]) {
                case 'X':
                case 'x':
                    return new LightOperationResult<MelsecA1EDataType, ushort>(MelsecA1EDataType.X, Convert.ToUInt16(address.Substring(1), MelsecA1EDataType.X.FromBase)); 
                case 'Y':
                case 'y':
                    return new LightOperationResult<MelsecA1EDataType, ushort>(MelsecA1EDataType.X, Convert.ToUInt16(address.Substring(1), MelsecA1EDataType.Y.FromBase));
                case 'M':
                case 'm':
                    return new LightOperationResult<MelsecA1EDataType, ushort>(MelsecA1EDataType.X, Convert.ToUInt16(address.Substring(1), MelsecA1EDataType.M.FromBase));
                case 'S':
                case 's':
                    return new LightOperationResult<MelsecA1EDataType, ushort>(MelsecA1EDataType.X, Convert.ToUInt16(address.Substring(1), MelsecA1EDataType.S.FromBase));
                case 'D':
                case 'd':
                    return new LightOperationResult<MelsecA1EDataType, ushort>(MelsecA1EDataType.X, Convert.ToUInt16(address.Substring(1), MelsecA1EDataType.D.FromBase));
                case 'R':
                case 'r':
                    return new LightOperationResult<MelsecA1EDataType, ushort>(MelsecA1EDataType.X, Convert.ToUInt16(address.Substring(1), MelsecA1EDataType.R.FromBase));
                default: throw new Exception(StringResources.Language.NotSupportedDataType);
            }
        }
        catch (Exception ex) {
            return new LightOperationResult<MelsecA1EDataType, ushort>(ex.Message);
        }
    }

    /// <summary>
    /// 从三菱地址，是否位读取进行创建读取的MC的核心报文
    /// </summary>
    /// <param name="isBit">是否进行了位读取操作</param>
    /// <param name="addressData">三菱Mc协议的数据地址</param>
    /// <returns>带有成功标识的报文对象</returns>
    public static byte[] BuildReadMcCoreCommand(McAddressData addressData, bool isBit) {
        byte[] command = new byte[10];
        command[0] = 0x01; // 批量读取数据命令
        command[1] = 0x04;
        command[2] = isBit ? (byte) 0x01 : (byte) 0x00; // 以点为单位还是字为单位成批读取
        command[3] = 0x00;

        byte[] addressStartBits = BitConverter.GetBytes(addressData.AddressStart);
        command[4] = BitConverter.GetBytes(addressData.AddressStart)[0]; // Starting Address
        command[5] = BitConverter.GetBytes(addressData.AddressStart)[1];
        command[6] = BitConverter.GetBytes(addressData.AddressStart)[2];
        command[7] = addressData.McDataType.DataCode; // 指明读取的数据
        command[8] = (byte) (addressData.Length % 256); // 软元件的长度
        command[9] = (byte) (addressData.Length / 256);

        return command;
    }

    /// <summary>
    /// 从三菱地址，是否位读取进行创建读取Ascii格式的MC的核心报文
    /// </summary>
    /// <param name="addressData">三菱Mc协议的数据地址</param>
    /// <param name="isBit">是否进行了位读取操作</param>
    /// <returns>带有成功标识的报文对象</returns>
    public static byte[] BuildAsciiReadMcCoreCommand(McAddressData addressData, bool isBit) {
        byte[] command = new byte[20];
        command[0] = 0x30; // 批量读取数据命令
        command[1] = 0x34;
        command[2] = 0x30;
        command[3] = 0x31;
        command[4] = 0x30; // 以点为单位还是字为单位成批读取
        command[5] = 0x30;
        command[6] = 0x30;
        command[7] = isBit ? (byte) 0x31 : (byte) 0x30;

        string code = addressData.McDataType.AsciiCode;
        byte[] asciiCode = Encoding.ASCII.GetBytes(code);
        
        command[8] = asciiCode[0]; // Component Type
        command[9] = asciiCode[1];

        byte[] asciiAddress = BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType);
        command[10] = asciiAddress[0]; // Starting Address
        command[11] = asciiAddress[1];
        command[12] = asciiAddress[2];
        command[13] = asciiAddress[3];
        command[14] = asciiAddress[4];
        command[15] = asciiAddress[5];

        byte[] asciiAddressLength = SoftBasic.BuildAsciiBytesFrom(addressData.Length); 
        command[16] = asciiAddressLength[0]; // 软元件点数
        command[17] = asciiAddressLength[1];
        command[18] = asciiAddressLength[2];
        command[19] = asciiAddressLength[3];

        return command;
    }

    /// <summary>
    /// 以字为单位，创建数据写入的核心报文
    /// </summary>
    /// <param name="addressData">三菱Mc协议的数据地址</param>
    /// <param name="value">实际的原始数据信息</param>
    /// <returns>带有成功标识的报文对象</returns>
    public static byte[] BuildWriteWordCoreCommand(McAddressData addressData, byte[] value) {
        if (value == null)
            value = Array.Empty<byte>();
        byte[] command = new byte[10 + value.Length];
        command[0] = 0x01; // 批量写入数据命令
        command[1] = 0x14;
        command[2] = 0x00; // 以字为单位成批读取
        command[3] = 0x00;

        byte[] addressBits = BitConverter.GetBytes(addressData.AddressStart);
        command[4] = addressBits[0]; // Starting Address
        command[5] = addressBits[1];
        command[6] = addressBits[2];
        command[7] = addressData.McDataType.DataCode; // 指明写入的数据
        command[8] = (byte) (value.Length / 2 % 256); // 软元件长度的地位
        command[9] = (byte) (value.Length / 2 / 256);
        value.CopyTo(command, 10);

        return command;
    }

    /// <summary>
    /// 以字为单位，创建ASCII数据写入的核心报文
    /// </summary>
    /// <param name="addressData">三菱Mc协议的数据地址</param>
    /// <param name="value">实际的原始数据信息</param>
    /// <returns>带有成功标识的报文对象</returns>
    public static byte[] BuildAsciiWriteWordCoreCommand(McAddressData addressData, byte[] value) {
        if (value == null)
            value = Array.Empty<byte>();
        byte[] buffer = new byte[value.Length * 2];
        for (int i = 0; i < value.Length / 2; i++) {
            SoftBasic.BuildAsciiBytesFrom(BitConverter.ToUInt16(value, i * 2)).CopyTo(buffer, 4 * i);
        }

        value = buffer;

        byte[] command = new byte[20 + value.Length];
        command[0] = 0x31; // 批量写入的命令
        command[1] = 0x34;
        command[2] = 0x30;
        command[3] = 0x31;
        command[4] = 0x30; // 子命令
        command[5] = 0x30;
        command[6] = 0x30;
        command[7] = 0x30;

        byte[] asciiCode = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode);
        command[8] = asciiCode[0]; // Component Type
        command[9] = asciiCode[1];

        byte[] asciiAddress = BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType); 
        command[10] = asciiAddress[0]; // Starting Address
        command[11] = asciiAddress[1];
        command[12] = asciiAddress[2];
        command[13] = asciiAddress[3];
        command[14] = asciiAddress[4];
        command[15] = asciiAddress[5];

        byte[] asciiValLen = SoftBasic.BuildAsciiBytesFrom((ushort) (value.Length / 4)); 
        command[16] = asciiValLen[0]; // 软元件点数
        command[17] = asciiValLen[1];
        command[18] = asciiValLen[2];
        command[19] = asciiValLen[3];
        value.CopyTo(command, 20);

        return command;
    }

    /// <summary>
    /// 以位为单位，创建数据写入的核心报文
    /// </summary>
    /// <param name="addressData">三菱Mc协议的数据地址</param>
    /// <param name="value">原始的bool数组数据</param>
    /// <returns>带有成功标识的报文对象</returns>
    public static byte[] BuildWriteBitCoreCommand(McAddressData addressData, bool[] value) {
        if (value == null)
            value = Array.Empty<bool>();
        byte[] buffer = TransBoolArrayToByteData(value);
        byte[] command = new byte[10 + buffer.Length];
        command[0] = 0x01; // 批量写入数据命令
        command[1] = 0x14;
        command[2] = 0x01; // 以位为单位成批写入
        command[3] = 0x00;

        byte[] addressBits = BitConverter.GetBytes(addressData.AddressStart);
        command[4] = addressBits[0]; // Starting Address
        command[5] = addressBits[1];
        command[6] = addressBits[2];
        command[7] = addressData.McDataType.DataCode; // 指明写入的数据
        command[8] = (byte) (value.Length % 256); // 软元件长度的地位
        command[9] = (byte) (value.Length / 256);
        buffer.CopyTo(command, 10);

        return command;
    }

    /// <summary>
    /// 以位为单位，创建ASCII数据写入的核心报文
    /// </summary>
    /// <param name="addressData">三菱Mc协议的数据地址</param>
    /// <param name="value">原始的bool数组数据</param>
    /// <returns>带有成功标识的报文对象</returns>
    public static byte[] BuildAsciiWriteBitCoreCommand(McAddressData addressData, bool[] value) {
        if (value == null)
            value = Array.Empty<bool>();
        byte[] buffer = value.Select(m => m ? (byte) 0x31 : (byte) 0x30).ToArray();

        byte[] command = new byte[20 + buffer.Length];
        command[0] = 0x31; // 批量写入的命令
        command[1] = 0x34;
        command[2] = 0x30;
        command[3] = 0x31;
        command[4] = 0x30; // 子命令
        command[5] = 0x30;
        command[6] = 0x30;
        command[7] = 0x31;
        
        byte[] asciiCode = Encoding.ASCII.GetBytes(addressData.McDataType.AsciiCode);
        command[8] = asciiCode[0]; // Component Type
        command[9] = asciiCode[1];


        byte[] asciiAddress = BuildBytesFromAddress(addressData.AddressStart, addressData.McDataType);
        command[10] = asciiAddress[0]; // Starting Address
        command[11] = asciiAddress[1];
        command[12] = asciiAddress[2];
        command[13] = asciiAddress[3];
        command[14] = asciiAddress[4];
        command[15] = asciiAddress[5];

        byte[] asciiValLen = SoftBasic.BuildAsciiBytesFrom((ushort) (value.Length));
        command[16] = asciiValLen[0]; // 软元件点数
        command[17] = asciiValLen[1];
        command[18] = asciiValLen[2];
        command[19] = asciiValLen[3];
        buffer.CopyTo(command, 20);

        return command;
    }

    /// <summary>
    /// 从三菱的地址中构建MC协议的6字节的ASCII格式的地址
    /// </summary>
    /// <param name="address">三菱地址</param>
    /// <param name="type">三菱的数据类型</param>
    /// <returns>6字节的ASCII格式的地址</returns>
    internal static byte[] BuildBytesFromAddress(int address, MelsecMcDataType type) {
        return Encoding.ASCII.GetBytes(address.ToString(type.FromBase == 10 ? "D6" : "X6"));
    }

    /// <summary>
    /// 将0，1，0，1的字节数组压缩成三菱格式的字节数组来表示开关量的
    /// </summary>
    /// <param name="value">原始的数据字节</param>
    /// <returns>压缩过后的数据字节</returns>
    internal static byte[] TransBoolArrayToByteData(byte[] value) {
        int length = (value.Length + 1) / 2;
        byte[] buffer = new byte[length];

        for (int i = 0; i < length; i++) {
            if (value[i * 2 + 0] != 0x00)
                buffer[i] += 0x10;
            if ((i * 2 + 1) < value.Length) {
                if (value[i * 2 + 1] != 0x00)
                    buffer[i] += 0x01;
            }
        }

        return buffer;
    }

    /// <summary>
    /// 将bool的组压缩成三菱格式的字节数组来表示开关量的
    /// </summary>
    /// <param name="value">原始的数据字节</param>
    /// <returns>压缩过后的数据字节</returns>
    internal static byte[] TransBoolArrayToByteData(bool[] value) {
        int length = (value.Length + 1) / 2;
        byte[] buffer = new byte[length];

        for (int i = 0; i < length; i++) {
            if (value[i * 2 + 0])
                buffer[i] += 0x10;
            if ((i * 2 + 1) < value.Length) {
                if (value[i * 2 + 1])
                    buffer[i] += 0x01;
            }
        }

        return buffer;
    }

    /// <summary>
    /// 计算Fx协议指令的和校验信息
    /// </summary>
    /// <param name="data">字节数据</param>
    /// <returns>校验之后的数据</returns>
    public static byte[] FxCalculateCRC(byte[] data) => FxCalculateCRC(data, data.Length);
    
    public static byte[] FxCalculateCRC(byte[] data, int length) {
        int sum = 0;
        for (int i = 1; i < length - 2; i++)
            sum += data[i];
        return SoftBasic.BuildAsciiBytesFrom((byte) sum);
    }
    
    public static byte[] FxCalculateCRCRaw(byte[] data, int offset, int count) {
        int sum = 0;
        for (int i = 0; i < count; i++)
            sum += data[offset + i];
        return SoftBasic.BuildAsciiBytesFrom((byte) sum);
    }
    
    /// <summary>
    /// 检查指定的和校验是否是正确的
    /// </summary>
    /// <param name="data">字节数据</param>
    /// <returns>是否成功</returns>
    internal static bool CheckCRC(byte[] data) => CheckCRC(data, data.Length);
    
    internal static bool CheckCRC(byte[] data, int length) {
        byte[] crc = FxCalculateCRC(data, length);
        if (crc[0] != data[length - 2])
            return false;
        if (crc[1] != data[length - 1])
            return false;
        return true;
    }
}