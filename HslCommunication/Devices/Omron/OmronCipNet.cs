namespace HslCommunication.Devices.Omron;

/// <summary>
/// 欧姆龙PLC的CIP协议的类，支持NJ,NX,NY系列PLC，支持tag名的方式读写数据
/// </summary>
public class OmronCipNet : AllenBradley.AllenBradleyNet {
    /// <summary>
    /// Instantiate a communication object for a OmronCipNet PLC protocol
    /// </summary>
    public OmronCipNet() : base() {
    }

    /// <summary>
    /// Instantiate a communication object for a OmronCipNet PLC protocol
    /// </summary>
    /// <param name="ipAddress">PLC IpAddress</param>
    /// <param name="port">PLC Port</param>
    public OmronCipNet(string ipAddress, int port = 44818) : base(ipAddress, port) {
    }

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串数据</returns>
    public override string ToString() {
        return $"OmronCipNet[{this.IpAddress}:{this.Port}]";
    }
}