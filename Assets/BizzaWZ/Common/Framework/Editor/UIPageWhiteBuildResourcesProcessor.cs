#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class UIPageWhiteBuildResourcesProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string RealWithdrawDefine = "BIZZA_REAL_WITHDRAW";
    private const string ResourcesPrefabFolder = "Assets/Resources/Prefabs/UI";

    private static readonly HashSet<string> WhitePageIds = new(StringComparer.Ordinal)
    {
        "LoadingPanel",
        "CommonConfirmTipsPanel",
        "RealGamePanel",
        "PausePanel",
        "LosePanel",
        "WhiteWinPanel",
        "SeabedMenuPanel",
        "TransitonPanel"
    };

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        CleanupTemporaryResources(false);

        if (!IsWhiteBuild(report.summary.platformGroup))
        {
            return;
        }

        var pageEntries = CollectPageEntries(true);
        int stagedCount = StageWhiteResources(pageEntries);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WhiteBuildUIResources] Prepared white build UI resources. pages:{pageEntries.Count} staged:{stagedCount}");
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        CleanupTemporaryResources(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WhiteBuildUIResources] Cleaned white build temporary UI resources.");
    }

    private static int StageWhiteResources(IReadOnlyList<PageEntry> entries)
    {
        EnsureResourcesPrefabFolder();

        int count = 0;
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.PageId) || !WhitePageIds.Contains(entry.PageId))
            {
                continue;
            }

            if (string.IsNullOrEmpty(entry.EditorAssetPath))
            {
                throw new BuildFailedException($"[WhiteBuildUIResources] Missing source prefab path for page:{entry.PageId}");
            }

            string sourcePath = entry.EditorAssetPath;
            string targetPath = GetTemporaryPrefabPath(entry.PageId);
            if (string.Equals(sourcePath, targetPath, StringComparison.Ordinal))
            {
                continue;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
            {
                throw new BuildFailedException($"[WhiteBuildUIResources] Source prefab not found for page:{entry.PageId}, path:{sourcePath}");
            }

            DeleteAssetIfExists(targetPath);
            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                throw new BuildFailedException($"[WhiteBuildUIResources] Failed to copy UI prefab: {sourcePath} -> {targetPath}");
            }

            AssetDatabase.ImportAsset(targetPath);
            count++;
        }

        return count;
    }

    private static List<PageEntry> CollectPageEntries(bool whiteOnly)
    {
        var result = new List<PageEntry>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(ResourcesPrefabFolder + "/", StringComparison.Ordinal))
            {
                continue;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var page = prefab.GetComponent<UIPageBase>();
            if (page == null) continue;

            string pageId = ResolvePageId(page, prefab);
            if (string.IsNullOrEmpty(pageId) || !used.Add(pageId))
            {
                continue;
            }

            if (whiteOnly && !WhitePageIds.Contains(pageId))
            {
                continue;
            }

            result.Add(new PageEntry(pageId, path));
        }

        result.Sort((a, b) => string.Compare(a.PageId, b.PageId, StringComparison.Ordinal));
        return result;
    }

    private static string ResolvePageId(UIPageBase page, GameObject prefab)
    {
        if (page != null && !page.PageId.IsEmpty)
        {
            return page.PageId.Value;
        }

        return prefab != null ? prefab.name : string.Empty;
    }

    private static int CleanupTemporaryResources(bool refresh)
    {
        int count = 0;
        foreach (string pageId in WhitePageIds)
        {
            string targetPath = GetTemporaryPrefabPath(pageId);
            if (DeleteAssetIfExists(targetPath))
            {
                count++;
            }
        }

        if (refresh)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        return count;
    }

    private static bool DeleteAssetIfExists(string assetPath)
    {
        bool deleted = false;
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
        {
            deleted = AssetDatabase.DeleteAsset(assetPath);
        }

        if (File.Exists(assetPath))
        {
            FileUtil.DeleteFileOrDirectory(assetPath);
            deleted = true;
        }

        string metaPath = assetPath + ".meta";
        if (File.Exists(metaPath))
        {
            FileUtil.DeleteFileOrDirectory(metaPath);
        }

        return deleted;
    }

    private static string GetTemporaryPrefabPath(string pageId)
    {
        return $"{ResourcesPrefabFolder}/{pageId}.prefab";
    }

    private static bool IsWhiteBuild(BuildTargetGroup buildTargetGroup)
    {
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        foreach (string symbol in symbols.Split(';'))
        {
            if (string.Equals(symbol.Trim(), RealWithdrawDefine, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureResourcesPrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(ResourcesPrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources/Prefabs", "UI");
        }
    }

    private sealed class PageEntry
    {
        public string PageId { get; }
        public string EditorAssetPath { get; }

        public PageEntry(string pageId, string editorAssetPath)
        {
            PageId = pageId;
            EditorAssetPath = editorAssetPath;
        }
    }
}
#endif
