#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// RemoteGroup 流程测试窗口。
/// 该窗口只生成测试资源和触发 RemoteGroupDataSystem 的初始化，不参与运行时编译。
/// </summary>
public sealed class RemoteGroupTestWindow : EditorWindow
{
#if BIZZA_REAL_WITHDRAW
    private struct CountryTestResult
    {
        public AccountModule.E_CountryType Country;
        public bool Success;
        public string GroupName;
        public RemoteGroupDataSource DataSource;
        public bool AssignmentCompleted;
        public string Error;
    }
#endif

    private struct AutomatedTestResult
    {
        public string Name;
        public bool Success;
        public string Detail;
    }

    private struct RuntimeObservation
    {
        public string GroupName;
        public RemoteGroupDataSource DataSource;
        public bool AssignmentCompleted;
        public string DataPath;
        public byte[] Data;
    }

    private struct RemoteScenarioResult
    {
        public string Name;
        public bool Success;
        public int PassedCount;
        public int TotalCount;
        public string Detail;
    }

    private const string TestOutputDirectoryName = "RemoteUpload";
    private const int TestPackageHeaderSize = 0x38;

    private RemoteGroupRuntimeConfig runtimeConfig = RemoteGroupRuntimeConfig.CreateDefault();
    private bool runtimeConfigLoaded;
    private string failureGroupName = "Default";
    private bool overwriteExistingFiles = true;
#if BIZZA_REAL_WITHDRAW
    private AccountModule.E_CountryType testCountry = AccountModule.E_CountryType.None;
    private readonly List<CountryTestResult> countryTestResults = new List<CountryTestResult>();
#endif
    private bool isRunning;
    private string lastResult = "尚未执行测试。";
    private Vector2 scrollPosition;
    private int randomSampleCount = 20;
    private bool probeCommercialResources = true;
    private readonly List<AutomatedTestResult> automatedTestResults = new List<AutomatedTestResult>();
    private readonly List<RemoteScenarioResult> remoteScenarioResults = new List<RemoteScenarioResult>();

    [MenuItem("Tools/Remote System/Remote Group Test")]
    private static void Open()
    {
        var window = GetWindow<RemoteGroupTestWindow>("Remote Group Test");
        window.minSize = new Vector2(620f, 560f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadCurrentRuntimeConfig();
#if BIZZA_REAL_WITHDRAW
        testCountry = AccountModule.CountryType;
#endif
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Remote Group 流程测试", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "这个窗口用于按步骤测试远端分组、远端关卡下载、本地 Default 回退和缓存复用。测试资源直接生成到 RemoteUpload，不会进入 Unity 包体；本地 Default 测试文件会写入 StreamingAssets。",
            MessageType.Info);

        DrawTestConfig();
        EditorGUILayout.Space(8f);
        DrawAutomationTests();
        EditorGUILayout.Space(8f);
        DrawTestSteps();
        EditorGUILayout.Space(8f);
        DrawResourceStatus();
        EditorGUILayout.Space(8f);
        DrawCurrentRuntimeStatus();

        EditorGUILayout.EndScrollView();
    }

    private void DrawTestConfig()
    {
        EditorGUILayout.LabelField("当前运行时配置（只读）", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Remote Root Url", runtimeConfig.RemoteRootUrl);
        EditorGUILayout.LabelField("Group Data Name", runtimeConfig.RemoteGroupDataName);
        EditorGUILayout.LabelField("Game Data Directory", runtimeConfig.GameDataDirectoryName);
        EditorGUILayout.LabelField("Common Directory", runtimeConfig.CommonDirectoryName);
        EditorGUILayout.LabelField("Default Group", runtimeConfig.UserGroupDefaultName);
        EditorGUILayout.LabelField("Country Group Enabled", runtimeConfig.CountryGroupEnabled ? "是" : "否");
        EditorGUILayout.LabelField("Country Level Enabled", runtimeConfig.CountryLevelEnabled ? "是" : "否");
#if BIZZA_REAL_WITHDRAW
        EditorGUILayout.LabelField("AccountModule.CountryType", AccountModule.CountryType.ToString());
        if (AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            if (runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled)
            {
                EditorGUILayout.HelpBox("当前国家为 None 且已开启国家相关配置：运行时等待用户数据确定国家；最终仍为 None 时使用本地 Default。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"当前国家为 None 但未开启国家相关配置：运行时按公共模式请求 {runtimeConfig.CommonDirectoryName}/{runtimeConfig.RemoteGroupDataName} 并正常随机，不使用国家目录。", MessageType.Info);
            }
        }
#endif

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("测试参数", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("策略来源", GetStrategySourceDescription());
        EditorGUILayout.LabelField("策略中的分组", GetConfiguredStrategyGroupDescription());
        var configuredRemoteGroups = GetConfiguredRemoteGroupNamesFromFiles();
        if (configuredRemoteGroups.Count < 2)
        {
            EditorGUILayout.HelpBox(
                "当前真实策略中的非 Default 分组少于 2 个，随机不会出现多个远端分组。若要测试多个远端分组，请在 Remote Group Config 中增加对应 UserGroupName 和权重后重新导出策略。",
                MessageType.Warning);
        }
        DrawUnreferencedGameDataWarning();
        var failureGroups = GetFailureGroupNames();
        var failureGroupIndex = failureGroups.IndexOf(failureGroupName);
        if (failureGroupIndex < 0)
        {
            failureGroupIndex = 0;
            failureGroupName = failureGroups[0];
        }

        failureGroupName = failureGroups[EditorGUILayout.Popup("失败测试分组", failureGroupIndex, failureGroups.ToArray())];
        overwriteExistingFiles = EditorGUILayout.Toggle("覆盖已有测试文件", overwriteExistingFiles);

#if BIZZA_REAL_WITHDRAW
        testCountry = (AccountModule.E_CountryType)EditorGUILayout.EnumPopup("手动测试国家", testCountry);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"当前实际国家：{AccountModule.CountryType}");
            if (GUILayout.Button("应用手动测试国家", GUILayout.Width(150f)))
            {
                AccountModule.CountryType = testCountry;
                lastResult = $"已将当前编辑器测试国家设置为：{testCountry}";
                Repaint();
            }
        }
#endif

        EditorGUILayout.HelpBox(
            $"本地测试资源目录：{GetTestOutputRootPath()}\n测试窗口直接读取 Remote Group Config 导出的运行时配置，不会另外写入测试地址或国家开关。",
            MessageType.None);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("当前远端地址预览", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("分组策略地址", runtimeConfig.GroupStrategyUrl);
        var previewGroupName = GetPreviewGroupName();
        EditorGUILayout.LabelField($"关卡地址（{previewGroupName}）", runtimeConfig.BuildGameDataUrl(previewGroupName));

        if (!runtimeConfigLoaded)
        {
            EditorGUILayout.HelpBox(
                "尚未读取到 Resources/RemoteGroupRuntimeConfig，请先在 Remote Group Config 窗口导出运行时配置，再点击“重新读取 Remote Group Config”。",
                MessageType.Warning);
        }

        if (runtimeConfigLoaded && !runtimeConfig.CountryGroupEnabled && !runtimeConfig.CountryLevelEnabled)
        {
            EditorGUILayout.HelpBox(
                "当前两个国家前缀开关都未开启，运行时会使用公共目录；测试资源生成仍会准备全部国家目录，便于你之后打开任一开关后直接测试。",
                MessageType.Warning);
        }

        if (RemoteGroupEditorOverride.Enabled)
        {
            EditorGUILayout.HelpBox(
                "当前启用了 Editor User Group Override。执行真实远端流程测试前，必须关闭它，否则会跳过远端分组策略请求。",
                MessageType.Warning);
        }
    }

    private void DrawTestSteps()
    {
        EditorGUILayout.LabelField("测试步骤", EditorStyles.boldLabel);

        DrawStepStatus(1, "读取 Remote Group Config 运行时配置", runtimeConfigLoaded);
        if (GUILayout.Button("1. 重新读取 Remote Group Config", GUILayout.Height(28f)))
        {
            LoadCurrentRuntimeConfig();
        }

        DrawStepStatus(2, "读取真实策略并生成全部分组关卡二进制", HasGeneratedTestResources());
        if (GUILayout.Button("2. 生成测试资源", GUILayout.Height(28f)))
        {
            GenerateTestResources();
        }

        DrawStepStatus(3, "清理正式分组记录和沙盒缓存", !RemoteGroupDataSystem.current.HasCompletedAssignment());
        if (GUILayout.Button("3. 清理缓存并关闭编辑器覆盖", GUILayout.Height(28f)))
        {
            ClearTestState();
        }

        using (new EditorGUI.DisabledScope(isRunning))
        {
            DrawStepStatus(4, "执行远端分组初始化", !isRunning && lastResult != "尚未执行测试。");
            if (GUILayout.Button(isRunning ? "4. 正在执行测试..." : "4. 执行当前分组初始化", GUILayout.Height(32f)))
            {
                RunInitialization();
            }

            if (GUILayout.Button("清理缓存并执行当前初始化", GUILayout.Height(28f)))
            {
                RunInitialization(true);
            }
        }

        using (new EditorGUI.DisabledScope(isRunning || !runtimeConfigLoaded))
        {
            if (GUILayout.Button("检查当前远端资源", GUILayout.Height(28f)))
            {
                ProbeCurrentRemoteResources();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("打开 Remote Group Config"))
            {
                EditorApplication.ExecuteMenuItem("Tools/Remote System/Remote Group Config");
            }

            if (GUILayout.Button("删除本地测试关卡文件（仅模拟本地文件缺失）"))
            {
                DeleteSelectedRemoteGameData();
            }
        }

#if BIZZA_REAL_WITHDRAW
        using (new EditorGUI.DisabledScope(isRunning || !runtimeConfigLoaded))
        {
            if (GUILayout.Button("批量测试全部国家（包含 None 公共配置）", GUILayout.Height(30f)))
            {
                RunAllCountryTests();
            }
        }
#endif

        EditorGUILayout.HelpBox(
            "测试窗口不会生成或修改分组策略内容。远端测试以 RemoteRootUrl 指向的服务器资源为准，本地 RemoteUpload 只用于状态显示和读取分组名称。需要覆盖不同结果时，清理正式分组记录后重复执行。国家配置开启后，可用“批量测试全部国家”逐个验证 None 和当前编译版本中的所有国家。",
            MessageType.Info);
        EditorGUILayout.LabelField("最近结果", lastResult, EditorStyles.wordWrappedLabel);
    }

    private void DrawAutomationTests()
    {
        EditorGUILayout.LabelField("一键测试中心", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "推荐先点“全量远端预检”，再点“当前配置：首请求 + 复用”。预检会覆盖四种国家开关组合的地址和远端文件；运行验证使用当前已导出的运行时配置，并会清理当前分组记录和沙盒缓存。",
            MessageType.Info);

        randomSampleCount = Mathf.Clamp(
            EditorGUILayout.IntField("随机覆盖次数", randomSampleCount),
            2,
            200);
        probeCommercialResources = EditorGUILayout.Toggle("预检商业化文件", probeCommercialResources);

        using (new EditorGUI.DisabledScope(isRunning || !runtimeConfigLoaded))
        {
            if (GUILayout.Button("A. 全量远端预检（四种开关组合）", GUILayout.Height(30f)))
            {
                RunRemoteScenarioMatrixTest();
            }

            if (GUILayout.Button("B. 当前配置：首请求 + 正式记录复用", GUILayout.Height(30f)))
            {
                RunFirstRequestAndReuseTest();
            }

            if (GUILayout.Button("C. 当前策略随机覆盖测试", GUILayout.Height(30f)))
            {
                RunRandomCoverageTest();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("检查本地 Default 回退条件"))
            {
                CheckLocalDefaultFallback();
            }

            if (GUILayout.Button("清空自动化结果"))
            {
                automatedTestResults.Clear();
                remoteScenarioResults.Clear();
                lastResult = "已清空自动化测试结果。";
                Repaint();
            }
        }

        if (remoteScenarioResults.Count > 0)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("远端预检结果", EditorStyles.miniBoldLabel);
            for (var i = 0; i < remoteScenarioResults.Count; i++)
            {
                var result = remoteScenarioResults[i];
                var messageType = result.Success ? MessageType.Info : MessageType.Error;
                EditorGUILayout.HelpBox(
                    $"{(result.Success ? "通过" : "失败")}：{result.Name}（{result.PassedCount}/{result.TotalCount}）\n{result.Detail}",
                    messageType);
            }
        }

        if (automatedTestResults.Count > 0)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("自动化运行结果", EditorStyles.miniBoldLabel);
            for (var i = 0; i < automatedTestResults.Count; i++)
            {
                var result = automatedTestResults[i];
                EditorGUILayout.HelpBox(
                    $"{(result.Success ? "通过" : "失败")}：{result.Name}\n{result.Detail}",
                    result.Success ? MessageType.Info : MessageType.Error);
            }
        }
    }

