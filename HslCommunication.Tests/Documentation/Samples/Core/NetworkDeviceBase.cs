using HslCommunication.Core.Reflection;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;
using HslCommunication.Devices.Melsec;
using HslCommunication.Devices.Siemens;

namespace HslCommunication.Tests.Documentation.Samples.Core;

public class NetworkDeviceBase {
    public class DataMy : IDataTransfer {
        // 根据对应的设备选择对应的实例化
        // 三菱 RegularByteTransform
        // 西门子 ReverseBytesTransform
        // Modbus及欧姆龙 ReverseWordTransform
        private IByteTransform byteTransform = new RegularByteTransform();

        public ushort ReadCount => 5;


        public short temperature = 0; // 温度
        public float press = 0f; // 压力
        public int others = 0; // 自定义的其他信息


        public void ParseSource(byte[] Content) {
            this.temperature = this.byteTransform.TransInt16(Content, 0);
            this.press = this.byteTransform.TransSingle(Content, 2);
            this.others = this.byteTransform.TransInt32(Content, 6);
        }

        public byte[] ToSource() {
            byte[] buffer = new byte[10];
            this.byteTransform.TransByte(this.temperature).CopyTo(buffer, 0);
            this.byteTransform.TransByte(this.press).CopyTo(buffer, 2);
            this.byteTransform.TransByte(this.others).CopyTo(buffer, 6);
            return buffer;
        }
    }


    public void ReadCustomerExample() {
        MelsecMcNet melsec = new MelsecMcNet("192.168.0.100", 6000);
        OperateResult<DataMy> read = melsec.ReadCustomer<DataMy>("M100");
        if (read.IsSuccess) {
            // success
            DataMy data = read.Content;
        }
        else {
            // failed
            Console.WriteLine("读取失败：" + read.Message);
        }
    }

    public async void ReadCustomerAsyncExample() {
        MelsecMcNet melsec = new MelsecMcNet("192.168.0.100", 6000);
        OperateResult<DataMy> read = await melsec.ReadCustomerAsync<DataMy>("M100");
        if (read.IsSuccess) {
            // success
            DataMy data = read.Content;
        }
        else {
            // failed
            Console.WriteLine("读取失败：" + read.Message);
        }
    }

    public void WriteCustomerExample() {
        MelsecMcNet melsec = new MelsecMcNet("192.168.0.100", 6000);

        DataMy dataMy = new DataMy();
        dataMy.temperature = 20;
        dataMy.press = 123.456f;
        dataMy.others = 1234232132;

        OperateResult write = melsec.WriteCustomer("M100", dataMy);
        if (write.IsSuccess) {
            // success
            Console.WriteLine("写入成功！");
        }
        else {
            // failed
            Console.WriteLine("读取失败：" + write.Message);
        }
    }

    public async void WriteCustomerAsyncExample() {
        MelsecMcNet melsec = new MelsecMcNet("192.168.0.100", 6000);

        DataMy dataMy = new DataMy();
        dataMy.temperature = 20;
        dataMy.press = 123.456f;
        dataMy.others = 1234232132;

        OperateResult write = await melsec.WriteCustomerAsync("M100", dataMy);
        if (write.IsSuccess) {
            // success
            Console.WriteLine("写入成功！");
        }
        else {
            // failed
            Console.WriteLine("读取失败：" + write.Message);
        }
    }

    // 假设你要读取几个数据的情况，我们把需要读取的数据定义成一个个的数量，本示例既适合单个读取，也适合批量读取，以下就是混搭的情况。
    // 我们假设，我们要读取的PLC是西门子PLC，地址数据的假设如下
    // 我们假设 设备是否启动是 M0.0
    // 产量是 M10 开始的2个地址数据
    // 温度信息是 DB1.0开始的4个地址数据
    // 报警的IO信息是 M200 开始，5个字节，共计40个IO点信息
    // 那么我们可以做如下的定义

    public class DataExample {
        /// <summary>
        /// 设备是否启动
        /// </summary>
        [HslDeviceAddress("M0.0")]
        public bool Enable { get; set; }

        /// <summary>
        /// 产量信息
        /// </summary>
        [HslDeviceAddress("M10")]
        public short Production { get; set; }

        /// <summary>
        /// 温度信息
        /// </summary>
        [HslDeviceAddress("DB1.0")]
        public float Temperature { get; set; }

