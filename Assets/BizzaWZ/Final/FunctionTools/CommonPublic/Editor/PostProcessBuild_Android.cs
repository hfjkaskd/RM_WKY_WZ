#if BIZZA_REAL_WITHDRAW || USER_VERIFICATION_SDK
using UnityEngine;
using System.IO;
using UnityEditor.Android;
using System;

public static class IOTool
{
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
#if UNITY_ANDROID
public class PostProcessBuild_Android : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 10000;

    private const string NativeKeyboardDependency = "implementation(name: 'NativeKeyboard', ext:'aar')";
    private static readonly string[] IpVerificationJavaFiles =
    {
        "unityLibrary/src/main/java/com/bangsawan/BangsawanLog.java",
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/DeviceUtils.java",
        "unityLibrary/src/main/java/com/bangsawan/unity/UnityHelper.java"
    };
    private static readonly string[] NonIpVerificationJavaFiles =
    {
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/ADUtils.java",
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/HttpUtil.java",
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/IBridgeRevenueUtil.java",
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/MainActivity.java",
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/MBridgeRevenueUtil.java",
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/MessageName.java",
        "unityLibrary/src/main/java/com/bangsawan/oceanshine/SdkHelper.java"
    };

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        const string sourceDir = "_Bizza_BuildTemplate/Android/";
        string destinationDir = Path.GetDirectoryName(path);
        var dir = new DirectoryInfo(sourceDir);
        if (dir.Exists)
        {
#if BIZZA_REAL_WITHDRAW
            IOTool.CopyDirectory(sourceDir, destinationDir, true);
#else
            RemoveNonIpVerificationJavaFiles(destinationDir);
            CopyIpVerificationJavaFiles(sourceDir, destinationDir);
#endif
        }

        Debug.Log("[PostProcessBuild_Android] Network build detected, keep INTERNET permission.");
        RemoveNativeKeyboardForWhitePackage(path);
    }

    private static void CopyIpVerificationJavaFiles(string sourceDir, string destinationDir)
    {
        foreach (string relativePath in IpVerificationJavaFiles)
        {
            string sourcePath = Path.Combine(sourceDir, relativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    $"IP verification Android source file not found: {sourcePath}",
                    sourcePath);
            }

            string destinationPath = Path.Combine(destinationDir, relativePath);
            string destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourcePath, destinationPath, true);
        }

        Debug.Log("[PostProcessBuild_Android] Copied minimal IP verification Java sources.");
    }

    private static void RemoveNonIpVerificationJavaFiles(string destinationDir)
    {
        foreach (string relativePath in NonIpVerificationJavaFiles)
        {
            string destinationPath = Path.Combine(destinationDir, relativePath);
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
        }
    }

    private static void RemoveNativeKeyboardForWhitePackage(string unityLibraryPath)
    {
#if BIZZA_REAL_WITHDRAW
        return;
#else

        string nativeKeyboardAarPath = Path.Combine(unityLibraryPath, "libs", "NativeKeyboard.aar");
        if (File.Exists(nativeKeyboardAarPath))
        {
            File.Delete(nativeKeyboardAarPath);
            Debug.Log("[PostProcessBuild_Android] White package detected, delete NativeKeyboard.aar.");
        }

        string unityLibraryGradlePath = Path.Combine(unityLibraryPath, "build.gradle");
        if (!File.Exists(unityLibraryGradlePath))
        {
            return;
        }

        string gradleContent = File.ReadAllText(unityLibraryGradlePath);
        if (!gradleContent.Contains(NativeKeyboardDependency))
        {
            return;
        }

        gradleContent = gradleContent.Replace("    " + NativeKeyboardDependency + Environment.NewLine, string.Empty);
        gradleContent = gradleContent.Replace("    " + NativeKeyboardDependency + "\n", string.Empty);
        gradleContent = gradleContent.Replace(NativeKeyboardDependency + Environment.NewLine, string.Empty);
        gradleContent = gradleContent.Replace(NativeKeyboardDependency + "\n", string.Empty);
        File.WriteAllText(unityLibraryGradlePath, gradleContent);
        Debug.Log("[PostProcessBuild_Android] White package detected, remove NativeKeyboard gradle dependency.");
#endif
    }
}
#endif
#endif
