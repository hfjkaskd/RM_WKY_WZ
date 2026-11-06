using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Linq;
using System;
using System.Text;
using Bizza.ActionSystem;
using UnityEditor;


public class SearchMenuWindowProvider : ScriptableObject, ISearchWindowProvider
{
    public static List<Type> includeTypes;
    public static List<Type> excludeTypes;

    public class TmpNodeData
    {
        public string text;
        public int level;
        public bool leaf;
        public FlowChartView.CreateNodeArgs userData;

        public TmpNodeData parent;
        public List<TmpNodeData> children = new();

        private TmpNodeData GetChildOrCreateChild(string inText)
        {
            foreach (var v in children)
            {
                if (v.text == inText)
                {
                    return v;
                }
            }

            var child = new TmpNodeData() {text = inText, level = level + 1};
            child.parent = this;
            children.Add(child);
            return child;
        }

        public TmpNodeData CreateOrGet(string path)
        {
            var arr = path.Split('/');
            var tmp = this;
            for (int i = 0; i < arr.Length; i++)
            {
                var child = tmp.GetChildOrCreateChild(arr[i]);
                tmp = child;
            }
            return tmp;
        }
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var entries = new List<SearchTreeEntry>();
        // TmpNodeData root = new TmpNodeData(){level = 1, text = "行为节点"};

        if (includeTypes == null || includeTypes.Count == 0)
        {
            //动作节点
            {
                // AddByType(typeof(NodeBase), root);
                AddGroup(new List<Type>() {typeof(NodeBase), typeof(VariableBase)}, "节点和变量", entries);
            }

            //变量
            {
                // AddByType(typeof(VariableBase), root);
                // AddGroup(new List<Type>() {typeof(VariableBase)}, "变量", entries);
            }
        }
        else
        {
            AddGroup(includeTypes, "可用节点", entries);
            // foreach (var v in includeTypes)
            // {
                // AddByType(v, root);
            // }
        }

        // 优化一下层级
        // OptimizeTree(root);

        // RecursiveAddEntry(root, entries);
        return entries;
    }

    private void AddGroup(List<Type> types, string groupName, List<SearchTreeEntry> list)
    {
        TmpNodeData root = new TmpNodeData(){level = 1, text = groupName};
        foreach (var type in types)
        {
            AddByType(type, root);
        }
        OptimizeTree(root);

        RecursiveAddEntry(root, list);
    }

    private void RecursiveAddEntry(TmpNodeData node, List<SearchTreeEntry> list, int level = 1)
    {
        if (!node.leaf)
        {
            list.Add(new SearchTreeGroupEntry(new GUIContent(node.text)) {level = level});
        }
        else
        {
            list.Add(new SearchTreeEntry(new GUIContent(node.text)) {level = level, userData = node.userData});
        }

        node.children = node.children.OrderBy(x=>x.text).ToList();

        foreach (var v in node.children)
        {
            if (v.children.Count != 0)
            {
                RecursiveAddEntry(v, list, level + 1);
            }
        }

        foreach (var v in node.children)
        {
            if (v.children.Count == 0)
            {
                RecursiveAddEntry(v, list, level + 1);
            }
        }
    }

    private void OptimizeTree(TmpNodeData node)
    {
        if (node.parent != null && node.parent.children.Count == 1 && !node.leaf)
        {
            Debug.Log("OptimizeTree:" + node.text);
            node.parent.children.Clear();
            node.parent.children.AddRange(node.children);
            foreach (var child in node.children)
            {
                child.parent = node.parent;
            }

            node = node.parent;
        }

        foreach (var child in node.children.ToList())
        {
            OptimizeTree(child);
        }
    }

    private void AddByType(Type targetType, TmpNodeData root)
    {
        bool varOrFunc = targetType == typeof(VariableBase) || targetType.IsSubclassOf(typeof(VariableBase));
        var types = TypeCache.GetTypesDerivedFrom(targetType);
        StringBuilder sb = new();
        foreach (var type in types)
        {
            if (type.IsAbstract) continue;
            bool exclude = false;
            if (excludeTypes != null)
            {
                foreach (var v in excludeTypes)
                {
                    if (type.IsSubclassOf(v))
                    {
                        exclude = true;
                        break;
                    }
                }
            }

            if (exclude) continue;

            var attr = type.GetCustomAttribute<GraphElementInfoAttribute>();
            if (!GraphDebugUtils.CheckElementSupport(attr))
            {
                continue;
            }

            var path = GetFullPath(type);
            // Debug.LogError(path);
            sb.AppendLine(path);
            var tmp = root.CreateOrGet(path);
            tmp.leaf = true;
            tmp.userData = new FlowChartView.CreateNodeArgs() { varOrAction = varOrFunc, type = type};
        }

        Debug.Log(sb.ToString());
    }


    private List<Type> GetAllBaseTypes(Type type)
    {
        List<Type> baseTypes = new List<Type>();
        Type currentBaseType = type.BaseType;

        while (currentBaseType != null)
        {
            baseTypes.Add(currentBaseType);
            currentBaseType = currentBaseType.BaseType;
        }

        return baseTypes;
    }

    private string GetFullPath(Type type)
    {
        string path = "";
        List<Type> baseTypes = GetAllBaseTypes(type);
        baseTypes.Insert(0, type);
        baseTypes.Reverse();

        //组合分类
        foreach (var baseType in baseTypes)
        {
            var attr = baseType.GetCustomAttribute<GraphElementInfoAttribute>();
            if (attr != null)
            {
                if (path != "" && !string.IsNullOrEmpty(attr.Category) && !path.EndsWith("/"))
                {
                    path += "/";
                }

                path += attr.Category;
            }
        }

        //添加名称
        {
            var attr = type.GetCustomAttribute<GraphElementInfoAttribute>();
            path = path + "/" + attr.Text;
        }
        return path;
    }


    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        ActionGraphEditorWindow.flowChart.MenuAddNode(searchTreeEntry.userData);
        return true;
    }
}