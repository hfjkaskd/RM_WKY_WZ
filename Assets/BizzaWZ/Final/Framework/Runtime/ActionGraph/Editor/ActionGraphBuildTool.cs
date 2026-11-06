using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class ActionGraphBuildTool : IPreprocessBuildWithReport
{
    // [MenuItem("工具/动作图/收集图表文件")]
    public static void CollectGraphFiles()
    {
        // Debug.Log("ActionGraphBuildTool:OnPreprocessBuild");
        // List<GraphSO> list = new();
        // var allGraphs = AssetDatabase.FindAssets($"t:{typeof(GraphSO)}", new string[] {GraphSaveUtils.SavePath});
        // foreach (var v in allGraphs)
        // {
        //     var so = AssetDatabase.LoadAssetAtPath<GraphSO>(AssetDatabase.GUIDToAssetPath(v));
        //     list.Add(so);
        // }
        //
        // var listSO = ScriptableObject.CreateInstance<GraphListSO>();
        // listSO.graphList = list;
        // var path = GraphSaveUtils.SavePath + $"/{nameof(GraphListSO)}.asset";
        // AssetDatabase.CreateAsset(listSO, path);
        // AssetDatabase.SaveAssets();
        // AddressableUtils.AddAssetToAddressable("Actions", path, nameof(GraphListSO));
        // AssetDatabase.Refresh();
    }

    public int callbackOrder => -9999999;

    //webgl平台加载多个小文件很慢，需要合并到一起
    public void OnPreprocessBuild(BuildReport report)
    {
        CollectGraphFiles();
    }
}
