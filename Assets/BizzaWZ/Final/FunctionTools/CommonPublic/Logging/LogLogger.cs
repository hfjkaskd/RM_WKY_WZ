using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/*
| 日志类型         | 推荐颜色 |       Hex |
| ------------ | ---: | --------: |
| 普通信息 Info    |  天蓝色 | `#66C2FF` |
| 成功 Success   | 清新绿色 | `#6FDB8D` |
| 警告 Warning   |  暖黄色 | `#FFC857` |
| 错误 Error     | 柔和红色 | `#FF6B6B` |
| 网络请求 Network |   紫色 | `#B78CFF` |
| 数据/金额 Data   |  青绿色 | `#42D6C8` |
| 系统流程 System  |  灰蓝色 | `#AAB4C3` |
| 重点参数         |   橙色 | `#FF9F43` |

*/

 
public static class LogTag
{
    #region 日志

    public const string LOGLevl_Audit = "[测试日志]";
    public const string LOGLevl_Develop = "[流程日志]";

    // 测试日志类型 ===============

    public const string LOG_Attribute = "[归因]";
    public const string LOG_PlatformInit = "[平台初始化]";
    public const string LOG_ADPlayReport = "[广告播放上报]";
    public const string LOG_UserInstall = "[用户安装]";
    public const string LOG_Environment = "[真机环境]";
    public const string LOG_GameStart = "[游戏启动]";
    public const string LOG_HTTPInfo = "[HTTP信息]";

    public const string LOG_ADPlay = "[广告ID]";

    // ===================

    // 流程日志类型 ===============
    public const string LOG_UserInfoAlter = "[用户信息变更]";

    // ===================

    public const string LOG_Asset = "AssetPro_";
    public const string LOG_HTTP = "HTTP_";
    public const string LOG_Account = "Account_";
    public const string ADAttribute = "广告归因_";
    public const string ADReportFlow = "广告播放流程_";
    public const string ADNumericalStatistics = "广告次数统计_";

    public const string DailyAD = "每日任务_";

    public const string LOG_Game = "GAME_";
    public const string LOG_Load =  "LOAD_";

    public const string LOG_Pool = "Pool";
    #endregion
}


public static partial class LogLogger
{
    public const string LogMode = "DEBUG_MODE";
    public static bool AdLogIsError = false;

    public static void LogAssert(bool condition, string text)
    {
        if (!condition)
        {
            Debug.LogError(text);
        }
    }

    [Conditional(LogMode)]
    public static void LogAdInfo(string text)
    {
        if (AdLogIsError)
        {
            Debug.LogError($"[广告SDk流程_] {text}");
        }
        else
        {
            Debug.Log($"[广告SDk流程_] {text}");
        }
    }

    [Conditional(LogMode)]
    public static void LogVerbose(string text)
    {
        Debug.Log(text);
    }

    [Conditional(LogMode)]
    public static void LogVerbose(string tag, string text)
    {
        Debug.Log($"[{tag}] {text}");
    }
    
    [Conditional(LogMode)]
    public static void LogInfo(string tag, string text)
    {
        Debug.Log($"[{tag}] {text}");
    }
    
    [Conditional(LogMode)]
    public static void LogInfo(string text)
    {
        Debug.Log(text);
    }

    public static void LogError(string tag, string text)
    {
        LogError($"[{tag}] {text}");
    }

    public static void LogError(object text)
    {
        Debug.LogError(text);
    }

    #region 测试日志

    public static void LogAttribute(string text) // 广告归因 - Adjust初始化 adjust MTG Inmobi
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_Attribute} {text}");
    }

    public static void LogPlatformInit(string text) // 平台初始化 - 
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_PlatformInit} {text}");
    }

    public static void LOGUserInstall(string text) // 用户安装
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_UserInstall} {text}");
    }

    public static void LOGGameStart(string text) // 游戏启动
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_GameStart} {text}");
    }
    
    public static void LOGHTTPInfo(string text) // 游戏启动
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_HTTPInfo} {text}");
    }
    
    public static void LogADPR(string text) // 广告播放上报
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_ADPlayReport} {text}");
    }

    public static void LogADId(string text) // 广告ID播放
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_ADPlay} {text}");
    }

    public static void LogDeviceEnvironment(string text) // 用户真机环境
    {
        Debug.Log($"{LogTag.LOGLevl_Audit}{LogTag.LOG_Environment} {text}");
    }

    [Conditional(LogMode)]
    public static void LogHttpInfo(string tag, string url, string text) // HTTP请求
    {
        Debug.Log($"HTTP数据内容: {tag} - {url} - {text}");
    }

    [Conditional(LogMode)]
    public static void LogReportToB(string text) 
    {
        Debug.Log($"<color=green> BizzaLogModule:  {text}</color>");
    }

    [Conditional(LogMode)]
    public static void LogGameConfigLoad(string text)
    {
        Debug.Log($"<color=#42D6C8> gameConfig:  {text}</color>");
    }


    #endregion

    #region 流程日志

    public static void LOGNumericalShow(string tag, string text) // 数值显示
    {
        Debug.Log($"{LogTag.LOGLevl_Develop}{tag} {text}");
    }

    public static void LOGUserInfoAlter(string text) // 用户信息变更
    {
        Debug.Log($"{LogTag.LOGLevl_Develop}{LogTag.LOG_UserInfoAlter} {text}");
    }

    #endregion
}
