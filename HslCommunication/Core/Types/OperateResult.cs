using System.Runtime.CompilerServices;

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
    public bool IsSuccess { get; }
    public string Message { get; }
    public int ErrorCode { get; }

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
    
    public OperateResult ToFailedResult() => new OperateResult(this.ErrorCode, this.Message);
    public OperateResult<T> ToFailedResult<T>() => new OperateResult<T>(this.ErrorCode, this.Message);
    public OperateResult<T1, T2> ToFailedResult<T1, T2>() => new OperateResult<T1, T2>(this.ErrorCode, this.Message);

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

    public LightOperationResult(string? msg = null) : this(10000, null) { }

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

    public LightOperationResult(string? msg = null) : this(10000, null) { }

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

    public LightOperationResult(string? msg = null) : this(10000, null) { }

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

/// <summary>
/// 操作结果的类，只带有成功标志和错误信息 -> The class that operates the result, with only success flags and error messages
/// </summary>
/// <remarks>
/// 当 <see cref="IsSuccess"/> 为 True 时，忽略 <see cref="Message"/> 及 <see cref="ErrorCode"/> 的值
/// </remarks>
public class OperateResult {
    /// <summary>
    /// 指示本次访问是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 具体的错误描述
    /// </summary>
    public string Message { get; set; } = StringResources.Language.UnknownError;

