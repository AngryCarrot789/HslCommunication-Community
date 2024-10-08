﻿using System.Net.Sockets;
using HslCommunication.Core.Thread;
using HslCommunication.Core.Types;
using HslCommunication.Enthernet.FileNet;

namespace HslCommunication.Core.Net.NetworkBase;

/// <summary>
/// 文件服务器类的基类，为直接映射文件模式和间接映射文件模式提供基础的方法支持
/// </summary>
public class NetworkFileServerBase : NetworkServerBase {
    /// <summary>
    /// 所有文件操作的词典锁
    /// </summary>
    internal Dictionary<string, FileMarkId> m_dictionary_files_marks = new Dictionary<string, FileMarkId>();

    /// <summary>
    /// 词典的锁
    /// </summary>
    private SimpleHybirdLock dict_hybirdLock = new SimpleHybirdLock();

    /// <summary>
    /// 获取当前文件的读写锁，如果没有会自动创建
    /// </summary>
    /// <param name="filename">完整的文件路径</param>
    /// <returns>读写锁</returns>
    internal FileMarkId GetFileMarksFromDictionaryWithFileName(string filename) {
        FileMarkId fileMarkId = null;
        this.dict_hybirdLock.Enter();

        // lock operator
        if (this.m_dictionary_files_marks.ContainsKey(filename)) {
            fileMarkId = this.m_dictionary_files_marks[filename];
        }
        else {
            fileMarkId = new FileMarkId(this.LogNet, filename);
            this.m_dictionary_files_marks.Add(filename, fileMarkId);
        }

        this.dict_hybirdLock.Leave();
        return fileMarkId;
    }

    /// <summary>
    /// 接收本次操作的信息头数据
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="command">命令</param>
    /// <param name="fileName">文件名</param>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <returns>是否成功的结果对象</returns>
    protected OperateResult ReceiveInformationHead(
        Socket socket,
        out int command,
        out string fileName,
        out string factory,
        out string group,
        out string id) {
        // 先接收文件名
        OperateResult<int, string> fileNameResult = this.ReceiveStringContentFromSocket(socket);
        if (!fileNameResult.IsSuccess) {
            command = 0;
            fileName = null;
            factory = null;
            group = null;
            id = null;
            return fileNameResult;
        }

        command = fileNameResult.Content1;
        fileName = fileNameResult.Content2;

        // 接收Factory
        OperateResult<int, string> factoryResult = this.ReceiveStringContentFromSocket(socket);
        if (!factoryResult.IsSuccess) {
            factory = null;
            group = null;
            id = null;
            return factoryResult;
        }

        factory = factoryResult.Content2;


        // 接收Group
        OperateResult<int, string> groupResult = this.ReceiveStringContentFromSocket(socket);
        if (!groupResult.IsSuccess) {
            group = null;
            id = null;
            return groupResult;
        }

        group = groupResult.Content2;

        // 最后接收id
        OperateResult<int, string> idResult = this.ReceiveStringContentFromSocket(socket);
        if (!idResult.IsSuccess) {
            id = null;
            return idResult;
        }

        id = idResult.Content2;

        return OperateResult.CreateSuccessResult();
    }

    /// <summary>
    /// 获取一个随机的文件名，由GUID码和随机数字组成
    /// </summary>
    /// <returns>文件名</returns>
    protected string CreateRandomFileName() {
        return BasicFramework.SoftBasic.GetUniqueStringByGuidAndRandom();
    }

    /// <summary>
    /// 返回服务器的绝对路径
    /// </summary>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <returns>是否成功的结果对象</returns>
    protected string ReturnAbsoluteFilePath(string factory, string group, string id) {
        string result = this.m_FilesDirectoryPath;
        if (!string.IsNullOrEmpty(factory))
            result += "\\" + factory;
        if (!string.IsNullOrEmpty(group))
            result += "\\" + group;
        if (!string.IsNullOrEmpty(id))
            result += "\\" + id;
        return result;
    }


    /// <summary>
    /// 返回服务器的绝对路径
    /// </summary>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <param name="fileName">文件名</param>
    /// <returns>是否成功的结果对象</returns>
    protected string ReturnAbsoluteFileName(string factory, string group, string id, string fileName) {
        return this.ReturnAbsoluteFilePath(factory, group, id) + "\\" + fileName;
    }


    /// <summary>
    /// 返回相对路径的名称
    /// </summary>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <param name="fileName">文件名</param>
    /// <returns>是否成功的结果对象</returns>
    protected string ReturnRelativeFileName(string factory, string group, string id, string fileName) {
        string result = "";
        if (!string.IsNullOrEmpty(factory))
            result += factory + "\\";
        if (!string.IsNullOrEmpty(group))
            result += group + "\\";
        if (!string.IsNullOrEmpty(id))
            result += id + "\\";
        return result + fileName;
    }

