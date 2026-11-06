using System;
using System.Collections.Generic;
using UnityEngine;
using Bizza.Unity.Android;
using System.IO;
using System.Text;
public static class DeviceNativeBridge
{
    const string ClassName = "com.bangsawan.oceanshine.DeviceUtils";
    const string GetAndroidIdMethod = "GetAndroidId";

    const string GetBrandMethod = "GetBrand";
    const string GetModelMethod = "GetModel";
    const string GetOSVersionMethod = "GetOSVersion";
    const string GetGAIDMethod = "GetGAID";
    const string GetNetworkTypeMethod = "GetNetworkType";
    const string IsRootedMethod = "IsRooted";
    const string HasSimCardMethod = "HasSimCard";
    const string GetCountryCodeMethod = "GetCountryCode";
    const string GetChannelMethod = "GetChannel";

    const string GetNetworkCountryIsoMethod = "GetNetworkCountryIso";
    const string GetNativeLocaleMethod = "GetNativeLocale";
    const string GetNativeLocaleCountryMethod = "GetNativeLocaleCountry";
    const string GetNativeTimeZoneIdMethod = "GetNativeTimeZoneId";
    const string GetDefaultInputMethodMethod = "GetDefaultInputMethod";
    const string GetEnabledInputMethodsMethod = "GetEnabledInputMethods";
    const string GetSimCountryIsoMethod = "GetSimCountryIso";
    const string GetSimOperatorMethod = "GetSimOperator";
    const string GetNetworkOperatorMethod = "GetNetworkOperator";
    const string GetNetworkOperatorNameMethod = "GetNetworkOperatorName";
    const string IsVpnConnectedMethod = "IsVpnConnected";

    // 非 WKY 项目使用完整 ISO 3166-1 alpha-2 国家/地区代码，避免接入时重复维护。
    // None 是现有配置使用的空国家哨兵，不属于 ISO 国家代码。
    private const string GlobalCountryCodeText =
        "None AD AE AF AG AI AL AM AO AQ AR AS AT AU AW AX AZ " +
        "BA BB BD BE BF BG BH BI BJ BL BM BN BO BQ BR BS BT BV BW BY BZ " +
        "CA CC CD CF CG CH CI CK CL CM CN CO CR CU CV CW CX CY CZ " +
        "DE DJ DK DM DO DZ EC EE EG EH ER ES ET FI FJ FK FM FO FR " +
        "GA GB GD GE GF GG GH GI GL GM GN GP GQ GR GS GT GU GW GY " +
        "HK HM HN HR HT HU ID IE IL IM IN IO IQ IR IS IT JE JM JO JP " +
        "KE KG KH KI KM KN KP KR KW KY KZ LA LB LC LI LK LR LS LT LU LV LY " +
        "MA MC MD ME MF MG MH MK ML MM MN MO MP MQ MR MS MT MU MV MW MX MY MZ " +
        "NA NC NE NF NG NI NL NO NP NR NU NZ OM PA PE PF PG PH PK PL PM PN PR PS PT PW PY " +
        "QA RE RO RS RU RW SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR SS ST SV SX SY SZ " +
        "TC TD TF TG TH TJ TK TL TM TN TO TR TT TV TW TZ UA UG UM US UY UZ " +
        "VA VC VE VG VI VN VU WF WS YE YT ZA ZM ZW";

    private static readonly string[] SupportedCountryNames =
        ResolveSupportedCountryNames();

