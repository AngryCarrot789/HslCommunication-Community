namespace HslCommunication.Core.Net;

/// <summary>
/// 文件传送的异步对象
/// </summary>
internal class FileStateObject : StateOneBase {
    /// <summary>
    /// 操作的流
    /// </summary>
    public Stream Stream { get; set; }
}