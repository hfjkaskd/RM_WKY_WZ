#if BIZZA_REAL_WITHDRAW
#if BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_MAX
using System;
using Bizza.Sdk;
using Bizza.GameAnalytics;
using Cysharp.Threading.Tasks;
using UnityEngine;


public class MaxInsertAdAdapter : InsertAdAdapter
{
    private string _adUnitId;
    private double ecpm;
    private bool m_IsReward;
    public string m_curPlacement;
    private bool m_loadingAds = false;
    private int retryAttempt = 0;

    public override double ECPM
    {
        get
        {
            if (CheckIsReady()) return ecpm;
            LogLogger.LogVerbose(LogTag.ADReportFlow, "MAX_Insert 没有准备好 ECPM为0");
            return 0;
        }
    }

    public override E_AdType adType => E_AdType.InsertAd;
    public override string AdUnitId => _adUnitId;

    public override bool IsValid => !string.IsNullOrEmpty(_adUnitId);

    public override bool IsReady => CheckIsReady();

    protected ShowAdArgs showAdArgs;
    public override ShowAdArgs ShowAdArg { get => showAdArgs; set { showAdArgs = value; } }

    public override void Init(string adUnitId)
    {
        _adUnitId = adUnitId;
        // Attach callback
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialPaidEvent;
#if BIZZA_REAL_WITHDRAW && BIZZA_HTTP_AD
        CheckLoadBatchId();
#endif
    }

    private bool CheckIsReady()
    {
#if DEBUG_MODE 
        if (ChannelConfig.Instance.real_CustomConfig.interAdNotReady)
        {
            LogLogger.LogAdInfo("GM 导致 激励广告未准备好");
            return false;
        }
#endif
        if (!CanPlayAd())
        {
            return false;
        }

        bool _isReadly = string.IsNullOrEmpty(_adUnitId);
        if (_isReadly)
        {
            LogLogger.LogAdInfo($"MAx插屏广告Id为空 需要检查： =====================  ");
            return false;
        }
        LogLogger.LogAdInfo("Max插屏广告" + _adUnitId + "是否准备成功 " + " " + _isReadly);
        return MaxSdk.IsInterstitialReady(_adUnitId);
    }


    public override void ShowAds(Action success, Action fail, ShowAdArgs args)
    {
        LogLogger.LogAdInfo($"进入到MAX 的插屏播放器中： ");
        onSuccess = success;
        onFail = fail;
        showAdArgs = args;
        if (CheckIsReady())
        {
#if BIZZA_HTTP_AD
            string id = _cachedBatchId;
            if (args.fakeDollarNum > 0)
            {
                LogLogger.LogAdInfo($"入队钞票： {args.fakeDollarNum}");
                CurrencyBar.OnEnQueue(id, args.fakeDollarNum);
            }
#if BIZZA_REAL_WITHDRAW && UNITY_EDITOR
            _adUnitId = ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).interAdId;
#endif

            LogLogger.LogADId("Max插屏广告Id" + _adUnitId + " BatchId " + id);
#else
            string id = "";
#endif
            MaxSdk.ShowInterstitial(_adUnitId, m_curPlacement, id);

#if UNITY_EDITOR
            LogLogger.LogAdInfo($"MAX 的插屏 ==模拟== 播放器中： ");
            GameUtils.DelayDo(() =>
            {
                OnInterstitialPaidEvent(_adUnitId, _simulateAdInfo);
                //"模拟观看广告：展示成功");
            }, 0.5f).Forget();
#endif
        }
        else
        {
            onFail?.Invoke();
        }
    }

    private float delayLoadInsterAd = 20;
    private bool isLoading = false;
    private bool IsFirstLogin_Event = true;

    public override void Load()
    {
        if (string.IsNullOrEmpty(_adUnitId))
        {
            LogLogger.LogAdInfo("无法加载Max插屏广告，_adUnitId为空");
            return;
        }

        if (IsFirstLogin_Event)
        {
            if (isLoading) return;

            isLoading = true;
            LogLogger.LOGGameStart("首次进入游戏 - 延迟20秒 - 加载MAX Insert");

            GameUtils.DelayDo(() =>
            {
                IsFirstLogin_Event = false;
                isLoading = false;

                LogLogger.LOGGameStart("延迟加载完成 - 加载MAX Insert");
                retryAttempt = 0;
                Load();
            }, delayLoadInsterAd).Forget();

            return;
        }

        LogLogger.LOGGameStart("非首次进入 - 直接加载MAX Insert");
        retryAttempt = 0;
        InternalLoad();
    }

    private void InternalLoad()
    {
        if (m_loadingAds || CheckIsReady()) return;
        m_loadingAds = true;

        LogLogger.LogInfo(LogTag.ADReportFlow, "加载插屏广告 MAXax");
#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
        //UIUtils.ShowTips("模拟观看广告：广告开始加载");
        _adUnitId = ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).interAdId;
