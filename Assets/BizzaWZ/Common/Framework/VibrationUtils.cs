using System.Collections;
using System.Collections.Generic;
#if BIZZA_REAL_WITHDRAW
using MoreMountains.NiceVibrations;
#endif
using UnityEngine;
[Obfuz.ObfuzIgnore]

public enum E_VibrateType
{
    None,
    Light,
    Medium,
    Heavy,
    // Short,
    // Middle,
    // Long,
}

public static class VibrationUtils
{
    private const long AndroidFallbackLightClickDuration = 15;
    private const long AndroidFallbackMediumClickDuration = 30;
    private const long AndroidFallbackHeavyClickDuration = 60;

#if UNITY_ANDROID && !UNITY_EDITOR
    private const int AndroidPredefinedHapticMinSdk = 29;
    private const int AndroidPredefinedEffectClick = 0;
    private static AndroidJavaClass cachedUnityPlayerClass;
    private static AndroidJavaObject cachedCurrentActivity;
    private static AndroidJavaObject cachedAndroidVibrator;
    private static AndroidJavaClass cachedVibrationEffectClass;
#endif

    public static bool IsVibrationEnabled()
    {
        var settingData = SaveDataUtils.SettingData;
        return settingData == null || settingData.enableVibrate;
    }

    public static void SetVibrationEnabled(bool enabled)
    {
        var settingData = SaveDataUtils.SettingData;
        if (settingData != null)
        {
            settingData.enableVibrate = enabled;
        }
#if BIZZA_REAL_WITHDRAW
        MMVibrationManager.SetHapticsActive(enabled);
#endif
    }

    public static void SyncVibrationSetting()
    {
#if BIZZA_REAL_WITHDRAW
        MMVibrationManager.SetHapticsActive(IsVibrationEnabled());
#endif
    }

    public static void VibrateDuration(E_VibrateType vibrateType, float duration, float interval)
    {
        if (!IsVibrationEnabled())
        {
            return;
        }

        for (float t = 0; t < duration; t += interval)
        {
            GameUtils.DelayDo(() =>
            {
                Vibrate(vibrateType);
            }, t);
        }
    }

    public static void VibrateStableClick(E_VibrateType vibrateType)
    {
        VibrateStableClick(
            vibrateType,
            AndroidFallbackLightClickDuration,
            AndroidFallbackMediumClickDuration,
            AndroidFallbackHeavyClickDuration);
    }

    public static void VibrateStableClick(
        E_VibrateType vibrateType,
        long androidFallbackLightDuration,
        long androidFallbackMediumDuration,
        long androidFallbackHeavyDuration)
    {
        if (!IsVibrationEnabled())
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrateType == E_VibrateType.Light && TryVibrateAndroidPredefinedClick())
        {
            return;
        }

        if (!MMNVAndroid.AndroidHasAmplitudeControl())
        {
            VibrateAndroidFallbackClick(
                vibrateType,
                androidFallbackLightDuration,
                androidFallbackMediumDuration,
                androidFallbackHeavyDuration);
            return;
        }
#endif
#if BIZZA_REAL_WITHDRAW
        switch (vibrateType)
        {
            case E_VibrateType.Light:
                MMVibrationManager.Haptic(HapticTypes.LightImpact, true);
                break;
            case E_VibrateType.Medium:
                MMVibrationManager.Haptic(HapticTypes.MediumImpact, true);
                break;
            case E_VibrateType.Heavy:
                MMVibrationManager.Haptic(HapticTypes.HeavyImpact, true);
                break;
            case E_VibrateType.None:
            default:
                break;
        }
#endif
    }

    public static void Vibrate(E_VibrateType vibrateType)
    {
        if (!IsVibrationEnabled())
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrateType == E_VibrateType.Light && TryVibrateAndroidPredefinedClick())
        {
            return;
        }

        if (!MMNVAndroid.AndroidHasAmplitudeControl())
        {
            VibrateAndroidFallbackClick(
                vibrateType,
                AndroidFallbackLightClickDuration,
                AndroidFallbackMediumClickDuration,
                AndroidFallbackHeavyClickDuration);
            return;
        }
#endif
#if BIZZA_REAL_WITHDRAW
        switch (vibrateType)
        {
            case E_VibrateType.Light:
                MMVibrationManager.Haptic(HapticTypes.LightImpact, true);
                break;
            case E_VibrateType.Medium:
                MMVibrationManager.Haptic(HapticTypes.MediumImpact, true);
                break;
            case E_VibrateType.Heavy:
                MMVibrationManager.Haptic(HapticTypes.HeavyImpact, true);
                break;
            case E_VibrateType.None:
            default:
                break;
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool TryVibrateAndroidPredefinedClick()
    {
        if (MMNVAndroid.AndroidSDKVersion() < AndroidPredefinedHapticMinSdk)
        {
            return false;
        }

        try
        {
            var vibrator = GetAndroidVibrator();
            if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
            {
                return false;
            }

            var vibrationEffectClass = GetAndroidVibrationEffectClass();
            var vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                "createPredefined",
                AndroidPredefinedEffectClick);
            if (vibrationEffect == null)
            {
                return false;
            }

            vibrator.Call("vibrate", vibrationEffect);
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[VibrationUtils] Android predefined click haptic failed; falling back to duration haptic. " +
                exception.Message);
            return false;
        }
    }

    private static AndroidJavaObject GetAndroidVibrator()
    {
        if (cachedAndroidVibrator != null)
        {
            return cachedAndroidVibrator;
        }

        if (cachedUnityPlayerClass == null)
        {
            cachedUnityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        }

        if (cachedCurrentActivity == null)
        {
            cachedCurrentActivity = cachedUnityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
        }

        if (cachedCurrentActivity == null)
        {
            return null;
        }

        cachedAndroidVibrator = cachedCurrentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        return cachedAndroidVibrator;
    }

    private static AndroidJavaClass GetAndroidVibrationEffectClass()
    {
        if (cachedVibrationEffectClass == null)
        {
            cachedVibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
        }

        return cachedVibrationEffectClass;
    }

    private static void VibrateAndroidFallbackClick(
        E_VibrateType vibrateType,
        long lightDuration,
        long mediumDuration,
        long heavyDuration)
    {
        var duration = GetAndroidFallbackClickDuration(vibrateType, lightDuration, mediumDuration, heavyDuration);
        if (duration <= 0)
        {
            return;
        }

        MMNVAndroid.AndroidVibrate(duration);
    }

    private static long GetAndroidFallbackClickDuration(
        E_VibrateType vibrateType,
        long lightDuration,
        long mediumDuration,
        long heavyDuration)
    {
        switch (vibrateType)
        {
            case E_VibrateType.Light:
                return SanitizeAndroidFallbackDuration(lightDuration);
            case E_VibrateType.Medium:
                return SanitizeAndroidFallbackDuration(mediumDuration);
            case E_VibrateType.Heavy:
                return SanitizeAndroidFallbackDuration(heavyDuration);
            case E_VibrateType.None:
            default:
                return 0;
        }
    }

    private static long SanitizeAndroidFallbackDuration(long duration)
    {
        return duration > 0 ? duration : 0;
    }
#endif
}
