using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Bizza.Unity.Android;

/// <summary>
/// 设备信息收集： 负责收集并提供所有设备信息
/// </summary>
public static class DeviceInfoUtil
{
    private const string NativeInfoPlayerPrefsKey = "DeviceInfoUtil.NativeDeviceInfo";
    private const string AdjustGoogleIdPlayerPrefsKey = "adJustGoogleIdKey";
    private const int NativeInfoCacheVersion = 4;
    private const long NativeInfoCacheMaxAgeSeconds = 300;
    private const int GoogleIdTimeoutMilliseconds = 10000;

    public static string m_googleIdKey = "googleId"; // Google Advertising ID 持久化键
    public static string m_seidKey = "seidId"; // 安装级客户端 ID 持久化键
    public static string m_appidKey = "appidId"; // 应用级客户端 ID 持久化键

    private static DeviceNativeInfoData cachedDeviceInfo;
    private static UniTaskCompletionSource<string> googleIdCompletionSource;
    private static UniTaskCompletionSource<DeviceInfoData> deviceInfoCompletionSource;
    private static CustomDeviceInfo injectedDeviceInfo;
    private static bool initializationCompleted;

    /// <summary>
    /// 设备采集完成后的统一快照。加载流程应先等待 InitializeAsync，再读取此属性。
    /// </summary>
    public static DeviceInfoData Current => cachedDeviceInfo;

    /// <summary>
    /// 兼容现有 SDK 调用。初始化完成后始终返回同一份公共设备快照。
    /// </summary>
    public static DeviceInfoData Data => GetDeviceInfoDataForCloud();

    public static bool IsInitialized => initializationCompleted && cachedDeviceInfo != null;

    private static DeviceNativeInfoData GetNativeDeviceInfo(bool forceRefresh = false)
    {
        EnsureDeviceInfoInjected();

        if (!forceRefresh && cachedDeviceInfo != null)
        {
            return cachedDeviceInfo;
        }

        if (!forceRefresh &&
            TryLoadNativeDeviceInfo(out var savedData) &&
            IsNativeInfoCacheFresh(savedData) &&
            IsInjectedConfigurationMatch(savedData))
        {
            savedData.Os_Apid = NormalizeAppId(injectedDeviceInfo.appId);
            cachedDeviceInfo = savedData;
            return cachedDeviceInfo;
        }

        return RefreshNativeDeviceInfo();
    }

    public static UniTask<DeviceInfoData> InitializeAsync(
        string _appId = "",
        string _country= "",
        string _defaultCountry= "",
        string _fakeAndroidId= "",
        CancellationToken cancellationToken = default)
    {
        var info = new CustomDeviceInfo
        {
            appId = _appId,
            country = _country,
            defaultCountry = _defaultCountry,
            fakeAndroidId = _fakeAndroidId
        };
        return InitializeAsync(info, cancellationToken);
    }

    /// <summary>
    /// 统一完成原生设备信息、云端设备信息和 Google ID 的采集。
    /// 多个模块并发调用时只执行一次，取消只影响当前调用方。
    /// </summary>
    /// <param name="customDeviceInfo">首次调用方注入的设备采集配置；后续调用不会覆盖。</param>
    /// <param name="cancellationToken">当前调用方的取消令牌。</param>
    public static UniTask<DeviceInfoData> InitializeAsync(
        CustomDeviceInfo customDeviceInfo,
        CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return UniTask.FromResult<DeviceInfoData>(cachedDeviceInfo);
        }

        if (deviceInfoCompletionSource == null)
        {
            injectedDeviceInfo = CloneDeviceInfo(customDeviceInfo);
            initializationCompleted = false;
            deviceInfoCompletionSource = new UniTaskCompletionSource<DeviceInfoData>();
            CollectDeviceInfoAsync().Forget();
        }

