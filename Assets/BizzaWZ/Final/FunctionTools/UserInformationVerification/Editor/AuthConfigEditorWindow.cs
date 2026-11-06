#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bizza.TokenClientSystem.Editor
{
    /// <summary>
    /// TokenClient AuthConfig 编辑器窗口。
    /// 使用 Unity 原生 IMGUI 绘制配置，使用 AuthConfigBinarySerializer 持久化到：
    /// Assets/StreamingAssets/AuthConfig.bytes
    /// </summary>
    public sealed class AuthConfigEditorWindow : EditorWindow
    {
        private const string ConfigFileName = "AuthConfig.bytes";
        private const string SessionBackupKeyPrefix =
            "Bizza.AuthConfigEditorWindow.SessionBackup.";

        private AuthConfigData config;
        private Vector2 scrollPosition;
        private bool showAuthSettings = true;
        private bool showMockDeviceSettings = true;
        private bool assemblyReloadCallbackRegistered;

        [MenuItem("Bizza/Token Client/Auth Config")]
        private static void OpenWindow()
        {
            AuthConfigEditorWindow window = GetWindow<AuthConfigEditorWindow>();
            window.titleContent = new GUIContent("Token Client Auth Config");
            window.minSize = new Vector2(800, 620);
            window.Show();
        }

        private void OnEnable()
        {
            if (!assemblyReloadCallbackRegistered)
            {
                AssemblyReloadEvents.beforeAssemblyReload += SaveSessionBackupBeforeAssemblyReload;
                assemblyReloadCallbackRegistered = true;
            }

            if (!TryLoadSessionBackup() && !TryLoadFromStreamingAssets(false))
            {
                CreateNewConfig();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "AppId 在独立模式下从 AuthConfig.bytes 读取；接入 SDK 后，" +
                "优先使用宿主或公共 DeviceInfoUtil 提供的值，初始化时无需重复传入 AppId。",
                MessageType.Info);

            if (config == null)
            {
                CreateNewConfig();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawConfigFields();
            EditorGUILayout.EndScrollView();
        }

        private void OnDisable()
        {
            if (assemblyReloadCallbackRegistered)
            {
                AssemblyReloadEvents.beforeAssemblyReload -= SaveSessionBackupBeforeAssemblyReload;
                assemblyReloadCallbackRegistered = false;
            }

        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                NewConfig();
            }

            if (GUILayout.Button("读取配置", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                LoadFromStreamingAssets();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("保存配置", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                SaveToStreamingAssets();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("保存路径", GUILayout.Width(60));
            EditorGUILayout.SelectableLabel(
                GetConfigPath(),
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConfigFields()
        {
            showAuthSettings = EditorGUILayout.Foldout(
                showAuthSettings,
                "授权基础配置",
                true);
            if (showAuthSettings)
            {
                EditorGUI.indentLevel++;
                config.AppId = EditorGUILayout.TextField(
                    new GUIContent(
                        "AppId",
                        "独立运行模式使用；接入 SDK 时优先由宿主或公共 DeviceInfoUtil 提供。"),
                    config.AppId);
                config.AuthTokenKey = EditorGUILayout.TextField(
                    new GUIContent(
                        "本地 Token 键名",
                        "PlayerPrefs 中保存服务端授权 Token 使用的键名。"),
                    config.AuthTokenKey);
                config.WorkerUrl = EditorGUILayout.TextField(
                    new GUIContent(
                        "Worker 地址",
                        "Token 授权请求的服务器接口地址。"),
                    config.WorkerUrl);
                config.RequestTimeoutSeconds = Mathf.Max(
                    1,
                    EditorGUILayout.IntField(
                        "请求超时（秒）",
                        config.RequestTimeoutSeconds));
                config.EnableDebugResponse = EditorGUILayout.Toggle(
                    new GUIContent(
                        "保留服务端调试信息",
                        "开启后会保留并打印服务端返回的 debugInfo。正式包建议关闭。"),
                    config.EnableDebugResponse);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4f);
            showMockDeviceSettings = EditorGUILayout.Foldout(
                showMockDeviceSettings,
                "编辑器模拟设备",
                true);
            if (showMockDeviceSettings)
            {
                EditorGUI.indentLevel++;
                config.UseEditorMockDeviceInfo = EditorGUILayout.Toggle(
                    new GUIContent(
                        "启用模拟设备信息",
                        "仅 Unity Editor 使用；真机仍然采集 Android 原生设备信息。"),
                    config.UseEditorMockDeviceInfo);
                config.EditorNativeLocale = EditorGUILayout.TextField(
                    "系统语言区域",
                    config.EditorNativeLocale);
                config.EditorNativeLocaleCountry = EditorGUILayout.TextField(
                    "系统国家/地区",
                    config.EditorNativeLocaleCountry);
                config.EditorNativeCountryCode = EditorGUILayout.TextField(
                    "原生国家代码",
                    config.EditorNativeCountryCode);
                config.EditorSimCountryIso = EditorGUILayout.TextField(
                    "SIM 国家代码",
                    config.EditorSimCountryIso);
                config.EditorNetworkCountryIso = EditorGUILayout.TextField(
                    "网络国家代码",
                    config.EditorNetworkCountryIso);
                config.EditorSimOperator = EditorGUILayout.TextField(
                    "SIM 运营商 MCC/MNC",
                    config.EditorSimOperator);
                config.EditorNetworkOperator = EditorGUILayout.TextField(
                    "网络运营商 MCC/MNC",
                    config.EditorNetworkOperator);
                config.EditorNetworkOperatorName = EditorGUILayout.TextField(
                    "网络运营商名称",
                    config.EditorNetworkOperatorName);
                config.EditorDefaultInputMethod = EditorGUILayout.TextField(
                    "默认输入法",
                    config.EditorDefaultInputMethod);
                config.EditorEnabledInputMethods = EditorGUILayout.TextField(
                    "已启用输入法列表",
                    config.EditorEnabledInputMethods);
                config.EditorIsVpnConnected = EditorGUILayout.Toggle(
                    "模拟 VPN 连接",
                    config.EditorIsVpnConnected);
                EditorGUI.indentLevel--;
            }
        }

        private void CreateNewConfig()
        {
            config = AuthConfigData.CreateDefault();
            AuthConfig.Apply(config);
            Repaint();
        }

        private void NewConfig()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "新建配置",
                "确定创建新的 AuthConfig？\n\n当前编辑内容不会保存。",
                "确定",
                "取消");
            if (!confirm)
            {
                return;
            }

            ClearSessionBackup();
            CreateNewConfig();
            Repaint();
        }

        private static string GetStreamingAssetsPath()
        {
            return Path.Combine(Application.dataPath, "StreamingAssets");
        }

        private static string GetConfigPath()
        {
            return Path.Combine(GetStreamingAssetsPath(), ConfigFileName);
        }

        private void SaveToStreamingAssets()
        {
            if (config == null)
            {
                EditorUtility.DisplayDialog("保存失败", "AuthConfig 为空。", "确定");
                return;
            }

            try
            {
                string directory = GetStreamingAssetsPath();
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] data = AuthConfigBinarySerializer.Serialize(config);
                string path = GetConfigPath();
                File.WriteAllBytes(path, data);
                AssetDatabase.Refresh();

                Debug.Log(
                    "====================================\n" +
                    "AuthConfig 保存成功\n" +
                    $"Path: {path}\n" +
                    $"Size: {data.Length} Bytes\n" +
                    "====================================");

                EditorUtility.DisplayDialog(
                    "保存成功",
                    $"AuthConfig 保存成功！\n\n文件：\n{path}\n\n大小：{data.Length} Bytes",
                    "确定");
                ClearSessionBackup();
            }
            catch (Exception exception)
            {
                Debug.LogError("AuthConfig 保存失败：\n" + exception);
                EditorUtility.DisplayDialog("保存失败", exception.Message, "确定");
            }
        }

        private void LoadFromStreamingAssets()
        {
            string path = GetConfigPath();
            if (!TryLoadFromStreamingAssets(true))
            {
                return;
            }

            ClearSessionBackup();
            EditorUtility.DisplayDialog("读取成功", "AuthConfig 读取成功！\n\n" + path, "确定");
        }

        private bool TryLoadFromStreamingAssets(bool showErrorDialog)
        {
            string path = GetConfigPath();
            if (!File.Exists(path))
            {
                if (showErrorDialog)
                {
                    EditorUtility.DisplayDialog(
                        "读取失败",
                        "StreamingAssets 中不存在配置文件。\n\n" + path,
                        "确定");
                }

                return false;
            }

            try
            {
                ApplySerializedConfig(File.ReadAllBytes(path), path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("AuthConfig 加载失败：\n" + exception);
                if (showErrorDialog)
                {
                    EditorUtility.DisplayDialog("读取失败", exception.Message, "确定");
                }

                return false;
            }
        }

        private void ApplySerializedConfig(byte[] data, string source)
        {
            if (data == null || data.Length == 0)
            {
                throw new InvalidDataException("AuthConfig.bytes 内容为空。");
            }

            AuthConfigData loadedConfig = AuthConfigBinarySerializer.Deserialize(data);
            if (loadedConfig == null)
            {
                throw new InvalidDataException("读取出来的 AuthConfig 为空。");
            }

            config = loadedConfig;
            AuthConfig.Apply(config);
            Repaint();

            Debug.Log(
                "====================================\n" +
                "AuthConfig 加载成功\n" +
                $"Path: {source}\n" +
                "====================================");
        }

        private void SaveSessionBackupBeforeAssemblyReload()
        {
            if (config == null)
            {
                return;
            }

            try
            {
                byte[] data = AuthConfigBinarySerializer.Serialize(config);
                SessionState.SetString(
                    GetSessionBackupKey(),
                    Convert.ToBase64String(data));
            }
            catch (Exception exception)
            {
                Debug.LogError("AuthConfig 编译前临时保存失败：\n" + exception);
            }
        }

        private bool TryLoadSessionBackup()
        {
            string encoded = SessionState.GetString(GetSessionBackupKey(), string.Empty);
            if (string.IsNullOrEmpty(encoded))
            {
                return false;
            }

            try
            {
                ApplySerializedConfig(
                    Convert.FromBase64String(encoded),
                    "Unity 编译前临时备份");
                ClearSessionBackup();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "AuthConfig 临时备份恢复失败，将尝试读取正式配置：\n" + exception);
                ClearSessionBackup();
                return false;
            }
        }

        private static string GetSessionBackupKey()
        {
            return SessionBackupKeyPrefix + Application.dataPath.Replace('\\', '/');
        }

        private static void ClearSessionBackup()
        {
            SessionState.EraseString(GetSessionBackupKey());
        }
    }
}
#endif
