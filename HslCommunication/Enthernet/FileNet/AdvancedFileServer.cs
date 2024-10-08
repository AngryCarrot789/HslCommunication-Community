﻿using System.Net;
using System.Net.Sockets;
using HslCommunication.Core.Net;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Types;

namespace HslCommunication.Enthernet.FileNet;

/// <summary>
/// 文件管理类服务器，负责服务器所有分类文件的管理，特点是不支持文件附加数据，但是支持直接访问文件名
/// </summary>
/// <remarks>
/// 本文件的服务器不支持存储文件携带的额外信息，是直接将文件存放在服务器指定目录下的，文件名不更改，特点是服务器查看方便。
/// </remarks>
/// <example>
/// 以下的示例来自Demo项目，创建了一个简单的服务器对象。
/// <code lang="cs" source="TestProject\FileNetServer\FormFileServer.cs" region="Advanced Server" title="AdvancedFileServer示例" />
/// </example>
public class AdvancedFileServer : NetworkFileServerBase {
    /// <summary>
    /// 实例化一个对象
    /// </summary>
    public AdvancedFileServer() {
    }

    /// <summary>
    /// 当接收到了新的请求的时候执行的操作
    /// </summary>
    /// <param name="socket">异步对象</param>
    /// <param name="endPoint">终结点</param>
    protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint) {
        OperateResult result = new OperateResult();
        // 获取ip地址
        string IpAddress = ((IPEndPoint) (socket.RemoteEndPoint)).Address.ToString();

        // 接收操作信息
        OperateResult infoResult = this.ReceiveInformationHead(
            socket,
            out int customer,
            out string fileName,
            out string Factory,
            out string Group,
            out string Identify);

        if (!infoResult.IsSuccess) {
            Console.WriteLine(infoResult.ToMessageShowString());
            return;
        }

        string relativeName = this.ReturnRelativeFileName(Factory, Group, Identify, fileName);

        // 操作分流

        if (customer == HslProtocol.ProtocolFileDownload) {
            string fullFileName = this.ReturnAbsoluteFileName(Factory, Group, Identify, fileName);

            // 发送文件数据
            OperateResult sendFile = this.SendFileAndCheckReceive(socket, fullFileName, fileName, "", "");
            if (!sendFile.IsSuccess) {
                this.LogNet?.WriteError(this.ToString(), $"{StringResources.Language.FileDownloadFailed}:{relativeName} ip:{IpAddress} reason：{sendFile.Message}");
                return;
            }
            else {
                socket?.Close();
                this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileDownloadSuccess + ":" + relativeName);
            }
        }
        else if (customer == HslProtocol.ProtocolFileUpload) {
            string tempFileName = this.FilesDirectoryPathTemp + "\\" + this.CreateRandomFileName();

            string fullFileName = this.ReturnAbsoluteFileName(Factory, Group, Identify, fileName);

            // 上传文件
            this.CheckFolderAndCreate();

            // 创建新的文件夹
            try {
                FileInfo info = new FileInfo(fullFileName);
                if (!Directory.Exists(info.DirectoryName)) {
                    Directory.CreateDirectory(info.DirectoryName);
                }
            }
            catch (Exception ex) {
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.FilePathCreateFailed + fullFileName, ex);
                socket?.Close();
                return;
            }

            OperateResult receiveFile = this.ReceiveFileFromSocketAndMoveFile(
                socket, // 网络套接字
                tempFileName, // 临时保存文件路径
                fullFileName, // 最终保存文件路径
                out string FileName, // 文件名称，从客户端上传到服务器时，为上传人
                out long FileSize,
                out string FileTag,
                out string FileUpload
            );

            if (receiveFile.IsSuccess) {
                socket?.Close();
                this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileUploadSuccess + ":" + relativeName);
            }
            else {
                this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileUploadFailed + ":" + relativeName + " " + StringResources.Language.TextDescription + receiveFile.Message);
            }
        }
        else if (customer == HslProtocol.ProtocolFileDelete) {
            string fullFileName = this.ReturnAbsoluteFileName(Factory, Group, Identify, fileName);

            bool deleteResult = this.DeleteFileByName(fullFileName);

            // 回发消息
            if (this.SendStringAndCheckReceive(
                    socket, // 网络套接字
                    deleteResult ? 1 : 0, // 是否移动成功
                    deleteResult ? StringResources.Language.FileDeleteSuccess : StringResources.Language.FileDeleteFailed
                ).IsSuccess) {
                socket?.Close();
            }

            if (deleteResult)
                this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileDeleteSuccess + ":" + relativeName);
        }
        else if (customer == HslProtocol.ProtocolFileDirectoryFiles) {
            List<GroupFileItem> fileNames = new List<GroupFileItem>();
            foreach (string m in this.GetDirectoryFiles(Factory, Group, Identify)) {
                FileInfo fileInfo = new FileInfo(m);
                fileNames.Add(new GroupFileItem() {
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length,
                });
            }

            Newtonsoft.Json.Linq.JArray jArray = Newtonsoft.Json.Linq.JArray.FromObject(fileNames.ToArray());
            if (this.SendStringAndCheckReceive(
                    socket,
                    HslProtocol.ProtocolFileDirectoryFiles,
                    jArray.ToString()).IsSuccess) {
                socket?.Close();
            }
        }
        else if (customer == HslProtocol.ProtocolFileDirectories) {
            List<string> folders = new List<string>();
            foreach (string m in this.GetDirectories(Factory, Group, Identify)) {
                DirectoryInfo directory = new DirectoryInfo(m);
                folders.Add(directory.Name);
            }

            Newtonsoft.Json.Linq.JArray jArray = Newtonsoft.Json.Linq.JArray.FromObject(folders.ToArray());
            if (this.SendStringAndCheckReceive(
                    socket,
                    HslProtocol.ProtocolFileDirectoryFiles,
                    jArray.ToString()).IsSuccess) {
                socket?.Close();
            }
        }
        else {
            socket?.Close();
        }
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    protected override void StartInitialization() {
        if (string.IsNullOrEmpty(this.FilesDirectoryPathTemp)) {
            throw new ArgumentNullException("FilesDirectoryPathTemp", "No saved path is specified");
        }

        base.StartInitialization();
    }

    /// <summary>
    /// 检查文件夹
    /// </summary>
    protected override void CheckFolderAndCreate() {
        if (!Directory.Exists(this.FilesDirectoryPathTemp)) {
            Directory.CreateDirectory(this.FilesDirectoryPathTemp);
        }

        base.CheckFolderAndCreate();
    }

    /// <summary>
    /// 从网络套接字接收文件并移动到目标的文件夹中，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="savename"></param>
    /// <param name="fileNameNew"></param>
    /// <param name="filename"></param>
    /// <param name="size"></param>
    /// <param name="filetag"></param>
    /// <param name="fileupload"></param>
    /// <returns></returns>
    private OperateResult ReceiveFileFromSocketAndMoveFile(
        Socket socket,
        string savename,
        string fileNameNew,
        out string filename,
        out long size,
        out string filetag,
        out string fileupload) {
        // 先接收文件
        OperateResult<FileBaseInfo> fileInfo = this.ReceiveFileFromSocket(socket, savename, null);
        if (!fileInfo.IsSuccess) {
            this.DeleteFileByName(savename);
            filename = null;
            size = 0;
            filetag = null;
            fileupload = null;
            return fileInfo;
        }

        filename = fileInfo.Content.Name;
        size = fileInfo.Content.Size;
        filetag = fileInfo.Content.Tag;
        fileupload = fileInfo.Content.Upload;


        // 标记移动文件，失败尝试三次
        int customer = 0;
        int times = 0;
        while (times < 3) {
            times++;
            if (this.MoveFileToNewFile(savename, fileNameNew)) {
                customer = 1;
                break;
            }
            else {
                Thread.Sleep(500);
            }
        }

        if (customer == 0) {
            this.DeleteFileByName(savename);
        }

        // 回发消息
        OperateResult sendString = this.SendStringAndCheckReceive(socket, customer, "success");
        return sendString;
    }

    /// <summary>
    /// 用于接收上传文件时的临时文件夹，临时文件使用结束后会被删除
    /// </summary>
    public string FilesDirectoryPathTemp {
        get { return this.m_FilesDirectoryPathTemp; }
        set { this.m_FilesDirectoryPathTemp = this.PreprocessFolderName(value); }
    }

    private string m_FilesDirectoryPathTemp = null;

    /// <summary>
    /// 获取本对象的字符串标识形式
    /// </summary>
    /// <returns>字符串对象</returns>
    public override string ToString() {
        return "AdvancedFileServer";
    }
}