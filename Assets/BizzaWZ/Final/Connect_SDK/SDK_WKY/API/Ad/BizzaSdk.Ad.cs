#if BIZZA_REAL_WITHDRAW
using System;
using Bizza.Sdk;
using UnityEngine;

namespace BizzaSdk
{
    /// <summary>
    /// 广告 SDK 统一入口，封装 Platform.Instance.AdSdk 的调用。
    /// </summary>
    public static class Ad
    {
        public static string curRewardAdPos = "";
        public static string curInterAdPos = "";

#if BIZZA_REAL_WITHDRAW
        public static bool Inited => OverseaPlatform.AdSdk != null && OverseaPlatform.AdSdk.Inited;

        private static AggregationAdSdk AdSdk => OverseaPlatform.AdSdk;

        private static bool CanShowOnlineAd => AccountModule.Instance?.AccountReady == true &&
            AdSdk != null && Application.internetReachability != NetworkReachability.NotReachable;

        private static void ShowOnlineAd(ShowAdArgs args, bool reward)
        {
            var account = AccountModule.Instance;
            var sdk = AdSdk;
            if (!CanShowOnlineAd)
            {
                Finish(args, default);
                return;
            }

            bool completed = false;
            var sdkArgs = args;
            sdkArgs.onFinish = Complete;
            sdkArgs.onFailed = () => Complete(default);
            try
            {
                if (reward) sdk.ShowRewardAd(sdkArgs);
                else sdk.ShowInterAd(sdkArgs);
            }
            catch (Exception error)
            {
                Debug.LogWarning("WKY ad request failed: " + error.GetType().Name);
                Complete(default);
            }

            void Complete(ShowAdResult result)
            {
                if (completed) return;
                completed = true;
                if (!ReferenceEquals(AccountModule.Instance, account) || !account.AccountReady)
                    result = default;
                Finish(args, result);
            }
        }

        private static void Finish(ShowAdArgs args, ShowAdResult result)
        {
            if (result.success)
            {
                args.onFinish?.Invoke(result);
                return;
            }

            try { UIUtils.ShowNetworkErrorTip(); }
            catch (Exception error) { Debug.LogWarning("WKY network tip unavailable: " + error.GetType().Name); }
            try { args.onFailed?.Invoke(); }
            finally { args.onFinish?.Invoke(result); }
        }

        public static void SetUserId(string userId) => AdSdk?.SetUserId(userId);

        #region 开屏
        public static bool IsSplashInvalid => AdSdk != null && AdSdk.IsSplashInvalid;
        public static bool IsSplashReady => AdSdk != null && AdSdk.IsSplashReady;
        public static bool IsSplashShowing => AdSdk != null && AdSdk.IsSplashShowing;
        public static void ShowSplashAd() => AdSdk?.ShowSplashAd();
        #endregion

        #region 激励视频
        public static bool IsRewardInvalid => AdSdk != null && AdSdk.IsRewardInvalid;
        public static bool IsRewardReady => CanShowOnlineAd && AdSdk.IsRewardReady;
        public static bool IsRewardShowing => AdSdk != null && AdSdk.IsRewardShowing;
        public static bool CheckRewardAdReady(CheckAdReadyArgs args = default) => CanShowOnlineAd && AdSdk.CheckRewardAdReady(args);
        public static void ShowRewardAd(ShowAdArgs args) => ShowOnlineAd(args, reward: true);
        public static void ShowRewardAd(string adPos, float dollarNum, Action<Bizza.Sdk.ShowAdResult> onFinish, float ecpmLimit = 0)
        {
            #if !COMMONGAME
            if (UIModule.Instance.isStatistics) SaveDataUtils.GameData.totalbeLookAdCount++;
            #else
            SaveDataUtils.GameData.totalbeLookAdCount++;
            #endif
            var showAdArgs = new Bizza.Sdk.ShowAdArgs()
            {
                adSource = E_AdsSource.Max,
                onFinish = onFinish,
                ecpmLimit = ecpmLimit,
                adPos = adPos.ToString(),
                beAdType = AdType.Reward,
                activeAdType = AdType.Reward,
                fakeDollarNum = dollarNum
            };
            ShowRewardAd(showAdArgs);
        }
        public static void ShowRewardAd(Action<Bizza.Sdk.ShowAdResult> onFinish) => ShowRewardAd(new ShowAdArgs(){onFinish = onFinish});
        #endregion