    private void DrawResourceStatus()
    {
        EditorGUILayout.LabelField("测试资源状态", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("公共分组策略", GetStatusLabel(File.Exists(GetTestStrategyPath()), GetTestStrategyPath()));
        EditorGUILayout.LabelField("本地 Default", GetStatusLabel(File.Exists(GetLocalDefaultPath()), GetLocalDefaultPath()));
#if BIZZA_REAL_WITHDRAW
        DrawGameDataStatus(AccountModule.E_CountryType.None);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("国家资源矩阵", EditorStyles.miniBoldLabel);
        var countries = GetTestCountries();
        for (var i = 0; i < countries.Count; i++)
        {
            var country = countries[i];
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{country} 目录", country == AccountModule.E_CountryType.None ? GetCommonOutputRootPath() : GetCountryOutputRootPath(country));
                if (country == AccountModule.E_CountryType.None)
                {
                    EditorGUILayout.LabelField(
                        "运行时行为",
                        runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled
                            ? "等待国家确定；最终为 None 时使用本地 Default"
                            : $"公共模式请求 {runtimeConfig.CommonDirectoryName}/{runtimeConfig.RemoteGroupDataName}；Default 本地，非 Default 使用 {runtimeConfig.CommonDirectoryName}/{runtimeConfig.GameDataDirectoryName} 下的公共远端关卡");
                    EditorGUILayout.LabelField("分组策略", GetStatusLabel(File.Exists(GetTestStrategyPath()), GetTestStrategyPath()));
                    DrawGameDataStatus(country);
                }
                else
                {
                    if (runtimeConfig.CountryGroupEnabled)
                    {
                        EditorGUILayout.LabelField("分组策略", GetStatusLabel(File.Exists(GetCountryStrategyPath(country)), GetCountryStrategyPath(country)));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("分组策略", "未启用国家分组路径");
                    }

                    if (runtimeConfig.CountryLevelEnabled)
                    {
                        DrawGameDataStatus(country);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("关卡配置", "未启用国家关卡路径");
                    }
                }
            }
        }
#endif

#if BIZZA_REAL_WITHDRAW
        DrawCountryTestResults();
#endif
    }

#if BIZZA_REAL_WITHDRAW
    private void DrawGameDataStatus(AccountModule.E_CountryType country)
    {
        var configuredGroupNames = GetConfiguredRemoteGroupNamesFromFiles();
        if (configuredGroupNames.Count == 0)
        {
            EditorGUILayout.LabelField("远端非 Default 关卡", "尚未从真实策略中读取到非 Default 分组");
            return;
        }

        for (var i = 0; i < configuredGroupNames.Count; i++)
        {
            var groupName = configuredGroupNames[i];
            var path = country == AccountModule.E_CountryType.None || !runtimeConfig.CountryLevelEnabled
                ? GetRemoteGameDataPath(groupName)
                : GetCountryGameDataPath(country, groupName);
            EditorGUILayout.LabelField($"{groupName} 关卡", GetStatusLabel(File.Exists(path), path));
        }
    }

