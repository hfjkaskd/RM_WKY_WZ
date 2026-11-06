#if BIZZA_REAL_WITHDRAW
#if BIZZA_ENABLE_TENJIN
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardAd_AttributionInfo
{
    public float ECPM;
    public string AT_ID;
    public string A_UID;

    public string AdPlatform;
    public string AdFormat;
    public string AdNetwork;

}

public static class AttributionUtil
    {
        public static TenjinMono tenjin_Mono;
        public static BaseTenjin Tenjin_Instance;

        public static RewardAd_AttributionInfo RewardAdInfo = new RewardAd_AttributionInfo();

        public static void InitTenjin()
        {
            if (tenjin_Mono == null)
            {
                tenjin_Mono = new GameObject("TenjinMono").AddComponent<TenjinMono>();
            }
        }

        public static void DoTenjinConnect(BaseTenjin tenjin)
        {
            Tenjin_Instance = tenjin;
            _retryTimes = 0;
            _hasAttributionInfo = false;
            Tenjin_Instance.Connect();
            if (tenjin_Mono == null) InitTenjin();
            tenjin_Mono.StartCoroutine(DelaySecondsRealtime(5f, LoopCall));
        }

        private static IEnumerator DelaySecondsRealtime(float seconds, System.Action onComplete)
        {
            yield return new WaitForSecondsRealtime(seconds);
            onComplete?.Invoke();
        }

        private static int _retryTimes;
        private static bool _hasAttributionInfo;

        private static void LoopCall()
        {
            _retryTimes++;
            if (_hasAttributionInfo)
            {
                return;
            }
            DoConnect();
            if (tenjin_Mono == null) InitTenjin();
            tenjin_Mono.StartCoroutine(DelaySecondsRealtime(30f, () =>
            {
                if (_retryTimes > 5)
                {
                    Debug.LogError("重试5次仍然没有获取到 Tenjin GetAttributionInfo");
                    return;
                }
                LogLogger.LogInfo("重试 Tenjin GetAttributionInfo " + _retryTimes);
                if (!_hasAttributionInfo)
                {
                    LoopCall();
                }
            }));
        }

        private static void DoConnect()
        {
            if (_hasAttributionInfo)
            {
                return;
            }
            Tenjin_Instance.SubscribeAppLovinImpressions();
            Debug.Log("Tenjin DoConnect" + _retryTimes);

            Tenjin_Instance.GetAttributionInfo((x) =>
            {
                if (x == null || x.Count == 0)
                {
                    Debug.LogError("Tenjin GetAttributionInfo Get NULL");
                }
                else
                {
                    Debug.Log("Tenjin初始化成功-----可以开始初始化H5---");
                    UploadAdNetwork(x);
                }
            });
        }

        public static void UploadAdNetwork(Dictionary<string, string> attributionInfoData)
        {
            _hasAttributionInfo = true;

            if (attributionInfoData.ContainsKey("advertising_id"))
                BizzaSdk.Analysis.SetUserProperty("advertising_id", attributionInfoData["advertising_id"]);

            if (attributionInfoData.ContainsKey("ad_network"))
                BizzaSdk.Analysis.SetUserProperty("ad_network", attributionInfoData["ad_network"]);

            if (attributionInfoData.ContainsKey("campaign_id"))
                BizzaSdk.Analysis.SetUserProperty("campaign_id", attributionInfoData["campaign_id"]);

            if (attributionInfoData.ContainsKey("campaign_name"))
                BizzaSdk.Analysis.SetUserProperty("campaign_name", attributionInfoData["campaign_name"]);

            if (attributionInfoData.ContainsKey("site_id"))
                BizzaSdk.Analysis.SetUserProperty("site_id", attributionInfoData["site_id"]);

            if (attributionInfoData.ContainsKey("creative_name"))
                BizzaSdk.Analysis.SetUserProperty("creative_name", attributionInfoData["creative_name"]);

            if (attributionInfoData.ContainsKey("remote_campaign_id"))
                BizzaSdk.Analysis.SetUserProperty("remote_campaign_id", attributionInfoData["remote_campaign_id"]);

            Debug.Log("Tenjin Back - UploadAdNetwork");
            BizzaSdk.Analysis.UploadUserProperty();
            string channel = attributionInfoData["ad_network"];

            if (!PlayerPrefs.HasKey("channel"))
            {
                // Debug.Log("H5的初始化" + attributionInfoData["ad_network"]);
                PlayerPrefs.SetString("channel", channel);
                // IDData.H5Init();
            }
        }

        
        
        public static void ReportADShow(string adType, string adId)
        {
            Debug.Log("ReportADShow");
            BizzaSdk.Analysis.AddParam("ad_type", adType);
    
            BizzaSdk.Analysis.AddParam("placement_id", adId);
            BizzaSdk.Analysis.SendCustomEvent("Ad_Show");
        }


        public static void ReportADRevenue(string country, double publisher_revenue, string network_name,
            string adunit_id, string adunit_format, string adsource_id,string platform)
        {
            Debug.Log("ReportADRevenue");
            Debug.Log("上报数数的参数"+"countrycode"+country+"revenue"+publisher_revenue+"networkname"+network_name+"adunitid"+ adunit_format+"adsourceid"+adsource_id+"platform"+platform+"adformat"+adunit_format);
            BizzaSdk.Analysis.AddParam("countrycode", country);
            BizzaSdk.Analysis.AddParam("revenue", publisher_revenue);
            BizzaSdk.Analysis.AddParam("networkname", network_name);
            BizzaSdk.Analysis.AddParam("adunitid", adunit_id);
            BizzaSdk.Analysis.AddParam("adformat", adunit_format);
            BizzaSdk.Analysis.AddParam("adsource", adsource_id);
            BizzaSdk.Analysis.AddParam("ADplatform", platform);
            BizzaSdk.Analysis.SendCustomEvent("Ad_Revenue");
        }

        public static void ReportADRevenueForBackup(string country, double publisher_revenue, string network_name,
            string adunit_id, string adunit_format, string adsource_id,string platform)
        {
            Debug.Log("ReportADRevenue");
            Debug.Log("上报数数的参数"+"countrycode"+country+"revenue"+publisher_revenue+"networkname"+network_name+"adunitid"+ adunit_format+"adsourceid"+adsource_id+"platform"+platform+"adformat"+adunit_format);
            BizzaSdk.Analysis.AddParam("countrycode", country);
            BizzaSdk.Analysis.AddParam("revenue", publisher_revenue);
            BizzaSdk.Analysis.AddParam("networkname", network_name);
            BizzaSdk.Analysis.AddParam("adunitid", adunit_id);
            BizzaSdk.Analysis.AddParam("adformat", adunit_format);
            BizzaSdk.Analysis.AddParam("adsource", adsource_id);
            BizzaSdk.Analysis.AddParam("ADplatform", platform);
            BizzaSdk.Analysis.SendCustomEvent("OnRewardedAdRevenuePaidEvent");
        }

        public static void ReportADRevenueOnHidden(string country, double publisher_revenue, string network_name,
            string adunit_id, string adunit_format, string adsource_id,string platform)
        {
            BizzaSdk.Analysis.AddParam("countrycode", country);
            BizzaSdk.Analysis.AddParam("revenue", publisher_revenue);
            BizzaSdk.Analysis.AddParam("networkname", network_name);
            BizzaSdk.Analysis.AddParam("adunitid", adunit_id);
            BizzaSdk.Analysis.AddParam("adformat", adunit_format);
            BizzaSdk.Analysis.AddParam("adsource", adsource_id);
            BizzaSdk.Analysis.AddParam("ADplatform", platform);
            BizzaSdk.Analysis.SendCustomEvent("Ad_Hidden");
        }
    }
    #endif
#endif
