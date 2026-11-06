using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class GraphDebugUtils
{
    #region Debug


#if UNITY_EDITOR
    // public static string debugGraphName = "";
    private static ActionGraphBase _debugGraph;
    public static ActionGraphBase debugGraph
    {
        set
        {
            if (_debugGraph != value)
            {
                _debugGraph = value;
            }
        }
        get
        {
            return _debugGraph;
        }
    }
    public static string debugGraphName;
    public static GameActor debugOwner;
    public static Action subDataToEditor;
    public static Dictionary<string, E_DebugExecuteState> actionDebugState = new();
    public static Dictionary<string, object> variableDebugValues = new();
    public static Func<NodeBase, bool> IsSelect;

    public static bool IsDebugSelected(this NodeBase node)
    {
        if (IsSelect == null) return false;
        return IsSelect.Invoke(node);
    }

    public static void UpdateNodeExecuteState(NodeBase node, ExecuteArgs executeArgs, E_ExecuteState ret)
    {
        // if (executeArgs.graph.graphName == debugGraphName)
        {
            if (ret == E_ExecuteState.Running && node._lastFrameRet != E_ExecuteState.Running)
            {
                // actionDebugState[node.nodeGuid] = E_DebugExecuteState.Start;
                node.debugState = E_DebugExecuteState.Start;
            }
            else
            {
                // actionDebugState[node.nodeGuid] = (E_DebugExecuteState)(int)ret;
                node.debugState = (E_DebugExecuteState)(int)ret;
            }
            node.lastHasStateFrame = Time.frameCount;
        }
    }
#endif

    public static bool CheckElementSupport(GraphElementInfoAttribute elementInfo)
    {
        if (ActionDataForEditor.curGraph == null)
        {
            ActionGraphLog.Error("只能在ActionEditorWindow中使用");
            return false;
        }

        if (elementInfo == null)
        {
            return false;
        }

        if (elementInfo.SupportTypes == null)
        {
            return false;
        }

        return elementInfo.SupportTypes.Any(supportType => supportType.IsInstanceOfType(ActionDataForEditor.curGraph));
    }

    public static void OpenGraph(string fileName)
    {
        try
        {
            // 获取当前加载的所有程序集
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                // 获取 ActionGraphEditorWindow 类型
                Type actionGraphEditorWindowType = assembly.GetType("ActionGraphEditorWindow");
                if (actionGraphEditorWindowType != null)
                {
                    // 获取 FunctionA 方法
                    MethodInfo functionAMethod =
                        actionGraphEditorWindowType.GetMethod("ShowWindow",
                            BindingFlags.Static | BindingFlags.Public);
                    if (functionAMethod != null)
                    {
                        // 调用 FunctionA 方法
                        functionAMethod.Invoke(null, new object[] {fileName});
                    }
                    else
                    {
                        Debug.LogError("未找到 FunctionA 方法");
                    }

                    return;
                }
            }

            Debug.LogError("未找到 ActionGraphEditorWindow 类型");
        }
        catch (Exception e)
        {
            Debug.LogError($"调用 FunctionA 时发生错误: {e.Message}");
        }
    }

    public static void DebugGraph(ActionGraphBase graph, GameActor owner)
    {
        #if UNITY_EDITOR
        debugGraph = graph;
        string graphName = graph.graphName;
        try
        {
            // 获取当前加载的所有程序集
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                // 获取 ActionGraphEditorWindow 类型
                Type actionGraphEditorWindowType = assembly.GetType("ActionGraphEditorWindow");
                if (actionGraphEditorWindowType != null)
                {
                    // 获取 FunctionA 方法
                    MethodInfo functionAMethod =
                        actionGraphEditorWindowType.GetMethod("ShowWindowWithDebug",
                            BindingFlags.Static | BindingFlags.Public);
                    if (functionAMethod != null)
                    {
                        // 调用 FunctionA 方法
                        functionAMethod.Invoke(null, new object[] {graph, graphName, owner});
                    }
                    else
                    {
                    }
                }
            }
        }
        catch (Exception e)
        {
        }

#endif
    }

    #endregion

    public class VariableViewData
    {
        public object instance;
        public FieldInfo fieldInfo;

        public object fieldValue
        {
            get => fieldInfo.GetValue(instance);
            set => fieldInfo.SetValue(instance, value);
        }
}

    public static List<VariableViewData> GetAllVariableInClass<T>(object instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        Type targetType = instance.GetType();

        // 获取所有实例字段（包括公共和非公共）
        FieldInfo[] allFields = targetType.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        var ret = new List<VariableViewData>();
        foreach (var field in allFields)
        {
            if (field.FieldType.IsSubclassOf(typeof(T)))
            {
                ret.Add(new VariableViewData()
                {
                    instance = instance,
                    fieldInfo = field,
                });
            }
        }
        // 筛选出类型为TypeA的字段，并获取它们的值
        return ret;
    }

    public static void CheckGraphClone(ActionGraphBase source, ActionGraphBase clone, string name, bool showTipsWindow = false)
    {
        var json1 = GraphSaveUtils.ToJson(source);
        var json2 = GraphSaveUtils.ToJson(clone);
        if (json1 != json2)
        {
            LogLogger.LogError($"CloneGraph错误:{name}");
            LogLogger.LogError(json1);
            LogLogger.LogError(json2);

            #if UNITY_EDITOR
            if (showTipsWindow)
            {
                bool shouldSave = UnityEditor.EditorUtility.DisplayDialog(
                    "",
                    "保存出现错误，请检查日志（通常是Clone函数漏掉了）",
                    "ok"
                );
            }
            #endif
        }
    }
}