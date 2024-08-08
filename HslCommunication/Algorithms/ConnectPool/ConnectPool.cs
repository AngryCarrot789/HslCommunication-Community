using HslCommunication.ModBus;

namespace HslCommunication.Algorithms.ConnectPool;

/// <summary>
/// 一个连接池管理器，负责维护多个可用的连接，并且自动清理，扩容
/// </summary>
/// <typeparam name="TConnector">管理的连接类，需要支持IConnector接口</typeparam>
/// <remarks>
/// 需要先实现 <see cref="IConnector"/> 接口的对象，然后就可以实现真正的连接池了，理论上可以实现任意的连接对象，包括modbus连接对象，各种PLC连接对象，数据库连接对象，redis连接对象，SimplifyNet连接对象等等。下面的示例就是modbus-tcp的实现
/// <note type="warning">要想真正的支持连接池访问，还需要服务器支持一个端口的多连接操作，三菱PLC的端口就不支持，如果要测试示例代码的连接池对象，需要使用本组件的<see cref="ModbusTcpServer"/>来创建服务器对象</note>
/// </remarks>
/// <example>
/// 下面举例实现一个modbus的连接池对象，先实现接口化的操作
/// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Algorithms\ConnectPool.cs" region="IConnector Example" title="IConnector示例" />
/// 然后就可以实现真正的连接池了
/// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\Algorithms\ConnectPool.cs" region="ConnectPoolExample" title="ConnectPool示例" />
/// </example>
public class ConnectPool<TConnector> where TConnector : IConnector {
    #region Constructor

    /// <summary>
    /// 实例化一个连接池对象，需要指定如果创建新实例的方法
    /// </summary>
    /// <param name="createConnector">创建连接对象的委托</param>
    public ConnectPool(Func<TConnector> createConnector) {
        this.CreateConnector = createConnector;
        this.hybirdLock = new Core.SimpleHybirdLock();
        this.connectors = new List<TConnector>();

        this.timerCheck = new System.Threading.Timer(this.TimerCheckBackground, null, 10000, 30000);
    }

    #endregion

    #region Public Method

    /// <summary>
    /// 获取可用的对象
    /// </summary>
    /// <returns>可用的连接对象</returns>
    public TConnector GetAvailableConnector() {
        while (!this.canGetConnector) {
            System.Threading.Thread.Sleep(100);
        }

        TConnector result = default(TConnector);
        this.hybirdLock.Enter();

        for (int i = 0; i < this.connectors.Count; i++) {
            if (!this.connectors[i].IsConnectUsing) {
                this.connectors[i].IsConnectUsing = true;
                result = this.connectors[i];
                break;
            }
        }

        if (result == null) {
            // 创建新的连接
            result = this.CreateConnector();
            result.IsConnectUsing = true;
            result.LastUseTime = DateTime.Now;
            result.Open();
            this.connectors.Add(result);
            this.usedConnector = this.connectors.Count;

            if (this.usedConnector == this.maxConnector)
                this.canGetConnector = false;
        }


        result.LastUseTime = DateTime.Now;

        this.hybirdLock.Leave();

        return result;
    }

    /// <summary>
    /// 使用完之后需要通知管理器
    /// </summary>
    /// <param name="connector">连接对象</param>
    public void ReturnConnector(TConnector connector) {
        this.hybirdLock.Enter();

        int index = this.connectors.IndexOf(connector);
        if (index != -1) {
            this.connectors[index].IsConnectUsing = false;
        }

        this.hybirdLock.Leave();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// 获取或设置最大的连接数
    /// </summary>
    public int MaxConnector {
        get { return this.maxConnector; }
        set { this.maxConnector = value; }
    }


    /// <summary>
    /// 获取或设置连接过期的时间，单位秒，默认30秒
    /// </summary>
    public int ConectionExpireTime {
        get { return this.expireTime; }
        set { this.expireTime = value; }
    }


    /// <summary>
    /// 当前已经使用的连接数
    /// </summary>
    public int UsedConnector {
        get { return this.usedConnector; }
    }

    #endregion

    #region Clear Timer

    private void TimerCheckBackground(object obj) {
        // 清理长久不用的连接对象
        this.hybirdLock.Enter();

        for (int i = this.connectors.Count - 1; i >= 0; i--) {
            if ((DateTime.Now - this.connectors[i].LastUseTime).TotalSeconds > this.expireTime && !this.connectors[i].IsConnectUsing) {
                // 10分钟未使用了，就要删除掉
                this.connectors[i].Close();
                this.connectors.RemoveAt(i);
            }
        }

        this.usedConnector = this.connectors.Count;
        if (this.usedConnector < this.MaxConnector)
            this.canGetConnector = true;

        this.hybirdLock.Leave();
    }

    #endregion

    #region Private Member

    private Func<TConnector> CreateConnector = null; // 创建新的连接对象的委托
    private int maxConnector = 10; // 最大的连接数
    private int usedConnector = 0; // 已经使用的连接
    private int expireTime = 30; // 连接的过期时间，单位秒
    private bool canGetConnector = true; // 是否可以获取连接
    private System.Threading.Timer timerCheck = null; // 对象列表检查的时间间隔
    private Core.SimpleHybirdLock hybirdLock = null; // 列表操作的锁
    private List<TConnector> connectors = null; // 所有连接的列表

    #endregion
}