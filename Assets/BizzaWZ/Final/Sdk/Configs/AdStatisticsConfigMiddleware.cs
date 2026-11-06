#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bizza.Sdk;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 广告数值配置的统一入口。
/// 业务层不需要感知配置来自远端分组还是本地 ChannelConfig。
/// </summary>
public static class AdStatisticsConfigMiddleware
{
    private static readonly Dictionary<string, GameAB_CustomData> bundledConfigs = new();
#if UNITY_EDITOR
    private static string lastLoggedSource;
#endif

    public static async Task PreloadBundledConfigAsync(int groupIndex, string configName)
    {
        var country = AccountModule.CountryType.ToString();
        if (await TryPreloadCountryAsync(country, configName, groupIndex)) return;
        if (country != nameof(AccountModule.E_CountryType.None))
        {
            await TryPreloadCountryAsync(nameof(AccountModule.E_CountryType.None), configName, groupIndex);
        }
    }

    public static GameAB_CustomData Get(int currentLevel)
    {
#if BIZZA_REMOTEGROUPDATA
        if (RemoteGroupDataSystem.current.TryGetActiveAdStatistics(currentLevel, out var remoteConfig))
        {
#if UNITY_EDITOR
            LogSourceOnce("远端或沙盒缓存");
#endif
            return remoteConfig;
        }
        if (TryGetBundledConfig(out var bundledConfig))
        {
#if UNITY_EDITOR
            LogSourceOnce("StreamingAssets 本地默认");
#endif
            return bundledConfig;
        }
#endif
#if UNITY_EDITOR
        LogSourceOnce("ChannelConfig 兜底");
#endif
        return GetLocalConfig(currentLevel);
    }

#if UNITY_EDITOR
    private static void LogSourceOnce(string source)
    {
        var marker = $"{source}/{AccountModule.CountryType}";
        if (marker == lastLoggedSource) return;
        lastLoggedSource = marker;
        Debug.Log($"[AdStatisticsConfig] 来源={source}，国家={AccountModule.CountryType}。");
    }
#endif

    public static bool HasPreloadedBundledConfig() => TryGetBundledConfig(out _);

    private static bool TryGetBundledConfig(out GameAB_CustomData config)
    {
        config = default;
#if BIZZA_REMOTEGROUPDATA
        var groupIndex = RemoteGroupDataSystem.current.GetCommercialGroupIndex();
        var configName = RemoteGroupDataSystem.current.GetCommercialConfigName();
#else
        const int groupIndex = 0;
        const string configName = "ChannelConfig";
#endif
        var country = AccountModule.CountryType.ToString();
        if (bundledConfigs.TryGetValue(BuildBundledKey(country, configName, groupIndex), out config)) return true;
        return country != nameof(AccountModule.E_CountryType.None)
            && bundledConfigs.TryGetValue(BuildBundledKey(nameof(AccountModule.E_CountryType.None), configName, groupIndex), out config);
    }

    private static async Task<bool> TryPreloadCountryAsync(string country, string configName, int groupIndex)
    {
        var key = BuildBundledKey(country, configName, groupIndex);
        if (bundledConfigs.ContainsKey(key)) return true;
        var fileName = $"{configName}{groupIndex}.bytes";
        var relativePath = country == nameof(AccountModule.E_CountryType.None)
            ? Path.Combine("commercial", fileName)
            : Path.Combine(country, "commercial", fileName);
        foreach (var rootName in new[]
                 {
                     RemoteGroupRuntimeConfig.StreamingAssetsDirectoryName,
                     RemoteGroupRuntimeConfig.BundledContentDirectoryName
                 })
        {
            var path = Path.Combine(Application.streamingAssetsPath, rootName, relativePath);
            if (await TryPreloadFileAsync(path, key, groupIndex)) return true;
        }

        var legacyPath = Path.Combine(Application.streamingAssetsPath,
            "RemoteAdStatisticsDefaults", country, fileName);
        return await TryPreloadFileAsync(legacyPath, key, groupIndex);
    }

    private static async Task<bool> TryPreloadFileAsync(string path, string key, int groupIndex)
    {
        try
        {
            var url = path.Contains("://") ? path.Replace("\\", "/") : new Uri(path).AbsoluteUri;
            using var request = UnityWebRequest.Get(url);
            request.timeout = 10;
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            if (request.result != UnityWebRequest.Result.Success) return false;
            if (!RemoteAdStatisticsConfigBinarySerializer.TryDeserialize(
                    request.downloadHandler.data, groupIndex, out var config, out _)) return false;
            bundledConfigs[key] = config;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string BuildBundledKey(string country, string configName, int groupIndex)
    {
        return $"{country}/{configName}{groupIndex}";
    }

    private static GameAB_CustomData GetLocalConfig(int currentLevel)
    {
        if (ChannelConfig.Instance == null)
        {
            return default;
        }

        var ranges = ChannelConfig.Instance.adStatisticsLevelRanges;
        if (ranges == null || ranges.Count == 0)
        {
            return default;
        }

        var level = Math.Max(1, currentLevel);
        var selectedStartLevel = int.MinValue;
        var selectedConfig = default(GameAB_CustomData);
        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            if (range == null)
            {
                continue;
            }

            var startLevel = Math.Max(1, range.StartLevel);
            var endLevel = Math.Max(startLevel, range.EndLevel);
            if (level < startLevel || level > endLevel || startLevel < selectedStartLevel)
            {
                continue;
            }

            selectedStartLevel = startLevel;
            selectedConfig = range.Statistics;
        }

        return selectedConfig;
    }
}

internal static class RemoteAdStatisticsConfigBinarySerializer
{
    private const int BinaryVersion = 1;
    private static readonly byte[] BinaryMagic = Encoding.ASCII.GetBytes("CHAB");

    public static bool TryDeserialize(
        byte[] bytes,
        int expectedGroupIndex,
        out GameAB_CustomData config,
        out string errorMessage)
    {
        config = default;
        errorMessage = string.Empty;

        if (bytes == null || bytes.Length == 0)
        {
            errorMessage = "商业化配置内容为空。";
            return false;
        }

        try
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);
            var magic = reader.ReadBytes(BinaryMagic.Length);
            if (magic.Length != BinaryMagic.Length)
            {
                errorMessage = "商业化配置文件头长度无效。";
                return false;
            }

            for (var i = 0; i < BinaryMagic.Length; i++)
            {
                if (magic[i] == BinaryMagic[i])
                {
                    continue;
                }

                errorMessage = "商业化配置 Magic 校验失败。";
                return false;
            }

            var version = reader.ReadInt32();
            if (version != BinaryVersion)
            {
                errorMessage = $"不支持的商业化配置版本：{version}。";
                return false;
            }

            var groupIndex = reader.ReadInt32();
            if (groupIndex != expectedGroupIndex)
            {
                errorMessage = $"商业化配置分组序号不匹配：期望 {expectedGroupIndex}，实际 {groupIndex}。";
                return false;
            }

            config = new GameAB_CustomData
            {
                CloseGetRewardCount = reader.ReadInt32(),
                ShowGetRewardCount = reader.ReadInt32(),
                ShowDollarCount = reader.ReadInt32(),
                InterAdCooldownMs = reader.ReadInt32(),
                InterAdStartLevel = reader.ReadInt32(),
                ReviveAdStartLevel = reader.ReadInt32()
            };
            if (stream.Position != stream.Length)
            {
                errorMessage = "商业化配置文件长度无效。";
                config = default;
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            config = default;
            errorMessage = $"商业化配置解析失败：{exception.Message}";
            return false;
        }
    }
}
#endif