        UniTask<DeviceInfoData> task = deviceInfoCompletionSource.Task;
        return cancellationToken.CanBeCanceled
            ? task.AttachExternalCancellation(cancellationToken)
            : task;
    }

    private static async UniTask CollectDeviceInfoAsync()
    {
        try
        {
            DeviceNativeInfoData deviceInfo = GetNativeDeviceInfo();
            deviceInfo.Os_Gaid = await GetGoogleIdAsync();
            cachedDeviceInfo = deviceInfo;
            initializationCompleted = true;
            deviceInfoCompletionSource?.TrySetResult(deviceInfo);
        }
        catch (Exception exception)
        {
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "统一设备信息采集失败：" + exception.Message);
            deviceInfoCompletionSource?.TrySetException(exception);
        }
    }

    /// <summary>
    /// 重新调用原生接口采集设备信息，并同步更新内存及 PlayerPrefs 缓存。
    /// 网络、运营商、VPN 等状态发生变化后可以调用此方法刷新。
    /// </summary>
    public static DeviceNativeInfoData RefreshNativeDeviceInfo()
    {
        EnsureDeviceInfoInjected();
        cachedDeviceInfo = BuildDeviceInfo(injectedDeviceInfo);
        SaveNativeDeviceInfo(cachedDeviceInfo);
        return cachedDeviceInfo;
    }

    /// <summary>
    /// 清除原生设备信息的内存及 PlayerPrefs 缓存。
    /// 下次访问 NativeDeviceInfo 时会重新采集。
    /// </summary>
    public static void ClearNativeDeviceInfoCache()
    {
        cachedDeviceInfo = null;
        injectedDeviceInfo = null;
        initializationCompleted = false;
        deviceInfoCompletionSource = null;
        PlayerPrefs.DeleteKey(NativeInfoPlayerPrefsKey);
        PlayerPrefs.Save();
    }

    private static bool TryLoadNativeDeviceInfo(out DeviceNativeInfoData data)
    {
        data = null;

        if (!PlayerPrefs.HasKey(NativeInfoPlayerPrefsKey))
        {
            return false;
        }

        try
        {
            string json = PlayerPrefs.GetString(NativeInfoPlayerPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            data = JsonUtility.FromJson<DeviceNativeInfoData>(json);
            return data != null && data.CacheVersion == NativeInfoCacheVersion;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取原生设备信息缓存失败，将重新采集：{exception.Message}");
            data = null;
            return false;
        }
    }

    private static void SaveNativeDeviceInfo(DeviceNativeInfoData data)
    {
        if (data == null)
        {
            return;
        }

        try
        {
            PlayerPrefs.SetString(NativeInfoPlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"保存原生设备信息缓存失败：{exception.Message}");
        }
    }

    private static bool IsNativeInfoCacheFresh(DeviceNativeInfoData data)
    {
        if (data == null || data.CacheVersion != NativeInfoCacheVersion)
        {
            return false;
        }

        long age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.CollectedAtUnixTimeSeconds;
        return age >= 0 && age <= NativeInfoCacheMaxAgeSeconds;
    }

    /// <summary>
    /// 获取当前会话可用的 Google Advertising ID。
    /// 多个模块并发调用时只触发一次原生请求，结果写回共享缓存。
    /// </summary>
    public static UniTask<string> GetGoogleIdAsync()
    {
        string cachedGoogleId = GetCachedGoogleId();
        if (!string.IsNullOrEmpty(cachedGoogleId))
        {
            UpdateCloudGoogleId(cachedGoogleId);
            return UniTask.FromResult(cachedGoogleId);
        }

        if (googleIdCompletionSource == null)
        {
            googleIdCompletionSource = new UniTaskCompletionSource<string>();
            ResolveGoogleIdAsync().Forget();
        }

        return googleIdCompletionSource.Task;
    }

    /// <summary>
    /// 带调用方取消的 Google Advertising ID 获取。
    /// 取消只影响当前调用，不会取消其它模块共享的原生请求。
    /// </summary>
    public static UniTask<string> GetGoogleIdAsync(CancellationToken cancellationToken)
    {
        UniTask<string> task = GetGoogleIdAsync();
        return cancellationToken.CanBeCanceled
            ? task.AttachExternalCancellation(cancellationToken)
            : task;
    }

    public static string GetCachedGoogleId()
    {
        string googleId = NormalizeGoogleId(PlayerPrefs.GetString(m_googleIdKey, string.Empty));
        if (!string.IsNullOrEmpty(googleId))
        {
            return googleId;
        }

        // Adjust 可能在本类超时后才回调，读取其兼容键并回填统一缓存，避免本次进程永久丢失 GAID。
        googleId = NormalizeGoogleId(PlayerPrefs.GetString(AdjustGoogleIdPlayerPrefsKey, string.Empty));
        if (!string.IsNullOrEmpty(googleId))
        {
            PlayerPrefs.SetString(m_googleIdKey, googleId);
            PlayerPrefs.Save();
        }

        return googleId;
    }

    private static async UniTask ResolveGoogleIdAsync()
    {
        string googleId = string.Empty;
        bool callbackReceived = false;
        bool subscribed = false;

        void OnGoogleId(string value)
        {
            googleId = NormalizeGoogleId(value);
            callbackReceived = true;
        }

        try
        {
            AndroidJavaMessageDispatcher.EnsureCreated();
            BizzaEventSystem.On(FrameEvent.Android.UnityJavaObjectGoogleMessage, OnGoogleId);
            subscribed = true;

            bool nativeRequestStarted = true;
            try
            {
                DeviceNativeBridge.GetGoogleId();
            }
            catch (Exception exception)
            {
                nativeRequestStarted = false;
                DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "Google Advertising ID 原生调用失败：" + exception.Message);
            }

            if (nativeRequestStarted)
            {
                using (var timeoutCts = new CancellationTokenSource(
                           TimeSpan.FromMilliseconds(GoogleIdTimeoutMilliseconds)))
                {
                    bool cancelled = await UniTask.WaitUntil(
                        () => callbackReceived,
                        cancellationToken: timeoutCts.Token
                    ).SuppressCancellationThrow();

                    if (cancelled)
                    {
                        DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "Google Advertising ID 获取超时");
                    }
                }
            }
        }
        catch (Exception exception)
        {
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "Google Advertising ID 获取失败：" + exception.Message);
        }
        finally
        {
            if (subscribed)
            {
                BizzaEventSystem.Off(FrameEvent.Android.UnityJavaObjectGoogleMessage, OnGoogleId);
            }

            if (string.IsNullOrEmpty(googleId))
            {
                googleId = NormalizeGoogleId(
                    PlayerPrefs.GetString(AdjustGoogleIdPlayerPrefsKey, string.Empty));
            }

            if (!string.IsNullOrEmpty(googleId))
            {
                PlayerPrefs.SetString(m_googleIdKey, googleId);
                PlayerPrefs.Save();
            }

            UpdateCloudGoogleId(googleId);
            googleIdCompletionSource?.TrySetResult(googleId);
        }
    }

    private static string NormalizeGoogleId(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        if (Guid.TryParse(normalized, out Guid guid) && guid == Guid.Empty)
        {
            return string.Empty;
        }

        return normalized;
    }

    private static void UpdateCloudGoogleId(string googleId)
    {
        if (cachedDeviceInfo != null)
        {
            cachedDeviceInfo.Os_Gaid = googleId ?? string.Empty;
        }
    }

    private static string GetOrCreateInstallationId()
    {
        string installationId = PlayerPrefs.GetString(m_seidKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(installationId))
        {
            return installationId;
        }

        installationId = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(m_seidKey, installationId);
        PlayerPrefs.Save();
        return installationId;
    }

    #region 统一设备信息

    [Serializable]
    public class DeviceInfoData
    {
        // Native cache metadata and runtime-only fields.
        public int CacheVersion;
        public long CollectedAtUnixTimeSeconds;
        public string NetworkCountryIso;
        public string NativeLocale;
        public string NativeLocaleCountry;
        public string NativeTimeZoneId;
        public string DefaultInputMethod;
        public string EnabledInputMethods;
        public string SimCountryIso;
        public string SimOperator;
        public string NetworkOperator;
        public string NetworkOperatorName;
        public bool IsVpnConnected;

        // Server request fields.
        public string Os_Anid; // 安卓ID
        public string Os_Apid; // 应用ID
        public int Os_Atd; // 调试模式
        public string Os_Bbd; // 手机品牌
        public string Os_Cal; // 来源
        public int Os_Ctr; // 当前小时值
        public string Os_Dtd; // 屏幕密度
        public int Os_Dth; // 屏幕高
        public int Os_Dtw; // 屏幕宽
        public string Os_Gaid; // GoogleId
        public string Os_Lag; // 语言
        public string Os_Mbl; // 设备型号
        public string Os_Nbt; // 网络
        public string Os_Obv; // 系统版本
        public int Os_Rtt; // 是否root
        public int Os_Sbi; // SIM卡
        public string Os_Seid; // 客户端UUID
        public string Os_Try; // 国家码
        public string Os_Ttz; // 时区
        public string Os_Usid; // 用户ID
        public string Os_Cpu; // CPU架构
        public string Os_Ua; // 客户端
        public int Os_Vc; // App版本号
        public string Os_Vn; // App版本名
    }

    /// <summary>
    /// 兼容旧字段名，实际与 Current/Data 指向同一份缓存。
    /// </summary>
    public static DeviceInfoData deviceInfoDataForCloud => cachedDeviceInfo;

    public static DeviceInfoData GetDeviceInfoDataForCloud()
    {
        EnsureInitialized();

        // 这些字段可能在进程运行期间变化，不能只在首次构造时固定。
        cachedDeviceInfo.Os_Ctr = DateTime.Now.Hour;
        cachedDeviceInfo.Os_Seid = GetOrCreateInstallationId();
        string cachedGoogleId = GetCachedGoogleId();
        if (!string.IsNullOrEmpty(cachedGoogleId))
        {
            cachedDeviceInfo.Os_Gaid = cachedGoogleId;
        }

        return cachedDeviceInfo;
    }

    [Serializable]
    public sealed class CustomDeviceInfo
    {
        public string country = string.Empty;
        public string defaultCountry = string.Empty;
        public string appId = string.Empty;
        public string fakeAndroidId = string.Empty;
    }

    private static DeviceNativeInfoData BuildDeviceInfo(CustomDeviceInfo customDeviceInfo)
    {
        customDeviceInfo = customDeviceInfo ?? new CustomDeviceInfo();

        return new DeviceNativeInfoData
        {
            CacheVersion = NativeInfoCacheVersion,
            CollectedAtUnixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            InjectedCountry = NormalizeConfigurationValue(customDeviceInfo.country),
            InjectedDefaultCountry = NormalizeConfigurationValue(customDeviceInfo.defaultCountry),
            InjectedFakeAndroidId = NormalizeConfigurationValue(customDeviceInfo.fakeAndroidId),

            // Native runtime fields.
            NetworkCountryIso = SafeGet(DeviceNativeBridge.GetNetworkCountryIso, string.Empty),
            NativeLocale = SafeGet(DeviceNativeBridge.GetNativeLocale, string.Empty),
            NativeLocaleCountry = SafeGet(DeviceNativeBridge.GetNativeLocaleCountry, string.Empty),
            NativeTimeZoneId = SafeGet(DeviceNativeBridge.GetNativeTimeZoneId, string.Empty),
            DefaultInputMethod = SafeGet(DeviceNativeBridge.GetDefaultInputMethod, string.Empty),
            EnabledInputMethods = SafeGet(DeviceNativeBridge.GetEnabledInputMethods, string.Empty),
            SimCountryIso = SafeGet(DeviceNativeBridge.GetSimCountryIso, string.Empty),
            SimOperator = SafeGet(DeviceNativeBridge.GetSimOperator, string.Empty),
            NetworkOperator = SafeGet(DeviceNativeBridge.GetNetworkOperator, string.Empty),
            NetworkOperatorName = SafeGet(DeviceNativeBridge.GetNetworkOperatorName, string.Empty),
            IsVpnConnected = SafeGet(DeviceNativeBridge.IsVpnConnected, false),

            // Unity 可直接获取
            Os_Apid = NormalizeAppId(customDeviceInfo.appId),
            Os_Atd = Debug.isDebugBuild ? 1 : 0,
            Os_Cpu = SafeGet(() => SystemInfo.processorType, string.Empty),
            Os_Ctr = DateTime.Now.Hour,
            Os_Dtd = SafeGet(() => Screen.dpi.ToString(), string.Empty),
            Os_Dth = SafeGet(() => Screen.height, 0),
            Os_Dtw = SafeGet(() => Screen.width, 0),
            Os_Lag = string.Empty,
            Os_Seid = GetOrCreateInstallationId(),
            Os_Ttz = SafeGet(() => TimeZoneInfo.Local.Id, string.Empty),
            Os_Ua = "Unity-Android",
            Os_Vc = ParseVersionCode(Application.version),
            Os_Vn = Application.version,

            // 需要原生获取的字段
            Os_Anid = SafeGet(
                () => DeviceNativeBridge.GetAndroidId(customDeviceInfo.fakeAndroidId),
                string.Empty),
            Os_Bbd = SafeGet(DeviceNativeBridge.GetBrand, string.Empty),
            Os_Mbl = SafeGet(DeviceNativeBridge.GetModel, string.Empty),
            Os_Obv = SafeGet(DeviceNativeBridge.GetOSVersion, string.Empty),
            Os_Gaid = GetCachedGoogleId(),
            Os_Nbt = SafeGet(DeviceNativeBridge.GetNetworkType, string.Empty),
            Os_Rtt = SafeGet(DeviceNativeBridge.IsRooted, 0),
            Os_Sbi = SafeGet(DeviceNativeBridge.HasSimCard, 0),
            Os_Try = SafeGet(
                () => DeviceNativeBridge.GetCountryCode(
                    customDeviceInfo.country,
                    customDeviceInfo.defaultCountry),
                string.Empty
            ),
            Os_Cal = SafeGet(DeviceNativeBridge.GetChannel, string.Empty),
            Os_Usid = SafeGet(DeviceNativeBridge.GetUserId, string.Empty)
        };
    }

    private static T SafeGet<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter == null ? fallback : getter();
        }
        catch (Exception exception)
        {
            DeviceInfoLog.LogVerbose(DeviceInfoLogTag.Device, "设备信息字段获取失败：" + exception.Message);
            return fallback;
        }
    }

    private static string NormalizeAppId(string appId)
    {
        return (appId ?? string.Empty).Trim();
    }

    private static string NormalizeConfigurationValue(string value)
    {
        return (value ?? string.Empty).Trim();
    }

    private static CustomDeviceInfo CloneDeviceInfo(CustomDeviceInfo source)
    {
        source = source ?? new CustomDeviceInfo();
        return new CustomDeviceInfo
        {
            appId = NormalizeAppId(source.appId),
            country = NormalizeConfigurationValue(source.country),
            defaultCountry = NormalizeConfigurationValue(source.defaultCountry),
            fakeAndroidId = NormalizeConfigurationValue(source.fakeAndroidId)
        };
    }

    private static bool IsInjectedConfigurationMatch(DeviceNativeInfoData cached)
    {
        return cached != null &&
               string.Equals(
                   cached.InjectedCountry,
                   NormalizeConfigurationValue(injectedDeviceInfo.country),
                   StringComparison.Ordinal) &&
               string.Equals(
                   cached.InjectedDefaultCountry,
                   NormalizeConfigurationValue(injectedDeviceInfo.defaultCountry),
                   StringComparison.Ordinal) &&
               string.Equals(
                   cached.InjectedFakeAndroidId,
                   NormalizeConfigurationValue(injectedDeviceInfo.fakeAndroidId),
                   StringComparison.Ordinal);
    }

    private static void EnsureDeviceInfoInjected()
    {
        if (injectedDeviceInfo == null)
        {
            throw new InvalidOperationException(
                "DeviceInfoUtil 尚未注入配置，请先调用 InitializeAsync。");
        }
    }

    private static void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "DeviceInfoUtil 尚未初始化完成，请先等待 InitializeAsync。");
        }
    }

    private static int ParseVersionCode(string version)
    {
        // 兼容 1.2.3-beta，解析失败时返回 0，不阻断登录流程。
        if (string.IsNullOrWhiteSpace(version))
        {
            return 0;
        }

        string normalized = version.Split('-', '+')[0];
        string[] parts = normalized.Split('.');
        int major = ParseVersionPart(parts, 0);
        int minor = ParseVersionPart(parts, 1);
        int patch = ParseVersionPart(parts, 2);
        long result = major * 100L + minor * 10L + patch;
        return result > int.MaxValue ? int.MaxValue : (int)result;
    }

    private static int ParseVersionPart(string[] parts, int index)
    {
        if (parts == null || index >= parts.Length || !int.TryParse(parts[index], out int value))
        {
            return 0;
        }

        return Math.Max(0, value);
    }
    #endregion
}
/// <summary>
/// 兼容旧调用方的设备信息类型别名。
/// 完整设备数据现在统一由 DeviceInfoUtil.DeviceInfoData 承载。
/// </summary>
[Serializable]
public class DeviceNativeInfoData : DeviceInfoUtil.DeviceInfoData
{
    public string InjectedCountry = string.Empty;
    public string InjectedDefaultCountry = string.Empty;
    public string InjectedFakeAndroidId = string.Empty;
}
