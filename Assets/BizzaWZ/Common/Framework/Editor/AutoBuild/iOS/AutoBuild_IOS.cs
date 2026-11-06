using UnityEditor;
using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;

public class AutoBuild_IOS
{
    // === 自定义配置 ===
    static string BUNDLE_ID = "com.yourcompany.yourgame";
    static string TEAM_ID = "ABCDEFG123";
    static string CODE_SIGN_IDENTITY = "Apple Development";
    static string PROVISION_PROFILE = "Your_Provisioning_Profile_Name";
    static string EXPORT_PLIST_PATH = "exportOptions.plist"; // 需提前放在项目根目录

    [MenuItem("工具/打包/iOS/更新SVN")]
    public static void SVN()
    {
        UpdateSVN();
    }

    [MenuItem("工具/打包/iOS/自动打包")]
    public static void BuildIOS()
    {
        try
        {
            Log("===== 🚀 自动化 iOS 打包开始 =====");

            // 1️⃣ 拉取 SVN
            // UpdateSVN();

            // 2️⃣ Unity 导出 Xcode 工程
            ExportXcode();

            // OpenAndBuildInXcode();

            // // 3️⃣ 修改 Info.plist / 签名设置
            // ModifyXcodeSettings();
            //
            // // 4️⃣ Xcode 打包
            // BuildXcodeProject();
            //
            // // 5️⃣ 安装到手机（可选）
            // DeployToDevice();

            Log("✅ 全部流程完成！");
            EditorUtility.DisplayDialog("打包完成", "打包成功", "好");
        }
        catch (Exception ex)
        {
            Log($"❌ 构建失败: {ex.Message}");
            EditorUtility.DisplayDialog("构建失败", ex.Message, "关闭");
        }
    }

    // 1️⃣ SVN 更新
    static void UpdateSVN()
    {
        Log("🔄 SVN 更新中...");
        string svnProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../"));
        SvnHelper.UpdateSVN(svnProjectPath);
    }

    // 2️⃣ Unity 导出 Xcode
    static void ExportXcode()
    {
        Log("🎮 导出 Xcode 工程中...");
        string outputPath = EditorUserBuildSettings.GetBuildLocation(BuildTarget.iOS);
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = "Build_iOS"; // 默认路径
        }

        var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, outputPath, BuildTarget.iOS, BuildOptions.None);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception("Unity 导出失败: " + report.summary.result);
        }
        Log("✅ Unity 导出成功！");
    }


    public static void Log(string message)
    {
        // Directory.CreateDirectory(Path.GetDirectoryName(LOG_PATH));
        // File.AppendAllText(LOG_PATH, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        UnityEngine.Debug.Log(message);
    }
}
