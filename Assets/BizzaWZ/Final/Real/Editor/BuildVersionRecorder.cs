#if BIZZA_REAL_WITHDRAW
using System;
using System.IO;
using Bizza.Sdk;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildVersionRecorder : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        try
        {
            WriteVersionLogToConfigFile();
        }
        catch (Exception e)
        {
            Debug.LogError($"[BuildVersionRecorder] 写入 versionLog 失败：{e}");
        }
    }

    private static void WriteVersionLogToConfigFile()
    {
#if BIZZA_REAL_WITHDRAW
        string configPath = Path.Combine(
            Application.dataPath,
            "StreamingAssets",
            "ChannelConfig.bytes");

        if (!File.Exists(configPath))
        {
            Debug.LogError(
                $"[BuildVersionRecorder] 找不到 ChannelConfig 配置文件，无法写入 versionLog：{configPath}");
            return;
        }

        Debug.Log($"[BuildVersionRecorder] ChannelConfig path: {configPath}");

        // ChannelConfig 现在是普通 C# 类，不能再通过 AssetDatabase/SetDirty
        // 保存。它的持久化格式是 StreamingAssets 下的加密二进制文件。
        byte[] data = File.ReadAllBytes(configPath);
        var config = ChannelConfigBinarySerializer.Deserialize(data);

        string version = PlayerSettings.bundleVersion;
        string timeString = DateTime.Now.ToString("MMddHHmm");
        string versionLog = $"{timeString}_{version}";

        var customConfig = config.real_CustomConfig;
        customConfig.versionLog = versionLog;
        config.real_CustomConfig = customConfig;

        File.WriteAllBytes(
            configPath,
            ChannelConfigBinarySerializer.Serialize(config));

        // 让 Unity 立即识别 StreamingAssets 中被修改的 bytes 文件。
        AssetDatabase.Refresh();

        Debug.Log(
            $"[BuildVersionRecorder] versionLog 写入成功：{config.real_CustomConfig.versionLog}");
#endif
    }
}
#endif
