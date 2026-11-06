using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class SvnHelper
{
    public static void UpdateSVN(string projectPath)
    {
        string command = $"update";
        RunShellCommand(command, projectPath);
    }

    static void RunShellCommand(string command, string path)
    {
        Debug.Log("RunShellCommand:" + command);
        Process process = new Process();
        process.StartInfo.FileName = "svn";
        process.StartInfo.Arguments = command;
        process.StartInfo.WorkingDirectory = path;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        UnityEngine.Debug.Log($"SVN Output:\n{output}");
        if (!string.IsNullOrEmpty(error))
            UnityEngine.Debug.LogError($"SVN Error:\n{error}");

        if (process.ExitCode != 0)
            throw new System.Exception("❌ SVN 更新失败，请检查仓库状态");
    }
}