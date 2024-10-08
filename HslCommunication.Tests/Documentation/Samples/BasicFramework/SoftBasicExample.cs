﻿using HslCommunication.BasicFramework;
using Newtonsoft.Json.Linq;

namespace HslCommunication.Tests.Documentation.Samples.BasicFramework;

public class SoftBasicExample {
    public void CalculateFileMD5Example() {
        try {
            string md5 = SoftBasic.CalculateFileMD5("D:\\123.txt");

            Console.WriteLine(md5);
        }
        catch (Exception ex) {
            Console.WriteLine("failed : " + ex.Message);
        }
    }

    public void CalculateStreamMD5Example1() {
        try {
            // stream 可以是文件流，网络流，内存流
            Stream stream = File.OpenRead("D:\\123.txt");

            string md5 = SoftBasic.CalculateStreamMD5(stream);

            Console.WriteLine(md5);
        }
        catch (Exception ex) {
            Console.WriteLine("failed : " + ex.Message);
        }
    }

    public void GetSizeDescriptionExample() {
        string size = SoftBasic.GetSizeDescription(1234254123);

        // 1.15 Gb
        Console.WriteLine(size);
    }

    public void GetTimeSpanDescriptionExample() {
        string size = SoftBasic.GetTimeSpanDescription(TimeSpan.FromMinutes(12.3d));

        // 12.3 分钟
        Console.WriteLine(size);
    }

    public void AddArrayDataExample() {
        int[] old = new int[5] { 1234, 1235, 1236, 1237, 1238 };
        int[] tmp = new int[2] { 456, 457 };


        SoftBasic.AddArrayData(ref old, tmp, 6);
        foreach (int m in old) {
            Console.Write(m + " ");
        }

        // 输出 1235, 1236, 1237, 1238, 456, 457
    }

    public void ArrayExpandToLengthExample() {
        int[] old = new int[5] { 1234, 1235, 1236, 1237, 1238 };
        old = SoftBasic.ArrayExpandToLength(old, 8);

        foreach (int m in old) {
            Console.Write(m + " ");
        }

        // 输出 1234, 1235, 1236, 1237, 1238, 0, 0, 0 
    }

    public void ArrayExpandToLengthEvenExample() {
        int[] old = new int[5] { 1234, 1235, 1236, 1237, 1238 };
        old = SoftBasic.ArrayExpandToLengthEven(old);

        foreach (int m in old) {
            Console.Write(m + " ");
        }

        // 输出 1234, 1235, 1236, 1237, 1238, 0 
    }

    public void ArraySplitByLengthExample() {
        int[] b1 = new int[10] { 12341, -2324, 84646, 324245, 352, 654332, 7687632, 435, 234, 3434 };
        List<int[]> b2 = SoftBasic.ArraySplitByLength(b1, 4);

        // b2 共有3个数组
        // 数组1   [12341, -2324, 84646, 324245]
        // 数组2   [352, 654332, 7687632, 435]
        // 数组3   [234, 3434]
    }

    public void SplitIntegerToArrayExample() {
        int[] b1 = SoftBasic.SplitIntegerToArray(10, 10);
        // b1为 [10]

        int[] b2 = SoftBasic.SplitIntegerToArray(10, 5);
        // b2为 [5,5]

        int[] b3 = SoftBasic.SplitIntegerToArray(10, 4);
        // b3为 [4,4,2]
    }

    public void IsTwoBytesEquelExample1() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        byte[] b2 = new byte[] { 0x12, 0xC6, 0x25, 0x3C, 0x42, 0x85, 0x5B, 0x05, 0x12, 0x87 };

        Console.WriteLine(SoftBasic.IsTwoBytesEquel(b1, 3, b2, 5, 4));

