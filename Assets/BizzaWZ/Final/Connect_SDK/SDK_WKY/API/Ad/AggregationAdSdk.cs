#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bizza.GameAnalytics;
using Bizza.Sdk;
using UnityEngine;

namespace Bizza.Sdk
{
    /// <summary>
    /// 聚合广告，可支持多个sdk竞价
    /// </summary>
    public class AggregationAdSdk : AdSdkBase
    {
        //全部加载完毕
        public bool Inited
        {
            get
            {
                if (_adSdkPlugins == null || _adSdkPlugins.Count == 0) return false;
                foreach (var v in _adSdkPlugins)
                {
                    if (!v.Inited) return false;
                }

                return true;
            }
        }

        private List<SDKPlugin> _adSdkPlugins = new();
        //在激励视频不可用时，是否使用插屏补充激励
        private bool _useInterReplenishReward;
        private bool _useRewardReplenishInter;

        private E_AdType currentPlayAdType;

        // public List<SDKPlugin> sdkPlugins = new();
        public void InitAdAdapter(ChannelConfig channelConfig)
        {
            //==============初始化广告sdk===========
            LoadAds(channelConfig.sourceAds);
            _useInterReplenishReward = channelConfig.useInterReplenishReward;
            _useRewardReplenishInter = channelConfig.useRewardReplenishInter;
        }

        #region 激励视频

        //实际上激励广告是否在展示中，替代展示的插屏不算
        public override bool IsRewardShowing => _isRewardShowing;
        private bool _isRewardShowing;
        private List<VideoAdAdapterBase> _rewardAdAdapters = new();
        public override bool IsRewardInvalid => true;
        public override bool IsRewardReady => CheckRewardAdReady(default);
        private bool isPlayingAd => BizzaSdk.Ad.IsInterShowing || BizzaSdk.Ad.IsRewardShowing;

        private void LoadRewardAd()
        {
            LogLogger.LogInfo(LogTag.ADReportFlow, "触发激励广告加载");
            if (_rewardAdAdapters == null)
            {
                return;
            }

            LogLogger.LogVerbose(LogTag.ADReportFlow, $"触发激励广告加载 adapter数量:{_rewardAdAdapters.Count}");
            foreach (var v in _rewardAdAdapters)
            {
                v.Load();
            }
        }

        public override bool CheckRewardAdReady(CheckAdReadyArgs args = default)
        {
            //激励本身是否可用
            LogLogger.LogAdInfo($"检查激励广告是否可播放：adapter列表存在={_rewardAdAdapters != null}，ecpm门槛={args.ecpmLimit}，是否排除补量={args.excludeReplenish}");
            if (_rewardAdAdapters != null)
            {
                LogLogger.LogAdInfo($"检查激励广告是否可播放：adapter数量={_rewardAdAdapters.Count}");
                for (int i = 0; i < _rewardAdAdapters.Count; i++)
                {
                    var v = _rewardAdAdapters[i];
                    if (v == null)
                    {
                        LogLogger.LogAdInfo($"检查激励广告：[{i}] adapter为空，跳过");
                        continue;
                    }
                    bool isReady = v.IsReady;
                    double ecpm = isReady ? v.ECPM : 0;
#if UNITY_EDITOR && BIZZA_ENABLE_MAX && BIZZA_HTTP_AD
                    ecpm = EcpmUtils.NormalizeEcpm(HttpAdAdapterBase.simulateECPM);
#endif
                    bool pass = isReady && ecpm >= args.ecpmLimit;
                    LogAdapterCheck("检查激励广告", v, i, isReady, isReady ? ecpm : null, args.ecpmLimit,
                        pass ? "满足播放条件" : (isReady ? "ECPM低于门槛" : "未就绪"));
                    if (pass)
                    {
                        LogLogger.LogAdInfo($"检查激励广告是否可播放：命中可用adapter，索引={i}，{FormatAdapterInfo(v)}");
                        return true;
                    }
                }
            }

            LogLogger.LogAdInfo($"激励广告是否允许插屏补量：启用={_useInterReplenishReward}，当前已排除补量={args.excludeReplenish}");
            //使用插屏替代
            if (_useInterReplenishReward && !args.excludeReplenish)
            {
                args.excludeReplenish = true;
                LogLogger.LogAdInfo("检查激励广告是否可播放：开始检查插屏补量");
                if (CheckInterAdReady(args))
                {
                    LogLogger.LogAdInfo("检查激励广告是否可播放：插屏补量可用");
                    return true;
                }
            }

            LogLogger.LogAdInfo("检查激励广告是否可播放：未找到满足条件的adapter");
            return false;
        }


