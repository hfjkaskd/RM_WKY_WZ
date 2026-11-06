using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class RemoteGroupConfigWindow : EditorWindow
{
    private const string RemoteContentAssetDirectory = "Assets/Editor/RemoteContent";

    private RemoteGroupRuntimeConfig config = RemoteGroupRuntimeConfig.CreateDefault();
    private RemoteGroupStrategy strategy = CreateDefaultStrategy();
#if BIZZA_REAL_WITHDRAW
    private ChannelABConfig channelABConfig;
#endif
    private bool editorOverrideEnabled;
    private string editorOverrideUserGroupName = RemoteGroupRuntimeConfig.DefaultUserGroupDefaultName;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Remote System/Remote Group Config")]
    private static void Open()
    {
        var window = GetWindow<RemoteGroupConfigWindow>("Remote Group Config");
        window.minSize = new Vector2(460f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadEditorOverridePrefs();
        LoadFromFile();
        LoadStrategyFromPath(GetCountryContentStrategyPath(), false);
#if BIZZA_REAL_WITHDRAW
        TryFindChannelABConfigAsset();
#endif
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Remote Group Runtime Config", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        config.RemoteRootUrl = EditorGUILayout.TextField("Remote Root Url", config.RemoteRootUrl);
        config.RemoteGroupDataName = EditorGUILayout.TextField("Group Data Name", config.RemoteGroupDataName);
        config.GameDataDirectoryName = EditorGUILayout.TextField("Game Data Directory", config.GameDataDirectoryName);
        config.CommercialDirectoryName = EditorGUILayout.TextField("Commercial Directory", config.CommercialDirectoryName);
        config.CommercialConfigName = EditorGUILayout.TextField("Commercial Config Name", config.CommercialConfigName);
        config.StartCommercialGroupIndexAtOne = EditorGUILayout.Toggle(
            "Commercial Index Starts At One",
            config.StartCommercialGroupIndexAtOne);
        config.CommonDirectoryName = EditorGUILayout.TextField("Common Directory", config.CommonDirectoryName);
        config.Timeout = EditorGUILayout.FloatField("Timeout", config.Timeout);
        config.RetryTimes = EditorGUILayout.IntField("Retry Times", config.RetryTimes);
        config.UserGroupDefaultName = EditorGUILayout.TextField("Default Group", config.UserGroupDefaultName);
        config.UserGroupNamePrefsKey = EditorGUILayout.TextField("Group Prefs Key", config.UserGroupNamePrefsKey);
        config.CountryGroupEnabled = EditorGUILayout.Toggle("Country Group Enabled", config.CountryGroupEnabled);
        if (config.CountryGroupEnabled)
        {
            EditorGUILayout.HelpBox($"开启后，具体国家使用对应国家分组策略；未使用国家目录时统一使用 {config.CommonDirectoryName} 公共目录；开启国家相关配置且 CountryType.None 时使用本地 Default。", MessageType.Info);
        }

        config.CountryLevelEnabled = EditorGUILayout.Toggle("Country Level Enabled", config.CountryLevelEnabled);
        if (config.CountryLevelEnabled)
        {
            EditorGUILayout.HelpBox($"开启后，具体国家的非 Default 关卡使用对应国家目录；关闭时统一使用 {config.CommonDirectoryName} 公共目录；开启国家相关配置且 CountryType.None 时使用本地 Default。", MessageType.Info);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox($"Export path: {RemoteGroupRuntimeConfig.ResourcesAssetPath}", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Load Exported Config"))
            {
                LoadFromFile();
            }

            if (GUILayout.Button("Reset Defaults"))
            {
                config = RemoteGroupRuntimeConfig.CreateDefault();
            }
        }

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Export Encrypted Config", GUILayout.Height(32f)))
        {
            ExportEncryptedConfig();
        }

        EditorGUILayout.Space(16f);
        DrawCurrentAssignmentStatus();

        EditorGUILayout.Space(16f);
        DrawEditorOverrideConfig();

        EditorGUILayout.Space(16f);
        DrawStrategyConfig();

        EditorGUILayout.EndScrollView();
    }

    private void LoadFromFile()
    {
        config = RemoteGroupRuntimeConfig.CreateDefault();

        try
        {
            if (!File.Exists(RemoteGroupRuntimeConfig.ResourcesAssetPath))
            {
                return;
            }

            var encryptedText = RemoteTextUtility.NormalizeText(File.ReadAllText(RemoteGroupRuntimeConfig.ResourcesAssetPath));
            if (!RemoteGroupConfigCrypto.TryDecryptToText(encryptedText, out var configJson))
            {
                return;
            }

            var loadedConfig = RemoteJsonUtility.FromJson<RemoteGroupRuntimeConfig>(configJson);
            if (loadedConfig != null)
            {
                config = loadedConfig.CloneSanitized();
            }
        }
        catch
        {
            config = RemoteGroupRuntimeConfig.CreateDefault();
        }
    }

    private void ExportEncryptedConfig()
    {
        try
        {
#if BIZZA_REAL_WITHDRAW
            if (channelABConfig != null)
            {
                config.CommercialDirectoryName = RemoteGroupRuntimeConfig.DefaultCommercialDirectoryName;
                config.CommercialConfigName = channelABConfig.groupConfigName;
                config.StartCommercialGroupIndexAtOne = channelABConfig.StartGroupIndexAtOne;
            }
#endif
            config = config.CloneSanitized();
            var configJson = JsonUtility.ToJson(config, true);
            var encryptedText = RemoteGroupConfigCrypto.EncryptToText(configJson);
            var directory = Path.GetDirectoryName(RemoteGroupRuntimeConfig.ResourcesAssetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(RemoteGroupRuntimeConfig.ResourcesAssetPath, encryptedText, RemoteTextUtility.Utf8WithoutBom);
            AssetDatabase.ImportAsset(RemoteGroupRuntimeConfig.ResourcesAssetPath);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Remote Group Config", "Encrypted config exported.", "OK");
        }
        catch (System.Exception exception)
        {
            EditorUtility.DisplayDialog("Remote Group Config", $"Export failed: {exception.Message}", "OK");
        }
    }

    private void DrawCurrentAssignmentStatus()
    {
        EditorGUILayout.LabelField("Current Assignment", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        var dataSystem = RemoteGroupDataSystem.current;
        var groupName = dataSystem.GetUserGroupName();
        var source = dataSystem.GetCurrentDataSource();
        var assignmentCompleted = dataSystem.HasCompletedAssignment();
        var path = dataSystem.GetCurrentGameDataPath();

        EditorGUILayout.LabelField("Current Group", groupName);
        EditorGUILayout.LabelField("Assignment Completed", assignmentCompleted ? "Yes" : "No");
        EditorGUILayout.LabelField("Data Source", GetDataSourceLabel(source));
        EditorGUILayout.LabelField("Game Data Path", path);

        if (!dataSystem.TryGetCurrentGameDataBytes(out var gameData))
        {
            EditorGUILayout.HelpBox("Current game data is not available. Runtime does not parse level data yet; this window only inspects the stored binary.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("First Level Decoded Preview", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField($"Raw size: {gameData.Length} bytes");
        EditorGUILayout.TextArea(RemoteGroupLevelPreviewUtility.BuildPreview(gameData), GUILayout.MinHeight(220f));
        EditorGUILayout.HelpBox("当前窗口会在编辑器侧解码包头、公共字段和第一关记录。项目运行时暂不读取关卡二进制，这个预览不会带入真机。", MessageType.Info);
    }

    private static string GetDataSourceLabel(RemoteGroupDataSource source)
    {
        switch (source)
        {
            case RemoteGroupDataSource.LocalDefault:
                return "Local Default (StreamingAssets)";
            case RemoteGroupDataSource.Remote:
                return "Remote Download (Sandbox)";
            default:
                return "Not Loaded";
        }
    }

    private void DrawStrategyConfig()
    {
        EnsureStrategy();

        EditorGUILayout.LabelField("Remote Group Strategy", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUILayout.LabelField("Output Directory", RemoteContentAssetDirectory);
        EditorGUILayout.LabelField("Output File Name", config.RemoteGroupDataName);

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox("两个导出按钮统一写入 Assets/Editor/RemoteContent。把其中的目录和 .bytes 文件复制到 Assets/StreamingAssets/RemoteGroup，或上传到服务器根目录；不要复制 .meta 文件。Editor 下的文件不会进入包体。", MessageType.Info);

#if BIZZA_REAL_WITHDRAW
        EditorGUILayout.Space(4f);
        channelABConfig = (ChannelABConfig)EditorGUILayout.ObjectField(
            "Commercial Config",
            channelABConfig,
            typeof(ChannelABConfig),
            false);

        if (channelABConfig == null)
        {
            EditorGUILayout.HelpBox("未指定 ChannelABConfig：只导出分组策略。需要远端商业化配置时再指定资源。", MessageType.Info);
        }
        else
        {
            var indexMode = channelABConfig.StartGroupIndexAtOne ? "从 1 开始" : "从 0 开始";
            var countryCount = channelABConfig.gameAB_CustomDatas == null ? 0 : channelABConfig.gameAB_CustomDatas.Count;
            EditorGUILayout.HelpBox($"商业化配置：{countryCount} 个国家配置，文件前缀：{channelABConfig.groupConfigName}，分组序号{indexMode}。文件统一导出到 {RemoteContentAssetDirectory}/commercial/ 或对应国家/commercial/ 目录；None 使用本地配置。", MessageType.Info);
        }
#endif

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("User Group Name", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Weight", EditorStyles.miniBoldLabel, GUILayout.Width(96f));
            GUILayout.Space(72f);
        }

        for (var i = 0; i < strategy.Datas.Count; i++)
        {
            var data = strategy.Datas[i];
            if (data == null)
            {
                data = new GroupData();
                strategy.Datas[i] = data;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                data.UserGroupName = EditorGUILayout.TextField(data.UserGroupName);
                data.Weight = EditorGUILayout.FloatField(data.Weight, GUILayout.Width(96f));

                if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                {
                    strategy.Datas.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Group"))
            {
                strategy.Datas.Add(new GroupData { UserGroupName = config.UserGroupDefaultName, Weight = 1f });
            }

            if (GUILayout.Button("Reset Strategy"))
            {
                strategy = CreateDefaultStrategy();
            }
        }

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Load Strategy"))
            {
                LoadStrategyFromPath(GetCountryContentStrategyPath());
            }

            if (GUILayout.Button("Export Strategy", GUILayout.Height(28f)))
            {
                ExportStrategy();
            }
        }
#if BIZZA_REAL_WITHDRAW
        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox("按当前分组与国家设置导出策略和商业化内容；需要随包兜底时，将目录内容复制到 StreamingAssets/RemoteGroup。", MessageType.Info);
        if (GUILayout.Button("一键导出运行时配置与内容", GUILayout.Height(32f)))
        {
            ExportCountryContent();
        }
#endif
    }

    private void DrawEditorOverrideConfig()
    {
        EditorGUILayout.LabelField("Editor User Group Override", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUI.BeginChangeCheck();
        editorOverrideEnabled = EditorGUILayout.Toggle("Enable In Editor", editorOverrideEnabled);
        using (new EditorGUI.DisabledScope(!editorOverrideEnabled))
        {
            editorOverrideUserGroupName = EditorGUILayout.TextField("User Group Name", editorOverrideUserGroupName);
        }

        if (EditorGUI.EndChangeCheck())
        {
            SaveEditorOverridePrefs();
        }

        if (GUILayout.Button("Clear Cached Remote Group", GUILayout.Height(26f)))
        {
            ClearCachedRemoteGroup();
        }

        EditorGUILayout.HelpBox("Only affects Unity Editor play mode. Disable it to simulate the real player weighted strategy cache.", MessageType.Info);
    }

    private void LoadEditorOverridePrefs()
    {
        editorOverrideEnabled = RemoteGroupEditorOverride.Enabled;
        editorOverrideUserGroupName = RemoteGroupEditorOverride.UserGroupName;
    }

    private void SaveEditorOverridePrefs()
    {
        RemoteGroupEditorOverride.Enabled = editorOverrideEnabled;
        RemoteGroupEditorOverride.UserGroupName = editorOverrideUserGroupName;
        editorOverrideUserGroupName = RemoteGroupEditorOverride.UserGroupName;
    }

    private void ClearCachedRemoteGroup()
    {
        RemoteGroupDataSystem.current.ClearCachedRemoteData();
        RemoteGroupEditorOverride.ClearSavedGroupMarker();
        EditorUtility.DisplayDialog("Remote Group Config", "Cached remote group data cleared.", "OK");
    }

    private void LoadStrategyFromPath(string path, bool showDialog = true)
    {
        try
        {
            if (!File.Exists(path))
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Remote Group Strategy", $"Strategy file not found: {path}", "OK");
                }

                return;
            }

            var loadedStrategy = LoadStrategyByExtension(path);
            if (loadedStrategy == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog("Remote Group Strategy", "Strategy parse failed.", "OK");
                }

                return;
            }

            strategy = loadedStrategy;
            EnsureStrategy();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Remote Group Strategy", "Strategy loaded.", "OK");
            }
        }
        catch (System.Exception exception)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Remote Group Strategy", $"Load failed: {exception.Message}", "OK");
            }
        }
    }

    private RemoteGroupStrategy LoadStrategyByExtension(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".bytes", StringComparison.OrdinalIgnoreCase))
        {
            var encryptedText = RemoteTextUtility.NormalizeText(File.ReadAllText(path));
            if (!RemoteGroupConfigCrypto.TryDecryptToText(encryptedText, out var base64Binary))
            {
                throw new InvalidDataException("Binary group strategy decrypt failed.");
            }

            var strategyBytes = Convert.FromBase64String(base64Binary.Trim());
            return RemoteGroupBinaryUtility.FromStrategyBytes(strategyBytes);
        }

        throw new InvalidDataException($"Unsupported group strategy extension: {extension}");
    }

    private void ExportStrategy()
    {
        try
        {
            ExportContentFiles(out var strategyCount, out var commercialCount);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Remote Group Strategy",
                $"导出目录：{GetRemoteContentRootPath()}\n分组策略：{strategyCount} 个\n商业化配置：{commercialCount} 个\n把目录结构和 .bytes 文件复制到 StreamingAssets/RemoteGroup，或上传到服务器根目录；不要复制 .meta。",
                "OK");
        }
        catch (System.Exception exception)
        {
            EditorUtility.DisplayDialog("Remote Group Strategy", $"Export failed: {exception.Message}", "OK");
        }
    }

    private static string GetRemoteContentRootPath()
    {
        return Path.Combine(Application.dataPath, "Editor", "RemoteContent");
    }

    private string GetCountryContentStrategyPath()
    {
        return Path.Combine(GetRemoteContentRootPath(), config.CommonDirectoryName, config.RemoteGroupDataName);
    }

    private void ExportContentFiles(out int strategyCount, out int commercialCount)
    {
        EnsureStrategy();
#if BIZZA_REAL_WITHDRAW
        if (channelABConfig != null)
        {
            ValidateChannelABConfigForExport();
            config.CommercialDirectoryName = RemoteGroupRuntimeConfig.DefaultCommercialDirectoryName;
            config.CommercialConfigName = channelABConfig.groupConfigName;
            config.StartCommercialGroupIndexAtOne = channelABConfig.StartGroupIndexAtOne;
        }
#endif
        config = config.CloneSanitized();
        var strategyBytes = RemoteGroupBinaryUtility.ToStrategyBytes(strategy);
        var encryptedText = RemoteGroupConfigCrypto.EncryptToText(Convert.ToBase64String(strategyBytes));
        strategyCount = ExportRemoteContentStrategyCopies(encryptedText).Count;
#if BIZZA_REAL_WITHDRAW
        commercialCount = channelABConfig == null ? 0 : ChannelABConfig.ExportConfigurationFile(
            channelABConfig, GetRemoteContentRootPath()).Count;
#else
        commercialCount = 0;
#endif
    }

    private List<string> ExportRemoteContentStrategyCopies(string encryptedText)
    {
        return ExportStrategyCopies(GetRemoteContentRootPath(), encryptedText);
    }

    private List<string> ExportStrategyCopies(string root, string encryptedText)
    {
        var exportedPaths = new List<string>();
        var fileName = config.RemoteGroupDataName;
        var commonDirectory = Path.Combine(root, config.CommonDirectoryName);
        Directory.CreateDirectory(commonDirectory);
        var commonPath = Path.Combine(commonDirectory, fileName);
        File.WriteAllText(commonPath, encryptedText, RemoteTextUtility.Utf8WithoutBom);
        exportedPaths.Add(commonPath);

        if (config.CountryGroupEnabled)
        {
            var countries = new HashSet<AccountModule.E_CountryType>();
#if BIZZA_REAL_WITHDRAW
            if (channelABConfig?.gameAB_CustomDatas != null)
            {
                foreach (var countryData in channelABConfig.gameAB_CustomDatas)
                {
                    if (countryData != null && countryData.e_CountryType != AccountModule.E_CountryType.None)
                    {
                        countries.Add(countryData.e_CountryType);
                    }
                }
            }
#endif
            if (countries.Count == 0)
            {
                foreach (AccountModule.E_CountryType country in Enum.GetValues(typeof(AccountModule.E_CountryType)))
                {
                    if (country != AccountModule.E_CountryType.None) countries.Add(country);
                }
            }

            foreach (var country in countries)
            {
                var countryDirectory = Path.Combine(root, country.ToString());
                Directory.CreateDirectory(countryDirectory);
                var countryPath = Path.Combine(countryDirectory, fileName);
                File.WriteAllText(countryPath, encryptedText, RemoteTextUtility.Utf8WithoutBom);
                exportedPaths.Add(countryPath);
            }
        }
        return exportedPaths;
    }

#if BIZZA_REAL_WITHDRAW
    private void ExportCountryContent()
    {
        try
        {
            ExportContentFiles(out var strategyCount, out var commercialCount);

            var encryptedConfig = RemoteGroupConfigCrypto.EncryptToText(JsonUtility.ToJson(config, true));
            Directory.CreateDirectory(Path.GetDirectoryName(RemoteGroupRuntimeConfig.ResourcesAssetPath));
            File.WriteAllText(RemoteGroupRuntimeConfig.ResourcesAssetPath, encryptedConfig, RemoteTextUtility.Utf8WithoutBom);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("远端配置内容导出完成",
                $"导出目录：{GetRemoteContentRootPath()}\n分组策略：{strategyCount} 个\n商业化配置：{commercialCount} 个\n把目录结构和 .bytes 文件复制到 StreamingAssets/RemoteGroup，或上传到服务器根目录；不要复制 .meta。", "确定");
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("远端配置内容导出失败", exception.Message, "确定");
        }
    }

    private void TryFindChannelABConfigAsset()
    {
        var guids = AssetDatabase.FindAssets("t:ChannelABConfig");
        if (guids.Length != 1)
        {
            return;
        }

        var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        channelABConfig = AssetDatabase.LoadAssetAtPath<ChannelABConfig>(assetPath);
    }

    private void ValidateChannelABConfigForExport()
    {
        if (channelABConfig == null)
        {
            throw new InvalidDataException("请先在 Remote Group Config 窗口中指定 ChannelABConfig 资源。");
        }

        if (channelABConfig.gameAB_CustomDatas == null || channelABConfig.gameAB_CustomDatas.Count == 0)
        {
            throw new InvalidDataException("ChannelABConfig 中没有国家商业化配置。");
        }

        var countrySet = new System.Collections.Generic.HashSet<AccountModule.E_CountryType>();
        for (var i = 0; i < channelABConfig.gameAB_CustomDatas.Count; i++)
        {
            var countryData = channelABConfig.gameAB_CustomDatas[i];
            if (countryData == null)
            {
                throw new InvalidDataException($"ChannelABConfig 第 {i + 1} 项国家配置为空。");
            }

            if (!countrySet.Add(countryData.e_CountryType))
            {
                throw new InvalidDataException($"ChannelABConfig 中国家 {countryData.e_CountryType} 配置重复。");
            }

            var commercialCount = countryData.gameAB_Datas == null ? 0 : countryData.gameAB_Datas.Count;
            if (commercialCount != strategy.Datas.Count)
            {
                throw new InvalidDataException(
                    $"国家 {countryData.e_CountryType} 的商业化配置数量必须和分组策略数量一致：策略={strategy.Datas.Count}，商业化配置={commercialCount}。");
            }
        }

    }
#endif

    private void EnsureStrategy()
    {
        if (strategy == null)
        {
            strategy = CreateDefaultStrategy();
        }

        if (strategy.Datas == null)
        {
            strategy.Datas = new System.Collections.Generic.List<GroupData>();
        }
    }

    private static RemoteGroupStrategy CreateDefaultStrategy()
    {
        return new RemoteGroupStrategy
        {
            Datas = new System.Collections.Generic.List<GroupData>
            {
                new GroupData
                {
                    UserGroupName = RemoteGroupRuntimeConfig.DefaultUserGroupDefaultName,
                    Weight = 1f
                }
            }
        };
    }
}
