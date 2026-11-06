#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bizza.Sdk
{
        [Obfuz.ObfuzIgnore]
        public class ChannelConfig
        {
                public static ChannelConfig Instance;
                public ChannelConfig()
                {
                        Instance = this;
                        // 补充一个从StreamingAssets读取的配置的操作
                }

                public static bool IsRelese_Mode
                {
                        get
                        {
#if DEBUG_MODE
                                return false;
#endif
                                return true;
                        }
                }

                #region 基础配置

                public string AppId = "";

                public bool GM;

                // 仅在编辑器下生效
                public bool RecordMode = false;

                #endregion


                #region 真提http
#if BIZZA_REAL_WITHDRAW
                [Space(5)]
                public HttpDataConfig httpConfig = new();

                [Space(5)]
                public bool isReportServer = false;
                public bool isEditorReportServer = false;

                public double incomeRate = 1.0;
                public float GetFreeDollarRewardRate = 0.6f;
                public const int DefaultSynthesisRewardMinRemainingCount = 5;
                // 剩余合成次数小于此值时跳过合成奖励检查；0 表示不限制。
                [Min(0)]
                public int SynthesisRewardMinRemainingCount = DefaultSynthesisRewardMinRemainingCount;

                [Space(5)]
                public Real_CustomConfig real_CustomConfig = new();

                public List<AdStatisticsLevelRange> adStatisticsLevelRanges = new();
#endif
                #endregion

                #region 归因

                public string adjustKey = "";

                #endregion


                #region 广告

                public List<AdConfig> sourceAds = new();
                public bool useInterReplenishReward;
                public bool useRewardReplenishInter;
                #endregion


                /// <summary>
                /// 获取广告参数配置
                /// </summary>
                /// <param name="type"></param>
                /// <returns></returns>
                public AdConfig GetAdsConfig(E_AdsSource type)
                {
                        foreach (var cfg in sourceAds)
                        {
                                if (cfg.AdsSource == type)
                                {
                                        return cfg;
                                }
                        }
                        LogLogger.LogError("没有这个广告源，注意配置信息");
                        return null;
                }

#if DEBUG_MODE
                public string GetAllParamsString()
                {
                        string ads = "";

                        if (sourceAds != null)
                        {
                                foreach (var ad in sourceAds)
                                {
                                        if (ad == null) continue;

                                        ads += $"[{ad.AdsSource}:R={ad.rewardAdId},I={ad.interAdId},B={ad.bannerAdId},O={ad.openAdId}]";
                                }
                        }

                        string adStatisticsRanges = "";
                        if (adStatisticsLevelRanges != null)
                        {
                                foreach (var range in adStatisticsLevelRanges)
                                {
                                        if (range == null) continue;
                                        adStatisticsRanges +=
                                                $"[{range.StartLevel}-{range.EndLevel}:" +
                                                $"Close={range.Statistics.CloseGetRewardCount}," +
                                                $"Show={range.Statistics.ShowGetRewardCount}," +
                                                $"Dollar={range.Statistics.ShowDollarCount}," +
                                                $"Cooldown={range.Statistics.InterAdCooldownMs}," +
                                                $"InterStart={range.Statistics.InterAdStartLevel}," +
                                                $"ReviveStart={range.Statistics.ReviveAdStartLevel}]";
                                }
                        }

                        return $"AppId={AppId}|GM={GM}|domain={httpConfig?.domain}|aes_key={httpConfig?.aes_key}" +
                               $"|isReportServer={isReportServer}|isEditorReportServer={isEditorReportServer}" +
                               $"|incomeRate={incomeRate}|adjustKey={adjustKey}" +
                               $"|useInterReplenishReward={useInterReplenishReward}" +
                               $"|useRewardReplenishInter={useRewardReplenishInter}" +
                               $"|AdStatisticsRanges={adStatisticsRanges}|Ads={ads}";
                }
#endif
        }

#if BIZZA_REAL_WITHDRAW
        [Serializable]
        [Obfuz.ObfuzIgnore]
        public class AdStatisticsLevelRange
        {
                [Min(1)] public int StartLevel = 1;
                [Min(1)] public int EndLevel = 1;
                public GameAB_CustomData Statistics;
        }
#endif

        #region 广告配置
        [Serializable]
        [Obfuz.ObfuzIgnore]

        public class AdConfig
        {
                [Tooltip("广告源")]
                public E_AdsSource AdsSource;

                [Tooltip("奖励广告ID")]
                public string rewardAdId = "";

                [Tooltip("插屏广告ID")]
                public string interAdId = "";

                [Tooltip("横幅广告ID")]
                public string bannerAdId = "";

                [Tooltip("开屏广告ID")]
                public string openAdId = "";

                // 构造函数初始化广告源
                public AdConfig(E_AdsSource adsSource)
                {
                        AdsSource = adsSource;
                }

                // 判断广告源是否被选择
                private bool IsMaxSelected => AdsSource == E_AdsSource.Max;
                private bool IsTopOnSelected => AdsSource == E_AdsSource.TopOn;
        }

        #endregion

#if BIZZA_REAL_WITHDRAW
        [Serializable]
        [Obfuz.ObfuzIgnore]
        public struct Real_CustomConfig
        {
                public bool singleCurrencyMode;
                public bool realWithdrawPassMode;
                [SerializeField] private AccountModule.E_CountryType defaultCountry;
                public AccountModule.E_CountryType DefaultCountry
                {
                        get { return defaultCountry; }
                        set { defaultCountry = value; }
                }
                public string DefaultCountryName => defaultCountry.ToString();
                [SerializeField] private AccountModule.E_CountryType country;
                public AccountModule.E_CountryType Country
                {
                        get { return country; }
                        set { country = value; }
                }
                public string CountryName
                {
                        get
                        {
#if DEBUG_MODE
                                return country.ToString();
#else
                                Debug.Log("正式包国家被赋值为NONE");
                                return AccountModule.E_CountryType.None.ToString();
#endif
                        }
                }

                public bool useEditorUserId;

                public string editorUserId;

                public bool testECPM1000;

                public float TestECPMValue;

                public bool openTestDevice;

                public string[] testDeviceIds;

                //  = "Assets/BizzaWZ/Real/Tools/GenereatedInfo"
                public string GenerateFakeAndroidId;

                public bool enterNewbieGuide;

                public bool failOpenAd;

                public string versionLog;

                public bool interAdNotReady;

                public bool rewardAdNotReady;

        }
#endif
        [Serializable]
        [Obfuz.ObfuzIgnore]
        public class HttpDataConfig
        {
                // 指定服务器的基础 URL（即域名）。这个参数通常用于构建完整的请求 URL，比如在 RequestToServer 中会根据这个 domain 参数与请求路径拼接成完整的 API URL
                public string domain = ""; // 然后在请求时会将这个 URL 用作目标地址

                // 用于 AES 加密和解密的密钥。aes_key 是一个用于对请求数据进行加密、对响应数据进行解密的关键参数。此密钥会与请求的参数一起使用，确保数据在传输过程中不被泄露
                public string aes_key = ""; // 是一个 32 字节的密钥（通常是一个十六进制字符串），用于 AES 加密算法中的对称加密。注意：密钥应该保密
        }

}
#endif
