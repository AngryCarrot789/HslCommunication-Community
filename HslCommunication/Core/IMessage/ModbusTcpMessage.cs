﻿namespace HslCommunication.Core.IMessage;

/// <summary>
/// Modbus-Tcp协议支持的消息解析类
/// </summary>
public class ModbusTcpMessage : INetMessage {
    /// <summary>
    /// 消息头的指令长度
    /// </summary>
    public int ProtocolHeadBytesLength {
        get { return 8; }
    }


    /// <summary>
    /// 从当前的头子节文件中提取出接下来需要接收的数据长度
    /// </summary>
    /// <returns>返回接下来的数据内容长度</returns>
    public int GetContentLengthByHeadBytes() {
        /************************************************************************
         *
         *    说明：为了应对有些特殊的设备，在整个指令的开端会增加一个额外的数据的时候
         *
         ************************************************************************/

        if (this.HeadBytes?.Length >= this.ProtocolHeadBytesLength) {
            int length = this.HeadBytes[4] * 256 + this.HeadBytes[5];
            if (length == 0) {
                byte[] buffer = new byte[this.ProtocolHeadBytesLength - 1];
                for (int i = 0; i < buffer.Length; i++) {
                    buffer[i] = this.HeadBytes[i + 1];
                }

                this.HeadBytes = buffer;
                return this.HeadBytes[5] * 256 + this.HeadBytes[6] - 1;
            }
            else {
                return length - 2;
            }
        }
        else {
            return 0;
        }
    }


    /// <summary>
    /// 检查头子节的合法性
    /// </summary>
    /// <param name="token">特殊的令牌，有些特殊消息的验证</param>
    /// <returns>是否成功的结果</returns>
    public bool CheckHeadBytesLegal(byte[] token) {
        if (this.HeadBytes == null)
            return false;
        if (this.SendBytes[0] != this.HeadBytes[0] || this.SendBytes[1] != this.HeadBytes[1])
            return false;
        return this.HeadBytes[2] == 0x00 && this.HeadBytes[3] == 0x00;
    }


    /// <summary>
    /// 获取头子节里的消息标识
    /// </summary>
    /// <returns>消息标识</returns>
    public int GetHeadBytesIdentity() {
        return this.HeadBytes[0] * 256 + this.HeadBytes[1];
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