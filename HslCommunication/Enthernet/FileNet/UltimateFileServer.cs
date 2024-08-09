using System.Net;
using System.Net.Sockets;
using HslCommunication.Core.Net;
using HslCommunication.Core.Net.NetworkBase;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Types;

namespace HslCommunication.Enthernet.FileNet;

/// <summary>
/// 终极文件管理服务器，实现所有的文件分类管理，读写分离，不支持直接访问文件名
/// </summary>
/// <remarks>
/// 本文件的服务器支持存储文件携带的额外信息，文件名被映射成了新的名称，无法在服务器直接查看文件信息。
/// </remarks>
/// <example>
/// 以下的示例来自Demo项目，创建了一个简单的服务器对象。
/// <code lang="cs" source="TestProject\FileNetServer\FormFileServer.cs" region="Ultimate Server" title="UltimateFileServer示例" />
/// </example>
public class UltimateFileServer : NetworkFileServerBase {
    /// <summary>
    /// 实例化一个对象
    /// </summary>
    public UltimateFileServer() {
    }

    /// <summary>
    /// 所有文件组操作的词典锁
    /// </summary>
    internal Dictionary<string, GroupFileContainer> m_dictionary_group_marks = new Dictionary<string, GroupFileContainer>();

    /// <summary>
    /// 词典的锁
    /// </summary>
    private SimpleHybirdLock hybirdLock = new SimpleHybirdLock();

    /// <summary>
    /// 获取当前目录的读写锁，如果没有会自动创建
    /// </summary>
    /// <param name="filePath">相对路径名</param>
    /// <returns>读写锁</returns>
    public GroupFileContainer GetGroupFromFilePath(string filePath) {
        GroupFileContainer GroupFile = null;
        this.hybirdLock.Enter();

        // lock operator
        if (this.m_dictionary_group_marks.ContainsKey(filePath)) {
            GroupFile = this.m_dictionary_group_marks[filePath];
        }
        else {
            GroupFile = new GroupFileContainer(this.LogNet, filePath);
            this.m_dictionary_group_marks.Add(filePath, GroupFile);
        }

        this.hybirdLock.Leave();
        return GroupFile;
    }

    /// <summary>
    /// 从套接字接收文件并保存，更新文件列表
    /// </summary>
    /// <param name="socket">套接字</param>
    /// <param name="savename">保存的文件名</param>
    /// <returns>是否成功的结果对象</returns>
    private OperateResult ReceiveFileFromSocketAndUpdateGroup(
        Socket socket,
        string savename) {
        FileInfo info = new FileInfo(savename);
        string guidName = this.CreateRandomFileName();
        string fileName = info.DirectoryName + "\\" + guidName;

        OperateResult<FileBaseInfo> receive = this.ReceiveFileFromSocket(socket, fileName, null);
        if (!receive.IsSuccess) {
            this.DeleteFileByName(fileName);
            return receive;
        }

        // 更新操作
        GroupFileContainer fileManagment = this.GetGroupFromFilePath(info.DirectoryName);
        string oldName = fileManagment.UpdateFileMappingName(
            info.Name,
            receive.Content.Size,
            guidName,
            receive.Content.Upload,
            receive.Content.Tag
        );

        // 删除旧的文件
        this.DeleteExsistingFile(info.DirectoryName, oldName);


        // 回发消息
        return this.SendStringAndCheckReceive(socket, 1, StringResources.Language.SuccessText);
    }

    /// <summary>
    /// 根据文件的显示名称转化为真实存储的名称
    /// </summary>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <param name="fileName">文件显示名称</param>
    /// <returns>是否成功的结果对象</returns>
    private string TransformFactFileName(string factory, string group, string id, string fileName) {
        string path = this.ReturnAbsoluteFilePath(factory, group, id);
        GroupFileContainer fileManagment = this.GetGroupFromFilePath(path);
        return fileManagment.GetCurrentFileMappingName(fileName);
    }

