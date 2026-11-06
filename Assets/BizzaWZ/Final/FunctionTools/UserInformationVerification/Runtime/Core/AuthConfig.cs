using System;
using UnityEngine;
using Bizza.UserInformationVerification;

namespace Bizza.TokenClientSystem
{
    /// <summary>
    /// TokenClient 授权配置数据。
    ///
    /// 配置由 AuthConfigEditorWindow 编辑，序列化到：
    /// Assets/StreamingAssets/AuthConfig.bytes
    /// </summary>
    [Serializable]
    public sealed class AuthConfigData
    {
        internal const string DefaultWorkerUrl =
            "https://iptokenclient.applebooks24.com/api/v1/decision";

        [Tooltip("独立运行模式使用的 AppId。接入 SDK 时优先使用宿主或公共 DeviceInfoUtil 提供的值。")]
        public string AppId = string.Empty;

        [Tooltip("PlayerPrefs 中保存服务端授权 Token 使用的键名。")]
        public string AuthTokenKey = "AUTH_TOKEN";

        [Tooltip("Token 授权请求的服务器接口地址。")]
        public string WorkerUrl = DefaultWorkerUrl;

        public int RequestTimeoutSeconds = 15;

        [Tooltip("开启后会保留并打印服务端返回的 debugInfo。正式包建议关闭。")]
        public bool EnableDebugResponse;

        [Tooltip("仅 Unity Editor 使用；真机仍然采集 Android 原生设备信息。")]
        public bool UseEditorMockDeviceInfo = true;

        public string EditorNativeLocale = "en_US";

        public string EditorNativeLocaleCountry = "US";

        public string EditorNativeCountryCode = "US";

        public string EditorSimCountryIso = "us";

        public string EditorNetworkCountryIso = "us";

        public string EditorSimOperator = "310260";

        public string EditorNetworkOperator = "310260";

        public string EditorNetworkOperatorName = "T-Mobile";

        public string EditorDefaultInputMethod = string.Empty;

        public string EditorEnabledInputMethods = string.Empty;

        public bool EditorIsVpnConnected;

        public static AuthConfigData CreateDefault()
        {
            return new AuthConfigData();
        }
    }

    /// <summary>
    /// TokenClient 授权配置的运行时访问门面。
    /// 保留原有静态调用方式，具体值来自 AuthConfig.bytes。
    /// </summary>
    public static class AuthConfig
    {
        private static AuthConfigData runtimeConfig = AuthConfigData.CreateDefault();
        private static string runtimeAppIdOverride = string.Empty;

        /// <summary>
        /// 当前运行时配置。没有找到 AuthConfig.bytes 时使用代码默认值。
        /// </summary>
        public static AuthConfigData Current => runtimeConfig;

        // PlayerPrefs 中保存授权 Token 时使用的键名
        public static string AuthTokenKey => Current.AuthTokenKey;

        // 当前应用/渠道的唯一 ID。集成模式由宿主或公共 DeviceInfoUtil 提供，
        // 独立模式或宿主未提供时使用 AuthConfig.bytes。
        public static string AppId
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(runtimeAppIdOverride))
                {
                    return runtimeAppIdOverride;
                }

                string hostAppId = UserInformationVerificationRuntime.GetAppId();
                return string.IsNullOrWhiteSpace(hostAppId)
                    ? Current.AppId ?? string.Empty
                    : hostAppId;
            }
        }

        // Token 授权请求的服务器接口地址
        public static string WorkerUrl => Current.WorkerUrl;

        // 请求超时时间，单位：秒
        public static int RequestTimeoutSeconds => Current.RequestTimeoutSeconds;

        // 是否保留服务器返回的调试信息，false 表示不保留
        public static bool EnableDebugResponse => Current.EnableDebugResponse;

#if UNITY_EDITOR
        // Editor-only test values. Device builds continue to use the Android Java path.
        // 是否在 Unity 编辑器中使用下面这组模拟设备信息；false 时改走设备信息采集路径。
        public static bool UseEditorMockDeviceInfo => Current.UseEditorMockDeviceInfo;
        // 系统语言区域，格式通常为“语言_国家/地区”，例如 en_US、zh_CN。
        public static string EditorNativeLocale => Current.EditorNativeLocale; // 当前会触发拦截的中国内容：zh_CN
        // 系统语言区域对应的国家/地区代码，例如 US、CN。
        public static string EditorNativeLocaleCountry => Current.EditorNativeLocaleCountry; // 当前会触发拦截的中国内容：CN
        // 原生设备上报的国家/地区代码，用于服务端地区判断。
        public static string EditorNativeCountryCode => Current.EditorNativeCountryCode; // 当前会触发拦截的中国内容：CN
        // SIM 卡所属国家/地区的 ISO 代码，例如 us、cn。
        public static string EditorSimCountryIso => Current.EditorSimCountryIso; // 当前会触发拦截的中国内容：cn
        // 当前移动网络所属国家/地区的 ISO 代码，例如 us、cn。
        public static string EditorNetworkCountryIso => Current.EditorNetworkCountryIso; // 当前会触发拦截的中国内容：cn
        // SIM 卡运营商的 MCC/MNC 编码，例如 310260、46000。
        public static string EditorSimOperator => Current.EditorSimOperator; // 当前会触发拦截的中国内容：46000
        // 当前注册网络运营商的 MCC/MNC 编码，例如 310260、46000。
        public static string EditorNetworkOperator => Current.EditorNetworkOperator; // 当前会触发拦截的中国内容：46000
        // 当前网络运营商名称，服务端会检查其中的地区关键词。
        public static string EditorNetworkOperatorName => Current.EditorNetworkOperatorName; // 当前会触发拦截的中国内容：China Mobile
        // 当前默认输入法的标识或名称；留空表示编辑器不模拟具体输入法。
        public static string EditorDefaultInputMethod => Current.EditorDefaultInputMethod;
        // 当前已启用输入法的列表；多个输入法通常用分隔符连接，留空表示不模拟。
        public static string EditorEnabledInputMethods => Current.EditorEnabledInputMethods;
        // 是否模拟设备正在使用 VPN；true 会触发服务端的 VPN 检查。
        public static bool EditorIsVpnConnected => Current.EditorIsVpnConnected;
#endif

        // Keep this public because the Unity EditorWindow can be compiled into
        // a separate editor assembly from the runtime configuration facade.
        public static void Apply(AuthConfigData config)
        {
            runtimeConfig = config ?? AuthConfigData.CreateDefault();
            runtimeConfig.AppId = Normalize(runtimeConfig.AppId, string.Empty);
            runtimeConfig.AuthTokenKey = Normalize(runtimeConfig.AuthTokenKey, "AUTH_TOKEN");
            runtimeConfig.WorkerUrl = Normalize(
                runtimeConfig.WorkerUrl,
                AuthConfigData.DefaultWorkerUrl);
            runtimeConfig.RequestTimeoutSeconds = Math.Max(1, runtimeConfig.RequestTimeoutSeconds);
        }

        public static void ResetToDefaults()
        {
            Apply(AuthConfigData.CreateDefault());
        }

        /// <summary>
        /// 保存本次授权流程解析出的有效 AppId，流程结束后立即清理。
        /// </summary>
        public static void SetAppIdOverride(string appId)
        {
            runtimeAppIdOverride = Normalize(appId, string.Empty);
        }

        public static void ClearAppIdOverride()
        {
            runtimeAppIdOverride = string.Empty;
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
        }
    }
}
