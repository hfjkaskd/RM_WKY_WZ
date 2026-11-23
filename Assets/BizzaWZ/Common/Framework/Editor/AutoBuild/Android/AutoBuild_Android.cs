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

    public static void BuildAndroidAabBatch()
    {
        BuildAndroidInternal(true);
    }

    private static void BuildAndroidApkInternal()
    {
        BuildAndroidInternal(false);
    }

    private static void BuildAndroidInternal(bool buildAppBundle)
    {
        bool obfuscationSucceeded = false;
        Action<Obfuz.Unity.ObfuscationEndEventArgs> onObfuscationEnd = null;
        try
        {
            if (buildAppBundle)
            {
                ConfigureReleaseSigning();
                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                EditorUserBuildSettings.connectProfiler = false;
                onObfuscationEnd = result =>
                {
                    if (!result.success)
                        throw new UnityEditor.Build.BuildFailedException("Release build stopped because Obfuz obfuscation failed.");
                    obfuscationSucceeded = true;
                    Debug.Log("[AutoBuild_Android] Release obfuscation verified.");
                };
                Obfuz.Unity.ObfuscationProcess.OnObfuscationEnd += onObfuscationEnd;
            }
            BuildAndroidPlayer(buildAppBundle);
            if (buildAppBundle && !obfuscationSucceeded)
                throw new UnityEditor.Build.BuildFailedException("Release build requires successful Obfuz obfuscation, but it did not run.");
        }
        finally
        {
            if (buildAppBundle)
            {
                if (onObfuscationEnd != null)
                    Obfuz.Unity.ObfuscationProcess.OnObfuscationEnd -= onObfuscationEnd;
                PlayerSettings.Android.keystorePass = string.Empty;
                PlayerSettings.Android.keyaliasPass = string.Empty;
            }
        }
    }

    private static void ConfigureReleaseSigning()
    {
        string keyStorePath = Path.GetFullPath(RequireEnvironmentVariable("GEMWINK_KEYSTORE_PATH"));
        if (!File.Exists(keyStorePath))
            throw new FileNotFoundException("The release keystore does not exist.", keyStorePath);
        string storePassword = RequireEnvironmentVariable("GEMWINK_KEYSTORE_PASSWORD");
        string keyAlias = RequireEnvironmentVariable("GEMWINK_KEY_ALIAS");
        string keyPassword = RequireEnvironmentVariable("GEMWINK_KEY_PASSWORD");

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keyStorePath;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keystorePass = storePassword;
        PlayerSettings.Android.keyaliasPass = keyPassword;
    }

    private static string RequireEnvironmentVariable(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Missing required release signing environment variable: " + name);
        return value;
    }

    private static void BuildAndroidPlayer(bool buildAppBundle)
    {
        string outputPath = ResolveOutputPath(buildAppBundle);
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

        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
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

    private static string ResolveOutputPath(bool buildAppBundle)
    {
        string customPath = GetCommandLineArg(BuildPathArg);
        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (string.Equals(Path.GetExtension(customPath), buildAppBundle ? ".aab" : ".apk", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(customPath);
            }

            return Path.Combine(Path.GetFullPath(customPath), BuildFileName(buildAppBundle));
        }

        string savedPath = EditorUserBuildSettings.GetBuildLocation(BuildTarget.Android);
        if (!string.IsNullOrWhiteSpace(savedPath) &&
            (!buildAppBundle || string.Equals(Path.GetExtension(savedPath), ".aab", StringComparison.OrdinalIgnoreCase)))
        {
            return Path.GetFullPath(savedPath);
        }

        return Path.Combine(Environment.CurrentDirectory, "Builds", "Android", BuildFileName(buildAppBundle));
    }

    private static string BuildFileName(bool buildAppBundle)
    {
        string productName = string.Concat(PlayerSettings.productName.Where(ch => !Path.GetInvalidFileNameChars().Contains(ch)));
        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = "AndroidBuild";
        }

        return $"{productName}_{PlayerSettings.bundleVersion}_{PlayerSettings.Android.bundleVersionCode}.{(buildAppBundle ? "aab" : "apk")}";
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
