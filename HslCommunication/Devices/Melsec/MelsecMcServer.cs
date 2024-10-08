﻿using System.Net.Sockets;
using HslCommunication.BasicFramework;
using HslCommunication.Core.Address;
using HslCommunication.Core.IMessage;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Transfer;
using HslCommunication.Core.Types;

namespace HslCommunication.Devices.Melsec;

/// <summary>
/// 三菱MC协议的虚拟服务器，支持M,X,Y,D,W的数据池读写操作，使用二进制进行读写操作
/// </summary>
public class MelsecMcServer : NetworkDataServerBase {
    /// <summary>
    /// 实例化一个mc协议的服务器
    /// </summary>
    public MelsecMcServer() {
        // 共计使用了五个数据池
        this.xBuffer = new SoftBuffer(DataPoolLength);
        this.yBuffer = new SoftBuffer(DataPoolLength);
        this.mBuffer = new SoftBuffer(DataPoolLength);
        this.dBuffer = new SoftBuffer(DataPoolLength * 2);
        this.wBuffer = new SoftBuffer(DataPoolLength * 2);

        this.WordLength = 1;
        this.ByteTransform = new RegularByteTransform();
    }

    /// <summary>
    /// 读取自定义的寄存器的值。按照字为单位
    /// </summary>
    /// <param name="address">起始地址，示例："D100"，"M100"</param>
    /// <param name="length">数据长度</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    /// <returns>byte数组值</returns>
    public override OperateResult<byte[]> Read(string address, ushort length) {
        // 分析地址
        OperateResult<McAddressData> analysis = McAddressData.ParseMelsecFrom(address, length);
        if (!analysis.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(analysis);

        if (analysis.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode) {
            bool[] buffer = this.mBuffer.GetBytes(analysis.Content.AddressStart, length * 16).Select(m => m != 0x00).ToArray();
            return OperateResult.CreateSuccessResult(SoftBasic.BoolArrayToByte(buffer));
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode) {
            bool[] buffer = this.xBuffer.GetBytes(analysis.Content.AddressStart, length * 16).Select(m => m != 0x00).ToArray();
            return OperateResult.CreateSuccessResult(SoftBasic.BoolArrayToByte(buffer));
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode) {
            bool[] buffer = this.yBuffer.GetBytes(analysis.Content.AddressStart, length * 16).Select(m => m != 0x00).ToArray();
            return OperateResult.CreateSuccessResult(SoftBasic.BoolArrayToByte(buffer));
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.D.DataCode) {
            return OperateResult.CreateSuccessResult(this.dBuffer.GetBytes(analysis.Content.AddressStart * 2, length * 2));
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.W.DataCode) {
            return OperateResult.CreateSuccessResult(this.wBuffer.GetBytes(analysis.Content.AddressStart * 2, length * 2));
        }
        else {
            return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
        }
    }

    /// <summary>
    /// 写入自定义的数据到数据内存中去
    /// </summary>
    /// <param name="address">地址</param>
    /// <param name="value">数据值</param>
    /// <returns>是否写入成功的结果对象</returns>
    public override OperateResult Write(string address, byte[] value) {
        // 分析地址
        OperateResult<McAddressData> analysis = McAddressData.ParseMelsecFrom(address, 0);
        if (!analysis.IsSuccess)
            return OperateResult.CreateFailedResult<byte[]>(analysis);

        if (analysis.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode) {
            byte[] buffer = SoftBasic.ByteToBoolArray(value).Select(m => m ? (byte) 1 : (byte) 0).ToArray();
            this.mBuffer.SetBytes(buffer, analysis.Content.AddressStart);
            return OperateResult.CreateSuccessResult();
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode) {
            byte[] buffer = SoftBasic.ByteToBoolArray(value).Select(m => m ? (byte) 1 : (byte) 0).ToArray();
            this.xBuffer.SetBytes(buffer, analysis.Content.AddressStart);
            return OperateResult.CreateSuccessResult();
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode) {
            byte[] buffer = SoftBasic.ByteToBoolArray(value).Select(m => m ? (byte) 1 : (byte) 0).ToArray();
            this.yBuffer.SetBytes(buffer, analysis.Content.AddressStart);
            return OperateResult.CreateSuccessResult();
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.D.DataCode) {
            this.dBuffer.SetBytes(value, analysis.Content.AddressStart * 2);
            return OperateResult.CreateSuccessResult();
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.W.DataCode) {
            this.wBuffer.SetBytes(value, analysis.Content.AddressStart * 2);
            return OperateResult.CreateSuccessResult();
        }
        else {
            return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
        }
    }

    /// <summary>
    /// 读取指定地址的bool数据对象
    /// </summary>
    /// <param name="address">西门子的地址信息</param>
    /// <returns>带有成功标志的结果对象</returns>
    public OperateResult<bool> ReadBool(string address) {
        OperateResult<bool[]> read = this.ReadBool(address, 1);
        if (!read.IsSuccess)
            return OperateResult.CreateFailedResult<bool>(read);

        return OperateResult.CreateSuccessResult(read.Content[0]);
    }

    /// <summary>
    /// 读取指定地址的bool数据对象
    /// </summary>
    /// <param name="address">三菱的地址信息</param>
    /// <param name="length">数组的长度</param>
    /// <returns>带有成功标志的结果对象</returns>
    public OperateResult<bool[]> ReadBool(string address, ushort length) {
        // 分析地址
        OperateResult<McAddressData> analysis = McAddressData.ParseMelsecFrom(address, 0);
        if (!analysis.IsSuccess)
            return OperateResult.CreateFailedResult<bool[]>(analysis);

        if (analysis.Content.McDataType.DataType == 0)
            return new OperateResult<bool[]>(StringResources.Language.MelsecCurrentTypeNotSupportedWordOperate);

        if (analysis.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode) {
            return OperateResult.CreateSuccessResult(this.mBuffer.GetBytes(analysis.Content.AddressStart, length).Select(m => m != 0x00).ToArray());
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode) {
            return OperateResult.CreateSuccessResult(this.xBuffer.GetBytes(analysis.Content.AddressStart, length).Select(m => m != 0x00).ToArray());
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode) {
            return OperateResult.CreateSuccessResult(this.yBuffer.GetBytes(analysis.Content.AddressStart, length).Select(m => m != 0x00).ToArray());
        }
        else {
            return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
        }
    }

    /// <summary>
    /// 往指定的地址里写入bool数据对象
    /// </summary>
    /// <param name="address">三菱的地址信息</param>
    /// <param name="value">值</param>
    /// <returns>是否成功的结果</returns>
    public OperateResult Write(string address, bool value) {
        return this.Write(address, new bool[] { value });
    }

    /// <summary>
    /// 往指定的地址里写入bool数组对象
    /// </summary>
    /// <param name="address">三菱的地址信息</param>
    /// <param name="value">值</param>
    /// <returns>是否成功的结果</returns>
    public OperateResult Write(string address, bool[] value) {
        // 分析地址
        OperateResult<McAddressData> analysis = McAddressData.ParseMelsecFrom(address, 0);
        if (!analysis.IsSuccess)
            return OperateResult.CreateFailedResult<bool[]>(analysis);

        if (analysis.Content.McDataType.DataType == 0)
            return new OperateResult<bool[]>(StringResources.Language.MelsecCurrentTypeNotSupportedWordOperate);

        if (analysis.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode) {
            this.mBuffer.SetBytes(value.Select(m => m ? (byte) 1 : (byte) 0).ToArray(), analysis.Content.AddressStart);
            return OperateResult.CreateSuccessResult();
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode) {
            this.xBuffer.SetBytes(value.Select(m => m ? (byte) 1 : (byte) 0).ToArray(), analysis.Content.AddressStart);
            return OperateResult.CreateSuccessResult();
        }
        else if (analysis.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode) {
            this.yBuffer.SetBytes(value.Select(m => m ? (byte) 1 : (byte) 0).ToArray(), analysis.Content.AddressStart);
            return OperateResult.CreateSuccessResult();
        }
        else {
            return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
        }
    }

    /// <summary>
    /// 当客户端登录后，进行Ip信息的过滤，然后触发本方法，也就是说之后的客户端需要
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="endPoint">终端节点</param>
    protected override void ThreadPoolLoginAfterClientCheck(Socket socket, System.Net.IPEndPoint endPoint) {
        // 开始接收数据信息
        AppSession appSession = new AppSession();
        appSession.IpEndPoint = endPoint;
        appSession.WorkSocket = socket;
        try {
            socket.BeginReceive(Array.Empty<byte>(), 0, 0, SocketFlags.None, new AsyncCallback(this.SocketAsyncCallBack), appSession);
            this.AddClient(appSession);
        }
        catch {
            socket.Close();
            this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientOfflineInfo, endPoint));
        }
    }

    private void SocketAsyncCallBack(IAsyncResult ar) {
        if (ar.AsyncState is AppSession session) {
            try {
                int receiveCount = session.WorkSocket.EndReceive(ar);

                MelsecQnA3EBinaryMessage mcMessage = new MelsecQnA3EBinaryMessage();
                OperateResult<byte[]> read1 = this.ReceiveByMessage(session.WorkSocket, 5000, mcMessage);
                if (!read1.IsSuccess) {
                    this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientOfflineInfo, session.IpEndPoint));
                    this.RemoveClient(session);
                    return;
                }

                ;

                byte[] back = this.ReadFromMcCore(read1.Content);
                if (back != null) {
                    session.WorkSocket.Send(back);
                }
                else {
                    session.WorkSocket.Close();
                    this.RemoveClient(session);
                    return;
                }

                this.RaiseDataReceived(read1.Content);
                session.WorkSocket.BeginReceive(Array.Empty<byte>(), 0, 0, SocketFlags.None, new AsyncCallback(this.SocketAsyncCallBack), session);
            }
            catch {
                // 关闭连接，记录日志
                session.WorkSocket?.Close();
                this.LogNet?.WriteDebug(this.ToString(), string.Format(StringResources.Language.ClientOfflineInfo, session.IpEndPoint));
                this.RemoveClient(session);
                return;
            }
        }
    }

    /// <summary>
    /// 当收到mc协议的报文的时候应该触发的方法，允许继承重写，来实现自定义的返回，或是数据监听。
    /// </summary>
    /// <param name="mcCore">mc报文</param>
    /// <returns>返回的报文信息</returns>
    protected virtual byte[] ReadFromMcCore(byte[] mcCore) {
        if (mcCore[11] == 0x01 && mcCore[12] == 0x04) {
            // 读数据
            return this.PackCommand(this.ReadByCommand(SoftBasic.BytesArrayRemoveBegin(mcCore, 11)));
        }
        else if (mcCore[11] == 0x01 && mcCore[12] == 0x14) {
            // 写数据
            return this.PackCommand(this.WriteByMessage(SoftBasic.BytesArrayRemoveBegin(mcCore, 11)));
        }
        else {
            return null;
        }
    }

    private byte[] PackCommand(byte[] data) {
        byte[] back = new byte[11 + data.Length];
        SoftBasic.HexStringToBytes("D0 00 00 FF FF 03 00 00 00 00 00").CopyTo(back, 0);
        if (data.Length > 0)
            data.CopyTo(back, 11);

        BitConverter.GetBytes((short) (data.Length + 2)).CopyTo(back, 7);
        return back;
    }

    private byte[] ReadByCommand(byte[] command) {
        if (command[2] == 0x01) {
            // 位读取
            ushort length = this.ByteTransform.TransUInt16(command, 8);
            int startIndex = (command[6] * 65536 + command[5] * 256 + command[4]);

            if (command[7] == MelsecMcDataType.M.DataCode) {
                byte[] buffer = this.mBuffer.GetBytes(startIndex, length);
                return MelsecHelper.TransBoolArrayToByteData(buffer);
            }
            else if (command[7] == MelsecMcDataType.X.DataCode) {
                byte[] buffer = this.xBuffer.GetBytes(startIndex, length);
                return MelsecHelper.TransBoolArrayToByteData(buffer);
            }
            else if (command[7] == MelsecMcDataType.Y.DataCode) {
                byte[] buffer = this.yBuffer.GetBytes(startIndex, length);
                return MelsecHelper.TransBoolArrayToByteData(buffer);
            }
            else {
                throw new Exception(StringResources.Language.NotSupportedDataType);
            }
        }
        else {
            // 字读取
            ushort length = this.ByteTransform.TransUInt16(command, 8);
            int startIndex = (command[6] * 65536 + command[5] * 256 + command[4]);
            if (command[7] == MelsecMcDataType.M.DataCode) {
                bool[] buffer = this.mBuffer.GetBytes(startIndex, length * 16).Select(m => m != 0x00).ToArray();
                return SoftBasic.BoolArrayToByte(buffer);
            }
            else if (command[7] == MelsecMcDataType.X.DataCode) {
                bool[] buffer = this.xBuffer.GetBytes(startIndex, length * 16).Select(m => m != 0x00).ToArray();
                return SoftBasic.BoolArrayToByte(buffer);
            }
            else if (command[7] == MelsecMcDataType.Y.DataCode) {
                bool[] buffer = this.yBuffer.GetBytes(startIndex, length * 16).Select(m => m != 0x00).ToArray();
                return SoftBasic.BoolArrayToByte(buffer);
            }
            else if (command[7] == MelsecMcDataType.D.DataCode) {
                return this.dBuffer.GetBytes(startIndex * 2, length * 2);
            }
            else if (command[7] == MelsecMcDataType.W.DataCode) {
                return this.wBuffer.GetBytes(startIndex * 2, length * 2);
            }
            else {
                throw new Exception(StringResources.Language.NotSupportedDataType);
            }
        }
    }


    private byte[] WriteByMessage(byte[] command) {
        if (command[2] == 0x01) {
            // 位写入
            ushort length = this.ByteTransform.TransUInt16(command, 8);
            int startIndex = (command[6] * 65536 + command[5] * 256 + command[4]);

            if (command[7] == MelsecMcDataType.M.DataCode) {
                byte[] buffer = MelsecMcNet.ExtractActualData(SoftBasic.BytesArrayRemoveBegin(command, 10), true).Content;
                this.mBuffer.SetBytes(buffer.Take(length).ToArray(), startIndex);
                return Array.Empty<byte>();
            }
            else if (command[7] == MelsecMcDataType.X.DataCode) {
                byte[] buffer = MelsecMcNet.ExtractActualData(SoftBasic.BytesArrayRemoveBegin(command, 10), true).Content;
                this.xBuffer.SetBytes(buffer.Take(length).ToArray(), startIndex);
                return Array.Empty<byte>();
            }
            else if (command[7] == MelsecMcDataType.Y.DataCode) {
                byte[] buffer = MelsecMcNet.ExtractActualData(SoftBasic.BytesArrayRemoveBegin(command, 10), true).Content;
                this.yBuffer.SetBytes(buffer.Take(length).ToArray(), startIndex);
                return Array.Empty<byte>();
            }
            else {
                throw new Exception(StringResources.Language.NotSupportedDataType);
            }
        }
        else {
            // 字写入
            ushort length = this.ByteTransform.TransUInt16(command, 8);
            int startIndex = (command[6] * 65536 + command[5] * 256 + command[4]);

            if (command[7] == MelsecMcDataType.M.DataCode) {
                byte[] buffer = SoftBasic.ByteToBoolArray(SoftBasic.BytesArrayRemoveBegin(command, 10)).Select(m => m ? (byte) 1 : (byte) 0).ToArray();
                this.mBuffer.SetBytes(buffer, startIndex);
                return Array.Empty<byte>();
            }
            else if (command[7] == MelsecMcDataType.X.DataCode) {
                byte[] buffer = SoftBasic.ByteToBoolArray(SoftBasic.BytesArrayRemoveBegin(command, 10)).Select(m => m ? (byte) 1 : (byte) 0).ToArray();
                this.xBuffer.SetBytes(buffer, startIndex);
                return Array.Empty<byte>();
            }
            else if (command[7] == MelsecMcDataType.Y.DataCode) {
                byte[] buffer = SoftBasic.ByteToBoolArray(SoftBasic.BytesArrayRemoveBegin(command, 10)).Select(m => m ? (byte) 1 : (byte) 0).ToArray();
                this.yBuffer.SetBytes(buffer, startIndex);
                return Array.Empty<byte>();
            }
            else if (command[7] == MelsecMcDataType.D.DataCode) {
                this.dBuffer.SetBytes(SoftBasic.BytesArrayRemoveBegin(command, 10), startIndex * 2);
                return Array.Empty<byte>();
            }
            else if (command[7] == MelsecMcDataType.W.DataCode) {
                this.wBuffer.SetBytes(SoftBasic.BytesArrayRemoveBegin(command, 10), startIndex * 2);
                return Array.Empty<byte>();
            }
            else {
                throw new Exception(StringResources.Language.NotSupportedDataType);
            }
        }
    }

    /// <summary>
    /// 从字节数据加载数据信息
    /// </summary>
    /// <param name="content">字节数据</param>
    protected override void LoadFromBytes(byte[] content) {
        if (content.Length < DataPoolLength * 7)
            throw new Exception("File is not correct");

        this.mBuffer.SetBytes(content, 0, 0, DataPoolLength);
        this.xBuffer.SetBytes(content, DataPoolLength, 0, DataPoolLength);
        this.yBuffer.SetBytes(content, DataPoolLength * 2, 0, DataPoolLength);
        this.dBuffer.SetBytes(content, DataPoolLength * 3, 0, DataPoolLength);
        this.wBuffer.SetBytes(content, DataPoolLength * 5, 0, DataPoolLength);
    }

    /// <summary>
    /// 将数据信息存储到字节数组去
    /// </summary>
    /// <returns>所有的内容</returns>
    protected override byte[] SaveToBytes() {
        byte[] buffer = new byte[DataPoolLength * 7];
        Array.Copy(this.mBuffer.GetBytes(), 0, buffer, 0, DataPoolLength);
        Array.Copy(this.xBuffer.GetBytes(), 0, buffer, DataPoolLength, DataPoolLength);
        Array.Copy(this.yBuffer.GetBytes(), 0, buffer, DataPoolLength * 2, DataPoolLength);
        Array.Copy(this.dBuffer.GetBytes(), 0, buffer, DataPoolLength * 3, DataPoolLength);
        Array.Copy(this.wBuffer.GetBytes(), 0, buffer, DataPoolLength * 5, DataPoolLength);

        return buffer;
    }

    /// <summary>
    /// 释放当前的对象
    /// </summary>
    /// <param name="disposing">是否托管对象</param>
    protected override void Dispose(bool disposing) {
        if (disposing) {
            this.xBuffer?.Dispose();
            this.yBuffer?.Dispose();
            this.mBuffer?.Dispose();
            this.dBuffer?.Dispose();
            this.wBuffer?.Dispose();
        }

        base.Dispose(disposing);
    }

    private SoftBuffer xBuffer; // x寄存器的数据池
    private SoftBuffer yBuffer; // y寄存器的数据池
    private SoftBuffer mBuffer; // m寄存器的数据池
    private SoftBuffer dBuffer; // d寄存器的数据池
    private SoftBuffer wBuffer; // w寄存器的数据池

    private const int DataPoolLength = 65536; // 数据的长度

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串信息</returns>
    public override string ToString() {
        return $"MelsecMcServer[{this.Port}]";
    }
}