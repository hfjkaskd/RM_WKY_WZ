#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using UnityEngine;
#if BIZZA_REAL_WITHDRAW && UNITY_ANDROID && !UNITY_EDITOR
using Bizza.Unity.Android;
#endif

public class TipNativeBridge : MonoBehaviour
{
    const string ClassName = "com.bangsawan.oceanshine.SdkHelper";
    const string ShowTip = "ShowTips";
    private static readonly HashSet<string> LoggedDeviceEnvironments = new();

    private static void LogDeviceEnvironmentOnce(string methodName, string environment)
    {
        string key = methodName + "|" + environment;
        if (LoggedDeviceEnvironments.Add(key))
        {
            LogLogger.LogDeviceEnvironment(
                $"功能=原生提示(TipNativeBridge); 方法={methodName}; 环境={environment}");
        }
    }

    void OnEnable()
    {
        BizzaEventSystem.Set(EventDefine.AdEvent.InterAdStart, OnAdStart, true);
        BizzaEventSystem.Set(EventDefine.AdEvent.RewardAdStart, OnAdStart, true);
    }

    void OnDisable()
    {
        BizzaEventSystem.Set(EventDefine.AdEvent.InterAdStart, OnAdStart, false);
        BizzaEventSystem.Set(EventDefine.AdEvent.RewardAdStart, OnAdStart, false);
    }

    private void OnAdStart()
    {
        ShowTips(LanguageUtils.GetText("ADShow_Hint"));
    }

    public static void ShowTips(string tips)
    {
#if BIZZA_REAL_WITHDRAW && UNITY_ANDROID && !UNITY_EDITOR && BIZZA_HTTP_AD
        LogDeviceEnvironmentOnce(nameof(ShowTips), "安卓真机");
        JavaBridgeUtils.CallStaticMethodCacheClass(ClassName, ShowTip, tips);
#else
#if UNITY_EDITOR
        LogDeviceEnvironmentOnce(nameof(ShowTips), "编辑器");
#elif !UNITY_ANDROID
        LogDeviceEnvironmentOnce(nameof(ShowTips), "非安卓运行环境");
#else
        LogDeviceEnvironmentOnce("ShowTips（HTTP 广告功能未启用）", "安卓真机");
#endif
#endif
    }
}
#endif