        /// <summary>
        /// 播放广告
        /// </summary>
        public override void ShowRewardAd(ShowAdArgs args)
        {
            if (isPlayingAd)
            {
                BizzaGameAnalytics.TrackAdShowRequest(
                    args.adPos,
                    BizzaAdType.Rewarded,
                    "max",
                    isReady: false);
                OnAdsRewardFail();
                return;
            }
#if BIZZA_REAL_WITHDRAW
            AccountModule.Instance.AdRevenueResponse = null;
#endif
            LogLogger.LogAdInfo($"触发激励视频展示：广告位={FormatAdPos(args.adPos)}，广告源={args.adSource}，ecpm门槛={args.ecpmLimit}");

            float ecmp = 0f;

            var checkArgs = new CheckAdReadyArgs
            {
                ecpmLimit = args.ecpmLimit,
                excludeReplenish = true
            };
            bool canShowRealReward = CheckRewardAdReady(checkArgs);
            LogLogger.LogAdInfo($"是否可以播放激励广告：{canShowRealReward}");
            bool _checkInterAdReady = CheckInterAdReady(checkArgs);
            bool useInterReplenish = !canShowRealReward && _useInterReplenishReward &&
                                     _checkInterAdReady;
            LogLogger.LogAdInfo($"是否可以播放插屏替代：{useInterReplenish} + {canShowRealReward} + {_useInterReplenishReward} + {_checkInterAdReady}");
            var biddingType = useInterReplenish ? E_AdType.InsertAd : E_AdType.RewardAd;
            LogLogger.LogAdInfo($"激励视频开始竞价：本次竞价类型={GetAdTypeDesc(biddingType)}，真实激励可播={canShowRealReward}，插屏补量启用={_useInterReplenishReward}，插屏补量可播={_checkInterAdReady}");

            VideoAdAdapterBase adapter = OnSDKAdBidding(biddingType, checkArgs);
            BizzaGameAnalytics.TrackAdShowRequest(
                args.adPos,
                biddingType == E_AdType.InsertAd ? BizzaAdType.Interstitial : BizzaAdType.Rewarded,
                "max",
                adapter != null);
            if (adapter == null)
            {
                LogLogger.LogAdInfo($"激励视频展示失败：竞价未选出可用adapter，本次竞价类型={GetAdTypeDesc(biddingType)}");
                OnAdsRewardFail();
                return;
            }

            if (adapter is InsertAdAdapter)
            {
                BizzaSdk.Ad.curInterAdPos = args.adPos;
                args.activeAdType = AdType.Interstitial;
            }

            LogLogger.LogAdInfo($"激励视频竞价成功：已选中adapter，{FormatAdapterInfo(adapter)}");

            try
            {
                ecmp = (float)adapter.ECPM;
#if UNITY_EDITOR && BIZZA_ENABLE_MAX && BIZZA_HTTP_AD
                ecmp = (float)EcpmUtils.NormalizeEcpm(HttpAdAdapterBase.simulateECPM);
#endif
#if DEBUG_MODE && BIZZA_REAL_WITHDRAW
                if (ChannelConfig.Instance.real_CustomConfig.testECPM1000)
                {
                    ecmp = ChannelConfig.Instance.real_CustomConfig.TestECPMValue; ;
                    // LogLogger.LogADPR($"测试阶段 比价后 ：：： ECPM设置为1000");
                }
#endif
                if (args.ecpmLimit > 0 && ecmp < args.ecpmLimit)
                {
                    LogLogger.LogAdInfo($"激励视频展示失败：Ecmp不满足要求 目标：{args.ecpmLimit} 当前：{ecmp}");
                    OnAdsRewardFail();
                    return;
                }

#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
                if (ChannelConfig.Instance.real_CustomConfig.failOpenAd)
                {
                    OnAdsRewardFail();
                    return;
                }
#endif
#if !COMMONGAME
                NumbericalStatistics.CloseGetRewardNum = 0;
#endif
                OnRewardAdStart(!useInterReplenish, args);
                SaveDataUtils.GameData.userLastLoginLookAdCount++;
                SaveDataUtils.GameData.userLastLoginLookRewardAdCount++;
                adapter.ShowAds(OnAdsRewardSuccess, OnAdsRewardFail, args);
            }
            catch (Exception e)
            {
                LogLogger.LogAdInfo($"广告未加载出来" + e);
                OnAdsRewardFail();
            }

            //============回调方法=========
            void OnAdsRewardSuccess()
            {
#if BIZZA_REAL_WITHDRAW
                Bizza.Sdk.ShowAdResult showAdResult = new ShowAdResult(true, AccountModule.Instance.AdRevenueResponse);
#else
                Bizza.Sdk.ShowAdResult showAdResult = new ShowAdResult(){success = true};
#endif
                OnRewardAdFinish(args, showAdResult);
            }

            void OnAdsRewardFail()
            {
                #if !COMMONGAME
                UIUtils.ShowTips(LanguageUtils.GetText("DailyMissionPanel_NoAd"));
                #endif
                OnRewardAdFinish(args, default);
            }
        }

