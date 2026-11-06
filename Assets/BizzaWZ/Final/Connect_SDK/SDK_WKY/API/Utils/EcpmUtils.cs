#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bizza.Sdk;
using UnityEngine;

public static class EcpmUtils
{
    /// <summary>
    /// 对齐 Android getEcPm 语义的 Unity 实现
    /// </summary>
    public static double NormalizeEcpm(double revenue)
    {
        bool hasRevenue = revenue > 0;

#if DEBUG_MODE
        if (ChannelConfig.Instance.real_CustomConfig.testECPM1000)
        {
            LogLogger.LogInfo($"测试阶段 - NormalizeEcpm - ：：： ECPM设置为1000");
            return ChannelConfig.Instance.real_CustomConfig.TestECPMValue;;
        }
#endif

        return hasRevenue ? revenue * 1000.0 : 0.0;
    }

    /// <summary>
    /// 将 Double 转换为 string
    /// </summary>
    /// <param name="_ecpm"></param>
    /// <returns></returns>
    public static string SwitchToStringForEcpm(double _ecpm)
    {
        string ecpm = _ecpm.ToString("R", CultureInfo.InvariantCulture);
        return ecpm;
    }
}
#endif