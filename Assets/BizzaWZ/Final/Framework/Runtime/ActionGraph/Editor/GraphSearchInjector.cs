using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.SearchService;
using UnityEngine;

[InitializeOnLoad]
public class GraphSearchInjector
{
    private static string _prevSearchText;

    static GraphSearchInjector()
    {
        // 监听编辑器更新事件
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        // UnityEditor.SearchService.ProjectSearch.RegisterEngine(new GraphSearchEngine());
    }

    private static void OnEditorUpdate()
    {
        try
        {
            CustomSearchGraph();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private static EditorWindow projectWindow;
    private static int _frame;
    public static void CustomSearchGraph()
    {
        var _projectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
        if (projectWindow == null)
        {
            // 获取 Project 窗口实例
            projectWindow = EditorWindow.GetWindow(_projectBrowserType, false, null, true);;
        }

        if (projectWindow == null) return;

        var _searchFilterField = _projectBrowserType.GetField("m_SearchFieldText",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var searchText = _searchFilterField.GetValue(projectWindow) as string;
        if (string.IsNullOrEmpty(searchText))
        {
            return;
        }

        if (!searchText.StartsWith("graph:"))
        {
            return;
        }

        if (searchText != _prevSearchText)
        {
            _prevSearchText = searchText;
            _frame = -100;
        }

        _frame++;
        //todo:不然会被刷新掉
        if (!(_frame % 300 == 0))
        {
            return;
        }

        // if (searchText == _prevSearchText)
        // {
        //     return;
        // }

        _prevSearchText = searchText;
        var keywords = searchText.Replace("graph:", "");
        var _listAreaField = _projectBrowserType.GetField("m_ListArea",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var listArea = _listAreaField?.GetValue(projectWindow);

        var localAssets = listArea.GetType().GetField("m_LocalAssets", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(listArea);
        var filterHierarchy = localAssets.GetType().GetField("m_FilteredHierarchy", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(localAssets);
        var resultField = filterHierarchy.GetType().GetField("m_VisibleItems", BindingFlags.NonPublic | BindingFlags.Instance);
        var results = resultField.GetValue(filterHierarchy);

        Assembly assembly = typeof(UnityEditor.EditorUtility).Assembly;
        var filterHierarchyType = assembly.GetType("UnityEditor.FilteredHierarchy");
        var elementType = filterHierarchyType.GetNestedType("FilterResult", BindingFlags.Public);

        FieldInfo nameField = elementType.GetField("name", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo guidField = elementType.GetField("m_Guid", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo instanceField = elementType.GetField("instanceID", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo typeField = elementType.GetField("type", BindingFlags.Public | BindingFlags.Instance);

        var allGraphs =
            UnityEditor.AssetDatabase.FindAssets($"t:{typeof(GraphSO)}", new string[] {GraphSaveUtils.SavePath});

        List<(string, GraphSO)> graphResults = new List<(string, GraphSO)>();
        foreach (var v in allGraphs)
        {
            var so = UnityEditor.AssetDatabase.LoadAssetAtPath<GraphSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(v));
            if (so == null || so.graph == null) continue;
            if ((so.graph.note != null && so.graph.note.Contains(keywords)) || so.name.Contains(keywords))
            {
                graphResults.Add((v, so));
            }

            foreach (var node in so.graph.GetAllNodes())
            {
                if (node.GetType().Name == keywords)
                {
                    graphResults.Add((v, so));
                    break;
                }
                var attr = node.GetType().GetCustomAttribute<GraphElementInfoAttribute>();
                if (attr != null && attr.Text != null)
                {
                    if (attr.Text.Contains(keywords))
                    {
                        graphResults.Add((v, so));
                        break;
                    }
                }
            }

            foreach (var variable in so.graph.GetAllVariable())
            {
                if (variable is Variable_String_Direct stringDirect && stringDirect.directValue != null && stringDirect.directValue.Contains(keywords))
                {
                    graphResults.Add((v, so));
                    break;
                }
            }
        }

        var newArray = Array.CreateInstance(elementType, graphResults.Count);
        for (var i = 0; i < graphResults.Count; i++)
        {
            var v = graphResults[i];
            var newElement = Activator.CreateInstance(elementType);
            nameField.SetValue(newElement, v.Item2.name);
            guidField.SetValue(newElement, v.Item1);
            instanceField.SetValue(newElement, v.Item2.GetInstanceID());
            typeField.SetValue(newElement, HierarchyType.Assets);
            resultField.SetValue(filterHierarchy, newArray);
            newArray.SetValue(newElement, i);
        }

        // var method = listArea.GetType().GetMethod("SetupData");
        // method.Invoke(listArea, new object[] {true});
    }
}