    private void DrawCountryTestResults()
    {
        if (countryTestResults.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("批量国家测试结果", EditorStyles.miniBoldLabel);
        for (var i = 0; i < countryTestResults.Count; i++)
        {
            var result = countryTestResults[i];
            var status = result.Success
                ? $"成功：分组={result.GroupName}，来源={result.DataSource}，正式完成={result.AssignmentCompleted}"
                : $"失败：{result.Error}";
            EditorGUILayout.LabelField(result.Country.ToString(), status, EditorStyles.wordWrappedLabel);
        }
    }
#endif

    private void DrawCurrentRuntimeStatus()
    {
        EditorGUILayout.LabelField("当前运行时状态", EditorStyles.boldLabel);
        var dataSystem = RemoteGroupDataSystem.current;
        EditorGUILayout.LabelField("当前分组", dataSystem.GetUserGroupName());
        EditorGUILayout.LabelField("正式分组完成", dataSystem.HasCompletedAssignment() ? "是" : "否");
        EditorGUILayout.LabelField("数据来源", dataSystem.GetCurrentDataSource().ToString());
        EditorGUILayout.LabelField("当前数据路径", dataSystem.GetCurrentGameDataPath());
        EditorGUILayout.LabelField(
            "正式记录",
            PlayerPrefs.GetInt(RemoteGroupRuntimeConfig.AssignmentCompletedPrefsKey, 0) != 0
                ? $"已写入，分组={PlayerPrefs.GetString(runtimeConfig.UserGroupNamePrefsKey, string.Empty)}"
                : "未写入");
        EditorGUILayout.LabelField("沙盒策略缓存", GetStatusLabel(File.Exists(GetPersistentStrategyPath()), GetPersistentStrategyPath()));
        EditorGUILayout.LabelField("沙盒关卡缓存目录", GetPersistentGameDataDirectory());

        if (dataSystem.TryGetCurrentGameDataBytes(out var gameData))
        {
            EditorGUILayout.LabelField("当前关卡预览", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"当前二进制大小：{gameData.Length} 字节");
            EditorGUILayout.TextArea(RemoteGroupLevelPreviewUtility.BuildPreview(gameData), GUILayout.MinHeight(180f));
        }
    }

    private void DrawStepStatus(int step, string title, bool completed)
    {
        EditorGUILayout.LabelField($"步骤 {step}：{title} —— {(completed ? "已完成" : "待执行")}");
    }

    private static string GetStatusLabel(bool exists, string path)
    {
        return exists ? $"已生成：{path}" : $"未生成：{path}";
    }

    private void LoadCurrentRuntimeConfig()
    {
        runtimeConfig = RemoteGroupRuntimeConfig.CreateDefault();
        runtimeConfigLoaded = false;

        try
        {
            var asset = Resources.Load<TextAsset>(RemoteGroupRuntimeConfig.ResourcesAssetName);
            if (asset == null)
            {
                return;
            }

            if (!RemoteGroupConfigCrypto.TryDecryptToText(asset.text, out var configJson))
            {
                return;
            }

            var loadedConfig = RemoteJsonUtility.FromJson<RemoteGroupRuntimeConfig>(configJson);
            if (loadedConfig == null)
            {
                return;
            }

            runtimeConfig = loadedConfig.CloneSanitized();
            runtimeConfigLoaded = true;
        }
        catch
        {
            runtimeConfig = RemoteGroupRuntimeConfig.CreateDefault();
            runtimeConfigLoaded = false;
        }
    }

    private void GenerateTestResources()
    {
        try
        {
            if (!runtimeConfigLoaded)
            {
                throw new InvalidOperationException("没有读取到 Remote Group Config，请先在 Remote Group Config 窗口中导出运行时配置，再重新读取。");
            }

            if (!TryLoadConfiguredStrategies(out var configuredStrategies, out var strategyError, false))
            {
                throw new InvalidOperationException(strategyError);
            }

            var pathsToWrite = GetAllTestGameDataPaths(configuredStrategies);
            pathsToWrite.Add(GetTestOutputStrategyPath());
#if BIZZA_REAL_WITHDRAW
            var countryStrategyPaths = GetCountryStrategyPaths();
            pathsToWrite.AddRange(countryStrategyPaths);
#endif

            if (!overwriteExistingFiles && HasAnyExistingFile(pathsToWrite))
            {
                EditorUtility.DisplayDialog("Remote Group Test", "检测到已有测试文件，当前未开启覆盖已有测试文件。", "确定");
                return;
            }

            if (overwriteExistingFiles
                && HasAnyExistingFile(pathsToWrite)
                && !EditorUtility.DisplayDialog(
                    "覆盖测试资源",
                    "测试目录中已存在文件，继续会覆盖这些测试文件，但不会删除其它目录内容。是否继续？",
                    "覆盖",
                    "取消"))
            {
                return;
            }

            WriteBytesFile(GetLocalDefaultPath(), CreateTestGameData("Default"));
            var configuredGroupNames = GetConfiguredRemoteGroupNames(configuredStrategies);
            for (var i = 0; i < configuredGroupNames.Count; i++)
            {
                var groupName = configuredGroupNames[i];
                WriteBytesFile(GetRemoteGameDataPath(groupName), CreateTestGameData("PUBLIC_" + groupName));
            }

#if BIZZA_REAL_WITHDRAW
            var publicStrategyText = RemoteTextUtility.NormalizeText(File.ReadAllText(GetTestStrategyPath()));
            WriteTextFile(GetTestOutputStrategyPath(), publicStrategyText);
            var countries = GetTestCountries();
            for (var i = 0; i < countries.Count; i++)
            {
                var country = countries[i];
                if (country == AccountModule.E_CountryType.None)
                {
                    continue;
                }

                var exportedCountryStrategy = GetCountryStrategyPath(country);
                var countryStrategyText = File.Exists(exportedCountryStrategy)
                    ? RemoteTextUtility.NormalizeText(File.ReadAllText(exportedCountryStrategy))
                    : publicStrategyText;
                WriteTextFile(GetCountryTestStrategyPath(country), countryStrategyText);
                for (var groupIndex = 0; groupIndex < configuredGroupNames.Count; groupIndex++)
                {
                    var groupName = configuredGroupNames[groupIndex];
                    WriteBytesFile(
                        GetCountryGameDataPath(country, groupName),
                        CreateTestGameData(country + "_" + groupName));
                }
            }
#endif

#if !BIZZA_REAL_WITHDRAW
            WriteTextFile(GetTestOutputStrategyPath(),
                RemoteTextUtility.NormalizeText(File.ReadAllText(GetTestStrategyPath())));
#endif

            AssetDatabase.ImportAsset("Assets/StreamingAssets/RemoteGroup/Default.bytes");
            AssetDatabase.Refresh();
            lastResult = $"测试资源生成完成，已读取并沿用 Remote Group Config 导出的真实策略；共生成 {configuredGroupNames.Count} 个非 Default 分组的公共关卡、全部国家策略副本和全部国家关卡二进制。";
            EditorUtility.DisplayDialog("Remote Group Test", lastResult, "确定");
        }
        catch (Exception exception)
        {
            lastResult = $"生成测试资源失败：{exception.Message}";
            EditorUtility.DisplayDialog("Remote Group Test", lastResult, "确定");
        }
    }

    private List<string> GetAllTestGameDataPaths(Dictionary<string, RemoteGroupStrategy> configuredStrategies, bool includeLocalDefault = true)
    {
        var paths = new List<string>();
        if (includeLocalDefault)
        {
            paths.Add(GetLocalDefaultPath());
        }

        var configuredGroupNames = GetConfiguredRemoteGroupNames(configuredStrategies);
        for (var i = 0; i < configuredGroupNames.Count; i++)
        {
            paths.Add(GetRemoteGameDataPath(configuredGroupNames[i]));
        }

#if BIZZA_REAL_WITHDRAW
        var countries = GetTestCountries();
        for (var countryIndex = 0; countryIndex < countries.Count; countryIndex++)
        {
            var country = countries[countryIndex];
            if (country == AccountModule.E_CountryType.None)
            {
                continue;
            }

            for (var groupIndex = 0; groupIndex < configuredGroupNames.Count; groupIndex++)
            {
                paths.Add(GetCountryGameDataPath(country, configuredGroupNames[groupIndex]));
            }
        }
#endif

        return paths;
    }

    private List<string> GetAllStrategyPaths()
    {
        var paths = new List<string> { GetTestStrategyPath() };

#if BIZZA_REAL_WITHDRAW
        var countries = GetTestCountries();
        for (var i = 0; i < countries.Count; i++)
        {
            if (countries[i] != AccountModule.E_CountryType.None)
            {
                var path = GetCountryStrategyPath(countries[i]);
                if (File.Exists(path)) paths.Add(path);
            }
        }
#endif

        return paths;
    }

#if BIZZA_REAL_WITHDRAW
    private List<string> GetCountryStrategyPaths()
    {
        var paths = new List<string>();
        var countries = GetTestCountries();
        for (var i = 0; i < countries.Count; i++)
        {
            if (countries[i] != AccountModule.E_CountryType.None)
            {
                paths.Add(GetCountryTestStrategyPath(countries[i]));
            }
        }

        return paths;
    }
#endif

    private bool TryLoadConfiguredStrategies(
        out Dictionary<string, RemoteGroupStrategy> configuredStrategies,
        out string error,
        bool requireCountryStrategies = true)
    {
        configuredStrategies = new Dictionary<string, RemoteGroupStrategy>(StringComparer.OrdinalIgnoreCase);
        error = string.Empty;

        var strategyPaths = GetAllStrategyPaths();
        for (var i = 0; i < strategyPaths.Count; i++)
        {
            var path = strategyPaths[i];
            if (!File.Exists(path))
            {
                if (i > 0 && !requireCountryStrategies)
                {
                    continue;
                }

                error = $"没有找到已导出的真实分组策略：{path}。请先在 Remote Group Config 中按当前权重导出策略，不要在测试窗口生成或替换策略。";
                return false;
            }

            try
            {
                configuredStrategies[path] = LoadStrategyFile(path);
            }
            catch (Exception exception)
            {
                error = $"真实分组策略读取失败：{path}\n{exception.Message}";
                return false;
            }
        }

        return configuredStrategies.Count > 0;
    }

    private static RemoteGroupStrategy LoadStrategyFile(string path)
    {
        var encryptedText = RemoteTextUtility.NormalizeText(File.ReadAllText(path));
        if (!RemoteGroupConfigCrypto.TryDecryptToText(encryptedText, out var base64Binary))
        {
            throw new InvalidDataException("分组策略解密失败。");
        }

        byte[] strategyBytes;
        try
        {
            strategyBytes = Convert.FromBase64String(base64Binary.Trim());
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("分组策略不是有效的二进制内容。", exception);
        }

        return RemoteGroupBinaryUtility.FromStrategyBytes(strategyBytes);
    }

    private List<string> GetConfiguredRemoteGroupNames(Dictionary<string, RemoteGroupStrategy> configuredStrategies)
    {
        var groupNames = new List<string>();
        foreach (var pair in configuredStrategies)
        {
            var strategy = pair.Value;
            if (strategy?.Datas == null)
            {
                continue;
            }

            for (var i = 0; i < strategy.Datas.Count; i++)
            {
                var groupData = strategy.Datas[i];
                if (groupData == null
                    || string.IsNullOrWhiteSpace(groupData.UserGroupName)
                    || string.Equals(groupData.UserGroupName, runtimeConfig.UserGroupDefaultName, StringComparison.OrdinalIgnoreCase)
                    || groupNames.Contains(groupData.UserGroupName))
                {
                    continue;
                }

                groupNames.Add(groupData.UserGroupName);
            }
        }

        return groupNames;
    }

    private List<string> GetConfiguredRemoteGroupNamesFromFiles()
    {
        if (!TryLoadConfiguredStrategies(out var configuredStrategies, out _, false))
        {
            return new List<string>();
        }

        return GetConfiguredRemoteGroupNames(configuredStrategies);
    }

    private List<string> GetFailureGroupNames()
    {
        var result = new List<string>();
        if (TryLoadConfiguredStrategies(out var configuredStrategies, out _, false))
        {
            result.Add(runtimeConfig.UserGroupDefaultName);
            result.AddRange(GetConfiguredRemoteGroupNames(configuredStrategies));
        }

        if (result.Count == 0)
        {
            result.Add(runtimeConfig.UserGroupDefaultName);
        }

        return result;
    }

    private string GetStrategySourceDescription()
    {
        var strategyPaths = GetAllStrategyPaths();
        if (!File.Exists(GetTestStrategyPath()))
        {
            return "公共真实策略未导出";
        }

        var existingCount = 1;
        for (var i = 1; i < strategyPaths.Count; i++)
        {
            if (File.Exists(strategyPaths[i]))
            {
                existingCount++;
            }
        }

        return $"公共真实策略已导出，国家策略测试副本 {existingCount - 1}/{strategyPaths.Count - 1}；初始化时按真实权重正常随机";
    }

    private string GetConfiguredStrategyGroupDescription()
    {
        if (!TryLoadConfiguredStrategies(out var configuredStrategies, out _, false))
        {
            return "尚未读取到公共真实策略";
        }

        var groupNames = new List<string>();
        foreach (var pair in configuredStrategies)
        {
            var strategy = pair.Value;
            if (strategy?.Datas == null)
            {
                continue;
            }

            for (var i = 0; i < strategy.Datas.Count; i++)
            {
                var groupData = strategy.Datas[i];
                if (groupData == null
                    || string.IsNullOrWhiteSpace(groupData.UserGroupName)
                    || groupNames.Contains(groupData.UserGroupName))
                {
                    continue;
                }

                groupNames.Add(groupData.UserGroupName);
            }
        }

        return groupNames.Count == 0
            ? "策略为空或没有有效分组"
            : string.Join("、", groupNames.ToArray());
    }

    private void DrawUnreferencedGameDataWarning()
    {
        if (!TryLoadConfiguredStrategies(out var configuredStrategies, out _, false))
        {
            return;
        }

        var referencedGroupNames = GetConfiguredRemoteGroupNames(configuredStrategies);
        referencedGroupNames.Add(runtimeConfig.UserGroupDefaultName);

        var directory = Path.Combine(GetCommonOutputRootPath(), runtimeConfig.GameDataDirectoryName);
        if (!Directory.Exists(directory))
        {
            return;
        }

        var unreferencedNames = new List<string>();
        var files = Directory.GetFiles(directory, "*.bytes", SearchOption.TopDirectoryOnly);
        for (var i = 0; i < files.Length; i++)
        {
            var groupName = Path.GetFileNameWithoutExtension(files[i]);
            if (!referencedGroupNames.Contains(groupName) && !unreferencedNames.Contains(groupName))
            {
                unreferencedNames.Add(groupName);
            }
        }

        if (unreferencedNames.Count > 0)
        {
            EditorGUILayout.HelpBox(
                $"检测到公共关卡文件未被真实策略引用：{string.Join("、", unreferencedNames.ToArray())}。这些文件不会被随机选中；如果需要测试，请把对应分组加入 Remote Group Config 并重新导出策略。",
                MessageType.Warning);
        }
    }

    private void ClearTestState()
    {
        RemoteGroupEditorOverride.Enabled = false;
        RemoteGroupEditorOverride.ClearSavedGroupMarker();
        RemoteGroupDataSystem.current.ClearCachedRemoteData();
        lastResult = "已清理正式分组记录和沙盒缓存，并关闭 Editor User Group Override。";
        Repaint();
    }

    private async void RunInitialization(bool clearBeforeRun = false)
    {
        if (isRunning)
        {
            return;
        }

        if (clearBeforeRun && RemoteGroupEditorOverride.Enabled)
        {
            RemoteGroupEditorOverride.Enabled = false;
            RemoteGroupEditorOverride.ClearSavedGroupMarker();
        }

        if (RemoteGroupEditorOverride.Enabled)
        {
            EditorUtility.DisplayDialog("Remote Group Test", "请先关闭 Editor User Group Override，再执行真实远端流程测试。", "确定");
            return;
        }

        if (clearBeforeRun)
        {
            RemoteGroupDataSystem.current.ClearCachedRemoteData();
        }

        isRunning = true;
        lastResult = clearBeforeRun
            ? "已清理正式分组记录和沙盒缓存，正在执行 RemoteGroupDataSystem.InitGameData，请查看 Console 中的 LogGameConfigLoad 中文日志。"
            : "正在执行 RemoteGroupDataSystem.InitGameData，请查看 Console 中的 LogGameConfigLoad 中文日志。";
        Repaint();

        try
        {
            await RemoteGroupDataSystem.current.InitGameData();
            lastResult = $"测试执行成功：分组={RemoteGroupDataSystem.current.GetUserGroupName()}，数据来源={RemoteGroupDataSystem.current.GetCurrentDataSource()}，正式完成={RemoteGroupDataSystem.current.HasCompletedAssignment()}。";
        }
        catch (Exception exception)
        {
            lastResult = $"测试执行失败：{exception.Message}";
        }
        finally
        {
            isRunning = false;
            Repaint();
        }
    }

    private string GetPreviewGroupName()
    {
        var configuredGroupNames = GetConfiguredRemoteGroupNamesFromFiles();
        return configuredGroupNames.Count > 0 ? configuredGroupNames[0] : "group1";
    }

    private async void ProbeCurrentRemoteResources()
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        lastResult = "正在检查当前 CountryType 对应的远端策略和关卡资源...";
        Repaint();

        try
        {
            var urls = new List<string>
            {
                runtimeConfig.GroupStrategyUrl
            };

            var groupNames = GetConfiguredRemoteGroupNamesFromFiles();
            if (groupNames.Count == 0)
            {
                groupNames.Add("group1");
            }

            for (var i = 0; i < groupNames.Count; i++)
            {
                urls.Add(runtimeConfig.BuildGameDataUrl(groupNames[i]));
            }

            var successCount = 0;
            var resultLines = new List<string>();
            for (var i = 0; i < urls.Count; i++)
            {
                var result = await ProbeRemoteUrl(urls[i]);
                if (result.Success)
                {
                    successCount++;
                }

                resultLines.Add(result.Message);
            }

            lastResult = $"远端资源检查完成：{successCount}/{urls.Count} 成功。\n{string.Join("\n", resultLines.ToArray())}";
        }
        catch (Exception exception)
        {
            lastResult = $"远端资源检查失败：{exception.Message}";
        }
        finally
        {
            isRunning = false;
            Repaint();
        }
    }

