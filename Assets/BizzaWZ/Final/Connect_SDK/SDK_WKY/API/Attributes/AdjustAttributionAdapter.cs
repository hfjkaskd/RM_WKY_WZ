#if BIZZA_REAL_WITHDRAW
#if BIZZA_ENABLE_ADJUST

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdjustSdk;
using Bizza.GameAnalytics;

namespace Bizza.Sdk
{
    public class AdjustAttributionAdapter
    {
        public static string adJustAdidKey => AccountModule.adJustAdidKey;
        public static string adJustGooglIdKey => AccountModule.adJustGooglIdKey;
        public static AdjustConfig adjustConfig;

        /// <summary>
        /// 初始化归因
        /// </summary>
        public static void AttributeStart(string eventName)
        {
            if (adjustConfig != null)
            {
                return;
            }

#if DEBUG_MODE
            adjustConfig = new AdjustConfig(
                    ChannelConfig.Instance.adjustKey,
                    AdjustEnvironment.Sandbox,
                    allowSuppressLogLevel: true
                );
            LogLogger.LogPlatformInit($"Adjust 是沙盒模式 ： {AdjustEnvironment.Sandbox}");
#else
            adjustConfig = new AdjustConfig(
                    ChannelConfig.Instance.adjustKey,
                    AdjustEnvironment.Production,
                    allowSuppressLogLevel: true
                );
            LogLogger.LogPlatformInit($"Adjust 是正式模式 ： {AdjustEnvironment.Production}");
#endif

            // 设置开始日志等级
            // AdjustLogLevel.Verbose	启用完整日志
            // AdjustLogLevel.Suppress	禁止所有日志
            adjustConfig.LogLevel = AdjustLogLevel.Verbose;

            var cloudDeviceInfo = DeviceInfoUtil.GetDeviceInfoDataForCloud();
            LogLogger.LogVerbose(LogTag.ADAttribute, "设备的安卓ID可用:" + !string.IsNullOrEmpty(cloudDeviceInfo.Os_Anid));
            // 外部设备ID
            adjustConfig.ExternalDeviceId = cloudDeviceInfo.Os_Anid;

            // SDK在应用在后台运行时发送信息
            adjustConfig.IsSendingInBackgroundEnabled = false;

            // 在归因中接收广告支出数据
            adjustConfig.IsCostDataInAttributionEnabled = true;

            adjustConfig.AttributionChangedDelegate = (attribution) =>
            {
                AttributionChangedDelegate(attribution, eventName);
            };

            Adjust.InitSdk(adjustConfig);
            LogLogger.LogAttribute($"Adjust初始化");
            var adid = PlayerPrefs.GetString(adJustAdidKey, "");
            if (string.IsNullOrEmpty(adid))
            {
                Adjust.GetAdid(idfa =>
                {
                    PlayerPrefs.SetString(adJustAdidKey, idfa);
                    LogLogger.LogVerbose(LogTag.LOG_Attribute, $"GetAdid 可用:{!string.IsNullOrEmpty(idfa)}");
                });
            }

            Adjust.GetGoogleAdId(googleId =>
            {
                PlayerPrefs.SetString(adJustGooglIdKey, googleId);
                LogLogger.LogVerbose(LogTag.LOG_Attribute, $"GetGoogleAdId 可用:{!string.IsNullOrEmpty(googleId)}");
            });
        }

        public static string networkKey = "network";
        public static string campaignKey = "campaign";
        public static string adgroupkKey = "adgroup";

        public static Dictionary<string, object> oceanShineUserAttrs = new Dictionary<string, object>();

        public static void AttributionChangedDelegate(AdjustAttribution attribution, string eventName)
        {
            var network = attribution?.Network ?? "";
            var campaign = attribution?.Campaign ?? "";
            var adgroup = attribution?.Adgroup ?? "";

            LogLogger.LogAttribute($"Adjust数据发生变化 ::: networkKey {network} campaignKey {campaign} adgroupkKey {adgroup}");
            PlayerPrefs.SetString(networkKey, network);
            PlayerPrefs.SetString(campaignKey, campaign);
            PlayerPrefs.SetString(adgroupkKey, adgroup);
            string country = AccountModule.CountryType == AccountModule.E_CountryType.None
                ? null
                : AccountModule.CountryType.ToString();
            BizzaGameAnalytics.SetAttribution(network, campaign, country);

            oceanShineUserAttrs = AccountModule.Instance.CreateFromJson(attribution, eventName);
            AccountModule.Instance.Request_UserAttrsRequest(oceanShineUserAttrs);
        }


        public static string AD_REVENUE_APPLOVIN_MAX = "applovin_max_sdk";
        public static void ReportAdShowForAdjust(double revenue, string currency, string AdRevenueNetwork, string AdRevenueUnit, string AdRevenuePlacement)
        {
#if BIZZA_ENABLE_ADJUST
            AdjustAdRevenue adjustAdRevenue = new AdjustAdRevenue(AD_REVENUE_APPLOVIN_MAX);
            adjustAdRevenue.SetRevenue(revenue, currency);
            adjustAdRevenue.AdRevenueNetwork = AdRevenueNetwork;
            adjustAdRevenue.AdRevenueUnit = AdRevenueUnit;
            adjustAdRevenue.AdRevenuePlacement = AdRevenuePlacement;


            LogLogger.LogAttribute($"Adjust收益上报 revenue:{revenue}; currency:{currency}; AdRevenueNetwork:{AdRevenueNetwork}; AdRevenueUnit:{AdRevenueUnit}; AdRevenuePlacement:{AdRevenuePlacement}");
            Adjust.TrackAdRevenue(adjustAdRevenue);
#endif
        }
    }
}
#endif
#endif