        private void OnRewardAdStart(bool isRealRewardAd, ShowAdArgs args)
        {
            LogLogger.LogAdInfo($"激励视频展示开始： {isRealRewardAd}");
            _isRewardShowing = isRealRewardAd;
            BizzaEventSystem.Emit(EventDefine.AdEvent.RewardAdStart);
            BizzaSdk.Ad.curRewardAdPos = args.adPos;
        }

        private void OnRewardAdFinish(ShowAdArgs args, Bizza.Sdk.ShowAdResult showAdResult)
        {
            args.onFinish?.Invoke(showAdResult);
            _isRewardShowing = false;
            BizzaEventSystem.Emit(EventDefine.AdEvent.RewardAdFinish, true);

            AdLoads();
        }

        #endregion

        #region 插屏广告

        //实际上插屏广告是否在展示中，替代展示的激励不算
        public override bool IsInterShowing => _isInterShowing;
        private bool _isInterShowing;
        private long _interAdShowTime = -1;

        private List<VideoAdAdapterBase> _interAdAdapters = new();
        public override bool IsInterInvalid => true;
        public override bool IsInterReady => CheckInterAdReady();


        public override void ShowInterAd(ShowAdArgs args)
        {
            if (isPlayingAd)
            {
                BizzaGameAnalytics.TrackAdShowRequest(
                    args.adPos,
                    BizzaAdType.Interstitial,
                    "max",
                    isReady: false);
                OnAdsRewardFail();
                return;
            }
#if BIZZA_REAL_WITHDRAW
            AccountModule.Instance.AdRevenueResponse = null;
#endif
            LogLogger.LogAdInfo($"触发插屏广告展示：广告位={FormatAdPos(args.adPos)}，广告源={args.adSource}，ecpm门槛={args.ecpmLimit}");
            var checkArgs = new CheckAdReadyArgs
            {
                ecpmLimit = args.ecpmLimit,
                excludeReplenish = true
            };
            bool canShowRealInter = CheckInterAdReady(checkArgs);
            bool canUseRewardReplenish = false;
            if (!canShowRealInter && _useRewardReplenishInter)
            {
                canUseRewardReplenish = CheckRewardAdReady(checkArgs);
            }
            bool useRewardReplenish = !canShowRealInter && _useRewardReplenishInter &&
                                      canUseRewardReplenish;
            var biddingType = useRewardReplenish ? E_AdType.RewardAd : E_AdType.InsertAd;
            LogLogger.LogAdInfo($"插屏广告开始竞价：真实插屏可播={canShowRealInter}，激励补量启用={_useRewardReplenishInter}，激励补量可播={canUseRewardReplenish}，本次竞价类型={GetAdTypeDesc(biddingType)}");

            void OnAdsRewardSuccess()
            {
#if BIZZA_REAL_WITHDRAW
                Bizza.Sdk.ShowAdResult showAdResult = new ShowAdResult(true, AccountModule.Instance.AdRevenueResponse);
#else
                Bizza.Sdk.ShowAdResult showAdResult = new ShowAdResult(){success = true};
#endif
                args.onFinish?.Invoke(showAdResult);
                _isInterShowing = false;
                BizzaEventSystem.Emit(EventDefine.AdEvent.InterAdFinish);
                AdLoads();
            }

            void OnAdsRewardFail()
            {
                _isInterShowing = false;
                // args.onFailed?.Invoke();
                args.onFinish?.Invoke(default);
                AdLoads();
            }

            VideoAdAdapterBase sdkInfo = OnSDKAdBidding(biddingType, checkArgs);
            BizzaGameAnalytics.TrackAdShowRequest(
                args.adPos,
                biddingType == E_AdType.RewardAd ? BizzaAdType.Rewarded : BizzaAdType.Interstitial,
                "max",
                sdkInfo != null);
            if (sdkInfo == null)
            {
                LogLogger.LogAdInfo($"插屏广告展示失败：竞价未选出可用adapter，本次竞价类型={GetAdTypeDesc(biddingType)}");
                OnAdsRewardFail();
            }
            else
            {
                LogLogger.LogAdInfo($"插屏广告竞价成功：已选中adapter，{FormatAdapterInfo(sdkInfo)}");
#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
                if (ChannelConfig.Instance.real_CustomConfig.failOpenAd)
                {
                    OnAdsRewardFail();
                    return;
                }
#endif

                // var cooldownMs =
                //     RemoteGroupDataSystem.current.GetActiveAdStatisticsOrDefault(SaveDataUtils.GameData.playerSelectedLv).InterAdCooldownMs;
                // long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                // long interval = currentTime - _interAdShowTime;
                // if (cooldownMs > 0 &&
                //     _interAdShowTime > 0 &&
                //     interval < cooldownMs)
                // {
                //     long remainingMs = cooldownMs - interval;

                //     LogLogger.LogAdInfo(
                //         $"插屏广告展示被冷却拦截，剩余冷却时间：{remainingMs / 1000f:F1} 秒");

                //     OnAdsRewardFail();
                //     return;
                // }
                _interAdShowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                _isInterShowing = !useRewardReplenish;
                BizzaEventSystem.Emit(EventDefine.AdEvent.InterAdStart);
                BizzaSdk.Ad.curInterAdPos = args.adPos;
#if BIZZA_REAL_WITHDRAW && !COMMONGAME
                NumbericalStatistics.CloseGetRewardNum = 0; // 业务代码
#endif
                SaveDataUtils.GameData.userLastLoginLookAdCount++;
                SaveDataUtils.GameData.userLastLoginLookInsertAdCount++;
                sdkInfo.ShowAds(OnAdsRewardSuccess, OnAdsRewardFail, args);
            }
        }