    private async void RunRemoteScenarioMatrixTest()
    {
        if (isRunning || !runtimeConfigLoaded)
        {
            return;
        }

        var groupNames = GetConfiguredRemoteGroupNamesFromFiles();
        if (groupNames.Count == 0)
        {
            groupNames.Add("group1");
        }

        if (!EditorUtility.DisplayDialog(
                "全量远端预检",
                "将依次检查公共模式、仅国家分组、仅国家关卡、国家分组和国家关卡同时开启四种地址组合；这一步只发起检查请求，不修改运行时配置。是否继续？",
                "开始",
                "取消"))
        {
            return;
        }

        isRunning = true;
        remoteScenarioResults.Clear();
        lastResult = "正在执行四种国家开关组合的远端预检...";
        Repaint();

        var originalCountry = GetCurrentCountryForTest();
        try
        {
            var modeNames = new[]
            {
                "公共模式（Group=false，Level=false）",
                "仅国家分组（Group=true，Level=false）",
                "仅国家关卡（Group=false，Level=true）",
                "国家分组+国家关卡（Group=true，Level=true）"
            };
            var groupFlags = new[] { false, true, false, true };
            var levelFlags = new[] { false, false, true, true };

            for (var modeIndex = 0; modeIndex < modeNames.Length; modeIndex++)
            {
                SetCountryForScenario(modeIndex == 0 ? GetNoneCountryName() : GetConcreteTestCountryName());
                var scenarioConfig = runtimeConfig.CloneSanitized();
                scenarioConfig.CountryGroupEnabled = groupFlags[modeIndex];
                scenarioConfig.CountryLevelEnabled = levelFlags[modeIndex];

                var passedCount = 0;
                var totalCount = 1;
                var scenarioGroupNames = new List<string>(groupNames);
                var detailLines = new List<string>();
                var strategyResult = await ProbeRemoteUrl(scenarioConfig.GroupStrategyUrl);
                RemoteGroupStrategy strategy = null;
                var strategyError = string.Empty;
                if (strategyResult.Success && TryParseStrategyPayload(strategyResult.Data, out strategy, out strategyError))
                {
                    passedCount++;
                    scenarioGroupNames = GetNonDefaultGroupNames(strategy);
                    totalCount += scenarioGroupNames.Count;
                    detailLines.Add($"策略通过：{scenarioConfig.GroupStrategyUrl}，分组数={strategy.Datas?.Count ?? 0}");
                }
                else
                {
                    totalCount += scenarioGroupNames.Count;
                    detailLines.Add($"策略失败：{scenarioConfig.GroupStrategyUrl}，{strategyResult.Message}{(string.IsNullOrEmpty(strategyError) ? string.Empty : "；解析=" + strategyError)}");
                }

                for (var groupIndex = 0; groupIndex < scenarioGroupNames.Count; groupIndex++)
                {
                    var groupName = scenarioGroupNames[groupIndex];
                    var gameDataResult = await ProbeRemoteUrl(scenarioConfig.BuildGameDataUrl(groupName));
                    if (gameDataResult.Success && gameDataResult.Data != null && gameDataResult.Data.Length > 0)
                    {
                        passedCount++;
                    }
                    else
                    {
                        detailLines.Add($"关卡失败：{scenarioConfig.BuildGameDataUrl(groupName)}，{gameDataResult.Message}");
                    }
                }

                remoteScenarioResults.Add(new RemoteScenarioResult
                {
                    Name = modeNames[modeIndex],
                    Success = passedCount == totalCount,
                    PassedCount = passedCount,
                    TotalCount = totalCount,
                    Detail = detailLines.Count == 0 ? "策略和全部非 Default 关卡文件均返回有效内容。" : string.Join("\n", detailLines.ToArray())
                });
                Repaint();
            }

#if BIZZA_REAL_WITHDRAW
            SetCountryForScenario(GetNoneCountryName());
            var localDefaultBytes = File.Exists(GetLocalDefaultPath()) ? File.ReadAllBytes(GetLocalDefaultPath()) : null;
            remoteScenarioResults.Add(new RemoteScenarioResult
            {
                Name = "国家配置 + CountryType.None（本地 Default 预期）",
                Success = localDefaultBytes != null && localDefaultBytes.Length > 0,
                PassedCount = localDefaultBytes != null && localDefaultBytes.Length > 0 ? 1 : 0,
                TotalCount = 1,
                Detail = localDefaultBytes != null && localDefaultBytes.Length > 0
                    ? "本地 Default 可读；运行时不应请求远端，也不应写入正式分组完成记录。"
                    : "本地 Default 缺失或为空，无法验证国家 None 回退。"
            });
#endif

            var commercialSuccess = true;
            var commercialStatus = "未执行";
            if (probeCommercialResources)
            {
                var commercialResult = await ProbeCommercialResourcesInternal();
                AddAutomatedResult(commercialResult.Name, commercialResult.Success, commercialResult.Detail);
                commercialSuccess = commercialResult.Success;
                commercialStatus = commercialSuccess ? "通过" : "未通过";
            }

            var failedCount = 0;
            for (var i = 0; i < remoteScenarioResults.Count; i++)
            {
                if (!remoteScenarioResults[i].Success)
                {
                    failedCount++;
                }
            }

            lastResult = failedCount == 0 && commercialSuccess
                ? $"四种国家开关组合远端预检通过；商业化预检={commercialStatus}。"
                : $"远端预检完成：{failedCount}/{remoteScenarioResults.Count} 个场景未通过；商业化预检={commercialStatus}，请展开结果查看具体 URL。";
        }
        catch (Exception exception)
        {
            lastResult = $"远端预检失败：{exception.Message}";
        }
        finally
        {
            SetCountryForScenario(originalCountry);
            isRunning = false;
            Repaint();
        }
    }

