using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Core.Transfer;

[TestClass]
public class ReverseWordTransformTest {
    public ReverseWordTransformTest() {
        this.byteTransform = new ReverseWordTransform();
    }

    protected ReverseWordTransform byteTransform;


    /// <summary>
    /// 按照字节错位的方法
    /// </summary>
    /// <param name="buffer">实际的字节数据</param>
    /// <param name="index">起始字节位置</param>
    /// <param name="length">数据长度</param>
    /// <param name="isReverse">是否按照字来反转</param>
    /// <returns></returns>
    private byte[] ReverseBytesByWord(byte[] buffer, int index, int length, DataFormat dataFormat) {
        byte[] tmp = new byte[length];

        for (int i = 0; i < length; i++) {
            tmp[i] = buffer[index + i];
        }

        if (tmp.Length == 4) {
            if (dataFormat == DataFormat.CDAB) {
                byte a = tmp[0];
                tmp[0] = tmp[1];
                tmp[1] = a;


                byte b = tmp[2];
                tmp[2] = tmp[3];
                tmp[3] = b;
            }
            else if (dataFormat == DataFormat.BADC) {
                byte a = tmp[0];
                tmp[0] = tmp[2];
                tmp[2] = a;


                byte b = tmp[1];
                tmp[1] = tmp[3];
                tmp[3] = b;
            }
            else if (dataFormat == DataFormat.ABCD) {
                byte a = tmp[0];
                tmp[0] = tmp[3];
                tmp[3] = a;

                byte b = tmp[1];
                tmp[1] = tmp[2];
                tmp[2] = b;
            }
        }
        else if (tmp.Length == 8) {
            if (dataFormat == DataFormat.CDAB) {
                byte a = tmp[0];
                tmp[0] = tmp[1];
                tmp[1] = a;


                byte b = tmp[2];
                tmp[2] = tmp[3];
                tmp[3] = b;

                a = tmp[4];
                tmp[4] = tmp[5];
                tmp[5] = a;

                a = tmp[6];
                tmp[6] = tmp[7];
                tmp[7] = a;
            }
            else if (dataFormat == DataFormat.BADC) {
                byte a = tmp[0];
                tmp[0] = tmp[6];
                tmp[6] = a;


                a = tmp[1];
                tmp[1] = tmp[7];
                tmp[7] = a;

                a = tmp[2];
                tmp[2] = tmp[4];
                tmp[4] = a;

                a = tmp[3];
                tmp[3] = tmp[5];
                tmp[5] = a;
            }
            else if (dataFormat == DataFormat.ABCD) {
                byte a = tmp[0];
                tmp[0] = tmp[7];
                tmp[7] = a;

                a = tmp[1];
                tmp[1] = tmp[6];
                tmp[6] = a;

                a = tmp[2];
                tmp[2] = tmp[5];
                tmp[5] = a;

                a = tmp[3];
                tmp[3] = tmp[4];
                tmp[4] = a;
            }
        }
        else {
            for (int i = 0; i < length / 2; i++) {
                byte b = tmp[i * 2 + 0];
                tmp[i * 2 + 0] = tmp[i * 2 + 1];
                tmp[i * 2 + 1] = b;
            }
        }

        return tmp;
    }

    private byte[] ReverseBytesByWord(byte[] buffer, DataFormat dataFormat) {
        return this.ReverseBytesByWord(buffer, 0, buffer.Length, dataFormat);
    }

