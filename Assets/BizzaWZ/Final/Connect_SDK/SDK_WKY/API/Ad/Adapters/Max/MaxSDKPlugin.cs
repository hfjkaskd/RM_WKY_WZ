#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bizza.Sdk
{
    [Obfuz.ObfuzIgnore]
    public interface SDKPlugin
    {
        bool Inited { get; }
        E_AdsSource Name { get; }

        bool CanPlayRewardAd { get; }
        bool CanPlayIntAd { get; }

        VideoAdAdapterBase InterAdAdapter { get; }
        VideoAdAdapterBase RewardAdAdapter { get; }
        /// <summary> 开屏广告适配器，未配置开屏时可为 null </summary>
        VideoAdAdapterBase SplashAdAdapter { get; }

        void Init(E_AdsSource name);
        void SetUserId(string userId);

        void LoadAds(E_AdType adType);

        void ShowBanner();
        void HideBanner();

        bool IsEmpty();
    }
}

[Obfuz.ObfuzIgnore]
public enum E_AdType
{
    All,
    InsertAd,
    RewardAd,
    SplashAd,
    Banner,
    Native,
}

#if BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_MAX

[Obfuz.ObfuzIgnore]
public class MaxSDKPlugin : SDKPlugin
{
    private MaxInsertAdAdapter _maxInsertAd = new();
    private MaxRewardAdAdapter _maxRewardAd = new();
    private MaxSplashAdAdapter _maxSplashAd;

    public VideoAdAdapterBase InterAdAdapter => _maxInsertAd;
    public VideoAdAdapterBase RewardAdAdapter => _maxRewardAd;
    public VideoAdAdapterBase SplashAdAdapter => _maxSplashAd;

    public bool CanPlayIntAd => _maxInsertAd.IsReady;
    public bool CanPlayRewardAd => _maxRewardAd.IsReady;
    private bool _inited = false;
    public bool Inited
    {
        get => _inited;
    }

    private E_AdsSource name;
    public E_AdsSource Name => name;

