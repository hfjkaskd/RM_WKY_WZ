using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xxtea;

namespace Bizza.Sdk
{
    /// <summary>
    /// ChannelConfig 自定义二进制序列化。
    /// 不使用 Odin Serializer。
    /// </summary>
    public static class ChannelConfigBinarySerializer
    {
        /// <summary>
        /// 文件 Magic。
        ///
        /// BIZZ
        /// </summary>
        private const int MAGIC = 0x42495A5A;

        /// <summary>
        /// 当前二进制版本
        /// </summary>
        private const int VERSION = 6;
        private const int MIN_SUPPORTED_VERSION = 1;

        /// <summary>
        /// XXTEA 加密 Key。
        /// 编辑器保存和 Runtime 读取必须保持一致。
        /// </summary>
        private const string XXTEA_KEY =
            "Bz9Kx_7QpL2@Config#2026";


        #region Serialize

        /// <summary>
        /// ChannelConfig -> XXTEA 加密后的 byte[]
        /// </summary>
        public static byte[] Serialize(
            ChannelConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(
                    nameof(config));
            }

            byte[] rawData;

            // ==========================================
            // Binary 序列化
            // ==========================================

            using (MemoryStream stream =
                   new MemoryStream())
            {
                using (BinaryWriter writer =
                       new BinaryWriter(stream))
                {
                    // ==========================================
                    // 文件头
                    // ==========================================

                    writer.Write(MAGIC);
                    writer.Write(VERSION);


                    // ==========================================
                    // 基础配置
                    // ==========================================

                    WriteString(
                        writer,
                        config.AppId);

                    writer.Write(
                        config.GM);

                    writer.Write(
                        config.RecordMode);


#if BIZZA_REAL_WITHDRAW

                    // ==========================================
                    // HTTP配置
                    // ==========================================

                    WriteHttpConfig(
                        writer,
                        config.httpConfig);

                    writer.Write(
                        config.isReportServer);

                    writer.Write(
                        config.isEditorReportServer);

                    writer.Write(
                        config.incomeRate);

                    writer.Write(
                        config.GetFreeDollarRewardRate);
                    writer.Write(
                        config.SynthesisRewardMinRemainingCount);


                    // ==========================================
                    // Real Custom
                    // ==========================================

                    WriteRealCustomConfig(
                        writer,
                        config.real_CustomConfig);

                    WriteAdStatisticsLevelRanges(
                        writer,
                        config.adStatisticsLevelRanges);

#endif


                    // ==========================================
                    // Adjust
                    // ==========================================

                    WriteString(
                        writer,
                        config.adjustKey);


                    // ==========================================
                    // 广告
                    // ==========================================

                    writer.Write(
                        config.useInterReplenishReward);

                    writer.Write(
                        config.useRewardReplenishInter);

                    WriteAdConfigs(
                        writer,
                        config.sourceAds);


                    writer.Flush();

                    rawData =
                        stream.ToArray();
                }
            }


            // ==========================================
            // XXTEA 加密
            // ==========================================

            byte[] encryptedData =
                XXTEA.Encrypt(
                    rawData,
                    XXTEA_KEY);


            return encryptedData;
        }

        #endregion


        #region Deserialize

        /// <summary>
        /// XXTEA 加密后的 byte[]
        ///     ↓
        /// ChannelConfig
        /// </summary>
        public static ChannelConfig Deserialize(
            byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(
                    nameof(data));
            }

            if (data.Length == 0)
            {
                throw new Exception(
                    "ChannelConfig 二进制数据为空。");
            }


            // ==========================================
            // XXTEA 解密
            // ==========================================

            byte[] rawData =
                XXTEA.Decrypt(
                    data,
                    XXTEA_KEY);


            if (rawData == null ||
                rawData.Length == 0)
            {
                throw new Exception(
                    "ChannelConfig XXTEA 解密失败。");
            }


            // ==========================================
            // Binary 反序列化
            // ==========================================

            using MemoryStream stream =
                new MemoryStream(rawData);

            using BinaryReader reader =
                new BinaryReader(stream)
            {
            };


            // ==========================================
            // Magic
            // ==========================================

            int magic =
                reader.ReadInt32();

            if (magic != MAGIC)
            {
                throw new Exception(
                    "不是有效的 ChannelConfig 文件。\n" +
                    "Magic 校验失败，可能是 XXTEA Key 不正确。");
            }


            // ==========================================
            // Version
            // ==========================================

            int version =
                reader.ReadInt32();

            if (version < MIN_SUPPORTED_VERSION ||
                version > VERSION)
            {
                throw new Exception(
                    $"不支持的 ChannelConfig 版本：{version}\n" +
                    $"当前支持版本：{MIN_SUPPORTED_VERSION}-{VERSION}");
            }