    private async void RunFirstRequestAndReuseTest()
    {
        if (isRunning || !runtimeConfigLoaded)
        {
            return;
        }

        if (RemoteGroupEditorOverride.Enabled)
        {
            EditorUtility.DisplayDialog("Remote Group Test", "请先关闭 Editor User Group Override，再执行首次请求和正式记录复用测试。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "首请求 + 正式记录复用",
                "将清理当前正式分组记录和沙盒缓存，第一次执行真实初始化，第二次执行验证 PlayerPrefs 正式记录和沙盒缓存是否复用。是否继续？",
                "开始",
                "取消"))
        {
            return;
        }

        isRunning = true;
        lastResult = "正在执行首次请求和正式记录复用测试...";
        Repaint();

        try
        {
            RemoteGroupDataSystem.current.ClearCachedRemoteData();
            await RemoteGroupDataSystem.current.InitGameData();
            var first = CaptureRuntimeObservation();
            var firstExpectedFormal = !runtimeConfig.CountryGroupEnabled && !runtimeConfig.CountryLevelEnabled
                                      || !string.Equals(GetCurrentCountryForTest(), GetNoneCountryName(), StringComparison.Ordinal);
            var firstValid = IsObservationUsable(first)
                             && first.AssignmentCompleted == firstExpectedFormal;

            await RemoteGroupDataSystem.current.InitGameData();
            var second = CaptureRuntimeObservation();
            var secondValid = IsObservationUsable(second)
                              && second.GroupName == first.GroupName
                              && second.DataSource == first.DataSource
                              && second.DataPath == first.DataPath
                              && AreSameBytes(first.Data, second.Data)
                              && second.AssignmentCompleted == firstExpectedFormal;

            var success = firstValid && secondValid;
            var detail =
                $"第一次：{FormatObservation(first)}\n" +
                $"第二次：{FormatObservation(second)}\n" +
                $"结果：{(success ? "分组、数据来源、路径和内容均一致" : "两次结果不一致或正式记录状态不符合预期")}。" +
                "\n请同时在 Console 搜索 LogGameConfigLoad，确认第二次出现“已完成的分组记录”或“已记录的 Default”日志。";
            AddAutomatedResult("当前配置首请求 + 正式记录复用", success, detail);
            lastResult = success ? "首请求和正式记录复用测试通过。" : "首请求和正式记录复用测试未通过，请查看自动化结果和 Console 日志。";
        }
        catch (Exception exception)
        {
            AddAutomatedResult("当前配置首请求 + 正式记录复用", false, exception.Message);
            lastResult = $"首请求和正式记录复用测试失败：{exception.Message}";
        }
        finally
        {
            isRunning = false;
            Repaint();
        }
    }