        public override bool CheckInterAdReady(CheckAdReadyArgs args = default)
        {
            //插屏本身是否可用
            LogLogger.LogAdInfo($"检查插屏广告是否可播放：adapter列表存在={_interAdAdapters != null}，ecpm门槛={args.ecpmLimit}，是否排除补量={args.excludeReplenish}");
            if (_interAdAdapters != null)
            {
                LogLogger.LogAdInfo($"检查插屏广告是否可播放：adapter数量={_interAdAdapters.Count}");
                for (int i = 0; i < _interAdAdapters.Count; i++)
                {
                    var v = _interAdAdapters[i];
                    if (v == null)
                    {
                        LogLogger.LogAdInfo($"检查插屏广告：[{i}] adapter为空，跳过");
                        continue;
                    }
                    bool isReady = v.IsReady;
                    double ecpm = isReady ? v.ECPM : 0;
                    bool pass = isReady && ecpm >= args.ecpmLimit;
                    LogAdapterCheck("检查插屏广告", v, i, isReady, isReady ? ecpm : null, args.ecpmLimit,
                        pass ? "满足播放条件" : (isReady ? "ECPM低于门槛" : "未就绪"));
                    if (pass)
                    {
                        LogLogger.LogAdInfo($"检查插屏广告是否可播放：命中可用adapter，索引={i}，{FormatAdapterInfo(v)}");
                        return true;
                    }
                }
            }

            //激励替补
            LogLogger.LogAdInfo($"插屏广告是否允许激励补量：启用={_useRewardReplenishInter}，当前已排除补量={args.excludeReplenish}");
            if (_useRewardReplenishInter && !args.excludeReplenish)
            {
                args.excludeReplenish = true;
                LogLogger.LogAdInfo("检查插屏广告是否可播放：开始检查激励补量");
                if (CheckRewardAdReady(args))
                {
                    LogLogger.LogAdInfo("检查插屏广告是否可播放：激励补量可用");
                    return true;
                }
            }

            LogLogger.LogAdInfo("检查插屏广告是否可播放：未找到满足条件的adapter");
            return false;
        }


