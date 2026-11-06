using System;
using System.IO;
using Bizza.UserInformationVerification.Cryptography;

namespace Bizza.TokenClientSystem
{
    /// <summary>
    /// AuthConfig 自定义二进制序列化。
    /// 与 ChannelConfig 一样，避免在 StreamingAssets 中保存明文配置。
    /// </summary>
    public static class AuthConfigBinarySerializer
    {
        private const int Magic = 0x42495A5A;
        private const int Version = 2;
        private const string XxteaKey = "Bz9Kx_7QpL2@Auth#2026";

        public static byte[] Serialize(AuthConfigData config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(Version);

                WriteString(writer, config.AppId);
                WriteString(writer, config.AuthTokenKey);
                WriteString(writer, config.WorkerUrl);
                writer.Write(config.RequestTimeoutSeconds);
                writer.Write(config.EnableDebugResponse);

                WriteString(writer, config.EditorNativeLocale);
                WriteString(writer, config.EditorNativeLocaleCountry);
                WriteString(writer, config.EditorNativeCountryCode);
                WriteString(writer, config.EditorSimCountryIso);
                WriteString(writer, config.EditorNetworkCountryIso);
                WriteString(writer, config.EditorSimOperator);
                WriteString(writer, config.EditorNetworkOperator);
                WriteString(writer, config.EditorNetworkOperatorName);
                WriteString(writer, config.EditorDefaultInputMethod);
                WriteString(writer, config.EditorEnabledInputMethods);
                writer.Write(config.UseEditorMockDeviceInfo);
                writer.Write(config.EditorIsVpnConnected);

                writer.Flush();
                return XXTEA.Encrypt(stream.ToArray(), XxteaKey);
            }
        }

        public static AuthConfigData Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("AuthConfig.bytes 内容为空。", nameof(data));
            }

            byte[] rawData = XXTEA.Decrypt(data, XxteaKey);
            if (rawData == null || rawData.Length == 0)
            {
                throw new InvalidDataException("AuthConfig XXTEA 解密失败。");
            }

            using (MemoryStream stream = new MemoryStream(rawData))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int magic = reader.ReadInt32();
                if (magic != Magic)
                {
                    throw new InvalidDataException(
                        "不是有效的 AuthConfig 文件，Magic 校验失败。");
                }

                int version = reader.ReadInt32();
                if (version != 1 && version != Version)
                {
                    throw new InvalidDataException(
                        $"不支持的 AuthConfig 版本：{version}，当前支持版本：1、{Version}。");
                }

                AuthConfigData config = new AuthConfigData
                {
                    AppId = version >= 2 ? ReadString(reader) : string.Empty,
                    AuthTokenKey = ReadString(reader),
                    WorkerUrl = ReadString(reader),
                    RequestTimeoutSeconds = reader.ReadInt32(),
                    EnableDebugResponse = reader.ReadBoolean(),
                    EditorNativeLocale = ReadString(reader),
                    EditorNativeLocaleCountry = ReadString(reader),
                    EditorNativeCountryCode = ReadString(reader),
                    EditorSimCountryIso = ReadString(reader),
                    EditorNetworkCountryIso = ReadString(reader),
                    EditorSimOperator = ReadString(reader),
                    EditorNetworkOperator = ReadString(reader),
                    EditorNetworkOperatorName = ReadString(reader),
                    EditorDefaultInputMethod = ReadString(reader),
                    EditorEnabledInputMethods = ReadString(reader),
                    UseEditorMockDeviceInfo = reader.ReadBoolean(),
                    EditorIsVpnConnected = reader.ReadBoolean(),
                };

                return config;
            }
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            writer.Write(value ?? string.Empty);
        }

        private static string ReadString(BinaryReader reader)
        {
            return reader.ReadString();
        }
    }
}
