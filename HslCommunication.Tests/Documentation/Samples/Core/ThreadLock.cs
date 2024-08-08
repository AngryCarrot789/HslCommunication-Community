using HslCommunication.Core;

namespace HslCommunication.Tests.Documentation.Samples.Core;

public class ThreadLockExample {
    #region SimpleHybirdLockExample1

    private SimpleHybirdLock simpleHybird = new SimpleHybirdLock();

    public void SimpleHybirdLockExample() {
        // 同步锁，简单的使用
        this.simpleHybird.Enter();

        // do something


        this.simpleHybird.Leave();
    }

    public void SimpleHybirdLockExample2() {
        // 高级应用，锁的中间是不允许有异常发生的，假如方法会发生异常

        this.simpleHybird.Enter();
        try {
            int i = 0;
            int j = 6 / i;
            this.simpleHybird.Leave();
        }
        catch {
            this.simpleHybird.Leave();
            throw;
        }

        // 这样做的好处是既没有吞噬异常，锁又安全的离开了
    }

    #endregion
}