    private static string[] ResolveSupportedCountryNames()
    {
#if WKY_SDK
        try
        {
            System.Reflection.BindingFlags nestedTypeFlags =
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

            foreach (System.Reflection.Assembly assembly in
                     System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type accountType = assembly.GetType(
                    "AccountModule",
                    throwOnError: false);
                System.Type countryType = accountType?.GetNestedType(
                    "E_CountryType",
                    nestedTypeFlags);

                if (countryType == null || !countryType.IsEnum)
                {
                    continue;
                }

                string[] countryNames = System.Enum.GetNames(countryType);
                if (countryNames.Length > 0)
                {
                    return countryNames;
                }
            }

            Debug.LogError(
                "[DeviceNativeBridge] WKY_SDK 已开启，但未找到 " +
                "AccountModule.E_CountryType，将使用全球国家代码兜底。");
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "[DeviceNativeBridge] 读取 AccountModule.E_CountryType 失败，" +
                $"将使用全球国家代码兜底：{exception.Message}");
        }
#endif

        return GlobalCountryCodeText.Split(' ');
    }

    private static readonly System.Random Random = new System.Random();
    private static readonly HashSet<string> LoggedDeviceEnvironments = new HashSet<string>();

    private static void LogDeviceEnvironmentOnce(string methodName, string environment)
    {
        string key = methodName + "|" + environment;
        if (LoggedDeviceEnvironments.Add(key))
        {
            Debug.Log(
                $"[测试日志][真机环境] 功能=设备信息(DeviceNativeBridge); 方法={methodName}; 环境={environment}");
        }
    }

    private static void LogFallbackEnvironmentOnce(string methodName)
    {
#if UNITY_EDITOR
        LogDeviceEnvironmentOnce(methodName, "编辑器");
#else
        LogDeviceEnvironmentOnce(methodName, "非安卓运行环境");
#endif
    }

    /* ===================== 安卓ID ===================== */
    /// <summary>
    /// 安卓ID（Android Settings.Secure.ANDROID_ID）
    /// 需要 Android 原生实现
    /// </summary>
    public static string GetAndroidId(string _fakeAndroidId = "")
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetAndroidId), "安卓真机");
        var temp = JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetAndroidIdMethod);
        DeviceInfoLog.LogUserInstall("获取的 GetAndroidId 信息 ： " + temp);
        return temp;
#else
        LogFallbackEnvironmentOnce(nameof(GetAndroidId));
        string fakeAndroidId = GenerateFakeAndroidId(_fakeAndroidId);
        return fakeAndroidId;
#endif
    }

    private static string GenerateFakeAndroidId(string fakeAndroidId)
    {
        string directoryPath = fakeAndroidId;
        const string prefix = "Bizza";
        const string suffix = "Bizza";
        const string chars = "0123456789abcdef";
        const int totalLength = 16;


        try
        {
            int middleLength = totalLength - prefix.Length - suffix.Length;
            if (middleLength < 0)
            {
                Debug.LogError("prefix 和 suffix 长度之和超过了 totalLength");
                return string.Empty;
            }

            var sb = new StringBuilder(totalLength);
            sb.Append(prefix);

            for (int i = 0; i < middleLength; i++)
            {
                sb.Append(chars[Random.Next(chars.Length)]);
            }

            sb.Append(suffix);

            string id = sb.ToString();

            return id;
        }
        catch (Exception ex)
        {
            Debug.LogError($"GenerateFakeAndroidId 失败: {ex}");
            return string.Empty;
        }
    }

    /* ===================== 手机品牌 ===================== */
    /// <summary>
    /// 手机品牌（Android Build.BRAND）
    /// 需要原生实现（Unity SystemInfo.deviceModel 不等价）
    /// </summary>
    public static string GetBrand()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetBrand), "安卓真机");
        var temp = JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetBrandMethod);
                DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "获取的 GetBrandMethod 信息 ： " + temp);
        return temp;
#else
        LogFallbackEnvironmentOnce(nameof(GetBrand));
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "走的测试路径 ： ");
        return "UMIDIGI";
#endif
    }

    /* ===================== 设备型号 ===================== */
    /// <summary>
    /// 设备型号（Android Build.MODEL）
    /// 需要原生实现
    /// </summary>
    public static string GetModel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetModel), "安卓真机");
        var temp = JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetModelMethod);
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "获取的 GetModelMethod 信息 ： " + temp);
        return temp;
