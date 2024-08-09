namespace HslCommunication;

/*******************************************************************************
 *
 *    用于显示和保存的数据信息，未来支持中英文
 *
 *    Used to the return result class in the synchronize communication and communication for industrial Ethernet
 *
 *******************************************************************************/

/// <summary>
/// 系统的字符串资源及多语言管理中心 ->
/// System string resource and multi-language management Center
/// </summary>
public static class StringResources {
    static StringResources() {
        if (System.Globalization.CultureInfo.CurrentCulture.ToString().StartsWith("zh")) {
            SetLanguageChinese();
        }
        else {
            SeteLanguageEnglish();
        }
    }


    /// <summary>
    /// 获取或设置系统的语言选项 ->
    /// Gets or sets the language options for the system
    /// </summary>
    public static Language.English Language = new Language.English();

    /// <summary>
    /// 将语言设置为中文 ->
    /// Set the language to Chinese
    /// </summary>
    public static void SetLanguageChinese() {
        Language = new Language.English();
    }

    /// <summary>
    /// 将语言设置为英文 ->
    /// Set the language to English
    /// </summary>
    public static void SeteLanguageEnglish() {
        Language = new Language.English();
    }
}