#endif
        LogLogger.LogAdInfo($" ==== 插屏广告加载: ");
        MaxSdk.LoadInterstitial(_adUnitId);
    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        m_loadingAds = false;
        double _ecpm = EcpmUtils.NormalizeEcpm(adInfo.Revenue);
        ecpm = _ecpm;
        m_curPlacement = adInfo.Placement;
        BizzaGameAnalytics.TrackAdLoad(
            adUnitId,
            BizzaAdType.Interstitial,
            "max",
            BizzaAdLoadResult.Success,
            adInfo.LatencyMillis);
        LogLogger.LogVerbose(LogTag.ADReportFlow, "Max Insert callback onAdLoad :" + adInfo.Revenue + " ecpm: " + _ecpm);
        LogLogger.LogAdInfo($" ==== 插屏广告加载完毕: " + adInfo.Revenue + " ecpm:" + ecpm);
#if BIZZA_REAL_WITHDRAW && BIZZA_HTTP_AD
        OnAdLoaded(adUnitId, adInfo);
#endif
        LogLogger.LogADPR($"广告上报触发归因_{AccountModule.Instance.ADInterstitialType}");
        AccountModule.Instance.activeAdjust(_ecpm, AccountModule.Instance.ADInterstitialType);
    }

    private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        LogLogger.LogVerbose(LogTag.ADReportFlow, $" ==== 插屏广告加载失败: ");
        m_loadingAds = false;
        retryAttempt++;
        BizzaGameAnalytics.TrackAdLoad(
            adUnitId,
            BizzaAdType.Interstitial,
            "max",
            BizzaAdLoadResult.Failed,
            errorInfo == null ? 0 : errorInfo.LatencyMillis,
            errorInfo == null ? null : errorInfo.Code.ToString());
        if (retryAttempt > 3) //
        {
            return;
        }

        Load();
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        BizzaGameAnalytics.TrackAdShow(showAdArgs.adPos, BizzaAdType.Interstitial, "max");
        LogLogger.LogAdInfo($" ==== 插屏广告显示 DIsplay ");
        LogLogger.LogInfo(LogTag.ADReportFlow, $" 广告显示: revenue:{adInfo.Revenue}");
    }

    private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo,
        MaxSdkBase.AdInfo adInfo)
    {
        LogLogger.LogAdInfo($" ==== 插屏广告显示失败 DIsplayFail ");
        LogLogger.LogInfo(LogTag.ADReportFlow, $" 广告显示失败: errorInfo:{errorInfo.ToString()}, adUnitId:{adUnitId}, adInfo:{(adInfo != null ? adInfo.ToString() : "null")}");
        onFail?.Invoke();
    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogLogger.LogAdInfo($" ==== 插屏广告点击 ");
        LogLogger.LogInfo(LogTag.ADReportFlow, $" 点击插屏广告");
#if BIZZA_REAL_WITHDRAW && BIZZA_HTTP_AD
        var id = _cachedBatchId;
        double _ecpm = EcpmUtils.NormalizeEcpm(adInfo.Revenue);
        AccountModule.Instance.Request_AdLogReportRequest(
            E_AdType.InsertAd, AccountModule.Instance.ADInterstitialType, "", "",
            AccountModule.Instance.ADClick, ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).interAdId,
            EcpmUtils.SwitchToStringForEcpm(_ecpm), adInfo.NetworkPlacement, adInfo.NetworkName, id, null);
#endif
    }

    private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogLogger.LogAdInfo($" ==== 插屏广告隐藏 ");
        LogLogger.LogInfo(LogTag.ADReportFlow, $"插屏广告隐藏");

        if (m_IsReward)
        {
            onSuccess?.Invoke();
        }
        else
        {
            onFail?.Invoke();
        }
    }

    private void OnInterstitialPaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        LogLogger.LogAdInfo($" ==== 插屏广告获取收益 ");
        m_IsReward = true;
#if UNITY_EDITOR
        adInfo = GenerateSimulateAdInfo();
