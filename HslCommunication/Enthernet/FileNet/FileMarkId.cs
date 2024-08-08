using HslCommunication.Core;
using HslCommunication.LogNet;

namespace HslCommunication.Enthernet;

/// <summary>
/// 文件标记对象类
/// </summary>
internal class FileMarkId {
    /// <summary>
    /// 实例化一个文件标记对象
    /// </summary>
    /// <param name="logNet">日志对象</param>
    /// <param name="fileName">完整的文件名称</param>
    public FileMarkId(ILogNet logNet, string fileName) {
        this.LogNet = logNet;
        this.FileName = fileName;
    }

    private ILogNet LogNet; // 日志
    private string FileName = null; // 文件名称
    private Queue<Action> queues = new Queue<Action>(); // 操作的队列
    private SimpleHybirdLock hybirdLock = new SimpleHybirdLock(); // 状态的锁


    /// <summary>
    /// 新增一个文件的操作，仅仅是删除文件
    /// </summary>
    /// <param name="action">对当前文件的操作内容</param>
    public void AddOperation(Action action) {
        this.hybirdLock.Enter();

        if (this.readStatus == 0) {
            // 没有读取状态，立马执行
            action?.Invoke();
        }
        else {
            // 添加标记
            this.queues.Enqueue(action);
        }

        this.hybirdLock.Leave();
    }


    private int readStatus = 0;

    /// <summary>
    /// 指示该对象是否能被清除
    /// </summary>
    /// <returns>是否能够删除</returns>
    public bool CanClear() {
        bool result = false;
        this.hybirdLock.Enter();
        result = this.readStatus == 0 && this.queues.Count == 0;
        this.hybirdLock.Leave();
        return result;
    }

    /// <summary>
    /// 进入文件的读取状态
    /// </summary>
    public void EnterReadOperator() {
        this.hybirdLock.Enter();
        this.readStatus++;
        this.hybirdLock.Leave();
    }

    /// <summary>
    /// 离开本次的文件读取状态
    /// </summary>
    public void LeaveReadOperator() {
        // 检查文件标记状态
        this.hybirdLock.Enter();
        this.readStatus--;
        if (this.readStatus == 0) {
            while (this.queues.Count > 0) {
                try {
                    this.queues.Dequeue()?.Invoke();
                }
                catch (Exception ex) {
                    this.LogNet?.WriteException("FileMarkId", "File Action Failed:", ex);
                }
            }
        }

        this.hybirdLock.Leave();
    }
}