#else
        LogFallbackEnvironmentOnce(nameof(GetModel));
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "走的测试路径 ： ");
        return SystemInfo.deviceModel;
#endif
    }

    /* ===================== 系统版本 ===================== */
    /// <summary>
    /// 系统版本（Android Build.VERSION.RELEASE / iOS systemVersion）
    /// 需要原生实现
    /// </summary>
    public static string GetOSVersion()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetOSVersion), "安卓真机");
        var temp = JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetOSVersionMethod);
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "获取的 GetOSVersionMethod 信息 ： " + temp);
        return temp;
#else
        LogFallbackEnvironmentOnce(nameof(GetOSVersion));
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "走的测试路径 ： ");
        return SystemInfo.operatingSystem;
#endif
    }

    public static void GetGoogleId()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetGoogleId), "安卓真机");
    JavaBridgeUtils.CallStaticMethodCacheClass(ClassName, GetGAIDMethod);
        return;
#else
        LogFallbackEnvironmentOnce(nameof(GetGoogleId));
        string fakeGaid = GenerateFakeGaid();
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "fakeGaid 已生成");
        BizzaEventSystem.Emit(FrameEvent.Android.UnityJavaObjectGoogleMessage, fakeGaid);
#endif
    }

    private static string GenerateFakeGaid()
    {
        return System.Guid.NewGuid().ToString().ToLower();
    }

    /* ===================== 网络类型 ===================== */
    /// <summary>
    /// 网络类型（WIFI / MOBILE / NONE）
    /// Unity 无法准确获取，需原生
    /// </summary>
    public static string GetNetworkType()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetNetworkType), "安卓真机");
        var temp = JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetNetworkTypeMethod);
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "获取的 GetNetworkTypeMethod 信息 ： " + temp);
        return temp;
#else
        LogFallbackEnvironmentOnce(nameof(GetNetworkType));
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "走的测试路径 ： ");
        return Application.internetReachability.ToString();
#endif
    }

    /* ===================== Root 检测 ===================== */
    /// <summary>
    /// 是否 Root（1=是 0=否）
    /// 需要原生实现
    /// </summary>
    public static int IsRooted()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(IsRooted), "安卓真机");
        var result = JavaBridgeUtils.CallStaticMethodCacheClass<int>(ClassName, IsRootedMethod);
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "IsRooted " + result);
        return result;
#else
        LogFallbackEnvironmentOnce(nameof(IsRooted));
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "走的测试路径 ： ");
        return 0;
#endif
    }

    /* ===================== SIM 卡 ===================== */
    /// <summary>
    /// 是否有 SIM 卡（1=有 0=无）
    /// 需要原生 + 权限
    /// </summary>
    public static int HasSimCard()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(HasSimCard), "安卓真机");
        var result = JavaBridgeUtils.CallStaticMethodCacheClass<int>(ClassName, HasSimCardMethod);
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "HasSimCard " + result);
        return result;
#else
        LogFallbackEnvironmentOnce(nameof(HasSimCard));
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "走的测试路径 ： ");
        return 0;
#endif
    }

    /* ===================== 国家码 ===================== */
    /// <summary>
    /// 国家码（ISO 3166，如 CN / US）
    /// Unity兜底：CultureInfo
    /// 原生可用 SIM / Network 国家码更准
    /// </summary>
    public static string GetCountryCode(string country, string defaultCountry)
    {
        DeviceInfoLog.LogCountry($"oversea中的国家设置为 ： {country} —— ChannelConfig.real_CustomConfig.CountryName: {country} - defaultCountry: {defaultCountry}");

#if UNITY_ANDROID && !UNITY_EDITOR

#if !DEBUG_MODE
        country = "";
        DeviceInfoLog.LogCountry("被正式包强制修改为 默认国家 ");
#endif
        LogDeviceEnvironmentOnce(nameof(GetCountryCode), "安卓真机");
        if (string.Equals(country, "None", StringComparison.OrdinalIgnoreCase))
        {
            country = "";
        }
        DeviceInfoLog.LogCountry(
            $"给JAVA 传入的国家为 ： {country} —— ChannelConfig.real_CustomConfig.CountryName: {country}"
            );

        var temp = JavaBridgeUtils.CallStaticMethodCacheClass<string>(
            ClassName,
            GetCountryCodeMethod,
            country,
            SupportedCountryNames,
            defaultCountry);

        DeviceInfoLog.LogCountry(
            $"JAVA 传回的国家为 ： {temp} —— ChannelConfig.real_CustomConfig.CountryName: {country}"
            );
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "获取的 GetCountryCodeMethod 信息 ： " + temp);

        Debug.Log("[JavaBridgeUtilsCountry]:" + temp);

        return temp;
