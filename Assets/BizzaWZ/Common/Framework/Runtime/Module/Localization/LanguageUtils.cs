using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class LanguageUtils
{
    private static string DefaultLanguage
    {
        get => "en-US";
    }

// #if UNITY_EDITOR
//     [UnityEditor.MenuItem("工具/本地化/选择语言（需要按Ctrl+W） %w")]
//     public static void ChangeLanguage_ZH()
//     {
//         var cur = UnityEditor.EditorPrefs.GetInt("Editor_GameLanguage", 0);
//         UnityEditor.GenericMenu menu = new UnityEditor.GenericMenu();
//         foreach (var v in languageMap)
//         {
//             bool select = false;
//             if (cur > 0 && languageMap.TryGetValue((SystemLanguage)cur, out var val))
//             {
//                 if (val == v.Value)
//                 {
//                     select = true;
//                 }
//             }
//             menu.AddItem(new GUIContent(v.Value), select, OnSelectLanguage, v.Key);
//         }
//         menu.ShowAsContext();
//     }
//
//     [UnityEditor.MenuItem("工具/本地化/清除选中语言")]
//     public static void ClearLanguage()
//     {
//         UnityEditor.EditorPrefs.DeleteKey("Editor_GameLanguage");
//     }

//     private static void OnSelectLanguage(object o)
//     {
//         var code = (int)o;
//         UnityEditor.EditorPrefs.SetInt("Editor_GameLanguage", code);
//         Debug.Log(code);
//     }
// #endif


    public static string SelectedLanguage
    {
        //先默认英语
        get => PlayerPrefs.GetString(nameof(SelectedLanguage), DefaultLanguage);
        set
        {
            PlayerPrefs.SetString(nameof(SelectedLanguage), value);
            PlayerPrefs.Save();
            BizzaEventSystem.Emit(EventDefine.Frame.LanguageChange);
        }
    }

    public static bool HasReadLanguage => PlayerPrefs.HasKey(nameof(SelectedLanguage));

    public static string GetStandardLanguageCode()
    {
        // 1. 获取Unity识别的系统语言
        SystemLanguage unityLang = Application.systemLanguage;

        // 2. 获取系统默认的地区文化信息（包含地区代码）
        CultureInfo systemCulture = CultureInfo.CurrentCulture;
        string regionCode = systemCulture.Name.Split('-')[1]; // 提取地区代码（如US、CN）

        // 3. 映射为标准BCP 47格式（语言代码-地区代码）
        return unityLang switch
        {
            SystemLanguage.Chinese => "zh-CN", // 简体中文（默认中国地区）
            SystemLanguage.ChineseSimplified => "zh-CN",
            SystemLanguage.ChineseTraditional => "zh-CN", // 繁体中文（默认台湾地区）
            SystemLanguage.English => $"en-{regionCode}",
            SystemLanguage.Portuguese => $"pt-{regionCode}", // 葡萄牙语（如pt-PT、pt-BR）
            SystemLanguage.Indonesian => "id-ID",
            SystemLanguage.Russian => "ru-RU",
            SystemLanguage.Japanese => "ja-JP",
            SystemLanguage.Korean => "ko-KR",
            SystemLanguage.Spanish => $"es-{regionCode}", // 西班牙语（如es-ES、es-MX）
            SystemLanguage.Turkish => "tr-TR",
            SystemLanguage.Vietnamese => "vi-VN",


            // SystemLanguage.German => $"de-{regionCode}",
            // SystemLanguage.French => $"fr-{regionCode}",
            // SystemLanguage.Italian => "it-IT",
            _ => "en-US" // 默认返回英语（美国）
        };
    }

    // public static string GetFormatNum(int num)
    // {
    //     return system.GetFormatNum(num);
    // }

    public static string GetFormatText(string key, object arg1)
    {
        return string.Format(GetText(key), arg1);
    }

    public static string GetFormatText(string key, object arg1, object arg2)
    {
        return string.Format(GetText(key), arg1, arg2);
    }

    public static string GetFormatText(string key, object arg1, object arg2, object arg3)
    {
        return string.Format(GetText(key), arg1, arg2, arg3);
    }

    // public static string GetFormatText<T1>(string key, T1 arg1)
    // {
    //     return string.Format(GetText(key), arg1.ToString());
    // }
    //
    // public static string GetFormatText<T1, T2>(string key, T1 arg1, T2 arg2)
    // {
    //     return string.Format(GetText(key), arg1.ToString(), arg2.ToString());
    // }
    //
    // public static string GetFormatText<T1, T2, T3>(string key, T1 arg1, T2 arg2, T3 arg3)
    // {
    //     return string.Format(GetText(key), arg1.ToString(), arg2.ToString(), arg3.ToString());
    // }


    /// <summary>
    /// 如果没有存储语言，则通过 IP 自动识别
    /// </summary>
    public static IEnumerator InitLanguageFromIP()
    {
        if (!string.IsNullOrEmpty(SelectedLanguage))
        {
            Debug.Log("[Language] 使用存储语言: " + SelectedLanguage);
            yield break;
        }

        const string defaultCountryCode = "US";
        SelectedLanguage = MapCountryToLanguage(defaultCountryCode);
        Debug.Log("[Language] 使用默认国家码设置语言: " + SelectedLanguage);
        yield break;
    }


    /// <summary>
    /// 国家码映射到语言
    /// </summary>
    private static string MapCountryToLanguage(string country)
    {
        switch (country)
        {
            case "BR": return "pt-BR";
            case "ID": return "id-ID";
            case "RU": return "ru-RU";
            case "JP": return "ja-JP";
            case "KR": return "ko-KR";

            case "ES":
            case "MX":
            case "AR":
            case "CO":
            case "CL":
            case "PE":
            case "VE":
                return "es-ES";

            case "TR": return "tr-TR";
            case "VN": return "vi-VN";

            case "US":
            case "GB":
            case "CA":
            case "AU":
            case "NZ":
            case "PH":
                return "en-US";

            default:
                return "en-US";
        }
    }

    // ======================= 语言表读取 ==========================
    public static string GetText(string key, string whiteBuildText = null)
    {
#if !BIZZA_REAL_WITHDRAW
        if (!string.IsNullOrEmpty(whiteBuildText))
        {
            return whiteBuildText;
        }
        return key;
#else
        var table = TableUtils.Tables.TblLanguage;
        if (table == null || table.DataMap == null)
        {
#if UNITY_EDITOR
            LogLogger.LogInfo($"GetText failed table is null:{key}");
#endif
            return key;
        }
        var dict = table.GetOrDefault(key);
        if (dict == null) return key;
        var curLanguage = SelectedLanguage;
        return dict.Dict.GetValueOrDefault(curLanguage, key);
#endif
    }

    public static string GetFormatText(string key, params object[] args)
    {
        return string.Format(GetText(key), args);
    }
}