        // 输出 true
    }

    public void IsTwoBytesEquelExample2() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        byte[] b2 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };

        Console.WriteLine(SoftBasic.IsTwoBytesEquel(b1, b2));

        // 输出 true
    }

    public void IsTwoTokenEquelExample() {
        Guid guid = new Guid("56b79cac-91e8-460f-95ce-72b39e19185e");
        byte[] b2 = new byte[32];
        guid.ToByteArray().CopyTo(b2, 12);

        Console.WriteLine(SoftBasic.IsByteTokenEquel(b2, guid));

        // 输出 true
    }

    public void GetEnumValuesExample() {
        FileMode[] modes = SoftBasic.GetEnumValues<FileMode>();

        foreach (FileMode m in modes) {
            Console.WriteLine(m);
        }

        // 输出
        // Append
        // Create
        // CreateNew
        // Open
        // OpenOrCreate
        // Truncate
    }

    public void GetEnumFromStringExample() {
        // 从字符串生成枚举值，可以用来方便的进行数据存储，解析

        FileMode fileMode = SoftBasic.GetEnumFromString<FileMode>("Append");

        if (fileMode == FileMode.Append) {
            // This is true
        }
    }

    public void GetValueFromJsonObjectExample() {
        JObject json = new JObject();
        json.Add("A", new JValue("Abcdea234a"));

        Console.WriteLine("Abcdea234a", SoftBasic.GetValueFromJsonObject(json, "A", ""));

        // 输出 true
    }

    public void JsonSetValueExample() {
        JObject json = new JObject();
        json.Add("A", new JValue("Abcdea234a"));

        SoftBasic.JsonSetValue(json, "B", "1234");
        // json  A:Abcdea234a B:1234
    }

    public void GetExceptionMessageExample1() {
        try {
            int i = 0;
            int j = 10 / i;
        }
        catch (Exception ex) {
            Console.WriteLine(SoftBasic.GetExceptionMessage(ex));
        }
    }

    public void GetExceptionMessageExample2() {
        try {
            int i = 0;
            int j = 10 / i;
        }
        catch (Exception ex) {
            Console.WriteLine("Msg", SoftBasic.GetExceptionMessage(ex));
        }
    }

    public void ByteToHexStringExample1() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
        Console.WriteLine(SoftBasic.ByteToHexString(b1, ' '));

        // 输出 "13 A6 15 85 5B 05 12 36 F2 27";
    }

    public void ByteToHexStringExample2() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };

        Console.WriteLine(SoftBasic.ByteToHexString(b1));

        // 输出 "13A615855B051236F227";
    }

    public void HexStringToBytesExample() {
        // str无论是下面哪种情况，都是等效的
        string str = "13 A6 15 85 5B 05 12 36 F2 27";
        //string str = "13-A6-15-85-5B-05-12-36-F2-27";
        //string str = "13A615855B051236F227";
        //string str = "13_A6_15_85_5B_05_12_36_F2_27";

        byte[] b1 = SoftBasic.HexStringToBytes(str);
        // b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12, 0x36, 0xF2, 0x27 };
    }

    public void BoolArrayToByteExample() {
        bool[] values = new bool[] { true, false, false, true, true, true, false, true, false, false, false, true, false, true, false, false };


        byte[] buffer = SoftBasic.BoolArrayToByte(values);

        // 结果如下
        // buffer = new byte[2] { 0xB9, 0x28 };
    }

    public void ByteToBoolArrayExample() {
        byte[] buffer = new byte[2] { 0xB9, 0x28 };
        bool[] result = SoftBasic.ByteToBoolArray(buffer, 15);

        // 结果如下
        // result = new bool[] { true, false, false, true, true, true, false, true, false, false, false, true, false, true, false };

        bool[] result2 = SoftBasic.ByteToBoolArray(buffer);
        // 结果如下
        // result2 = new bool[] { true, false, false, true, true, true, false, true, false, false, false, true, false, true, false, false };
    }

    public void SpliceTwoByteArrayExample() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85 };
        byte[] b2 = new byte[] { 0x5B, 0x05, 0x12 };

        byte[] buffer = SoftBasic.SpliceTwoByteArray(b1, b2);

        // buffer 的值就是 new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };
    }

    public void BytesArrayRemoveBeginExample() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };

        byte[] buffer = SoftBasic.BytesArrayRemoveBegin(b1, 3);


        // buffer 的值就是b1移除了前三个字节 new byte[] { 0x85, 0x5B, 0x05, 0x12 };
    }

    public void BytesArrayRemoveLastExample() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };

        byte[] buffer = SoftBasic.BytesArrayRemoveLast(b1, 4);

        // buffer 的值就是b1移除了后四个字节 new byte[] { 0x13, 0xA6, 0x15 };
    }


    public void BytesArrayRemoveDoubleExample() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };

        byte[] buffer = SoftBasic.BytesArrayRemoveDouble(b1, 1, 3);

        // buffer的值就是移除了第一个字节数据和最后两个字节数据的新值 new byte[] { 0xA6, 0x15, 0x85 };
    }

    public void GetUniqueStringByGuidAndRandomExample() {
        string uid = SoftBasic.GetUniqueStringByGuidAndRandom();

        // 例子，随机的一串数字，重复概率几乎为0，长度为36位字节
        // ed28ea220cd34fea9fdd07a926be757d4562
    }

    public void BytesReverseByWordExample() {
        byte[] b1 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05, 0x12 };


        byte[] buffer = SoftBasic.BytesReverseByWord(b1);

        // buffer的值就为 = new byte[] { 0xA6, 0x13, 0x85, 0x15, 0x05, 0x5B, 0x00, 0x12 };

        // 再举个例子

        byte[] b2 = new byte[] { 0x13, 0xA6, 0x15, 0x85, 0x5B, 0x05 };

        byte[] buffer2 = SoftBasic.BytesReverseByWord(b1);

        // buffer2的值就是 = new byte[] { 0xA6, 0x13, 0x85, 0x15, 0x05, 0x5B };
    }
}