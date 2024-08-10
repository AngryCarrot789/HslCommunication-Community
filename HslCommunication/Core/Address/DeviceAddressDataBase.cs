namespace HslCommunication.Core.Address;

/// <summary>
/// Device address data information, usually including the starting address, data type, and length
/// </summary>
public class DeviceAddressDataBase {
    public int AddressStart { get; set; }

    public ushort Length { get; set; }

    /// <summary>
    /// 从指定的地址信息解析成真正的设备地址信息
    /// </summary>
    /// <param name="address">地址信息</param>
    /// <param name="length">数据长度</param>
    public virtual void Parse(string address, ushort length) {
    }
}