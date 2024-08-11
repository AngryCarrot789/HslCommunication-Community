namespace HslCommunication.Core.Types;

/*******************************************************************************
 *
 *    用户返回多个结果数据的一个类，允许返回操作结果，文本信息，错误代号，等等
 *
 *    Used to the return result class in the synchronize communication and communication for industrial Ethernet
 *
 *    时间：2017年11月20日 11:43:57
 *    更新：废除原先的2个结果派生类，新增10个泛型派生类，来满足绝大多数的场景使用
 *
 *    时间：2018年3月11日 22:08:08
 *    更新：新增一些静态方法来方便的获取带有参数的成功对象，新增快速复制错误信息的方法
 *
 *    时间：2018年8月23日 12:19:36
 *    更新：新增两个不同的结果对象构造方法
 *
 *    AngryCarrot789 2024 16:16
 *    Created LightOperationResult + generic types, to reduce
 *    class allocations to reduce GC activity
 *
 *******************************************************************************/

public readonly struct LightOperationResult {
    public int ErrorCode { get; }
    public string Message { get; }
    public bool IsSuccess { get; }

    public LightOperationResult() : this(true) {
    }

    public LightOperationResult(string? msg = null) : this(10000, msg) { }

    public LightOperationResult(int err, string? msg = null) {
        this.IsSuccess = false;
        this.Message = msg ?? StringResources.Language.UnknownError;
        this.ErrorCode = err;
    }

    private LightOperationResult(bool isSuccess) {
        this.IsSuccess = isSuccess;
        this.ErrorCode = 0;
        this.Message = StringResources.Language.SuccessText;
    }

    public OperateResult ToOperateResult() => this.IsSuccess ? OperateResult.CreateSuccessResult() : new OperateResult(this.ErrorCode, this.Message);

    public static LightOperationResult CreateSuccessResult() => new LightOperationResult(true);
    public static LightOperationResult<T> CreateSuccessResult<T>(T content) => new LightOperationResult<T>(content);
    public static LightOperationResult<T1, T2> CreateSuccessResult<T1, T2>(T1 t1, T2 t2) => new LightOperationResult<T1, T2>(t1, t2);
    public static LightOperationResult<T1, T2, T3> CreateSuccessResult<T1, T2, T3>(T1 t1, T2 t2, T3 t3) => new LightOperationResult<T1, T2, T3>(t1, t2, t3);

    public override string ToString() => ToString(this.Message, this.ErrorCode);

    public static string ToString(string msg, int errorCode) => $"{StringResources.Language.ErrorCode}{errorCode}{Environment.NewLine}{StringResources.Language.TextDescription}: {msg}";
}

public readonly struct LightOperationResult<T> {
    public bool IsSuccess { get; }
    public string Message { get; }
    public int ErrorCode { get; }

    public T Content { get; }

    public LightOperationResult(string? msg = null) : this(10000, msg) { }

    public LightOperationResult(int err, string? msg = null) {
        this.IsSuccess = false;
        this.Message = msg ?? StringResources.Language.UnknownError;
        this.ErrorCode = err;
    }

    public LightOperationResult(T value) {
        this.IsSuccess = true;
        this.Content = value;
    }

    public override string ToString() => LightOperationResult.ToString(this.Message, this.ErrorCode);

    public OperateResult<T> ToOperateResult() => this.IsSuccess ? OperateResult.CreateSuccessResult(this.Content) : new OperateResult<T>(this.ErrorCode, this.Message);
}