#else
        LogFallbackEnvironmentOnce(nameof(GetCountryCode));
        if (string.IsNullOrEmpty(country) ||
            string.Equals(country, "None", StringComparison.OrdinalIgnoreCase))
        {
            country = defaultCountry;
        }
        DeviceInfoLog.LogCountry($"编辑器下国家选择为 ： {country} —— ChannelConfig.real_CustomConfig.CountryName: {country}"
            );
        return country;
#endif
    }

    /* ===================== 渠道 ===================== */
    /// <summary>
    /// 来源渠道（如：googleplay / taptap / huawei）
    /// 通常来自：打包宏 / 配置文件 / 服务器
    /// </summary>
    public static string GetChannel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetChannel), "安卓真机");
        var temp = JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetChannelMethod);
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "获取的 GetChannelMethod 信息 ： " + temp);
        return temp;
#else
        LogFallbackEnvironmentOnce(nameof(GetChannel));
        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "走的测试路径 ： ");
        return "editor";
#endif
    }

    /// <summary>
    /// 获取当前注册网络的国家/地区代码，例如 CN、US。
    /// </summary>
    public static string GetNetworkCountryIso()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetNetworkCountryIso), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetNetworkCountryIsoMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetNetworkCountryIso));
        return string.Empty;
#endif
    }

    /// <summary>
    /// 获取系统 Locale，例如 zh_CN、en_US。
    /// </summary>
    public static string GetNativeLocale()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetNativeLocale), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetNativeLocaleMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetNativeLocale));
        return System.Globalization.CultureInfo.CurrentCulture.Name.Replace('-', '_');
#endif
    }

    /// <summary>
    /// 获取系统 Locale 的国家/地区代码，例如 CN、US。
    /// </summary>
    public static string GetNativeLocaleCountry()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetNativeLocaleCountry), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetNativeLocaleCountryMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetNativeLocaleCountry));
        try
        {
            return new System.Globalization.RegionInfo(System.Globalization.CultureInfo.CurrentCulture.Name)
                .TwoLetterISORegionName;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
#endif
    }

    /// <summary>
    /// 获取系统时区 ID，例如 Asia/Shanghai。
    /// </summary>
    public static string GetNativeTimeZoneId()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetNativeTimeZoneId), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetNativeTimeZoneIdMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetNativeTimeZoneId));
        return TimeZoneInfo.Local.Id;
#endif
    }

    /// <summary>
    /// 获取当前默认输入法 ID。
    /// </summary>
    public static string GetDefaultInputMethod()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetDefaultInputMethod), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetDefaultInputMethodMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetDefaultInputMethod));
        return string.Empty;
#endif
    }

    /// <summary>
    /// 获取已启用输入法 ID 列表，多个值由 | 分隔。
    /// </summary>
    public static string GetEnabledInputMethods()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetEnabledInputMethods), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetEnabledInputMethodsMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetEnabledInputMethods));
        return string.Empty;
#endif
    }

    /// <summary>
    /// 获取 SIM 卡国家/地区代码，例如 CN、US。
    /// </summary>
    public static string GetSimCountryIso()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetSimCountryIso), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetSimCountryIsoMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetSimCountryIso));
        return string.Empty;
