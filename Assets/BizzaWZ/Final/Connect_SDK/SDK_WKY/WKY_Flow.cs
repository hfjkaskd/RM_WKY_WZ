using System;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class WKY_Flow
{
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