    private async void RunRandomCoverageTest()
    {
        if (isRunning || !runtimeConfigLoaded)
        {
            return;
        }

        if (RemoteGroupEditorOverride.Enabled)
        {
            EditorUtility.DisplayDialog("Remote Group Test", "请先关闭 Editor User Group Override，再执行随机覆盖测试。", "确定");
            return;
        }

        if (runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled)
        {
            if (string.Equals(GetCurrentCountryForTest(), GetNoneCountryName(), StringComparison.Ordinal))
            {
                AddAutomatedResult(
                    "当前策略随机覆盖测试",
                    false,
                    "当前国家为 None 且已开启国家配置，运行时按代码直接使用本地 Default，不会随机请求远端策略；请先通过真实用户数据让 CountryType 变为具体国家。");
                lastResult = "随机覆盖测试未执行：当前国家为 None 的国家配置分支不包含随机流程。";
                Repaint();
                return;
            }
        }

        if (!EditorUtility.DisplayDialog(
                "随机覆盖测试",
                $"将清理缓存并连续执行 {randomSampleCount} 次真实初始化，用于统计策略随机结果。测试结束后会保留最后一次运行状态。是否继续？",
                "开始",
                "取消"))
        {
            return;
        }

        isRunning = true;
        lastResult = $"正在执行 {randomSampleCount} 次随机覆盖测试...";
        Repaint();

        try
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var failureLines = new List<string>();
            for (var i = 0; i < randomSampleCount; i++)
            {
                RemoteGroupDataSystem.current.ClearCachedRemoteData();
                try
                {
                    await RemoteGroupDataSystem.current.InitGameData();
                    var groupName = RemoteGroupDataSystem.current.GetUserGroupName();
                    counts.TryGetValue(groupName, out var count);
                    counts[groupName] = count + 1;
                }
                catch (Exception exception)
                {
                    failureLines.Add($"第 {i + 1} 次：{exception.Message}");
                }
            }

            var expectedGroups = GetConfiguredRemoteGroupNamesFromFiles();
            expectedGroups.Add(runtimeConfig.UserGroupDefaultName);
            var missingGroups = new List<string>();
            for (var i = 0; i < expectedGroups.Count; i++)
            {
                if (!counts.ContainsKey(expectedGroups[i]))
                {
                    missingGroups.Add(expectedGroups[i]);
                }
            }

            var countLines = new List<string>();
            foreach (var pair in counts)
            {
                countLines.Add($"{pair.Key}={pair.Value}");
            }

            var success = failureLines.Count == 0 && missingGroups.Count == 0;
            var detail =
                $"结果分布：{(countLines.Count == 0 ? "无" : string.Join("，", countLines.ToArray()))}\n" +
                $"未覆盖分组：{(missingGroups.Count == 0 ? "无" : string.Join("、", missingGroups.ToArray()))}";
            if (failureLines.Count > 0)
            {
                detail += $"\n失败次数={failureLines.Count}：{string.Join("；", failureLines.ToArray())}";
            }

            if (missingGroups.Count > 0 && failureLines.Count == 0)
            {
                detail += "\n请求本身没有失败，但样本次数不足以覆盖全部权重分组；可提高随机覆盖次数后重试。";
            }

            AddAutomatedResult($"当前策略随机覆盖测试（{randomSampleCount} 次）", success, detail);
            lastResult = success ? "随机覆盖测试通过，所有策略分组均被抽到。" : "随机覆盖测试完成，但存在未覆盖分组或请求失败。";
        }
        catch (Exception exception)
        {
            AddAutomatedResult("当前策略随机覆盖测试", false, exception.Message);
            lastResult = $"随机覆盖测试失败：{exception.Message}";
        }
        finally
        {
            isRunning = false;
            Repaint();
        }
    }

    private void CheckLocalDefaultFallback()
    {
        try
        {
            var path = GetLocalDefaultPath();
            var bytes = File.Exists(path) ? File.ReadAllBytes(path) : null;
            var success = bytes != null && bytes.Length > 0;
            AddAutomatedResult(
                "本地 Default 回退条件",
                success,
                success
                    ? $"本地 Default 可读：{path}，字节数={bytes.Length}。远端失败时本次会话可回退到此文件；代码不会写入正式分组完成记录。"
                    : $"本地 Default 缺失或为空：{path}。远端失败时无法完成回退。\n注意：当前按钮不会主动制造真实远端失败。",
                true);
            lastResult = success ? "本地 Default 回退条件检查通过。" : "本地 Default 回退条件检查未通过。";
        }
        catch (Exception exception)
        {
            AddAutomatedResult("本地 Default 回退条件", false, exception.Message);
            lastResult = $"本地 Default 回退条件检查失败：{exception.Message}";
        }

        Repaint();
    }

    private void AddAutomatedResult(string name, bool success, string detail, bool clearPrevious = true)
    {
        if (clearPrevious)
        {
            for (var i = automatedTestResults.Count - 1; i >= 0; i--)
            {
                if (automatedTestResults[i].Name == name)
                {
                    automatedTestResults.RemoveAt(i);
                }
            }
        }

        automatedTestResults.Add(new AutomatedTestResult
        {
            Name = name,
            Success = success,
            Detail = detail
        });
    }

    private RuntimeObservation CaptureRuntimeObservation()
    {
        var dataSystem = RemoteGroupDataSystem.current;
        dataSystem.TryGetCurrentGameDataBytes(out var data);
        return new RuntimeObservation
        {
            GroupName = dataSystem.GetUserGroupName(),
            DataSource = dataSystem.GetCurrentDataSource(),
            AssignmentCompleted = dataSystem.HasCompletedAssignment(),
            DataPath = dataSystem.GetCurrentGameDataPath(),
            Data = data
        };
    }

    private static bool IsObservationUsable(RuntimeObservation observation)
    {
        return !string.IsNullOrWhiteSpace(observation.GroupName)
               && observation.DataSource != RemoteGroupDataSource.None
               && observation.Data != null
               && observation.Data.Length > 0;
    }

    private static bool AreSameBytes(byte[] first, byte[] second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        if (first == null || second == null || first.Length != second.Length)
        {
            return false;
        }

        for (var i = 0; i < first.Length; i++)
        {
            if (first[i] != second[i])
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatObservation(RuntimeObservation observation)
    {
        return $"分组={observation.GroupName}，来源={observation.DataSource}，正式完成={observation.AssignmentCompleted}，字节数={observation.Data?.Length ?? 0}，指纹={GetDataFingerprint(observation.Data)}，路径={observation.DataPath}";
    }

    private static string GetDataFingerprint(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return "<empty>";
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return BitConverter.ToString(hash, 0, Math.Min(6, hash.Length)).Replace("-", string.Empty);
    }

    private async Task<RemoteProbeResult> ProbeRemoteUrl(string url)
    {
        using var request = UnityWebRequest.Get(url);
        request.timeout = Mathf.Max(1, Mathf.CeilToInt(runtimeConfig.Timeout));
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Yield();
        }

        var isLocalUrl = url.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                         || url.StartsWith("jar:", StringComparison.OrdinalIgnoreCase);
        var success = request.result == UnityWebRequest.Result.Success
                      && (isLocalUrl || (request.responseCode >= 200 && request.responseCode < 300));
        var message = success
            ? $"成功：{url}，HTTP={request.responseCode}，字节数={request.downloadHandler?.data?.Length ?? 0}"
            : $"失败：{url}，结果={request.result}，HTTP={request.responseCode}，错误={request.error}";
        return new RemoteProbeResult(success, message, request.downloadHandler?.data);
    }

    private static bool TryParseStrategyPayload(byte[] payload, out RemoteGroupStrategy strategy, out string error)
    {
        strategy = null;
        error = string.Empty;
        if (payload == null || payload.Length == 0)
        {
            error = "响应为空。";
            return false;
        }

        try
        {
            var encryptedText = RemoteTextUtility.NormalizeText(Encoding.UTF8.GetString(payload));
            if (!RemoteGroupConfigCrypto.TryDecryptToText(encryptedText, out var base64Binary))
            {
                error = "分组策略解密失败。";
                return false;
            }

            var strategyBytes = Convert.FromBase64String(base64Binary.Trim());
            strategy = RemoteGroupBinaryUtility.FromStrategyBytes(strategyBytes);
            if (strategy == null || strategy.Datas == null || strategy.Datas.Count == 0)
            {
                error = "策略没有有效分组。";
                strategy = null;
                return false;
            }

            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            strategy = null;
            return false;
        }
    }

    private List<string> GetNonDefaultGroupNames(RemoteGroupStrategy strategy)
    {
        var groupNames = new List<string>();
        if (strategy?.Datas == null)
        {
            return groupNames;
        }

        for (var i = 0; i < strategy.Datas.Count; i++)
        {
            var groupData = strategy.Datas[i];
            if (groupData == null
                || string.IsNullOrWhiteSpace(groupData.UserGroupName)
                || string.Equals(groupData.UserGroupName, runtimeConfig.UserGroupDefaultName, StringComparison.OrdinalIgnoreCase)
                || groupNames.Contains(groupData.UserGroupName))
            {
                continue;
            }

            groupNames.Add(groupData.UserGroupName);
        }

        return groupNames;
    }

    private async Task<AutomatedTestResult> ProbeCommercialResourcesInternal()
    {
#if BIZZA_REAL_WITHDRAW
        var guids = AssetDatabase.FindAssets("t:ChannelABConfig");
        if (guids.Length == 0)
        {
            return new AutomatedTestResult
            {
                Name = "商业化远端文件预检",
                Success = false,
                Detail = "项目中没有找到 ChannelABConfig 资源。"
            };
        }

        var channelConfig = AssetDatabase.LoadAssetAtPath<ChannelABConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        if (channelConfig == null || channelConfig.gameAB_CustomDatas == null || channelConfig.gameAB_CustomDatas.Count == 0)
        {
            return new AutomatedTestResult
            {
                Name = "商业化远端文件预检",
                Success = false,
                Detail = "ChannelABConfig 为空或没有国家配置。"
            };
        }

        var prefix = Path.GetFileNameWithoutExtension(channelConfig.groupConfigName?.Trim());
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return new AutomatedTestResult
            {
                Name = "商业化远端文件预检",
                Success = false,
                Detail = "ChannelABConfig.groupConfigName 为空。"
            };
        }

        var passedCount = 0;
        var totalCount = 0;
        var failedLines = new List<string>();
        for (var countryIndex = 0; countryIndex < channelConfig.gameAB_CustomDatas.Count; countryIndex++)
        {
            var countryData = channelConfig.gameAB_CustomDatas[countryIndex];
            if (countryData == null || countryData.gameAB_Datas == null)
            {
                failedLines.Add($"第 {countryIndex + 1} 个国家配置为空。\n");
                continue;
            }

            var countryDirectory = countryData.e_CountryType == AccountModule.E_CountryType.None
                ? string.Empty
                : countryData.e_CountryType + "/";
            for (var groupIndex = 0; groupIndex < countryData.gameAB_Datas.Count; groupIndex++)
            {
                var fileIndex = groupIndex + (channelConfig.StartGroupIndexAtOne ? 1 : 0);
                var relativePath = $"{countryDirectory}commercial/{prefix}{fileIndex}.bytes";
                var url = BuildRemoteUrl(relativePath);
                totalCount++;
                var result = await ProbeRemoteUrl(url);
                if (result.Success && IsCommercialPayloadValid(result.Data, fileIndex))
                {
                    passedCount++;
                }
                else
                {
                    failedLines.Add($"{countryData.e_CountryType}/{prefix}{fileIndex}.bytes：{result.Message}，内容格式不是有效 CHAB。\n");
                }
            }
        }

        return new AutomatedTestResult
        {
            Name = "商业化远端文件预检",
            Success = totalCount > 0 && passedCount == totalCount,
            Detail = $"ChannelABConfig={channelConfig.name}，检查 {passedCount}/{totalCount} 个文件。\n" +
                     (failedLines.Count == 0
                         ? "文件均可访问且 CHAB 头、版本和分组序号有效。注意：当前 InitRemoteGroupDataTask 不负责运行时加载商业化文件。"
                         : string.Join("", failedLines.ToArray()))
        };
#else
        return new AutomatedTestResult
        {
            Name = "商业化远端文件预检",
            Success = true,
            Detail = "当前编译未启用 BIZZA_REAL_WITHDRAW，跳过 ChannelABConfig 商业化文件检查。"
        };
#endif
    }

