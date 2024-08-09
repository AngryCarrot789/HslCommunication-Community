using HslCommunication.Core.Net;
using HslCommunication.Devices.Melsec;
using HslCommunication.Devices.Siemens;
using HslCommunication.ModBus.ModbusTcp;

namespace HslCommunication.Tests.Documentation.Samples.Core;

public class IReadWriteNetExample {
    public void IReadWriteNet() {
        List<IReadWriteNet> devices = new List<IReadWriteNet>();
        devices.Add(new ModbusTcpNet("192.168.0.7")); // 新增modbus的设备
        devices.Add(new MelsecMcNet("192.168.0.8", 2000)); // 新增三菱的设备
        devices.Add(new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.9")); // 新增西门子的设备

        // 添加各自的数据地址，不同的设备的数据地址格式肯定不一致的
        List<string> address = new List<string>();
        address.Add("x=4;100"); // 假设modbus的数据地址是输入寄存器的100的地址，类型为short
        address.Add("M100"); // 假设三菱的数据地址是数据寄存器M100
        address.Add("DB1.100"); // 假设西门子的数据地址在DB块1的偏移地址100上

        short[] values = new short[3];
        for (int i = 0; i < devices.Count; i++) {
            values[i] = devices[i].ReadInt16(address[i]).Content;
        }

        // values即包含了多种设备的值，实际上如果想要开发一个完善的系统，还要更加复杂点
    }
}