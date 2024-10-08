﻿using System.Text;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Security;

namespace HslCommunication.Core.Net;

/*******************************************************************************
 *
 *    一个通信辅助类，对通信中的数据做了信号区分
 *
 *    Communications helper classes, the data signal in a communications distinction
 *
 *******************************************************************************/

/// <summary>
/// 用于本程序集访问通信的暗号说明
/// </summary>
internal class HslProtocol {
    /// <summary>
    /// 规定所有的网络传输指令头都为32字节
    /// </summary>
    internal const int HeadByteLength = 32;

    /// <summary>
    /// 所有网络通信中的缓冲池数据信息
    /// </summary>
    internal const int ProtocolBufferSize = 1024;


    /// <summary>
    /// 用于心跳程序的暗号信息
    /// </summary>
    internal const int ProtocolCheckSecends = 1;

    /// <summary>
    /// 客户端退出消息
    /// </summary>
    internal const int ProtocolClientQuit = 2;

    /// <summary>
    /// 因为客户端达到上限而拒绝登录
    /// </summary>
    internal const int ProtocolClientRefuseLogin = 3;

    /// <summary>
    /// 允许客户端登录到服务器
    /// </summary>
    internal const int ProtocolClientAllowLogin = 4;

    /// <summary>
    /// 客户端登录的暗号信息
    /// </summary>
    internal const int ProtocolAccountLogin = 5;


    /// <summary>
    /// 说明发送的只是文本信息
    /// </summary>
    internal const int ProtocolUserString = 1001;

    /// <summary>
    /// 发送的数据就是普通的字节数组
    /// </summary>
    internal const int ProtocolUserBytes = 1002;

    /// <summary>
    /// 发送的数据就是普通的图片数据
    /// </summary>
    internal const int ProtocolUserBitmap = 1003;

    /// <summary>
    /// 发送的数据是一条异常的数据，字符串为异常消息
    /// </summary>
    internal const int ProtocolUserException = 1004;

    /// <summary>
    /// 说明发送的数据是字符串的数组
    /// </summary>
    internal const int ProtocolUserStringArray = 1005;


    /// <summary>
    /// 请求文件下载的暗号
    /// </summary>
    internal const int ProtocolFileDownload = 2001;

    /// <summary>
    /// 请求文件上传的暗号
    /// </summary>
    internal const int ProtocolFileUpload = 2002;

    /// <summary>
    /// 请求删除文件的暗号
    /// </summary>
    internal const int ProtocolFileDelete = 2003;

    /// <summary>
    /// 文件校验成功
    /// </summary>
    internal const int ProtocolFileCheckRight = 2004;

    /// <summary>
    /// 文件校验失败
    /// </summary>
    internal const int ProtocolFileCheckError = 2005;

    /// <summary>
    /// 文件保存失败
    /// </summary>
    internal const int ProtocolFileSaveError = 2006;

    /// <summary>
    /// 请求文件列表的暗号
    /// </summary>
    internal const int ProtocolFileDirectoryFiles = 2007;

    /// <summary>
    /// 请求子文件的列表暗号
    /// </summary>
    internal const int ProtocolFileDirectories = 2008;

    /// <summary>
    /// 进度返回暗号
    /// </summary>
    internal const int ProtocolProgressReport = 2009;

    /// <summary>
    /// 返回的错误信息
    /// </summary>
    internal const int ProtocolErrorMsg = 2010;


    /// <summary>
    /// 不压缩数据字节
    /// </summary>
    internal const int ProtocolNoZipped = 3001;

    /// <summary>
    /// 压缩数据字节
    /// </summary>
    internal const int ProtocolZipped = 3002;