#if BIZZA_REAL_WITHDRAW
    private static bool IsCommercialPayloadValid(byte[] payload, int expectedGroupIndex)
    {
        if (payload == null || payload.Length < 12)
        {
            return false;
        }

        return payload[0] == (byte)'C'
               && payload[1] == (byte)'H'
               && payload[2] == (byte)'A'
               && payload[3] == (byte)'B'
               && BitConverter.ToInt32(payload, 4) == 1
               && BitConverter.ToInt32(payload, 8) == expectedGroupIndex;
    }
#endif

    private string BuildRemoteUrl(string relativePath)
    {
        var root = runtimeConfig.RemoteRootUrl?.TrimEnd('/') ?? string.Empty;
        return root + "/" + (relativePath ?? string.Empty).TrimStart('/');
    }

    private string GetPersistentStrategyPath()
    {
        return Path.Combine(
            Application.persistentDataPath,
            RemoteGroupRuntimeConfig.PersistentDataDirectoryName,
            RemoteGroupRuntimeConfig.PersistentStrategyFileName).Replace("\\", "/");
    }

    private string GetPersistentGameDataDirectory()
    {
        return Path.Combine(
            Application.persistentDataPath,
            RemoteGroupRuntimeConfig.PersistentDataDirectoryName,
            RemoteGroupRuntimeConfig.PersistentGameDataDirectoryName).Replace("\\", "/");
    }

    private static string GetNoneCountryName()
    {
#if BIZZA_REAL_WITHDRAW
        return AccountModule.E_CountryType.None.ToString();
#else
        return "None";
#endif
    }

    private static string GetCurrentCountryForTest()
    {
#if BIZZA_REAL_WITHDRAW
        return AccountModule.CountryType.ToString();
#else
        return "None";
#endif
    }

    private static string GetConcreteTestCountryName()
    {
#if BIZZA_REAL_WITHDRAW
        var values = Enum.GetValues(typeof(AccountModule.E_CountryType));
        for (var i = 0; i < values.Length; i++)
        {
            var country = (AccountModule.E_CountryType)values.GetValue(i);
            if (country != AccountModule.E_CountryType.None)
            {
                return country.ToString();
            }
        }

        return AccountModule.E_CountryType.None.ToString();
#else
        return "None";
#endif
    }

    private static void SetCountryForScenario(string countryName)
    {
#if BIZZA_REAL_WITHDRAW
        if (Enum.TryParse(countryName, out AccountModule.E_CountryType country))
        {
            AccountModule.CountryType = country;
        }
#endif
    }

    private readonly struct RemoteProbeResult
    {
        public readonly bool Success;
        public readonly string Message;
        public readonly byte[] Data;

        public RemoteProbeResult(bool success, string message, byte[] data)
        {
            Success = success;
            Message = message;
            Data = data;
        }
    }

#if BIZZA_REAL_WITHDRAW
    private async void RunAllCountryTests()
    {
        if (isRunning)
        {
            return;
        }

        if (!runtimeConfigLoaded)
        {
            EditorUtility.DisplayDialog("Remote Group Test", "请先读取 Remote Group Config 运行时配置。", "确定");
            return;
        }

        if (RemoteGroupEditorOverride.Enabled)
        {
            EditorUtility.DisplayDialog("Remote Group Test", "请先关闭 Editor User Group Override，再进行批量国家测试。", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "批量测试全部国家",
                "将依次测试 None 和当前项目 E_CountryType 中的所有具体国家。每个国家都会清理一次分组记录后直接请求 RemoteRootUrl 对应的远端资源。是否继续？",
                "开始",
                "取消"))
        {
            return;
        }

        var originalCountry = AccountModule.CountryType;
        countryTestResults.Clear();
        isRunning = true;
        lastResult = "正在批量测试全部国家，请查看 Console 中每个国家对应的 LogGameConfigLoad 日志。";
        Repaint();

        try
        {
            var countries = GetTestCountries();
            for (var i = 0; i < countries.Count; i++)
            {
                var country = countries[i];
                AccountModule.CountryType = country;
                RemoteGroupEditorOverride.Enabled = false;
                RemoteGroupEditorOverride.ClearSavedGroupMarker();
                RemoteGroupDataSystem.current.ClearCachedRemoteData();

                try
                {
                    await RemoteGroupDataSystem.current.InitGameData();
                    countryTestResults.Add(new CountryTestResult
                    {
                        Country = country,
                        Success = true,
                        GroupName = RemoteGroupDataSystem.current.GetUserGroupName(),
                        DataSource = RemoteGroupDataSystem.current.GetCurrentDataSource(),
                        AssignmentCompleted = RemoteGroupDataSystem.current.HasCompletedAssignment(),
                        Error = string.Empty
                    });
                }
                catch (Exception exception)
                {
                    countryTestResults.Add(new CountryTestResult
                    {
                        Country = country,
                        Success = false,
                        GroupName = RemoteGroupDataSystem.current.GetUserGroupName(),
                        DataSource = RemoteGroupDataSystem.current.GetCurrentDataSource(),
                        AssignmentCompleted = RemoteGroupDataSystem.current.HasCompletedAssignment(),
                        Error = exception.Message
                    });
                }

                Repaint();
            }

            lastResult = $"全部国家测试完成，共测试 {countryTestResults.Count} 个国家配置。";
        }
        finally
        {
            AccountModule.CountryType = originalCountry;
            isRunning = false;
            Repaint();
        }
    }

    private static List<AccountModule.E_CountryType> GetTestCountries()
    {
        var countries = new List<AccountModule.E_CountryType>();
        var values = Enum.GetValues(typeof(AccountModule.E_CountryType));
        for (var i = 0; i < values.Length; i++)
        {
            countries.Add((AccountModule.E_CountryType)values.GetValue(i));
        }

        return countries;
    }
