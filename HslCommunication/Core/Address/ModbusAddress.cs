﻿using HslCommunication.BasicFramework;
using System.Text;
using HslCommunication.ModBus;

namespace HslCommunication.Core.Address;

/// <summary>
/// Modbus协议地址格式，可以携带站号，功能码，地址信息
/// </summary>
public class ModbusAddress : DeviceAddressBase {
    /// <summary>
    /// 实例化一个默认的对象
    /// </summary>
    public ModbusAddress() {
        this.Station = -1;
        this.Function = -1;
        this.Address = 0;
    }

    /// <summary>
    /// 实例化一个默认的对象，使用默认的地址初始化
    /// </summary>
    /// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
    public ModbusAddress(string address) {
        this.Station = -1;
        this.Function = -1;
        this.Address = 0;
        this.Parse(address);
    }

    /// <summary>
    /// 实例化一个默认的对象，使用默认的地址初始化
    /// </summary>
    /// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
    /// <param name="function">默认的功能码信息</param>
    public ModbusAddress(string address, byte function) {
        this.Station = -1;
        this.Function = function;
        this.Address = 0;
        this.Parse(address);
    }

    /// <summary>
    /// 实例化一个默认的对象，使用默认的地址初始化
    /// </summary>
    /// <param name="station">站号信息</param>
    /// <param name="function">功能码信息</param>
    /// <param name="address">地址信息</param>
    public ModbusAddress(byte station, byte function, ushort address) {
        this.Station = -1;
        this.Function = function;
        this.Address = 0;
    }

    /// <summary>
    /// 站号信息
    /// </summary>
    public int Station { get; set; }

    /// <summary>
    /// 功能码
    /// </summary>
    public int Function { get; set; }

    /// <summary>
    /// 解析Modbus的地址码
    /// </summary>
    /// <param name="address">地址数据信息</param>
    public override void Parse(string address) {
        if (address.IndexOf(';') < 0) {
            // 正常地址，功能码03
            this.Address = ushort.Parse(address);
        }
        else {
            // 带功能码的地址
            string[] list = address.Split(';');
            for (int i = 0; i < list.Length; i++) {
                string item = list[i];
                if (item[0] == 's' || item[0] == 'S') {
                    // 站号信息
                    this.Station = byte.Parse(item.AsSpan(2));
                }
                else if (item[0] == 'x' || item[0] == 'X') {
                    this.Function = byte.Parse(item.AsSpan(2));
                }
                else {
                    this.Address = ushort.Parse(item);
                }
            }
        }
    }

    /// <summary>
    /// 创建一个读取线圈的字节对象
    /// </summary>
    /// <param name="station">读取的站号</param>
    /// <param name="length">读取数据的长度</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateReadCoils(byte station, ushort length) {
        byte[] buffer = new byte[6];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 0 ? ModbusInfo.ReadCoil : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = BitConverter.GetBytes(length)[1];
        buffer[5] = BitConverter.GetBytes(length)[0];
        return buffer;
    }

    /// <summary>
    /// 创建一个读取离散输入的字节对象
    /// </summary>
    /// <param name="station">读取的站号</param>
    /// <param name="length">读取数据的长度</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateReadDiscrete(byte station, ushort length) {
        byte[] buffer = new byte[6];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 0 ? ModbusInfo.ReadDiscrete : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = BitConverter.GetBytes(length)[1];
        buffer[5] = BitConverter.GetBytes(length)[0];
        return buffer;
    }

    /// <summary>
    /// 创建一个读取寄存器的字节对象
    /// </summary>
    /// <param name="station">读取的站号</param>
    /// <param name="length">读取数据的长度</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateReadRegister(byte station, ushort length) {
        byte[] buffer = new byte[6];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 1 ? ModbusInfo.ReadRegister : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = BitConverter.GetBytes(length)[1];
        buffer[5] = BitConverter.GetBytes(length)[0];
        return buffer;
    }


    /// <summary>
    /// 创建一个读取寄存器的字节对象
    /// </summary>
    /// <param name="station">读取的站号</param>
    /// <param name="length">读取数据的长度</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateReadInputRegister(byte station, ushort length) {
        byte[] buffer = new byte[6];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 1 ? ModbusInfo.ReadInputRegister : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = BitConverter.GetBytes(length)[1];
        buffer[5] = BitConverter.GetBytes(length)[0];
        return buffer;
    }

    /// <summary>
    /// 创建一个写入单个线圈的指令
    /// </summary>
    /// <param name="station">站号</param>
    /// <param name="value">值</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateWriteOneCoil(byte station, bool value) {
        byte[] buffer = new byte[6];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 0 ? ModbusInfo.WriteOneCoil : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = (byte) (value ? 0xFF : 0x00);
        buffer[5] = 0x00;
        return buffer;
    }

    /// <summary>
    /// 创建一个写入单个寄存器的指令
    /// </summary>
    /// <param name="station">站号</param>
    /// <param name="values">值</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateWriteOneRegister(byte station, byte[] values) {
        byte[] buffer = new byte[6];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 0 ? ModbusInfo.WriteOneRegister : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = values[0];
        buffer[5] = values[1];
        return buffer;
    }

    /// <summary>
    /// 创建一个写入批量线圈的指令
    /// </summary>
    /// <param name="station">站号</param>
    /// <param name="values">值</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateWriteCoil(byte station, bool[] values) {
        byte[] data = SoftBasic.BoolArrayToByte(values);
        byte[] buffer = new byte[7 + data.Length];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 0 ? ModbusInfo.WriteCoil : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = (byte) (values.Length / 256);
        buffer[5] = (byte) (values.Length % 256);
        buffer[6] = (byte) (data.Length);
        data.CopyTo(buffer, 7);
        return buffer;
    }

    /// <summary>
    /// 创建一个写入批量寄存器的指令
    /// </summary>
    /// <param name="station">站号</param>
    /// <param name="values">值</param>
    /// <returns>原始的modbus指令</returns>
    public byte[] CreateWriteRegister(byte station, byte[] values) {
        byte[] buffer = new byte[7 + values.Length];
        buffer[0] = this.Station < 0 ? station : (byte) this.Station;
        buffer[1] = this.Function < 1 ? ModbusInfo.WriteRegister : (byte) this.Function;
        buffer[2] = BitConverter.GetBytes(this.Address)[1];
        buffer[3] = BitConverter.GetBytes(this.Address)[0];
        buffer[4] = (byte) (values.Length / 2 / 256);
        buffer[5] = (byte) (values.Length / 2 % 256);
        buffer[6] = (byte) (values.Length);
        values.CopyTo(buffer, 7);
        return buffer;
    }

    /// <summary>
    /// 地址新增指定的数
    /// </summary>
    /// <param name="value">数据值信息</param>
    /// <returns>新增后的地址信息</returns>
    public ModbusAddress AddressAdd(int value) {
        return new ModbusAddress() {
            Station = this.Station,
            Function = this.Function,
            Address = (ushort) (this.Address + value),
        };
    }

    /// <summary>
    /// 地址新增1
    /// </summary>
    /// <returns>新增后的地址信息</returns>
    public ModbusAddress AddressAdd() {
        return this.AddressAdd(1);
    }

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>地址表示形式</returns>
    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        if (this.Station >= 0)
            sb.Append("s=" + this.Station + ";");
        if (this.Function >= 1)
            sb.Append("x=" + this.Function + ";");
        sb.Append(this.Address.ToString());

        return sb.ToString();
    }
}