﻿using HslCommunication.BasicFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HslCommunication.Tests.BasicFramework;

[TestClass]
public class SoftBufferTest {
    [TestMethod]
    public void SoftBuffer1() {
        SoftBuffer softBuffer = new SoftBuffer(1000);

        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        softBuffer.SetBytes(b1, 367);

        byte[] b2 = softBuffer.GetBytes(367, b1.Length);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b1, b2), "第一次" + SoftBasic.ByteToHexString(b2));

        byte[] b3 = new byte[] { 0x12, 0xC6, 0x25, 0x3C, 0x42, 0x85, 0x5B, 0x05, 0x12, 0x87 };
        softBuffer.SetBytes(b3, 367 + b1.Length);

        byte[] b4 = softBuffer.GetBytes(367 + b1.Length, b3.Length);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b3, b4), "第二次");

        byte[] b5 = SoftBasic.SpliceTwoByteArray(b1, b3);
        byte[] b6 = softBuffer.GetBytes(367, b1.Length + b3.Length);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b5, b6), "第三次");
    }

    [TestMethod]
    public void BoolTest() {
        SoftBuffer softBuffer = new SoftBuffer(1000);

        softBuffer.SetBool(true, 1234);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(softBuffer.GetBool(1234));

        softBuffer.SetBool(true, 2234);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(softBuffer.GetBool(2234));

        softBuffer.SetBool(false, 2234);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(!softBuffer.GetBool(2234));

        softBuffer.SetBool(true, 8);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(softBuffer.GetByte(1) == 0x01);

        softBuffer.SetBool(new bool[] { true, true, false, false, true, false, true }, 3451);
        bool[] data = softBuffer.GetBool(3451, 7);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(data[0]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(data[1]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(data[2]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(data[3]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(data[4]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(data[5]);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(data[6]);
    }

    [TestMethod]
    public void Int16Test() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new short[] { 123, -123, 24567 }, 328);

        short[] read = softBuffer.GetInt16(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123 && read[1] == -123 && read[2] == 24567);
    }

    [TestMethod]
    public void UInt16Test() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new ushort[] { 123, 42123, 24567 }, 328);

        ushort[] read = softBuffer.GetUInt16(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123 && read[1] == 42123 && read[2] == 24567);
    }

    [TestMethod]
    public void Int32Test() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new int[] { 123456, -12345, 231412 }, 328);

        int[] read = softBuffer.GetInt32(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123456 && read[1] == -12345 && read[2] == 231412);
    }

    [TestMethod]
    public void UInt32Test() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new uint[] { 123, 42123, 24567 }, 328);

        uint[] read = softBuffer.GetUInt32(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123 && read[1] == 42123 && read[2] == 24567);
    }

    [TestMethod]
    public void Int64Test() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new long[] { 123456, -12345, 231412 }, 328);

        long[] read = softBuffer.GetInt64(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123456 && read[1] == -12345 && read[2] == 231412);
    }

    [TestMethod]
    public void UInt64Test() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new ulong[] { 123, 42123, 24567 }, 328);

        ulong[] read = softBuffer.GetUInt64(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123 && read[1] == 42123 && read[2] == 24567);
    }

    [TestMethod]
    public void SingleTest() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new float[] { 123456f, -12345f, 231412f }, 328);

        float[] read = softBuffer.GetSingle(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123456f && read[1] == -12345f && read[2] == 231412f);
    }

    [TestMethod]
    public void DoubleTest() {
        SoftBuffer softBuffer = new SoftBuffer(1000);
        softBuffer.SetValue(new double[] { 123456d, -12345d, 231412d }, 328);

        double[] read = softBuffer.GetDouble(328, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(read[0] == 123456d && read[1] == -12345d && read[2] == 231412d);
    }
}