using HslCommunication.BasicFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace HslCommunication.Tests.BasicFramework;

[TestClass]
public class SoftBasicTest {
    [TestMethod]
    public void GetSizeDescriptionTest() {
        long value = 123564357;
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("117.84 Mb", SoftBasic.GetSizeDescription(value));
        value = 345;
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("345 B", SoftBasic.GetSizeDescription(value));
        value = 453268;
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("442.64 Kb", SoftBasic.GetSizeDescription(value));
        value = 452342343424;
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("421.28 Gb", SoftBasic.GetSizeDescription(value));
    }

    [TestMethod]
    public void AddArrayDataTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85 };
        byte[] b2 = new byte[] { 0x5B, 0x05, 0x12 };
        byte[] b3 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };
        byte[] b4 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B };

        SoftBasic.AddArrayData(ref b1, b2, 7);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b1, b3));
    }

    [TestMethod]
    public void AddArrayDataTest2() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85 };
        byte[] b2 = new byte[] { 0x5B, 0x05, 0x12 };
        byte[] b3 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };
        byte[] b4 = new byte[] { 0x15, 0x85, 0x5B, 0x05, 0x12 };

        SoftBasic.AddArrayData(ref b1, b2, 5);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b1, b4));
    }

    [TestMethod]
    public void ArrayExpandToLengthTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85 };
        byte[] b2 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x00, 0x00, 0x00 };

        byte[] b3 = SoftBasic.ArrayExpandToLength(b1, 7);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, b3));
    }

    [TestMethod]
    public void ArrayExpandToLengthEvenTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15 };
        byte[] b2 = new byte[] { 0x13, 0xA6, 0x15, 0x00 };

        byte[] b3 = SoftBasic.ArrayExpandToLengthEven(b1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, b3));

        b3 = SoftBasic.ArrayExpandToLengthEven(b3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, b3));
    }

    [TestMethod]
    public void ArraySplitByLengthTest() {
        int[] b1 = new int[10] { 12341, -2324, 84646, 324245, 352, 654332, 7687632, 435, 234, 3434 };
        List<int[]> b2 = SoftBasic.ArraySplitByLength(b1, 4);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[0].Length == 4);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[0][0] == 12341);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[0][1] == -2324);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[0][2] == 84646);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[0][3] == 324245);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[1].Length == 4);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[1][0] == 352);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[1][1] == 654332);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[1][2] == 7687632);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[1][3] == 435);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[2].Length == 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[2][0] == 234);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[2][1] == 3434);
    }

    [TestMethod]
    public void SplitIntegerToArrayTest() {
        int[] b1 = SoftBasic.SplitIntegerToArray(10, 10);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b1.Length == 1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b1[0] == 10);

        int[] b2 = SoftBasic.SplitIntegerToArray(10, 5);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2.Length == 2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[0] == 5);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b2[1] == 5);

        int[] b3 = SoftBasic.SplitIntegerToArray(10, 4);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b3.Length == 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b3[0] == 4);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b3[1] == 4);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(b3[2] == 2);
    }

    [TestMethod]
    public void IsTwoBytesEquelTest1() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        byte[] b2 = new byte[] { 0x12, 0xC6, 0x25, 0x3C, 0x42, 0x85, 0x5B, 0x05, 0x12, 0x87 };

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b1, 3, b2, 5, 4));
    }


    [TestMethod]
    public void IsTwoBytesEquelTest2() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        byte[] b2 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b1, b2));
    }

    [TestMethod]
    public void IsTwoBytesEquelTest3() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x13, 0x36, 0xF2, 0x27 };
        byte[] b2 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsFalse(SoftBasic.IsTwoBytesEquel(b1, b2));
    }

    [TestMethod]
    public void IsByteTokenEquelTest() {
        Guid guid = new Guid("56b79cac-91e8-460f-95ce-72b39e19185e");
        byte[] b2 = new byte[32];
        guid.ToByteArray().CopyTo(b2, 12);

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsByteTokenEquel(b2, guid));
    }

    [TestMethod]
    public void IsTwoTokenEquelTest() {
        Guid guid1 = new Guid("56b79cac-91e8-460f-95ce-72b39e19185e");
        Guid guid2 = new Guid("56b79cac-91e8-460f-95ce-72b39e19185e");

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoTokenEquel(guid1, guid2));
    }

    [TestMethod]
    public void GetValueFromJsonObjectTest() {
        JObject json = new JObject();
        json.Add("A", new JValue("Abcdea234a"));

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual("Abcdea234a", SoftBasic.GetValueFromJsonObject(json, "A", ""));
    }


    [TestMethod]
    public void ByteToHexStringTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        string str = "13 A6 15 85 5B 05 12 36 F2 27";

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(str, SoftBasic.ByteToHexString(b1, ' '));
    }

    [TestMethod]
    public void ByteToHexStringTest2() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        string str = "13A615855B051236F227";

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(str, SoftBasic.ByteToHexString(b1));
    }


    [TestMethod]
    public void ByteToHexStringTest3() {
        string str1 = "1234";
        string str2 = "3100320033003400";

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(str2, SoftBasic.ByteToHexString(str1));
    }

    [TestMethod]
    public void HexStringToBytesTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        string str = "13 A6 15 85 5B 05 12 36 F2 27";

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b1, SoftBasic.HexStringToBytes(str)));
    }


    [TestMethod]
    public void BoolArrayToByteTest() {
        bool[] values = new bool[] { true, false, false, true, true, true, false, true, false, false, false, true, false, true, false, false };
        byte[] buffer = new byte[2] { 0xB9, 0x28 };

        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(buffer, SoftBasic.BoolArrayToByte(values)));
    }

    [TestMethod]
    public void ByteToBoolArrayTest() {
        bool[] values = new bool[] { true, false, false, true, true, true, false, true, false, false, false, true, false, true, false };
        byte[] buffer = new byte[2] { 0xB9, 0x28 };
        bool[] result = SoftBasic.ByteToBoolArray(buffer, 15);

        for (int i = 0; i < result.Length; i++) {
            if (values[i] != result[i]) {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("值不一致");
            }
        }
    }

    [TestMethod]
    public void SpliceTwoByteArrayTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85 };
        byte[] b2 = new byte[] { 0x5B, 0x05, 0x12 };
        byte[] b3 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };

        byte[] buffer = SoftBasic.SpliceTwoByteArray(b1, b2);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b3, buffer));
    }

    [TestMethod]
    public void BytesArrayRemoveBeginTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };
        byte[] b2 = new byte[] { 0x85, 0x5B, 0x05, 0x12 };

        byte[] buffer = SoftBasic.BytesArrayRemoveBegin(b1, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, buffer));
    }

    [TestMethod]
    public void BytesArrayRemoveLastTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };
        byte[] b2 = new byte[] { 0x13, 0xA6, 0x15 };

        byte[] buffer = SoftBasic.BytesArrayRemoveLast(b1, 4);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, buffer));
    }

    [TestMethod]
    public void BytesArrayRemoveDoubleTest() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };
        byte[] b2 = new byte[] { 0xA6, 0x15, 0x85 };

        byte[] buffer = SoftBasic.BytesArrayRemoveDouble(b1, 1, 3);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, buffer));
    }

    [TestMethod]
    public void DeepCloneTest() {
        SystemVersion version1 = new SystemVersion("1.2.3");
        SystemVersion version2 = (SystemVersion) SoftBasic.DeepClone(version1);

        if (version1.MainVersion != version2.MainVersion)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("主版本不一致");
        if (version1.SecondaryVersion != version2.SecondaryVersion)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("副版本不一致");
        if (version1.EditVersion != version2.EditVersion)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("修订版不一致");
        if (version1.InnerVersion != version2.InnerVersion)
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail("内部版不一致");
    }


    [TestMethod]
    public void BytesReverseByWordTest1() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };
        byte[] b2 = new byte[] { 0xA6, 0x13, 0x85, 0x15, 0x05, 0x5B, 0x00, 0x12 };


        byte[] buffer = SoftBasic.BytesReverseByWord(b1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, buffer));
    }

    [TestMethod]
    public void BytesReverseByWordTest2() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05 };
        byte[] b2 = new byte[] { 0xA6, 0x13, 0x85, 0x15, 0x05, 0x5B };


        byte[] buffer = SoftBasic.BytesReverseByWord(b1);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(SoftBasic.IsTwoBytesEquel(b2, buffer));
    }

    [TestMethod]
    public void GetEnumFromStringTest() {
        FileMode fileMode = SoftBasic.GetEnumFromString<FileMode>("Append");
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(fileMode == FileMode.Append);
    }

    [TestMethod]
    public void GetTimeSpanDescriptionTest() {
        string size = SoftBasic.GetTimeSpanDescription(TimeSpan.FromMinutes(12.3d));
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(size == "12.3 分钟");

        size = SoftBasic.GetTimeSpanDescription(TimeSpan.FromSeconds(12.3d));
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(size == "12 秒");

        size = SoftBasic.GetTimeSpanDescription(TimeSpan.FromHours(12.3d));
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(size == "12.3 小时");

        size = SoftBasic.GetTimeSpanDescription(TimeSpan.FromDays(12.3d));
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(size == "12.3 天");
    }
}