            // ==========================================
            // 创建对象
            // ==========================================

            ChannelConfig config =
                new ChannelConfig();


            // ==========================================
            // 基础配置
            // ==========================================

            config.AppId =
                ReadString(reader);

            config.GM =
                reader.ReadBoolean();

            // VERSION 1-5 没有此字段，保留 ChannelConfig 中定义的默认值。
            if (version >= 6)
            {
                config.RecordMode = reader.ReadBoolean();
            }


#if BIZZA_REAL_WITHDRAW

            // ==========================================
            // HTTP
            // ==========================================

            config.httpConfig =
                ReadHttpConfig(reader);

            config.isReportServer =
                reader.ReadBoolean();

            config.isEditorReportServer =
                reader.ReadBoolean();

            config.incomeRate =
                reader.ReadDouble();

            // VERSION 1-3 没有此字段，保留 ChannelConfig 中定义的默认值。
            if (version >= 4)
            {
                config.GetFreeDollarRewardRate = reader.ReadSingle();
            }

            // VERSION 1-4 没有此字段，保留 ChannelConfig 中定义的默认值。
            if (version >= 5)
            {
                config.SynthesisRewardMinRemainingCount = reader.ReadInt32();
            }


            // ==========================================
            // Real Custom
            // ==========================================

            config.real_CustomConfig =
                ReadRealCustomConfig(reader, version, out var legacyAdStatistics);

            config.adStatisticsLevelRanges = version >= 3
                ? ReadAdStatisticsLevelRanges(reader)
                : CreateLegacyAdStatisticsLevelRanges(version, legacyAdStatistics);

#endif


            // ==========================================
            // Adjust
            // ==========================================

            config.adjustKey =
                ReadString(reader);


            // ==========================================
            // 广告
            // ==========================================

            config.useInterReplenishReward =
                reader.ReadBoolean();

            config.useRewardReplenishInter =
                reader.ReadBoolean();

            config.sourceAds =
                ReadAdConfigs(reader);


            return config;
        }

        #endregion


        #region HttpDataConfig

        private static void WriteHttpConfig(
            BinaryWriter writer,
            HttpDataConfig config)
        {
            if (config == null)
            {
                writer.Write(false);
                return;
            }


            writer.Write(true);

            WriteString(
                writer,
                config.domain);

            WriteString(
                writer,
                config.aes_key);
        }


        private static HttpDataConfig ReadHttpConfig(
            BinaryReader reader)
        {
            bool hasValue =
                reader.ReadBoolean();

            if (!hasValue)
            {
                return new HttpDataConfig();
            }


            HttpDataConfig config =
                new HttpDataConfig();

            config.domain =
                ReadString(reader);

            config.aes_key =
                ReadString(reader);

            return config;
        }

        #endregion


        #region Real_CustomConfig