#endif
    }

    /// <summary>
    /// 获取 SIM 运营商 MCC/MNC，例如 46000。
    /// </summary>
    public static string GetSimOperator()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetSimOperator), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetSimOperatorMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetSimOperator));
        return string.Empty;
#endif
    }

    /// <summary>
    /// 获取当前网络运营商 MCC/MNC，例如 46001。
    /// </summary>
    public static string GetNetworkOperator()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetNetworkOperator), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetNetworkOperatorMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetNetworkOperator));
        return string.Empty;
#endif
    }

    /// <summary>
    /// 获取当前网络运营商名称。
    /// </summary>
    public static string GetNetworkOperatorName()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(GetNetworkOperatorName), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<string>(ClassName, GetNetworkOperatorNameMethod) ?? string.Empty;
#else
        LogFallbackEnvironmentOnce(nameof(GetNetworkOperatorName));
        return string.Empty;
#endif
    }

    /// <summary>
    /// 当前设备是否连接 VPN。
    /// </summary>
    public static bool IsVpnConnected()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(IsVpnConnected), "安卓真机");
        return JavaBridgeUtils.CallStaticMethodCacheClass<bool>(ClassName, IsVpnConnectedMethod);
#else
        LogFallbackEnvironmentOnce(nameof(IsVpnConnected));
        return false;
#endif
    }

    /* ===================== 用户ID ===================== */
    /// <summary>
    /// 用户ID（登录成功后由业务层赋值）
    /// 这里仅兜底
    /// </summary>
    public static string GetUserId()
    {
        return "";
    }

    /// <summary>
    /// 获取 Android 软键盘高度（返回：屏幕像素高度 & Canvas高度）
    /// </summary>
    public static Dictionary<string, int> AndroidGetKeyboardHeight(int canvasHeight)
    {
        var dic = new Dictionary<string, int>();

#if UNITY_ANDROID && !UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(AndroidGetKeyboardHeight), "安卓真机");
        using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject activity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject player = activity.Get<AndroidJavaObject>("mUnityPlayer");
            AndroidJavaObject view = player.Call<AndroidJavaObject>("getView");

            using (AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect"))
            {
                view.Call("getWindowVisibleDisplayFrame", rect);
                int androidRectH = rect.Call<int>("height");           // 非键盘部分可见高度
                int h = Screen.height - androidRectH;                  // 键盘高度（屏幕像素）

                // 兼容 Unity 某些版本的输入 Dialog 高度
                try
                {
                    AndroidJavaObject dialog = player.Get<AndroidJavaObject>("mSoftInputDialog");
                    if (dialog != null && !TouchScreenKeyboard.hideInput)
                    {
                        AndroidJavaObject decorView = dialog.Call<AndroidJavaObject>("getWindow")
                                                           .Call<AndroidJavaObject>("getDecorView");
                        if (decorView != null)
                        {
                            int h1 = decorView.Call<int>("getHeight");
                            h += h1;
                            dic["dialogH"] = h1;
                        }
                    }
                }
                catch (Exception)
                {
                    dic["dialogH"] = -1;
                }

                dic["androidRectH"] = androidRectH;
                dic["keyboardInScreenHeight"] = Mathf.Max(0, h);

                int canvasH = (int)(dic["keyboardInScreenHeight"] * (float)canvasHeight / Screen.height);
                dic["keyboardInCanvasHeight"] = Mathf.Max(0, canvasH);
            }
        }
#else
        LogFallbackEnvironmentOnce(nameof(AndroidGetKeyboardHeight));
        // 编辑器/非安卓：返回 0
        dic["androidRectH"] = Screen.height;
        dic["keyboardInScreenHeight"] = 0;
        dic["keyboardInCanvasHeight"] = 0;
        dic["dialogH"] = 0;
#endif

        return dic;
    }
}
