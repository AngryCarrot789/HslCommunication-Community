﻿using System.Net.Sockets;
using HslCommunication.Core.Net;
using HslCommunication.Core.Types;

namespace HslCommunication.Enthernet.FileNet;

/// <summary>
/// 与服务器文件引擎交互的客户端类，支持操作Advanced引擎和Ultimate引擎
/// </summary>
/// <remarks>
/// 这里需要需要的是，本客户端支持Advanced引擎和Ultimate引擎文件服务器，服务的类型需要您根据自己的需求来选择。
/// </remarks>
/// <example>
/// 此处只演示创建实例，具体的上传，下载，删除的例子请参照对应的方法
/// <code lang="cs" source="TestProject\HslCommunicationDemo\FormFileClient.cs" region="Intergration File Client" title="IntegrationFileClient示例" />
/// </example>
public class IntegrationFileClient : FileClientBase {
    /// <summary>
    /// 实例化一个对象
    /// </summary>
    public IntegrationFileClient() {
    }

    /// <summary>
    /// 删除服务器的文件操作
    /// </summary>
    /// <param name="fileName">文件名称，带后缀</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <returns>是否成功的结果对象</returns>
    public OperateResult DeleteFile(
        string fileName,
        string factory,
        string group,
        string id) {
        return this.DeleteFileBase(fileName, factory, group, id);
    }

    /// <summary>
    /// 下载服务器的文件到本地的文件操作
    /// </summary>
    /// <param name="fileName">文件名称，带后缀</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <param name="processReport">下载的进度报告</param>
    /// <param name="fileSaveName">准备本地保存的名称</param>
    /// <returns>是否成功的结果对象</returns>
    /// <remarks>
    /// 用于分类的参数<paramref name="factory"/>，<paramref name="group"/>，<paramref name="id"/>中间不需要的可以为空，对应的是服务器上的路径系统。
    /// <br /><br />
    /// <note type="warning">
    /// 失败的原因大多数来自于网络的接收异常，或是服务器不存在文件。
    /// </note>
    /// </remarks>
    /// <example>
    /// <code lang="cs" source="TestProject\HslCommunicationDemo\FormFileClient.cs" region="Download File" title="DownloadFile示例" />
    /// </example>
    public OperateResult DownloadFile(
        string fileName,
        string factory,
        string group,
        string id,
        Action<long, long> processReport,
        string fileSaveName) {
        return this.DownloadFileBase(factory, group, id, fileName, processReport, fileSaveName);
    }

    /// <summary>
    /// 下载服务器的文件到本地的数据流中
    /// </summary>
    /// <param name="fileName">文件名称，带后缀</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <param name="processReport">下载的进度报告</param>
    /// <param name="stream">流数据</param>
    /// <returns>是否成功的结果对象</returns>
    /// <remarks>
    /// 用于分类的参数<paramref name="factory"/>，<paramref name="group"/>，<paramref name="id"/>中间不需要的可以为空，对应的是服务器上的路径系统。
    /// <br /><br />
    /// <note type="warning">
    /// 失败的原因大多数来自于网络的接收异常，或是服务器不存在文件。
    /// </note>
    /// </remarks>
    /// <example>
    /// <code lang="cs" source="TestProject\HslCommunicationDemo\FormFileClient.cs" region="Download File" title="DownloadFile示例" />
    /// </example>
    public OperateResult DownloadFile(
        string fileName,
        string factory,
        string group,
        string id,
        Action<long, long> processReport,
        Stream stream) {
        return this.DownloadFileBase(factory, group, id, fileName, processReport, stream);
    }

