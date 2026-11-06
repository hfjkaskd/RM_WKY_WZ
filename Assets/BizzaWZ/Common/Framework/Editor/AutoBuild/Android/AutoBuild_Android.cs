using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AutoBuild_Android
{
    private const string BuildPathArg = "-customBuildPath";

    [MenuItem("工具/打包/Android/自动打包APK")]
    public static void BuildAndroidApk()
    {
        BuildAndroidApkInternal();
    }

    public static void BuildAndroidApkBatch()
    {
        BuildAndroidApkInternal();
    }

    private static void BuildAndroidApkInternal()
    {
        string outputPath = ResolveOutputPath();
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        Debug.Log($"[AutoBuild_Android] Output: {outputPath}");
        Debug.Log($"[AutoBuild_Android] Product: {PlayerSettings.productName}");
        Debug.Log($"[AutoBuild_Android] Bundle: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}");
        Debug.Log($"[AutoBuild_Android] Version: {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode})");

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            throw new InvalidOperationException("Failed to switch active build target to Android.");
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Android build failed: {report.summary.result}, errors={report.summary.totalErrors}, warnings={report.summary.totalWarnings}");
        }

        Debug.Log($"[AutoBuild_Android] Build succeeded: {outputPath}");
    }

    private static string ResolveOutputPath()
    {
        string customPath = GetCommandLineArg(BuildPathArg);
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (string.Equals(Path.GetExtension(customPath), ".apk", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(customPath);
            }

            return Path.Combine(Path.GetFullPath(customPath), BuildFileName());
        }

        string savedPath = EditorUserBuildSettings.GetBuildLocation(BuildTarget.Android);
        if (!string.IsNullOrWhiteSpace(savedPath))
        {
            return Path.GetFullPath(savedPath);
        }

        return Path.Combine(Environment.CurrentDirectory, "Builds", "Android", BuildFileName());
    }

    private static string BuildFileName()
    {
        string productName = string.Concat(PlayerSettings.productName.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = "AndroidBuild";
        }

        return $"{productName}_{PlayerSettings.bundleVersion}_{PlayerSettings.Android.bundleVersionCode}.apk";
    }

    private static string GetCommandLineArg(string argName)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return string.Empty;
    }
}
