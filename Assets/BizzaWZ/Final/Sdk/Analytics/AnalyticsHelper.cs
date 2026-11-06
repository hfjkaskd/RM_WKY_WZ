#if BIZZA_REAL_WITHDRAW
// using System;
// using System.Collections.Generic;
// using Bizza;
// using Bizza.Sdk;
//
// public static class AnalyticsHelper
// {
//     private static readonly Dictionary<string, object> publicParams = new();
//     private static readonly Dictionary<string, object> userProperties = new();
//     private static readonly Dictionary<string, object> cacheParams = new();
//
//     public static Dictionary<string, object> NewEventParamsDictionary
//     {
//         get
//         {
// #if UsingThinkingData
//             //if (!ThinkingData.Analytics.TDAnalytics.track)
//             {
//                 return new Dictionary<string, object>();
//             }
// #endif
//             cacheParams.Clear();
//             return cacheParams;
//         }
//     }
//
//     public static void SetPublicParam(IEnumerable<KeyValuePair<string, object>> @params)
//     {
//         foreach (var param in @params)
//         {
//             publicParams[param.Key] = param.Value;
//         }
//
//         ThinkingDataAdapter.Instance?.SetSuperProperties(publicParams);
//     }
//
//     public static void SetPublicParam(string key, object value)
//     {
//         publicParams[key] = value;
//         ThinkingDataAdapter.Instance?.SetSuperProperties(publicParams);
//     }
//
//     public static void SetUserProperty(string key, object value)
//     {
//         userProperties[key] = value;
//     }
//
//     public static void UploadUserProperty()
//     {
// #if UNITY_EDITOR || DEBUG_MODE
//         string content = string.Concat("[打点-用户属性] ", userProperties.ToPairString());
//         UnityEngine.Debug.Log(content);
// #endif
//         ThinkingDataAdapter.Instance?.SetUserProperties(userProperties);
//     }
//
//     public static void SendCustomEvent(string eventName, Dictionary<string, object> @params = null)
//     {
// #if UNITY_EDITOR || DEBUG_MODE
//         string content = string.Concat("[打点-事件] ", "<color=white>", eventName, "</color>", " | ", @params.ToPairString());
//         UnityEngine.Debug.Log(content);
// #endif
//         ThinkingDataAdapter.Instance?.SendCustomEvent(eventName, @params);
//     }
//
// }
#endif
