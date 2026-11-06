using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class BizzaAnalyticsConfigSerializer
{
  private const int Magic = 0x42494143; // BIAC
  private const int Version = 1;
  private const int MaxPayloadBytes = 1024 * 1024;

  // This key protects the packaged configuration from casual inspection.
  // It is intentionally separate from the per-project payload encryption key.
  private const string ConfigEncryptionKeyBase64 =
    "xPfnb0Z1Dw6hn0HuFbGxY/RmA0MYNRupo8B/ZddLNiA=";
  private const string ConfigEncryptionKid = "bizza-analytics-config-k1";

  public const string DefaultPayloadEncryptionKeyBase64 =
    "xPfnb0Z1Dw6hn0HuFbGxY/RmA0MYNRupo8B/ZddLNiA=";
  public const string DefaultPayloadEncryptionKid = "k1-20260306";

  public const string ConfigFileName = "BizzaAnalytics.bytes";

  public static string RuntimeConfigFileName
  {
    get
    {
      return ConfigFileName;
    }
  }

  public static string GetConfigPath()
  {
    return GetConfigPath(RuntimeConfigFileName);
  }

  public static string GetConfigPath(string configFileName)
  {
    if (string.IsNullOrWhiteSpace(configFileName))
    {
      throw new ArgumentException("Analytics 配置文件名不能为空。", "configFileName");
    }

    return Path.Combine(Application.streamingAssetsPath, configFileName).Replace('\\', '/');
  }

  public static byte[] Serialize(BizzaAnalyticsConfig config)
  {
    string validationError;
    if (!ValidateConfig(config, out validationError))
    {
      throw new InvalidOperationException(validationError);
    }

    BizzaCrypto crypto;
    string cryptoError;
    if (!BizzaCrypto.TryCreate(ConfigEncryptionKeyBase64, ConfigEncryptionKid, out crypto, out cryptoError))
    {
      throw new InvalidOperationException("Analytics 配置加密器创建失败：" + cryptoError);
    }

    Dictionary<string, object> payload = new Dictionary<string, object>
    {
      { "schema", Version },
      { "app_id", config.appId },
      { "ingest_url", config.ingestUrl },
      { "ingest_token", config.ingestToken },
      { "message_frequency", (int)config.messageFrequency },
      { "reporting_ratio", config.reportingRatio },
      { "formal_package_reporting_enabled", config.formalPackageReportingEnabled },
      { "encryption_enabled", config.encryptionEnabled },
      { "encryption_key_base64", config.encryptionKeyBase64 ?? string.Empty },
      { "encryption_kid", config.encryptionKid ?? string.Empty },
      { "verbose_log", config.verboseLog },
    };

    Dictionary<string, object> encryptedEnvelope = crypto.EncryptValue(payload);
    byte[] encryptedJson = Encoding.UTF8.GetBytes(BizzaJson.Serialize(encryptedEnvelope));

    using (MemoryStream stream = new MemoryStream())
    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
    {
      writer.Write(Magic);
      writer.Write(Version);
      writer.Write(encryptedJson.Length);
      writer.Write(encryptedJson);
      writer.Flush();
      return stream.ToArray();
    }
  }

  public static BizzaAnalyticsConfig Deserialize(byte[] data)
  {
    if (data == null || data.Length == 0)
    {
      throw new InvalidDataException("BizzaAnalytics.bytes 内容为空。");
    }

    byte[] encryptedJson;
    using (MemoryStream stream = new MemoryStream(data))
    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
    {
      int magic = reader.ReadInt32();
      if (magic != Magic)
      {
        throw new InvalidDataException("BizzaAnalytics.bytes Magic 校验失败。");
      }

      int version = reader.ReadInt32();
      if (version != Version)
      {
        throw new InvalidDataException(
          string.Format("不支持的 BizzaAnalytics 配置版本：{0}，当前版本：{1}。", version, Version));
      }

      int length = reader.ReadInt32();
      if (length <= 0 || length > MaxPayloadBytes || length > stream.Length - stream.Position)
      {
        throw new InvalidDataException("BizzaAnalytics.bytes 加密数据长度无效。");
      }

      encryptedJson = reader.ReadBytes(length);
      if (encryptedJson.Length != length)
      {
        throw new InvalidDataException("BizzaAnalytics.bytes 数据不完整。");
      }
    }

    object parsedEnvelope = BizzaJson.Deserialize(Encoding.UTF8.GetString(encryptedJson));
    if (parsedEnvelope == null)
    {
      throw new InvalidDataException("BizzaAnalytics.bytes 加密信封解析失败。");
    }

    BizzaCrypto crypto;
    string cryptoError;
    if (!BizzaCrypto.TryCreate(ConfigEncryptionKeyBase64, ConfigEncryptionKid, out crypto, out cryptoError))
    {
      throw new InvalidDataException("Analytics 配置解密器创建失败：" + cryptoError);
    }

    object decryptedPayload = crypto.DecryptValue(parsedEnvelope);
    Dictionary<string, object> payload = BizzaValueUtil.AsStringObjectDictionary(decryptedPayload);
    if (payload.Count == 0)
    {
      throw new InvalidDataException("BizzaAnalytics.bytes 解密后配置为空。");
    }

    return CreateConfig(payload);
  }

  public static bool TryDeserialize(byte[] data, out BizzaAnalyticsConfig config, out string error)
  {
    config = null;
    error = string.Empty;

    try
    {
      config = Deserialize(data);
      return true;
    }
    catch (Exception exception)
    {
      error = exception.Message;
      return false;
    }
  }

  public static bool ValidateConfig(BizzaAnalyticsConfig config, out string error)
  {
    error = string.Empty;
    if (config == null)
    {
      error = "Analytics 配置为空。";
      return false;
    }

    if (!IsValidAppId(config.appId))
    {
      error = "Analytics 必填参数 appId 缺失或格式无效，只允许 1-128 位字母、数字、点、下划线和短横线。";
      return false;
    }

    if (string.IsNullOrWhiteSpace(config.ingestUrl)
      || !config.ingestUrl.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
    {
      error = "Analytics 必填参数 ingestUrl 缺失或格式无效。";
      return false;
    }

    if (string.IsNullOrWhiteSpace(config.ingestToken))
    {
      error = "Analytics 必填参数 ingestToken 缺失。";
      return false;
    }

    if (!Enum.IsDefined(typeof(BizzaMessageFrequency), config.messageFrequency))
    {
      error = "Analytics 参数 messageFrequency 无效。";
      return false;
    }

    if (float.IsNaN(config.reportingRatio)
      || float.IsInfinity(config.reportingRatio)
      || config.reportingRatio < 0f
      || config.reportingRatio > 1f)
    {
      error = "Analytics 参数 reportingRatio 必须在 0 到 1 之间。";
      return false;
    }

    if (config.encryptionEnabled)
    {
      if (string.IsNullOrWhiteSpace(config.encryptionKeyBase64))
      {
        error = "Analytics 必填参数 encryptionKeyBase64 缺失。";
        return false;
      }

      if (string.IsNullOrWhiteSpace(config.encryptionKid))
      {
        error = "Analytics 必填参数 encryptionKid 缺失。";
        return false;
      }

      BizzaCrypto crypto;
      string cryptoError;
      if (!BizzaCrypto.TryCreate(config.encryptionKeyBase64, config.encryptionKid, out crypto, out cryptoError))
      {
        error = "Analytics 加密参数无效：" + cryptoError;
        return false;
      }
    }

    return true;
  }

  private static BizzaAnalyticsConfig CreateConfig(Dictionary<string, object> payload)
  {
    RequireField(payload, "schema");
    int schema = BizzaValueUtil.AsInt(payload["schema"], -1);
    if (schema != Version)
    {
      throw new InvalidDataException(
        string.Format("Analytics 配置 schema 不匹配：{0}。", schema));
    }

    BizzaAnalyticsConfig config = new BizzaAnalyticsConfig
    {
      appId = RequireString(payload, "app_id"),
      ingestUrl = RequireString(payload, "ingest_url"),
      ingestToken = RequireString(payload, "ingest_token"),
      messageFrequency = (BizzaMessageFrequency)RequireInt(payload, "message_frequency"),
      reportingRatio = (float)RequireDouble(payload, "reporting_ratio"),
      formalPackageReportingEnabled = OptionalBool(payload, "formal_package_reporting_enabled", true),
      encryptionEnabled = RequireBool(payload, "encryption_enabled"),
      encryptionKeyBase64 = OptionalString(payload, "encryption_key_base64"),
      encryptionKid = OptionalString(payload, "encryption_kid"),
      verboseLog = RequireBool(payload, "verbose_log"),
    };

    string validationError;
    if (!ValidateConfig(config, out validationError))
    {
      throw new InvalidDataException(validationError);
    }

    return config;
  }

  private static bool IsValidAppId(string value)
  {
    string appId = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    if (appId.Length == 0 || appId.Length > 128)
    {
      return false;
    }

    for (int i = 0; i < appId.Length; i++)
    {
      char character = appId[i];
      bool accepted = (character >= 'A' && character <= 'Z')
        || (character >= 'a' && character <= 'z')
        || (character >= '0' && character <= '9')
        || character == '.'
        || character == '_'
        || character == '-';
      if (!accepted)
      {
        return false;
      }
    }

    return true;
  }

  private static void RequireField(Dictionary<string, object> payload, string key)
  {
    if (payload == null || !payload.ContainsKey(key) || payload[key] == null)
    {
      throw new InvalidDataException("Analytics 配置缺少必填字段：" + key + "。");
    }
  }

  private static string RequireString(Dictionary<string, object> payload, string key)
  {
    RequireField(payload, key);
    string value = BizzaValueUtil.AsString(payload[key], string.Empty);
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new InvalidDataException("Analytics 配置必填字段为空：" + key + "。");
    }
    return value.Trim();
  }

  private static string OptionalString(Dictionary<string, object> payload, string key)
  {
    return payload != null && payload.ContainsKey(key)
      ? BizzaValueUtil.AsString(payload[key], string.Empty)
      : string.Empty;
  }

  private static int RequireInt(Dictionary<string, object> payload, string key)
  {
    RequireField(payload, key);
    return BizzaValueUtil.AsInt(payload[key], int.MinValue);
  }

  private static double RequireDouble(Dictionary<string, object> payload, string key)
  {
    RequireField(payload, key);
    return BizzaValueUtil.AsDouble(payload[key], double.NaN);
  }

  private static bool RequireBool(Dictionary<string, object> payload, string key)
  {
    RequireField(payload, key);
    string raw = BizzaValueUtil.AsString(payload[key], string.Empty);
    if (payload[key] is bool)
    {
      return (bool)payload[key];
    }
    if (bool.TryParse(raw, out bool value))
    {
      return value;
    }
    throw new InvalidDataException("Analytics 配置布尔字段无效：" + key + "。");
  }

  private static bool OptionalBool(Dictionary<string, object> payload, string key, bool fallback)
  {
    if (payload == null || !payload.ContainsKey(key) || payload[key] == null)
    {
      return fallback;
    }

    if (payload[key] is bool)
    {
      return (bool)payload[key];
    }

    string raw = BizzaValueUtil.AsString(payload[key], string.Empty);
    if (bool.TryParse(raw, out bool value))
    {
      return value;
    }

    throw new InvalidDataException("Analytics 配置布尔字段无效：" + key + "。");
  }
}