    [Obfuz.ObfuzIgnore]
    public void Init(E_AdsSource name)
    {
        this.name = name;
        this._inited = false;
#if DEBUG_MODE
        bool openTestDevice = ChannelConfig.Instance.real_CustomConfig.openTestDevice;
        Debug.LogError("开启测试设备" + openTestDevice);
        if (openTestDevice)
        {
            string[] devices = ChannelConfig.Instance.real_CustomConfig.testDeviceIds;
            foreach (string device in devices)
            {
                Debug.LogError("MaxSDK Test Device:" + device);
            }
            MaxSdk.SetTestDeviceAdvertisingIdentifiers(devices);
        }
        //MaxSdk.SetCreativeDebuggerEnabled(true);
#else
        MaxSdk.SetCreativeDebuggerEnabled(false);
        LogLogger.LogADPR("MaxSdk正式-关闭测试模式");
#endif
        MaxSdk.SetHasUserConsent(true);
        MaxSdk.SetVerboseLogging(false);
        LogLogger.LogInfo(LogTag.ADReportFlow, "初始化MaxSdk-开始");
        MaxSdkCallbacks.OnSdkInitializedEvent += (x) =>
        {
#if DEBUG_MODE
    if (openTestDevice)
    {
        MaxSdk.ShowMediationDebugger();
    }      
#endif
            var uid = PlayerPrefs.GetString(AccountModule.m_userIdKey);
            if (!String.IsNullOrEmpty(uid))
            {
                LogLogger.LogADId("初始化UID到MAX平台:成功 " + uid);
                MaxSdk.SetUserId(uid);
            }
            else
            {
                Debug.LogError("初始化UID到MAX平台:失败，没有UID");
            }
            LogLogger.LogInfo(LogTag.ADReportFlow, "初始化MaxSdk-完成");
            if (_maxInsertAd == null) _maxInsertAd = new MaxInsertAdAdapter();
            if (_maxRewardAd == null) _maxRewardAd = new MaxRewardAdAdapter();

            var rewardAdUnitId = ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).rewardAdId;
            _maxRewardAd.Init(rewardAdUnitId);
            var interAdUnitId = ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).interAdId;
            _maxInsertAd.Init(interAdUnitId);
            var openAdId = ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).openAdId;
            if (!string.IsNullOrEmpty(openAdId))
            {
                _maxSplashAd = new MaxSplashAdAdapter();
                _maxSplashAd.Init(openAdId);
            }

            _inited = true;
            LoadAd(E_AdType.All).Forget();

            RunInitializationDelayAsync(1f, () =>
            {
                // canPlayAd = true;
            }).Forget();

            RunInitializationDelayAsync(1f, () =>
            {
                // BannerAd.InitBanner();
            }).Forget();

            RunInitializationDelayAsync(2f, () =>
            {
                // BannerAd.ShowBanner();
            }).Forget();
        };

        MaxSdk.InitializeSdk();
    }

    private static async UniTask RunInitializationDelayAsync(float delay, Action onComplete)
    {
        await UniTask.Delay(
            TimeSpan.FromSeconds(delay),
            ignoreTimeScale: true);
        onComplete?.Invoke();
    }

    public void SetUserId(string userId)
    {
        MaxSdk.SetUserId(userId);
    }


    public void LoadAds(E_AdType adType)
    {
        LoadAd(adType).Forget();
    }


    #region 加载广告
    private async UniTask LoadAd(E_AdType adType)
    {
        LogLogger.LogVerbose(LogTag.ADReportFlow, $"Plugin加载广告:{adType}");
        if (_inited)
        {
            switch (adType)
            {
                case E_AdType.All:
                    LoadRewardAd();
                    LoadInserAd();
                    LoadSplashAd();
                    break;
                case E_AdType.InsertAd:
                    LoadInserAd();
                    break;
                case E_AdType.RewardAd:
                    LoadRewardAd();
                    break;
                case E_AdType.SplashAd:
                    LoadSplashAd();
                    break;
            }
        }
        else
        {
            await UniTask.Delay(1000); // 延迟1秒
            LoadAd(adType);
        }
    }

    private void LoadRewardAd()
    {
        LogLogger.LogInfo(LogTag.ADReportFlow, "加载MAX Reward... ");
        _maxRewardAd.Load();
    }
    private void LoadInserAd()
    {
        LogLogger.LogInfo(LogTag.ADReportFlow, "加载MAX Insert... ");
        _maxInsertAd.Load();

    }

    private void LoadSplashAd()
    {
        if (_maxSplashAd == null) return;
        LogLogger.LogInfo(LogTag.ADReportFlow, "加载MAX 开屏... ");
        _maxSplashAd.Load();
    }
    #endregion


    #region 播放广告
    // /// <summary>
    // /// 播放激励广告
    // /// </summary>
    // /// <param name="adId"></param>
    // /// <param name="success"></param>
    // /// <param name="fail"></param>
    // public void ShowRewardAd(ShowAdArgs args)
    // {
    //     _maxRewardAd.ShowAds(args);
    // }
    //
    // /// <summary>
    // /// 播放插屏广告
    // /// </summary>
    // /// <param name="adId"></param>
    // /// <param name="success"></param>
    // /// <param name="fail"></param>
    // public void ShowInterstitialAd(E_AdPos pos, string adId, Action success, Action fail = null)
    // {
    //     _maxInsertAd.ShowAds(pos, adId, success, fail);
    // }

    public void ShowBanner()
    {

    }

    public void HideBanner()
    {

    }
    #endregion

    /// <summary>
    /// 判断本轮中 该SDK是否有广告播放
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        bool isEmpty = string.IsNullOrEmpty(_maxInsertAd.AdUnitId) && string.IsNullOrEmpty(_maxRewardAd.AdUnitId);
        return isEmpty;
    }
}

#endif
#endif
