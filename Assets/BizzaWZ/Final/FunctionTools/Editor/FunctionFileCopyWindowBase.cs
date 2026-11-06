#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BizzaWZ.FunctionTools
{
    /// <summary>
    /// SDK、IP 等功能导出的公共窗口逻辑。
    /// 具体功能只维护自己的 JSON 清单，复制、清空、备份和路径保护统一在这里处理。
    /// </summary>
    public abstract class FunctionFileCopyWindowBase : EditorWindow
    {
        private readonly List<CopyItem> _items = new List<CopyItem>();

        private string _destinationRoot = string.Empty;
        private bool _includeMeta = true;
        private bool _backupExisting = true;
        private Vector2 _scrollPosition;
        private string _lastMessage = string.Empty;
        private bool _lastMessageIsError;
        private string _configurationMessage = string.Empty;

        protected abstract string ToolName { get; }
        protected abstract string PreferenceKeyPrefix { get; }
        protected abstract IEnumerable<CopyItem> CreateCopyItems();

        /// <summary>
        /// 是否允许当前功能在清空目标前创建备份。
        /// 功能复制工具默认用于同步最新文件，因此统一关闭备份。
        /// 如未来确有特殊工具需要备份，可由该工具显式开启。
        /// </summary>
        protected virtual bool SupportsBackup => false;

        /// <summary>
        /// 当前窗口配置的导出目标，供具体功能在加载清单时做模式判断。
        /// </summary>
        protected string ConfiguredDestinationRoot => TryGetFullPath(_destinationRoot);

        private string DestinationPrefKey => PreferenceKeyPrefix + ".Destination";
        private string IncludeMetaPrefKey => PreferenceKeyPrefix + ".IncludeMeta";
        private string BackupPrefKey => PreferenceKeyPrefix + ".BackupExisting";

        protected static void OpenWindow<T>(string title, float minWidth = 800f, float minHeight = 700f)
            where T : FunctionFileCopyWindowBase
        {
            T window = GetWindow<T>();
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(minWidth, minHeight);

            Rect position = window.position;
            position.width = Mathf.Max(position.width, window.minSize.x);
            position.height = Mathf.Max(position.height, window.minSize.y);
            window.position = position;
            window.Show();
        }

        protected virtual void OnEnable()
        {
            _destinationRoot = EditorPrefs.GetString(DestinationPrefKey, string.Empty);
            _includeMeta = EditorPrefs.GetBool(IncludeMetaPrefKey, true);
            _backupExisting = EditorPrefs.GetBool(BackupPrefKey, true);
            ReloadItems();
        }

        protected virtual void OnDisable()
        {
            SavePreferences();
        }

        protected virtual void OnGUI()
        {
            EditorGUILayout.LabelField(ToolName + " 文件复制工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                SupportsBackup
                    ? "导出前会清空目标目录（保留 .git* 内容），再按功能清单复制完整内容；可选在清空前备份。Unity 项目中的源文件始终保留。"
                    : "导出前会清空目标目录（保留 .git* 内容），再按功能清单复制完整内容；不会创建 FunctionCopyBackup。Unity 项目中的源文件始终保留。",
                MessageType.Info);

            DrawDestinationField();
            DrawOptions();
            DrawSourceList();

            MigrationPlan plan = BuildPlan();
            DrawPlanSummary(plan);
            DrawActionButtons(plan);
        }

        /// <summary>
        /// 从工程中的 JSON 清单加载功能文件。清单中的 sourceRoot 为相对项目根目录的 Assets 路径。
        /// </summary>
        protected IEnumerable<CopyItem> LoadManifestItems(string manifestFileName)
        {
            _configurationMessage = string.Empty;
            List<CopyItem> result = new List<CopyItem>();
            HashSet<string> loadedManifests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> loadingManifests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!LoadManifestItemsRecursive(manifestFileName, result, loadedManifests, loadingManifests))
            {
                return new CopyItem[0];
            }

            return result;
        }

        private bool LoadManifestItemsRecursive(
            string manifestFileName,
            List<CopyItem> result,
            HashSet<string> loadedManifests,
            HashSet<string> loadingManifests)
        {
            string manifestKey = Path.GetFileNameWithoutExtension(manifestFileName);
            if (loadedManifests.Contains(manifestKey))
            {
                return true;
            }

            if (!loadingManifests.Add(manifestKey))
            {
                _configurationMessage = "复制清单存在循环引用：" + manifestKey;
                return false;
            }

            string assetPath = FindManifestAssetPath(manifestKey);
            if (string.IsNullOrEmpty(assetPath))
            {
                _configurationMessage = "未找到复制清单：" + manifestKey + ".json";
                loadingManifests.Remove(manifestKey);
                return false;
            }

            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (asset == null)
            {
                _configurationMessage = "无法读取复制清单：" + assetPath;
                loadingManifests.Remove(manifestKey);
                return false;
            }

            try
            {
                FunctionCopyManifest manifest = JsonUtility.FromJson<FunctionCopyManifest>(asset.text);
                if (manifest == null || manifest.items == null || manifest.items.Length == 0)
                {
                    _configurationMessage = "复制清单为空：" + assetPath;
                    loadingManifests.Remove(manifestKey);
                    return false;
                }

                if (manifest.includes != null)
                {
                    for (int includeIndex = 0; includeIndex < manifest.includes.Length; includeIndex++)
                    {
                        string include = manifest.includes[includeIndex];
                        if (string.IsNullOrWhiteSpace(include))
                        {
                            continue;
                        }

                        if (!LoadManifestItemsRecursive(include, result, loadedManifests, loadingManifests))
                        {
                            loadingManifests.Remove(manifestKey);
                            return false;
                        }
                    }
                }

                string sourceRoot = NormalizeAssetPath(manifest.sourceRoot);
                for (int itemIndex = 0; itemIndex < manifest.items.Length; itemIndex++)
                {
                    FunctionCopyManifestItem item = manifest.items[itemIndex];
                    if (item == null || string.IsNullOrWhiteSpace(item.source))
                    {
                        continue;
                    }

                    string source = CombineAssetPath(sourceRoot, item.source);
                    string displayName = string.IsNullOrWhiteSpace(item.name)
                        ? item.destination
                        : item.name;
                    if (string.IsNullOrWhiteSpace(displayName))
                    {
                        displayName = item.source;
                    }

                    result.Add(new CopyItem(
                        displayName,
                        source,
                        item.isDirectory,
                        NormalizeAssetPath(item.destination),
                        item.required,
                        string.IsNullOrWhiteSpace(item.category) ? "功能代码" : item.category,
                        item.excludes,
                        item.shared,
                        item.standaloneOnly,
                        item.allowParentDestination));
                }

                loadingManifests.Remove(manifestKey);
                loadedManifests.Add(manifestKey);
                return true;
            }
            catch (Exception exception)
            {
                _configurationMessage = "复制清单解析失败：" + assetPath + "\n" + exception.Message;
                loadingManifests.Remove(manifestKey);
                return false;
            }
        }

        private static string FindManifestAssetPath(string manifestFileName)
        {
            string[] guids = AssetDatabase.FindAssets(manifestFileName + " t:TextAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(assetPath),
                        manifestFileName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return assetPath;
                }
            }

            return string.Empty;
        }

        private void ReloadItems()
        {
            _items.Clear();
            IEnumerable<CopyItem> configuredItems = CreateCopyItems();
            if (configuredItems == null)
            {
                return;
            }

            foreach (CopyItem item in configuredItems)
            {
                if (item != null)
                {
                    _items.Add(item);
                }
            }
        }

        private void DrawDestinationField()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("目标目录");
            _destinationRoot = EditorGUILayout.TextField(_destinationRoot);

            if (GUILayout.Button("选择...", GUILayout.Width(70f)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel(
                    "选择" + ToolName + "导出目标目录",
                    GetUsableDestinationPath(),
                    string.Empty);
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    _destinationRoot = selectedPath;
                    SavePreferences();
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();

            string fullPath = TryGetFullPath(_destinationRoot);
            if (!string.IsNullOrEmpty(fullPath))
            {
                EditorGUILayout.LabelField("解析路径", fullPath, EditorStyles.miniLabel);
            }
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginHorizontal();
            bool includeMeta = EditorGUILayout.ToggleLeft("包含 Unity .meta 文件", _includeMeta);
            bool backupExisting = SupportsBackup
                && EditorGUILayout.ToggleLeft("清空前备份", _backupExisting);
            EditorGUILayout.EndHorizontal();

            if (includeMeta != _includeMeta || backupExisting != _backupExisting)
            {
                _includeMeta = includeMeta;
                _backupExisting = backupExisting;
                SavePreferences();
            }

            EditorGUILayout.HelpBox(
                SupportsBackup
                    ? "推荐保留 .meta 并开启备份。每次导出都会先清空目标目录，避免旧版本残留文件。"
                    : "每次导出都会直接清空目标目录，再复制当前清单内容；不会创建 FunctionCopyBackup。",
                MessageType.Warning);
        }

        private void DrawSourceList()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("复制内容", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(_configurationMessage))
            {
                EditorGUILayout.HelpBox(_configurationMessage, MessageType.Error);
            }

            if (_items.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "当前没有可导出的内容。请检查对应功能的 JSON 清单。",
                    MessageType.Error);
                return;
            }

            // 复制内容区域随窗口高度变化，同时为底部预览和执行按钮预留空间。
            float sourceListHeight = Mathf.Clamp(position.height - 390f, 120f, 680f);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(sourceListHeight));
            string destinationRoot = TryGetFullPath(_destinationRoot);
            for (int i = 0; i < _items.Count; i++)
            {
                CopyItem item = _items[i];
                string sourcePath = GetProjectPath(item.SourceRelativePath);
                bool exists = item.IsDirectory
                    ? Directory.Exists(sourcePath)
                    : File.Exists(sourcePath);
                bool coveredByParent = IsCoveredBySelectedParent(i, destinationRoot);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.BeginDisabledGroup(item.Required || coveredByParent);
                string selectionLabel = coveredByParent
                    ? "[父项已包含] " + item.DisplayName
                    : (item.Required ? "[必需] " : "[可选] ") + item.DisplayName;
                item.Selected = EditorGUILayout.ToggleLeft(
                    selectionLabel,
                    item.Selected);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("分类", item.Category, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("源路径", item.SourceRelativePath, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("目标路径", string.IsNullOrEmpty(item.DestinationRelativePath) ? "." : item.DestinationRelativePath, EditorStyles.miniLabel);
                string status = exists ? "已找到" : "路径不存在";
                if (coveredByParent)
                {
                    status += "；已由选中的父项包含，不会重复复制";
                }

                EditorGUILayout.LabelField("状态", status, EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选可选项"))
            {
                SetAllItemsSelected(true);
            }

            if (GUILayout.Button("全不选可选项"))
            {
                SetAllItemsSelected(false);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlanSummary(MigrationPlan plan)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("导出预览", EditorStyles.boldLabel);

            if (plan.Errors.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", plan.Errors.ToArray()), MessageType.Error);
            }

            if (plan.SelectedSourceCount > 0)
            {
                EditorGUILayout.LabelField(
                    "将处理",
                    string.Format(
                        "{0} 个源项，{1} 个文件，{2}；冲突 {3} 个",
                        plan.SelectedSourceCount,
                        plan.TransferCount,
                        FormatBytes(plan.TotalBytes),
                        plan.ConflictCount));
            }
            else if (_items.Count > 0)
            {
                EditorGUILayout.HelpBox("请至少选择一个可选复制项；必需项会自动包含。", MessageType.Warning);
            }
        }

        private void DrawActionButtons(MigrationPlan plan)
        {
            EditorGUILayout.Space(7f);
            EditorGUI.BeginDisabledGroup(
                _items.Count == 0
                || plan.SelectedSourceCount == 0
                || plan.TransferCount == 0
                || plan.Errors.Count > 0);

            if (GUILayout.Button("清空目标并开始复制", GUILayout.Height(34f)))
            {
                ExecutePlan(plan);
            }

            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(_lastMessage))
            {
                EditorGUILayout.HelpBox(_lastMessage, _lastMessageIsError ? MessageType.Error : MessageType.Info);
            }
        }

        private MigrationPlan BuildPlan()
        {
            MigrationPlan plan = new MigrationPlan();
            string destinationRoot = TryGetFullPath(_destinationRoot);
            if (string.IsNullOrEmpty(destinationRoot))
            {
                if (!string.IsNullOrWhiteSpace(_destinationRoot))
                {
                    plan.Errors.Add("目标目录路径无效。");
                }

                return plan;
            }

            if (IsSameOrChildPath(destinationRoot, ProjectRoot)
                || IsSameOrChildPath(ProjectRoot, destinationRoot))
            {
                plan.Errors.Add("目标目录必须位于 Unity 项目目录之外，不能覆盖项目目录或其父目录。");
                return plan;
            }

            plan.DestinationRoot = destinationRoot;
            HashSet<string> destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _items.Count; i++)
            {
                CopyItem item = _items[i];
                if (item.Required)
                {
                    item.Selected = true;
                }

                if (!item.Selected)
                {
                    continue;
                }

                // 父目录已经覆盖该源项时，只处理父目录，避免同一文件被再次规划。
                // 如果父目录通过 excludes 排除了子项，IsCoveredBySelectedParent 会返回 false，
                // 子项仍会按独立清单项继续处理。
                if (IsCoveredBySelectedParent(i, destinationRoot))
                {
                    continue;
                }

                plan.SelectedSourceCount++;
                string sourcePath = GetProjectPath(item.SourceRelativePath);
                bool sourceExists = item.IsDirectory
                    ? Directory.Exists(sourcePath)
                    : File.Exists(sourcePath);
                if (!sourceExists)
                {
                    plan.Errors.Add((item.Required ? "缺少必需源路径：" : "找不到源路径：") + item.SourceRelativePath);
                    continue;
                }

                string destinationPath = GetDestinationPath(destinationRoot, item.DestinationRelativePath, item.Shared);
                string allowedRoot = item.Shared
                    ? GetSharedRoot(destinationRoot)
                    : item.AllowParentDestination
                        ? GetParentDirectory(destinationRoot)
                        : destinationRoot;
                if (!IsSameOrChildPath(destinationPath, allowedRoot))
                {
                    plan.Errors.Add("目标路径越界：" + item.DestinationRelativePath);
                    continue;
                }

                try
                {
                    if (item.IsDirectory)
                    {
                        string[] files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                        for (int fileIndex = 0; fileIndex < files.Length; fileIndex++)
                        {
                            if (!_includeMeta && IsMetaFile(files[fileIndex]))
                            {
                                continue;
                            }

                            string relativePath = GetRelativePath(sourcePath, files[fileIndex]);
                            if (IsExcluded(relativePath, item.Excludes))
                            {
                                continue;
                            }

                            AddTransfer(plan, destinations, files[fileIndex], Path.Combine(destinationPath, relativePath));
                        }

                        string sourceDirectoryMeta = sourcePath + ".meta";
                        if (_includeMeta && File.Exists(sourceDirectoryMeta))
                        {
                            AddTransfer(plan, destinations, sourceDirectoryMeta, destinationPath + ".meta");
                        }
                    }
                    else
                    {
                        AddTransfer(plan, destinations, sourcePath, destinationPath);
                        string sourceFileMeta = sourcePath + ".meta";
                        if (_includeMeta && File.Exists(sourceFileMeta))
                        {
                            AddTransfer(plan, destinations, sourceFileMeta, destinationPath + ".meta");
                        }
                    }
                }
                catch (Exception exception)
                {
                    plan.Errors.Add("读取源路径失败：" + item.SourceRelativePath + "\n" + exception.Message);
                }
            }

            return plan;
        }

        /// <summary>
        /// 判断当前源项是否已经被一个已选中的父目录完整覆盖。
        /// 同时校验目标路径层级和父项 excludes，避免误跳过 SDK/IP 的拆分公共依赖。
        /// </summary>
        private bool IsCoveredBySelectedParent(int childIndex, string destinationRoot)
        {
            if (childIndex < 0
                || childIndex >= _items.Count
                || string.IsNullOrEmpty(destinationRoot))
            {
                return false;
            }

            CopyItem child = _items[childIndex];
            if (!child.Selected && !child.Required)
            {
                return false;
            }

            string childSourcePath = GetProjectPath(child.SourceRelativePath);
            bool childExists = child.IsDirectory
                ? Directory.Exists(childSourcePath)
                : File.Exists(childSourcePath);
            if (!childExists)
            {
                return false;
            }

            string childDestinationPath = GetDestinationPath(
                destinationRoot,
                child.DestinationRelativePath,
                child.Shared);

            for (int parentIndex = 0; parentIndex < _items.Count; parentIndex++)
            {
                if (parentIndex == childIndex)
                {
                    continue;
                }

                CopyItem parent = _items[parentIndex];
                if (!parent.Selected || !parent.IsDirectory)
                {
                    continue;
                }

                string parentSourcePath = GetProjectPath(parent.SourceRelativePath);
                if (!Directory.Exists(parentSourcePath))
                {
                    continue;
                }

                // 目录项除了递归复制目录内文件，还会自动复制“目录名.meta”。
                // 该旁置 meta 不在目录路径内部，也必须视为已被父项覆盖。
                bool isDirectoryMeta = !child.IsDirectory
                    && string.Equals(
                        childSourcePath,
                        parentSourcePath + ".meta",
                        StringComparison.OrdinalIgnoreCase);
                if (!isDirectoryMeta
                    && !IsSameOrChildPath(childSourcePath, parentSourcePath))
                {
                    continue;
                }

                string relativeChildSourcePath = isDirectoryMeta
                    ? string.Empty
                    : GetRelativePath(parentSourcePath, childSourcePath);
                if (IsExcluded(relativeChildSourcePath, parent.Excludes))
                {
                    continue;
                }

                string parentDestinationPath = GetDestinationPath(
                    destinationRoot,
                    parent.DestinationRelativePath,
                    parent.Shared);
                bool isDirectoryMetaDestination = !child.IsDirectory
                    && string.Equals(
                        childDestinationPath,
                        parentDestinationPath + ".meta",
                        StringComparison.OrdinalIgnoreCase);
                if (isDirectoryMetaDestination
                    || IsSameOrChildPath(childDestinationPath, parentDestinationPath))
                {
                    return true;
                }
            }

            return false;
        }

        private void ExecutePlan(MigrationPlan plan)
        {
            string destinationRoot = TryGetFullPath(_destinationRoot);
            if (string.IsNullOrEmpty(destinationRoot))
            {
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "确认导出",
                string.Format(
                    "将先清空目标目录（保留 .git* 内容），再复制 {0} 个文件到：\n{1}\n\n目标已有文件：{2} 个。{3}",
                    plan.TransferCount,
                    destinationRoot,
                    plan.ConflictCount,
                    (SupportsBackup && _backupExisting)
                        ? "清空前会生成备份。"
                        : "不会生成备份。"),
                "导出",
                "取消");
            if (!confirmed)
            {
                return;
            }

            bool backupEnabled = SupportsBackup && _backupExisting;
            string backupRoot = backupEnabled
                ? Path.Combine(
                    Path.GetDirectoryName(destinationRoot),
                    Path.GetFileName(destinationRoot) + ".FunctionCopyBackup",
                    PreferenceKeyPrefix,
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                : string.Empty;
            int copied = 0;
            int backedUp = 0;

            try
            {
                Directory.CreateDirectory(destinationRoot);
                if (backupEnabled)
                {
                    backedUp = BackupDestinationDirectory(destinationRoot, backupRoot);
                }

                ClearDestinationDirectory(destinationRoot);
                for (int i = 0; i < plan.Transfers.Count; i++)
                {
                    FileTransfer transfer = plan.Transfers[i];
                    string parent = Path.GetDirectoryName(transfer.DestinationPath);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }

                    File.Copy(transfer.SourcePath, transfer.DestinationPath, true);
                    copied++;
                    EditorUtility.DisplayProgressBar(
                        ToolName + " 导出",
                        string.Format("{0}/{1}：{2}", copied, plan.TransferCount, transfer.RelativeDestinationPath),
                        (float)copied / plan.TransferCount);
                }

                _lastMessage = string.Format(
                    "导出完成：{0} 个文件；备份：{1} 个；目标：{2}",
                    copied,
                    backedUp,
                    destinationRoot);
                _lastMessageIsError = false;
                Debug.Log("[" + ToolName + "导出] " + _lastMessage);
                EditorUtility.RevealInFinder(destinationRoot);
            }
            catch (Exception exception)
            {
                _lastMessage = string.Format(
                    "导出失败，已完成 {0}/{1} 个文件。\n{2}",
                    copied,
                    plan.TransferCount,
                    exception.Message);
                _lastMessageIsError = true;
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private static int BackupDestinationDirectory(string destinationRoot, string backupRoot)
        {
            if (!Directory.Exists(destinationRoot))
            {
                return 0;
            }

            int backedUp = 0;
            BackupDestinationDirectoryRecursive(destinationRoot, destinationRoot, backupRoot, ref backedUp);
            return backedUp;
        }

        private static void BackupDestinationDirectoryRecursive(
            string currentDirectory,
            string destinationRoot,
            string backupRoot,
            ref int backedUp)
        {
            string[] entries = Directory.GetFileSystemEntries(currentDirectory);
            for (int i = 0; i < entries.Length; i++)
            {
                string relativePath = GetRelativePath(destinationRoot, entries[i]);
                if (IsGitRelatedPath(relativePath))
                {
                    continue;
                }

                if (Directory.Exists(entries[i]))
                {
                    BackupDestinationDirectoryRecursive(entries[i], destinationRoot, backupRoot, ref backedUp);
                    continue;
                }

                if (!File.Exists(entries[i]))
                {
                    continue;
                }

                string backupPath = Path.Combine(
                    backupRoot,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                string parent = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                File.Copy(entries[i], backupPath, true);
                backedUp++;
            }
        }

        private static void ClearDestinationDirectory(string destinationRoot)
        {
            if (!Directory.Exists(destinationRoot))
            {
                return;
            }

            ClearDestinationDirectoryRecursive(destinationRoot, destinationRoot);
        }

        private static void ClearDestinationDirectoryRecursive(string currentDirectory, string destinationRoot)
        {
            string[] entries = Directory.GetFileSystemEntries(currentDirectory);
            for (int i = 0; i < entries.Length; i++)
            {
                string relativePath = GetRelativePath(destinationRoot, entries[i]);
                if (IsGitRelatedPath(relativePath))
                {
                    continue;
                }

                if (Directory.Exists(entries[i]))
                {
                    ClearDestinationDirectoryRecursive(entries[i], destinationRoot);
                    if (Directory.GetFileSystemEntries(entries[i]).Length == 0)
                    {
                        Directory.Delete(entries[i], false);
                    }
                }
                else if (File.Exists(entries[i]))
                {
                    File.Delete(entries[i]);
                }
            }
        }

        private static void AddTransfer(
            MigrationPlan plan,
            HashSet<string> destinations,
            string sourcePath,
            string destinationPath)
        {
            string normalizedDestination = Path.GetFullPath(destinationPath);
            string relativeDestination = GetRelativePath(plan.DestinationRoot, normalizedDestination);
            if (IsGitRelatedPath(relativeDestination))
            {
                return;
            }

            if (!destinations.Add(normalizedDestination))
            {
                plan.Errors.Add("多个复制项产生了相同目标文件：" + normalizedDestination);
                return;
            }

            plan.Transfers.Add(new FileTransfer(
                sourcePath,
                normalizedDestination,
                relativeDestination));
            plan.TotalBytes += new FileInfo(sourcePath).Length;
            if (File.Exists(normalizedDestination))
            {
                plan.ConflictCount++;
            }
        }

        private void SetAllItemsSelected(bool selected)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (!_items[i].Required)
                {
                    _items[i].Selected = selected;
                }
            }
        }

        private void SavePreferences()
        {
            EditorPrefs.SetString(DestinationPrefKey, _destinationRoot ?? string.Empty);
            EditorPrefs.SetBool(IncludeMetaPrefKey, _includeMeta);
            EditorPrefs.SetBool(BackupPrefKey, _backupExisting);
        }

        private string GetUsableDestinationPath()
        {
            string path = TryGetFullPath(_destinationRoot);
            return string.IsNullOrEmpty(path) ? ProjectRoot : path;
        }

        private static string GetProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string GetParentDirectory(string path)
        {
            string normalizedPath = NormalizePath(path);
            string parent = Path.GetDirectoryName(normalizedPath);
            return string.IsNullOrEmpty(parent) ? normalizedPath : NormalizePath(parent);
        }

        private static string GetDestinationPath(string destinationRoot, string relativePath, bool shared)
        {
            string normalized = string.IsNullOrWhiteSpace(relativePath) || relativePath == "."
                ? string.Empty
                : relativePath.Replace('/', Path.DirectorySeparatorChar);
            const string sharedPrefix = "@SHARED";
            if (shared && (string.Equals(normalized, sharedPrefix, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(sharedPrefix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                string sharedRelative = normalized.Length == sharedPrefix.Length
                    ? string.Empty
                    : normalized.Substring(sharedPrefix.Length + 1);
                return Path.GetFullPath(Path.Combine(GetSharedRoot(destinationRoot), sharedRelative));
            }

            return Path.GetFullPath(Path.Combine(destinationRoot, normalized));
        }

        /// <summary>
        /// 公共清单的共享根目录：
        /// - 导出到已有 Unity 项目根（存在 Assets 子目录）时，公共文件就在该项目根；
        /// - 导出到 Assets 下的模块目录时，公共文件统一放到 Assets；
        /// - 导出到项目外的模块目录时，公共文件跟随当前目标目录，保证一个工具的内容完整收敛在一个目录内。
        /// </summary>
        private static string GetSharedRoot(string destinationRoot)
        {
            string normalizedDestination = NormalizePath(destinationRoot);
            string projectRoot = ProjectRoot;
            string assetsRoot = NormalizePath(Application.dataPath);
            if (IsSameOrChildPath(normalizedDestination, projectRoot))
            {
                if (IsSameOrChildPath(normalizedDestination, assetsRoot))
                {
                    return assetsRoot;
                }

                return normalizedDestination;
            }

            // 项目外导出时，公共目录必须留在用户选择的目标目录内，
            // 不能写到目标目录的父级或同级目录。
            return normalizedDestination;
        }

        private static string TryGetFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                string trimmed = path.Trim().Trim('"');
                string fullPath = Path.IsPathRooted(trimmed)
                    ? trimmed
                    : Path.Combine(ProjectRoot, trimmed);
                return NormalizePath(fullPath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizePath(string path)
        {
            string fullPath = Path.GetFullPath(path);
            string root = Path.GetPathRoot(fullPath);
            if (!string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
            {
                fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return fullPath;
        }

        private static bool IsSameOrChildPath(string candidatePath, string parentPath)
        {
            string candidate = NormalizePath(candidatePath);
            string parent = NormalizePath(parentPath);
            if (string.Equals(candidate, parent, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return candidate.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || candidate.StartsWith(parent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRelativePath(string parentPath, string filePath)
        {
            string parent = NormalizePath(parentPath);
            string file = NormalizePath(filePath);
            if (!IsSameOrChildPath(file, parent))
            {
                return file;
            }

            return file.Substring(parent.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsMetaFile(string path)
        {
            return path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 任何路径段以 .git 开头时，都视为 Git 相关内容。
        /// 包括 .git、.gitignore、.gitattributes、.gitmodules 和 .github 等。
        /// </summary>
        private static bool IsGitRelatedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalizedPath = path.Replace('\\', '/');
            string[] segments = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].StartsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsExcluded(string relativePath, string[] excludes)
        {
            if (excludes == null || excludes.Length == 0)
            {
                return false;
            }

            string normalizedPath = NormalizeAssetPath(relativePath);
            for (int i = 0; i < excludes.Length; i++)
            {
                string exclude = NormalizeAssetPath(excludes[i]);
                if (string.IsNullOrEmpty(exclude))
                {
                    continue;
                }

                if (string.Equals(normalizedPath, exclude, StringComparison.OrdinalIgnoreCase)
                    || normalizedPath.StartsWith(exclude + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeAssetPath(string path)
        {
            return (path ?? string.Empty).Trim().Trim('/').Replace('\\', '/');
        }

        private static string CombineAssetPath(string root, string child)
        {
            string normalizedRoot = NormalizeAssetPath(root);
            string normalizedChild = NormalizeAssetPath(child);
            if (string.IsNullOrEmpty(normalizedRoot))
            {
                return normalizedChild;
            }

            if (string.IsNullOrEmpty(normalizedChild) || normalizedChild == ".")
            {
                return normalizedRoot;
            }

            return normalizedRoot + "/" + normalizedChild;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024L)
            {
                return bytes + " B";
            }

            if (bytes < 1024L * 1024L)
            {
                return (bytes / 1024f).ToString("0.##") + " KB";
            }

            if (bytes < 1024L * 1024L * 1024L)
            {
                return (bytes / (1024f * 1024f)).ToString("0.##") + " MB";
            }

            return (bytes / (1024f * 1024f * 1024f)).ToString("0.##") + " GB";
        }

        private static string ProjectRoot
        {
            get { return NormalizePath(Path.Combine(Application.dataPath, "..")); }
        }

        [Serializable]
        private sealed class FunctionCopyManifest
        {
            public string module = string.Empty;
            public string version = string.Empty;
            public string sourceRoot = string.Empty;
            public string[] includes = new string[0];
            public FunctionCopyManifestItem[] items = new FunctionCopyManifestItem[0];
        }

        [Serializable]
        private sealed class FunctionCopyManifestItem
        {
            public string name = string.Empty;
            public string source = string.Empty;
            public string destination = string.Empty;
            public bool isDirectory = true;
            public bool required = true;
            public string category = string.Empty;
            public string[] excludes = new string[0];
            public bool shared;
            public bool standaloneOnly;
            public bool allowParentDestination;
        }

        protected sealed class CopyItem
        {
            public readonly string DisplayName;
            public readonly string SourceRelativePath;
            public readonly bool IsDirectory;
            public readonly string DestinationRelativePath;
            public readonly bool Required;
            public readonly string Category;
            public readonly string[] Excludes;
            public readonly bool Shared;
            public readonly bool StandaloneOnly;
            public readonly bool AllowParentDestination;
            public bool Selected;

            public CopyItem(string displayName, string sourceRelativePath)
                : this(displayName, sourceRelativePath, true, displayName, true, "功能代码")
            {
            }

            public CopyItem(string displayName, string sourceRelativePath, bool isDirectory)
                : this(displayName, sourceRelativePath, isDirectory, displayName, true, "功能代码")
            {
            }

            public CopyItem(
                string displayName,
                string sourceRelativePath,
                bool isDirectory,
                string destinationRelativePath,
                bool required,
                string category,
                string[] excludes = null,
                bool shared = false,
                bool standaloneOnly = false,
                bool allowParentDestination = false)
            {
                DisplayName = displayName ?? string.Empty;
                SourceRelativePath = sourceRelativePath ?? string.Empty;
                IsDirectory = isDirectory;
                DestinationRelativePath = destinationRelativePath ?? string.Empty;
                Required = required;
                Category = string.IsNullOrWhiteSpace(category) ? "功能代码" : category;
                Excludes = excludes ?? new string[0];
                Shared = shared;
                StandaloneOnly = standaloneOnly;
                AllowParentDestination = allowParentDestination;
                Selected = true;
            }
        }

        private sealed class FileTransfer
        {
            public readonly string SourcePath;
            public readonly string DestinationPath;
            public readonly string RelativeDestinationPath;

            public FileTransfer(string sourcePath, string destinationPath, string relativeDestinationPath)
            {
                SourcePath = sourcePath;
                DestinationPath = destinationPath;
                RelativeDestinationPath = relativeDestinationPath;
            }
        }

        private sealed class MigrationPlan
        {
            public readonly List<FileTransfer> Transfers = new List<FileTransfer>();
            public readonly List<string> Errors = new List<string>();
            public int SelectedSourceCount;
            public long TotalBytes;
            public int ConflictCount;
            public string DestinationRoot = string.Empty;

            public int TransferCount => Transfers.Count;
        }
    }
}
#endif