        private void LoadInterAd()
        {
            foreach (var adapter in _interAdAdapters)
            {
                adapter.Load();
            }
        }

        #endregion

        #region 开屏广告

        public override void SetUserId(string userId)
        {
            foreach (var v in _adSdkPlugins)
            {
                v.SetUserId(userId);
            }
        }

        public override bool IsSplashInvalid => _splashAdAdapters == null || _splashAdAdapters.Count == 0;
        public override bool IsSplashReady => GetReadySplashAdapter() != null;
        public override bool IsSplashShowing => _isSplashShowing;
        private bool _isSplashShowing;
        private List<VideoAdAdapterBase> _splashAdAdapters = new();

        private VideoAdAdapterBase GetReadySplashAdapter()
        {
            if (_splashAdAdapters == null) return null;
            foreach (var a in _splashAdAdapters)
            {
                if (a != null && a.IsReady) return a;
            }
            return null;
        }

        private void LoadSplashAd()
        {
            if (_splashAdAdapters == null) return;
            foreach (var a in _splashAdAdapters)
                a?.Load();
        }

        public override void ShowSplashAd()
        {
            const string analyticsPlacement = "app_open";
            var adapter = GetReadySplashAdapter();
            BizzaGameAnalytics.TrackAdShowRequest(
                analyticsPlacement,
                BizzaAdType.AppOpen,
                "max",
                adapter != null);
            if (adapter == null)
            {
                LogLogger.LogInfo(LogTag.ADReportFlow, "开屏广告展示失败：无可用适配器或未就绪");
                return;
            }
            _isSplashShowing = true;
            BizzaEventSystem.Emit(EventDefine.AdEvent.SplashAdStart);
            adapter.ShowAds(
                onSuccess: () =>
                {
                    _isSplashShowing = false;
                    BizzaEventSystem.Emit(EventDefine.AdEvent.SplashAdFinish);
                    LoadSplashAd();
                },
                onFailed: () =>
                {
                    _isSplashShowing = false;
                    LoadSplashAd();
                }, new ShowAdArgs { adPos = analyticsPlacement });
        }

        #endregion


        public void LoadAds(List<AdConfig> adConfigs)
        {
            if (adConfigs.Count == 0)
            {
                LogLogger.LogError(LogTag.ADReportFlow, "======================================================");
                LogLogger.LogError(LogTag.ADReportFlow, "No sdk plugins found");
                LogLogger.LogError(LogTag.ADReportFlow, "======================================================");
            }
            _adSdkPlugins.Clear();
            foreach (var sdkPlugin in adConfigs)
            {
                switch (sdkPlugin.AdsSource)
                {
                    case E_AdsSource.Max:
#if BIZZA_ENABLE_MAX
                        MaxSDKPlugin maxSDKPlugin = new MaxSDKPlugin();
                        maxSDKPlugin.Init(E_AdsSource.Max);
                        _rewardAdAdapters.Add(maxSDKPlugin.RewardAdAdapter);
                        _interAdAdapters.Add(maxSDKPlugin.InterAdAdapter);
                        if (maxSDKPlugin.SplashAdAdapter != null)
                            _splashAdAdapters.Add(maxSDKPlugin.SplashAdAdapter);
                        _adSdkPlugins.Add(maxSDKPlugin);
#else
#if !UNITY_EDITOR
                        LogLogger.LogError(LogTag.ADReportFlow, "配置了Max广告，但是未开启BIZZA_ENABLE_MAX宏定义");
#endif
#endif
                        break;
                    case E_AdsSource.TopOn:
                        LogLogger.LogError(LogTag.ADReportFlow, "TopOn广告功能暂未实现");
                        break;
                }
            }
        }