    /// <summary>
    /// 生成终极传送指令的方法，所有的数据均通过该方法出来
    /// </summary>
    /// <param name="command">命令头</param>
    /// <param name="customer">自用自定义</param>
    /// <param name="token">令牌</param>
    /// <param name="data">字节数据</param>
    /// <returns>包装后的数据信息</returns>
    internal static byte[] CommandBytes(int command, int customer, Guid token, byte[] data) {
        byte[] _temp = null;
        int _zipped = ProtocolNoZipped;
        int _sendLength = 0;
        if (data == null) {
            _temp = new byte[HeadByteLength];
        }
        else {
            // 加密
            data = HslSecurity.ByteEncrypt(data);
            if (data.Length > 102400) {
                // 100K以上的数据，进行数据压缩
                data = SoftZipped.CompressBytes(data);
                _zipped = ProtocolZipped;
            }

            _temp = new byte[HeadByteLength + data.Length];
            _sendLength = data.Length;
        }

        BitConverter.GetBytes(command).CopyTo(_temp, 0);
        BitConverter.GetBytes(customer).CopyTo(_temp, 4);
        BitConverter.GetBytes(_zipped).CopyTo(_temp, 8);
        token.ToByteArray().CopyTo(_temp, 12);
        BitConverter.GetBytes(_sendLength).CopyTo(_temp, 28);
        if (_sendLength > 0) {
            Array.Copy(data, 0, _temp, 32, _sendLength);
        }

        return _temp;
    }


    /// <summary>
    /// 解析接收到数据，先解压缩后进行解密
    /// </summary>
    /// <param name="head">指令头</param>
    /// <param name="content">指令的内容</param>
    /// <return>真实的数据内容</return>
    internal static byte[] CommandAnalysis(byte[] head, byte[] content) {
        if (content != null) {
            int _zipped = BitConverter.ToInt32(head, 8);
            // 先进行解压
            if (_zipped == ProtocolZipped) {
                content = SoftZipped.Decompress(content);
            }

            // 进行解密
            return HslSecurity.ByteDecrypt(content);
        }
        else {
            return null;
        }
    }


    /// <summary>
    /// 获取发送字节数据的实际数据，带指令头
    /// </summary>
    /// <param name="customer">用户数据</param>
    /// <param name="token">令牌</param>
    /// <param name="data">字节信息</param>
    /// <returns>包装后的指令信息</returns>
    internal static byte[] CommandBytes(int customer, Guid token, byte[] data) {
        return CommandBytes(ProtocolUserBytes, customer, token, data);
    }


    /// <summary>
    /// 获取发送字节数据的实际数据，带指令头
    /// </summary>
    /// <param name="customer">用户数据</param>
    /// <param name="token">令牌</param>
    /// <param name="data">字符串数据信息</param>
    /// <returns>包装后的指令信息</returns>
    internal static byte[] CommandBytes(int customer, Guid token, string data) {
        if (data == null)
            return CommandBytes(ProtocolUserString, customer, token, null);
        else
            return CommandBytes(ProtocolUserString, customer, token, Encoding.Unicode.GetBytes(data));
    }

    /// <summary>
    /// 获取发送字节数据的实际数据，带指令头
    /// </summary>
    /// <param name="customer">用户数据</param>
    /// <param name="token">令牌</param>
    /// <param name="data">字符串数据信息</param>
    /// <returns>包装后的指令信息</returns>
    internal static byte[] CommandBytes(int customer, Guid token, string[] data) {
        return CommandBytes(ProtocolUserStringArray, customer, token, PackStringArrayToByte(data));
    }

    /// <summary>
    /// 将字符串打包成字节数组内容
    /// </summary>
    /// <param name="data">字符串数组</param>
    /// <returns>打包后的原始数据内容</returns>
    internal static byte[] PackStringArrayToByte(string[] data) {
        if (data == null)
            data = Array.Empty<string>();

        List<byte> buffer = new List<byte>();
        buffer.AddRange(BitConverter.GetBytes(data.Length));

        for (int i = 0; i < data.Length; i++) {
            if (!string.IsNullOrEmpty(data[i])) {
                byte[] tmp = Encoding.Unicode.GetBytes(data[i]);
                buffer.AddRange(BitConverter.GetBytes(tmp.Length));
                buffer.AddRange(tmp);
            }
            else {
                buffer.AddRange(BitConverter.GetBytes(0));
            }
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// 将字节数组还原成真实的字符串数组
    /// </summary>
    /// <param name="content">原始字节数组</param>
    /// <returns>解析后的字符串内容</returns>
    internal static string[] UnPackStringArrayFromByte(byte[] content) {
        if (content?.Length < 4)
            return null;

        int index = 0;
        int count = BitConverter.ToInt32(content, index);
        string[] result = new string[count];
        index += 4;
        for (int i = 0; i < count; i++) {
            int length = BitConverter.ToInt32(content, index);
            index += 4;
            if (length > 0)
                result[i] = Encoding.Unicode.GetString(content, index, length);
            else
                result[i] = string.Empty;
            index += length;
        }

        return result;
    }
}