        /// <summary>
        /// 连续的位报警信息
        /// </summary>
        [HslDeviceAddress("M200", 5)]
        public byte[] AlarmStatus { get; set; }
    }

    public void ReadObjectExample() {
        SiemensS7Net plc = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.100");

        // 此处需要注意的是，凡是带有 HslDeviceAddress 特性的属性都会被读取出来
        OperateResult<DataExample> read = plc.Read<DataExample>();
        if (read.IsSuccess) {
            // success
            DataExample data = read.Content;
        }
        else {
            // failed
            Console.WriteLine("读取失败：" + read.Message);
        }
    }

    public async void ReadObjectAsyncExample() {
        SiemensS7Net plc = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.100");

        // 此处需要注意的是，凡是带有 HslDeviceAddress 特性的属性都会被读取出来
        OperateResult<DataExample> read = await plc.ReadAsync<DataExample>();
        if (read.IsSuccess) {
            // success
            DataExample data = read.Content;
        }
        else {
            // failed
            Console.WriteLine("读取失败：" + read.Message);
        }
    }

    public void WriteObjectExample() {
        SiemensS7Net plc = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.100");

        // 此处需要注意的是，凡是带有 HslDeviceAddress 特性的属性都会被写入进去
        DataExample data = new DataExample() {
            Enable = true,
            Production = 123,
            Temperature = 123.4f,
            AlarmStatus = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
        };

        OperateResult write = plc.Write(data);
        if (write.IsSuccess) {
            // success
            Console.WriteLine("写入成功！");
        }
        else {
            // failed
            Console.WriteLine("写入失败：" + write.Message);
        }
    }

    public async void WriteObjectAsyncExample() {
        SiemensS7Net plc = new SiemensS7Net(SiemensPLCS.S1200, "192.168.0.100");

        // 此处需要注意的是，凡是带有 HslDeviceAddress 特性的属性都会被写入进去
        DataExample data = new DataExample() {
            Enable = true,
            Production = 123,
            Temperature = 123.4f,
            AlarmStatus = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
        };

        OperateResult write = await plc.WriteAsync(data);
        if (write.IsSuccess) {
            // success
            Console.WriteLine("写入成功！");
        }
        else {
            // failed
            Console.WriteLine("写入失败：" + write.Message);
        }
    }

    public void ReadInt16() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        short d100 = melsec_net.ReadInt16("D100").Content;


