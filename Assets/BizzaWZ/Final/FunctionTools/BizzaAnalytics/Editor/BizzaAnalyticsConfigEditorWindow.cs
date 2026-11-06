#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class BizzaAnalyticsConfigEditorWindow : EditorWindow
{
  private BizzaAnalyticsConfig _config;
  private string _status = string.Empty;
  private MessageType _statusType = MessageType.Info;

  [MenuItem("Bizza/Analytics Config")]
  private static void OpenWindow()
  {
    BizzaAnalyticsConfigEditorWindow window = GetWindow<BizzaAnalyticsConfigEditorWindow>();
    window.titleContent = new GUIContent("Bizza Analytics Config");
    window.minSize = new Vector2(620f, 630f);
    window.Show();
  }

  private void OnEnable()
  {
    CreateDefaultConfig();
    TryLoadConfig(false);
  }

  private void OnGUI()
  {
    EditorGUILayout.LabelField("Bizza Analytics 配置", EditorStyles.boldLabel);
    EditorGUILayout.HelpBox(
      "这里生成包含 AppId 的加密二进制配置。运行时首次上报会自动初始化，不再要求业务代码单独传入 AppId。",
      MessageType.Info);

    EditorGUILayout.Space(6f);
    DrawPackageSelector();
    DrawPath();
    DrawConfigFields();
    DrawValidation();
    DrawToolbar();
    DrawStatus();
  }

  private void DrawPackageSelector()
  {
    EditorGUILayout.LabelField("配置目标", EditorStyles.boldLabel);
    EditorGUILayout.HelpBox(
      "编辑器与正式包统一使用 BizzaAnalytics.bytes，DEBUG_MODE 不再切换 BizzaAnalytics 配置。",
      MessageType.Info);
  }

  private void DrawPath()
  {
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.PrefixLabel("配置文件");
    EditorGUILayout.SelectableLabel(
      GetSelectedConfigPath(),
      EditorStyles.textField,
      GUILayout.Height(EditorGUIUtility.singleLineHeight));
    EditorGUILayout.EndHorizontal();
  }

  private void DrawConfigFields()
  {
    if (_config == null)
    {
      CreateDefaultConfig();
    }

    EditorGUILayout.Space(6f);
    EditorGUILayout.LabelField("运行时配置", EditorStyles.boldLabel);

    _config.appId = EditorGUILayout.TextField("App Id", _config.appId);
    _config.ingestUrl = EditorGUILayout.TextField("Ingest Url", _config.ingestUrl);
    _config.ingestToken = EditorGUILayout.TextField("Ingest Token", _config.ingestToken);
    _config.messageFrequency = (BizzaMessageFrequency)EditorGUILayout.EnumPopup(
      "Message Frequency",
      _config.messageFrequency);
    _config.reportingRatio = EditorGUILayout.Slider(
      "Reporting Ratio",
      _config.reportingRatio,
      0f,
      1f);
    _config.formalPackageReportingEnabled = EditorGUILayout.Toggle(
      "正式包上报数据",
      _config.formalPackageReportingEnabled);
    _config.encryptionEnabled = EditorGUILayout.Toggle("Encryption Enabled", _config.encryptionEnabled);
    _config.encryptionKeyBase64 = EditorGUILayout.TextField(
      "Encryption Key Base64",
      _config.encryptionKeyBase64);
    _config.encryptionKid = EditorGUILayout.TextField("Encryption Kid", _config.encryptionKid);
    _config.verboseLog = EditorGUILayout.Toggle("Verbose Log", _config.verboseLog);

    EditorGUILayout.Space(4f);
    EditorGUILayout.HelpBox(
      "“正式包上报数据”只控制非 Development 的正式 Release 包；Editor 和 Development Build 仍可用于测试上报。运行时必要字段：appId、ingestUrl、ingestToken、messageFrequency、reportingRatio、encryptionEnabled；开启数据加密时还必须填写 encryptionKeyBase64 和 encryptionKid。",
      MessageType.None);
  }

  private void DrawValidation()
  {
    string validationError;
    if (BizzaAnalyticsConfigSerializer.ValidateConfig(_config, out validationError))
    {
      EditorGUILayout.HelpBox("配置校验通过，可以保存。", MessageType.Info);
    }
    else
    {
      EditorGUILayout.HelpBox("配置校验失败：" + validationError, MessageType.Error);
    }
  }

  private void DrawToolbar()
  {
    EditorGUILayout.Space(8f);
    EditorGUILayout.BeginHorizontal();

    if (GUILayout.Button("新建", GUILayout.Height(28f)))
    {
      if (EditorUtility.DisplayDialog("新建 Analytics 配置", "当前编辑内容不会保存，确定继续吗？", "确定", "取消"))
      {
        CreateDefaultConfig();
        SetStatus("已创建新的编辑配置。", MessageType.Info);
      }
    }

    if (GUILayout.Button("读取配置", GUILayout.Height(28f)))
    {
      TryLoadConfig(true);
    }

    string validationError;
    bool canSave = BizzaAnalyticsConfigSerializer.ValidateConfig(_config, out validationError);
    EditorGUI.BeginDisabledGroup(!canSave);
    if (GUILayout.Button("保存加密配置", GUILayout.Height(28f)))
    {
      SaveConfig();
    }
    EditorGUI.EndDisabledGroup();

    EditorGUILayout.EndHorizontal();
  }

  private void DrawStatus()
  {
    if (!string.IsNullOrEmpty(_status))
    {
      EditorGUILayout.Space(6f);
      EditorGUILayout.HelpBox(_status, _statusType);
    }
  }

  private void CreateDefaultConfig()
  {
    _config = new BizzaAnalyticsConfig
    {
      appId = string.Empty,
      ingestUrl = "https://ingest.popemusic.us",
      ingestToken = string.Empty,
      messageFrequency = BizzaMessageFrequency.Default,
      reportingRatio = 1f,
      formalPackageReportingEnabled = true,
      encryptionEnabled = true,
      encryptionKeyBase64 = BizzaAnalyticsConfigSerializer.DefaultPayloadEncryptionKeyBase64,
      encryptionKid = BizzaAnalyticsConfigSerializer.DefaultPayloadEncryptionKid,
      verboseLog = false,
    };
  }

  private bool TryLoadConfig(bool showDialog)
  {
    string path = GetSelectedConfigPath();
    string fileName = GetSelectedConfigFileName();
    if (!File.Exists(path))
    {
      SetStatus("尚未找到配置文件：" + path, MessageType.Warning);
      return false;
    }

    try
    {
      BizzaAnalyticsConfig config = BizzaAnalyticsConfigSerializer.Deserialize(File.ReadAllBytes(path));
      _config = config;
      SetStatus("读取配置成功：" + path, MessageType.Info);
      if (showDialog)
      {
        EditorUtility.DisplayDialog("读取成功", fileName + " 读取成功。", "确定");
      }
      return true;
    }
    catch (Exception exception)
    {
      SetStatus("读取配置失败：" + exception.Message, MessageType.Error);
      Debug.LogError("[BizzaAnalytics] EditorWindow 读取配置失败：\n" + exception);
      if (showDialog)
      {
        EditorUtility.DisplayDialog("读取失败", exception.Message, "确定");
      }
      return false;
    }
  }

  private void SaveConfig()
  {
    string validationError;
    if (!BizzaAnalyticsConfigSerializer.ValidateConfig(_config, out validationError))
    {
      SetStatus("保存失败：" + validationError, MessageType.Error);
      return;
    }

    string path = GetSelectedConfigPath();
    string fileName = GetSelectedConfigFileName();
    try
    {
      string directory = Path.GetDirectoryName(path);
      if (!string.IsNullOrEmpty(directory))
      {
        Directory.CreateDirectory(directory);
      }

      byte[] data = BizzaAnalyticsConfigSerializer.Serialize(_config);
      File.WriteAllBytes(path, data);
      AssetDatabase.Refresh();

      SetStatus("保存成功，大小：" + data.Length + " Bytes。", MessageType.Info);
      Debug.Log("[BizzaAnalytics] 加密配置保存成功：" + path + "，大小：" + data.Length + " Bytes");
      EditorUtility.DisplayDialog("保存成功", fileName + " 已保存。", "确定");
    }
    catch (Exception exception)
    {
      SetStatus("保存失败：" + exception.Message, MessageType.Error);
      Debug.LogError("[BizzaAnalytics] EditorWindow 保存配置失败：\n" + exception);
      EditorUtility.DisplayDialog("保存失败", exception.Message, "确定");
    }
  }

  private void SetStatus(string message, MessageType type)
  {
    _status = message ?? string.Empty;
    _statusType = type;
    Repaint();
  }

  private string GetSelectedConfigFileName()
  {
    return BizzaAnalyticsConfigSerializer.ConfigFileName;
  }

  private string GetSelectedConfigPath()
  {
    return BizzaAnalyticsConfigSerializer.GetConfigPath();
  }
}
#endif
