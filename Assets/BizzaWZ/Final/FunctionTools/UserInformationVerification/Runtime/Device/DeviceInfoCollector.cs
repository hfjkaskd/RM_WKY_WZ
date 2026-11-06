using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Bizza.UserInformationVerification;
using LogLogger = Bizza.UserInformationVerification.AuthLog;

namespace Bizza.TokenClientSystem
{
    [Serializable]
    public sealed class AuthDeviceInfo
    {
        [JsonProperty("googleId")] public string googleId = string.Empty;
        [JsonProperty("androidId")] public string androidId = string.Empty;
        [JsonProperty("deviceUniqueIdentifier")] public string deviceUniqueIdentifier = string.Empty;
        [JsonProperty("packageName")] public string packageName = string.Empty;
        [JsonProperty("UnityLanguage")] public string UnityLanguage = string.Empty;
        [JsonProperty("UnityRegionCode")] public string UnityRegionCode = string.Empty;
        [JsonProperty("NativeLocale")] public string NativeLocale = string.Empty;
        [JsonProperty("NativeLocaleCountry")] public string NativeLocaleCountry = string.Empty;
        [JsonProperty("NativeCountryCode")] public string NativeCountryCode = string.Empty;
        [JsonProperty("UnityTimeZoneId")] public string UnityTimeZoneId = string.Empty;
        [JsonProperty("NativeTimeZoneId")] public string NativeTimeZoneId = string.Empty;
        [JsonProperty("SimCountryIso")] public string SimCountryIso = string.Empty;
        [JsonProperty("NetworkCountryIso")] public string NetworkCountryIso = string.Empty;
        [JsonProperty("SimOperator")] public string SimOperator = string.Empty;
        [JsonProperty("NetworkOperator")] public string NetworkOperator = string.Empty;
        [JsonProperty("NetworkOperatorName")] public string NetworkOperatorName = string.Empty;
        [JsonProperty("DefaultInputMethod")] public string DefaultInputMethod = string.Empty;
        [JsonProperty("EnabledInputMethods")] public string EnabledInputMethods = string.Empty;
        [JsonProperty("IsVpnConnected")] public bool IsVpnConnected;
    }

    /// <summary>
    /// 将公共 DeviceInfoUtil 的统一设备快照转换为 IP 服务端需要的 AuthDeviceInfo。
    /// </summary>
    public sealed class DeviceInfoCollector
    {
        public UniTask<AuthDeviceInfo> CollectAsync(
            CancellationToken cancellationToken = default)
        {
            LogLogger.LogIP("[设备信息采集] 开始采集设备信息");
            cancellationToken.ThrowIfCancellationRequested();

            DeviceInfoUtil.DeviceInfoData deviceInfo = DeviceInfoUtil.Current;
            if (!DeviceInfoUtil.IsInitialized || deviceInfo == null)
            {
                throw new InvalidOperationException(
                    "DeviceInfoUtil 尚未初始化，请先等待 DeviceInfoUtil.InitializeAsync。" );
            }

            AuthDeviceInfo info = CreateInfo(deviceInfo);

#if UNITY_EDITOR
            if (AuthConfig.UseEditorMockDeviceInfo)
            {
                ApplyEditorMockInfo(info);
                LogLogger.LogIP("[设备信息采集] 当前使用编辑器 AuthConfig 模拟设备信息");
            }
#endif

            LogLogger.LogIP(
                $"[设备信息采集] 设备信息采集完成：Google ID存在={!string.IsNullOrWhiteSpace(info.googleId)}，Android ID存在={!string.IsNullOrWhiteSpace(info.androidId)}，设备唯一标识存在={!string.IsNullOrWhiteSpace(info.deviceUniqueIdentifier)}，VPN连接={info.IsVpnConnected}");
            return UniTask.FromResult(info);
        }

#if UNITY_EDITOR
        private static void ApplyEditorMockInfo(AuthDeviceInfo target)
        {
            if (target == null)
            {
                return;
            }

            target.NativeLocale = AuthConfig.EditorNativeLocale;
            target.NativeLocaleCountry = AuthConfig.EditorNativeLocaleCountry;
            target.NativeCountryCode = AuthConfig.EditorNativeCountryCode;
            target.SimCountryIso = AuthConfig.EditorSimCountryIso;
            target.NetworkCountryIso = AuthConfig.EditorNetworkCountryIso;
            target.SimOperator = AuthConfig.EditorSimOperator;
            target.NetworkOperator = AuthConfig.EditorNetworkOperator;
            target.NetworkOperatorName = AuthConfig.EditorNetworkOperatorName;
            target.DefaultInputMethod = AuthConfig.EditorDefaultInputMethod;
            target.EnabledInputMethods = AuthConfig.EditorEnabledInputMethods;
            target.IsVpnConnected = AuthConfig.EditorIsVpnConnected;
        }
#endif

        public static string Serialize(AuthDeviceInfo info)
        {
            return JsonConvert.SerializeObject(info ?? new AuthDeviceInfo());
        }

        private static AuthDeviceInfo CreateInfo(DeviceInfoUtil.DeviceInfoData deviceInfo)
        {
            string unityTimeZoneId = FirstNonEmpty(
                deviceInfo.Os_Ttz,
                SafeGet(() => TimeZoneInfo.Local.Id, string.Empty));
            string unityRegionCode = FirstNonEmpty(
                deviceInfo.NativeLocaleCountry,
                deviceInfo.Os_Try,
                UserInformationVerificationRuntime.GetUnityRegionCode());

            return new AuthDeviceInfo
            {
                googleId = deviceInfo.Os_Gaid ?? string.Empty,
                androidId = deviceInfo.Os_Anid ?? string.Empty,
                deviceUniqueIdentifier = SafeGet(
                    () => SystemInfo.deviceUniqueIdentifier,
                    string.Empty),
                packageName = UserInformationVerificationRuntime.GetPackageName(),
                UnityLanguage = FirstNonEmpty(
                    deviceInfo.Os_Lag,
                    SafeGet(() => Application.systemLanguage.ToString(), string.Empty)),
                UnityRegionCode = unityRegionCode,
                NativeLocale = deviceInfo.NativeLocale ?? string.Empty,
                NativeLocaleCountry = deviceInfo.NativeLocaleCountry ?? string.Empty,
                NativeCountryCode = FirstNonEmpty(
                    deviceInfo.NetworkCountryIso,
                    deviceInfo.Os_Try),
                UnityTimeZoneId = unityTimeZoneId,
                NativeTimeZoneId = FirstNonEmpty(
                    deviceInfo.NativeTimeZoneId,
                    unityTimeZoneId),
                SimCountryIso = deviceInfo.SimCountryIso ?? string.Empty,
                NetworkCountryIso = deviceInfo.NetworkCountryIso ?? string.Empty,
                SimOperator = deviceInfo.SimOperator ?? string.Empty,
                NetworkOperator = deviceInfo.NetworkOperator ?? string.Empty,
                NetworkOperatorName = deviceInfo.NetworkOperatorName ?? string.Empty,
                DefaultInputMethod = deviceInfo.DefaultInputMethod ?? string.Empty,
                EnabledInputMethods = deviceInfo.EnabledInputMethods ?? string.Empty,
                IsVpnConnected = deviceInfo.IsVpnConnected
            };
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return string.Empty;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try
            {
                return getter == null ? fallback : getter();
            }
            catch (Exception exception)
            {
                LogLogger.LogIP("[设备信息采集] Unity 字段不可用：" + exception.Message);
                return fallback;
            }
        }
    }
}
