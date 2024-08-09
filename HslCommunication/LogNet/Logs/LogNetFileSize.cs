using HslCommunication.LogNet.Core;

namespace HslCommunication.LogNet.Logs;

/// <summary>
/// 根据文件的大小来存储日志信息
/// </summary>
/// <remarks>
/// 此日志的实例是根据文件的大小储存，例如设置了2M，每隔2M，系统将生成一个新的日志文件。
/// </remarks>
public class LogNetFileSize : LogNetBase, ILogNet {
    /// <summary>
    /// 实例化一个根据文件大小生成新文件的
    /// </summary>
    /// <param name="filePath">日志文件的保存路径</param>
    /// <param name="fileMaxSize">每个日志文件的最大大小，默认2M</param>
    public LogNetFileSize(string filePath, int fileMaxSize = 2 * 1024 * 1024) {
        this.m_filePath = filePath;
        this.m_fileMaxSize = fileMaxSize;
        this.LogSaveMode = LogNetManagment.LogSaveModeByFileSize;
        this.m_filePath = this.CheckPathEndWithSprit(this.m_filePath);
    }

    /// <summary>
    /// 获取需要保存的日志文件
    /// </summary>
    /// <returns>字符串数据</returns>
    protected override string GetFileSaveName() {
        //路径没有设置则返回空
        if (string.IsNullOrEmpty(this.m_filePath))
            return string.Empty;

        if (string.IsNullOrEmpty(this.m_fileName)) {
            //加载文件名称
            this.m_fileName = this.GetLastAccessFileName();
        }

        if (File.Exists(this.m_fileName)) {
            FileInfo fileInfo = new FileInfo(this.m_fileName);

            if (fileInfo.Length > this.m_fileMaxSize) {
                //新生成文件
                this.m_fileName = this.GetDefaultFileName();
            }
        }

        return this.m_fileName;
    }

    /// <summary>
    /// 返回所有的日志文件
    /// </summary>
    /// <returns>所有的日志文件信息</returns>
    public string[] GetExistLogFileNames() {
        if (!string.IsNullOrEmpty(this.m_filePath)) {
            return Directory.GetFiles(this.m_filePath, LogNetManagment.LogFileHeadString + "*.txt");
        }
        else {
            return new string[] { };
        }
    }

    /// <summary>
    /// 获取之前保存的日志文件
    /// </summary>
    /// <returns></returns>
    private string GetLastAccessFileName() {
        foreach (string m in this.GetExistLogFileNames()) {
            FileInfo fileInfo = new FileInfo(m);
            if (fileInfo.Length < this.m_fileMaxSize) {
                this.m_CurrentFileSize = (int) fileInfo.Length;
                return m;
            }
        }

        //返回一个新的默认当前时间的日志名称
        return this.GetDefaultFileName();
    }

    /// <summary>
    /// 获取一个新的默认的文件名称
    /// </summary>
    /// <returns></returns>
    private string GetDefaultFileName() {
        //返回一个新的默认当前时间的日志名称
        return this.m_filePath + LogNetManagment.LogFileHeadString + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
    }

    private string m_fileName = string.Empty; // 当前正在存储的文件名
    private string m_filePath = string.Empty; // 存储文件的路径
    private int m_fileMaxSize = 2 * 1024 * 1024; //2M
    private int m_CurrentFileSize = 0;

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串数据</returns>
    public override string ToString() {
        return $"LogNetFileSize[{this.m_fileMaxSize}]";
    }
}