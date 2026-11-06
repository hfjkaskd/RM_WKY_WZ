#if UNITY_IOS
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using Debug = UnityEngine.Debug;

public static class PostXcodeModify
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        Debug.Log("🚀 [PostXcodeModify] 开始自动修改 Xcode 工程设置...");

        string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);

        // 获取 Target GUID
#if UNITY_2019_3_OR_NEWER
        string mainTarget = proj.GetUnityMainTargetGuid();           // Unity-iPhone
        string frameworkTarget = proj.GetUnityFrameworkTargetGuid(); // UnityFramework
#else
        string mainTarget = proj.TargetGuidByName("Unity-iPhone");
        string frameworkTarget = proj.TargetGuidByName("UnityFramework");
#endif

        // 1️⃣ 自动签名
        // Debug.Log("🔑 设置自动签名...");
        // proj.SetBuildProperty(mainTarget, "CODE_SIGN_STYLE", "Automatic");
        // proj.SetBuildProperty(mainTarget, "CODE_SIGN_IDENTITY", "Apple Development");
        // proj.SetTeamId(mainTarget, "BYV2364U84");

        // 2️⃣ Info.plist: 添加 App Transport Security
        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;
        PlistElementDict ats = rootDict.CreateDict("NSAppTransportSecurity");
        ats.SetBoolean("NSAllowsArbitraryLoads", true);

        plist.WriteToFile(plistPath);
        Debug.Log("🧾 已修改 Info.plist: 允许 HTTP 请求 (ATS)");

        // 3️⃣ 添加 frameworks
        Debug.Log("📦 添加 du.framework 和 libresolv.tbd...");
        string frameworkPath = "Frameworks/du.framework";
        string duFrameworkGUID = proj.AddFile(frameworkPath, "Frameworks/du.framework", PBXSourceTree.Source);
        proj.AddFileToBuild(mainTarget, duFrameworkGUID);

        proj.AddFrameworkToProject(mainTarget, "libresolv.tbd", false);

        // 4️⃣ 在 UnityFramework 的 Compile Sources 中添加 OpenUDID.m
        Debug.Log("🧩 添加 OpenUDID.m 到 UnityFramework -> Compile Sources");
        string openUDIDPath = Path.Combine(pathToBuiltProject, "Libraries/Plugins/iOS/OpenUDID.m");
        string fileGUID = proj.AddFile(openUDIDPath, openUDIDPath, PBXSourceTree.Source);
        proj.AddFileToBuild(frameworkTarget, fileGUID);

        // 5️⃣ UnityFramework Build Setting: Always Embed Swift Standard Libraries = NO
        Debug.Log("⚙️ 设置 Always Embed Swift Standard Libraries = NO");
        proj.SetBuildProperty(frameworkTarget, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

        // 保存修改
        proj.WriteToFile(projPath);

        Debug.Log("✅ [PostXcodeModify] Xcode 工程修改完成！");



        // string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        // OpenAndBuildInXcode(pathToBuiltProject);

        EditorApplication.delayCall += () =>
        {
            OpenAndBuildInXcode(pathToBuiltProject);
        };
    }

    [MenuItem("工具/打包/iOS/测试Xcode")]
    static void TestOpenXcode()
    {
        OpenAndBuildInXcode("");
    }

    [MenuItem("工具/打包/iOS/测试Xcode 2")]
    static void TestOpenXcode2()
    {
        OpenAndBuildInXcode("");
    }

    private static void OpenAndBuildInXcode(string pathToBuiltProject)
    {
        string xcodeProjPath = System.IO.Path.Combine(pathToBuiltProject, "Unity-iPhone.xcworkspace");
        xcodeProjPath = "/Users/mac/Desktop/bd/Unity-iPhone.xcworkspace";


        // 2️⃣ 等待 Xcode 启动（视机器性能而定）
        // System.Threading.Thread.Sleep(30000);
        int maxWaitMs = 30000;
        int waited = 0;
        int step = 1000; // 每秒检测一次

        while (!System.IO.File.Exists(xcodeProjPath) && waited < maxWaitMs)
        {
            System.Threading.Thread.Sleep(step);
            waited += step;
            UnityEngine.Debug.Log($"等待 Xcode 工程生成中... {waited / 1000}s");
        }

        AutoBuild_IOS.Log($"📂 打开 Xcode 工程: {xcodeProjPath}");
        // 1️⃣ 打开 Xcode 工程
        // RunShellCommand($"open \"{xcodeProjPath}\"");
        RunShellCommand($"open {xcodeProjPath}");

        // 2️⃣ 等待 Xcode 启动（视机器性能而定）
        System.Threading.Thread.Sleep(30000);

        // 3️⃣ 构造 AppleScript
        string appleScript = @"
        tell application ""Xcode""
            activate
            delay 1
            tell application ""System Events""
                keystroke ""b"" using {command down}
            end tell
        end tell
    ";
        string tempScriptPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "autoBuildXcode.scpt");
        File.WriteAllText(tempScriptPath, appleScript);

        // 4️⃣ 执行 AppleScript
        RunShellCommand($"osascript \"{tempScriptPath}\"");
        AutoBuild_IOS.Log("🛠️ 已触发 Xcode Build (⌘B)");
    }



    private static void RunShellCommand(string command)
    {
        Process process = new Process();
        process.StartInfo.FileName = "/bin/bash";
        process.StartInfo.Arguments = $"-c \"{command}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
            AutoBuild_IOS.Log(output);
        if (!string.IsNullOrEmpty(error))
            AutoBuild_IOS.Log(error);
    }
}
#endif