    //private Timer timer;

    //private void ClearDict(object obj)
    //{
    //    hybirdLock.Enter();

    //    List<string> waitRemove = new List<string>();
    //    foreach(var m in m_dictionary_files_marks)
    //    {
    //        if(m.Value.CanClear())
    //        {
    //            waitRemove.Add(m.Key);
    //        }
    //    }

    //    foreach(var m in waitRemove)
    //    {
    //        m_dictionary_files_marks.Remove(m);
    //    }

    //    waitRemove.Clear();
    //    waitRemove = null;

    //    hybirdLock.Leave();
    //}

    /// <summary>
    /// 移动一个文件到新的文件去
    /// </summary>
    /// <param name="fileNameOld">旧的文件名称</param>
    /// <param name="fileNameNew">新的文件名称</param>
    /// <returns>是否成功</returns>
    protected bool MoveFileToNewFile(string fileNameOld, string fileNameNew) {
        try {
            FileInfo info = new FileInfo(fileNameNew);
            if (!Directory.Exists(info.DirectoryName)) {
                Directory.CreateDirectory(info.DirectoryName);
            }

            if (File.Exists(fileNameNew)) {
                File.Delete(fileNameNew);
            }

            File.Move(fileNameOld, fileNameNew);
            return true;
        }
        catch (Exception ex) {
            this.LogNet?.WriteException(this.ToString(), "Move a file to new file failed: ", ex);
            return false;
        }
    }


    /// <summary>
    /// 删除文件并回发确认信息，如果结果异常，则结束通讯
    /// </summary>
    /// <param name="socket">网络套接字</param>
    /// <param name="fullname">完整路径的文件名称</param>
    /// <returns>是否成功的结果对象</returns>
    protected OperateResult DeleteFileAndCheck(
        Socket socket,
        string fullname) {
        // 删除文件，如果失败，重复三次
        int customer = 0;
        int times = 0;
        while (times < 3) {
            times++;
            if (this.DeleteFileByName(fullname)) {
                customer = 1;
                break;
            }
            else {
                System.Threading.Thread.Sleep(500);
            }
        }

        // 回发消息
        return this.SendStringAndCheckReceive(socket, customer, StringResources.Language.SuccessText);
    }

    // 文件的上传事件

    /// <summary>
    /// 服务器启动时的操作
    /// </summary>
    protected override void StartInitialization() {
        if (string.IsNullOrEmpty(this.FilesDirectoryPath)) {
            throw new ArgumentNullException("FilesDirectoryPath", "No saved path is specified");
        }

        this.CheckFolderAndCreate();
        base.StartInitialization();
    }

    /// <summary>
    /// 检查文件夹是否存在，不存在就创建
    /// </summary>
    protected virtual void CheckFolderAndCreate() {
        if (!Directory.Exists(this.FilesDirectoryPath)) {
            Directory.CreateDirectory(this.FilesDirectoryPath);
        }
    }

    /// <summary>
    /// 文件所存储的路径
    /// </summary>
    public string FilesDirectoryPath {
        get { return this.m_FilesDirectoryPath; }
        set { this.m_FilesDirectoryPath = this.PreprocessFolderName(value); }
    }


    /// <summary>
    /// 获取文件夹的所有文件列表
    /// </summary>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <returns>文件列表</returns>
    public virtual string[] GetDirectoryFiles(string factory, string group, string id) {
        if (string.IsNullOrEmpty(this.FilesDirectoryPath))
            return Array.Empty<string>();

        string absolutePath = this.ReturnAbsoluteFilePath(factory, group, id);

        // 如果文件夹不存在
        if (!Directory.Exists(absolutePath))
            return Array.Empty<string>();
        // 返回文件列表
        return Directory.GetFiles(absolutePath);
    }

    /// <summary>
    /// 获取文件夹的所有文件夹列表
    /// </summary>
    /// <param name="factory">第一大类</param>
    /// <param name="group">第二大类</param>
    /// <param name="id">第三大类</param>
    /// <returns>文件夹列表</returns>
    public string[] GetDirectories(string factory, string group, string id) {
        if (string.IsNullOrEmpty(this.FilesDirectoryPath))
            return Array.Empty<string>();

        string absolutePath = this.ReturnAbsoluteFilePath(factory, group, id);

        // 如果文件夹不存在
        if (!Directory.Exists(absolutePath))
            return Array.Empty<string>();
        // 返回文件列表
        return Directory.GetDirectories(absolutePath);
    }

    private string m_FilesDirectoryPath = null; // 文件的存储路径
    private Random m_random = new Random(); // 随机生成的文件名

    /// <summary>
    /// 获取本对象的字符串标识形式
    /// </summary>
    /// <returns>对象信息</returns>
    public override string ToString() {
        return "NetworkFileServerBase";
    }
}