#if BIZZA_REAL_WITHDRAW
#if BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_MAX
using System;
using Bizza.GameAnalytics;
using Bizza.Sdk;
using UnityEngine;

namespace Bizza.Sdk
{
    /// <summary> Max 开屏（App Open）广告适配器 </summary>
     
    public class MaxSplashAdAdapter : VideoAdAdapterBase
    {
        private string _adUnitId;
        private double _ecpm;
        private bool _loading;
        private Action _onSuccess;
        private Action _onFailed;

        public override E_AdType adType => E_AdType.SplashAd;
        public override string AdUnitId => _adUnitId ?? "";
        public override bool IsValid => !string.IsNullOrEmpty(_adUnitId);
        public override bool IsReady => !string.IsNullOrEmpty(_adUnitId) && MaxSdk.IsAppOpenAdReady(_adUnitId);
        public override double ECPM => IsReady ? _ecpm : 0;
        protected ShowAdArgs showAdArgs;
        public override ShowAdArgs ShowAdArg { get => showAdArgs; set { showAdArgs = value; } }

        public override void Init(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId)) return;
            _adUnitId = adUnitId;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(_adUnitId) || _loading || IsReady) return;
            _loading = true;
            MaxSdk.LoadAppOpenAd(_adUnitId);
        }

        public override void ShowAds(Action onSuccess, Action onFailed, ShowAdArgs args)
        {
            showAdArgs = args;
            _onSuccess = onSuccess;
            _onFailed = onFailed;
            if (!IsReady)
            {
                _onFailed?.Invoke();
                return;
            }
            MaxSdk.ShowAppOpenAd(_adUnitId);
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _loading = false;
            _ecpm = adInfo.Revenue * 1000;
            BizzaGameAnalytics.TrackAdLoad(
                adUnitId,
                BizzaAdType.AppOpen,
                "max",
                BizzaAdLoadResult.Success,
                adInfo.LatencyMillis);
            LogLogger.LogInfo(LogTag.ADReportFlow, $"[Max开屏] OnAdLoadedEvent revenue:{adInfo.Revenue}");
        }

        private void OnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            _loading = false;
            BizzaGameAnalytics.TrackAdLoad(
                adUnitId,
                BizzaAdType.AppOpen,
                "max",
                BizzaAdLoadResult.Failed,
                errorInfo == null ? 0 : errorInfo.LatencyMillis,
                errorInfo == null ? null : errorInfo.Code.ToString());
            LogLogger.LogInfo(LogTag.ADReportFlow, $"[Max开屏] OnAdLoadFailedEvent: {errorInfo.Message}");
        }

        private void OnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            BizzaGameAnalytics.TrackAdShow(showAdArgs.adPos, BizzaAdType.AppOpen, "max");
            LogLogger.LogInfo(LogTag.ADReportFlow, "[Max开屏] OnAdDisplayedEvent");
        }

        private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            var cb = _onSuccess;
            _onSuccess = null;
            _onFailed = null;
            cb?.Invoke();
            Load();
        }

        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            var cb = _onFailed;
            _onSuccess = null;
            _onFailed = null;
            cb?.Invoke();
            Load();
        }

        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null)
            {
                return;
            }

            BizzaGameAnalytics.TrackAdRevenue(
                showAdArgs.adPos,
                BizzaAdType.AppOpen,
                "max",
                adInfo.Revenue,
                "USD");
            _ecpm = adInfo.Revenue * 1000;
        }
    }
}
#endif
#endif
