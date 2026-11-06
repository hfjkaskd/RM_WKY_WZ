using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

#if BIZZA_REAL_WITHDRAW

// 其中要有 广告频率配置，循环关卡，插屏播放频率，其它配置
[Serializable]
[Obfuz.ObfuzIgnore]
public struct GameAB_CustomData
{
    [LabelText("关闭恭喜获得界面出现插屏")]
    public int CloseGetRewardCount;

    [LabelText("合成多少次后出现恭喜获得界面")]
    public int ShowGetRewardCount;

    [LabelText("合成多少次后出现假钞")]
    public int ShowDollarCount;

    [LabelText("插屏广告间隔(毫秒)")]
    [MinValue(0)]
    public int InterAdCooldownMs;

    [LabelText("从哪一关开始显示插屏广告")]
    [MinValue(1)]
    public int InterAdStartLevel;

    [LabelText("从哪一关开始复活需要看广告")]
    [MinValue(1)]
    public int ReviveAdStartLevel;
}

[Serializable][Obfuz.ObfuzIgnore]
public class GameAB_CustomDatas
{
    public AccountModule.E_CountryType e_CountryType;
    public List<GameAB_CustomData> gameAB_Datas = new();
}

[CreateAssetMenu(fileName = "ChannelABConfig", menuName = "BizzaGame/Channel AB Config")]
public class ChannelABConfig : ScriptableObject
{
    public string groupConfigName = "ChannelConfig"; // 后续向远端拉取的时候就是 "groupConfigName + 序号".bytes 这样的命名拉取
    [LabelText("分组序号从 1 开始")]
    public bool StartGroupIndexAtOne;

    public List<GameAB_CustomDatas> gameAB_CustomDatas = new();

    // 一个按钮可以将配置导出为二进制文件，共后续上传给远端
#if UNITY_EDITOR
    private const string BinaryMagic = "CHAB";
    private const int BinaryVersion = 1;
    private const string CommercialDirectoryName = "commercial";

    public static List<string> ExportConfigurationFile(ChannelABConfig config, string outputDirectory)
    {
        if (config == null)
        {
            throw new InvalidDataException("ChannelABConfig 资源为空。");
        }

        if (config.gameAB_CustomDatas == null || config.gameAB_CustomDatas.Count == 0)
        {
            throw new InvalidDataException("ChannelABConfig 中没有商业化分组配置。");
        }

        var groupNamePrefix = Path.GetFileNameWithoutExtension(config.groupConfigName?.Trim());
        if (string.IsNullOrWhiteSpace(groupNamePrefix))
        {
            throw new InvalidDataException("ChannelABConfig.groupConfigName 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidDataException("商业化配置导出目录不能为空。");
        }

        Directory.CreateDirectory(outputDirectory);
        var exportedPaths = new List<string>();
        var countrySet = new HashSet<AccountModule.E_CountryType>();
        for (var countryIndex = 0; countryIndex < config.gameAB_CustomDatas.Count; countryIndex++)
        {
            var countryData = config.gameAB_CustomDatas[countryIndex];
            if (countryData == null)
            {
                throw new InvalidDataException($"ChannelABConfig 第 {countryIndex + 1} 项国家配置为空。");
            }

            if (!Enum.IsDefined(typeof(AccountModule.E_CountryType), countryData.e_CountryType))
            {
                throw new InvalidDataException($"ChannelABConfig 第 {countryIndex + 1} 项国家类型无效。");
            }

            if (!countrySet.Add(countryData.e_CountryType))
            {
                throw new InvalidDataException($"ChannelABConfig 中国家 {countryData.e_CountryType} 配置重复。");
            }

            if (countryData.gameAB_Datas == null || countryData.gameAB_Datas.Count == 0)
            {
                throw new InvalidDataException($"ChannelABConfig 国家 {countryData.e_CountryType} 没有商业化分组配置。");
            }

            var countryRootDirectory = countryData.e_CountryType == AccountModule.E_CountryType.None
                ? outputDirectory
                : Path.Combine(outputDirectory, countryData.e_CountryType.ToString());
            var countryOutputDirectory = Path.Combine(countryRootDirectory, CommercialDirectoryName);
            Directory.CreateDirectory(countryOutputDirectory);

            for (var listIndex = 0; listIndex < countryData.gameAB_Datas.Count; listIndex++)
            {
                var groupIndex = listIndex + (config.StartGroupIndexAtOne ? 1 : 0);
                var outputPath = Path.Combine(countryOutputDirectory, $"{groupNamePrefix}{groupIndex}.bytes");
                var data = countryData.gameAB_Datas[listIndex];

                using (var stream = new MemoryStream())
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(Encoding.ASCII.GetBytes(BinaryMagic));
                    writer.Write(BinaryVersion);
                    writer.Write(groupIndex);
                    writer.Write(data.CloseGetRewardCount);
                    writer.Write(data.ShowGetRewardCount);
                    writer.Write(data.ShowDollarCount);
                    writer.Write(data.InterAdCooldownMs);
                    writer.Write(data.InterAdStartLevel);
                    writer.Write(data.ReviveAdStartLevel);
                    writer.Flush();
                    File.WriteAllBytes(outputPath, stream.ToArray());
                }

                exportedPaths.Add(outputPath.Replace("\\", "/"));
            }
        }

        return exportedPaths;
    }
#endif
}

#endif