    /// <summary>
    /// 具体的错误代码
    /// </summary>
    public int ErrorCode { get; set; } = 10000;
    
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) {
        this.Message = msg;
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) {
        this.ErrorCode = err;
        this.Message = msg;
    }

    /// <summary>
    /// 获取错误代号及文本描述
    /// </summary>
    /// <returns>包含错误码及错误消息</returns>
    public string ToMessageShowString() {
        return $"{StringResources.Language.ErrorCode}:{this.ErrorCode}{Environment.NewLine}{StringResources.Language.TextDescription}:{this.Message}";
    }


    /// <summary>
    /// 从另一个结果类中拷贝错误信息
    /// </summary>
    /// <typeparam name="TResult">支持结果类及派生类</typeparam>
    /// <param name="result">结果类及派生类的对象</param>
    public void CopyErrorFromOther<TResult>(TResult result) where TResult : OperateResult {
        if (result != null) {
            this.ErrorCode = result.ErrorCode;
            this.Message = result.Message;
        }
    }

    /*****************************************************************************************************
     *
     *    主要是方便获取到一些特殊状态的结果对象
     *
     ******************************************************************************************************/


    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T">目标数据类型</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T> CreateFailedResult<T>(OperateResult result) {
        return new OperateResult<T>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }

    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2> CreateFailedResult<T1, T2>(OperateResult result) {
        return new OperateResult<T1, T2>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }


    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3> CreateFailedResult<T1, T2, T3>(OperateResult result) {
        return new OperateResult<T1, T2, T3>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }


    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <typeparam name="T4">目标数据类型四</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3, T4> CreateFailedResult<T1, T2, T3, T4>(OperateResult result) {
        return new OperateResult<T1, T2, T3, T4>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }


    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <typeparam name="T4">目标数据类型四</typeparam>
    /// <typeparam name="T5">目标数据类型五</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3, T4, T5> CreateFailedResult<T1, T2, T3, T4, T5>(OperateResult result) {
        return new OperateResult<T1, T2, T3, T4, T5>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }


    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <typeparam name="T4">目标数据类型四</typeparam>
    /// <typeparam name="T5">目标数据类型五</typeparam>
    /// <typeparam name="T6">目标数据类型六</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6> CreateFailedResult<T1, T2, T3, T4, T5, T6>(OperateResult result) {
        return new OperateResult<T1, T2, T3, T4, T5, T6>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }

    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <typeparam name="T4">目标数据类型四</typeparam>
    /// <typeparam name="T5">目标数据类型五</typeparam>
    /// <typeparam name="T6">目标数据类型六</typeparam>
    /// <typeparam name="T7">目标数据类型七</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7> CreateFailedResult<T1, T2, T3, T4, T5, T6, T7>(OperateResult result) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }

    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <typeparam name="T4">目标数据类型四</typeparam>
    /// <typeparam name="T5">目标数据类型五</typeparam>
    /// <typeparam name="T6">目标数据类型六</typeparam>
    /// <typeparam name="T7">目标数据类型七</typeparam>
    /// <typeparam name="T8">目标数据类型八</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7, T8> CreateFailedResult<T1, T2, T3, T4, T5, T6, T7, T8>(OperateResult result) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7, T8>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }


    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <typeparam name="T4">目标数据类型四</typeparam>
    /// <typeparam name="T5">目标数据类型五</typeparam>
    /// <typeparam name="T6">目标数据类型六</typeparam>
    /// <typeparam name="T7">目标数据类型七</typeparam>
    /// <typeparam name="T8">目标数据类型八</typeparam>
    /// <typeparam name="T9">目标数据类型九</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateFailedResult<T1, T2, T3, T4, T5, T6, T7, T8, T9>(OperateResult result) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }


    /// <summary>
    /// 创建并返回一个失败的结果对象，该对象复制另一个结果对象的错误信息
    /// </summary>
    /// <typeparam name="T1">目标数据类型一</typeparam>
    /// <typeparam name="T2">目标数据类型二</typeparam>
    /// <typeparam name="T3">目标数据类型三</typeparam>
    /// <typeparam name="T4">目标数据类型四</typeparam>
    /// <typeparam name="T5">目标数据类型五</typeparam>
    /// <typeparam name="T6">目标数据类型六</typeparam>
    /// <typeparam name="T7">目标数据类型七</typeparam>
    /// <typeparam name="T8">目标数据类型八</typeparam>
    /// <typeparam name="T9">目标数据类型九</typeparam>
    /// <typeparam name="T10">目标数据类型十</typeparam>
    /// <param name="result">之前的结果对象</param>
    /// <returns>带默认泛型对象的失败结果类</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateFailedResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(OperateResult result) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() {
            ErrorCode = result.ErrorCode,
            Message = result.Message,
        };
    }


    /// <summary>
    /// 创建并返回一个成功的结果对象
    /// </summary>
    /// <returns>成功的结果对象</returns>
    public static OperateResult CreateSuccessResult() {
        return new OperateResult() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
        };
    }

    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有一个参数对象
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="value">类型的值对象</param>
    /// <returns>成功的结果对象</returns>
    public static OperateResult<T> CreateSuccessResult<T>(T value) {
        return new OperateResult<T>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content = value
        };
    }


    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有两个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2> CreateSuccessResult<T1, T2>(T1 value1, T2 value2) {
        return new OperateResult<T1, T2>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
        };
    }


    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有三个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3> CreateSuccessResult<T1, T2, T3>(T1 value1, T2 value2, T3 value3) {
        return new OperateResult<T1, T2, T3>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
        };
    }

    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有四个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="T4">第四个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <param name="value4">类型四对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3, T4> CreateSuccessResult<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) {
        return new OperateResult<T1, T2, T3, T4>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
            Content4 = value4,
        };
    }


    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有五个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="T4">第四个参数类型</typeparam>
    /// <typeparam name="T5">第五个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <param name="value4">类型四对象</param>
    /// <param name="value5">类型五对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3, T4, T5> CreateSuccessResult<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) {
        return new OperateResult<T1, T2, T3, T4, T5>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
            Content4 = value4,
            Content5 = value5,
        };
    }

    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有六个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="T4">第四个参数类型</typeparam>
    /// <typeparam name="T5">第五个参数类型</typeparam>
    /// <typeparam name="T6">第六个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <param name="value4">类型四对象</param>
    /// <param name="value5">类型五对象</param>
    /// <param name="value6">类型六对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6> CreateSuccessResult<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) {
        return new OperateResult<T1, T2, T3, T4, T5, T6>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
            Content4 = value4,
            Content5 = value5,
            Content6 = value6,
        };
    }

    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有七个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="T4">第四个参数类型</typeparam>
    /// <typeparam name="T5">第五个参数类型</typeparam>
    /// <typeparam name="T6">第六个参数类型</typeparam>
    /// <typeparam name="T7">第七个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <param name="value4">类型四对象</param>
    /// <param name="value5">类型五对象</param>
    /// <param name="value6">类型六对象</param>
    /// <param name="value7">类型七对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7> CreateSuccessResult<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
            Content4 = value4,
            Content5 = value5,
            Content6 = value6,
            Content7 = value7,
        };
    }


    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有八个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="T4">第四个参数类型</typeparam>
    /// <typeparam name="T5">第五个参数类型</typeparam>
    /// <typeparam name="T6">第六个参数类型</typeparam>
    /// <typeparam name="T7">第七个参数类型</typeparam>
    /// <typeparam name="T8">第八个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <param name="value4">类型四对象</param>
    /// <param name="value5">类型五对象</param>
    /// <param name="value6">类型六对象</param>
    /// <param name="value7">类型七对象</param>
    /// <param name="value8">类型八对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7, T8> CreateSuccessResult<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7, T8>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
            Content4 = value4,
            Content5 = value5,
            Content6 = value6,
            Content7 = value7,
            Content8 = value8,
        };
    }


    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有九个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="T4">第四个参数类型</typeparam>
    /// <typeparam name="T5">第五个参数类型</typeparam>
    /// <typeparam name="T6">第六个参数类型</typeparam>
    /// <typeparam name="T7">第七个参数类型</typeparam>
    /// <typeparam name="T8">第八个参数类型</typeparam>
    /// <typeparam name="T9">第九个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <param name="value4">类型四对象</param>
    /// <param name="value5">类型五对象</param>
    /// <param name="value6">类型六对象</param>
    /// <param name="value7">类型七对象</param>
    /// <param name="value8">类型八对象</param>
    /// <param name="value9">类型九对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9> CreateSuccessResult<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
            Content4 = value4,
            Content5 = value5,
            Content6 = value6,
            Content7 = value7,
            Content8 = value8,
            Content9 = value9,
        };
    }

    /// <summary>
    /// 创建并返回一个成功的结果对象，并带有十个参数对象
    /// </summary>
    /// <typeparam name="T1">第一个参数类型</typeparam>
    /// <typeparam name="T2">第二个参数类型</typeparam>
    /// <typeparam name="T3">第三个参数类型</typeparam>
    /// <typeparam name="T4">第四个参数类型</typeparam>
    /// <typeparam name="T5">第五个参数类型</typeparam>
    /// <typeparam name="T6">第六个参数类型</typeparam>
    /// <typeparam name="T7">第七个参数类型</typeparam>
    /// <typeparam name="T8">第八个参数类型</typeparam>
    /// <typeparam name="T9">第九个参数类型</typeparam>
    /// <typeparam name="T10">第十个参数类型</typeparam>
    /// <param name="value1">类型一对象</param>
    /// <param name="value2">类型二对象</param>
    /// <param name="value3">类型三对象</param>
    /// <param name="value4">类型四对象</param>
    /// <param name="value5">类型五对象</param>
    /// <param name="value6">类型六对象</param>
    /// <param name="value7">类型七对象</param>
    /// <param name="value8">类型八对象</param>
    /// <param name="value9">类型九对象</param>
    /// <param name="value10">类型十对象</param>
    /// <returns>成的结果对象</returns>
    public static OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> CreateSuccessResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10) {
        return new OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() {
            IsSuccess = true,
            ErrorCode = 0,
            Message = StringResources.Language.SuccessText,
            Content1 = value1,
            Content2 = value2,
            Content3 = value3,
            Content4 = value4,
            Content5 = value5,
            Content6 = value6,
            Content7 = value7,
            Content8 = value8,
            Content9 = value9,
            Content10 = value10,
        };
    }
}

