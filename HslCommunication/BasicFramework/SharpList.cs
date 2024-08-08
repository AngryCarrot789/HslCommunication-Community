using HslCommunication.Core;

namespace HslCommunication.BasicFramework;

/// <summary>
/// 一个高效的数组管理类，用于高效控制固定长度的数组实现
/// </summary>
/// <typeparam name="T">泛型类型</typeparam>
public class SharpList<T> {
    #region Constructor

    /// <summary>
    /// 实例化一个对象，需要指定数组的最大数据对象
    /// </summary>
    /// <param name="count">数据的个数</param>
    /// <param name="appendLast">是否从最后一个数添加</param>
    public SharpList(int count, bool appendLast = false) {
        if (count > 8192)
            this.capacity = 4096;

        this.array = new T[this.capacity + count];
        this.hybirdLock = new SimpleHybirdLock();
        this.count = count;
        if (appendLast)
            this.lastIndex = count;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// 获取数据的个数
    /// </summary>
    public int Count => this.count;

    #endregion

    #region Public Method

    /// <summary>
    /// 新增一个数据值
    /// </summary>
    /// <param name="value">数据值</param>
    public void Add(T value) {
        this.hybirdLock.Enter();

        if (this.lastIndex < (this.capacity + this.count)) {
            this.array[this.lastIndex++] = value;
        }
        else {
            // 需要重新挪位置了
            T[] buffer = new T[this.capacity + this.count];
            Array.Copy(this.array, this.capacity, buffer, 0, this.count);
            this.array = buffer;
            this.lastIndex = this.count;
        }

        this.hybirdLock.Leave();
    }

    /// <summary>
    /// 批量的增加数据
    /// </summary>
    /// <param name="values">批量数据信息</param>
    public void Add(IEnumerable<T> values) {
        foreach (T m in values) {
            this.Add(m);
        }
    }

    /// <summary>
    /// 获取数据的数组值
    /// </summary>
    /// <returns>数组值</returns>
    public T[] ToArray() {
        T[] result = null;
        this.hybirdLock.Enter();

        if (this.lastIndex < this.count) {
            result = new T[this.lastIndex];
            Array.Copy(this.array, 0, result, 0, this.lastIndex);
        }
        else {
            result = new T[this.count];
            Array.Copy(this.array, this.lastIndex - this.count, result, 0, this.count);
        }

        this.hybirdLock.Leave();
        return result;
    }

    /// <summary>
    /// 获取或设置指定索引的位置的数据
    /// </summary>
    /// <param name="index">索引位置</param>
    /// <returns>数据值</returns>
    public T this[int index] {
        get {
            if (index < 0)
                throw new IndexOutOfRangeException("Index must larger than zero");
            if (index >= this.count)
                throw new IndexOutOfRangeException("Index must smaller than array length");
            T tmp = default(T);
            this.hybirdLock.Enter();

            if (this.lastIndex < this.count) {
                tmp = this.array[index];
            }
            else {
                tmp = this.array[index + this.lastIndex - this.count];
            }

            this.hybirdLock.Leave();
            return tmp;
        }
        set {
            if (index < 0)
                throw new IndexOutOfRangeException("Index must larger than zero");
            if (index >= this.count)
                throw new IndexOutOfRangeException("Index must smaller than array length");
            this.hybirdLock.Enter();

            if (this.lastIndex < this.count) {
                this.array[index] = value;
            }
            else {
                this.array[index + this.lastIndex - this.count] = value;
            }

            this.hybirdLock.Leave();
        }
    }

    #endregion

    #region private Member

    private T[] array;
    private int capacity = 2048; // 整个数组的附加容量
    private int count = 0; // 数组的实际数据容量
    private int lastIndex = 0; // 最后一个数的索引位置
    private SimpleHybirdLock hybirdLock; // 数组的操作锁

    #endregion
}