public readonly struct LightOperationResult<T1, T2> {
    public bool IsSuccess { get; }
    public string Message { get; }
    public int ErrorCode { get; }

    public T1 Content1 { get; }
    public T2 Content2 { get; }
    
    public LightOperationResult(string? msg = null) : this(10000, msg) { }

    public LightOperationResult(int err, string? msg = null) {
        this.IsSuccess = false;
        this.Message = msg ?? StringResources.Language.UnknownError;
        this.ErrorCode = err;
    }

    public LightOperationResult(T1 t1, T2 t2) {
        this.IsSuccess = true;
        this.Content1 = t1;
        this.Content2 = t2;
    }

    public override string ToString() => LightOperationResult.ToString(this.Message, this.ErrorCode);

    public OperateResult<T1, T2> ToOperateResult() => this.IsSuccess ? OperateResult.CreateSuccessResult(this.Content1, this.Content2) : new OperateResult<T1, T2>(this.ErrorCode, this.Message);
}

public readonly struct LightOperationResult<T1, T2, T3> {
    public bool IsSuccess { get; }
    public string Message { get; }
    public int ErrorCode { get; }

    public T1 Content1 { get; }
    public T2 Content2 { get; }
    public T3 Content3 { get; }
    
    public LightOperationResult(string? msg = null) : this(10000, msg) { }

    public LightOperationResult(int err, string? msg = null) {
        this.IsSuccess = false;
        this.Message = msg ?? StringResources.Language.UnknownError;
        this.ErrorCode = err;
    }

    public LightOperationResult(T1 t1, T2 t2, T3 t3) {
        this.IsSuccess = true;
        this.Content1 = t1;
        this.Content2 = t2;
        this.Content3 = t3;
    }

    public override string ToString() => LightOperationResult.ToString(this.Message, this.ErrorCode);

    public OperateResult<T1, T2, T3> ToOperateResult() => this.IsSuccess ? OperateResult.CreateSuccessResult(this.Content1, this.Content2, this.Content3) : new OperateResult<T1, T2, T3>(this.ErrorCode, this.Message);
}

public readonly struct LightOperationResult<T1, T2, T3, T4> {
    public bool IsSuccess { get; }
    public string Message { get; }
    public int ErrorCode { get; }

    public T1 Content1 { get; }
    public T2 Content2 { get; }
    public T3 Content3 { get; }
    public T4 Content4 { get; }

    public LightOperationResult(string? msg = null) : this(10000, msg) { }

    public LightOperationResult(int err, string? msg = null) {
        this.IsSuccess = false;
        this.Message = msg ?? StringResources.Language.UnknownError;
        this.ErrorCode = err;
    }

    public LightOperationResult(T1 t1, T2 t2, T3 t3, T4 t4) {
        this.IsSuccess = true;
        this.Content1 = t1;
        this.Content2 = t2;
        this.Content3 = t3;
        this.Content4 = t4;
    }

    public override string ToString() => LightOperationResult.ToString(this.Message, this.ErrorCode);

    public OperateResult<T1, T2, T3, T4> ToOperateResult() => this.IsSuccess ? OperateResult.CreateSuccessResult(this.Content1, this.Content2, this.Content3, this.Content4) : new OperateResult<T1, T2, T3, T4>(this.ErrorCode, this.Message);
}

/// <summary>
/// 操作结果的类，只带有成功标志和错误信息 -> The class that operates the result, with only success flags and error messages
/// </summary>
/// <remarks>
/// 当 <see cref="IsSuccess"/> 为 True 时，忽略 <see cref="Message"/> 及 <see cref="ErrorCode"/> 的值
/// </remarks>
public class OperateResult {
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = StringResources.Language.UnknownError;
    public int ErrorCode { get; set; } = 10000;
    public OperateResult() {
    }
    public OperateResult(string msg) {
        this.Message = msg;
    }
    public OperateResult(int err, string msg) {
        this.ErrorCode = err;
        this.Message = msg;
    }

    /// <summary>
    /// Displays this operation result in a readable way
    /// </summary>
    /// <returns>包含错误码及错误消息</returns>
    public string ToMessageShowString() {
        return $"{StringResources.Language.ErrorCode}:{this.ErrorCode}{Environment.NewLine}{StringResources.Language.TextDescription}:{this.Message}";
    }
    
