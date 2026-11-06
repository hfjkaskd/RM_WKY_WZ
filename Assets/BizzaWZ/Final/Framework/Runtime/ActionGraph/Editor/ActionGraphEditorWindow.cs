using System;
using System.Linq;
using Bizza;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class ActionGraphEditorWindow : EditorWindow
{
    enum E_OpenFileType
    {
        None,
        CreateNew,
        OpenFile,
        OpenGraph,
    }

    [MenuItem("工具/动作图/新建编辑器 %w")]
    public static void OpenNew()
    {
        if (Instance != null)
        {
            Instance.Close();
        }

        ActionGraphEditorWindow window = GetWindow<ActionGraphEditorWindow>();
        window.titleContent = new GUIContent("ActionEditorWindow");
    }

    [MenuItem("工具/动作图/打开上一个 %&w")]
    public static void OpenLast()
    {
        var graphName = EditorPrefs.GetString("ActionGraph_LastOpenFileName");
        ShowWindow(graphName);
    }

    public static void ShowWindow(string fileName)
    {
        if (Instance != null)
        {
            Instance.Close();
        }

        _openFileType = E_OpenFileType.OpenFile;
        _toOpenFile = fileName;

        //修正路径
        {
            string[] guids = AssetDatabase.FindAssets("t:GraphSO", new[] { GraphSaveUtils.SavePath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GraphSO graphSO = AssetDatabase.LoadAssetAtPath<GraphSO>(path);
                if (graphSO.name == fileName)
                {
                    _toOpenFile = path;
                }
            }
        }

        ActionGraphEditorWindow window = GetWindow<ActionGraphEditorWindow>();
        window.titleContent = new GUIContent("ActionEditorWindow");
    }

    public static void ShowWindowWithDebug(ActionGraphBase graph, string graphName, GameActor owner)
    {
        if (Instance != null)
        {
            Instance.Close();
        }
        GraphDebugUtils.variableDebugValues.Clear();
        GraphDebugUtils.debugGraphName = graphName;
        GraphDebugUtils.debugOwner = owner;
        if (graph != null)
        {
            ShowWindowGraph(graph);
        }
        else
        {
            ShowWindow(graphName);
        }
    }

    public static void ShowWindowGraph(ActionGraphBase graph)
    {
        _openFileType = E_OpenFileType.OpenGraph;
        _toOpenGraph = graph;
        ActionGraphEditorWindow window = GetWindow<ActionGraphEditorWindow>();
        window.titleContent = new GUIContent("ActionEditorWindow");
        // flowChart.Read(graph);
    }

    public static ActionGraphEditorWindow Instance;
    public static InspectorView inspector;
    public static FlowChartView flowChart;
    private static E_OpenFileType _openFileType;
    private static string _toOpenFile = "";
    private static ActionGraphBase _toOpenGraph;

    public void CreateGUI()
    {
        Instance = this;
        VisualElement root = rootVisualElement;

        var visualTree =
            EditorUtils.GetObjBySamePathScript<VisualTreeAsset>(nameof(ActionGraphEditorWindow),
                nameof(ActionGraphEditorWindow) + ".uxml");
        visualTree.CloneTree(root);

        var styleSheet =
            EditorUtils.GetObjBySamePathScript<StyleSheet>(nameof(ActionGraphEditorWindow),
                nameof(ActionGraphEditorWindow) + ".uss");
        root.styleSheets.Add(styleSheet);

        flowChart = root.Q<FlowChartView>();
        inspector = root.Q<InspectorView>();


        flowChart.window = this;

        flowChart.Init();


        var curOpenFileType = _openFileType;
        _openFileType = E_OpenFileType.None;
        if (curOpenFileType == E_OpenFileType.CreateNew)
        {
            flowChart.CreateNew();
        }
        else if(curOpenFileType == E_OpenFileType.OpenFile)
        {
            // if (string.IsNullOrEmpty(_toOpenFile))
            // {
            //     if (Selection.activeObject != null && Selection.activeObject is GraphSO graphSO)
            //     {
            //         _toOpenFile = AssetDatabase.GetAssetPath(graphSO);
            //     }
            // }
            if (!string.IsNullOrEmpty(_toOpenFile))
            {
                var tmp = _toOpenFile;
                _toOpenFile = "";
                try
                {
                    flowChart.Read(tmp);
                }
                catch (Exception e)
                {
                    flowChart.Read("");
                    EditorUtility.DisplayDialog("", "读取失败", "OK");
                    LogLogger.LogError(e);
                }
            }
        }
        else if (curOpenFileType == E_OpenFileType.OpenGraph)
        {
            flowChart.Read(_toOpenGraph);
        }
    }

    void OnGUI()
    {
        flowChart?.OnGUI();
    }

    private void OnDestroy()
    {
        flowChart?.Dispose();
        ActionDataForEditor.curGraph = null;
        Instance = null;
    }
}
