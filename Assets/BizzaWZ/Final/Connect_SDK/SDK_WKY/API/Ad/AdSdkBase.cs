#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bizza.Sdk
{
    /// <summary>
    /// 广告sdk的接口，包含广告相关的全部功能，如各类型的广告播放
    /// </summary>
    public abstract class AdSdkBase
    {
    #region 广告

    public abstract void SetUserId(string userId);

    #region 开屏
    public abstract bool IsSplashInvalid { get; }
    public abstract bool IsSplashReady { get; }
    public abstract bool IsSplashShowing { get; }
    public abstract void ShowSplashAd();
    #endregion

    #region 激励视频
    public abstract bool IsRewardInvalid { get; }
    public abstract bool IsRewardReady { get; }
    public abstract bool IsRewardShowing { get; }
    public abstract bool CheckRewardAdReady(CheckAdReadyArgs args = default);
    public abstract void ShowRewardAd(ShowAdArgs args);
    #endregion

    #region 插屏/插页
    public abstract bool IsInterInvalid { get; }
    public abstract bool IsInterReady { get; }
    public abstract bool IsInterShowing { get; }
    public abstract bool CheckInterAdReady(CheckAdReadyArgs args = default);
    public abstract void ShowInterAd(ShowAdArgs args = default);
    #endregion

    #region Banner
    public abstract bool IsBannerInvalid { get; }
    public abstract bool IsBannerReady { get; }
    public abstract bool IsBannerShowing { get; }
    public abstract void ShowBanner();
    public abstract void HideBanner();
    #endregion

    #region Native
    public abstract bool IsNativeValid { get; }
    public abstract bool IsNativeReady { get; }
    public abstract bool IsNativeShowing { get; }

    public abstract void ShowNative();
    public abstract void HideNative();
    #endregion
    #endregion
    }
}
#endif
