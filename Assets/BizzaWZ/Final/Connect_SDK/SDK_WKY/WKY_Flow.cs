using System;
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
        await VerifyCountryAccessAsync();
#if USER_VERIFICATION_SDK
        // Fetch the remote switch before the local VPN check and account login.
        await Boots_Flow.VerifyVpnAccessAsync();
#endif

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
        await DeviceInfoUtil.InitializeAsync(customDeviceInfo);
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
#if BIZZA_HTTP_AD
        await UniTask.WaitUntil(() =>
            account.LoadingFinish ||
            account.LoadingUserDataFailed);

        if (account.LoadingUserDataFailed || account.Os_Current_Uso == null)
        {
            string message = "账号数据加载失败，无法继续初始化 SDK。" +
                             account.LoadingUserDataErrorCode;
            Debug.LogError(message);
            throw new WKYInitializationException(
                "AccountLoginFailed",
                message);
        }
#else
        await UniTask.WaitUntil(() =>
                            account.LoadingFinish);
#endif
        onProgress?.Invoke(0.9f);

        LogLogger.LogInfo("State_deviceInfo2: ", $"<color=red>----------------------------</color>");
#if DEBUG_MODE
        {
            string _info = JsonUtility.ToJson(deciceInfo);
            Debug.Log(_info);
        }
#endif
        LogLogger.LogInfo("State_AccountInfo: ", $"{account.Os_Current_Uso?.GetInfo()}");

        var platform = new OverseaPlatform(config);

    }

    private static async UniTask VerifyCountryAccessAsync()
    {
        DeviceNativeBridge.ApplyVpnDetectionPolicy(null);
        for (int attempt = 1; attempt <= CountryCheckAttempts; attempt++)
        {
            using var request = UnityWebRequest.Get(CountryWorkerUrl);
            request.timeout = 8;
            request.redirectLimit = 0;

            try
            {
                await request.SendWebRequest().ToUniTask();
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
                DeviceNativeBridge.ApplyVpnDetectionPolicy(
                    request.GetResponseHeader("X-Game-Vpn-Detection"));
                Debug.Log($"[VpnAccess] detectionEnabled={DeviceNativeBridge.IsVpnDetectionEnabled}");
                return;
            }
            if (status != 403) continue;

            await SDKAssetHandler.OpenCommonConfirmTipsPanel(new CommonConfirmTipsPanel.Args
            {
                isLanguage = true,
                des = "TokenClient_REGION",
                onFinish = Application.Quit
            });
            throw new WKYInitializationException(
                "CountryRestricted",
                "Country is not enabled for this app.");
        }

        throw new WKYInitializationException(
            "CountryCheckUnavailable",
            "Country policy service did not return a valid decision.");
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
