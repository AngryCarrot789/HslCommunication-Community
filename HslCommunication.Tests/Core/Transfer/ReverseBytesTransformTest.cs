using System.Text;
using HslCommunication.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.Core.Transfer;

[TestClass]
public class ReverseBytesTransformTest {
    public ReverseBytesTransformTest() {
        this.byteTransform = new ReverseBytesTransform();
    }

    private ReverseBytesTransform byteTransform;

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
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, value));
    }

    [TestMethod]
    public void BytesToInt16TransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((short) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((short) -9876).CopyTo(data, 2);
        Array.Reverse(data, 0, 2);
        Array.Reverse(data, 2, 2);


        short[] array = this.byteTransform.TransInt16(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<short>(1234, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<short>(-9876, array[1]);
    }

    [TestMethod]
    public void Int16ToBytesTransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((short) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((short) -9876).CopyTo(data, 2);
        Array.Reverse(data, 0, 2);
        Array.Reverse(data, 2, 2);

        byte[] buffer = this.byteTransform.TransByte(new short[] { 1234, -9876 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToUInt16TransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((ushort) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((ushort) 54321).CopyTo(data, 2);
        Array.Reverse(data, 0, 2);
        Array.Reverse(data, 2, 2);

        ushort[] array = this.byteTransform.TransUInt16(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(1234, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ushort>(54321, array[1]);
    }


    [TestMethod]
    public void UInt16ToBytesTransferTest() {
        byte[] data = new byte[4];
        BitConverter.GetBytes((ushort) 1234).CopyTo(data, 0);
        BitConverter.GetBytes((ushort) 54321).CopyTo(data, 2);
        Array.Reverse(data, 0, 2);
        Array.Reverse(data, 2, 2);

        byte[] buffer = this.byteTransform.TransByte(new ushort[] { 1234, 54321 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToInt32TransferTest() {
        byte[] data = new byte[8];
        BitConverter.GetBytes(12345678).CopyTo(data, 0);
        BitConverter.GetBytes(-9876654).CopyTo(data, 4);
        Array.Reverse(data, 0, 4);
        Array.Reverse(data, 4, 4);

        int[] array = this.byteTransform.TransInt32(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(12345678, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<int>(-9876654, array[1]);
    }

    [TestMethod]
    public void Int32ToBytesTransferTest() {
        byte[] data = new byte[8];
        BitConverter.GetBytes(12345678).CopyTo(data, 0);
        BitConverter.GetBytes(-9876654).CopyTo(data, 4);
        Array.Reverse(data, 0, 4);
        Array.Reverse(data, 4, 4);

        byte[] buffer = this.byteTransform.TransByte(new int[] { 12345678, -9876654 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToUInt32TransferTest() {
        byte[] data = new byte[8];
        BitConverter.GetBytes((uint) 12345678).CopyTo(data, 0);
        BitConverter.GetBytes((uint) 9876654).CopyTo(data, 4);
        Array.Reverse(data, 0, 4);
        Array.Reverse(data, 4, 4);

        uint[] array = this.byteTransform.TransUInt32(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<uint>(12345678, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<uint>(9876654, array[1]);
    }


    [TestMethod]
    public void UInt32ToBytesTransferTest() {
        byte[] data = new byte[8];
        BitConverter.GetBytes((uint) 12345678).CopyTo(data, 0);
        BitConverter.GetBytes((uint) 9876654).CopyTo(data, 4);
        Array.Reverse(data, 0, 4);
        Array.Reverse(data, 4, 4);

        byte[] buffer = this.byteTransform.TransByte(new uint[] { 12345678, 9876654 });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToInt64TransferTest() {
        byte[] data = new byte[16];
        BitConverter.GetBytes(12345678911234L).CopyTo(data, 0);
        BitConverter.GetBytes(-987665434123245L).CopyTo(data, 8);
        Array.Reverse(data, 0, 8);
        Array.Reverse(data, 8, 8);

        long[] array = this.byteTransform.TransInt64(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<long>(12345678911234L, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<long>(-987665434123245L, array[1]);
    }

    [TestMethod]
    public void Int64ToBytesTransferTest() {
        byte[] data = new byte[16];
        BitConverter.GetBytes(12345678911234L).CopyTo(data, 0);
        BitConverter.GetBytes(-987665434123245L).CopyTo(data, 8);
        Array.Reverse(data, 0, 8);
        Array.Reverse(data, 8, 8);

        byte[] buffer = this.byteTransform.TransByte(new long[] { 12345678911234L, -987665434123245L });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToUInt64TransferTest() {
        byte[] data = new byte[16];
        BitConverter.GetBytes(1234567812123334123UL).CopyTo(data, 0);
        BitConverter.GetBytes(92353421232423213UL).CopyTo(data, 8);
        Array.Reverse(data, 0, 8);
        Array.Reverse(data, 8, 8);

        ulong[] array = this.byteTransform.TransUInt64(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ulong>(1234567812123334123UL, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<ulong>(92353421232423213UL, array[1]);
    }


    [TestMethod]
    public void UInt64ToBytesTransferTest() {
        byte[] data = new byte[16];
        BitConverter.GetBytes(1234567812123334123UL).CopyTo(data, 0);
        BitConverter.GetBytes(92353421232423213UL).CopyTo(data, 8);
        Array.Reverse(data, 0, 8);
        Array.Reverse(data, 8, 8);

        byte[] buffer = this.byteTransform.TransByte(new ulong[] { 1234567812123334123UL, 92353421232423213UL });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToFloatTransferTest() {
        byte[] data = new byte[8];
        BitConverter.GetBytes(123.456f).CopyTo(data, 0);
        BitConverter.GetBytes(-0.001234f).CopyTo(data, 4);
        Array.Reverse(data, 0, 4);
        Array.Reverse(data, 4, 4);

        float[] array = this.byteTransform.TransSingle(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<float>(123.456f, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<float>(-0.001234f, array[1]);
    }

    [TestMethod]
    public void FloatToBytesTransferTest() {
        byte[] data = new byte[8];
        BitConverter.GetBytes(123.456f).CopyTo(data, 0);
        BitConverter.GetBytes(-0.001234f).CopyTo(data, 4);
        Array.Reverse(data, 0, 4);
        Array.Reverse(data, 4, 4);

        byte[] buffer = this.byteTransform.TransByte(new float[] { 123.456f, -0.001234f });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }

    [TestMethod]
    public void BytesToDoubleTransferTest() {
        byte[] data = new byte[16];
        BitConverter.GetBytes(123.456789D).CopyTo(data, 0);
        BitConverter.GetBytes(-0.00000123D).CopyTo(data, 8);
        Array.Reverse(data, 0, 8);
        Array.Reverse(data, 8, 8);

        double[] array = this.byteTransform.TransDouble(data, 0, 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<double>(123.456789D, array[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual<double>(-0.00000123D, array[1]);
    }

    [TestMethod]
    public void DoubleToBytesTransferTest() {
        byte[] data = new byte[16];
        BitConverter.GetBytes(123.456789D).CopyTo(data, 0);
        BitConverter.GetBytes(-0.00000123D).CopyTo(data, 8);
        Array.Reverse(data, 0, 8);
        Array.Reverse(data, 8, 8);

        byte[] buffer = this.byteTransform.TransByte(new double[] { 123.456789D, -0.00000123D });
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
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
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(HslCommunication.BasicFramework.SoftBasic.IsTwoBytesEquel(data, buffer));
    }
}