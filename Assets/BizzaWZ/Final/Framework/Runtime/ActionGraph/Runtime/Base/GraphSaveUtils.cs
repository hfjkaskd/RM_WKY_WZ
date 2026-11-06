using System.IO;
using Newtonsoft.Json;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

 
public static class GraphSaveUtils
{

    public const string SavePath = "Assets/Game/Resources/Actions";
    public static string FullPath => Application.dataPath.Substring(0, Application.dataPath.Length - 6) + SavePath;

    public static string GetAssetsPath(string fileName)
    {
        return SavePath + "/" + fileName + ".asset";
    }

    public static string GetFullPath(string fileName)
    {
        return FullPath + "/" + fileName + ".asset";
    }

#if UNITY_EDITOR
    public static ActionGraphBase EditorRead(string fileNameOrPath, out string fileName, out string filePath)
    {
        fileName = "";
        filePath = SavePath;
        var assetPath = fileNameOrPath.StartsWith("Assets") ? fileNameOrPath : GetAssetsPath(fileNameOrPath);
        var so = AssetDatabase.LoadAssetAtPath<GraphSO>(assetPath);
        if (so == null)
        {
            ActionGraphLog.Error($"加载Graph文件失败:{fileNameOrPath}");
            return null;
        }

        filePath = assetPath.Substring(0, assetPath.LastIndexOf('/'));
        fileName = so.name; 
        return so.graph;

        // if (string.IsNullOrEmpty(fileName))
        // {
        //     return null;
        // }
        // var settings = new JsonSerializerSettings
        // {
        //     TypeNameHandling = TypeNameHandling.Auto,
        //     ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        //     Formatting = Formatting.Indented,
        // };
        // Debug.Log($"Read:{fileName}");
        // var path = GetFullPath(fileName);
        // if (!File.Exists(path))
        // {
        //     ActionGraphLog.Error($"文件错误:{path}");
        //     return null;
        // }
        // var json = File.ReadAllText(path);
        // var saveData = JsonConvert.DeserializeObject<GraphSaveData>(json, settings);
        // return saveData;
    }


    public static void Save(ActionGraphBase graph, string fileName, string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = SavePath;
        }

        var newGraph = graph.Clone() as ActionGraphBase;

        GraphDebugUtils.CheckGraphClone(graph, newGraph, fileName, true);

        newGraph.graphName = fileName;
        var so = ScriptableObject.CreateInstance<GraphSO>();
        so.graph = newGraph;

        //确保目录存在
        string directoryPath = Path.GetDirectoryName(GetFullPath(fileName));
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string assetPath = $"{filePath}/{fileName}.asset"; //GetAssetsPath(fileName); // 自定义路径
        // if (AssetDatabase.LoadAssetAtPath<GraphSO>(assetPath) != null)
        // {
        //     bool shouldSave = EditorUtility.DisplayDialog(
        //         "保存确认",
        //         "是否覆盖？",
        //         "覆盖",
        //         "取消"
        //     );
        //     if (!shouldSave)
        //     {
        //         return;
        //     }
        // }
        AssetDatabase.CreateAsset(so, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = so;
        EditorUtility.FocusProjectWindow();
        EditorGUIUtility.PingObject(so);
        //
        // var settings = new JsonSerializerSettings
        // {
        //     TypeNameHandling = TypeNameHandling.Auto,
        //     ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        //     Formatting = Formatting.Indented,
        // };
        // var saveData = new GraphSaveData();
        // saveData.graph = graph;
        // var json = JsonConvert.SerializeObject(saveData, settings);
        // File.WriteAllText(GetFullPath(fileName), json);
        //
        // Debug.Log($"Save:{fileName} {json}");
        // AssetDatabase.Refresh();
    }

#endif

    public static string ToJson(ActionGraphBase graph)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
        };
        var json = JsonConvert.SerializeObject(graph, settings);
        ActionGraphLog.Verbose(json);
        return json;
    }
}