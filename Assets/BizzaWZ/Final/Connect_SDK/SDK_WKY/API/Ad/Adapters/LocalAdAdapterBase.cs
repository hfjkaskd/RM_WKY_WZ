#if BIZZA_REAL_WITHDRAW && BIZZA_ENABLE_MAX && !BIZZA_HTTP_AD
using System;
using System.Collections;
using System.Collections.Generic;
using Bizza.Sdk;
using UnityEngine;

public abstract class LocalAdAdapterBase : VideoAdAdapterBase
{
    protected bool _adFlowFinish = true; //广告流程是否结束

    public override double ECPM => throw new NotImplementedException();

    public override E_AdType adType => throw new NotImplementedException();

    public override string AdUnitId => throw new NotImplementedException();

    public override bool IsValid => throw new NotImplementedException();

    public override bool IsReady => throw new NotImplementedException();

    protected bool CanPlayAd()//需要
    {
        LogLogger.LogInfo(" _adFlowFinish ： " + _adFlowFinish);
        return _adFlowFinish;
    }

    protected void OnAdFlowStart()//需要
    {
        _adFlowFinish = false;
    }

    protected void OnAdFlowEnd()
    {
        _adFlowFinish = true;
    }


    protected MaxSdkBase.AdInfo _simulateAdInfo;
    protected bool _simulateRewardPlaying;
    public const double simulateECPM = 0.00034d;

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
}

#endif
