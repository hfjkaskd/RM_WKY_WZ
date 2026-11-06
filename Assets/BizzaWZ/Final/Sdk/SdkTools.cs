#if BIZZA_REAL_WITHDRAW
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;  // 明确指定 Debug 类型

namespace Bizza.Sdk
{
    public class SDKManagerWindow : EditorWindow
{
    private string sdkDirectory;  // SDK 根目录
    private string configFilePath = "ProjectSettings/SDKPathConfig.json";  // 保存 SDK 路径的配置文件
    private string savePath = "ProjectSettings/SDKFilePaths.json";  // 保存文件路径的 JSON 文件
    private const string SdkDirectoryEditorPrefsKey = "Bizza.Sdk.SDKManagerWindow.SdkDirectory";
    private const string ProjectRootRecordPrefix = "ProjectRoot/";
    private List<SDKInfo> sdkList = new List<SDKInfo>();
    
    // 存储每个 SDK 导入的文件路径，键为 SDK 名称
    private Dictionary<string, List<string>> importedFiles = new Dictionary<string, List<string>>();
    private Dictionary<string, List<ManifestDependencyRecord>> importedManifestDependencies = new Dictionary<string, List<ManifestDependencyRecord>>();
    private Dictionary<string, List<ScopedRegistryRecord>> importedScopedRegistries = new Dictionary<string, List<ScopedRegistryRecord>>();

    [MenuItem("工具/SDK/管理器")]
    public static void ShowWindow()
    {
        SDKManagerWindow window = GetWindow<SDKManagerWindow>();
        window.titleContent = new GUIContent("SDK 管理器");
        window.minSize = new Vector2(800, 500);
        window.Show();
    }

    private void OnEnable()
    {
        LoadSDKPath();  // 加载 SDK 路径
        LoadSDKs();     // 加载 SDK 配置
        LoadImportedFiles(); // 加载已导入的文件记录
    }

