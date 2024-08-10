using HslCommunication.Core.Thread;

namespace HslCommunication.BasicFramework;
/********************************************************************************************
 *
 *    一个高度灵活的流水号生成的类，允许根据指定规则加上时间数据进行生成
 *
 *    根据保存机制进行优化，必须做到好并发量
 *
 ********************************************************************************************/

/// <summary>
/// 一个简单的不持久化的序号自增类，采用线程安全实现，并允许指定最大数字，将包含该最大值，到达后清空从指定数开始
/// </summary>
public sealed class SoftIncrementCount : IDisposable {
    private readonly long start;
    private long current;
    private long max;
    private SimpleHybirdLock hybirdLock;
    
    /// <summary>
    /// 实例化一个自增信息的对象，包括最大值
    /// </summary>
    /// <param name="max">数据的最大值，必须指定</param>
    /// <param name="start">数据的起始值，默认为0</param>
    public SoftIncrementCount(long max, long start = 0) {
        this.start = start;
        this.max = max;
        this.current = start;
        this.hybirdLock = new SimpleHybirdLock();
    }

    public long GetValueAndIncrement() {
        this.hybirdLock.Enter();

        long value = this.current;
        this.current += this.IncreaseTick;
        if (this.current > this.max) {
            this.current = this.start;
        }

        this.hybirdLock.Leave();
        return value;
    }

    /// <summary>
    /// 重置当前序号的最大值
    /// </summary>
    /// <param name="max">最大值</param>
    public void ResetMaxNumber(long max) {
        this.hybirdLock.Enter();

        if (max > this.start) {
            if (max < this.current)
                this.current = this.start;
            this.max = max;
        }

        this.hybirdLock.Leave();
    }

    /// <summary>
    /// 增加的单元，如果设置为0，就是不增加。注意，不能小于0
    /// </summary>
    public int IncreaseTick { get; set; } = 1;

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>返回具体的值信息</returns>
    public override string ToString() {
        return $"SoftIncrementCount[{this.current}]";
    }

    private bool disposedValue = false; // 要检测冗余调用

    void Dispose(bool disposing) {
        if (!this.disposedValue) {
            if (disposing) {
                // TODO: 释放托管状态(托管对象)。

                this.hybirdLock.Dispose();
            }

            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
            // TODO: 将大型字段设置为 null。


            this.disposedValue = true;
        }
    }

    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    // ~SoftIncrementCount() {
    //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //   Dispose(false);
    // }

    // 添加此代码以正确实现可处置模式。
    /// <summary>
    /// 释放当前对象所占用的资源
    /// </summary>
    public void Dispose() {
        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        this.Dispose(true);
        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
        // GC.SuppressFinalize(this);
    }
}