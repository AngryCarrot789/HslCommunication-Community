﻿namespace HslCommunication.Core.IMessage;

/// <summary>
/// 三菱的Qna兼容3E帧协议解析规则
/// </summary>
public class MelsecQnA3EBinaryMessage : INetMessage {
    /// <summary>
    /// 消息头的指令长度
    /// </summary>
    public int ProtocolHeadBytesLength {
        get {
            return 9;
        }
    }


    /// <summary>
    /// 从当前的头子节文件中提取出接下来需要接收的数据长度
    /// </summary>
    /// <returns>返回接下来的数据内容长度</returns>
    public int GetContentLengthByHeadBytes() {
        return BitConverter.ToUInt16(this.HeadBytes, 7);
    }


    /// <summary>
    /// 检查头子节的合法性
    /// </summary>
    /// <param name="token">特殊的令牌，有些特殊消息的验证</param>
    /// <returns>是否成功的结果</returns>
    public bool CheckHeadBytesLegal(byte[] token) {
        if (this.HeadBytes == null)
            return false;

        if (this.HeadBytes[0] == 0xD0 && this.HeadBytes[1] == 0x00) {
            return true;
        }
        else {
            return false;
        }
    }


    /// <summary>
    /// 获取头子节里的消息标识
    /// </summary>
    /// <returns>消息标识</returns>
    public int GetHeadBytesIdentity() {
        return 0;
    }


    /// <summary>
    /// 消息头字节
    /// </summary>
    public byte[] HeadBytes { get; set; }


    /// <summary>
    /// 消息内容字节
    /// </summary>
    public byte[] ContentBytes { get; set; }


    /// <summary>
    /// 发送的字节信息
    /// </summary>
    public byte[] SendBytes { get; set; }
}