    private void OnGUI()
    {
        // 显示 SDK 路径设置框
        GUILayout.Label("SDK 路径设置", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        string newSdkPath = EditorGUILayout.TextField("SDK 根目录", sdkDirectory, GUILayout.ExpandWidth(true));

        // 如果路径发生变化，记录并更新
        if (newSdkPath != sdkDirectory)
        {
            sdkDirectory = newSdkPath;
            SaveSDKPath();  // 保存修改后的 SDK 路径
            LoadSDKs();     // 重新加载 SDK
        }

        if (GUILayout.Button("浏览...", GUILayout.Width(80)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择 SDK 路径", sdkDirectory, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                sdkDirectory = selectedPath;
                SaveSDKPath();
                LoadSDKs();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        // 显示当前导入状态
        string currentlyImportedSDK = GetCurrentlyImportedSDK();
        string currentlyImportedSDKDisplayName = GetSDKDisplayName(currentlyImportedSDK);
        if (!string.IsNullOrEmpty(currentlyImportedSDK))
        {
            EditorGUILayout.HelpBox($"当前已导入的 SDK: {currentlyImportedSDKDisplayName}\n必须删除当前 SDK 后才能导入其他 SDK。", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("当前没有导入任何 SDK。您可以导入任意 SDK。", MessageType.Info);
        }

        GUILayout.Space(10);

        // 显示当前宏定义状态
        string currentSDK = GetCurrentlyImportedSDK();
        if (!string.IsNullOrEmpty(currentSDK))
        {
            var sdk = sdkList.FirstOrDefault(s => s.Name == currentSDK);
            if (sdk != null)
            {
                string macroText = sdk.Macros != null && sdk.Macros.Count > 0
                    ? string.Join(";", sdk.Macros)
                    : "无";
                EditorGUILayout.HelpBox($"当前宏定义: {macroText}", MessageType.Info);
            }
        }

        GUILayout.Space(10);

        // SVN 忽略设置区域
        // GUILayout.Label("SVN 忽略设置", EditorStyles.boldLabel);
        // EditorGUILayout.HelpBox(
        //     "SDK 文件在导入后将自动被 SVN 忽略。\n" +
        //     "系统会自动创建 .svnignore 文件。\n" +
        //     "如果 SVN 命令执行失败，请手动应用忽略规则。",
        //     MessageType.Info);

        GUILayout.BeginHorizontal();

        // if (GUILayout.Button("生成 SVN 忽略文件", GUILayout.Width(160)))
        // {
        //     GenerateSVNIgnoreFile();
        // }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 显示 SDK 列表
        GUILayout.Label("SDK 列表", EditorStyles.boldLabel);

        if (sdkList.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到任何 SDK。请检查 SDK 路径设置。", MessageType.Info);
        }

        // 添加刷新按钮
        if (GUILayout.Button("刷新 SDK 列表", GUILayout.Width(120)))
        {
            LoadSDKs();
        }

        GUILayout.Space(10);

        // 显示每个 SDK 的信息和操作按钮
        foreach (var sdk in sdkList)
        {
            SDKStatus status = GetSDKStatus(sdk);
            Color originalColor = GUI.color;
            bool hasImportedSDK = !string.IsNullOrEmpty(currentlyImportedSDK);

            GUILayout.BeginVertical("Box");

            // 第一行：SDK 名称和状态
            GUILayout.BeginHorizontal();

            // 根据状态设置颜色
            switch (status)
            {
                case SDKStatus.Imported:
                    GUI.color = Color.green;
                    break;
                case SDKStatus.Corrupted:
                    GUI.color = Color.red;
                    break;
                default:
                    GUI.color = Color.white;
                    break;
            }

            GUILayout.Label(sdk.DisplayName, EditorStyles.boldLabel, GUILayout.Width(200));
            GUI.color = originalColor;

            GUILayout.Label("当前状态：" + GetStatusText(status), GetStatusStyle(status));

            GUILayout.FlexibleSpace();

            // 不再显示文件大小（优化性能）
            GUILayout.Label("", EditorStyles.miniLabel, GUILayout.Width(80));

            GUILayout.EndHorizontal();

            // 显示宏定义信息
            if (sdk.Macros != null && sdk.Macros.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUI.color = Color.yellow;
                GUILayout.Label($"宏定义: {string.Join(", ", sdk.Macros)}", EditorStyles.miniLabel);
                GUI.color = originalColor;
                GUILayout.EndHorizontal();
            }

            // 第二行：文档链接和操作按钮
            GUILayout.BeginHorizontal();

            if (!string.IsNullOrEmpty(sdk.Reference))
            {
                if (GUILayout.Button("打开文档", GUILayout.Width(80)))
                {
                    Application.OpenURL(sdk.Reference);
                }
            }
            else
            {
                GUILayout.Label("无文档链接", EditorStyles.miniLabel, GUILayout.Width(80));
            }

            // 导入按钮 - 只在未导入或损坏状态可用，且没有其他 SDK 被导入时
            bool canImport = (status == SDKStatus.NotImported || status == SDKStatus.Corrupted) && !hasImportedSDK;
            GUI.enabled = canImport;
            if (GUILayout.Button(status == SDKStatus.Corrupted ? "修复" : "导入", GUILayout.Width(80)))
            {
                if (hasImportedSDK)
                {
                    EditorUtility.DisplayDialog("无法导入",
                        $"在导入 '{sdk.DisplayName}' 之前，必须删除当前已导入的 SDK '{currentlyImportedSDKDisplayName}'。", "确定");
                    return;
                }

                if (status == SDKStatus.Imported && !EditorUtility.DisplayDialog("确认重新导入",
                    $"SDK '{sdk.DisplayName}' 已导入。是否重新导入？这将覆盖现有文件。",
                    "是", "否"))
                {
                    return;
                }
                ImportSDK(sdk);
            }
            GUI.enabled = true;

            // 删除按钮 - 只在已导入状态可用
            GUI.enabled = status == SDKStatus.Imported;
            if (GUILayout.Button("删除", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("确认删除",
                    $"确定要删除 SDK '{sdk.Name}' 吗？",
                    "是", "否"))
                {
                    DeleteSDK(sdk);
                }
            }
            GUI.enabled = true;

            GUI.enabled = Directory.Exists(sdk.Directory);
            if (GUILayout.Button("\u6253\u5f00\u8def\u5f84", GUILayout.Width(80)))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = sdk.Directory,
                    UseShellExecute = true
                });
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }

    // 生成 .svnignore 文件
    private void GenerateSVNIgnoreFile()
    {
        return;
        try
        {
            StringBuilder ignoreContent = new StringBuilder();
            ignoreContent.AppendLine("# 自动生成的 .svnignore 文件 - SDK 文件忽略规则");
            ignoreContent.AppendLine("# 由 SDK 管理器生成于 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            ignoreContent.AppendLine("# 此文件会被 SVN 自动忽略");
            ignoreContent.AppendLine();

            if (importedFiles.Count > 0)
            {
                ignoreContent.AppendLine("# 需要忽略的 SDK 文件");
                foreach (var sdkEntry in importedFiles)
                {
                    if (sdkEntry.Value.Count > 0)
                    {
                        ignoreContent.AppendLine($"# SDK: {sdkEntry.Key}");
                        // 添加每个 SDK 文件的忽略规则
                        foreach (var filePath in sdkEntry.Value)
                        {
                            // 确保路径使用正斜杠
                            string normalizedPath = filePath.Replace("\\", "/");
                            ignoreContent.AppendLine(normalizedPath);
                        }
                        ignoreContent.AppendLine();
                    }
                }
            }
            else
            {
                ignoreContent.AppendLine("# 当前没有导入任何 SDK 文件");
            }

            // 确保 .svnignore 文件本身也被忽略
            ignoreContent.AppendLine();
            ignoreContent.AppendLine("# 忽略 .svnignore 文件本身");
            ignoreContent.AppendLine(".svnignore");

            string ignoreFilePath = Path.Combine(Application.dataPath, "..", ".svnignore");
            File.WriteAllText(ignoreFilePath, ignoreContent.ToString());

            // 尝试应用 SVN 忽略规则，但失败时只记录不弹窗
            TryApplySVNIgnoreRules(ignoreFilePath);

            EditorUtility.DisplayDialog("SVN 忽略文件已生成",
                $".svnignore 文件已生成到：\n{ignoreFilePath}\n\n" +
                "如果自动应用失败，请手动执行以下命令：\n" +
                $"cd \"{Path.GetDirectoryName(ignoreFilePath)}\"\n" +
                "svn propset svn:global-ignores -F .svnignore .", "确定");

            Debug.Log($"SVN 忽略文件已生成: {ignoreFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"生成 .svnignore 文件失败: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"生成 .svnignore 文件失败: {e.Message}", "确定");
        }
    }

    // 尝试应用 SVN 忽略规则，但不强制要求
    private void TryApplySVNIgnoreRules(string svnIgnorePath)
    {
        try
        {
            string projectRoot = Path.GetDirectoryName(svnIgnorePath);

            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "svn",
                Arguments = $"propset svn:global-ignores -F .svnignore .",
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(processInfo))
            {
                process.WaitForExit(5000);

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Debug.Log($"SVN 忽略规则应用成功: {output}");
                }
                else
                {
                    // 只记录错误，不弹窗
                    Debug.LogWarning($"SVN 忽略设置失败（可能不在 SVN 仓库中）: {error}");
                }
            }
        }
        catch (Exception e)
        {
            // 只记录错误，不弹窗
            Debug.LogWarning($"尝试应用 SVN 忽略规则失败: {e.Message}");
        }
    }

    // 获取当前已导入的 SDK 名称（如果有的话）
    private string GetCurrentlyImportedSDK()
    {
        var importedSDKs = importedFiles.Keys
            .Union(importedManifestDependencies.Keys)
            .Union(importedScopedRegistries.Keys)
            .Distinct()
            .Where(sdkName =>
                HasImportedContent(sdkName) &&
                GetSDKStatus(sdkList.FirstOrDefault(s => s.Name == sdkName)) == SDKStatus.Imported)
            .ToList();

        return importedSDKs.FirstOrDefault();
    }

    private string GetSDKDisplayName(string sdkName)
    {
        if (string.IsNullOrEmpty(sdkName))
        {
            return sdkName;
        }

        return sdkList.FirstOrDefault(s => s.Name == sdkName)?.DisplayName ?? sdkName;
    }

    // SDK 状态枚举
    private enum SDKStatus
    {
        NotImported,  // 未导入
        Imported,     // 已导入
        Corrupted     // 损坏（记录存在但文件缺失）
    }

    // 获取 SDK 状态 - 修复关键bug：检查目标文件而不是源文件
    private SDKStatus GetSDKStatus(SDKInfo sdk)
    {
        if (sdk == null) return SDKStatus.NotImported;

        string sdkName = sdk.Name;

        if (!HasImportedContent(sdkName))
        {
            return SDKStatus.NotImported;
        }

        // 检查已记录的文件是否在目标位置实际存在
        // 修复：检查目标文件（Unity项目中的文件），而不是源文件
        var importedPaths = importedFiles.TryGetValue(sdkName, out var paths) && paths != null
            ? paths
            : new List<string>();
        int checkCount = Math.Min(3, importedPaths.Count); // 只检查前3个文件（性能优化）

        for (int i = 0; i < checkCount; i++)
        {
            string relativePath = importedPaths[i];
            // 关键修复：检查目标路径（Unity项目中的文件）
            // 需要根据文件类型确定正确的路径
            string targetPath = GetTargetFilePath(relativePath);

            if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
            {
                Debug.LogWarning($"检测到损坏文件: {relativePath} (目标路径: {targetPath})");
                return SDKStatus.Corrupted;
            }
        }

        if (!AreManifestDependenciesImported(sdkName))
        {
            Debug.LogWarning($"妫€娴嬪埌 manifest 渚濊禆涓庡鍏ヨ褰曚笉涓€鑷? SDK: {sdkName}");
            return SDKStatus.Corrupted;
        }

        if (!AreScopedRegistriesImported(sdkName))
        {
            Debug.LogWarning($"Scoped registries are out of sync for SDK: {sdkName}");
            return SDKStatus.Corrupted;
        }

        return SDKStatus.Imported;
    }

    // 根据相对路径获取目标文件完整路径
    private string GetTargetFilePath(string relativePath)
    {
        // 统一路径分隔符
        string normalizedPath = relativePath.Replace("\\", "/");

        if (normalizedPath.StartsWith(ProjectRootRecordPrefix))
        {
            return Path.GetFullPath(Path.Combine(GetProjectRootPath(),
                normalizedPath.Substring(ProjectRootRecordPrefix.Length)));
        }
        else if (normalizedPath.StartsWith("Packages/"))
        {
            // Packages 文件夹的文件 - 相对于项目根目录
            return Path.GetFullPath(Path.Combine(GetProjectRootPath(), normalizedPath));
        }
        else
        {
            // Assets 文件夹的文件 - 相对于 Assets 目录
            return Path.GetFullPath(Path.Combine(Application.dataPath, normalizedPath));
        }
    }

    // 获取状态显示文本
    private bool TryGetDirectoryCleanupRoot(string targetPath, out string cleanupRoot)
    {
        string fullTargetPath = Path.GetFullPath(targetPath);
        string assetsRoot = Path.GetFullPath(Application.dataPath);
        if (IsPathInsideRoot(assetsRoot, fullTargetPath))
        {
            cleanupRoot = assetsRoot;
            return true;
        }

        string packagesRoot = Path.GetFullPath(Path.Combine(GetProjectRootPath(), "Packages"));
        if (IsPathInsideRoot(packagesRoot, fullTargetPath))
        {
            cleanupRoot = packagesRoot;
            return true;
        }

        string projectRoot = Path.GetFullPath(GetProjectRootPath());
        if (IsPathInsideRoot(projectRoot, fullTargetPath))
        {
            cleanupRoot = projectRoot;
            return true;
        }

        cleanupRoot = null;
        return false;
    }

    private int DeleteEmptyParentDirectories(string targetPath, List<string> failedDeletes)
    {
        if (!TryGetDirectoryCleanupRoot(targetPath, out var cleanupRoot))
        {
            return 0;
        }

        int deletedDirectoryCount = 0;
        string currentDirectory = Path.GetDirectoryName(Path.GetFullPath(targetPath));

        while (!string.IsNullOrEmpty(currentDirectory) &&
               !string.Equals(currentDirectory, cleanupRoot, StringComparison.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(currentDirectory))
            {
                currentDirectory = Path.GetDirectoryName(currentDirectory);
                continue;
            }

            if (Directory.EnumerateFileSystemEntries(currentDirectory).Any())
            {
                break;
            }

            try
            {
                Directory.Delete(currentDirectory);
                deletedDirectoryCount++;

                string metaPath = currentDirectory + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
            catch (Exception e)
            {
                failedDeletes?.Add($"{currentDirectory}: {e.Message}");
                break;
            }

            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }

        return deletedDirectoryCount;
    }

    private string GetStatusText(SDKStatus status)
    {
        switch (status)
        {
            case SDKStatus.NotImported: return "[未导入]";
            case SDKStatus.Imported: return "[已导入]";
            case SDKStatus.Corrupted: return "[已损坏]";
            default: return "[未知]";
        }
    }

    // 获取状态显示样式
    private GUIStyle GetStatusStyle(SDKStatus status)
    {
        switch (status)
        {
            case SDKStatus.Imported:
                var greenStyle = new GUIStyle(EditorStyles.miniLabel);
                greenStyle.normal.textColor = Color.green;
                return greenStyle;
            case SDKStatus.Corrupted:
                var redStyle = new GUIStyle(EditorStyles.miniLabel);
                redStyle.normal.textColor = Color.red;
                return redStyle;
            default:
                return EditorStyles.miniLabel;
        }
    }

    // 添加宏定义到 PlayerSettings
    private void AddMacroDefinitions(SDKInfo sdk)
    {
        if (sdk.Macros == null || sdk.Macros.Count == 0)
        {
            Debug.Log($"SDK '{sdk.Name}' 没有配置宏定义，跳过宏定义设置");
            return;
        }

        try
        {
            // 获取当前构建目标
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                // 如果没有选择构建目标，默认使用当前平台
                targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            }

            // 获取现有的宏定义
            string existingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            List<string> defineList = new List<string>();

            if (!string.IsNullOrEmpty(existingDefines))
            {
                defineList.AddRange(existingDefines.Split(';'));
            }

            // 去重并移除空字符串
            defineList = defineList.Where(d => !string.IsNullOrEmpty(d)).Distinct().ToList();

            // 添加新的宏定义
            int addedCount = 0;
            foreach (string macro in sdk.Macros)
            {
                if (!string.IsNullOrEmpty(macro) && !defineList.Contains(macro))
                {
                    defineList.Add(macro);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                // 设置新的宏定义
                string newDefines = string.Join(";", defineList.ToArray());
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);

                Debug.Log($"已为 SDK '{sdk.Name}' 添加 {addedCount} 个宏定义: {string.Join(", ", sdk.Macros)}");
                Debug.Log($"当前宏定义: {newDefines}");
            }
            else
            {
                Debug.Log($"SDK '{sdk.Name}' 的宏定义已存在，无需添加");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"添加宏定义失败: {e.Message}");
        }
    }

    // 移除宏定义
    private void RemoveMacroDefinitions(SDKInfo sdk)
    {
        if (sdk.Macros == null || sdk.Macros.Count == 0)
        {
            Debug.Log($"SDK '{sdk.Name}' 没有配置宏定义，跳过宏定义移除");
            return;
        }

        try
        {
            // 获取当前构建目标
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                // 如果没有选择构建目标，默认使用当前平台
                targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            }

            // 获取现有的宏定义
            string existingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            if (string.IsNullOrEmpty(existingDefines))
            {
                Debug.Log($"当前没有宏定义，无需移除");
                return;
            }

            List<string> defineList = new List<string>(existingDefines.Split(';'));

            // 移除属于这个SDK的宏定义
            int removedCount = 0;
            List<string> macrosToRemove = sdk.Macros;

            foreach (string macro in macrosToRemove)
            {
                if (!string.IsNullOrEmpty(macro) && defineList.Contains(macro))
                {
                    defineList.Remove(macro);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                // 设置新的宏定义
                string newDefines = string.Join(";", defineList.ToArray());
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, newDefines);

                Debug.Log($"已移除 SDK '{sdk.Name}' 的 {removedCount} 个宏定义: {string.Join(", ", sdk.Macros)}");
                Debug.Log($"当前宏定义: {newDefines}");
            }
            else
            {
                Debug.Log($"SDK '{sdk.Name}' 的宏定义已不存在，无需移除");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"移除宏定义失败: {e.Message}");
        }
    }

    // 加载保存的 SDK 路径
    private void LoadSDKPath()
    {
        if (EditorPrefs.HasKey(SdkDirectoryEditorPrefsKey))
        {
            sdkDirectory = EditorPrefs.GetString(SdkDirectoryEditorPrefsKey, GetDefaultSDKPath());
        }
        else if (File.Exists(configFilePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(configFilePath);
                sdkDirectory = JsonUtility.FromJson<SDKPathConfig>(jsonContent).SdkPath;
                if (!string.IsNullOrEmpty(sdkDirectory))
                {
                    EditorPrefs.SetString(SdkDirectoryEditorPrefsKey, sdkDirectory);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载 SDK 路径配置失败: {e.Message}");
                sdkDirectory = GetDefaultSDKPath();
            }
        }
        else
        {
            sdkDirectory = GetDefaultSDKPath();
        }

        Debug.Log($"SDK 目录: {sdkDirectory}");
    }

    private string GetDefaultSDKPath()
    {
        var rawPath = Path.Combine(Application.dataPath, "../../../SdkProject");
        string normalizedPath = Path.GetFullPath(rawPath);

        string finalPath = normalizedPath.Replace('/', '\\');
        return finalPath;
    }

    // 保存 SDK 路径
    private void SaveSDKPath()
    {
        try
        {
            if (string.IsNullOrEmpty(sdkDirectory))
            {
                EditorPrefs.DeleteKey(SdkDirectoryEditorPrefsKey);
            }
            else
            {
                EditorPrefs.SetString(SdkDirectoryEditorPrefsKey, sdkDirectory);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"保存 SDK 路径失败: {e.Message}");
        }
    }

    // 加载 SDK 配置信息
    private void LoadSDKs()
    {
        sdkList.Clear();

        if (Directory.Exists(sdkDirectory))
        {
            var subDirectories = Directory.GetDirectories(sdkDirectory);

            Debug.Log($"在 SDK 路径中找到 {subDirectories.Length} 个目录");

            foreach (var dir in subDirectories)
            {
                string sdkName = Path.GetFileName(dir);
                string configPath = Path.Combine(dir, "sdk_config.json");

                if (File.Exists(configPath))
                {
                    Debug.Log($"读取配置文件: {configPath}");

                    try
                    {
                        var sdkConfig = LoadSDKConfigFromFile(configPath);

                        var sdkInfo = new SDKInfo
                        {
                            Name = sdkName,
                            DisplayName = string.IsNullOrEmpty(sdkConfig.name) ? sdkName : sdkConfig.name,
                            ConfigJson = configPath,
                            Reference = sdkConfig.reference,
                            Directory = dir,
                            Macros = sdkConfig.macros
                        };

                        sdkList.Add(sdkInfo);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"解析 {sdkName} 的配置文件失败: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log($"目录中未找到配置文件: {dir}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"SDK 目录不存在: {sdkDirectory}");
        }
    }

    // 加载已导入的文件记录
    private void LoadImportedFiles()
    {
        importedFiles.Clear();
        importedManifestDependencies.Clear();
        importedScopedRegistries.Clear();

        if (File.Exists(savePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(savePath);
                SDKImportRecords savedData = null;

                try
                {
                    savedData = JsonConvert.DeserializeObject<SDKImportRecords>(jsonContent);
                }
                catch
                {
                    savedData = JsonUtility.FromJson<SDKImportRecords>(jsonContent);
                }

                if (savedData?.Records == null)
                {
                    return;
                }

                foreach (var record in savedData.Records)
                {
                    importedFiles[record.SdkName] = record.Paths ?? new List<string>();
                    importedManifestDependencies[record.SdkName] = record.ManifestDependencies ?? new List<ManifestDependencyRecord>();
                    importedScopedRegistries[record.SdkName] = record.ScopedRegistries ?? new List<ScopedRegistryRecord>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载已导入文件记录失败: {e.Message}");
            }
        }
    }

    // 保存导入的文件记录
    private void SaveImportedFiles()
    {
        try
        {
            var sdkNames = importedFiles.Keys
                .Union(importedManifestDependencies.Keys)
                .Union(importedScopedRegistries.Keys)
                .Distinct()
                .ToList();

            var records = sdkNames
                .Select(sdkName => new SDKImportRecord
                {
                    SdkName = sdkName,
                    Paths = importedFiles.TryGetValue(sdkName, out var paths) ? paths : new List<string>(),
                    ManifestDependencies = importedManifestDependencies.TryGetValue(sdkName, out var manifestDependencies)
                        ? manifestDependencies
                        : new List<ManifestDependencyRecord>(),
                    ScopedRegistries = importedScopedRegistries.TryGetValue(sdkName, out var scopedRegistries)
                        ? scopedRegistries
                        : new List<ScopedRegistryRecord>()
                })
                .Where(record =>
                    (record.Paths != null && record.Paths.Count > 0) ||
                    (record.ManifestDependencies != null && record.ManifestDependencies.Count > 0) ||
                    (record.ScopedRegistries != null && record.ScopedRegistries.Count > 0))
                .ToList();

            var saveData = new SDKImportRecords { Records = records };
            File.WriteAllText(
                savePath,
                JsonConvert.SerializeObject(saveData, Formatting.Indented) + Environment.NewLine,
                new UTF8Encoding(false));
        }
        catch (Exception e)
        {
            Debug.LogError($"保存已导入文件记录失败: {e.Message}");
        }
    }

    // 检查路径是否在忽略列表中
    private SDKConfig LoadSDKConfigFromFile(string configPath)
    {
        string jsonContent = File.ReadAllText(configPath);
        JObject root = JObject.Parse(jsonContent);

        return new SDKConfig
        {
            name = root.Value<string>("name"),
            reference = root.Value<string>("reference"),
            packages = ParseStringList(root["packages"]),
            projectRootPaths = ParseStringList(
                root["projectRootPaths"] ??
                root["rootPaths"] ??
                root["projectPaths"] ??
                root["assetSiblingPaths"]),
            macros = ParseStringList(root["macros"]),
            scopedRegistries = ParseScopedRegistries(root["scopedRegistries"]),
            manifestDependencies = ParseManifestDependencies(
                root["manifestDependencies"] ??
                root["manifestPackages"] ??
                root["packageDependencies"])
        };
    }

    private List<string> ParseStringList(JToken token)
    {
        if (!(token is JArray array))
        {
            return new List<string>();
        }

        return array
            .Values<string>()
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private List<ManifestPackageConfig> ParseManifestDependencies(JToken token)
    {
        var manifestDependencies = new List<ManifestPackageConfig>();

        if (token == null)
        {
            return manifestDependencies;
        }

        if (token is JObject manifestObject)
        {
            foreach (var property in manifestObject.Properties())
            {
                string version = property.Value?.ToString();
                if (string.IsNullOrWhiteSpace(property.Name) || string.IsNullOrWhiteSpace(version))
                {
                    continue;
                }

                manifestDependencies.Add(new ManifestPackageConfig
                {
                    Name = property.Name,
                    Version = version
                });
            }
        }
        else if (token is JArray manifestArray)
        {
            foreach (var item in manifestArray.OfType<JObject>())
            {
                string packageName = item.Value<string>("name") ?? item.Value<string>("package");
                string packageVersion = item.Value<string>("version") ?? item.Value<string>("value");

                if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(packageVersion))
                {
                    continue;
                }

                manifestDependencies.Add(new ManifestPackageConfig
                {
                    Name = packageName,
                    Version = packageVersion
                });
            }
        }

        return manifestDependencies
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToList();
    }

    private List<ScopedRegistryConfig> ParseScopedRegistries(JToken token)
    {
        var scopedRegistries = new List<ScopedRegistryConfig>();
        if (!(token is JArray registryArray))
        {
            return scopedRegistries;
        }

        foreach (var item in registryArray.OfType<JObject>())
        {
            string name = item.Value<string>("name");
            string url = item.Value<string>("url");
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            scopedRegistries.Add(new ScopedRegistryConfig
            {
                Name = name,
                Url = url,
                Scopes = ParseStringList(item["scopes"])
                    .Distinct(StringComparer.Ordinal)
                    .ToList()
            });
        }

        return scopedRegistries
            .GroupBy(item => GetScopedRegistryKey(item.Name, item.Url), StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToList();
    }

    private bool IsPathInsideRoot(string rootPath, string targetPath)
    {
        string relativePath = Path.GetRelativePath(rootPath, targetPath);
        return !relativePath.StartsWith("..") && !Path.IsPathRooted(relativePath);
    }

    private string BuildProjectRootRecordedPath(string relativePath)
    {
        return $"{ProjectRootRecordPrefix}{relativePath.Replace("\\", "/").TrimStart('/')}";
    }

    #if false
    private void ImportProjectRootPaths(string sdkFolder, SDKConfig sdkConfig, List<string> copiedFiles, string sdkName)
    {
        var projectRootPaths = sdkConfig.projectRootPaths ?? new List<string>();
        if (projectRootPaths.Count == 0)
        {
            return;
        }

        string projectRootPath = GetProjectRootPath();
        foreach (var configuredPath in projectRootPaths)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                continue;
            }

            string sourcePath = Path.GetFullPath(Path.Combine(sdkFolder, configuredPath));
            if (!IsPathInsideRoot(sdkFolder, sourcePath))
            {
                Debug.LogWarning($"SDK '{sdkName}' 鐨勯」鐩牴鐩綍璺緞瓒呭嚭浜?SDK 鏍圭洰褰? {configuredPath}");
                continue;
            }

            string relativeProjectRootPath = Path.GetRelativePath(sdkFolder, sourcePath).Replace("\\", "/");
            if (string.Equals(relativeProjectRootPath, "Assets", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(relativeProjectRootPath, "Packages", StringComparison.OrdinalIgnoreCase) ||
                relativeProjectRootPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                relativeProjectRootPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"SDK '{sdkName}' 鐨?projectRootPaths 涓嶉渶閲嶅閰嶇疆 '{relativeProjectRootPath}'");
                continue;
            }

            string targetPath = Path.GetFullPath(Path.Combine(projectRootPath, relativeProjectRootPath));
            if (Directory.Exists(sourcePath))
            {
                var rootFiles = GetFilesIgnoringIgnoreFolders(sourcePath, SearchOption.AllDirectories);
                Debug.Log($"寮€濮嬪鍒堕」鐩牴鐩綍璺緞 '{relativeProjectRootPath}'锛屽叡 {rootFiles.Count} 涓枃浠?);

                for (int i = 0; i < rootFiles.Count; i++)
                {
                    string file = rootFiles[i];
                    string childRelativePath = Path.GetRelativePath(sourcePath, file);
                    string targetFilePath = Path.Combine(targetPath, childRelativePath);

                    string targetDirectory = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    File.Copy(file, targetFilePath, true);
                    copiedFiles.Add(BuildProjectRootRecordedPath(
                        Path.Combine(relativeProjectRootPath, childRelativePath)));

                    if (i % 500 == 0 || i == rootFiles.Count - 1)
                    {
                        Debug.Log($"鏍圭洰褰?'{relativeProjectRootPath}': 宸插鍒?{i + 1}/{rootFiles.Count} 涓枃浠?);
                    }
                }
            }
            else if (File.Exists(sourcePath))
            {
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath, true);
                copiedFiles.Add(BuildProjectRootRecordedPath(relativeProjectRootPath));
            }
            else
            {
                Debug.LogWarning($"鍦?SDK '{sdkName}' 涓湭鎵惧埌 Assets 同级路径: {sourcePath}");
            }
        }
    }

    #endif

    private void ImportProjectRootPaths(string sdkFolder, SDKConfig sdkConfig, List<string> copiedFiles, string sdkName)
    {
        var projectRootPaths = sdkConfig.projectRootPaths ?? new List<string>();
        if (projectRootPaths.Count == 0)
        {
            return;
        }

        string projectRootPath = GetProjectRootPath();
        foreach (var configuredPath in projectRootPaths)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                continue;
            }

            string sourcePath = Path.GetFullPath(Path.Combine(sdkFolder, configuredPath));
            if (!IsPathInsideRoot(sdkFolder, sourcePath))
            {
                Debug.LogWarning($"SDK '{sdkName}' projectRootPaths entry is outside the SDK root: {configuredPath}");
                continue;
            }

            string relativeProjectRootPath = Path.GetRelativePath(sdkFolder, sourcePath).Replace("\\", "/");
            if (string.Equals(relativeProjectRootPath, "Assets", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(relativeProjectRootPath, "Packages", StringComparison.OrdinalIgnoreCase) ||
                relativeProjectRootPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                relativeProjectRootPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"SDK '{sdkName}' projectRootPaths should not include '{relativeProjectRootPath}' because Assets and Packages are handled separately.");
                continue;
            }

            string targetPath = Path.GetFullPath(Path.Combine(projectRootPath, relativeProjectRootPath));
            if (Directory.Exists(sourcePath))
            {
                var rootFiles = GetFilesIgnoringIgnoreFolders(sourcePath, SearchOption.AllDirectories);
                Debug.Log($"Copying project root path '{relativeProjectRootPath}', total {rootFiles.Count} files.");

                for (int i = 0; i < rootFiles.Count; i++)
                {
                    string file = rootFiles[i];
                    string childRelativePath = Path.GetRelativePath(sourcePath, file);
                    string targetFilePath = Path.Combine(targetPath, childRelativePath);

                    string targetDirectory = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    File.Copy(file, targetFilePath, true);
                    copiedFiles.Add(BuildProjectRootRecordedPath(
                        Path.Combine(relativeProjectRootPath, childRelativePath)));

                    if (i % 500 == 0 || i == rootFiles.Count - 1)
                    {
                        Debug.Log($"Project root path '{relativeProjectRootPath}': copied {i + 1}/{rootFiles.Count} files.");
                    }
                }
            }
            else if (File.Exists(sourcePath))
            {
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                File.Copy(sourcePath, targetPath, true);
                copiedFiles.Add(BuildProjectRootRecordedPath(relativeProjectRootPath));
            }
            else
            {
                Debug.LogWarning($"SDK '{sdkName}' projectRootPaths source was not found: {sourcePath}");
            }
        }
    }

    private string GetProjectRootPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    }

    private string GetProjectManifestPath()
    {
        return Path.GetFullPath(Path.Combine(GetProjectRootPath(), "Packages", "manifest.json"));
    }

    private void SaveManifestJson(string manifestPath, JObject manifest)
    {
        File.WriteAllText(manifestPath, manifest.ToString(Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
    }

    private string GetScopedRegistryKey(string name, string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            return $"url::{url}";
        }

        return $"name::{name ?? string.Empty}";
    }

    private bool IsScopedRegistryConfigValid(ScopedRegistryConfig scopedRegistry)
    {
        return scopedRegistry != null &&
               (!string.IsNullOrWhiteSpace(scopedRegistry.Name) || !string.IsNullOrWhiteSpace(scopedRegistry.Url));
    }

    private JObject CreateScopedRegistryObject(ScopedRegistryConfig scopedRegistry)
    {
        return new JObject
        {
            ["name"] = scopedRegistry.Name ?? string.Empty,
            ["url"] = scopedRegistry.Url ?? string.Empty,
            ["scopes"] = new JArray((scopedRegistry.Scopes ?? new List<string>())
                .Where(scope => !string.IsNullOrWhiteSpace(scope))
                .Distinct(StringComparer.Ordinal))
        };
    }

    private ScopedRegistryConfig ParseScopedRegistryObject(JObject registryObject)
    {
        if (registryObject == null)
        {
            return null;
        }

        return new ScopedRegistryConfig
        {
            Name = registryObject.Value<string>("name"),
            Url = registryObject.Value<string>("url"),
            Scopes = ParseStringList(registryObject["scopes"])
                .Distinct(StringComparer.Ordinal)
                .ToList()
        };
    }

    private int FindScopedRegistryIndex(JArray scopedRegistries, string name, string url)
    {
        if (scopedRegistries == null)
        {
            return -1;
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            for (int i = 0; i < scopedRegistries.Count; i++)
            {
                if (scopedRegistries[i] is JObject registryObject &&
                    string.Equals(registryObject.Value<string>("url"), url, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            for (int i = 0; i < scopedRegistries.Count; i++)
            {
                if (scopedRegistries[i] is JObject registryObject &&
                    string.Equals(registryObject.Value<string>("name"), name, StringComparison.Ordinal))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private bool AreScopedRegistryScopesEqual(IEnumerable<string> leftScopes, IEnumerable<string> rightScopes)
    {
        var left = (leftScopes ?? Enumerable.Empty<string>())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToList();
        var right = (rightScopes ?? Enumerable.Empty<string>())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.Ordinal)
            .ToList();

        return left.SequenceEqual(right, StringComparer.Ordinal);
    }

    private bool AreScopedRegistriesEquivalent(JObject registryObject, ScopedRegistryConfig scopedRegistry)
    {
        if (registryObject == null || !IsScopedRegistryConfigValid(scopedRegistry))
        {
            return false;
        }

        if (!string.Equals(registryObject.Value<string>("name") ?? string.Empty, scopedRegistry.Name ?? string.Empty, StringComparison.Ordinal) ||
            !string.Equals(registryObject.Value<string>("url") ?? string.Empty, scopedRegistry.Url ?? string.Empty, StringComparison.Ordinal))
        {
            return false;
        }

        return AreScopedRegistryScopesEqual(
            ParseStringList(registryObject["scopes"]),
            scopedRegistry.Scopes);
    }

    private ScopedRegistryConfig ParseScopedRegistryJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return ParseScopedRegistryObject(JObject.Parse(json));
        }
        catch
        {
            return null;
        }
    }

    private List<ScopedRegistryConfig> GetConfiguredScopedRegistries(string sdkName)
    {
        SDKInfo sdkInfo = sdkList.FirstOrDefault(item => item.Name == sdkName);
        if (sdkInfo == null || string.IsNullOrWhiteSpace(sdkInfo.ConfigJson) || !File.Exists(sdkInfo.ConfigJson))
        {
            return new List<ScopedRegistryConfig>();
        }

        try
        {
            SDKConfig sdkConfig = LoadSDKConfigFromFile(sdkInfo.ConfigJson);
            return sdkConfig.scopedRegistries ?? new List<ScopedRegistryConfig>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to load scopedRegistries config for SDK '{sdkName}': {e.Message}");
            return new List<ScopedRegistryConfig>();
        }
    }

    private List<ManifestPackageConfig> GetConfiguredManifestDependencies(string sdkName)
    {
        SDKInfo sdkInfo = sdkList.FirstOrDefault(item => item.Name == sdkName);
        if (sdkInfo == null || string.IsNullOrWhiteSpace(sdkInfo.ConfigJson) || !File.Exists(sdkInfo.ConfigJson))
        {
            return new List<ManifestPackageConfig>();
        }

        try
        {
            SDKConfig sdkConfig = LoadSDKConfigFromFile(sdkInfo.ConfigJson);
            return sdkConfig.manifestDependencies ?? new List<ManifestPackageConfig>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"加载 SDK '{sdkName}' 的 manifestDependencies 配置失败: {e.Message}");
            return new List<ManifestPackageConfig>();
        }
    }

    private int RestoreManifestDependency(JObject dependencies, ManifestDependencyRecord dependencyRecord, string fallbackImportedValue)
    {
        if (dependencyRecord == null || string.IsNullOrWhiteSpace(dependencyRecord.Name))
        {
            return 0;
        }

        string currentValue = dependencies.Value<string>(dependencyRecord.Name);
        string importedValue = string.IsNullOrWhiteSpace(dependencyRecord.ImportedValue)
            ? fallbackImportedValue
            : dependencyRecord.ImportedValue;

        if (dependencyRecord.HadPreviousValue)
        {
            if (string.Equals(currentValue, dependencyRecord.PreviousValue, StringComparison.Ordinal))
            {
                return 0;
            }

            dependencies[dependencyRecord.Name] = dependencyRecord.PreviousValue;
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(importedValue) &&
            !string.Equals(currentValue, importedValue, StringComparison.Ordinal))
        {
            return 0;
        }

        JProperty property = dependencies.Property(dependencyRecord.Name);
        if (property == null)
        {
            return 0;
        }

        property.Remove();
        return 1;
    }

    private List<ManifestDependencyRecord> ImportManifestDependencies(SDKConfig sdkConfig)
    {
        var manifestDependencies = sdkConfig.manifestDependencies ?? new List<ManifestPackageConfig>();
        if (manifestDependencies.Count == 0)
        {
            return new List<ManifestDependencyRecord>();
        }

        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("链壘鍒?Packages/manifest.json", manifestPath);
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JObject dependencies = manifest["dependencies"] as JObject;
        if (dependencies == null)
        {
            dependencies = new JObject();
            manifest["dependencies"] = dependencies;
        }

        var importedDependencyRecords = new List<ManifestDependencyRecord>();
        foreach (var manifestDependency in manifestDependencies)
        {
            if (string.IsNullOrWhiteSpace(manifestDependency.Name) || string.IsNullOrWhiteSpace(manifestDependency.Version))
            {
                continue;
            }

            JToken existingValue = dependencies[manifestDependency.Name];
            importedDependencyRecords.Add(new ManifestDependencyRecord
            {
                Name = manifestDependency.Name,
                ImportedValue = manifestDependency.Version,
                HadPreviousValue = existingValue != null,
                PreviousValue = existingValue?.ToString()
            });

            dependencies[manifestDependency.Name] = manifestDependency.Version;
        }

        SaveManifestJson(manifestPath, manifest);
        return importedDependencyRecords;
    }

    private int RemoveManifestDependenciesByConfig(string sdkName)
    {
        var manifestDependencies = GetConfiguredManifestDependencies(sdkName);
        if (manifestDependencies.Count == 0)
        {
            return 0;
        }

        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            return 0;
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JObject dependencies = manifest["dependencies"] as JObject;
        if (dependencies == null)
        {
            return 0;
        }

        int removedCount = 0;
        foreach (var manifestDependency in manifestDependencies)
        {
            if (string.IsNullOrWhiteSpace(manifestDependency.Name))
            {
                continue;
            }

            string currentValue = dependencies.Value<string>(manifestDependency.Name);
            if (!string.Equals(currentValue, manifestDependency.Version, StringComparison.Ordinal))
            {
                continue;
            }

            JProperty property = dependencies.Property(manifestDependency.Name);
            if (property != null)
            {
                property.Remove();
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            SaveManifestJson(manifestPath, manifest);
        }

        return removedCount;
    }

    #if false
    private int RestoreManifestDependencies(string sdkName)
    {
        if (!importedManifestDependencies.TryGetValue(sdkName, out var manifestDependencies) ||
            manifestDependencies == null ||
            manifestDependencies.Count == 0)
        {
            return RemoveManifestDependenciesByConfig(sdkName);
        }

        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning($"链壘鍒?manifest.json锛屾棤娉曞洖婊?SDK '{sdkName}' 鐨?Package 渚濊禆");
            return 0;
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JObject dependencies = manifest["dependencies"] as JObject;
        if (dependencies == null)
        {
            dependencies = new JObject();
            manifest["dependencies"] = dependencies;
        }

        int restoredCount = 0;
        foreach (var dependencyRecord in manifestDependencies)
        {
            if (dependencyRecord == null || string.IsNullOrWhiteSpace(dependencyRecord.Name))
            {
                continue;
            }

            if (dependencyRecord.HadPreviousValue)
            {
                dependencies[dependencyRecord.Name] = dependencyRecord.PreviousValue;
                restoredCount++;
            }
            else
            {
                JProperty property = dependencies.Property(dependencyRecord.Name);
                if (property != null)
                {
                    property.Remove();
                    restoredCount++;
                }
            }
        }

        SaveManifestJson(manifestPath, manifest);
        return restoredCount;
    }
    #endif

    private int RestoreManifestDependencies(string sdkName)
    {
        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning($"未找到 manifest.json，无法回滚 SDK '{sdkName}' 的 Packages/manifest.json 依赖。");
            return 0;
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JObject dependencies = manifest["dependencies"] as JObject;
        if (dependencies == null)
        {
            dependencies = new JObject();
            manifest["dependencies"] = dependencies;
        }

        var configuredDependencyMap = GetConfiguredManifestDependencies(sdkName)
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToDictionary(item => item.Name, item => item, StringComparer.Ordinal);

        var recordedDependencies = importedManifestDependencies.TryGetValue(sdkName, out var manifestDependencies) &&
                                   manifestDependencies != null
            ? manifestDependencies
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                .GroupBy(item => item.Name, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToDictionary(item => item.Name, item => item, StringComparer.Ordinal)
            : new Dictionary<string, ManifestDependencyRecord>(StringComparer.Ordinal);

        if (configuredDependencyMap.Count == 0 && recordedDependencies.Count == 0)
        {
            return 0;
        }

        int restoredCount = 0;
        var processedNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var configuredDependency in configuredDependencyMap)
        {
            processedNames.Add(configuredDependency.Key);

            if (recordedDependencies.TryGetValue(configuredDependency.Key, out var dependencyRecord))
            {
                restoredCount += RestoreManifestDependency(
                    dependencies,
                    dependencyRecord,
                    configuredDependency.Value.Version);
            }
            else
            {
                string currentValue = dependencies.Value<string>(configuredDependency.Key);
                if (!string.Equals(currentValue, configuredDependency.Value.Version, StringComparison.Ordinal))
                {
                    continue;
                }

                JProperty property = dependencies.Property(configuredDependency.Key);
                if (property == null)
                {
                    continue;
                }

                property.Remove();
                restoredCount++;
            }
        }

        foreach (var recordedDependency in recordedDependencies)
        {
            if (processedNames.Contains(recordedDependency.Key))
            {
                continue;
            }

            string fallbackImportedValue = configuredDependencyMap.TryGetValue(recordedDependency.Key, out var configuredDependency)
                ? configuredDependency.Version
                : null;
            restoredCount += RestoreManifestDependency(
                dependencies,
                recordedDependency.Value,
                fallbackImportedValue);
        }

        if (restoredCount > 0)
        {
            SaveManifestJson(manifestPath, manifest);
        }

        return restoredCount;
    }

    private bool AreManifestDependenciesImported(string sdkName)
    {
        if (!importedManifestDependencies.TryGetValue(sdkName, out var manifestDependencies) ||
            manifestDependencies == null ||
            manifestDependencies.Count == 0)
        {
            return true;
        }

        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            return false;
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JObject dependencies = manifest["dependencies"] as JObject;
        if (dependencies == null)
        {
            return false;
        }

        foreach (var dependencyRecord in manifestDependencies)
        {
            if (dependencyRecord == null || string.IsNullOrWhiteSpace(dependencyRecord.Name))
            {
                continue;
            }

            string currentValue = dependencies.Value<string>(dependencyRecord.Name);
            if (!string.Equals(currentValue, dependencyRecord.ImportedValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private List<ScopedRegistryRecord> ImportScopedRegistries(SDKConfig sdkConfig)
    {
        var scopedRegistries = sdkConfig.scopedRegistries ?? new List<ScopedRegistryConfig>();
        if (scopedRegistries.Count == 0)
        {
            return new List<ScopedRegistryRecord>();
        }

        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Missing Packages/manifest.json", manifestPath);
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JArray registryArray = manifest["scopedRegistries"] as JArray;
        if (registryArray == null)
        {
            registryArray = new JArray();
            manifest["scopedRegistries"] = registryArray;
        }

        var importedRegistryRecords = new List<ScopedRegistryRecord>();
        foreach (var scopedRegistry in scopedRegistries.Where(IsScopedRegistryConfigValid))
        {
            JObject registryObject = CreateScopedRegistryObject(scopedRegistry);
            int existingIndex = FindScopedRegistryIndex(registryArray, scopedRegistry.Name, scopedRegistry.Url);
            JToken existingValue = existingIndex >= 0 ? registryArray[existingIndex] : null;

            importedRegistryRecords.Add(new ScopedRegistryRecord
            {
                Name = scopedRegistry.Name,
                Url = scopedRegistry.Url,
                ImportedJson = registryObject.ToString(Formatting.None),
                HadPreviousValue = existingValue != null,
                PreviousJson = existingValue?.ToString(Formatting.None)
            });

            if (existingIndex >= 0)
            {
                registryArray[existingIndex] = registryObject;
            }
            else
            {
                registryArray.Add(registryObject);
            }
        }

        SaveManifestJson(manifestPath, manifest);
        return importedRegistryRecords;
    }

    private int RemoveScopedRegistriesByConfig(string sdkName)
    {
        var scopedRegistries = GetConfiguredScopedRegistries(sdkName);
        if (scopedRegistries.Count == 0)
        {
            return 0;
        }

        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            return 0;
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JArray registryArray = manifest["scopedRegistries"] as JArray;
        if (registryArray == null)
        {
            return 0;
        }

        int removedCount = 0;
        for (int i = registryArray.Count - 1; i >= 0; i--)
        {
            if (!(registryArray[i] is JObject registryObject))
            {
                continue;
            }

            if (!scopedRegistries.Any(scopedRegistry => AreScopedRegistriesEquivalent(registryObject, scopedRegistry)))
            {
                continue;
            }

            registryArray.RemoveAt(i);
            removedCount++;
        }

        if (removedCount > 0)
        {
            SaveManifestJson(manifestPath, manifest);
        }

        return removedCount;
    }

    private int RestoreScopedRegistry(JArray registryArray, ScopedRegistryRecord registryRecord, ScopedRegistryConfig fallbackScopedRegistry)
    {
        if (registryRecord == null && fallbackScopedRegistry == null)
        {
            return 0;
        }

        string name = !string.IsNullOrWhiteSpace(registryRecord?.Name)
            ? registryRecord.Name
            : fallbackScopedRegistry?.Name;
        string url = !string.IsNullOrWhiteSpace(registryRecord?.Url)
            ? registryRecord.Url
            : fallbackScopedRegistry?.Url;
        int existingIndex = FindScopedRegistryIndex(registryArray, name, url);

        if (registryRecord != null && registryRecord.HadPreviousValue)
        {
            JObject previousRegistryObject = null;
            try
            {
                previousRegistryObject = string.IsNullOrWhiteSpace(registryRecord.PreviousJson)
                    ? null
                    : JObject.Parse(registryRecord.PreviousJson);
            }
            catch
            {
                return 0;
            }

            if (previousRegistryObject == null)
            {
                return 0;
            }

            if (existingIndex >= 0)
            {
                if (JToken.DeepEquals(registryArray[existingIndex], previousRegistryObject))
                {
                    return 0;
                }

                registryArray[existingIndex] = previousRegistryObject;
            }
            else
            {
                registryArray.Add(previousRegistryObject);
            }

            return 1;
        }

        ScopedRegistryConfig importedScopedRegistry = fallbackScopedRegistry ?? ParseScopedRegistryJson(registryRecord?.ImportedJson);
        if (!IsScopedRegistryConfigValid(importedScopedRegistry) || existingIndex < 0)
        {
            return 0;
        }

        if (!(registryArray[existingIndex] is JObject currentRegistryObject) ||
            !AreScopedRegistriesEquivalent(currentRegistryObject, importedScopedRegistry))
        {
            return 0;
        }

        registryArray.RemoveAt(existingIndex);
        return 1;
    }

    private int RestoreScopedRegistries(string sdkName)
    {
        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning($"manifest.json not found; unable to restore scopedRegistries for SDK '{sdkName}'.");
            return 0;
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JArray registryArray = manifest["scopedRegistries"] as JArray;
        if (registryArray == null)
        {
            registryArray = new JArray();
            manifest["scopedRegistries"] = registryArray;
        }

        var configuredRegistryMap = GetConfiguredScopedRegistries(sdkName)
            .Where(IsScopedRegistryConfigValid)
            .GroupBy(item => GetScopedRegistryKey(item.Name, item.Url), StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToDictionary(item => GetScopedRegistryKey(item.Name, item.Url), item => item, StringComparer.Ordinal);

        var recordedRegistryMap = importedScopedRegistries.TryGetValue(sdkName, out var scopedRegistryRecords) &&
                                  scopedRegistryRecords != null
            ? scopedRegistryRecords
                .Where(item => item != null &&
                               (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.Url)))
                .GroupBy(item => GetScopedRegistryKey(item.Name, item.Url), StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToDictionary(item => GetScopedRegistryKey(item.Name, item.Url), item => item, StringComparer.Ordinal)
            : new Dictionary<string, ScopedRegistryRecord>(StringComparer.Ordinal);

        if (configuredRegistryMap.Count == 0 && recordedRegistryMap.Count == 0)
        {
            return 0;
        }

        int restoredCount = 0;
        var processedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var configuredRegistry in configuredRegistryMap)
        {
            processedKeys.Add(configuredRegistry.Key);

            if (recordedRegistryMap.TryGetValue(configuredRegistry.Key, out var registryRecord))
            {
                restoredCount += RestoreScopedRegistry(registryArray, registryRecord, configuredRegistry.Value);
            }
            else
            {
                int existingIndex = FindScopedRegistryIndex(
                    registryArray,
                    configuredRegistry.Value.Name,
                    configuredRegistry.Value.Url);
                if (existingIndex >= 0 &&
                    registryArray[existingIndex] is JObject currentRegistryObject &&
                    AreScopedRegistriesEquivalent(currentRegistryObject, configuredRegistry.Value))
                {
                    registryArray.RemoveAt(existingIndex);
                    restoredCount++;
                }
            }
        }

        foreach (var recordedRegistry in recordedRegistryMap)
        {
            if (processedKeys.Contains(recordedRegistry.Key))
            {
                continue;
            }

            ScopedRegistryConfig fallbackScopedRegistry = configuredRegistryMap.TryGetValue(recordedRegistry.Key, out var configuredRegistry)
                ? configuredRegistry
                : null;
            restoredCount += RestoreScopedRegistry(registryArray, recordedRegistry.Value, fallbackScopedRegistry);
        }

        if (restoredCount > 0)
        {
            SaveManifestJson(manifestPath, manifest);
        }

        return restoredCount;
    }

    private bool AreScopedRegistriesImported(string sdkName)
    {
        var configuredRegistryMap = GetConfiguredScopedRegistries(sdkName)
            .Where(IsScopedRegistryConfigValid)
            .GroupBy(item => GetScopedRegistryKey(item.Name, item.Url), StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToDictionary(item => GetScopedRegistryKey(item.Name, item.Url), item => item, StringComparer.Ordinal);

        var recordedRegistryMap = importedScopedRegistries.TryGetValue(sdkName, out var scopedRegistryRecords) &&
                                  scopedRegistryRecords != null
            ? scopedRegistryRecords
                .Where(item => item != null &&
                               (!string.IsNullOrWhiteSpace(item.Name) || !string.IsNullOrWhiteSpace(item.Url)))
                .GroupBy(item => GetScopedRegistryKey(item.Name, item.Url), StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToDictionary(item => GetScopedRegistryKey(item.Name, item.Url), item => item, StringComparer.Ordinal)
            : new Dictionary<string, ScopedRegistryRecord>(StringComparer.Ordinal);

        if (configuredRegistryMap.Count == 0 && recordedRegistryMap.Count == 0)
        {
            return true;
        }

        string manifestPath = GetProjectManifestPath();
        if (!File.Exists(manifestPath))
        {
            return false;
        }

        JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
        JArray registryArray = manifest["scopedRegistries"] as JArray;
        if (registryArray == null)
        {
            return false;
        }

        var processedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var configuredRegistry in configuredRegistryMap)
        {
            processedKeys.Add(configuredRegistry.Key);

            ScopedRegistryConfig expectedRegistry = configuredRegistry.Value;
            if (recordedRegistryMap.TryGetValue(configuredRegistry.Key, out var registryRecord))
            {
                expectedRegistry = ParseScopedRegistryJson(registryRecord.ImportedJson) ?? expectedRegistry;
            }

            int existingIndex = FindScopedRegistryIndex(
                registryArray,
                expectedRegistry.Name,
                expectedRegistry.Url);
            if (existingIndex < 0 ||
                !(registryArray[existingIndex] is JObject registryObject) ||
                !AreScopedRegistriesEquivalent(registryObject, expectedRegistry))
            {
                return false;
            }
        }

        foreach (var recordedRegistry in recordedRegistryMap)
        {
            if (processedKeys.Contains(recordedRegistry.Key))
            {
                continue;
            }

            ScopedRegistryConfig expectedRegistry = ParseScopedRegistryJson(recordedRegistry.Value.ImportedJson);
            if (!IsScopedRegistryConfigValid(expectedRegistry))
            {
                continue;
            }

            int existingIndex = FindScopedRegistryIndex(
                registryArray,
                expectedRegistry.Name,
                expectedRegistry.Url);
            if (existingIndex < 0 ||
                !(registryArray[existingIndex] is JObject registryObject) ||
                !AreScopedRegistriesEquivalent(registryObject, expectedRegistry))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasImportedContent(string sdkName)
    {
        int fileCount = importedFiles.TryGetValue(sdkName, out var paths) && paths != null ? paths.Count : 0;
        int dependencyCount = importedManifestDependencies.TryGetValue(sdkName, out var manifestDependencies) &&
                              manifestDependencies != null
            ? manifestDependencies.Count
            : 0;
        int scopedRegistryCount = importedScopedRegistries.TryGetValue(sdkName, out var scopedRegistries) &&
                                  scopedRegistries != null
            ? scopedRegistries.Count
            : 0;

        return fileCount > 0 || dependencyCount > 0 || scopedRegistryCount > 0;
    }

    private bool ShouldIgnorePath(string path, string basePath)
    {
        // 获取相对于基础路径的相对路径
        string relativePath = Path.GetRelativePath(basePath, path);
        string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // 检查路径的任何部分是否包含"Ignore"（不区分大小写）
        foreach (string part in pathParts)
        {
            if (part.Equals("Ignore", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // 获取文件列表，忽略包含"Ignore"文件夹的文件
    private List<string> GetFilesIgnoringIgnoreFolders(string directory, SearchOption searchOption = SearchOption.AllDirectories)
    {
        var allFiles = Directory.GetFiles(directory, "*", searchOption)
            .Where(f => !f.EndsWith("sdk_config.json"))
            .ToList();

        var filteredFiles = new List<string>();

        foreach (string file in allFiles)
        {
            if (!ShouldIgnorePath(file, directory))
            {
                filteredFiles.Add(file);
            }
        }

        Debug.Log($"在目录 {directory} 中找到 {allFiles.Count} 个文件，忽略Ignore文件夹后剩余 {filteredFiles.Count} 个文件");
        return filteredFiles;
    }

    // 导入 SDK 文件
    private void ImportSDK(SDKInfo sdk)
    {
        try
        {
            string sdkFolder = sdk.Directory;
            string sdkName = sdk.Name;

            // 检查是否已经有 SDK 被导入
            string currentlyImportedSDK = GetCurrentlyImportedSDK();
            if (!string.IsNullOrEmpty(currentlyImportedSDK) && currentlyImportedSDK != sdkName)
            {
                EditorUtility.DisplayDialog("无法导入",
                    $"在导入 '{sdk.Name}' 之前，必须删除当前已导入的 SDK '{currentlyImportedSDK}'。", "确定");
                return;
            }

            // 读取 SDK 配置
            var sdkConfig = LoadSDKConfigFromFile(sdk.ConfigJson);

            List<string> copiedFiles = new List<string>();
            List<ManifestDependencyRecord> importedDependencyRecords = new List<ManifestDependencyRecord>();
            List<ScopedRegistryRecord> importedScopedRegistryRecords = new List<ScopedRegistryRecord>();

            // 1. 复制 Assets 文件夹内容
            string assetsFolder = Path.Combine(sdkFolder, "Assets");
            if (Directory.Exists(assetsFolder))
            {
                // 使用新方法获取文件列表，忽略包含"Ignore"的文件夹
                var files = GetFilesIgnoringIgnoreFolders(assetsFolder, SearchOption.AllDirectories);

                Debug.Log($"开始复制 Assets 文件，共 {files.Count} 个文件");

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    string relativePath = Path.GetRelativePath(assetsFolder, file);
                    string targetPath = Path.Combine(Application.dataPath, relativePath);

                    string targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    File.Copy(file, targetPath, true);
                    copiedFiles.Add(relativePath);

                    // 每500个文件记录一次进度
                    if (i % 500 == 0 || i == files.Count - 1)
                    {
                        Debug.Log($"已复制 {i + 1}/{files.Count} 个 Assets 文件");
                    }
                }
            }

            // 2. 复制 Packages 文件夹中的本地 Package
            if (sdkConfig.packages != null && sdkConfig.packages.Count > 0)
            {
                Debug.Log($"开始复制 {sdkConfig.packages.Count} 个 Package");

                for (int p = 0; p < sdkConfig.packages.Count; p++)
                {
                    var packageName = sdkConfig.packages[p];

                    // SDK 目录中的 Package 路径
                    string sourcePackagePath = Path.Combine(sdkFolder, "Packages", packageName);
                    // Unity 项目中的目标 Package 路径
                    string targetPackagePath = Path.Combine(Application.dataPath, "..", "Packages", packageName);

                    if (Directory.Exists(sourcePackagePath))
                    {
                        // 复制整个 Package 文件夹
                        Directory.CreateDirectory(targetPackagePath);

                        // 使用新方法获取文件列表，忽略包含"Ignore"的文件夹
                        var packageFiles = GetFilesIgnoringIgnoreFolders(sourcePackagePath, SearchOption.AllDirectories);
                        Debug.Log($"Package '{packageName}' 有 {packageFiles.Count} 个文件（已忽略Ignore文件夹）");

                        for (int i = 0; i < packageFiles.Count; i++)
                        {
                            var file = packageFiles[i];
                            string relativePath = Path.GetRelativePath(sourcePackagePath, file);
                            string targetPath = Path.Combine(targetPackagePath, relativePath);

                            string targetDir = Path.GetDirectoryName(targetPath);
                            if (!Directory.Exists(targetDir))
                            {
                                Directory.CreateDirectory(targetDir);
                            }

                            File.Copy(file, targetPath, true);
                            // 记录相对路径（相对于项目根目录）
                            string recordedPath = Path.Combine("Packages", packageName, relativePath).Replace("\\", "/");
                            copiedFiles.Add(recordedPath);

                            if (i % 500 == 0 || i == packageFiles.Count - 1)
                            {
                                Debug.Log($"Package '{packageName}': 已复制 {i + 1}/{packageFiles.Count} 个文件");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"在 SDK '{sdkName}' 中未找到 Package 文件夹: {sourcePackagePath}");
                    }
                }
            }

            // 更新导入记录
            ImportProjectRootPaths(sdkFolder, sdkConfig, copiedFiles, sdkName);

            if (sdkConfig.manifestDependencies != null && sdkConfig.manifestDependencies.Count > 0)
            {
                importedDependencyRecords = ImportManifestDependencies(sdkConfig);
            }

            if (sdkConfig.scopedRegistries != null && sdkConfig.scopedRegistries.Count > 0)
            {
                importedScopedRegistryRecords = ImportScopedRegistries(sdkConfig);
            }

            importedFiles[sdkName] = copiedFiles;
            importedManifestDependencies[sdkName] = importedDependencyRecords;
            importedScopedRegistries[sdkName] = importedScopedRegistryRecords;
            SaveImportedFiles();

            // 添加宏定义
            AddMacroDefinitions(sdk);

            // 刷新 Asset Database
            AssetDatabase.Refresh();

            // 自动生成 SVN 忽略文件
            try
            {
                GenerateSVNIgnoreFile();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"生成 SVN 忽略文件失败: {e.Message}");
            }

            EditorUtility.DisplayDialog("导入成功",
                $"SDK '{sdkName}' 已成功导入。已复制 {copiedFiles.Count} 个文件。\n" +
                $"已添加宏定义: {(sdk.Macros != null && sdk.Macros.Count > 0 ? string.Join(", ", sdk.Macros) : "无")}",
                "确定");

            Debug.Log($"SDK '{sdkName}' 导入成功。已复制 {copiedFiles.Count} 个文件。");
        }
        catch (Exception e)
        {
            Debug.LogError($"导入 SDK '{sdk.Name}' 失败: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("导入失败",
                $"导入 SDK '{sdk.Name}' 失败: {e.Message}", "确定");
        }
    }

    // 删除 SDK 文件 - 完全修复版
    private void DeleteSDK(SDKInfo sdk)
    {
        try
        {
            string sdkName = sdk.Name;

            if (!HasImportedContent(sdkName))
            {
                Debug.LogWarning($"未找到 SDK '{sdkName}' 的导入记录");
                return;
            }

            var filesToDelete = importedFiles.TryGetValue(sdkName, out var paths) && paths != null
                ? paths
                : new List<string>();
            Debug.Log($"开始删除 SDK '{sdkName}'，记录的文件数: {filesToDelete.Count}");

            int deletedCount = 0;
            int skippedCount = 0;
            List<string> failedDeletes = new List<string>();
            int deletedEmptyDirectoryCount = 0;

            // 批量删除，提高效率
            for (int i = 0; i < filesToDelete.Count; i++)
            {
                string relativePath = filesToDelete[i];

                try
                {
                    // 获取目标文件完整路径
                    string targetPath = GetTargetFilePath(relativePath);
                    bool shouldCleanupDirectories = false;

                    if (File.Exists(targetPath))
                    {
                        try
                        {
                            File.Delete(targetPath);
                            deletedCount++;
                            shouldCleanupDirectories = true;

                            // 删除对应的 .meta 文件
                            string metaPath = targetPath + ".meta";
                            if (File.Exists(metaPath))
                            {
                                File.Delete(metaPath);
                                deletedCount++;
                            }
                        }
                        catch (Exception e)
                        {
                            failedDeletes.Add($"{relativePath}: {e.Message}");
                        }
                    }
                    else
                    {
                        skippedCount++;
                        shouldCleanupDirectories = true;
                    }

                    if (shouldCleanupDirectories)
                    {
                        deletedEmptyDirectoryCount += DeleteEmptyParentDirectories(targetPath, failedDeletes);
                    }

                    // 每删除100个文件显示一次进度
                    if (i % 100 == 0 && i > 0)
                    {
                        Debug.Log($"删除进度: {i + 1}/{filesToDelete.Count} (已删除: {deletedCount}, 跳过: {skippedCount})");
                    }
                }
                catch (Exception e)
                {
                    failedDeletes.Add($"{relativePath}: {e.Message}");
                }
            }

            Debug.Log($"删除完成: 已删除 {deletedCount} 个文件，跳过 {skippedCount} 个文件");

            // 从记录中移除
            Debug.Log($"Removed empty directories: {deletedEmptyDirectoryCount}");
            int restoredManifestCount = RestoreManifestDependencies(sdkName);
            int restoredScopedRegistryCount = RestoreScopedRegistries(sdkName);

            importedFiles.Remove(sdkName);
            importedManifestDependencies.Remove(sdkName);
            importedScopedRegistries.Remove(sdkName);
            SaveImportedFiles();

            // 删除宏定义
            RemoveMacroDefinitions(sdk);

            // 刷新 Asset Database
            AssetDatabase.Refresh();

            // 检查是否还有已导入的 SDK 文件
            bool hasImportedFiles = importedFiles.Keys
                .Union(importedManifestDependencies.Keys)
                .Union(importedScopedRegistries.Keys)
                .Distinct()
                .Any(HasImportedContent);

            if (!hasImportedFiles)
            {
                // 如果没有已导入的 SDK 文件了，删除 .svnignore 文件
                string svnIgnorePath = Path.Combine(Application.dataPath, "..", ".svnignore");
                if (File.Exists(svnIgnorePath))
                {
                    File.Delete(svnIgnorePath);
                    AssetDatabase.Refresh();
                    Debug.Log($"已删除 .svnignore 文件，因为没有已导入的 SDK 文件了");
                }
            }

            // 显示删除结果
            string resultMessage = $"SDK '{sdkName}' 删除完成。\n";
            resultMessage += $"已删除: {deletedCount} 个文件\n";
            resultMessage += $"跳过: {skippedCount} 个文件\n";
            resultMessage += $"已移除宏定义: {(sdk.Macros != null && sdk.Macros.Count > 0 ? string.Join(", ", sdk.Macros) : "无")}\n";

            if (failedDeletes.Count > 0)
            {
                resultMessage += $"失败: {failedDeletes.Count} 个文件\n\n";
                resultMessage += "前10个失败的文件:\n" + string.Join("\n", failedDeletes.Take(10));

                EditorUtility.DisplayDialog("删除完成（有错误）", resultMessage, "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("删除成功", resultMessage, "确定");
            }

            Debug.Log($"SDK '{sdkName}' 删除完成。删除: {deletedCount}, 跳过: {skippedCount}, 失败: {failedDeletes.Count}。");
        }
        catch (Exception e)
        {
            Debug.LogError($"删除 SDK '{sdk.Name}' 失败: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("删除失败",
                $"删除 SDK '{sdk.Name}' 失败: {e.Message}", "确定");
        }
    }

    // 用于存储 SDK 路径配置
    [Serializable]
    public class SDKPathConfig
    {
        public string SdkPath;
    }

    // 用于存储每个 SDK 的导入记录
    [Serializable]
    public class SDKImportRecord
    {
        public string SdkName;
        public List<string> Paths;
        public List<ManifestDependencyRecord> ManifestDependencies;
        public List<ScopedRegistryRecord> ScopedRegistries;
    }

    // 用于存储所有 SDK 的导入记录
    [Serializable]
    public class SDKImportRecords
    {
        public List<SDKImportRecord> Records;
    }

    // 用于存储每个 SDK 的信息
    [Serializable]
    public class SDKInfo
    {
        public string Name;
        public string DisplayName;
        public string ConfigJson;
        public string Reference;  // 参考文档 URL
        public string Directory;  // SDK 目录路径
        public List<string> Macros; // SDK 的宏定义列表
    }

    // SDK 配置文件中的内容
    [Serializable]
    public class ManifestPackageConfig
    {
        public string Name;
        public string Version;
    }

    [Serializable]
    public class ManifestDependencyRecord
    {
        public string Name;
        public string ImportedValue;
        public string PreviousValue;
        public bool HadPreviousValue;
    }

    [Serializable]
    public class ScopedRegistryConfig
    {
        public string Name;
        public string Url;
        public List<string> Scopes;
    }

    [Serializable]
    public class ScopedRegistryRecord
    {
        public string Name;
        public string Url;
        public string ImportedJson;
        public string PreviousJson;
        public bool HadPreviousValue;
    }

    [Serializable]
    public class SDKConfig
    {
        public List<ManifestPackageConfig> manifestDependencies;
        public List<ScopedRegistryConfig> scopedRegistries;
        public List<string> projectRootPaths;
        public string name;  // SDK 显示名称
        public string reference;  // 参考文档 URL
        public List<string> packages; // 需要复制的本地 Package 名称列表
        public List<string> macros; // SDK 的宏定义列表
    }
    }
}
#endif
#endif