/// <summary>
/// 操作结果的泛型类，允许带一个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T">泛型类</typeparam>
public class OperateResult<T> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据
    /// </summary>
    public T Content { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带两个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
public class OperateResult<T1, T2> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带三个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
public class OperateResult<T1, T2, T3> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带四个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据4
    /// </summary>
    public T4 Content4 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带五个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
/// <typeparam name="T5">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4, T5> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据4
    /// </summary>
    public T4 Content4 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据5
    /// </summary>
    public T5 Content5 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带六个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
/// <typeparam name="T5">泛型类</typeparam>
/// <typeparam name="T6">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4, T5, T6> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据4
    /// </summary>
    public T4 Content4 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据5
    /// </summary>
    public T5 Content5 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据5
    /// </summary>
    public T6 Content6 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带七个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
/// <typeparam name="T5">泛型类</typeparam>
/// <typeparam name="T6">泛型类</typeparam>
/// <typeparam name="T7">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4, T5, T6, T7> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据4
    /// </summary>
    public T4 Content4 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据5
    /// </summary>
    public T5 Content5 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据6
    /// </summary>
    public T6 Content6 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据7
    /// </summary>
    public T7 Content7 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带八个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
/// <typeparam name="T5">泛型类</typeparam>
/// <typeparam name="T6">泛型类</typeparam>
/// <typeparam name="T7">泛型类</typeparam>
/// <typeparam name="T8">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4, T5, T6, T7, T8> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据4
    /// </summary>
    public T4 Content4 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据5
    /// </summary>
    public T5 Content5 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据6
    /// </summary>
    public T6 Content6 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据7
    /// </summary>
    public T7 Content7 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据8
    /// </summary>
    public T8 Content8 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带九个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
/// <typeparam name="T5">泛型类</typeparam>
/// <typeparam name="T6">泛型类</typeparam>
/// <typeparam name="T7">泛型类</typeparam>
/// <typeparam name="T8">泛型类</typeparam>
/// <typeparam name="T9">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据4
    /// </summary>
    public T4 Content4 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据5
    /// </summary>
    public T5 Content5 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据6
    /// </summary>
    public T6 Content6 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据7
    /// </summary>
    public T7 Content7 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据8
    /// </summary>
    public T8 Content8 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据9
    /// </summary>
    public T9 Content9 { get; set; }
}

