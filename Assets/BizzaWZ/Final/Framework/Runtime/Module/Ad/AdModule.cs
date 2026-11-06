#if BIZZA_REAL_WITHDRAW
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using Bizza.Channel;
// using Obfuz;
// using UnityEngine;
//
// public class AdModule : BaseGameModule<AdModule>
// {
//
//     public static string LastShow_RewardAdEvent = "";
//     public static string LastShow_InsertAdEvent = "";
//     public static string LastShow_SplashAdEvent = "";
//
//     private Action _successCallback;
//
//     public override void InitGameModule()
//     {
//         EventModule.AddListener(E_GameEvent.AdLoadFail,DelayLoad);//初始化失败两秒后再次重试直到成功
//     }
//
//     public override void ReleaseGameModule()
//     {
//         EventModule.RemoveListener(E_GameEvent.AdLoadFail,DelayLoad);//初始化失败两秒后再次重试直到成功
//     }
//
//
//
//     public void DelayLoad()
//     {
//         if (RemoteConfigModule.reviewMode)
//         {
//             return;
//         }
//         Invoke(nameof(LoadAd), 1);
//     }
//
//      
//     public void LoadAd()
//     {
// #if Bizza_Platform_Oversea
//         OverseaPlatform.Instance.LoadAds();
// #endif
//     }
//
//     public static bool bPlayingAd = false;
//     public static void OnVideoAdStart()
//     {
//          // LogUtil.Verbose( // LogUtil.LOG_AD, "OnVideoAdStart");
//         bPlayingAd = true;
//     }
//
//     public static void OnVideoAdEnd()
//     {
//          // LogUtil.Verbose( // LogUtil.LOG_AD, "OnVideoAdEnd");
//         bPlayingAd = false;
//     }
//
//     public static void OnInterAdStart()
//     {
//          // LogUtil.Verbose( // LogUtil.LOG_AD, "OnInterAdStart");
//         bPlayingAd = true;
//     }
//
//     public static void OnInterAdEnd()
//     {
//          // LogUtil.Verbose( // LogUtil.LOG_AD, "OnInterAdEnd");
//         bPlayingAd = false;
//
//         LastInterAdTime = Time.realtimeSinceStartup;
//     }
//
//     public static float LastInterAdTime;
//
//     /// <summary>
//     /// 请求播放广告
//     /// </summary>
//     /// <param name="adsEventName"></param>
//     /// <param name="closeCallback"></param>
//     public static void OpenRewardAds(ShowRewardAdArgs args)
//     {
//         #if UNITY_EDITOR
//         args.callback?.Invoke(true);
//         // closeCallback?.Invoke(true, default);
//         // EditorAddCurrency();
//         return;
//         #endif
//         var platform = Bizza.Channel.Platform.Instance;
//         if (platform == null)
//         {
//              // LogUtil.Verbose( // LogUtil.LOG_AD, "错误+ Bizza.Channel.Platform 未实例化");
//             args.callback?.Invoke(false);
//             return;
//         }
//
//         if (platform == null || !platform.CanPlayRewardAd)
//         {
//             UIUtils.ShowTips(LanguageUtils.GetText("DailyMissionPanel_NoAd"));
//             platform.LoadAds(E_AdsSource.Max, E_AdType.RewardAd);
//             // closeCallback?.Invoke(false, default);
//             return;
//         }
//
//         //  // LogUtil.Error("当前状态 isCanShowRewardAd " + !CloudPlatform.isCanShowRewardAd);
//         // if (!CloudPlatform.isCanShowRewardAd)
//         // {
//             // UIUtils.ShowTips(LanguageUtils.GetText("DailyMissionPanel_NoAd"));
//             // return;
//         // }
//
//
//          // LogUtil.Verbose( // LogUtil.LOG_AD, "流程 +  真网赚 准备开始播放广告");
//         // 开始激励视频阻断
//
//         LastShow_RewardAdEvent = adsEventName;
//
//          // LogUtil.Verbose(BaseConst.LOG_ADBUg,$"开始播放广告 ");
//         platform.ShowAds(E_AdsSource.Max, adsEventName, closeCallback, isLimitEcpm, showHint);
//         // AccountModule.Instance.GetRewardRevenueId(GetData);
//
//         // void GetData(FailHttpResponse<AccountModule.OceanShineGetAdRevenueReportIdResponse> response)
//         // {
//         //     if (response.success)
//         //     {
//         //         CurrencyBar.OnEnQueue(response.data.Os_Btid, WithdrawalUtil.GetDollarCountByReward());
//         //          // LogUtil.Verbose( // LogUtil.LOG_ADBUg,$"开始播放广告 ");
//         //         platform.ShowAds(E_AdsSource.Max, adsEventName, response.data.Os_Btid, closeCallback, isLimitEcpm, showHint);
//         //     }
//         //     else
//         //     {
//         //         closeCallback?.Invoke(false, null);
//         //     }
//         //
//         //     //EventModule.BroadCast(E_GameEvent.ShowMask, false);
//         // }
//     }
//
//
//     /// <summary>
//     /// 播放无奖励广告 --- 插屏广告
//     /// </summary>
//     public void ShowInterstitialAd(ShowInterAdArgs args)
//     {
// #if UNITY_EDITOR
//         args.callback?.Invoke(true);
//         return;
// #endif
//         var platform = Bizza.Channel.Platform.Instance;
//         if (platform == null)
//         {
//              // LogUtil.Verbose( // LogUtil.LOG_AD, "错误+ Bizza.Channel.Platform 未实例化");
//             return;
//         }
//
//          // LogUtil.Verbose( // LogUtil.LOG_AD, "流程 +  真网赚 插屏广告");
//
//         if (platform.CanPlayInterAd)
//         {
//             UIUtils.ShowTips(LanguageUtils.GetText("DailyMissionPanel_NoAd"));
//             platform.LoadAds(E_AdsSource.Max, E_AdType.InsertAd);
//             // closeCallback?.Invoke(false, default);
//             return;
//         }
//
//         //  // LogUtil.Error("当前状态 isCanShowIntAd " + !CloudPlatform.isCanShowIntAd);
//         // if (!CloudPlatform.isCanShowIntAd)
//         // {
//             // UIUtils.ShowTips(LanguageUtils.GetText("DailyMissionPanel_NoAd"));
//             // return;
//         // }
//
//         Platform.Instance.ShowInterstitialAd();
//         // AccountModule.Instance.GetInterRevenueId(GetData);
//
//         // void GetData(FailHttpResponse<AccountModule.OceanShineGetAdRevenueReportIdResponse> response)
//         // {
//         //     if (!response.success || response.data == null)
//         //         return;
//         //
//         //     if (response.success)
//         //     {
//         //         CurrencyBar.OnEnQueue(response.data.Os_Btid, WithdrawalUtil.GetDollarCountByReward());
//         //         Platform.Instance.ShowInterstitialAd(E_AdsSource.Max, E_AdPos.Interstitial, response.data.Os_Btid, closeCallback);
//         //     }
//         // }
//     }
//
//
//     //     public static void OpenRewardAdsNoCoin(E_AdPos adsEventName, Action<bool> closeCallback = null, bool isLimitEcpm = false, bool showHint = true)
// //     {
// // #if UNITY_EDITOR
// //         closeCallback?.Invoke(true);
// //         return;
// // #endif
// //          // LogUtil.Verbose( // LogUtil.LOG_AD, "流程 +  普通 准备开始播放广告");
// //         MatchUIHandler._notShowAdTimes = 1;
// //         var platform = Bizza.Channel.Platform.Instance;
// //         if (platform == null)
// //         {
// //              // LogUtil.Verbose( // LogUtil.LOG_AD, "错误+ Bizza.Channel.Platform 未实例化");
// //             closeCallback?.Invoke(default);
// //             return;
// //         }
// //          // LogUtil.Verbose( // LogUtil.LOG_AD, "流程 +  准备开始播放广告");
// //         // 开始激励视频阻断
// //         EventModule.BroadCast(E_GameEvent.ShowMask, true);
// //         LastShow_RewardAdEvent = adsEventName;
// //
// //         platform.ShowAds(E_AdsSource.Max,adsEventName, GetData, isLimitEcpm, showHint);
// //
// //         void GetData(bool response)
// //         {
// //             closeCallback?.Invoke(response);
// //
// //             EventModule.BroadCast(E_GameEvent.ShowMask, false);
// //         }
// //     }
//
//
// //     /// <summary>
// //     /// 播放有奖励的广告
// //     /// </summary>
// //     /// <param name="success">成功回调</param>
// //     public void PlayAd(E_AdPos adPos, Action success = null)
// //     {
// //         if (success != null)
// //         {
// //             _successCallback = success;
// //         }
// //         OpenRewardAds(adPos, closeCallback: OnAdClaimSuccess);
// //         EventModule.AddListener<AccountController.AdsRewardData>(E_GameEvent.GetAdsRewardFromServerResponse, OnServerRespones);
// //     }
// //
// //      
// //     private void OnAdClaimSuccess(bool success)
// //     {
// //         if (success)
// //         {
// //             #if Bizza_Platform_Oversea
// //             var rewardInfo = AttributionUtil.RewardAdInfo;
// //             EventModule.BroadCast(E_GameEvent.ShowMask, true);
// //             AccountController.Instance.GetAdsReward(false, rewardInfo.ECPM, rewardInfo.AT_ID, rewardInfo.A_UID, 0, 100,
// //                 100, rewardInfo.AdPlatform, rewardInfo.AdFormat, rewardInfo.AdNetwork, "", "",OverseaPlatform.IsTopon);
// //             #endif
// //         }
// //         else
// //         {
// //              // LogUtil.Verbose( // LogUtil.LOG_AD, "错误 + 获取广告奖励失败");
// //             EventModule.BroadCast(E_GameEvent.ShowMask, false);
// //         }
// // #if UNITY_EDITOR
// //         OnServerRespones(new AccountController.AdsRewardData() { doubleSwitch = "t", rewardCoin = 100 });
// // #endif
// //     }
// //
// //     private void OnServerRespones(AccountController.AdsRewardData rewardData)
// //     {
// //         if (_successCallback != null)
// //         {
// //             _successCallback.Invoke();
// //         }
// //         EventModule.BroadCast(E_GameEvent.ShowMask, false);
// //         EventModule.RemoveListener<AccountController.AdsRewardData>(E_GameEvent.GetAdsRewardFromServerResponse, OnServerRespones);
// //     }
// //
//     /// <summary>
//     /// 只播放广告
//     /// </summary>
//     // public void JustPlayAd()
//     // {
//     //     OpenRewardAds(E_AdPos.None, closeCallback: null);
//     //     EventModule.BroadCast(E_GameEvent.ShowMask, false);
//     // }
//
//
//
//
//     #if Bizza_Platform_Oversea
//     void OnApplicationPause(bool isPaused)
//     {
//         // if (isPaused)
//         // {
//         //     // 记录进入后台的时间
//         //     //_backgroundStartTime = DateTime.UtcNow;
//         //      // LogUtil.Verbose( // LogUtil.LOG_AD, "应用进入后台，开始计时");
//         //     Time.timeScale = 0;
//         // }
//         // else
//         // {
//         //     Time.timeScale = 1;
//         //      // LogUtil.Verbose( // LogUtil.LOG_AD, "----回到应用");
//         //     // 计算后台持续时间
//         //     if (_backgroundStartTime != default)
//         //     {
//         //          // LogUtil.Verbose( // LogUtil.LOG_AD, "----开始计算时间");
//         //         TimeSpan duration = DateTime.UtcNow - _backgroundStartTime;
//         //          // LogUtil.Verbose( // LogUtil.LOG_AD, "----应用的后台时间为"+ duration.TotalSeconds);
//         //         // 如果超过2分钟且未触发过
//         //         if (duration.TotalMinutes >= 2)
//         //         {
//         //             HandleAdTimeout();
//         //              // LogUtil.Verbose( // LogUtil.LOG_AD, "应用在后台超过2分钟！");
//         //
//         //         }
//         //         else
//         //         {
//         //              // LogUtil.Verbose( // LogUtil.LOG_AD, "----不触发广告"+ duration.TotalSeconds);
//         //         }
//         //
//         //     }
//         // }
//     }
//
//         // 超时后的处理逻辑
//     private void HandleAdTimeout()
//     {
//         // 简化空检查
//         var account = AccountController.Instance?.stInfo?.adConf;
//         string toponAd = account?.TOPON_ID?.SPLASH?.FirstOrDefault() ?? "";
//         string maxAd = account?.MAX_ID?.SPLASH?.FirstOrDefault() ?? "";
//
//         // 检查广告就绪状态（确保ID非空）
//         bool maxAdIsReady = !string.IsNullOrEmpty(maxAd) && MaxSdk.IsAppOpenAdReady(maxAd);
//         bool toponAdisReady = !string.IsNullOrEmpty(toponAd) && IOSPluginAdapter.ToponIsSplashAdReady(toponAd);
//
//         // 比价逻辑
//         if (toponAdisReady && maxAdIsReady)
//         {
//             var adEntries = new List<string>();
//             if (!string.IsNullOrEmpty(maxAd))
//             {
//                 adEntries.Add($"{{\"key\":\"{maxAd}\",\"price\":{SplashInstance.splashEcpm}}}");
//             }
//             if (!string.IsNullOrEmpty(toponAd))
//             {
//                 adEntries.Add($"{{\"key\":\"{toponAd}\",\"price\":{-1}}}");
//             }
//
//             string priceInfo = $"[{string.Join(",", adEntries)}]";
//             var target = IOSPluginAdapter.CallComparePrice(priceInfo);
//
//             if (target.Count > 0)
//             {
//                 string winningAd = target.Keys.First();
//                 if (winningAd == toponAd && IOSPluginAdapter.ToponIsSplashAdReady(toponAd))
//                 {
//                      // LogUtil.Verbose( // LogUtil.LOG_AD, "胜出的首屏广告是Topon");
//                     IOSPluginAdapter.ToponShowSplashAd(toponAd);
//                 }
//                 else if (winningAd == maxAd && MaxSdk.IsAppOpenAdReady(maxAd))
//                 {
//                      // LogUtil.Verbose( // LogUtil.LOG_AD, "胜出的首屏广告是Max");
//                     MaxSdk.ShowAppOpenAd(maxAd);
//                 }
//             }
//         }
//         else if (toponAdisReady)
//         {
//              // LogUtil.Verbose( // LogUtil.LOG_AD, "只加载完成了Topon");
//             IOSPluginAdapter.ToponShowSplashAd(toponAd);
//         }
//         else if (maxAdIsReady)
//         {
//              // LogUtil.Verbose( // LogUtil.LOG_AD, "只加载完成了Max");
//             MaxSdk.ShowAppOpenAd(maxAd);
//         }
//         else
//         {
//              // LogUtil.Verbose( // LogUtil.LOG_AD, "超时，谁都没加载完成");
//         }
//     }
//
// #endif
// }
#endif
