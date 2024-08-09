using HslCommunication.Core.Net.StateOne;
using HslCommunication.Core.Thread;

namespace HslCommunication.Enthernet.PushNet;

/// <summary>
/// 订阅分类的核心组织对象
/// </summary>
public class PushGroupClient : IDisposable {
    /// <summary>
    /// 实例化一个默认的对象
    /// </summary>
    public PushGroupClient() {
        this.appSessions = new List<AppSession>();
        this.simpleHybird = new SimpleHybirdLock();
    }

    /// <summary>
    /// 新增一个订阅的会话
    /// </summary>
    /// <param name="session">会话</param>
    public void AddPushClient(AppSession session) {
        this.simpleHybird.Enter();
        this.appSessions.Add(session);
        this.simpleHybird.Leave();
    }

    /// <summary>
    /// 移除一个订阅的会话
    /// </summary>
    /// <param name="clientID">客户端唯一的ID信息</param>
    public bool RemovePushClient(string clientID) {
        bool result = false;
        this.simpleHybird.Enter();
        for (int i = 0; i < this.appSessions.Count; i++) {
            if (this.appSessions[i].ClientUniqueID == clientID) {
                this.appSessions[i].WorkSocket?.Close();
                this.appSessions.RemoveAt(i);
                result = true;
                break;
            }
        }

        this.simpleHybird.Leave();

        return result;
    }

    /// <summary>
    /// 使用固定的发送方法将数据发送出去
    /// </summary>
    /// <param name="content">数据内容</param>
    /// <param name="send">指定的推送方法</param>
    public void PushString(string content, Action<AppSession, string> send) {
        this.simpleHybird.Enter();

        Interlocked.Increment(ref this.pushTimesCount);
        for (int i = 0; i < this.appSessions.Count; i++) {
            send(this.appSessions[i], content);
        }

        this.simpleHybird.Leave();
    }

    /// <summary>
    /// 移除并关闭所有的客户端
    /// </summary>
    public int RemoveAllClient() {
        int result = 0;
        this.simpleHybird.Enter();

        for (int i = 0; i < this.appSessions.Count; i++) {
            this.appSessions[i].WorkSocket?.Close();
        }

        result = this.appSessions.Count;

        this.appSessions.Clear();
        this.simpleHybird.Leave();

        return result;
    }

    /// <summary>
    /// 获取是否推送过数据
    /// </summary>
    /// <returns>True代表有，False代表没有</returns>
    public bool HasPushedContent() {
        return this.pushTimesCount > 0L;
    }

    private List<AppSession> appSessions; // 所有的客户端信息
    private SimpleHybirdLock simpleHybird; // 列表的锁
    private long pushTimesCount = 0L; // 推送的次数总和

    private bool disposedValue = false; // 要检测冗余调用

    /// <summary>
    /// 释放当前的程序所占用的资源
    /// </summary>
    /// <param name="disposing">是否释放资源</param>
    protected virtual void Dispose(bool disposing) {
        if (!this.disposedValue) {
            if (disposing) {
                // TODO: 释放托管状态(托管对象)。
            }

            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
            // TODO: 将大型字段设置为 null。


            this.simpleHybird.Enter();
            this.appSessions.ForEach(m => m.WorkSocket?.Close());
            this.appSessions.Clear();
            this.simpleHybird.Leave();

            this.simpleHybird.Dispose();

            this.disposedValue = true;
        }
    }

    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    // ~PushGroupClient() {
    //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //   Dispose(false);
    // }


    // 添加此代码以正确实现可处置模式。

    /// <summary>
    /// 释放当前的对象所占用的资源
    /// </summary>
    public void Dispose() {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        this.Dispose(true);
        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
        // GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 获取本对象的字符串表示形式
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        return "PushGroupClient";
    }
}