    /// <summary>
    /// Copy the error information from another result instance
    /// </summary>
    public void CopyErrorFromOther(OperateResult? result) {
        if (result != null) {
            this.ErrorCode = result.ErrorCode;
            this.Message = result.Message;
        }
    }

    public static OperateResult<T> CreateFailedResult<T>(OperateResult result) => new OperateResult<T> { ErrorCode = result.ErrorCode, Message = result.Message, };
    public static OperateResult<T1, T2> CreateFailedResult<T1, T2>(OperateResult result) => new OperateResult<T1, T2> { ErrorCode = result.ErrorCode, Message = result.Message, };
    public static OperateResult<T1, T2, T3> CreateFailedResult<T1, T2, T3>(OperateResult result) => new OperateResult<T1, T2, T3> { ErrorCode = result.ErrorCode, Message = result.Message, };
    public static OperateResult<T1, T2, T3, T4> CreateFailedResult<T1, T2, T3, T4>(OperateResult result) => new OperateResult<T1, T2, T3, T4> { ErrorCode = result.ErrorCode, Message = result.Message, };

    public static OperateResult CreateSuccessResult() => new OperateResult { IsSuccess = true, ErrorCode = 0, Message = StringResources.Language.SuccessText, };
    public static OperateResult<T> CreateSuccessResult<T>(T value) => new OperateResult<T> { IsSuccess = true, ErrorCode = 0, Message = StringResources.Language.SuccessText, Content = value };
    public static OperateResult<T1, T2> CreateSuccessResult<T1, T2>(T1 value1, T2 value2) => new OperateResult<T1, T2> { IsSuccess = true, ErrorCode = 0, Message = StringResources.Language.SuccessText, Content1 = value1, Content2 = value2, };
    public static OperateResult<T1, T2, T3> CreateSuccessResult<T1, T2, T3>(T1 value1, T2 value2, T3 value3) => new OperateResult<T1, T2, T3> { IsSuccess = true, ErrorCode = 0, Message = StringResources.Language.SuccessText, Content1 = value1, Content2 = value2, Content3 = value3, };
    public static OperateResult<T1, T2, T3, T4> CreateSuccessResult<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) => new OperateResult<T1, T2, T3, T4> { IsSuccess = true, ErrorCode = 0, Message = StringResources.Language.SuccessText, Content1 = value1, Content2 = value2, Content3 = value3, Content4 = value4, };
}

/// <summary>
/// 操作结果的泛型类，允许带一个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T">泛型类</typeparam>
public class OperateResult<T> : OperateResult {
    public T Content { get; set; }

    public OperateResult() : base() { }

    public OperateResult(string msg) : base(msg) { }

    public OperateResult(int err, string msg) : base(err, msg) { }
}

/// <summary>
/// 操作结果的泛型类，允许带两个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
public class OperateResult<T1, T2> : OperateResult {
    public T1 Content1 { get; set; }

    public T2 Content2 { get; set; }

    public OperateResult() : base() { }

    public OperateResult(string msg) : base(msg) { }

    public OperateResult(int err, string msg) : base(err, msg) { }
}

/// <summary>
/// 操作结果的泛型类，允许带三个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
public class OperateResult<T1, T2, T3> : OperateResult {
    public T1 Content1 { get; set; }

    public T2 Content2 { get; set; }

    public T3 Content3 { get; set; }

    public OperateResult() : base() { }

    public OperateResult(string msg) : base(msg) { }

    public OperateResult(int err, string msg) : base(err, msg) { }
}

/// <summary>
/// 操作结果的泛型类，允许带四个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4> : OperateResult {
    public T1 Content1 { get; set; }

    public T2 Content2 { get; set; }

    public T3 Content3 { get; set; }

    public T4 Content4 { get; set; }

    public OperateResult() : base() { }

    public OperateResult(string msg) : base(msg) { }

    public OperateResult(int err, string msg) : base(err, msg) { }
}