        private VideoAdAdapterBase OnSDKAdBidding(E_AdType adType, CheckAdReadyArgs args = default)
        {
            LogLogger.LogAdInfo($"开始SDK竞价：广告类型={GetAdTypeDesc(adType)}，ecpm门槛={args.ecpmLimit}，是否排除补量={args.excludeReplenish}");
            currentPlayAdType = adType;
            VideoAdAdapterBase _adapter = null;
            List<VideoAdAdapterBase> list = null;
            if (adType == E_AdType.All)
            {
                list = new List<VideoAdAdapterBase>();
                list.AddRange(_rewardAdAdapters);
                list.AddRange(_interAdAdapters);
            }
            else
            {
                list = adType == E_AdType.RewardAd ? _rewardAdAdapters : _interAdAdapters;
            }

            if (list == null)
            {
                LogLogger.LogAdInfo($"开始SDK竞价：候选adapter列表为null，广告类型={GetAdTypeDesc(adType)}");
                return null;
            }

            LogLogger.LogAdInfo($"开始SDK竞价：候选adapter数量={list.Count}");
            var maxEcpm = double.MinValue;
            for (int i = 0; i < list.Count; i++)
            {
                var adapter = list[i];
                if (adapter == null)
                {
                    LogLogger.LogAdInfo($"SDK竞价：[{i}] adapter为空，跳过");
                    continue;
                }

                bool isReady = adapter.IsReady;
                if (!isReady)
                {
                    LogAdapterCheck("SDK竞价", adapter, i, false, null, args.ecpmLimit, "跳过，未就绪");
                    continue;
                }

                double ecpm = adapter.ECPM;

#if UNITY_EDITOR && BIZZA_ENABLE_MAX && BIZZA_HTTP_AD
                ecpm = EcpmUtils.NormalizeEcpm(HttpAdAdapterBase.simulateECPM);
#endif
                if (ecpm < args.ecpmLimit)
                {
                    LogAdapterCheck("SDK竞价", adapter, i, true, ecpm, args.ecpmLimit, "跳过，ECPM低于门槛");
                    continue;
                }

                if (ecpm > maxEcpm)
                {
                    _adapter = adapter;
                    maxEcpm = ecpm;
                    LogAdapterCheck("SDK竞价", adapter, i, true, ecpm, args.ecpmLimit, "当前最高价，暂定选中");
                }
                else
                {
                    LogAdapterCheck("SDK竞价", adapter, i, true, ecpm, args.ecpmLimit, $"未选中，低于当前最高ECPM={maxEcpm:F4}");
                }
            }

            if (_adapter == null)
            {
                LogLogger.LogAdInfo($"SDK竞价结束：未选出可用adapter，广告类型={GetAdTypeDesc(adType)}");
            }
            else
            {
                LogLogger.LogAdInfo($"SDK竞价结束：最终选中adapter，{FormatAdapterInfo(_adapter)}，最终ECPM={maxEcpm:F4}");
            }

            return _adapter;
        }

        private static string GetAdTypeDesc(E_AdType adType)
        {
            return adType switch
            {
                E_AdType.All => "全部广告",
                E_AdType.InsertAd => "插屏广告",
                E_AdType.RewardAd => "激励广告",
                E_AdType.SplashAd => "开屏广告",
                E_AdType.Banner => "Banner广告",
                E_AdType.Native => "原生广告",
                _ => adType.ToString()
            };
        }

        private static string FormatAdPos(string adPos)
        {
            return string.IsNullOrEmpty(adPos) ? "未传广告位" : adPos;
        }

        private static string FormatAdapterInfo(VideoAdAdapterBase adapter)
        {
            if (adapter == null)
            {
                return "adapter=null";
            }

            return $"类型={adapter.GetType().Name}，AdUnitId={GetSafeAdUnitId(adapter)}，IsValid={GetSafeIsValid(adapter)}";
        }

        private static string GetSafeAdUnitId(VideoAdAdapterBase adapter)
        {
            try
            {
                return string.IsNullOrEmpty(adapter.AdUnitId) ? "空" : adapter.AdUnitId;
            }
            catch (Exception e)
            {
                return $"读取异常:{e.GetType().Name}";
            }
        }

        private static string GetSafeIsValid(VideoAdAdapterBase adapter)
        {
            try
            {
                return adapter.IsValid.ToString();
            }
            catch (Exception e)
            {
                return $"读取异常:{e.GetType().Name}";
            }
        }

        private static void LogAdapterCheck(string scene, VideoAdAdapterBase adapter, int index, bool isReady,
            double? ecpm, float ecpmLimit, string result)
        {
            string ecpmText = ecpm.HasValue ? ecpm.Value.ToString("F4") : "未读取";
            LogLogger.LogAdInfo(
                $"{scene}：[{index}] {FormatAdapterInfo(adapter)}，IsReady={isReady}，ECPM={ecpmText}，门槛={ecpmLimit:F4}，结果={result}");
        }


        #region Banner