    /// <summary>
    /// 上传本地的文件到服务器操作
    /// </summary>
    /// <param name="fileName">本地的完整路径的文件名称</param>
    /// <param name="serverName">服务器存储的文件名称，带后缀</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <param name="fileTag">文件的额外描述</param>
    /// <param name="fileUpload">文件的上传人</param>
    /// <param name="processReport">上传的进度报告</param>
    /// <returns>是否成功的结果对象</returns>
    /// <remarks>
    /// 用于分类的参数<paramref name="factory"/>，<paramref name="group"/>，<paramref name="id"/>中间不需要的可以为空，对应的是服务器上的路径系统。
    /// <br /><br />
    /// <note type="warning">
    /// 失败的原因大多数来自于网络的接收异常，或是客户端不存在文件。
    /// </note>
    /// </remarks>
    /// <example>
    /// <code lang="cs" source="TestProject\HslCommunicationDemo\FormFileClient.cs" region="Upload File" title="UploadFile示例" />
    /// </example>
    public OperateResult UploadFile(
        string fileName,
        string serverName,
        string factory,
        string group,
        string id,
        string fileTag,
        string fileUpload,
        Action<long, long> processReport) {
        return this.UploadFileBase(fileName, serverName, factory, group, id, fileTag, fileUpload, processReport);
    }

    /// <summary>
    /// 上传数据流到服务器操作
    /// </summary>
    /// <param name="stream">数据流内容</param>
    /// <param name="serverName">服务器存储的文件名称，带后缀</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <param name="fileTag">文件的额外描述</param>
    /// <param name="fileUpload">文件的上传人</param>
    /// <param name="processReport">上传的进度报告</param>
    /// <returns>是否成功的结果对象</returns>
    /// <remarks>
    /// 用于分类的参数<paramref name="factory"/>，<paramref name="group"/>，<paramref name="id"/>中间不需要的可以为空，对应的是服务器上的路径系统。
    /// <br /><br />
    /// <note type="warning">
    /// 失败的原因大多数来自于网络的接收异常，或是客户端不存在文件。
    /// </note>
    /// </remarks>
    /// <example>
    /// <code lang="cs" source="TestProject\HslCommunicationDemo\FormFileClient.cs" region="Upload File" title="UploadFile示例" />
    /// </example>
    public OperateResult UploadFile(
        Stream stream,
        string serverName,
        string factory,
        string group,
        string id,
        string fileTag,
        string fileUpload,
        Action<long, long> processReport) {
        return this.UploadFileBase(stream, serverName, factory, group, id, fileTag, fileUpload, processReport);
    }

    /// <summary>
    /// 根据三种分类信息，还原成在服务器的相对路径，包含文件
    /// </summary>
    /// <param name="fileName">文件名称，包含后缀名</param>
    /// <param name="factory">第一类</param>
    /// <param name="group">第二类</param>
    /// <param name="id">第三类</param>
    /// <returns>是否成功的结果对象</returns>
    private string TranslateFileName(string fileName, string factory, string group, string id) {
        string file_save_server_name = fileName;

        if (id.IndexOf('\\') >= 0)
            id = id.Replace('\\', '_');
        if (group.IndexOf('\\') >= 0)
            group = id.Replace('\\', '_');
        if (factory.IndexOf('\\') >= 0)
            id = factory.Replace('\\', '_');


        if (id?.Length > 0)
            file_save_server_name = id + @"\" + file_save_server_name;

        if (group?.Length > 0)
            file_save_server_name = group + @"\" + file_save_server_name;

        if (factory?.Length > 0)
            file_save_server_name = factory + @"\" + file_save_server_name;

        return file_save_server_name;
    }

    /// <summary>
    /// 根据三种分类信息，还原成在服务器的相对路径，仅仅路径
    /// </summary>
    /// <param name="factory">第一类</param>
    /// <param name="group">第二类</param>
    /// <param name="id">第三类</param>
    /// <returns>是否成功的结果对象</returns>
    private string TranslatePathName(string factory, string group, string id) {
        string file_save_server_name = "";

        if (id.IndexOf('\\') >= 0)
            id = id.Replace('\\', '_');
        if (group.IndexOf('\\') >= 0)
            group = id.Replace('\\', '_');
        if (factory.IndexOf('\\') >= 0)
            id = factory.Replace('\\', '_');

        if (id?.Length > 0)
            file_save_server_name = @"\" + id;

        if (group?.Length > 0)
            file_save_server_name = @"\" + group + file_save_server_name;

        if (factory?.Length > 0)
            file_save_server_name = @"\" + factory + file_save_server_name;

        return file_save_server_name;
    }

