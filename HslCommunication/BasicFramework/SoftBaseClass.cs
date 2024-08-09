﻿using HslCommunication.Core;
using System.Text;
using HslCommunication.Core.Thread;
using HslCommunication.LogNet.Core;

namespace HslCommunication.BasicFramework;

/*****************************************************************************
 *
 *    一些类的基类，实现一些共同的基础功能
 *
 *    Create Date : 2017-05-03 17:45:37
 *
 *
 *    2018年3月6日 21:38:37
 *    修改：提炼接口，完善注释和分块
 *
 *
 *****************************************************************************/

/// <summary>
/// 支持字符串信息加载存储的接口，定义了几个通用的方法
/// </summary>
public interface ISoftFileSaveBase {
    /// <summary>
    /// 获取需要保存的数据，需要重写实现
    /// </summary>
    /// <returns>需要存储的信息</returns>
    string ToSaveString();

    /// <summary>
    /// 从字符串加载数据，需要重写实现
    /// </summary>
    /// <param name="content">字符串数据</param>
    void LoadByString(string content);


    /// <summary>
    /// 不使用解密方法从文件读取数据
    /// </summary>
    void LoadByFile();


    /// <summary>
    /// 不使用加密方法保存数据到文件
    /// </summary>
    void SaveToFile();


    /// <summary>
    /// 文件路径的存储
    /// </summary>
    string FileSavePath { get; set; }
}

/// <summary>
/// 文件存储功能的基类，包含了文件存储路径，存储方法等
/// </summary>
/// <remarks>
/// 需要继承才能实现你想存储的数据，比较经典的例子就是存储你的应用程序的配置信息，通常的格式就是xml文件或是json文件。具体请看例子：
/// </remarks>
/// <example>
/// 下面举例实现两个字段的普通数据存储
/// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\BasicFramework\SoftFileSaveBaseExample.cs" region="SoftFileSaveBase1" title="简单示例" />
/// 然后怎么调用呢？
/// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\BasicFramework\SoftFileSaveBaseExample.cs" region="Example" title="调用示例" />
/// 如果你想实现加密存储，这样就不用关心被用户看到了。
/// <code lang="cs" source="HslCommunication.Test\Documentation\Samples\BasicFramework\SoftFileSaveBaseExample.cs" region="SoftFileSaveBase2" title="加密示例" />
/// 如果还是担心被反编译获取数据，那么这个密钥就要来自服务器的数据，本地不做存储。
/// </example>
public class SoftFileSaveBase : ISoftFileSaveBase {
    /// <summary>
    /// 实例化一个文件存储的基类
    /// </summary>
    public SoftFileSaveBase() {
        this.HybirdLock = new SimpleHybirdLock();
    }

    private SimpleHybirdLock HybirdLock; // 文件存储的同步锁

    /// <summary>
    /// 在日志保存时的标记当前调用类的信息
    /// </summary>
    protected string LogHeaderText { get; set; }

    /// <summary>
    /// 获取需要保存的数据，需要重写实现
    /// </summary>
    /// <returns>需要存储的信息</returns>
    public virtual string ToSaveString() {
        return string.Empty;
    }


    /// <summary>
    /// 从字符串加载数据，需要重写实现
    /// </summary>
    /// <param name="content">字符串数据</param>
    public virtual void LoadByString(string content) {
    }

    /// <summary>
    /// 不使用解密方法从文件读取数据
    /// </summary>
    public virtual void LoadByFile() {
        this.LoadByFile(m => m);
    }


    /// <summary>
    /// 使用用户自定义的解密方法从文件读取数据
    /// </summary>
    /// <param name="decrypt">用户自定义的解密方法</param>
    public void LoadByFile(Converter<string, string> decrypt) {
        if (this.FileSavePath != "") {
            if (File.Exists(this.FileSavePath)) {
                this.HybirdLock.Enter();
                try {
                    using (StreamReader sr = new StreamReader(this.FileSavePath, Encoding.Default)) {
                        this.LoadByString(decrypt(sr.ReadToEnd()));
                    }
                }
                catch (Exception ex) {
                    this.ILogNet?.WriteException(this.LogHeaderText, StringResources.Language.FileLoadFailed, ex);
                }
                finally {
                    this.HybirdLock.Leave();
                }
            }
        }
    }


    /// <summary>
    /// 不使用加密方法保存数据到文件
    /// </summary>
    public virtual void SaveToFile() {
        this.SaveToFile(m => m);
    }


    /// <summary>
    /// 使用用户自定义的加密方法保存数据到文件
    /// </summary>
    /// <param name="encrypt">用户自定义的加密方法</param>
    public void SaveToFile(Converter<string, string> encrypt) {
        if (this.FileSavePath != "") {
            this.HybirdLock.Enter();
            try {
                using (StreamWriter sw = new StreamWriter(this.FileSavePath, false, Encoding.Default)) {
                    sw.Write(encrypt(this.ToSaveString()));
                    sw.Flush();
                }
            }
            catch (Exception ex) {
                this.ILogNet?.WriteException(this.LogHeaderText, StringResources.Language.FileSaveFailed, ex);
            }
            finally {
                this.HybirdLock.Leave();
            }
        }
    }


    /// <summary>
    /// 文件存储的路径
    /// </summary>
    public string FileSavePath { get; set; }

    /// <summary>
    /// 日志记录类
    /// </summary>
    public ILogNet ILogNet { get; set; }
}