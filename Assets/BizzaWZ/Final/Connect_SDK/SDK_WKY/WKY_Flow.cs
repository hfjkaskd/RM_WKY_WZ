using System;
using System.Threading;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class WKY_Flow
{
    private const string CountryWorkerUrl =
        "https://iptokenclient.applebooks24.com/api/v1/rm-wky-wz/region";
    private const string CountryWorkerPolicy = "rm-wky-wz-v1";
    private const int CountryCheckAttempts = 2;
    private const string CountryDeniedPrefsKey = "WKY_CountryDenied";
    private enum CountryAccess { Unavailable, Allowed, Denied }
    private static CountryAccess countryAccess;

    internal static bool IsCountryAccessAllowed => countryAccess == CountryAccess.Allowed;
    internal static bool IsAccessDenied => countryAccess == CountryAccess.Denied;

    public async UniTask Start_Init_Flow(Action<float> onProgress)
    {
        onProgress?.Invoke(0f);
        await Init(onProgress);
        onProgress?.Invoke(1f);
    }

    private async UniTask Init(Action<float> onProgress)
    {
        Bizza.Unity.Android.AndroidJavaMessageDispatcher.SetHttpMessageHandler(
            HttpUtil.LoginHTTPDict);
        onProgress?.Invoke(0.05f);
        var cancellation = Application.exitCancellationToken;
        var initialAccess = await VerifyCountryAccessAsync(cancellation);
        if (initialAccess == CountryAccess.Denied ||
            (initialAccess == CountryAccess.Unavailable && PlayerPrefs.GetInt(CountryDeniedPrefsKey, 0) == 1))
        {
            countryAccess = CountryAccess.Denied;
            await ShowCountryRestrictionAsync();
            throw new WKYInitializationException("CountryRestricted", "Country is not enabled for this app.");
        }
        if (!await VerifyVpnAccessAsync())
            throw new Bizza.TokenClientSystem.TokenClientBlockedException("VPN");
        countryAccess = initialAccess;

        ChannelConfig config = await ChannelConfigLoader.LoadAsync();
        if (config == null)
        {
            const string message = "ChannelConfig 加载失败，SDK 初始化终止。";
            Debug.LogError(message);
            throw new WKYInitializationException(
                "ChannelConfigLoadFailed",
                message);
        }
        onProgress?.Invoke(0.2f);

        LogLogger.LogInfo("State_ChannelConfig: ", $"<color=red>----------------------------</color>");
#if DEBUG_MODE
        LogLogger.LogAdInfo($"{config.GetAllParamsString()}");
#endif

        var customDeviceInfo = new DeviceInfoUtil.CustomDeviceInfo
        {
            appId = config.AppId,
            country = config.real_CustomConfig.CountryName,
            defaultCountry = config.real_CustomConfig.DefaultCountryName,
            fakeAndroidId = config.real_CustomConfig.GenerateFakeAndroidId
        };
        await DeviceInfoUtil.InitializeAsync(customDeviceInfo, cancellation);
        onProgress?.Invoke(0.45f);

        var deciceInfo = DeviceInfoUtil.Data;
        LogLogger.LogInfo("State_deviceInfo: ", $"<color=red>----------------------------</color>");
#if DEBUG_MODE
        {
            string _info = JsonUtility.ToJson(deciceInfo);
            Debug.Log(_info);
        }
#endif

        LogLogger.LogInfo("State_analyticsAgent: ", $"<color=red>----------------------------</color>");

        AccountModule account = new AccountModule();
        account.Login();
        onProgress?.Invoke(0.5f);
        await UniTask.WaitUntil(() => account.StartupResultAvailable, cancellationToken: cancellation);
        if (account.IsSuspended)
            throw new WKYInitializationException(AccountModule.SuspendedCode, "WKY account suspended.");

        Bizza.UserInformationVerification.ScreenCaptureProtection.TryProtect();
        if (!account.AccountReady)
        {
            Debug.LogWarning("WKY user data unavailable, starting basic gameplay while retrying.");
            account.LoginSucceeded += OnLoginSucceeded;
            return;
        }

        onProgress?.Invoke(0.9f);
        LogLogger.LogInfo("State_deviceInfo2: ", $"<color=red>----------------------------</color>");
#if DEBUG_MODE
        {
            string _info = JsonUtility.ToJson(deciceInfo);
            Debug.Log(_info);
        }
#endif
        LogLogger.LogInfo("State_AccountInfo: ", $"{account.Os_Current_Uso?.GetInfo()}");
        InitializePlatform();

        void OnLoginSucceeded()
        {
            account.LoginSucceeded -= OnLoginSucceeded;
            InitializePlatform();
        }

        void InitializePlatform()
        {
            if (!account.AccountReady || !ReferenceEquals(AccountModule.Instance, account)) return;
            if (!OverseaPlatform.IsInit) new OverseaPlatform(config);
            BizzaSdk.Ad.SetUserId(PlayerPrefs.GetString(AccountModule.m_userIdKey));
        }
    }

    internal static async UniTask<bool> CanStartAccountRequestsAsync(CancellationToken cancellationToken)
    {
        if (countryAccess == CountryAccess.Denied) return false;
        if (countryAccess == CountryAccess.Allowed) return true;

        var access = await VerifyCountryAccessAsync(cancellationToken);
        if (access == CountryAccess.Denied)
        {
            countryAccess = CountryAccess.Denied;
            await ShowCountryRestrictionAsync();
            return false;
        }

        if (!await VerifyVpnAccessAsync()) return false;
        countryAccess = access;
        return IsCountryAccessAllowed;
    }

    private static async UniTask<bool> VerifyVpnAccessAsync()
    {
        bool connected = DeviceNativeBridge.IsVpnConnected();
        DeviceInfoUtil.UpdateVpnStatus(connected);
        if (!connected) return true;

        countryAccess = CountryAccess.Denied;
        await SDKAssetHandler.OpenCommonConfirmTipsPanel(new CommonConfirmTipsPanel.Args
        {
            isLanguage = true,
            persistAcrossScenes = true,
            des = "TokenClient_VPN",
            onFinish = Application.Quit
        });
        return false;
    }

    private static async UniTask ShowCountryRestrictionAsync()
    {
        await SDKAssetHandler.OpenCommonConfirmTipsPanel(new CommonConfirmTipsPanel.Args
        {
            isLanguage = true,
            persistAcrossScenes = true,
            des = "TokenClient_REGION",
            onFinish = Application.Quit
        });
    }

    private static async UniTask<CountryAccess> VerifyCountryAccessAsync(CancellationToken cancellationToken)
    {
        DeviceNativeBridge.ApplyVpnDetectionPolicy(null);
        if (Application.internetReachability == NetworkReachability.NotReachable) return CountryAccess.Unavailable;

        for (int attempt = 1; attempt <= CountryCheckAttempts; attempt++)
        {
            using var request = UnityWebRequest.Get(CountryWorkerUrl);
            request.timeout = 8;
            request.redirectLimit = 0;

            try
            {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            }
            catch (UnityWebRequestException)
            {
                // Unity reports a country denial as a protocol error.
            }

            long status = request.responseCode;
            bool policyConfirmed = string.Equals(
                request.GetResponseHeader("X-Game-Region-Policy"),
                CountryWorkerPolicy,
                StringComparison.Ordinal);
            Debug.Log($"[CountryAccess] attempt={attempt}/{CountryCheckAttempts} status={status} " +
                      $"result={request.result} policyConfirmed={policyConfirmed}");

            if (!policyConfirmed) continue;
            if (status == 204 && request.result == UnityWebRequest.Result.Success)
            {
                DeviceNativeBridge.ApplyVpnDetectionPolicy(request.GetResponseHeader("X-Game-Vpn-Detection"));
                Debug.Log($"[VpnAccess] detectionEnabled={DeviceNativeBridge.IsVpnDetectionEnabled}");
                if (PlayerPrefs.HasKey(CountryDeniedPrefsKey))
                {
                    PlayerPrefs.DeleteKey(CountryDeniedPrefsKey);
                    PlayerPrefs.Save();
                }
                return CountryAccess.Allowed;
            }
            if (status != 403) continue;

            PlayerPrefs.SetInt(CountryDeniedPrefsKey, 1);
            PlayerPrefs.Save();
            return CountryAccess.Denied;
        }

        return CountryAccess.Unavailable;
    }

}

public sealed class WKYInitializationException : Exception
{
    public string ErrorCode { get; }

    public WKYInitializationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = string.IsNullOrWhiteSpace(errorCode)
            ? "WKYInitializationFailed"
            : errorCode;
    }
}
