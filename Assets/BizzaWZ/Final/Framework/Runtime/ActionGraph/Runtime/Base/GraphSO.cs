using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

  [Obfuz.ObfuzIgnore]

public class GraphSO : ScriptableObject
{
    // [ShowInInspector][LabelText("图表类型")][PropertyOrder(-999)]
    // public string GraphType
    // {
    //     get
    //     {
    //         if (graph != null)
    //         {
    //             var attr = graph.GetType().GetCustomAttribute<GraphElementInfoAttribute>();
    //             if (attr != null)
    //             {
    //                 return $"{attr.Text}";
    //             }
    //         }
    //
    //         return "";
    //     } 
    // }

    [SerializeReference][HideLabel][HideReferenceObjectPicker]
    public ActionGraphBase graph;

    [Title("预览")]
    [HideLabel]
    [ShowInInspector][MultiLineProperty(20)]
    public string DebugText => graph?.ToString() ?? "null";
}

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
public class GraphSOOpenType
{
    [OnOpenAsset(1)]
    public static bool OnOpenAssets(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is GraphSO so)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            GraphDebugUtils.OpenGraph(path);
            return true;
        }
        return false; // we did not handle the open
    }
}
#endif