    [TestMethod]
    public void ReverseBytesByWordTest1() {
        byte[] data = new byte[4] { 0x46, 0x38, 0xA0, 0xB0 };
        byte[] buffer = this.ReverseBytesByWord(data, DataFormat.ABCD);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(new byte[] { 0xB0, 0xA0, 0x38, 0x46 }, buffer));
    }

    [TestMethod]
    public void ReverseBytesByWordTest2() {
        byte[] data = new byte[4] { 0x46, 0x38, 0xA0, 0xB0 };
        byte[] buffer = this.ReverseBytesByWord(data, DataFormat.BADC);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(new byte[] { 0xA0, 0xB0, 0x46, 0x38 }, buffer));
    }

    [TestMethod]
    public void ReverseBytesByWordTest3() {
        byte[] data = new byte[4] { 0x46, 0x38, 0xA0, 0xB0 };
        byte[] buffer = this.ReverseBytesByWord(data, DataFormat.CDAB);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(new byte[] { 0x38, 0x46, 0xB0, 0xA0 }, buffer));
    }

    [TestMethod]
    public void ReverseBytesByWordTest4() {
        byte[] data = new byte[4] { 0x46, 0x38, 0xA0, 0xB0 };
        byte[] buffer = this.ReverseBytesByWord(data, DataFormat.DCBA);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(new byte[] { 0x46, 0x38, 0xA0, 0xB0 }, buffer));
    }

    [TestMethod]
    public void ReverseBytesByWordTest5() {
        byte[] data = new byte[8] { 0x46, 0x38, 0xA0, 0xB0, 0xFF, 0x3D, 0xC1, 0x08 };
        byte[] buffer = this.ReverseBytesByWord(data, DataFormat.DCBA);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(new byte[] { 0x46, 0x38, 0xA0, 0xB0, 0xFF, 0x3D, 0xC1, 0x08 }, buffer),
            "Data:" + SoftBasic.ByteToHexString(buffer) + " Actual:" + SoftBasic.ByteToHexString(new byte[] { 0x08, 0xC1, 0x3D, 0xFF, 0xB0, 0xA0, 0x38, 0x46 }));
    }

    [TestMethod]
    public void BoolTransferTest() {
        byte[] data = new byte[2] { 0x01, 0x00 };
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(true, this.byteTransform.TransBool(data, 0));
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(false, this.byteTransform.TransBool(data, 1));
    }

    [TestMethod]
    public void ByteToBoolArrayTransferTest() {
        byte[] data = new byte[2] { 0xA3, 0x46 };
        bool[] array = this.byteTransform.TransBool(data, 1, 1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(false, array[7]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(true, array[6]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(false, array[5]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(false, array[4]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(false, array[3]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(true, array[2]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(true, array[1]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(false, array[0]);
    }


    [TestMethod]
    public void BoolArrayToByteTransferTest() {
        byte[] data = new byte[2] { 0xA3, 0x46 };
        bool[] buffer = new bool[] { true, true, false, false, false, true, false, true, false, true, true, false, false, false, true, false };


        byte[] value = this.byteTransform.TransByte(buffer);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, value));
    }

    [TestMethod]
    public void BytesToInt16TransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((short) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((short) -9876).CopyTo(data, 2);
        data = SoftBasic.BytesReverseByWord(data);


        short[] array = this.byteTransform.TransInt16(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<short>(1234, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<short>(-9876, array[1]);
    }

    [TestMethod]
    public void Int16ToBytesTransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((short) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((short) -9876).CopyTo(data, 2);
        data = SoftBasic.BytesReverseByWord(data);

        byte[] buffer = this.byteTransform.TransByte(new short[] { 1234, -9876 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToUInt16TransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((ushort) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((ushort) 54321).CopyTo(data, 2);
        data = SoftBasic.BytesReverseByWord(data);

        ushort[] array = this.byteTransform.TransUInt16(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(1234, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(54321, array[1]);
    }


    [TestMethod]
    public void UInt16ToBytesTransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((ushort) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((ushort) 54321).CopyTo(data, 2);
        data = SoftBasic.BytesReverseByWord(data);

        byte[] buffer = this.byteTransform.TransByte(new ushort[] { 1234, 54321 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToInt32TransferTest() {
        byte[] data = new byte[8];
        this.ReverseBytesByWord(BitConverter.GetBytes(12345678), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-9876654), this.byteTransform.DataFormat).CopyTo(data, 4);

        int[] array = this.byteTransform.TransInt32(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(12345678, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(-9876654, array[1]);
    }

    [TestMethod]
    public void Int32ToBytesTransferTest() {
        byte[] data = new byte[8];
        this.ReverseBytesByWord(BitConverter.GetBytes(12345678), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-9876654), this.byteTransform.DataFormat).CopyTo(data, 4);

        byte[] buffer = this.byteTransform.TransByte(new int[] { 12345678, -9876654 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToUInt32TransferTest() {
        byte[] data = new byte[8];
        this.ReverseBytesByWord(BitConverter.GetBytes((uint) 12345678), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes((uint) 9876654), this.byteTransform.DataFormat).CopyTo(data, 4);

        uint[] array = this.byteTransform.TransUInt32(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<uint>(12345678, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<uint>(9876654, array[1]);
    }


    [TestMethod]
    public void UInt32ToBytesTransferTest() {
        byte[] data = new byte[8];
        this.ReverseBytesByWord(BitConverter.GetBytes((uint) 12345678), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes((uint) 9876654), this.byteTransform.DataFormat).CopyTo(data, 4);

        byte[] buffer = this.byteTransform.TransByte(new uint[] { 12345678, 9876654 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToInt64TransferTest() {
        byte[] data = new byte[16];
        this.ReverseBytesByWord(BitConverter.GetBytes(12345678911234L), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-987665434123245L), this.byteTransform.DataFormat).CopyTo(data, 8);

        long[] array = this.byteTransform.TransInt64(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<long>(12345678911234L, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<long>(-987665434123245L, array[1]);
    }

    [TestMethod]
    public void Int64ToBytesTransferTest() {
        byte[] data = new byte[16];
        this.ReverseBytesByWord(BitConverter.GetBytes(12345678911234L), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-987665434123245L), this.byteTransform.DataFormat).CopyTo(data, 8);

        byte[] buffer = this.byteTransform.TransByte(new long[] { 12345678911234L, -987665434123245L });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToUInt64TransferTest() {
        byte[] data = new byte[16];
        this.ReverseBytesByWord(BitConverter.GetBytes(1234567812123334123UL), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(92353421232423213UL), this.byteTransform.DataFormat).CopyTo(data, 8);

        ulong[] array = this.byteTransform.TransUInt64(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ulong>(1234567812123334123UL, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ulong>(92353421232423213UL, array[1]);
    }


    [TestMethod]
    public void UInt64ToBytesTransferTest() {
        byte[] data = new byte[16];
        this.ReverseBytesByWord(BitConverter.GetBytes(1234567812123334123UL), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(92353421232423213UL), this.byteTransform.DataFormat).CopyTo(data, 8);

        byte[] buffer = this.byteTransform.TransByte(new ulong[] { 1234567812123334123UL, 92353421232423213UL });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToFloatTransferTest() {
        byte[] data = new byte[8];
        this.ReverseBytesByWord(BitConverter.GetBytes(123.456f), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-0.001234f), this.byteTransform.DataFormat).CopyTo(data, 4);

        float[] array = this.byteTransform.TransSingle(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<float>(123.456f, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<float>(-0.001234f, array[1]);
    }

    [TestMethod]
    public void FloatToBytesTransferTest() {
        byte[] data = new byte[8];
        this.ReverseBytesByWord(BitConverter.GetBytes(123.456f), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-0.001234f), this.byteTransform.DataFormat).CopyTo(data, 4);

        byte[] buffer = this.byteTransform.TransByte(new float[] { 123.456f, -0.001234f });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToDoubleTransferTest() {
        byte[] data = new byte[16];
        this.ReverseBytesByWord(BitConverter.GetBytes(123.456789D), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-0.00000123D), this.byteTransform.DataFormat).CopyTo(data, 8);

        double[] array = this.byteTransform.TransDouble(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<double>(123.456789D, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<double>(-0.00000123D, array[1]);
    }

    [TestMethod]
    public void DoubleToBytesTransferTest() {
        byte[] data = new byte[16];
        this.ReverseBytesByWord(BitConverter.GetBytes(123.456789D), this.byteTransform.DataFormat).CopyTo(data, 0);
        this.ReverseBytesByWord(BitConverter.GetBytes(-0.00000123D), this.byteTransform.DataFormat).CopyTo(data, 8);

        byte[] buffer = this.byteTransform.TransByte(new double[] { 123.456789D, -0.00000123D });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToStringTransferTest() {
        byte[] data = Encoding.ASCII.GetBytes("ABCDEFG5");

        string str = this.byteTransform.TransString(data, 0, 8, Encoding.ASCII);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<string>("ABCDEFG5", str);
    }


    [TestMethod]
    public void StringToBytesTransferTest() {
        byte[] data = Encoding.ASCII.GetBytes("ABCDEFG5");

        byte[] buffer = this.byteTransform.TransByte("ABCDEFG5", Encoding.ASCII);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(data, buffer));
    }
}