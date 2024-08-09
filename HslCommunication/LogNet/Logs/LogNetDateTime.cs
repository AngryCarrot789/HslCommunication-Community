using System.Globalization;
using HslCommunication.LogNet.Core;

namespace HslCommunication.LogNet.Logs;

/// <summary>
/// 一个日志组件，可以根据时间来区分不同的文件存储
/// </summary>
/// <remarks>
/// 此日志实例将根据日期时间来进行分类，支持的时间分类如下：
/// <list type="number">
/// <item>小时</item>
/// <item>天</item>
/// <item>周</item>
/// <item>月份</item>
/// <item>季度</item>
/// <item>年份</item>
/// </list>
/// </remarks>
public class LogNetDateTime : LogNetBase, ILogNet {
    /// <summary>
    /// 实例化一个根据时间存储的日志组件
    /// </summary>
    /// <param name="filePath">文件存储的路径</param>
    /// <param name="generateMode">存储文件的间隔</param>
    public LogNetDateTime(string filePath, GenerateMode generateMode = GenerateMode.ByEveryYear) {
        this.m_filePath = filePath;
        this.generateMode = generateMode;

        this.LogSaveMode = LogNetManagment.LogSaveModeByDateTime;

        this.m_filePath = this.CheckPathEndWithSprit(this.m_filePath);
    }

    /// <summary>
    /// 获取需要保存的日志文件
    /// </summary>
    /// <returns>完整的文件路径，含文件名</returns>
    protected override string GetFileSaveName() {
        if (string.IsNullOrEmpty(this.m_filePath))
            return string.Empty;

        switch (this.generateMode) {
            case GenerateMode.ByEveryHour: {
                return this.m_filePath + LogNetManagment.LogFileHeadString + DateTime.Now.ToString("yyyyMMdd_HH") + ".txt";
            }
            case GenerateMode.ByEveryDay: {
                return this.m_filePath + LogNetManagment.LogFileHeadString + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            }
            case GenerateMode.ByEveryWeek: {
                GregorianCalendar gc = new GregorianCalendar();
                int weekOfYear = gc.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                return this.m_filePath + LogNetManagment.LogFileHeadString + DateTime.Now.Year + "_W" + weekOfYear + ".txt";
            }
            case GenerateMode.ByEveryMonth: {
                return this.m_filePath + LogNetManagment.LogFileHeadString + DateTime.Now.ToString("yyyy_MM") + ".txt";
            }
            case GenerateMode.ByEverySeason: {
                return this.m_filePath + LogNetManagment.LogFileHeadString + DateTime.Now.Year + "_Q" + (DateTime.Now.Month / 3 + 1) + ".txt";
            }
            case GenerateMode.ByEveryYear: {
                return this.m_filePath + LogNetManagment.LogFileHeadString + DateTime.Now.Year + ".txt";
            }
            default: return string.Empty;
        }
    }

    /// <summary>
    /// 获取所有的文件夹中的日志文件
    /// </summary>
    /// <returns>所有的文件路径集合</returns>
    public string[] GetExistLogFileNames() {
        if (!string.IsNullOrEmpty(this.m_filePath)) {
            return Directory.GetFiles(this.m_filePath, LogNetManagment.LogFileHeadString + "*.txt");
        }
        else {
            return new string[] { };
        }
    }

    private string m_fileName = string.Empty; // 当前正在存储的文件名称
    private string m_filePath = string.Empty; // 文件的路径
    private GenerateMode generateMode = GenerateMode.ByEveryYear; // 文件的存储模式，默认按照年份来存储

    /// <summary>
    /// 返回表示当前对象的字符串
    /// </summary>
    /// <returns>字符串</returns>
    public override string ToString() {
        return $"LogNetDateTime[{this.generateMode}]";
    }
}