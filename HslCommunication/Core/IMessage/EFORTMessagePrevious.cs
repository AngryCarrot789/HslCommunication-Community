﻿namespace HslCommunication.Core.IMessage;

/// <summary>
/// 旧版的机器人的消息类对象，保留此类为了实现兼容
/// </summary>
public class EFORTMessagePrevious : INetMessage {
    /// <summary>
    /// 消息头的指令长度
    /// </summary>
    public int ProtocolHeadBytesLength {
        get { return 17; }
    }

    /// <summary>
    /// 从当前的头子节文件中提取出接下来需要接收的数据长度
    /// </summary>
    /// <returns>返回接下来的数据内容长度</returns>
    public int GetContentLengthByHeadBytes() {
        return BitConverter.ToInt16(this.HeadBytes, 15) - 17;
    }

    /// <summary>
    /// 检查头子节的合法性
    /// </summary>
    /// <param name="token">特殊的令牌，有些特殊消息的验证</param>
    /// <returns>是否合法</returns>
    public bool CheckHeadBytesLegal(byte[] token) {
        if (this.HeadBytes == null)
            return false;
        return true;
    }

    /// <summary>
    /// 获取头子节里的消息标识
    /// </summary>
    /// <returns>标识信息</returns>
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