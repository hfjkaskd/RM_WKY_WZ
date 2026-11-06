#if BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_MAX && BIZZA_HTTP_AD
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using Newtonsoft.Json;
using UnityEngine;

namespace Bizza.Sdk
{
     
    public abstract class HttpAdAdapterBase : VideoAdAdapterBase
    {
        #region http

        private AccountModule.OceanShineAdLogReportRequest loadedAdInfo = new();
        protected string _cachedBatchId;
        protected bool _isLoadingBatchId = false;
        protected bool _adFlowFinish = true; //广告流程是否结束
        
        

        public string CachedBatchId
        {
            get => _cachedBatchId;
        }

        protected bool CanPlayAd()//需要
        {
            LogLogger.LogInfo("CanPlayAd 阶段");
            LogLogger.LogInfo(" _adFlowFinish ： " + _adFlowFinish);
            if (!_adFlowFinish)
                return false;
            LogLogger.LogInfo(" _isLoadingBatchId ： " + _isLoadingBatchId);
            LogLogger.LogInfo(" string.IsNullOrEmpty(_cachedBatchId) ： " + string.IsNullOrEmpty(_cachedBatchId));
            if (_isLoadingBatchId || string.IsNullOrEmpty(_cachedBatchId))
            {
                CheckLoadBatchId();
                LogLogger.LogInfo("CanPlayAd 阶段 返回 false ： ");
                return false;
            }

            LogLogger.LogInfo("CanPlayAd 阶段 返回 true ： ");
            return true;
        }

        protected void OnAdFlowStart()//需要
        {
            _adFlowFinish = false;
        }

        //广告流程结束：完成/中途失败
        protected void OnAdFlowEnd()
        {
            _adFlowFinish = true;
            _reportLoadedEvent = false;
            _cachedBatchId = "";
            CheckLoadBatchId();
        }

        protected void OnAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)//需要
        {
            LogLogger.LogInfo($"OnAdLoaded {adUnitId} {_cachedBatchId}");

            double _ecpm = EcpmUtils.NormalizeEcpm(adInfo.Revenue);// 1000f; //
#if UNITY_EDITOR
            var simulateItem = GenerateSimulateAdInfo();
            _ecpm =  EcpmUtils.NormalizeEcpm(simulateItem.Revenue);
            adInfo = simulateItem;
#endif
            // var MAXadId = adType == E_AdType.RewardAd ? ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).rewardAdId :
                // ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).interAdId;;
            loadedAdInfo.Os_Epm.Os_Abd = adType == E_AdType.RewardAd ? AccountModule.Instance.ADRewardType : AccountModule.Instance.ADInterstitialType;
            loadedAdInfo.Os_Epm.Os_Cid = adUnitId;
            loadedAdInfo.Os_Epm.Os_Cmp = PlayerPrefs.GetString(AccountModule.campaignKey, "");
            loadedAdInfo.Os_Epm.Os_Ctc = DeviceInfoUtil.deviceInfoDataForCloud.Os_Ctr.ToString();
            loadedAdInfo.Os_Epm.Os_Ecm = EcpmUtils.SwitchToStringForEcpm(_ecpm);
            loadedAdInfo.Os_Epm.Os_Evt = AccountModule.Instance.ADLoad;
            loadedAdInfo.Os_Epm.Os_Mbc = adInfo.NetworkPlacement;
            loadedAdInfo.Os_Epm.Os_Mbp = adInfo.NetworkName;
            loadedAdInfo.Os_Epm.Os_Adgp = PlayerPrefs.GetString(AccountModule.adgroupkKey, "");
            loadedAdInfo.Os_Epm.Os_Nbt = PlayerPrefs.GetString(AccountModule.networkKey, "");
            loadedAdInfo.Os_Epm.Os_Ptf = "Max";

            CheckReportAdLoaded();
        }

