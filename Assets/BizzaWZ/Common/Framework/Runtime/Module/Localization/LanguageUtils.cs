using System.Collections;
using System.Globalization;
using UnityEngine;

public class LanguageUtils
{
    private const string DefaultLanguage = "en-US";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitLanguage()
    {
        string saved = PlayerPrefs.GetString(nameof(SelectedLanguage), string.Empty);
        if (!HasReadLanguage)
            SelectedLanguage = GetStandardLanguageCode();
        else if (!IsSupportedLanguage(saved))
            SelectedLanguage = DefaultLanguage;
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
        get => PlayerPrefs.GetString(nameof(SelectedLanguage), DefaultLanguage);
        set
        {
            string language = IsSupportedLanguage(value) ? value : DefaultLanguage;
            if (HasReadLanguage && SelectedLanguage == language) return;
            PlayerPrefs.SetString(nameof(SelectedLanguage), language);
            PlayerPrefs.Save();
            BizzaEventSystem.Emit(EventDefine.Frame.LanguageChange);
        }
    }

    public static bool HasReadLanguage => PlayerPrefs.HasKey(nameof(SelectedLanguage));

    private static bool IsSupportedLanguage(string language) =>
        language is "en-US" or "pt-BR" or "id-ID" or "zh-CN" or "ru-RU" or "ja-JP" or "ko-KR" or
            "es-ES" or "es-MX" or "tr-TR" or "vi-VN" or "en-PH" or "ar-SA" or "de-DE";

    public static string GetStandardLanguageCode() => Application.systemLanguage switch
    {
        SystemLanguage.Portuguese => "pt-BR",
        SystemLanguage.Indonesian => "id-ID",
        SystemLanguage.Chinese or SystemLanguage.ChineseSimplified or SystemLanguage.ChineseTraditional => "zh-CN",
        SystemLanguage.Russian => "ru-RU",
        SystemLanguage.Japanese => "ja-JP",
        SystemLanguage.Korean => "ko-KR",
        SystemLanguage.English when CultureInfo.CurrentCulture.Name.EndsWith("-PH", System.StringComparison.OrdinalIgnoreCase) => "en-PH",
        SystemLanguage.Spanish when CultureInfo.CurrentCulture.Name.EndsWith("-MX", System.StringComparison.OrdinalIgnoreCase) => "es-MX",
        SystemLanguage.Spanish => "es-ES",
        SystemLanguage.Turkish => "tr-TR",
        SystemLanguage.Vietnamese => "vi-VN",
        SystemLanguage.Arabic => "ar-SA",
        SystemLanguage.German => "de-DE",
        _ => DefaultLanguage
    };

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


    public static IEnumerator InitLanguageFromIP()
    {
        InitLanguage();
        yield break;
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
        if (dict.Dict.TryGetValue(SelectedLanguage, out var text) && !string.IsNullOrEmpty(text)) return text;
        return dict.Dict.TryGetValue(DefaultLanguage, out var fallback) && !string.IsNullOrEmpty(fallback)
            ? fallback
            : key;
#endif
    }

    public static string GetFormatText(string key, params object[] args)
    {
        return string.Format(GetText(key), args);
    }
}