        #region 插屏/插页
        public static bool IsInterInvalid => AdSdk != null && AdSdk.IsInterInvalid;
        public static bool IsInterReady => CanShowOnlineAd && AdSdk.IsInterReady;
        public static bool IsInterShowing => AdSdk != null && AdSdk.IsInterShowing;
        public static bool CheckInterAdReady(CheckAdReadyArgs args = default) => CanShowOnlineAd && AdSdk.CheckInterAdReady(args);
        public static void ShowInterAd(ShowAdArgs args = default) => ShowOnlineAd(args, reward: false);
        public static void ShowInterAd(string adPos, float dollarNum, Action<Bizza.Sdk.ShowAdResult> onFinish, bool forceCount)
        {
            #if !COMMONGAME
            if (UIModule.Instance.isStatistics || forceCount) SaveDataUtils.GameData.totalbeLookAdCount++;
            #else
            SaveDataUtils.GameData.totalbeLookAdCount++;
            #endif
            var showAdArgs = new ShowAdArgs()
            {
                adSource = E_AdsSource.Max,
                onFinish = onFinish,
                adPos = adPos.ToString(),
                fakeDollarNum = dollarNum,
                beAdType = AdType.Interstitial,
                activeAdType = AdType.Interstitial
            };
            ShowInterAd(showAdArgs);
        }
        #endregion

        #region Banner
        public static bool IsBannerInvalid => AdSdk != null && AdSdk.IsBannerInvalid;
        public static bool IsBannerReady => AdSdk != null && AdSdk.IsBannerReady;
        public static bool IsBannerShowing => AdSdk != null && AdSdk.IsBannerShowing;
        public static void ShowBanner() => AdSdk?.ShowBanner();
        public static void HideBanner() => AdSdk?.HideBanner();
        #endregion

        #region Native
        public static bool IsNativeValid => AdSdk != null && AdSdk.IsNativeValid;
        public static bool IsNativeReady => AdSdk != null && AdSdk.IsNativeReady;
        public static bool IsNativeShowing => AdSdk != null && AdSdk.IsNativeShowing;
        public static void ShowNative() => AdSdk?.ShowNative();
        public static void HideNative() => AdSdk?.HideNative();
        #endregion

        /// <summary>
        /// 获取广告 SDK 调试信息（各适配器 UnitId、就绪状态、ECPM 等）。
        /// </summary>
        public static string GetAdSdkDebugInfo()
        {
            if (AdSdk == null) return "AdSdk 未初始化";
            return AdSdk.GetAdSdkDebugInfo();
        }
#else
        public static bool Inited => false;

        public static void SetUserId(string userId) { }

        #region 开屏
        public static bool IsSplashInvalid => true;
        public static bool IsSplashReady => false;
        public static bool IsSplashShowing => false;
        public static void ShowSplashAd() { }
        #endregion

        #region 激励视频
        public static bool IsRewardInvalid => true;
        public static bool IsRewardReady => false;
        public static bool IsRewardShowing => false;
        public static bool CheckRewardAdReady(CheckAdReadyArgs args = default) => false;
        public static void ShowRewardAd(ShowAdArgs args) => FinishAdAsFailed(args);
        public static void ShowRewardAd(string adPos, float dollarNum, Action<Bizza.Sdk.ShowAdResult> onFinish, float ecpmLimit = 0)
        {
            onFinish?.Invoke(new Bizza.Sdk.ShowAdResult { success = false });
        }
        public static void ShowRewardAd(Action<Bizza.Sdk.ShowAdResult> onFinish)
        {
            onFinish?.Invoke(new Bizza.Sdk.ShowAdResult { success = false });
        }
        #endregion

        #region 插屏/插页
        public static bool IsInterInvalid => true;
        public static bool IsInterReady => false;
        public static bool IsInterShowing => false;
        public static bool CheckInterAdReady(CheckAdReadyArgs args = default) => false;
        public static void ShowInterAd(ShowAdArgs args = default) => FinishAdAsFailed(args);
        public static void ShowInterAd(string adPos, float dollarNum, Action<Bizza.Sdk.ShowAdResult> onFinish)
        {
            onFinish?.Invoke(new Bizza.Sdk.ShowAdResult { success = false });
        }
        public static void ShowInterAd(string adPos, float dollarNum, Action<Bizza.Sdk.ShowAdResult> onFinish, bool forceCount)
            => ShowInterAd(adPos, dollarNum, onFinish);
        #endregion

        #region Banner
        public static bool IsBannerInvalid => true;
        public static bool IsBannerReady => false;
        public static bool IsBannerShowing => false;
        public static void ShowBanner() { }
        public static void HideBanner() { }
        #endregion

        #region Native
        public static bool IsNativeValid => false;
        public static bool IsNativeReady => false;
        public static bool IsNativeShowing => false;
        public static void ShowNative() { }
        public static void HideNative() { }
        #endregion

        public static string GetAdSdkDebugInfo() => "AdSdk disabled";

        private static void FinishAdAsFailed(ShowAdArgs args)
        {
            args.onFailed?.Invoke();
            args.onFinish?.Invoke(new Bizza.Sdk.ShowAdResult { success = false });
        }
#endif
    }
}
#endif