#endif

    private void DeleteSelectedRemoteGameData()
    {
#if BIZZA_REAL_WITHDRAW
        if ((runtimeConfig.CountryGroupEnabled || runtimeConfig.CountryLevelEnabled)
            && AccountModule.CountryType == AccountModule.E_CountryType.None)
        {
            lastResult = "当前国家为 None 且已开启国家相关配置，运行时最终会使用本地 Default；无需模拟远端下载失败。";
            Repaint();
            return;
        }
#endif

        var groupName = failureGroupName;
        if (string.Equals(groupName, runtimeConfig.UserGroupDefaultName, StringComparison.OrdinalIgnoreCase))
        {
            var remoteGroupNames = GetConfiguredRemoteGroupNamesFromFiles();
            if (remoteGroupNames.Count == 0)
            {
                lastResult = "当前真实分组策略中没有非 Default 分组，Default 不会下载远端关卡，无可模拟的远端关卡失败。";
                Repaint();
                return;
            }

            groupName = remoteGroupNames[0];
        }

        var path = GetRemoteGameDataPath(groupName);
#if BIZZA_REAL_WITHDRAW
        if (runtimeConfig.CountryLevelEnabled && AccountModule.CountryType != AccountModule.E_CountryType.None)
        {
            path = GetCountryGameDataPath(AccountModule.CountryType, groupName);
        }
#endif
        if (!File.Exists(path))
        {
            lastResult = $"测试关卡文件不存在，无需删除：{path}";
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "删除本地测试文件",
                $"将删除测试目录中的文件：\n{path}\n这只会影响本地 RemoteUpload 测试资源，不会删除或修改真正的远端文件；使用当前 RemoteRootUrl 时不能借此制造真实远端 HTTP 失败。是否继续？",
                "删除本地文件",
                "取消"))
        {
            return;
        }

        File.Delete(path);
        lastResult = $"已删除本地测试关卡文件：{path}。这不会影响 RemoteRootUrl 指向的远端资源。";
        Repaint();
    }

    private bool HasGeneratedTestResources()
    {
        if (!runtimeConfigLoaded
            || !TryLoadConfiguredStrategies(out var configuredStrategies, out _))
        {
            return false;
        }

        var paths = new List<string>();
        paths.AddRange(GetAllStrategyPaths());
        paths.Add(GetTestOutputStrategyPath());
#if BIZZA_REAL_WITHDRAW
        paths.AddRange(GetCountryStrategyPaths());
#endif
        paths.AddRange(GetAllTestGameDataPaths(configuredStrategies));
        for (var i = 0; i < paths.Count; i++)
        {
            if (!File.Exists(paths[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasAnyExistingFile(List<string> paths)
    {
        for (var i = 0; i < paths.Count; i++)
        {
            if (File.Exists(paths[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] CreateTestGameData(string groupName)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            var header = new byte[TestPackageHeaderSize];
            Array.Copy(Encoding.ASCII.GetBytes("RGPK"), header, 4);
            writer.Write(header);
            WriteLengthPrefixedString(writer, "TEST_GROUP_" + groupName);
            WriteLengthPrefixedString(writer, "REMOTE_TEST_" + groupName);
            WriteLengthPrefixedString(writer, "这是用于确认分组下载结果的测试关卡配置：" + groupName);

            WriteLevelRecord(writer, 1, groupName, "REMOTE_CODE_" + groupName, "第一关测试内容：" + groupName);
            WriteLevelRecord(writer, 2, groupName, "REMOTE_CODE_" + groupName + "_2", "第二关测试内容：" + groupName);

            var dataOffset = (int)stream.Position;
            var payloadText = "TEST_BINARY_PAYLOAD_" + groupName + "_" + groupName.Length;
            writer.Write(Encoding.UTF8.GetBytes(payloadText));
            for (var i = 0; i < 32; i++)
            {
                writer.Write((byte)((groupName.GetHashCode() + i * 17) & 0xFF));
            }

            writer.Flush();
            var bytes = stream.ToArray();
            WriteInt32(bytes, 0x04, 1);
            WriteInt32(bytes, 0x08, 2);
            WriteInt32(bytes, 0x0C, dataOffset);
            WriteInt32(bytes, 0x18, bytes.Length);
            WriteInt32(bytes, 0x1C, bytes.Length - dataOffset);
            return bytes;
        }
    }

    private static void WriteLevelRecord(BinaryWriter writer, int levelId, string groupName, string remoteCode, string description)
    {
        writer.Write(1);
        writer.Write(levelId);
        WriteLengthPrefixedString(writer, groupName);
        WriteLengthPrefixedString(writer, remoteCode);
        WriteLengthPrefixedString(writer, description);
    }

    private static void WriteLengthPrefixedString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        if (bytes.Length > byte.MaxValue)
        {
            throw new InvalidDataException("测试二进制文本字段长度不能超过 255 字节。");
        }

        writer.Write((byte)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)value;
        bytes[offset + 1] = (byte)(value >> 8);
        bytes[offset + 2] = (byte)(value >> 16);
        bytes[offset + 3] = (byte)(value >> 24);
    }

    private void WriteBytesFile(string path, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, bytes);
    }

    private void WriteTextFile(string path, string text)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, text, RemoteTextUtility.Utf8WithoutBom);
    }

    private string GetProjectRootPath()
    {
        var assetsDirectory = new DirectoryInfo(Application.dataPath);
        return assetsDirectory.Parent?.FullName ?? Directory.GetCurrentDirectory();
    }

    private string GetTestOutputRootPath()
    {
        return Path.GetFullPath(Path.Combine(GetProjectRootPath(), TestOutputDirectoryName)).Replace("\\", "/");
    }

    private string GetCommonOutputRootPath()
    {
        return Path.Combine(GetTestOutputRootPath(), runtimeConfig.CommonDirectoryName).Replace("\\", "/");
    }

    private string GetTestStrategyPath()
    {
        return Path.Combine(Application.dataPath, "Editor", "RemoteContent",
            runtimeConfig.CommonDirectoryName, runtimeConfig.RemoteGroupDataName).Replace("\\", "/");
    }

    private string GetTestOutputStrategyPath()
    {
        return Path.Combine(GetCommonOutputRootPath(), runtimeConfig.RemoteGroupDataName).Replace("\\", "/");
    }

    private string GetLocalDefaultPath()
    {
        return Path.Combine(
            Application.dataPath,
            "StreamingAssets",
            RemoteGroupRuntimeConfig.StreamingAssetsDirectoryName,
            RemoteGroupRuntimeConfig.LocalDefaultGameDataFileName).Replace("\\", "/");
    }

    private string GetRemoteGameDataPath(string groupName)
    {
        return Path.Combine(GetCommonOutputRootPath(), runtimeConfig.GameDataDirectoryName, groupName + ".bytes").Replace("\\", "/");
    }

#if BIZZA_REAL_WITHDRAW
    private string GetCountryOutputRootPath(AccountModule.E_CountryType country)
    {
        return Path.Combine(GetTestOutputRootPath(), country.ToString()).Replace("\\", "/");
    }

    private string GetCountryStrategyPath(AccountModule.E_CountryType country)
    {
        return Path.Combine(Application.dataPath, "Editor", "RemoteContent",
            country.ToString(), runtimeConfig.RemoteGroupDataName).Replace("\\", "/");
    }

    private string GetCountryTestStrategyPath(AccountModule.E_CountryType country)
    {
        return Path.Combine(GetCountryOutputRootPath(country), runtimeConfig.RemoteGroupDataName).Replace("\\", "/");
    }

    private string GetCountryGameDataPath(AccountModule.E_CountryType country, string groupName)
    {
        return Path.Combine(GetCountryOutputRootPath(country), runtimeConfig.GameDataDirectoryName, groupName + ".bytes").Replace("\\", "/");
    }
#endif
}
#endif
