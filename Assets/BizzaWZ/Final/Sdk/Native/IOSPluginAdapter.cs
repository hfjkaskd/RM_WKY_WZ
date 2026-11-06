#if BIZZA_REAL_WITHDRAW
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Bizza.Sdk;
// using Obfuz;
// using UnityEngine;
// using System.Text.RegularExpressions;
//
// namespace Bizza.Sdk
// {
//
//     public class IOSPluginAdapter
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//     [DllImport("__Internal")]
//     private static extern void RequestIDFA();
//     [DllImport("__Internal")]
//     private static extern string GetCountryCode();
//
//     [DllImport("__Internal")]
//     private static extern string GetOpenUDID();
//     [DllImport("__Internal")]
//     private static extern string comparePrice(string priceInfo);
//     // 1. VPN检测
//     [DllImport("__Internal")]
//     private static extern bool IsVpn();
//     //2.模拟器检测
//     [DllImport("__Internal")]
//     private static extern bool IsSimul();
//     //3.版本是否匹配
//     [DllImport("__Internal")]
//     private static extern bool SysVNMatch();
//     //4.是否进行了越狱(越狱)
//     [DllImport("__Internal")]
//     private static extern bool IsDJbPath();
//     //5.是否进行了越狱(isDJbUSche)
//     [DllImport("__Internal")]
//     private static extern bool IsDJbUSche();
//     //6.是否进行了动态库越狱()
//     [DllImport("__Internal")]
//     private static extern bool IsDJbLib();
//     //7.是否进行了动态库越狱(idTamper)
//     [DllImport("__Internal")]
//     private static extern bool IdTamper(string apiBundleIdentifier);
//     //8.是否有风险
//     [DllImport("__Internal")]
//      private static extern bool InjectLib();
//     [DllImport("__Internal")]
//     private static extern void _LoadRewardedAd(string placementId, string extraJson);
//     [DllImport("__Internal")]
//     private static extern bool _IsRewardedAdReady(string placementId);
//      [DllImport("__Internal")]
//     private static extern void _ShowRewardedAd(string placementId, string sceneId,string extraJson);
//
//     [DllImport("__Internal")]
//     private static extern void _LoadInterstitialAd(string placementId, string extraJson);
//     [DllImport("__Internal")]
//     private static extern void _ShowInterstitialAd(string placementId, string sceneId,string extraJson);
//     [DllImport("__Internal")]
//     private static extern bool _IsInterstitialAdReady(string placementId);
//     [DllImport("__Internal")]
//      private static extern bool _SetRewardedAdBool();
//      [DllImport("__Internal")]
//      private static extern bool _SetInterAdBool();
//       [DllImport("__Internal")]
//      private static extern void _LoadSplashAd(string placementId);
//     [DllImport("__Internal")]
//      private static extern bool _IsSplashAdReady(string placementId);
//        [DllImport("__Internal")]
//      private static extern void _ShowSplashAd(string placementId);
//      // 定义iOS调用接口
//     [DllImport("__Internal")]
//      private static extern void DigitalUnion_Initialize(string customerId);
//
//     [DllImport("__Internal")]
//     private static extern string DigitalUnion_GetQueryIDSync(string msg);
//
//
// #endif
//
//     public static void InitSZLMSdk(string customerId)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         DigitalUnion_Initialize(customerId);
// #endif
//     }
//
//     public static string CallGetQueryIdSync(string ouid, string pkg, string rid, string scene, string atid = "")
//     {
//         string cdid = "";
//         // 根据scene值动态处理atid参数
//         if (scene != "REWARD")
//         {
//             atid = ""; // 如果scene不是REWARD，atid为空
//         }
//
//         // 构造自定义消息
//         string msg = "{\"ouid\":\"" + ouid + "\",\"atid\":\"" + atid + "\",\"pkg\":\"" + pkg + "\",\"rid\":\"" + rid +
//                      "\",\"scene\":\"" + scene + "\"}";
// #if UNITY_IOS && !UNITY_EDITOR
//         // 调用iOS的同步getQueryId()方法
//          cdid = DigitalUnion_GetQueryIDSync(msg);
// #endif
//         PlayerPrefs.SetString("cdid", cdid);
//         //
//         Debug.Log("----------------cdid" + cdid);
//         return cdid;
//     }
//
//
//     public static void ToponLoadSplashAd(string placementId)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         _LoadSplashAd(placementId);
// #endif
//     }
//
//     public static bool ToponIsSplashAdReady(string placementId)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//        return _IsSplashAdReady(placementId);
// #endif
//         return false;
//     }
//
//     public static void ToponShowSplashAd(string placementId)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         _ShowSplashAd(placementId);
// #endif
//     }
//
//     public static void SetAdBool()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         _SetRewardedAdBool();
//         _SetInterAdBool();
// #endif
//     }
//
//     /// <summary>
//     /// 加载topon的插屏广告
//     /// </summary>
//     /// <param name="placementId"></param>
//     /// <param name="extraParams"></param>
//     public static void LoadInterstitialAd(string placementId, string extraParams)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//            _LoadInterstitialAd(placementId, extraParams);
// #endif
//     }
//
//     /// <summary>
//     /// 展示Topon的插屏广告
//     /// </summary>
//     /// <param name="placementId"></param>
//     /// <param name="extraJson"></param>
//     /// <param name="sceneId"></param>
//     public static void ShowInterstitialAd(string placementId, string extraJson, string sceneId = "")
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         _ShowInterstitialAd(placementId,"",extraJson);
// #else
//         Debug.Log($"模拟展示广告：{placementId} (场景：{sceneId})");
// #endif
//     }
//
//     /// <summary>
//     /// topon的插屏广告是否准备好了
//     /// </summary>
//     /// <param name="placementId"></param>
//     /// <returns></returns>
//     public static bool IsInterstitialAdReady(string placementId)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         return _IsInterstitialAdReady(placementId);
// #endif
//         return false;
//     }
//
//     /// <summary>
//     /// 加载topon的激励广告
//     /// </summary>
//     /// <param name="placementId"></param>
//     /// <param name="extraParams"></param>
//     public static void LoadRewardeAd(string placementId, string extraParams)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//            _LoadRewardedAd(placementId, extraParams);
// #endif
//     }
//
//     /// <summary>
//     /// 展示Topon激励视频广告
//     /// </summary>
//     /// <param name="sceneId">场景ID（传空字符串使用默认场景）</param>
//     public static void ShowRewardedAd(string placementId, string extraJson, string sceneId = "")
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         _ShowRewardedAd(placementId,"",extraJson);
// #else
//         Debug.Log($"模拟展示广告：{placementId} (场景：{sceneId})");
// #endif
//     }
//
//     /// <summary>
//     /// topon的激励广告是否已经准备好了
//     /// </summary>
//     /// <param name="placementId"></param>
//     /// <returns></returns>
//     public static bool IsRewardedAdReady(string placementId)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         return _IsRewardedAdReady(placementId);
// #endif
//         return false;
//     }
//
//     /// <summary>
//     /// 是否是vpn
//     /// </summary>
//     /// <returns></returns>
//     public static int IsVPNConnected()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         bool result = IsVpn();
//         Debug.Log("是否是vpn " + result);
//         int boolAsString = result ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     /// <summary>
//     /// 是否是模拟器
//     /// </summary>
//     /// <returns></returns>
//     public static int IsCallBackSimul()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         bool result = IsSimul();
//         Debug.Log("是否是模拟器 " + result);
//         int boolAsString = result ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     /// <summary>
//     /// 是否进行了路径越狱
//     /// </summary>
//     /// <returns></returns>
//     public static int IsCallBackIsDJbPath()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         bool result = IsDJbPath();
//         Debug.Log("是否进行了路径越狱 " + result);
//         int boolAsString = result ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     /// <summary>
//     /// 是否进行了URL Scheme越狱
//     /// </summary>
//     /// <returns></returns>
//     public static int IsCallBackIsDJbUSche()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         bool result = IsDJbUSche();
//         Debug.Log("是否进行了URL Scheme越狱 " + result);
//         int boolAsString = result ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     /// <summary>
//     /// 是否进行了动态越狱
//     /// </summary>
//     /// <returns></returns>
//     public static int IsCallBackIsDJbLib()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         bool result = IsDJbLib();
//         Debug.Log("是否进行了动态越狱 " + result);
//          int boolAsString = result ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     /// <summary>
//     /// 是否进行了apiBundleID越狱
//     /// </summary>
//     /// <returns></returns>
//     public static int IsCallBackIdTamper()
//     {
//         string apiBundleID = ChannelConfig.Instance.bundleId;
// #if UNITY_IOS && !UNITY_EDITOR
//         bool result = IdTamper(apiBundleID);
//         Debug.Log("是否进行了apiBundleID越狱 " + result);
//         int boolAsString = result ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     /// <summary>
//     /// 版本是否匹配
//     /// </summary>
//     /// <returns></returns>
//     public static int IsCallBackSysVNMatch()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         bool result = SysVNMatch();
//         Debug.Log("版本是否匹配 " + result);
//         int boolAsString = result ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     /// <summary>
//     /// 注入共享库风险检测结果
//     /// </summary>
//     /// <returns></returns>
//     public static int CheckInjectedLibraries()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         bool isInjected = InjectLib();
//         Debug.Log("注入共享库风险检测结果: " + isInjected);
//         int boolAsString = isInjected ? 1 : 0;
//         return boolAsString;
// #else
//         return 0;
// #endif
//     }
//
//     [System.Serializable]
//     public class AdPriceInfo
//     {
//         public string key;
//
//         // 注意：Unity的JsonUtility需要字段为public
//         public double price;
//     }
//
//     [System.Serializable]
//     private class AdPriceInfoWrapper
//     {
//         public AdPriceInfo[] prices;
//     }
//
//     /// <summary>
//     /// 广告比价
//     /// </summary>
//     /// <param name="priceInfo"></param>
//     /// <returns></returns>
//     public static Dictionary<string, double> CallComparePrice(string priceInfo)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         string result = comparePrice(priceInfo);
//         Debug.Log("胜出的广告 " + result);
//         var a = ParsePriceResult(result);
//         // 5. 使用结果数据
//         foreach (var pair in a)
//         {
//             Debug.Log($"最终比价结果 - 广告位: {pair.Key}, 价格: {pair.Value}");
//         }
//         Debug.Log("comparePrice is only available on iOS devices.");
//          return a;
// #elif UNITY_ANDROID
//         string result = comparePrice(priceInfo); // 这里需要你帮我补充一个方法
//         LogLogger.LogError("胜出的广告 " + result);
//         var a = ParsePriceResult_1(result);
//         // 5. 使用结果数据
//         foreach (var pair in a)
//         {
//             LogLogger.LogError($"最终比价结果 - 广告位: {pair.Key}, 价格: {pair.Value}");
//         }
//         return a;
// #else
//         return null;
// #endif
//     }
// #if UNITY_ANDROID
//     private static string comparePrice(string priceInfo)
//     {
//         if (string.IsNullOrEmpty(priceInfo))
//             return string.Empty;
//
//         try
//         {
//             // 直接解析整个数组
//             var dict = ParsePriceResult(priceInfo);
//
//             if (dict == null || dict.Count == 0)
//                 return string.Empty;
//
//             var winner = dict
//                 .OrderByDescending(p => p.Value)
//                 .First();
//
//             return $"{{\"key\":\"{winner.Key}\",\"price\":{winner.Value}}}";
//         }
//         catch (Exception e)
//         {
//             Debug.LogError("Android comparePrice error: " + e);
//             return string.Empty;
//         }
//     }
//
//     public static Dictionary<string, double> ParsePriceResult_1(string json)
//     {
//         Dictionary<string, double> dict = new Dictionary<string, double>();
//         var keyMatch = Regex.Match(json, @"""key""\s*:\s*""([^""]+)""");
//         var priceMatch = Regex.Match(json, @"""price""\s*:\s*([+-]?\d*\.?\d+)");
//
//         if (keyMatch.Success && priceMatch.Success)
//         {
//             string key = keyMatch.Groups[1].Value;
//             double price = double.Parse(priceMatch.Groups[1].Value);
//             dict[key] = price;
//             LogLogger.LogError("匹配成功");
//         }
//         else
//         {
//             throw new FormatException("JSON 格式不匹配");
//         }
//
//         return dict;
//     }
// #endif
//
//     /// <summary>
//     /// 解析JSON字符串为字典
//     /// </summary>
//     public static Dictionary<string, double> ParsePriceResult(string json)
//     {
//         Dictionary<string, double> resultDict = new Dictionary<string, double>();
//
//         try
//         {
//             // 添加外层包裹对象解决数组解析问题
//             string wrappedJson = $"{{\"prices\":{json}}}";
//             AdPriceInfoWrapper wrapper = JsonUtility.FromJson<AdPriceInfoWrapper>(wrappedJson);
//
//             if (wrapper.prices != null)
//             {
//                 foreach (var info in wrapper.prices)
//                 {
//                     if (!string.IsNullOrEmpty(info.key))
//                     {
//                         resultDict[info.key] = info.price;
//                     }
//                 }
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"JSON解析失败: {e.Message}\n原始数据: {json}");
//         }
//
//         return resultDict;
//     }
//
//     public static string GetCountryCodeFromIOS()
//     {
// #if UNITY_EDITOR
//         return "";
// #elif UNITY_IOS
//         string countryCode = GetCountryCode();
//         Debug.Log("countryCode: "+countryCode);
//         return countryCode;
// #else
//         return System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
// #endif
//     }
//
//     public static void RequestToIDFA()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         RequestIDFA();
// #endif
//     }
//
//     public static string GetOpenUDIDFromiOS()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         string opudid = GetOpenUDID();
//         Debug.Log("opudid: "+opudid);
//         return opudid;
// #else
//         Debug.LogWarning("Not available on this platform");
//         string udid = PlayerPrefs.GetString("OtherOpenUDID", Guid.NewGuid().ToString());
//         PlayerPrefs.SetString("OtherOpenUDID", udid);
//         return udid;
// #endif
//     }
//
//     public static void GetIDFAString(Action<bool, string> callback)
//     {
//         Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) =>
//         {
//             if (string.IsNullOrEmpty(error))
//             {
//                 Debug.Log("IDFA: " + advertisingId);
//                 callback?.Invoke(true, advertisingId);
//             }
//             else
//             {
//                 Debug.LogError("Error getting IDFA: " + error);
//                 callback?.Invoke(false, "");
//             }
//         });
//     }
//     }
// }
#endif