    /// <summary>
    /// 删除已经存在的文件信息
    /// </summary>
    /// <param name="path">文件的路径</param>
    /// <param name="fileName">文件的名称</param>
    private void DeleteExsistingFile(string path, string fileName) {
        if (!string.IsNullOrEmpty(fileName)) {
            string fileUltimatePath = path + "\\" + fileName;
            FileMarkId fileMarkId = this.GetFileMarksFromDictionaryWithFileName(fileName);

            fileMarkId.AddOperation(() => {
                if (!this.DeleteFileByName(fileUltimatePath)) {
                    this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileDeleteFailed + fileUltimatePath);
                }
            });
        }
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
        if (!this.ReceiveInformationHead(
                socket,
                out int customer,
                out string fileName,
                out string Factory,
                out string Group,
                out string Identify).IsSuccess) {
            return;
        }

        string relativeName = this.ReturnRelativeFileName(Factory, Group, Identify, fileName);

        if (customer == HslProtocol.ProtocolFileDownload) {
            // 先获取文件的真实名称
            string guidName = this.TransformFactFileName(Factory, Group, Identify, fileName);
            // 获取文件操作锁
            FileMarkId fileMarkId = this.GetFileMarksFromDictionaryWithFileName(guidName);
            fileMarkId.EnterReadOperator();
            // 发送文件数据
            OperateResult send = this.SendFileAndCheckReceive(socket, this.ReturnAbsoluteFileName(Factory, Group, Identify, guidName), fileName, "", "", null);
            if (!send.IsSuccess) {
                fileMarkId.LeaveReadOperator();
                this.LogNet?.WriteError(this.ToString(), $"{StringResources.Language.FileDownloadFailed} : {send.Message} :{relativeName} ip:{IpAddress}");
                return;
            }
            else {
                this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileDownloadSuccess + ":" + relativeName);
            }

            fileMarkId.LeaveReadOperator();
            // 关闭连接
            socket?.Close();
        }
        else if (customer == HslProtocol.ProtocolFileUpload) {
            string fullFileName = this.ReturnAbsoluteFileName(Factory, Group, Identify, fileName);
            // 上传文件
            this.CheckFolderAndCreate();
            FileInfo info = new FileInfo(fullFileName);

            try {
                if (!Directory.Exists(info.DirectoryName)) {
                    Directory.CreateDirectory(info.DirectoryName);
                }
            }
            catch (Exception ex) {
                this.LogNet?.WriteException(this.ToString(), StringResources.Language.FilePathCreateFailed + fullFileName, ex);
                socket?.Close();
                return;
            }

            // 接收文件并回发消息
            if (this.ReceiveFileFromSocketAndUpdateGroup(
                    socket, // 网络套接字
                    fullFileName).IsSuccess) {
                socket?.Close();
                this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileUploadSuccess + ":" + relativeName);
            }
            else {
                this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileUploadFailed + ":" + relativeName);
            }
        }
        else if (customer == HslProtocol.ProtocolFileDelete) {
            string fullFileName = this.ReturnAbsoluteFileName(Factory, Group, Identify, fileName);

            FileInfo info = new FileInfo(fullFileName);
            GroupFileContainer fileManagment = this.GetGroupFromFilePath(info.DirectoryName);

            // 新增删除的任务
            this.DeleteExsistingFile(info.DirectoryName, fileManagment.DeleteFile(info.Name));

            // 回发消息
            if (this.SendStringAndCheckReceive(
                    socket, // 网络套接字
                    1, // 没啥含义
                    "success" // 没啥含意
                ).IsSuccess) {
                socket?.Close();
            }

            this.LogNet?.WriteInfo(this.ToString(), StringResources.Language.FileDeleteSuccess + ":" + relativeName);
        }
        else if (customer == HslProtocol.ProtocolFileDirectoryFiles) {
            GroupFileContainer fileManagment = this.GetGroupFromFilePath(this.ReturnAbsoluteFilePath(Factory, Group, Identify));

            if (this.SendStringAndCheckReceive(
                    socket,
                    HslProtocol.ProtocolFileDirectoryFiles,
                    fileManagment.JsonArrayContent).IsSuccess) {
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
            // close not supported client
            socket?.Close();
        }
    }

    /// <summary>
    /// 获取本对象的字符串表示形式
    /// </summary>
    /// <returns>字符串对象</returns>
    public override string ToString() {
        return "UltimateFileServer";
    }
}