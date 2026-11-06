using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class AutoBuild_Android_RequestListener
{
    private const string RequestFilePath = "Temp/AutoBuildAndroid.request.txt";
    private const string OutputFilePath = "Temp/AutoBuildAndroid.output.txt";
    private const string StateFilePath = "Temp/AutoBuildAndroid.state.txt";
    private const string ErrorFilePath = "Temp/AutoBuildAndroid.error.txt";

    private static bool _isProcessing;

    static AutoBuild_Android_RequestListener()
    {
        EditorApplication.update -= TryProcessPendingRequest;
        EditorApplication.update += TryProcessPendingRequest;
    }

    private static void TryProcessPendingRequest()
    {
        if (_isProcessing || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (!File.Exists(RequestFilePath))
        {
            return;
        }

        string requestedPath = string.Empty;
        try
        {
            _isProcessing = true;
            requestedPath = File.ReadAllText(RequestFilePath).Trim().Trim('"');
            File.Delete(RequestFilePath);
            DeleteFileIfExists(OutputFilePath);
            DeleteFileIfExists(ErrorFilePath);
            WriteTextFile(StateFilePath, "[AutoBuild_Android] Processing request: " + requestedPath);

            string outputPath = BuildAndroidApkToPath(requestedPath);
            WriteTextFile(OutputFilePath, outputPath);
            WriteTextFile(StateFilePath, "[AutoBuild_Android] Request completed: " + outputPath);
        }
        catch (Exception exception)
        {
            string errorText = "[AutoBuild_Android] Request failed: " + requestedPath + Environment.NewLine + exception;
            WriteTextFile(ErrorFilePath, errorText);
            WriteTextFile(OutputFilePath, errorText);
            WriteTextFile(StateFilePath, "[AutoBuild_Android] Request failed.");
            Debug.LogException(exception);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static string BuildAndroidApkToPath(string customPath)
    {
        string outputPath = ResolveOutputPath(customPath);
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
        return outputPath;
    }

    private static string ResolveOutputPath(string customPath)
    {
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

    private static void WriteTextFile(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