#endif
        if (adInfo == null)
        {
            return;
        }
        BizzaGameAnalytics.TrackAdRevenue(
            showAdArgs.adPos,
            BizzaAdType.Interstitial,
            "max",
            adInfo.Revenue,
            "USD");
        double _ecpm = EcpmUtils.NormalizeEcpm(adInfo.Revenue);
        SaveDataUtils.GameData.totalAdEcpmValue += _ecpm;
        double ecpm_report = adInfo.Revenue * IncomeRate;
        LogLogger.LogADPR($"Insert_上报收益变化：：： ratio_{ChannelConfig.Instance.incomeRate} --- original_{adInfo.Revenue} --- report_{ecpm_report}");
        try
        {
            LogLogger.LogVerbose(LogTag.ADAttribute,
                $"Adjust : adInfo.Revenue_{adInfo.Revenue}; adInfo.NetworkName_{adInfo.NetworkName}; adInfo.AdUnitIdentifier_{adInfo.AdUnitIdentifier}; adInfo.Placement_{adInfo.Placement}");
            AdjustAttributionAdapter.ReportAdShowForAdjust(
                ecpm_report,
                "USD",
                adInfo.NetworkName,
                adInfo.AdUnitIdentifier,
                adInfo.Placement
            );
        }
        catch (Exception e)
        {
            LogLogger.LogError("插屏AttributionUtil失败");
        }

        try
        {
            LogLogger.LogVerbose(LogTag.ADAttribute,
                $"MBridge : adInfo.Revenue_{adInfo.Revenue}; adJustAdidKey_{PlayerPrefs.GetString(AccountModule.adJustAdidKey)}; adInfo_{adInfo}; adInfo.WaterfallInfo_{adInfo.WaterfallInfo}; adInfo.RevenuePrecision_{adInfo.RevenuePrecision}");
            AndroidBridgeRevenueManager.OnReportRevenue(
                "Adjust",
                PlayerPrefs.GetString(AccountModule.adJustAdidKey),
                adInfo.ToString(),
                adInfo.WaterfallInfo.ToString(),
                adInfo.RevenuePrecision,
                ecpm_report
            );
        }
        catch (Exception e)
        {
            LogLogger.LogError("插屏AndroidBridgeRevenueManager失败");
        }

        try
        {
            AndroidBridgeRevenueManager.OnInMobiReportRevenue(
                 adInfo
            );
        }
        catch (Exception e)
        {
            LogLogger.LogError("激励 AndroidBridgeRevenueManager失败");
        }


        string id = "";
#if BIZZA_HTTP_AD
        id = _cachedBatchId;
#endif
        AccountModule.Instance.Request_AdLogReportRequest(
            E_AdType.InsertAd, AccountModule.Instance.ADInterstitialType, showAdArgs.beAdType.ToString(), showAdArgs.activeAdType.ToString(),
            AccountModule.Instance.ADShow, ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).interAdId,
            EcpmUtils.SwitchToStringForEcpm(_ecpm), adInfo.NetworkPlacement, adInfo.NetworkName, id, callback);

        void callback(FailHttpResponse<AccountModule.OceanShineAdLogReportRequest> response)
        {
            if (!response.success)
            {
                OnAdFlowEnd();
            }
        }
#if BIZZA_HTTP_AD
        AddRevenue();

        void AddRevenue()
        {
            LogLogger.LogInfo(LogTag.ADReportFlow, $"上报广告收益事件 {_cachedBatchId}");
            string _Os_Sal = BizzaSdk.Ad.curInterAdPos == E_AdPos.DailyMission.ToString()
                ? "task_" + AccountModule.Instance.routineTaskLookAdMoneyResponse.Os_Tid
                : "";
            var request = AccountModule.Instance.GetAdRevenueRequest(_ecpm, _Os_Sal, id);
            LogLogger.LogADId("Max插屏广告Id获取上报" + " BatchId " + id);
            AccountModule.Instance.Request_AdRevenueRequest(request, E_AdType.InsertAd, (request) =>
                {
                    LogLogger.LogInfo(LogTag.ADReportFlow, $"增加广告收益 {_cachedBatchId}");
                    OnAdFlowEnd();
                    if (request.Equals(default) || request.data == null)
                    {
                        LogLogger.LogADPR($"吞掉-插屏-收益ID {_cachedBatchId}");
                        LogLogger.LogInfo(LogTag.ADReportFlow, $"吞掉收益ID {_cachedBatchId}");
                        return;
                    }

                    if (string.Equals(E_AdPos.DailyMission.ToString(), BizzaSdk.Ad.curInterAdPos))
                    {
                        LogLogger.LogInfo(LogTag.ADReportFlow, $"任务插屏 不可以收益");
                    }
                    else if (string.Equals(E_AdPos.USSlot.ToString(), BizzaSdk.Ad.curInterAdPos))
                    {
                        LogLogger.LogAdInfo($"老虎机广告延迟触发收益事件  " + id);
                    }
                    else
                    {
                        LogLogger.LogInfo(LogTag.ADReportFlow, $"插屏 开始收益了");
                        BizzaEventSystem.Emit(EventDefine.RealWithdraw.PlayCurrentFly, id, request.data.GetBalance());
                        SaveDataUtils.GameData.userTodayLookAdCount++;
                    }
                }, 5
            );
        }
#endif
    }
}


#endif
#endif
