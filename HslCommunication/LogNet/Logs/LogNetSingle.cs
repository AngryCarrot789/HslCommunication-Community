﻿using System.Text;
using HslCommunication.LogNet.Core;

namespace HslCommunication.LogNet.Logs;

/// <summary>
/// 单日志文件对象
/// </summary>
/// <remarks>
/// 此日志实例化需要指定一个完整的文件路径，当需要记录日志的时候调用方法，会使得日志越来越大，对于写入的性能没有太大影响，但是会影响文件读取。
/// </remarks>
public class LogNetSingle : LogNetBase, ILogNet {
    private string m_fileName = string.Empty;

    /// <summary>
    /// 实例化一个单文件日志的对象
    /// </summary>
    /// <param name="filePath">文件的路径</param>
    /// <exception cref="FileNotFoundException"></exception>
    public LogNetSingle(string filePath) {
        this.LogSaveMode = LogNetManagment.LogSaveModeBySingleFile;

        this.m_fileName = filePath;

        FileInfo fileInfo = new FileInfo(filePath);
        if (!Directory.Exists(fileInfo.DirectoryName)) {
            Directory.CreateDirectory(fileInfo.DirectoryName);
        }
    }

    /// <summary>
    /// 单日志文件允许清空日志内容
    /// </summary>
    public void ClearLog() {
        this.m_fileSaveLock.Enter();
        if (!string.IsNullOrEmpty(this.m_fileName)) {
            File.Create(this.m_fileName).Dispose();
        }

        this.m_fileSaveLock.Leave();
    }

    /// <summary>
    /// 获取单日志文件的所有保存记录
    /// </summary>
    /// <returns>字符串信息</returns>
    public string GetAllSavedLog() {
        string result = string.Empty;
        this.m_fileSaveLock.Enter();
        if (!string.IsNullOrEmpty(this.m_fileName)) {
            if (File.Exists(this.m_fileName)) {
                StreamReader stream = new StreamReader(this.m_fileName, Encoding.UTF8);
                result = stream.ReadToEnd();
                stream.Dispose();
            }
        }

        this.m_fileSaveLock.Leave();
        return result;
    }

    /// <summary>
    /// 获取所有的日志文件数组，对于单日志文件来说就只有一个
    /// </summary>
    /// <returns>字符串数组，包含了所有的存在的日志数据</returns>
    public string[] GetExistLogFileNames() {
        return new string[] {
            this.m_fileName,
        };
    }

    /// <summary>
    /// 获取存储的文件的名称
    /// </summary>
    /// <returns>字符串数据</returns>
    protected override string GetFileSaveName() {
        return this.m_fileName;
    }

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return $"LogNetSingle";
    }
}