        private bool _reportLoadedEvent;
        protected void CheckReportAdLoaded()
        {
            if (_reportLoadedEvent)
            {
                return;
            }

            if (!_adFlowFinish)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_cachedBatchId) && IsReady && !string.IsNullOrEmpty(loadedAdInfo.Os_Epm.Os_Abd))
            {
                Request_AdLoadReport(adType);
                _reportLoadedEvent = true;
            }
        }

        public void Request_AdLoadReport(E_AdType adtype)
        {
            LogLogger.LogVerbose($"上报广告加载事件 {adtype} {_cachedBatchId}");
            loadedAdInfo ??= new();
            loadedAdInfo.Os_Epm.Os_Btid = _cachedBatchId;
            loadedAdInfo.Os_Cnf = new AccountModule.OceanShineAdLogReportRequest.CommonInfo()
            {
                Os_Apid = DeviceInfoUtil.GetDeviceInfoDataForCloud().Os_Apid,
                Os_Usid = PlayerPrefs.GetString(AccountModule.m_userIdKey),
                Os_Vn = AccountModule.version
            };
            LogLogger.LogVerbose(LogTag.ADReportFlow,
                "上报前" + adtype.ToString() + "广告上报 ：" + loadedAdInfo.Os_Epm.Os_Btid);
            LogLogger.LogADId("Max广告Id加载上报" + " BatchId " + loadedAdInfo.Os_Epm.Os_Btid);
            string requestParams = JsonConvert.SerializeObject(loadedAdInfo);
            LogLogger.LogADPR($"{adtype}广告上报 ：{loadedAdInfo.Os_Epm.Os_Btid} ecmp {loadedAdInfo.Os_Epm.Os_Ecm} evt {loadedAdInfo.Os_Epm.Os_Evt}");
            loadedAdInfo.Clear();
            HttpUtil.RequestToServer(AccountModuleCfg.ad_info, requestParams, null, false);
        }

        protected void CheckLoadBatchId()
        {
            LogLogger.LogVerbose(LogTag.ADReportFlow, $"尝试加载batchid {_cachedBatchId} {_isLoadingBatchId}");
            if (string.IsNullOrEmpty(_cachedBatchId) && !_isLoadingBatchId)
            {
                LogLogger.LogVerbose(LogTag.ADReportFlow, $"加载batchid {_cachedBatchId} {_isLoadingBatchId}");
                _isLoadingBatchId = true;
                AccountModule.Instance.Request_BatchId((respon) =>
                {
                    _isLoadingBatchId = false;
                    if (respon.success && respon.data != null)
                    {
                        _cachedBatchId = respon.data.Os_Btid;
                    }
                    else
                    {
                        _cachedBatchId = "";
                        GameUtils.DelayDo(() =>
                        {
                            CheckLoadBatchId();
                        }, 10);
                    }
                    LogLogger.LogVerbose(LogTag.ADReportFlow, $"设置batchId {_cachedBatchId} {_isLoadingBatchId}");

                    CheckReportAdLoaded();
                });
            }
        }


        protected void OnAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogLogger.LogVerbose(LogTag.ADReportFlow, $"广告展示 {_cachedBatchId} {_isLoadingBatchId}");
        }

        #region 编辑器测试
        protected MaxSdkBase.AdInfo _simulateAdInfo;
        protected bool _simulateRewardPlaying;
        public const double simulateECPM = 0.0034d;

        protected MaxSdkBase.AdInfo GenerateSimulateAdInfo()
        {
            var adInfoDictionary = new Dictionary<string, object>
            {
                ["adUnitId"] = "YOUR_AD_UNIT_ID",
                ["adFormat"] = "INTERSTITIAL", // 例如: BANNER / MREC / INTERSTITIAL / REWARDED / NATIVE
                ["networkName"] = "AppLovin",
                ["networkPlacement"] = "default",
                ["creativeId"] = "creative_12345",
                ["placement"] = "level_end", // 你自己在展示时传的 placement
                ["revenue"] = simulateECPM, // double
                ["revenuePrecision"] = "exact", // 例如: exact / estimated / publisher_defined（按你实际）
                ["waterfallInfo"] = new Dictionary<string, object>
                {
                    // WaterfallInfo 的字段按你项目 WaterfallInfo 构造函数需要的 key 来补
                    // 这里给一个常见的示例结构（如果你那边字段不同，按需改 key）
                    ["name"] = "default_waterfall",
                    ["testName"] = "A/B_01",
                    ["networkResponses"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["networkName"] = "AppLovin",
                            ["adLoadState"] = "loaded",
                            ["latencyMillis"] = 120L
                        },
                        new Dictionary<string, object>
                        {
                            ["networkName"] = "UnityAds",
                            ["adLoadState"] = "no_fill",
                            ["latencyMillis"] = 250L
                        }
                    }
                },
                ["latencyMillis"] = 345L, // long
                ["dspName"] = "applovin_dsp" // string
            };

            var ret = new MaxSdkBase.AdInfo(adInfoDictionary);
            return ret;
        }

        #endregion
        #endregion
    }
}
#endif
