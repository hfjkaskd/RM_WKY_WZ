#if BIZZA_REAL_WITHDRAW
#if BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_MAX
using Bizza.Sdk;
using Bizza.GameAnalytics;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza.Sdk
{
    /// <summary> Max 激励视频适配器 </summary>
    public class MaxRewardAdAdapter : RewardTimeOutAdapter
    {
        //广告Id
        private string _adUnitId;

        private bool m_IsReward;

        //重试次数
        private int retryAttempt;
        double ecpm;
        private string m_curPlacement;
        private bool m_loadingAds = false;
        /// <summary> 当前广告位/场景，如 "DailyMission"，由调用方在 ShowAds 前设置，用于上报。 </summary>

        public override double ECPM
        {
            get
            {
                if (CheckIsReady()) return ecpm;

                return 0;
            }
        }

        public override ShowAdArgs ShowAdArg { get => showAdArgs; set { showAdArgs = value; } }

        public override E_AdType adType => E_AdType.RewardAd;
        public override string AdUnitId => _adUnitId;
        public override bool IsValid => !string.IsNullOrEmpty(_adUnitId);
        public override bool IsReady => CheckIsReady();

        public override void Init(string adUnitId)
        {
            _adUnitId = adUnitId;
            // Attach callback
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent; // 加载成功
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent; // 加载失败
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent; // 开始展示/展示成功

            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent; // 用户点击，可能发生也可能不发生
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent; // 产生收入/付费回传，可能在展示后任意时刻触发，且可能多次
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent; // 广告关闭/消失
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent; // 展示失败 通常是在你调用 Show 之后
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent; // 用户满足激励条件，通常看完/达到阈值

#if BIZZA_REAL_WITHDRAW && BIZZA_HTTP_AD
            CheckLoadBatchId();
#endif
        }

        public override void Load()
        {
            if (string.IsNullOrEmpty(_adUnitId))
            {
                LogLogger.LogAdInfo("无法加载Max激励广告，_adUnitId为空");
                return;
            }

            retryAttempt = 0;
            LoadAds();
        }

        private bool CheckIsReady()
        {
#if DEBUG_MODE
            if (ChannelConfig.Instance.real_CustomConfig.rewardAdNotReady)
            {
                LogLogger.LogAdInfo("GM 导致 激励广告未准备好");
                return false;
            }
#endif
#if BIZZA_REAL_WITHDRAW
            if (!CanPlayAd())
            {
                return false;
            }
#endif
#if BIZZA_HTTP_AD
            LogLogger.LogAdInfo("Reward is ready111 " + _adUnitId);
            if (string.IsNullOrEmpty(_adUnitId))
            {
                return false;
            }
#endif
            bool _isReady = MaxSdk.IsRewardedAdReady(_adUnitId);
            LogLogger.LogAdInfo("Reward is ready22 " + _isReady);
            return _isReady;
        }

        private void LoadAds()
        {
            if (m_loadingAds || CheckIsReady())
            {
                LogLogger.LogInfo(LogTag.ADReportFlow, "激励广告已加载就绪 ： return");
                return;
            }
            m_loadingAds = true;
            LogLogger.LogInfo(LogTag.ADReportFlow, "加载激励广告 MAXax");

#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
            //UIUtils.ShowTips("模拟观看广告：广告开始加载");
#endif
            LogLogger.LogAdInfo($" ==== 激励广告加载: ");
            //Debug.Log($"[测试日志][广告加载] 加载MAX Reward ");
            MaxSdk.LoadRewardedAd(_adUnitId);
        }

        protected override void ProtectedShowRewardAds()
        {
            bool _isReady = CheckIsReady();
            LogLogger.LogInfo(LogTag.ADReportFlow, "播放激励广告 MAXax - 其是否准备好 " + _isReady);
            LogLogger.LogAdInfo($"激励广告 - ProtectedShowRewardAds {_isReady} - {_simulateRewardPlaying}");
            if (_isReady)
            {
#if BIZZA_HTTP_AD
                LogLogger.LogADId("Max激励广告Id" + _adUnitId + " BatchId " + _cachedBatchId);
#else
                string _cachedBatchId = "";
#endif
                MaxSdk.ShowRewardedAd(_adUnitId, m_curPlacement, _cachedBatchId);
#if UNITY_EDITOR
                LogLogger.LogAdInfo($"MAX 的激励 ==模拟== 播放器中： ");
                GameUtils.DelayDo(() =>
                                {
                                    OnRewardedAdRevenuePaidEvent(_adUnitId, _simulateAdInfo);
                                    //UIUtils.ShowTips("模拟观看广告：展示成功");
                                }, 0.5f).Forget();
#endif
            }
        }

        protected override void ProtectedDestroyRewardAd()
        {
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_loadingAds = false;
            double _ecpm = EcpmUtils.NormalizeEcpm(adInfo.Revenue);
            ecpm = _ecpm;
            m_curPlacement = adInfo.Placement;
            BizzaGameAnalytics.TrackAdLoad(
                adUnitId,
                BizzaAdType.Rewarded,
                "max",
                BizzaAdLoadResult.Success,
                adInfo.LatencyMillis);
            LogLogger.LogInfo(LogTag.ADReportFlow, "加载广告 信息 :" + adInfo.Revenue + " ecpm:" + ecpm);
            LogLogger.LogAdInfo($" ==== 广告加载: " + adInfo.Revenue + " ecpm:" + ecpm);

#if BIZZA_REAL_WITHDRAW && BIZZA_HTTP_AD
            OnAdLoaded(adUnitId, adInfo);
#endif
        LogLogger.LogADPR($"广告上报触发归因_{AccountModule.Instance.ADRewardType}");
        AccountModule.Instance.activeAdjust(_ecpm, AccountModule.Instance.ADRewardType);
        }

        private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            m_loadingAds = false;
            retryAttempt++;
            BizzaGameAnalytics.TrackAdLoad(
                adUnitId,
                BizzaAdType.Rewarded,
                "max",
                BizzaAdLoadResult.Failed,
                errorInfo == null ? 0 : errorInfo.LatencyMillis,
                errorInfo == null ? null : errorInfo.Code.ToString());
            LogLogger.LogInfo(LogTag.ADReportFlow, "加载广告 retryAttempt ：" + retryAttempt);
            LogLogger.LogAdInfo($" ==== 广告加载失败: " + retryAttempt);
            if (retryAttempt > 3)
            {
                return;
            }

            LoadAds();
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            BizzaGameAnalytics.TrackAdShow(showAdArgs.adPos, BizzaAdType.Rewarded, "max");
            LogLogger.LogInfo(LogTag.ADReportFlow, "激励显示");
            LogLogger.LogAdInfo($" ==== 广告 展示 : ");
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo,
            MaxSdkBase.AdInfo adInfo)
        {
            LogLogger.LogInfo(LogTag.ADReportFlow, "激励显示失败");
            LogLogger.LogAdInfo($" ==== 用户奖励失败: ");
            // Rewarded ad failed to display. AppLovin recommends that you load the next ad.
            ShowRewardAdFail();
            ShowRewardAdFinish();
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogLogger.LogInfo(LogTag.ADReportFlow, "激励视频点击");
            LogLogger.LogAdInfo($" ==== 用户点击广告: ");
#if BIZZA_REAL_WITHDRAW && BIZZA_HTTP_AD
            string id = _cachedBatchId;
            double _ecpm = EcpmUtils.NormalizeEcpm(adInfo.Revenue);
            AccountModule.Instance.Request_AdLogReportRequest(
                E_AdType.RewardAd, AccountModule.Instance.ADRewardType, "", "",
                AccountModule.Instance.ADClick, _adUnitId,
                EcpmUtils.SwitchToStringForEcpm(_ecpm), adInfo.NetworkPlacement, adInfo.NetworkName, id, null);
#endif
            // AccountController.Instance.ReportAdEvent(AccountController.AdReportType.AD_CLICK, uuid, m_LastAtid,"MAX", adInfo.NetworkName,(float)(adInfo.Revenue *1000),_adUnitId,"REWARDED_VIDEO");
        }

        // 进行了归因
        private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
#if BIZZA_REAL_WITHDRAW
            _simulateRewardPlaying = false;
#endif
            LogLogger.LogVerbose(LogTag.ADReportFlow, "MAX 激励视频关闭");
            LogLogger.LogAdInfo($" ==== 广告关闭: {adInfo == null}");

            if (adInfo == null) return;

            if (!m_IsReward)
            {
                LogLogger.LogVerbose(LogTag.ADReportFlow, "MAX 激励视频关闭 -- 失败");
                ShowRewardAdFail();
                ShowRewardAdFinish();
            }
            else
            {
                LogLogger.LogVerbose(LogTag.ADReportFlow, "MAX 激励视频关闭 -- 成功");
                //每日任务没有奖励
                if (BizzaSdk.Ad.curRewardAdPos != E_AdPos.DailyMission.ToString())
                {
                    OnReward();
                }

                ShowRewardAdFinish();
            }
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            // The rewarded ad displayed and the user should receive the reward.

        }

        // 进行了归因
        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogLogger.LogAdInfo($" ==== 广告产生收益: {adInfo == null}");
            LogLogger.LogVerbose(LogTag.ADReportFlow, "MAX 激励视频产生收益");
