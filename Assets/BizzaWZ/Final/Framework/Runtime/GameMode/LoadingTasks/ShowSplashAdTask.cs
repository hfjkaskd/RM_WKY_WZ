#if BIZZA_REAL_WITHDRAW
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Bizza.Sdk;
// using Bizza.Loading;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
//
// [Obfuz.ObfuzIgnore]
// public class ShowSplashAdTask : LoadingTaskBase
// {
//     public override LoadingTaskName TaskName => LoadingTaskName.ShowSplashAd;
//     public override float Weight => 1f;
//     public override LoadingTaskName[] Dependencies => new LoadingTaskName[] { LoadingTaskName.LoadRemoteConfig };
//
//     private const float MaxWaitTime = 5f;
//
//     public override async UniTask Execute()
//     {
//         bool needShow = PlayerPrefs.GetInt("ShowSplashAd_FirstTime", 0) == 1;
//         SetProgress(0.9f);
//         PlayerPrefs.SetInt("ShowSplashAd_FirstTime", 1);
//
//         #if UNITY_EDITOR
//         needShow = false;
//         #endif
//
//         // if (RemoteConfigModule.reviewMode)
//         // {
//         //     needShow = false;
//         // }
//
//         if (!needShow)
//         {
//             return;
//         }
//
//         _finish = false;
//         await UniTask.DelayFrame(1);
//         GameInstance.Instance.StartCoroutine(ShowSplashAd());
//         await UniTask.WaitUntil(() => _finish);
//     }
//
//     private bool _finish;
//     private IEnumerator ShowSplashAd()
//     {
// #if BIZZA_REAL_WITHDRAW
//         while (!BizzaSdk.Ad.Inited)
//         {
//             yield return new WaitForEndOfFrame();
//         }
//
//         // var adId = ChannelConfig.Instance.GetAdsConfig(E_AdsSource.Max).openAdId;
//         // MaxSdk.LoadAppOpenAd(adId);
//
//         var startTime = Time.realtimeSinceStartup;
//         while (Time.realtimeSinceStartup - startTime < MaxWaitTime)
//         {
//             SetProgress(Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / MaxWaitTime));
//             if (BizzaSdk.Ad.IsSplashReady)
//             {
//                 BizzaSdk.Ad.ShowSplashAd();
//                 break;
//             }
//
//             yield return new WaitForEndOfFrame();
//         }
// #else
//         yield return new WaitForEndOfFrame();
// #endif
//         _finish = true;
//     }
// }
#endif
