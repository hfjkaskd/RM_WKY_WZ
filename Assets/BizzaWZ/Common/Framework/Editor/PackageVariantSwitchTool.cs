#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

public static class PackageVariantSwitchTool
{
    private const string MenuRoot = "工具/打包/包体切换/";
    private const string RealWithdrawDefine = "BIZZA_REAL_WITHDRAW";
    private const string FinalAssetPath = "Assets/BizzaWZ/Final";
    private const string BackupFinalPath = "Backup/Final";
    private const string AddressableAssetsDataPath = "Assets/AddressableAssetsData";
    private const string BackupAddressableAssetsDataPath = "Backup/AddressableAssetsData";
    private const string GameConfigAssetsPath = "Assets/Game/Resources/ConfigAssets";
    private const string BackupGameConfigAssetsPath = "Backup/Game/Resources/ConfigAssets";
    private const string GameGeneratedCodePath = "Assets/Game/Scripts/GeneratedCode";
    private const string BackupGameGeneratedCodePath = "Backup/Game/Scripts/GeneratedCode";
    private const string ExternalSwitchLogPath = "Temp/PackageVariantSwitch.log";

    private static Process _externalSwitchProcess;
    private static bool _externalSwitchFormalPackage;
    private static string _externalSwitchScriptPath;
    private static bool _autoRefreshDisabled;

    private static readonly (string Id, string Version)[] FormalPackageDependencies =
    {
        ("com.unity.addressables", "1.22.3"),
        ("com.unity.modules.unitywebrequest", "1.0.0"),
        ("com.unity.modules.unitywebrequestassetbundle", "1.0.0"),
        ("com.unity.modules.unitywebrequestaudio", "1.0.0"),
        ("com.unity.modules.unitywebrequesttexture", "1.0.0"),
        ("com.unity.modules.unitywebrequestwww", "1.0.0")
    };

    private static readonly BuildTargetGroup[] BuildTargetGroups =
    {
        BuildTargetGroup.Android,
        BuildTargetGroup.Standalone,
        BuildTargetGroup.iOS
    };