/// <summary>
/// 操作结果的泛型类，允许带十个用户自定义的泛型对象，推荐使用这个类
/// </summary>
/// <typeparam name="T1">泛型类</typeparam>
/// <typeparam name="T2">泛型类</typeparam>
/// <typeparam name="T3">泛型类</typeparam>
/// <typeparam name="T4">泛型类</typeparam>
/// <typeparam name="T5">泛型类</typeparam>
/// <typeparam name="T6">泛型类</typeparam>
/// <typeparam name="T7">泛型类</typeparam>
/// <typeparam name="T8">泛型类</typeparam>
/// <typeparam name="T9">泛型类</typeparam>
/// <typeparam name="T10">泛型类</typeparam>
public class OperateResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : OperateResult {
    /// <summary>
    /// 实例化一个默认的结果对象
    /// </summary>
    public OperateResult() : base() {
    }

    /// <summary>
    /// 使用指定的消息实例化一个默认的结果对象
    /// </summary>
    /// <param name="msg">错误消息</param>
    public OperateResult(string msg) : base(msg) {
    }

    /// <summary>
    /// 使用错误代码，消息文本来实例化对象
    /// </summary>
    /// <param name="err">错误代码</param>
    /// <param name="msg">错误消息</param>
    public OperateResult(int err, string msg) : base(err, msg) {
    }

    /// <summary>
    /// 用户自定义的泛型数据1
    /// </summary>
    public T1 Content1 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据2
    /// </summary>
    public T2 Content2 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据3
    /// </summary>
    public T3 Content3 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据4
    /// </summary>
    public T4 Content4 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据5
    /// </summary>
    public T5 Content5 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据6
    /// </summary>
    public T6 Content6 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据7
    /// </summary>
    public T7 Content7 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据8
    /// </summary>
    public T8 Content8 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据9
    /// </summary>
    public T9 Content9 { get; set; }

    /// <summary>
    /// 用户自定义的泛型数据10
    /// </summary>
    public T10 Content10 { get; set; }
}