#if BIZZA_REAL_WITHDRAW

        private static void WriteRealCustomConfig(
            BinaryWriter writer,
            Real_CustomConfig config)
        {
            // 是否单货币
            writer.Write(
                config.singleCurrencyMode);

            // 真提现 Pass 模式
            writer.Write(
                config.realWithdrawPassMode);


            // defaultCountry
            writer.Write(
                (int)GetPrivateEnum(
                    config,
                    "defaultCountry"));


            // country
            writer.Write(
                (int)GetPrivateEnum(
                    config,
                    "country"));


            // 自定义账号
            writer.Write(
                config.useEditorUserId);

            WriteString(
                writer,
                config.editorUserId);


            // ECPM
            writer.Write(
                config.testECPM1000);

            writer.Write(
                config.TestECPMValue);


            // 测试设备
            writer.Write(
                config.openTestDevice);

            WriteStringArray(
                writer,
                config.testDeviceIds);


            // Fake Android ID
            WriteString(
                writer,
                config.GenerateFakeAndroidId);


            // 新手引导
            writer.Write(
                config.enterNewbieGuide);


            // 失败广告
            writer.Write(
                config.failOpenAd);


            // 版本日志
            WriteString(
                writer,
                config.versionLog);


            // 插屏没准备好
            writer.Write(
                config.interAdNotReady);


            // 激励没准备好
            writer.Write(
                config.rewardAdNotReady);


        }


        private static Real_CustomConfig ReadRealCustomConfig(
            BinaryReader reader,
            int version,
            out GameAB_CustomData legacyAdStatistics)
        {
            legacyAdStatistics = default;
            Real_CustomConfig config =
                new Real_CustomConfig();


            // ==========================================
            // bool
            // ==========================================

            config.singleCurrencyMode =
                reader.ReadBoolean();

            config.realWithdrawPassMode =
                reader.ReadBoolean();


            // ==========================================
            // private enum
            // ==========================================

            AccountModule.E_CountryType defaultCountry =
                (AccountModule.E_CountryType)
                reader.ReadInt32();

            AccountModule.E_CountryType country =
                (AccountModule.E_CountryType)
                reader.ReadInt32();


            SetPrivateEnum(
                ref config,
                "defaultCountry",
                defaultCountry);

            SetPrivateEnum(
                ref config,
                "country",
                country);


            // ==========================================
            // 自定义账号
            // ==========================================

            config.useEditorUserId =
                reader.ReadBoolean();

            config.editorUserId =
                ReadString(reader);


            // ==========================================
            // ECPM
            // ==========================================

            config.testECPM1000 =
                reader.ReadBoolean();

            config.TestECPMValue =
                reader.ReadSingle();


            // ==========================================
            // 测试设备
            // ==========================================

            config.openTestDevice =
                reader.ReadBoolean();

            config.testDeviceIds =
                ReadStringArray(reader);


            // ==========================================
            // Fake Android ID
            // ==========================================

            config.GenerateFakeAndroidId =
                ReadString(reader);


            // ==========================================
            // 新手引导
            // ==========================================

            config.enterNewbieGuide =
                reader.ReadBoolean();


            // ==========================================
            // 失败广告
            // ==========================================

            config.failOpenAd =
                reader.ReadBoolean();


            // ==========================================
            // 版本日志
            // ==========================================

            config.versionLog =
                ReadString(reader);


            // ==========================================
            // 广告没准备好
            // ==========================================

            config.interAdNotReady =
                reader.ReadBoolean();

            config.rewardAdNotReady =
                reader.ReadBoolean();


            // VERSION 2 使用单组广告数值；VERSION 3 起改为关卡区间列表。
            if (version == 2)
            {
                legacyAdStatistics = new GameAB_CustomData
                {
                    CloseGetRewardCount = reader.ReadInt32(),
                    ShowGetRewardCount = reader.ReadInt32(),
                    ShowDollarCount = reader.ReadInt32(),
                    InterAdCooldownMs = reader.ReadInt32(),
                    InterAdStartLevel = reader.ReadInt32(),
                    ReviveAdStartLevel = reader.ReadInt32()
                };
            }


            return config;
        }


        /// <summary>
        /// 获取 Real_CustomConfig 的 private enum。
        /// </summary>
        private static AccountModule.E_CountryType
            GetPrivateEnum(
                Real_CustomConfig config,
                string fieldName)
        {
            FieldInfo field =
                typeof(Real_CustomConfig)
                    .GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);


            if (field == null)
            {
                throw new Exception(
                    $"找不到字段：Real_CustomConfig.{fieldName}");
            }


            object value =
                field.GetValue(config);


            return (AccountModule.E_CountryType)value;
        }


        /// <summary>
        /// 设置 Real_CustomConfig private enum。
        ///
        /// Real_CustomConfig 是 struct，
        /// 所以需要处理 boxing。
        /// </summary>
        private static void SetPrivateEnum(
            ref Real_CustomConfig config,
            string fieldName,
            AccountModule.E_CountryType value)
        {
            FieldInfo field =
                typeof(Real_CustomConfig)
                    .GetField(
                        fieldName,
                        BindingFlags.Instance |
                        BindingFlags.NonPublic);


            if (field == null)
            {
                throw new Exception(
                    $"找不到字段：Real_CustomConfig.{fieldName}");
            }


            object boxed =
                config;


            field.SetValue(
                boxed,
                value);


            config =
                (Real_CustomConfig)boxed;
        }

#endif

        #endregion


        #region AdStatisticsLevelRange