    [MenuItem(MenuRoot + "切换到白包")]
    public static void SwitchToWhitePackage()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Switch To White Package",
            "This will remove BIZZA_REAL_WITHDRAW, move formal-only assets to Backup, and remove addressables plus UnityWebRequest packages.",
            "Switch",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        SwitchVariant(false);
    }

    [MenuItem(MenuRoot + "切换到正式包")]
    public static void SwitchToFormalPackage()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Switch To Formal Package",
            "This will restore formal-only assets from Backup, add addressables plus UnityWebRequest packages, and add BIZZA_REAL_WITHDRAW.",
            "Switch",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        SwitchVariant(true);
    }

    private static void SwitchVariant(bool formalPackage)
    {
        try
        {
            StartExternalFolderSwitch(formalPackage);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            AllowAutoRefreshIfNeeded();
            EditorUtility.DisplayDialog("Package Variant Switch Failed", ex.Message, "OK");
        }
    }

    private static void StartExternalFolderSwitch(bool formalPackage)
    {
        if (_externalSwitchProcess != null && !_externalSwitchProcess.HasExited)
        {
            EditorUtility.DisplayDialog(
                "Package Variant Switch",
                "A package variant switch is already running. Wait for it to finish before starting another one.",
                "OK");
            return;
        }

        if (formalPackage)
        {
            ValidateFormalAssetsFromBackup();
        }
        else
        {
            EnsureBackupFolder();
            ValidateFormalAssetsToBackup();
        }

        ReleaseEditorFileHandles();
        AssetDatabase.DisallowAutoRefresh();
        _autoRefreshDisabled = true;

        _externalSwitchFormalPackage = formalPackage;
        _externalSwitchScriptPath = WriteExternalMoveScript(formalPackage);
        _externalSwitchProcess = StartExternalMoveScript(_externalSwitchScriptPath);
        if (_externalSwitchProcess == null)
        {
            throw new InvalidOperationException("Failed to start external package folder move process.");
        }

        EditorApplication.update -= PollExternalFolderSwitch;
        EditorApplication.update += PollExternalFolderSwitch;

        string variantName = formalPackage ? "formal package" : "white package";
        Debug.Log($"[PackageVariantSwitch] Started external folder move for {variantName}. Script: {_externalSwitchScriptPath}");
        EditorUtility.DisplayDialog(
            "Package Variant Switch",
            $"Started external folder move for {variantName}. Unity auto-refresh is paused and will resume after the move finishes.",
            "OK");
    }

    private static void PollExternalFolderSwitch()
    {
        if (_externalSwitchProcess == null || !_externalSwitchProcess.HasExited)
        {
            return;
        }

        EditorApplication.update -= PollExternalFolderSwitch;

        Process process = _externalSwitchProcess;
        _externalSwitchProcess = null;
        int exitCode = process.ExitCode;
        process.Dispose();

        string variantName = _externalSwitchFormalPackage ? "formal package" : "white package";
        try
        {
            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"External folder move failed with exit code {exitCode}.\n"
                    + $"Script: {_externalSwitchScriptPath}\n"
                    + $"Log: {ProjectPath(ExternalSwitchLogPath)}\n\n"
                    + ReadExternalSwitchLog());
            }

            SetFormalPackageDependencies(_externalSwitchFormalPackage);
            SetScriptingDefine(RealWithdrawDefine, _externalSwitchFormalPackage);
            AssetDatabase.SaveAssets();
            AllowAutoRefreshIfNeeded();
            AssetDatabase.Refresh();
            Client.Resolve();

            Debug.Log($"[PackageVariantSwitch] Switched to {variantName}.");
            EditorUtility.DisplayDialog("Package Variant Switch", $"Switched to {variantName}.", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            AllowAutoRefreshIfNeeded();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Package Variant Switch Failed", ex.Message, "OK");
        }
    }

    private static void ValidateFormalAssetsToBackup()
    {
        ValidateAssetFolderMoveWithMeta(ProjectPath(FinalAssetPath), ProjectPath(BackupFinalPath));
        ValidateAssetFolderMoveWithMeta(ProjectPath(AddressableAssetsDataPath), ProjectPath(BackupAddressableAssetsDataPath));
        ValidateAssetFolderMoveWithMeta(ProjectPath(GameConfigAssetsPath), ProjectPath(BackupGameConfigAssetsPath));
        ValidateAssetFolderMoveWithMeta(ProjectPath(GameGeneratedCodePath), ProjectPath(BackupGameGeneratedCodePath));
    }

    private static void ValidateFormalAssetsFromBackup()
    {
        ValidateAssetFolderMoveWithMeta(ProjectPath(BackupFinalPath), ProjectPath(FinalAssetPath));
        ValidateAssetFolderMoveWithMeta(ProjectPath(BackupAddressableAssetsDataPath), ProjectPath(AddressableAssetsDataPath));
        ValidateAssetFolderMoveWithMeta(ProjectPath(BackupGameConfigAssetsPath), ProjectPath(GameConfigAssetsPath));
        ValidateAssetFolderMoveWithMeta(ProjectPath(BackupGameGeneratedCodePath), ProjectPath(GameGeneratedCodePath));
    }

    private static void SetScriptingDefine(string define, bool enabled)
    {
        foreach (BuildTargetGroup group in BuildTargetGroups)
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var values = new List<string>(symbols.Split(';'));
            values.RemoveAll(string.IsNullOrWhiteSpace);

            bool contains = values.Contains(define);
            bool changed = false;
            if (enabled && !contains)
            {
                values.Add(define);
                changed = true;
            }
            else if (!enabled && contains)
            {
                values.RemoveAll(value => value == define);
                changed = true;
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", values));
            }
        }
    }

    private static void SetFormalPackageDependencies(bool enabled)
    {
        string manifestPath = ProjectPath("Packages/manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Cannot find Unity package manifest.", manifestPath);
        }

        var manifest = JObject.Parse(File.ReadAllText(manifestPath));
        var dependencies = manifest["dependencies"] as JObject;
        if (dependencies == null)
        {
            dependencies = new JObject();
            manifest["dependencies"] = dependencies;
        }

        bool changed = false;
        foreach (var package in FormalPackageDependencies)
        {
            if (enabled)
            {
                if (dependencies[package.Id] == null
                    || !string.Equals((string)dependencies[package.Id], package.Version, StringComparison.Ordinal))
                {
                    dependencies[package.Id] = package.Version;
                    changed = true;
                }
            }
            else if (dependencies.Remove(package.Id))
            {
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        File.WriteAllText(manifestPath, manifest.ToString(Formatting.Indented) + Environment.NewLine, new UTF8Encoding(false));
        Debug.Log($"[PackageVariantSwitch] {(enabled ? "Added" : "Removed")} formal package dependencies in Packages/manifest.json.");
    }

    private static void EnsureBackupFolder()
    {
        Directory.CreateDirectory(ProjectPath("Backup"));
    }

    private static void ValidateAssetFolderMoveWithMeta(string sourceFolderPath, string targetFolderPath)
    {
        string sourceMetaPath = sourceFolderPath + ".meta";
        string targetMetaPath = targetFolderPath + ".meta";

        bool sourceFolderExists = Directory.Exists(sourceFolderPath);
        bool targetFolderExists = Directory.Exists(targetFolderPath);
        bool sourceMetaExists = File.Exists(sourceMetaPath);
        bool targetMetaExists = File.Exists(targetMetaPath);

        if (!sourceFolderExists && !sourceMetaExists && targetFolderExists && targetMetaExists)
        {
            Debug.Log($"[PackageVariantSwitch] Target asset folder already exists, external move will skip: {targetFolderPath}");
            return;
        }

        if (sourceFolderExists && targetFolderExists)
        {
            throw new InvalidOperationException($"Both source and target folders exist. Resolve manually before switching: {sourceFolderPath} -> {targetFolderPath}");
        }

        if (sourceMetaExists && targetMetaExists)
        {
            throw new InvalidOperationException($"Both source and target .meta files exist. Resolve manually before switching: {sourceMetaPath} -> {targetMetaPath}");
        }

        if (!sourceFolderExists || !sourceMetaExists)
        {
            throw new FileNotFoundException($"Cannot find source asset folder and .meta pair: {sourceFolderPath}");
        }
    }

    private static void ReleaseEditorFileHandles()
    {
        AssetDatabase.ReleaseCachedFileHandles();
        EditorUtility.UnloadUnusedAssetsImmediate(true);
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private static void AllowAutoRefreshIfNeeded()
    {
        if (!_autoRefreshDisabled)
        {
            return;
        }

        AssetDatabase.AllowAutoRefresh();
        _autoRefreshDisabled = false;
    }

    private static string WriteExternalMoveScript(bool formalPackage)
    {
        string scriptPath = ProjectPath($"Temp/PackageVariantSwitch_Move_{(formalPackage ? "Formal" : "White")}.cmd");
        Directory.CreateDirectory(Path.GetDirectoryName(scriptPath) ?? ProjectPath("Temp"));
        File.WriteAllText(scriptPath, BuildExternalMoveScript(formalPackage), new UTF8Encoding(false));
        return scriptPath;
    }

    private static Process StartExternalMoveScript(string scriptPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{ToWindowsPath(scriptPath)}\"\"",
            WorkingDirectory = ToWindowsPath(ProjectRoot),
            CreateNoWindow = true,
            UseShellExecute = false
        };
        return Process.Start(startInfo);
    }

    private static string BuildExternalMoveScript(bool formalPackage)
    {
        string projectRoot = ToWindowsPath(ProjectRoot);

        var moves = formalPackage
            ? new[]
            {
                (BackupFinalPath, FinalAssetPath),
                (BackupAddressableAssetsDataPath, AddressableAssetsDataPath),
                (BackupGameConfigAssetsPath, GameConfigAssetsPath),
                (BackupGameGeneratedCodePath, GameGeneratedCodePath)
            }
            : new[]
            {
                (FinalAssetPath, BackupFinalPath),
                (AddressableAssetsDataPath, BackupAddressableAssetsDataPath),
                (GameConfigAssetsPath, BackupGameConfigAssetsPath),
                (GameGeneratedCodePath, BackupGameGeneratedCodePath)
            };

        var builder = new StringBuilder();
        builder.AppendLine("@echo off");
        builder.AppendLine("setlocal");
        builder.AppendLine($"set \"PROJECT={projectRoot}\"");
        builder.AppendLine($"set \"LOG=%PROJECT%\\{ToWindowsPath(ExternalSwitchLogPath)}\"");
        builder.AppendLine("echo [%date% %time%] External package folder move started. > \"%LOG%\"");
        builder.AppendLine("cd /d \"%PROJECT%\" >> \"%LOG%\" 2>&1");
        builder.AppendLine("if errorlevel 1 exit /b 1");
        builder.AppendLine("if not exist \"Backup\" mkdir \"Backup\"");

        foreach (var move in moves)
        {
            builder.AppendLine($"call :MovePair \"{ToWindowsPath(ProjectPath(move.Item1))}\" \"{ToWindowsPath(ProjectPath(move.Item2))}\" >> \"%LOG%\" 2>&1");
            builder.AppendLine("if errorlevel 1 exit /b 1");
        }

        builder.AppendLine("echo [%date% %time%] External package folder move finished. >> \"%LOG%\"");
        builder.AppendLine("exit /b 0");
        builder.AppendLine();
        builder.AppendLine(":MovePair");
        builder.AppendLine("set \"SRC=%~1\"");
        builder.AppendLine("set \"DST=%~2\"");
        builder.AppendLine("echo Moving %SRC% to %DST%");
        builder.AppendLine("if exist \"%DST%\" (");
        builder.AppendLine("    if exist \"%SRC%\" (");
        builder.AppendLine("        echo ERROR: Both source and target folders exist: %SRC% -- %DST%");
        builder.AppendLine("        exit /b 10");
        builder.AppendLine("    ) else (");
        builder.AppendLine("        echo Target folder already exists, skip folder: %DST%");
        builder.AppendLine("    )");
        builder.AppendLine(") else (");
        builder.AppendLine("if exist \"%SRC%\" (");
        builder.AppendLine("        for %%D in (\"%DST%\") do if not exist \"%%~dpD\" mkdir \"%%~dpD\"");
        builder.AppendLine("        robocopy \"%SRC%\" \"%DST%\" /E /MOVE /R:2 /W:1 /NP");
        builder.AppendLine("        if errorlevel 8 exit /b 11");
        builder.AppendLine("        if exist \"%SRC%\" rmdir \"%SRC%\" 2>nul");
        builder.AppendLine("        if exist \"%SRC%\" exit /b 16");
        builder.AppendLine(") else (");
        builder.AppendLine("        echo ERROR: Source folder missing: %SRC%");
        builder.AppendLine("        exit /b 12");
        builder.AppendLine(")");
        builder.AppendLine(")");
        builder.AppendLine("if exist \"%DST%.meta\" (");
        builder.AppendLine("    if exist \"%SRC%.meta\" (");
        builder.AppendLine("        echo ERROR: Both source and target meta files exist: %SRC%.meta -- %DST%.meta");
        builder.AppendLine("        exit /b 13");
        builder.AppendLine("    ) else (");
        builder.AppendLine("        echo Target meta already exists, skip meta: %DST%.meta");
        builder.AppendLine("    )");
        builder.AppendLine(") else (");
        builder.AppendLine("    if exist \"%SRC%.meta\" (");
        builder.AppendLine("        move /Y \"%SRC%.meta\" \"%DST%.meta\"");
        builder.AppendLine("        if errorlevel 1 exit /b 14");
        builder.AppendLine("    ) else (");
        builder.AppendLine("        echo ERROR: Source meta missing: %SRC%.meta");
        builder.AppendLine("        exit /b 15");
        builder.AppendLine("    )");
        builder.AppendLine(")");
        builder.AppendLine("exit /b 0");
        return builder.ToString();
    }

    private static string ReadExternalSwitchLog()
    {
        string logPath = ProjectPath(ExternalSwitchLogPath);
        if (!File.Exists(logPath))
        {
            return "External switch log was not created.";
        }

        string log = File.ReadAllText(logPath);
        const int maxLength = 4000;
        return log.Length <= maxLength ? log : log.Substring(log.Length - maxLength);
    }

    private static string ToWindowsPath(string path)
    {
        return path.Replace('/', '\\');
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)))
            .Replace('\\', '/');
    }

    private static string ProjectRoot
    {
        get
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
        }
    }
}
#endif