        private BannerAdapterBase _bannerAdapter;
        public override bool IsBannerInvalid => _bannerAdapter != null && _bannerAdapter.IsValid;
        public override bool IsBannerReady => _bannerAdapter != null && _bannerAdapter.IsReady;
        public override bool IsBannerShowing => _isBannerShowing;
        private bool _isBannerShowing;

        // 暂时未对 Banner 广告进行处理， 后续有需求再处理
        public override void ShowBanner()
        {
            // BannerAd.ShowBanner();
            if (!IsBannerReady)
            {
                return;
            }

            _bannerAdapter.Show();
            _isBannerShowing = true;
            BizzaEventSystem.Emit(EventDefine.AdEvent.BannerShowOrHide, true);
        }

        // 暂时未对 Banner 广告进行处理， 后续有需求再处理
        public override void HideBanner()
        {
            if (!_isBannerShowing)
            {
                return;
            }

            _bannerAdapter.Hide();
            _isBannerShowing = false;
            BizzaEventSystem.Emit(EventDefine.AdEvent.BannerShowOrHide, false);
        }

        #endregion

        public NativeAdapterBase nativeAdapterBase;
        public override bool IsNativeValid { get; }
        public override bool IsNativeReady { get; }
        public override bool IsNativeShowing { get; }

        public override void ShowNative()
        {
        }

        public override void HideNative()
        {
        }

        /// <summary>
        /// 输出广告 SDK 调试信息（UnitId、就绪状态、ECPM 等），供 GM 或日志使用。
        /// </summary>
        public string GetAdSdkDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("========== 广告SDK 状态 ==========");
            sb.AppendLine($"Platform.IsInit: {OverseaPlatform.IsInit}");
            sb.AppendLine($"激励视频 就绪: {IsRewardReady}, 展示中: {IsRewardShowing}");
            sb.AppendLine($"插屏 就绪: {IsInterReady}, 展示中: {IsInterShowing}");
            sb.AppendLine($"开屏 就绪: {IsSplashReady}, 展示中: {IsSplashShowing}");
            sb.AppendLine($"Banner 就绪: {IsBannerReady}, 展示中: {IsBannerShowing}");
            sb.AppendLine("--- 开屏适配器 ---");
            if (_splashAdAdapters != null && _splashAdAdapters.Count > 0)
            {
                for (int i = 0; i < _splashAdAdapters.Count; i++)
                {
                    var v = _splashAdAdapters[i];
                    if (v == null) { sb.AppendLine($"  [{i}] (null)"); continue; }
                    sb.AppendLine($"  [{i}] {v.GetType().Name}  UnitId={v.AdUnitId ?? ""}  IsReady={v.IsReady}  ECPM={v.ECPM}");
                }
            }
            else
                sb.AppendLine("  (无)");
            sb.AppendLine("--- 激励视频适配器 ---");
            if (_rewardAdAdapters != null)
            {
                for (int i = 0; i < _rewardAdAdapters.Count; i++)
                {
                    var v = _rewardAdAdapters[i];
                    if (v == null) { sb.AppendLine($"  [{i}] (null)"); continue; }
                    sb.AppendLine($"  [{i}] {v.GetType().Name}  UnitId={v.AdUnitId ?? ""}  IsReady={v.IsReady}  ECPM={v.ECPM}");
                }
            }
            else
                sb.AppendLine("  (无)");
            sb.AppendLine("--- 插屏适配器 ---");
            if (_interAdAdapters != null)
            {
                for (int i = 0; i < _interAdAdapters.Count; i++)
                {
                    var v = _interAdAdapters[i];
                    if (v == null) { sb.AppendLine($"  [{i}] (null)"); continue; }
                    sb.AppendLine($"  [{i}] {v.GetType().Name}  UnitId={v.AdUnitId ?? ""}  IsReady={v.IsReady}  ECPM={v.ECPM}");
                }
            }
            else
                sb.AppendLine("  (无)");
            sb.AppendLine("====================================");
            return sb.ToString();
        }

        private void AdLoads()
        {
            switch (currentPlayAdType)
            {
                case E_AdType.InsertAd:
                    LoadInterAd();
                    break;
                case E_AdType.RewardAd:
                    LoadRewardAd();
                    break;
                case E_AdType.All:
                    LoadRewardAd();
                    LoadInterAd();
                    break;
            }
        }
    }
}
#endif
