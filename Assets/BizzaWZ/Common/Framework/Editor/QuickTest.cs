#if UNITY_EDITOR
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class QuickLaunch : MonoBehaviour
{
    [MenuItem("工具/常用/进入加载场景 %q")]
    public static void EnterScene_Loading()
    {
        EditorSceneManager.OpenScene(WZProjectSetting.EditorResPath + "Scenes/InitWZ.unity");
        EditorApplication.isPlaying = true;
    }

    [MenuItem("工具/常用/进入战斗测试场景 %w")]
    public static void EnterScene_BattleTest()
    {
        EditorSceneManager.OpenScene(WZProjectSetting.EditorResPath + "Scenes/GamePlay.unity");
    }

    [MenuItem("工具/常用/打开表格目录 %t")]
    static void OpenTableFolder()
    {
        var path = Application.dataPath + "\\..\\..\\_Data\\Data";
        Debug.Log(path);
        Process.Start(path);
    }

    [MenuItem("工具/版本管理/打开仓库根目录 %&[")]
    static void OpenSVNFolder1()
    {
        var path = Application.dataPath + "\\..\\..\\..";
        Debug.Log(path);
        Process.Start(path);
    }

    [MenuItem("工具/版本管理/打开框架目录 %&]")]
    static void OpenSVNFolder2()
    {
        var path = Application.dataPath + "\\BizzaGame";
        Debug.Log(path);
        Process.Start(path);
    }

    [MenuItem("工具/版本管理/更新 %&u")]
    static void SVNUpdate()
    {
        var path = Application.dataPath + "\\..\\..";
        _SVNCommand(path, "update");
    }

    [MenuItem("工具/版本管理/提交 %&c")]
    static void SVNCommit()
    {
        var path = Application.dataPath + "\\..\\..";
        _SVNCommand(path, "commit");
    }

    static async Task<bool> _SVNCommand(string workingCopyPath, string command)
    {
        var process = new Process
        {
            StartInfo =
            {
                FileName = "svn",
                Arguments = $"{command} \"{workingCopyPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            }
        };

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();

        Debug.Log(output);
        var ret = process.ExitCode == 0;
        return ret;
    }

    // [MenuItem("工具/常用/打开动作目录 %&w")]
    // public static void PingFolder_Action()
    // {
    //     var targetPath = "Assets/GameRes/ConfigAssets/Actions/BL_BV_PlayerWeapon1.txt";
    //     Debug.LogError(targetPath);
    //     Object targetObject = AssetDatabase.LoadAssetAtPath<Object>(targetPath);
    //     Debug.LogError(targetObject == null);
    //     if (targetObject != null)
    //     {
    //         Selection.activeObject = targetObject;
    //         EditorGUIUtility.PingObject(targetObject);
    //     }
    // }
}
#endif