    /// <summary>
    /// 获取指定路径下的所有的文档
    /// </summary>
    /// <param name="fileNames">获取得到的文件合集</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <returns>是否成功的结果对象</returns>
    /// <remarks>
    /// 用于分类的参数<paramref name="factory"/>，<paramref name="group"/>，<paramref name="id"/>中间不需要的可以为空，对应的是服务器上的路径系统。
    /// <br /><br />
    /// <note type="warning">
    /// 失败的原因大多数来自于网络的接收异常。
    /// </note>
    /// </remarks>
    /// <example>
    /// <code lang="cs" source="TestProject\HslCommunicationDemo\FormFileClient.cs" region="DownloadPathFileNames" title="DownloadPathFileNames示例" />
    /// </example>
    public OperateResult DownloadPathFileNames(
        out GroupFileItem[] fileNames,
        string factory,
        string group,
        string id) {
        return this.DownloadStringArrays(
            out fileNames,
            HslProtocol.ProtocolFileDirectoryFiles,
            factory,
            group,
            id
        );
    }

    /// <summary>
    /// 获取指定路径下的所有的文档
    /// </summary>
    /// <param name="folders">输出结果</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <returns>是否成功的结果对象</returns>
    /// <remarks>
    /// 用于分类的参数<paramref name="factory"/>，<paramref name="group"/>，<paramref name="id"/>中间不需要的可以为空，对应的是服务器上的路径系统。
    /// <br /><br />
    /// <note type="warning">
    /// 失败的原因大多数来自于网络的接收异常。
    /// </note>
    /// </remarks>
    /// <example>
    /// <code lang="cs" source="TestProject\HslCommunicationDemo\FormFileClient.cs" region="DownloadPathFolders" title="DownloadPathFolders示例" />
    /// </example>
    public OperateResult DownloadPathFolders(
        out string[] folders,
        string factory,
        string group,
        string id) {
        return this.DownloadStringArrays(
            out folders,
            HslProtocol.ProtocolFileDirectories,
            factory,
            group,
            id);
    }

    /// <summary>
    /// 获取指定路径下的所有的文档
    /// </summary>
    /// <param name="arrays">想要获取的队列</param>
    /// <param name="protocol">指令</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <typeparam name="T">数组的类型</typeparam>
    /// <returns>是否成功的结果对象</returns>
    private OperateResult DownloadStringArrays<T>(
        out T[] arrays,
        int protocol,
        string factory,
        string group,
        string id) {
        OperateResult result = new OperateResult();
        // 连接服务器
        // connect server
        OperateResult<Socket> socketResult = this.CreateSocketAndConnect(this.ServerIpEndPoint, this.ConnectTimeOut);
        if (!socketResult.IsSuccess) {
            arrays = Array.Empty<T>();
            return socketResult;
        }


        // 上传信息
        OperateResult send = this.SendStringAndCheckReceive(socketResult.Content, protocol, "nosense");
        if (!send.IsSuccess) {
            arrays = Array.Empty<T>();
            return send;
        }

        // 上传三级分类
        OperateResult sendClass = this.SendFactoryGroupId(socketResult.Content, factory, group, id);
        if (!sendClass.IsSuccess) {
            arrays = Array.Empty<T>();
            return sendClass;
        }

        // 接收数据信息
        OperateResult<int, string> receive = this.ReceiveStringContentFromSocket(socketResult.Content);
        if (!receive.IsSuccess) {
            arrays = Array.Empty<T>();
            return receive;
        }

        socketResult.Content?.Close();

        // 数据转化
        try {
            arrays = Newtonsoft.Json.Linq.JArray.Parse(receive.Content2).ToObject<T[]>();
            return OperateResult.CreateSuccessResult();
        }
        catch (Exception ex) {
            arrays = Array.Empty<T>();
            return new OperateResult() {
                Message = ex.Message
            };
        }
    }
}