#if BIZZA_REAL_WITHDRAW

        private static void WriteAdStatisticsLevelRanges(
            BinaryWriter writer,
            List<AdStatisticsLevelRange> ranges)
        {
            if (ranges == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(ranges.Count);
            foreach (var range in ranges)
            {
                if (range == null)
                {
                    writer.Write(false);
                    continue;
                }

                writer.Write(true);
                writer.Write(range.StartLevel);
                writer.Write(range.EndLevel);
                WriteAdStatistics(writer, range.Statistics);
            }
        }

        private static List<AdStatisticsLevelRange> ReadAdStatisticsLevelRanges(
            BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
            {
                return new List<AdStatisticsLevelRange>();
            }

            if (count > 10000)
            {
                throw new InvalidDataException($"广告数值关卡区间数量无效：{count}");
            }

            var ranges = new List<AdStatisticsLevelRange>(count);
            for (var i = 0; i < count; i++)
            {
                if (!reader.ReadBoolean())
                {
                    ranges.Add(null);
                    continue;
                }

                ranges.Add(new AdStatisticsLevelRange
                {
                    StartLevel = reader.ReadInt32(),
                    EndLevel = reader.ReadInt32(),
                    Statistics = ReadAdStatistics(reader)
                });
            }

            return ranges;
        }

        private static List<AdStatisticsLevelRange> CreateLegacyAdStatisticsLevelRanges(
            int version,
            GameAB_CustomData legacyAdStatistics)
        {
            if (version < 2)
            {
                return new List<AdStatisticsLevelRange>();
            }

            return new List<AdStatisticsLevelRange>
            {
                new AdStatisticsLevelRange
                {
                    StartLevel = 1,
                    EndLevel = int.MaxValue,
                    Statistics = legacyAdStatistics
                }
            };
        }

        private static void WriteAdStatistics(
            BinaryWriter writer,
            GameAB_CustomData statistics)
        {
            writer.Write(statistics.CloseGetRewardCount);
            writer.Write(statistics.ShowGetRewardCount);
            writer.Write(statistics.ShowDollarCount);
            writer.Write(statistics.InterAdCooldownMs);
            writer.Write(statistics.InterAdStartLevel);
            writer.Write(statistics.ReviveAdStartLevel);
        }

        private static GameAB_CustomData ReadAdStatistics(BinaryReader reader)
        {
            return new GameAB_CustomData
            {
                CloseGetRewardCount = reader.ReadInt32(),
                ShowGetRewardCount = reader.ReadInt32(),
                ShowDollarCount = reader.ReadInt32(),
                InterAdCooldownMs = reader.ReadInt32(),
                InterAdStartLevel = reader.ReadInt32(),
                ReviveAdStartLevel = reader.ReadInt32()
            };
        }

#endif

        #endregion


        #region AdConfig

        /// <summary>
        /// 写入广告配置 List
        /// </summary>
        private static void WriteAdConfigs(
            BinaryWriter writer,
            List<AdConfig> configs)
        {
            if (configs == null)
            {
                writer.Write(-1);
                return;
            }


            writer.Write(
                configs.Count);


            foreach (AdConfig config in configs)
            {
                // null 标记
                if (config == null)
                {
                    writer.Write(false);
                    continue;
                }


                writer.Write(true);


                // 广告源
                writer.Write(
                    (int)config.AdsSource);


                // Reward
                WriteString(
                    writer,
                    config.rewardAdId);


                // Inter
                WriteString(
                    writer,
                    config.interAdId);


                // Banner
                WriteString(
                    writer,
                    config.bannerAdId);


                // Open
                WriteString(
                    writer,
                    config.openAdId);
            }
        }


        /// <summary>
        /// 读取广告配置 List
        /// </summary>
        private static List<AdConfig> ReadAdConfigs(
            BinaryReader reader)
        {
            int count =
                reader.ReadInt32();


            // null List
            if (count < 0)
            {
                return new List<AdConfig>();
            }


            List<AdConfig> result =
                new List<AdConfig>(count);


            for (int i = 0;
                 i < count;
                 i++)
            {
                bool hasValue =
                    reader.ReadBoolean();


                if (!hasValue)
                {
                    result.Add(null);
                    continue;
                }


                E_AdsSource adsSource =
                    (E_AdsSource)
                    reader.ReadInt32();


                AdConfig config =
                    new AdConfig(adsSource);


                config.rewardAdId =
                    ReadString(reader);

                config.interAdId =
                    ReadString(reader);

                config.bannerAdId =
                    ReadString(reader);

                config.openAdId =
                    ReadString(reader);


                result.Add(config);
            }


            return result;
        }

        #endregion


        #region String

        private static void WriteString(
            BinaryWriter writer,
            string value)
        {
            bool hasValue =
                !string.IsNullOrEmpty(value);


            writer.Write(
                hasValue);


            if (hasValue)
            {
                writer.Write(value);
            }
        }


        private static string ReadString(
            BinaryReader reader)
        {
            bool hasValue =
                reader.ReadBoolean();


            if (!hasValue)
            {
                return string.Empty;
            }


            return reader.ReadString();
        }

        #endregion


        #region StringArray

        private static void WriteStringArray(
            BinaryWriter writer,
            string[] values)
        {
            if (values == null)
            {
                writer.Write(-1);
                return;
            }


            writer.Write(
                values.Length);


            for (int i = 0;
                 i < values.Length;
                 i++)
            {
                WriteString(
                    writer,
                    values[i]);
            }
        }


        private static string[] ReadStringArray(
            BinaryReader reader)
        {
            int count =
                reader.ReadInt32();


            if (count < 0)
            {
                return null;
            }


            string[] result =
                new string[count];


            for (int i = 0;
                 i < count;
                 i++)
            {
                result[i] =
                    ReadString(reader);
            }


            return result;
        }

        #endregion
    }
}