#if UNITY_EDITOR
            adInfo = GenerateSimulateAdInfo();
#endif

            if (adInfo == null) return;

            m_IsReward = true;
            BizzaGameAnalytics.TrackAdRevenue(
                showAdArgs.adPos,
                BizzaAdType.Rewarded,
                "max",
                adInfo.Revenue,
                "USD");

            double _ecpm = EcpmUtils.NormalizeEcpm(adInfo.Revenue);
            SaveDataUtils.GameData.totalAdEcpmValue += _ecpm;
            double ecpm_report = adInfo.Revenue * IncomeRate;
            LogLogger.LogADPR($"Reward_上报收益变化：：： ratio_{ChannelConfig.Instance.incomeRate} --- original_{adInfo.Revenue} --- report_{ecpm_report}");
            try
            {
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
                LogLogger.LogError("激励 AttributionUtil失败");
            }

            try
            {
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
                LogLogger.LogError("激励 AndroidBridgeRevenueManager失败");
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
            LogLogger.LogVerbose(LogTag.ADReportFlow, $"上报广告展示事件 {adType} {_cachedBatchId}");
#endif

            AccountModule.Instance.Request_AdLogReportRequest(
                E_AdType.RewardAd, AccountModule.Instance.ADRewardType, showAdArgs.beAdType.ToString(), showAdArgs.activeAdType.ToString(),
                AccountModule.Instance.ADShow, _adUnitId,
                EcpmUtils.SwitchToStringForEcpm(_ecpm), adInfo.NetworkPlacement, adInfo.NetworkName, id, callback);

            void callback(FailHttpResponse<AccountModule.OceanShineAdLogReportRequest> response)
            {
                if (!response.success)
                {
                    OnAdFlowEnd();
                    LogLogger.LogVerbose(LogTag.ADReportFlow, "告广告上报出现问题现在处于无id 状态");
                }
            }

#if BIZZA_HTTP_AD
            OnAddRevence();

            void OnAddRevence()
            {
                LogLogger.LogVerbose(LogTag.ADReportFlow, $"上报广告收益事件 {adType} {_cachedBatchId}");
                LogLogger.LogAdInfo($"上报广告收益事件:  {adType} {_cachedBatchId}");
                int hour = System.DateTime.Now.Hour;
                int minute = System.DateTime.Now.Minute;
                int miao = System.DateTime.Now.Second;
                LogLogger.LogInfo($"收益开始上报 {_cachedBatchId} --- Time {hour}:{minute}:{miao}");
                string _Os_Sal = BizzaSdk.Ad.curRewardAdPos == E_AdPos.DailyMission.ToString()
                    ? "task_" + AccountModule.Instance.routineTaskLookAdMoneyResponse.Os_Tid
                    : "";
                LogLogger.LogADId("Max激励广告Id获取上报" + " BatchId " + id);
                var request = AccountModule.Instance.GetAdRevenueRequest(_ecpm, _Os_Sal, id);
                AccountModule.Instance.Request_AdRevenueRequest(request, E_AdType.RewardAd, (request) =>
                    {
                        LogLogger.LogVerbose(LogTag.ADReportFlow, $"增加广告收益 {adType} {_cachedBatchId}");
                        OnAdFlowEnd();
                        if (request.Equals(default) || request.data == null)
                        {
                            LogLogger.LogADPR($"吞掉-激励-收益ID {_cachedBatchId}");
                            LogLogger.LogError("吞掉收益ID " + id);
                            return;
                        }

                        if (string.Equals(E_AdPos.DailyMission.ToString(), BizzaSdk.Ad.curRewardAdPos))
                        {
                            LogLogger.LogAdInfo($"任务广告 不会 触发收益事件  " + id);
                            LogLogger.LogVerbose(LogTag.ADReportFlow, "任务广告 的回调");
                            OnReward();
                        }
                        else if (string.Equals(E_AdPos.USSlot.ToString(), BizzaSdk.Ad.curRewardAdPos))
                        {
                            LogLogger.LogAdInfo($"老虎机广告延迟触发收益事件  " + id);
                        }
                        else
                        {
                            LogLogger.LogVerbose(LogTag.ADReportFlow, $"开始收益了 " + id);
                            LogLogger.LogAdInfo($"触发收益事件 " + id);
                            BizzaEventSystem.Emit(EventDefine.RealWithdraw.PlayCurrentFly, id, request.data.GetBalance());
                            SaveDataUtils.GameData.userTodayLookAdCount++;
                        }

                    }, 5
                );
            }
#endif
        }
    }
}
#endif
#endif
