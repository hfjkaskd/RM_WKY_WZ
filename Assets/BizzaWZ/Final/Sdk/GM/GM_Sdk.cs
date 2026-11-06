#if BIZZA_REAL_WITHDRAW
using System.ComponentModel;
using Bizza.Sdk;
using BizzaSdk;
using UnityEngine;

public partial class SROptions
{
    private const string SdkCategory = "Sdk/广告";

    [Category(SdkCategory)]
    [DisplayName("播放激励视频")]
    public void Sdk_ShowRewardAd()
    {
        var money = new ItemEntry() { Type = E_ItemType.Dollar, Count = 100f };
        BizzaSdk.Ad.ShowRewardAd(E_AdPos.LocalTest.ToString(), money.Count, (success) =>
        {
            LogLogger.LogAdInfo("激励广告成功");
        }
        );
    }

    [Category(SdkCategory)]
    [DisplayName("播放插屏广告")]
    public void Sdk_ShowInterAd()
    {
        var money = new ItemEntry() { Type = E_ItemType.Dollar, Count = 100f };
        BizzaSdk.Ad.ShowInterAd(E_AdPos.LocalTest.ToString(), money.Count, (success) =>
        {
            LogLogger.LogAdInfo("插屏广告成功");
        }, false
        );
    }

    [Category(SdkCategory)]
    [DisplayName("切换插屏准备状态")]
    public void InterestAdReay()
    {
        ChannelConfig.Instance.real_CustomConfig.interAdNotReady = !ChannelConfig.Instance.real_CustomConfig.interAdNotReady;
        LogLogger.LogADPR($"插屏广告准备状态:{ChannelConfig.Instance.real_CustomConfig.interAdNotReady}");

    }

    [Category(SdkCategory)]
    [DisplayName("切换激励准备状态")]
    public void RewardAdReay()
    {
        ChannelConfig.Instance.real_CustomConfig.rewardAdNotReady = !ChannelConfig.Instance.real_CustomConfig.rewardAdNotReady;
        LogLogger.LogADPR($"激励广告准备状态:{ChannelConfig.Instance.real_CustomConfig.rewardAdNotReady}");
    }

    [Category(SdkCategory)]
    [DisplayName("激励是否就绪")]
    public bool Sdk_IsRewardReady => Ad.IsRewardReady;

    [Category(SdkCategory)]
    [DisplayName("插屏是否就绪")]
    public bool Sdk_IsInterReady => Ad.IsInterReady;

    [Category(SdkCategory)]
    [DisplayName("显示Banner")]
    public void Sdk_ShowBanner()
    {
        Ad.ShowBanner();
    }

    [Category(SdkCategory)]
    [DisplayName("隐藏Banner")]
    public void Sdk_HideBanner()
    {
        Ad.HideBanner();
    }

    [Category(SdkCategory)]
    [DisplayName("开屏广告")]
    public void Sdk_ShowSplashAd()
    {
        Ad.ShowSplashAd();
    }

    [Category(SdkCategory)]
    [DisplayName("Native广告-显示")]
    public void Sdk_ShowNative()
    {
        Ad.ShowNative();
    }

    [Category(SdkCategory)]
    [DisplayName("Native广告-隐藏")]
    public void Sdk_HideNative()
    {
        Ad.HideNative();
    }

    [Category(SdkCategory)]
    [DisplayName("输出广告SDK信息")]
    public void Sdk_LogAdSdkInfo()
    {
        string info = Ad.GetAdSdkDebugInfo();
        Debug.Log(info);
    }
}
#endif
