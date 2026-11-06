using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// 设备公共模块自己的日志边界，避免与 IP 的 AuthLog 或 SDK 的 LogLogger 同名冲突。
/// </summary>
public static class DeviceInfoLogTag
{
    public const string Device = "Device_";
    public const string Http = "HTTP_";
}

public static class DeviceInfoLog
{
    public const string LogMode = "DEBUG_MODE";

    [Conditional(LogMode)]
    public static void LogVerbose(string message)
    {
        Debug.Log(message);
    }

    [Conditional(LogMode)]
    public static void LogVerbose(string tag, string message)
    {
        Debug.Log(tag + message);
    }

    public static void LogUserInstall(string message)
    {
        Debug.Log("[用户安装] " + message);
    }

    public static void LogCountry(string message)
    {
        Debug.Log("[国家选择] " + message);
    }
}