        // 如果需要判断是否读取成功
        OperateResult<short> R_d100 = melsec_net.ReadInt16("D100");
        if (R_d100.IsSuccess) {
            // success
            short value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadInt16Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        short d100 = (await melsec_net.ReadInt16Async("D100")).Content;


        // 如果需要判断是否读取成功
        OperateResult<short> R_d100 = await melsec_net.ReadInt16Async("D100");
        if (R_d100.IsSuccess) {
            // success
            short value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadInt16Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        short[] d100_109_value = melsec_net.ReadInt16("D100", 10).Content;

        // 如果需要判断是否读取成功
        OperateResult<short[]> R_d100_109_value = melsec_net.ReadInt16("D100", 10);
        if (R_d100_109_value.IsSuccess) {
            // success
            short value_d100 = R_d100_109_value.Content[0];
            short value_d101 = R_d100_109_value.Content[1];
            short value_d102 = R_d100_109_value.Content[2];
            short value_d103 = R_d100_109_value.Content[3];
            short value_d104 = R_d100_109_value.Content[4];
            short value_d105 = R_d100_109_value.Content[5];
            short value_d106 = R_d100_109_value.Content[6];
            short value_d107 = R_d100_109_value.Content[7];
            short value_d108 = R_d100_109_value.Content[8];
            short value_d109 = R_d100_109_value.Content[9];
        }
        else {
            // failed
        }
    }

    public async void ReadInt16ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        short[] d100_109_value = (await melsec_net.ReadInt16Async("D100", 10)).Content;

        // 如果需要判断是否读取成功
        OperateResult<short[]> R_d100_109_value = await melsec_net.ReadInt16Async("D100", 10);
        if (R_d100_109_value.IsSuccess) {
            // success
            short value_d100 = R_d100_109_value.Content[0];
            short value_d101 = R_d100_109_value.Content[1];
            short value_d102 = R_d100_109_value.Content[2];
            short value_d103 = R_d100_109_value.Content[3];
            short value_d104 = R_d100_109_value.Content[4];
            short value_d105 = R_d100_109_value.Content[5];
            short value_d106 = R_d100_109_value.Content[6];
            short value_d107 = R_d100_109_value.Content[7];
            short value_d108 = R_d100_109_value.Content[8];
            short value_d109 = R_d100_109_value.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadUInt16() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ushort d100 = melsec_net.ReadUInt16("D100").Content;

        // 如果需要判断是否读取成功
        OperateResult<ushort> R_d100 = melsec_net.ReadUInt16("D100");
        if (R_d100.IsSuccess) {
            // success
            ushort value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadUInt16Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ushort d100 = (await melsec_net.ReadUInt16Async("D100")).Content;

        // 如果需要判断是否读取成功
        OperateResult<ushort> R_d100 = await melsec_net.ReadUInt16Async("D100");
        if (R_d100.IsSuccess) {
            // success
            ushort value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadUInt16Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ushort[] d100_109 = melsec_net.ReadUInt16("D100", 10).Content;

        // 如果需要判断是否读取成功
        OperateResult<ushort[]> R_d100_109 = melsec_net.ReadUInt16("D100", 10);
        if (R_d100_109.IsSuccess) {
            // success
            ushort value_d100 = R_d100_109.Content[0];
            ushort value_d101 = R_d100_109.Content[1];
            ushort value_d102 = R_d100_109.Content[2];
            ushort value_d103 = R_d100_109.Content[3];
            ushort value_d104 = R_d100_109.Content[4];
            ushort value_d105 = R_d100_109.Content[5];
            ushort value_d106 = R_d100_109.Content[6];
            ushort value_d107 = R_d100_109.Content[7];
            ushort value_d108 = R_d100_109.Content[8];
            ushort value_d109 = R_d100_109.Content[9];
        }
        else {
            // failed
        }
    }

    public async void ReadUInt16ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ushort[] d100_109 = (await melsec_net.ReadUInt16Async("D100", 10)).Content;

        // 如果需要判断是否读取成功
        OperateResult<ushort[]> R_d100_109 = await melsec_net.ReadUInt16Async("D100", 10);
        if (R_d100_109.IsSuccess) {
            // success
            ushort value_d100 = R_d100_109.Content[0];
            ushort value_d101 = R_d100_109.Content[1];
            ushort value_d102 = R_d100_109.Content[2];
            ushort value_d103 = R_d100_109.Content[3];
            ushort value_d104 = R_d100_109.Content[4];
            ushort value_d105 = R_d100_109.Content[5];
            ushort value_d106 = R_d100_109.Content[6];
            ushort value_d107 = R_d100_109.Content[7];
            ushort value_d108 = R_d100_109.Content[8];
            ushort value_d109 = R_d100_109.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadInt32() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        int d100 = melsec_net.ReadInt32("D100").Content;

        // 如果需要判断是否读取成功
        OperateResult<int> R_d100 = melsec_net.ReadInt32("D100");
        if (R_d100.IsSuccess) {
            // success
            int value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadInt32Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        int d100 = (await melsec_net.ReadInt32Async("D100")).Content;

        // 如果需要判断是否读取成功
        OperateResult<int> R_d100 = await melsec_net.ReadInt32Async("D100");
        if (R_d100.IsSuccess) {
            // success
            int value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadInt32Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        int[] d100_119 = melsec_net.ReadInt32("D100", 10).Content;

        // 如果需要判断是否读取成功

        OperateResult<int[]> R_d100_119 = melsec_net.ReadInt32("D100", 10);
        if (R_d100_119.IsSuccess) {
            // success
            int value_d100 = R_d100_119.Content[0];
            int value_d102 = R_d100_119.Content[1];
            int value_d104 = R_d100_119.Content[2];
            int value_d106 = R_d100_119.Content[3];
            int value_d108 = R_d100_119.Content[4];
            int value_d110 = R_d100_119.Content[5];
            int value_d112 = R_d100_119.Content[6];
            int value_d114 = R_d100_119.Content[7];
            int value_d116 = R_d100_119.Content[8];
            int value_d118 = R_d100_119.Content[9];
        }
        else {
            // failed
        }
    }

    public async void ReadInt32ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        int[] d100_119 = (await melsec_net.ReadInt32Async("D100", 10)).Content;

        // 如果需要判断是否读取成功

        OperateResult<int[]> R_d100_119 = await melsec_net.ReadInt32Async("D100", 10);
        if (R_d100_119.IsSuccess) {
            // success
            int value_d100 = R_d100_119.Content[0];
            int value_d102 = R_d100_119.Content[1];
            int value_d104 = R_d100_119.Content[2];
            int value_d106 = R_d100_119.Content[3];
            int value_d108 = R_d100_119.Content[4];
            int value_d110 = R_d100_119.Content[5];
            int value_d112 = R_d100_119.Content[6];
            int value_d114 = R_d100_119.Content[7];
            int value_d116 = R_d100_119.Content[8];
            int value_d118 = R_d100_119.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadUInt32() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        uint d100 = melsec_net.ReadUInt32("D100").Content;

        // 如果需要判断是否读取成功
        OperateResult<uint> R_d100 = melsec_net.ReadUInt32("D100");
        if (R_d100.IsSuccess) {
            // success
            uint value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadUInt32Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        uint d100 = (await melsec_net.ReadUInt32Async("D100")).Content;

        // 如果需要判断是否读取成功
        OperateResult<uint> R_d100 = await melsec_net.ReadUInt32Async("D100");
        if (R_d100.IsSuccess) {
            // success
            uint value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadUInt32Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        uint[] d100_119 = melsec_net.ReadUInt32("D100", 10).Content;

        // 如果需要判断是否读取成功

        OperateResult<uint[]> R_d100_119 = melsec_net.ReadUInt32("D100", 10);
        if (R_d100_119.IsSuccess) {
            uint value_d100 = R_d100_119.Content[0];
            uint value_d102 = R_d100_119.Content[1];
            uint value_d104 = R_d100_119.Content[2];
            uint value_d106 = R_d100_119.Content[3];
            uint value_d108 = R_d100_119.Content[4];
            uint value_d110 = R_d100_119.Content[5];
            uint value_d112 = R_d100_119.Content[6];
            uint value_d114 = R_d100_119.Content[7];
            uint value_d116 = R_d100_119.Content[8];
            uint value_d118 = R_d100_119.Content[9];
        }
        else {
            // failed
        }
    }


    public async void ReadUInt32ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        uint[] d100_119 = (await melsec_net.ReadUInt32Async("D100", 10)).Content;

        // 如果需要判断是否读取成功

        OperateResult<uint[]> R_d100_119 = await melsec_net.ReadUInt32Async("D100", 10);
        if (R_d100_119.IsSuccess) {
            uint value_d100 = R_d100_119.Content[0];
            uint value_d102 = R_d100_119.Content[1];
            uint value_d104 = R_d100_119.Content[2];
            uint value_d106 = R_d100_119.Content[3];
            uint value_d108 = R_d100_119.Content[4];
            uint value_d110 = R_d100_119.Content[5];
            uint value_d112 = R_d100_119.Content[6];
            uint value_d114 = R_d100_119.Content[7];
            uint value_d116 = R_d100_119.Content[8];
            uint value_d118 = R_d100_119.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadFloat() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        float d100 = melsec_net.ReadFloat("D100").Content;

        // 如果需要判断是否读取成功
        OperateResult<float> R_d100 = melsec_net.ReadFloat("D100");
        if (R_d100.IsSuccess) {
            // success
            float value = R_d100.Content;
        }
        else {
            // failed
        }
    }


    public async void ReadFloatAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        float d100 = (await melsec_net.ReadFloatAsync("D100")).Content;

        // 如果需要判断是否读取成功
        OperateResult<float> R_d100 = await melsec_net.ReadFloatAsync("D100");
        if (R_d100.IsSuccess) {
            // success
            float value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadFloatArray() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        float[] d100_119 = melsec_net.ReadFloat("D100", 10).Content;

        // 如果需要判断是否读取成功

        OperateResult<float[]> R_d100_119 = melsec_net.ReadFloat("D100", 10);
        if (R_d100_119.IsSuccess) {
            float value_d100 = R_d100_119.Content[0];
            float value_d102 = R_d100_119.Content[1];
            float value_d104 = R_d100_119.Content[2];
            float value_d106 = R_d100_119.Content[3];
            float value_d108 = R_d100_119.Content[4];
            float value_d110 = R_d100_119.Content[5];
            float value_d112 = R_d100_119.Content[6];
            float value_d114 = R_d100_119.Content[7];
            float value_d116 = R_d100_119.Content[8];
            float value_d118 = R_d100_119.Content[9];
        }
        else {
            // failed
        }
    }

    public async void ReadFloatArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        float[] d100_119 = (await melsec_net.ReadFloatAsync("D100", 10)).Content;

        // 如果需要判断是否读取成功

        OperateResult<float[]> R_d100_119 = await melsec_net.ReadFloatAsync("D100", 10);
        if (R_d100_119.IsSuccess) {
            float value_d100 = R_d100_119.Content[0];
            float value_d102 = R_d100_119.Content[1];
            float value_d104 = R_d100_119.Content[2];
            float value_d106 = R_d100_119.Content[3];
            float value_d108 = R_d100_119.Content[4];
            float value_d110 = R_d100_119.Content[5];
            float value_d112 = R_d100_119.Content[6];
            float value_d114 = R_d100_119.Content[7];
            float value_d116 = R_d100_119.Content[8];
            float value_d118 = R_d100_119.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadInt64() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        long d100 = melsec_net.ReadInt64("D100").Content;

        // 如果需要判断是否读取成功
        OperateResult<long> R_d100 = melsec_net.ReadInt64("D100");
        if (R_d100.IsSuccess) {
            // success
            double value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadInt64Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        long d100 = (await melsec_net.ReadInt64Async("D100")).Content;

        // 如果需要判断是否读取成功
        OperateResult<long> R_d100 = await melsec_net.ReadInt64Async("D100");
        if (R_d100.IsSuccess) {
            // success
            double value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadInt64Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        long[] d100_139 = melsec_net.ReadInt64("D100", 10).Content;

        // 如果需要判断是否读取成功

        OperateResult<long[]> R_d100_139 = melsec_net.ReadInt64("D100", 10);
        if (R_d100_139.IsSuccess) {
            long value_d100 = R_d100_139.Content[0];
            long value_d104 = R_d100_139.Content[1];
            long value_d108 = R_d100_139.Content[2];
            long value_d112 = R_d100_139.Content[3];
            long value_d116 = R_d100_139.Content[4];
            long value_d120 = R_d100_139.Content[5];
            long value_d124 = R_d100_139.Content[6];
            long value_d128 = R_d100_139.Content[7];
            long value_d132 = R_d100_139.Content[8];
            long value_d136 = R_d100_139.Content[9];
        }
        else {
            // failed
        }
    }

    public async void ReadInt64ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        long[] d100_139 = (await melsec_net.ReadInt64Async("D100", 10)).Content;

        // 如果需要判断是否读取成功

        OperateResult<long[]> R_d100_139 = await melsec_net.ReadInt64Async("D100", 10);
        if (R_d100_139.IsSuccess) {
            long value_d100 = R_d100_139.Content[0];
            long value_d104 = R_d100_139.Content[1];
            long value_d108 = R_d100_139.Content[2];
            long value_d112 = R_d100_139.Content[3];
            long value_d116 = R_d100_139.Content[4];
            long value_d120 = R_d100_139.Content[5];
            long value_d124 = R_d100_139.Content[6];
            long value_d128 = R_d100_139.Content[7];
            long value_d132 = R_d100_139.Content[8];
            long value_d136 = R_d100_139.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadUInt64() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ulong d100 = melsec_net.ReadUInt64("D100").Content;

        // 如果需要判断是否读取成功
        OperateResult<ulong> R_d100 = melsec_net.ReadUInt64("D100");
        if (R_d100.IsSuccess) {
            // success
            double value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadUInt64Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ulong d100 = (await melsec_net.ReadUInt64Async("D100")).Content;

        // 如果需要判断是否读取成功
        OperateResult<ulong> R_d100 = await melsec_net.ReadUInt64Async("D100");
        if (R_d100.IsSuccess) {
            // success
            double value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadUInt64Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ulong[] d100_139 = melsec_net.ReadUInt64("D100", 10).Content;

        // 如果需要判断是否读取成功

        OperateResult<ulong[]> R_d100_139 = melsec_net.ReadUInt64("D100", 10);
        if (R_d100_139.IsSuccess) {
            ulong value_d100 = R_d100_139.Content[0];
            ulong value_d104 = R_d100_139.Content[1];
            ulong value_d108 = R_d100_139.Content[2];
            ulong value_d112 = R_d100_139.Content[3];
            ulong value_d116 = R_d100_139.Content[4];
            ulong value_d120 = R_d100_139.Content[5];
            ulong value_d124 = R_d100_139.Content[6];
            ulong value_d128 = R_d100_139.Content[7];
            ulong value_d132 = R_d100_139.Content[8];
            ulong value_d136 = R_d100_139.Content[9];
        }
        else {
            // failed
        }
    }

    public async void ReadUInt64ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        ulong[] d100_139 = (await melsec_net.ReadUInt64Async("D100", 10)).Content;

        // 如果需要判断是否读取成功

        OperateResult<ulong[]> R_d100_139 = await melsec_net.ReadUInt64Async("D100", 10);
        if (R_d100_139.IsSuccess) {
            ulong value_d100 = R_d100_139.Content[0];
            ulong value_d104 = R_d100_139.Content[1];
            ulong value_d108 = R_d100_139.Content[2];
            ulong value_d112 = R_d100_139.Content[3];
            ulong value_d116 = R_d100_139.Content[4];
            ulong value_d120 = R_d100_139.Content[5];
            ulong value_d124 = R_d100_139.Content[6];
            ulong value_d128 = R_d100_139.Content[7];
            ulong value_d132 = R_d100_139.Content[8];
            ulong value_d136 = R_d100_139.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadDouble() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        double d100 = melsec_net.ReadDouble("D100").Content;

        // 如果需要判断是否读取成功
        OperateResult<double> R_d100 = melsec_net.ReadDouble("D100");
        if (R_d100.IsSuccess) {
            // success
            double value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadDoubleAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        double d100 = (await melsec_net.ReadDoubleAsync("D100")).Content;

        // 如果需要判断是否读取成功
        OperateResult<double> R_d100 = await melsec_net.ReadDoubleAsync("D100");
        if (R_d100.IsSuccess) {
            // success
            double value = R_d100.Content;
        }
        else {
            // failed
        }
    }

    public void ReadDoubleArray() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        double[] d100_139 = melsec_net.ReadDouble("D100", 10).Content;

        // 如果需要判断是否读取成功

        OperateResult<double[]> R_d100_139 = melsec_net.ReadDouble("D100", 10);
        if (R_d100_139.IsSuccess) {
            double value_d100 = R_d100_139.Content[0];
            double value_d104 = R_d100_139.Content[1];
            double value_d108 = R_d100_139.Content[2];
            double value_d112 = R_d100_139.Content[3];
            double value_d116 = R_d100_139.Content[4];
            double value_d120 = R_d100_139.Content[5];
            double value_d124 = R_d100_139.Content[6];
            double value_d128 = R_d100_139.Content[7];
            double value_d132 = R_d100_139.Content[8];
            double value_d136 = R_d100_139.Content[9];
        }
        else {
            // failed
        }
    }

    public async void ReadDoubleArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        double[] d100_139 = (await melsec_net.ReadDoubleAsync("D100", 10)).Content;

        // 如果需要判断是否读取成功

        OperateResult<double[]> R_d100_139 = await melsec_net.ReadDoubleAsync("D100", 10);
        if (R_d100_139.IsSuccess) {
            double value_d100 = R_d100_139.Content[0];
            double value_d104 = R_d100_139.Content[1];
            double value_d108 = R_d100_139.Content[2];
            double value_d112 = R_d100_139.Content[3];
            double value_d116 = R_d100_139.Content[4];
            double value_d120 = R_d100_139.Content[5];
            double value_d124 = R_d100_139.Content[6];
            double value_d128 = R_d100_139.Content[7];
            double value_d132 = R_d100_139.Content[8];
            double value_d136 = R_d100_139.Content[9];
        }
        else {
            // failed
        }
    }

    public void ReadString() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        string d100_value = melsec_net.ReadString("D100", 5).Content;

        // 如果需要判断是否读取成功
        OperateResult<string> R_d100_value = melsec_net.ReadString("D100", 5);
        if (R_d100_value.IsSuccess) {
            // success
            string value = R_d100_value.Content;
        }
        else {
            // failed
        }
    }

    public async void ReadStringAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 以下是简单的读取，没有仔细校验的方式
        string d100_value = (await melsec_net.ReadStringAsync("D100", 5)).Content;

        // 如果需要判断是否读取成功
        OperateResult<string> R_d100_value = await melsec_net.ReadStringAsync("D100", 5);
        if (R_d100_value.IsSuccess) {
            // success
            string value = R_d100_value.Content;
        }
        else {
            // failed
        }
    }

    public async void WriteAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", (short) 123);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", (short) 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteInt16() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", (short) 123);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", (short) 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteInt16Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", (short) 123);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", (short) 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteInt16Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new short[] { 123, -342, 3535 });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new short[] { 123, -342, 3535 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteInt16ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new short[] { 123, -342, 3535 });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new short[] { 123, -342, 3535 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteUInt16() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", (ushort) 123);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", (ushort) 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteUInt16Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", (ushort) 123);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", (ushort) 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteUInt16Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new ushort[] { 123, 342, 3535 });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new ushort[] { 123, 342, 3535 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteUInt16ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new ushort[] { 123, 342, 3535 });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new ushort[] { 123, 342, 3535 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }


    public void WriteInt32() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", 123);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteInt32Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", 123);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteInt32Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new int[] { 123, 342, -3535, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new int[] { 123, 342, -3535, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteInt32ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new int[] { 123, 342, -3535, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new int[] { 123, 342, -3535, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteUInt32() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", (uint) 123);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", (uint) 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteUInt32Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", (uint) 123);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", (uint) 123);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteUInt32Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new uint[] { 123, 342, 3535, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new uint[] { 123, 342, 3535, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteUInt32ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new uint[] { 123, 342, 3535, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new uint[] { 123, 342, 3535, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteFloat() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", 123.456f);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", 123.456f);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteFloatAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", 123.456f);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", 123.456f);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteFloatArray() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new float[] { 123f, 342.23f, 0.001f, -123.34f });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new float[] { 123f, 342.23f, 0.001f, -123.34f });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteFloatArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new float[] { 123f, 342.23f, 0.001f, -123.34f });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new float[] { 123f, 342.23f, 0.001f, -123.34f });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteInt64() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", 12334242354L);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", 12334242354L);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteInt64Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", 12334242354L);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", 12334242354L);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteInt64Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new long[] { 123, 342, -352312335, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new long[] { 123, 342, -352312335, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteInt64ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new long[] { 123, 342, -352312335, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new long[] { 123, 342, -352312335, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteUInt64() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", 12334242354UL);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", 12334242354UL);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }


    public async void WriteUInt64Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", 12334242354UL);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", 12334242354UL);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteUInt64Array() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new ulong[] { 123, 342, 352312335, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new ulong[] { 123, 342, 352312335, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteUInt64ArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new ulong[] { 123, 342, 352312335, 1235234 });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new ulong[] { 123, 342, 352312335, 1235234 });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }


    public void WriteDouble() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", 123.456d);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", 123.456d);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public async void WriteDoubleAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", 123.456d);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", 123.456d);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteDoubleArray() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", new double[] { 123d, 342.23d, 0.001d, -123.34d });

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", new double[] { 123d, 342.23d, 0.001d, -123.34d });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }


    public async void WriteDoubleArrayAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", new double[] { 123d, 342.23d, 0.001d, -123.34d });

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", new double[] { 123d, 342.23d, 0.001d, -123.34d });
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteString() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", "ABCDEFGH");

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", "ABCDEFGH");
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }


    public async void WriteStringAsync() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", "ABCDEFGH");

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", "ABCDEFGH");
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }

    public void WriteString2() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        melsec_net.Write("D100", "ABCDEFGH", 10);

        // 如果想要判断是否写入成功
        OperateResult write = melsec_net.Write("D100", "ABCDEFGH", 10);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }


    public async void WriteString2Async() {
        MelsecMcNet melsec_net = new MelsecMcNet("192.168.0.100", 6000);

        // 简单的写入
        await melsec_net.WriteAsync("D100", "ABCDEFGH", 10);

        // 如果想要判断是否写入成功
        OperateResult write = await melsec_net.WriteAsync("D100", "ABCDEFGH", 10);
        if (write.IsSuccess) {
            // success
        }
        else {
            // failed
        }
    }
}