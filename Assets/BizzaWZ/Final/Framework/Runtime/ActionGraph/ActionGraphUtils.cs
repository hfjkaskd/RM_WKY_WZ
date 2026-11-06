using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 动作图表的公共接口
/// </summary>
 [Obfuz.ObfuzIgnore]
public static class ActionGraphUtils
{
    #region 常用接口
    /// <summary>
    /// 全部图标
    /// </summary>
    public static IReadOnlyDictionary<string, GraphSO> AllGraphs => _allGraphs;
    private static Dictionary<string, GraphSO> _allGraphs = new();

    /// <summary>
    /// 根据名称执行一个图表
    /// </summary>
    /// <param name="fileName">图表名，so文件的名称（无后缀）</param>
    /// <param name="context">上下文，不同的图表需要不同的上下文，如ActionGraph_Buff需要提供ActionContext_Buff，Context需要使用对象池</param>
    /// <returns>执行成功，则返回图表，失败返回null</returns>
    public static ActionGraphBase Run(string fileName, ActionContextBase context, Action<ActionGraphBase> onEnd = null)
    {
        if (ActionModule.Instance == null)
        {
            ActionGraphLog.Error("ActionModule尚未准备就绪");
            return null;
        }

        var graph = ActionModule.Instance.Run(fileName, context);
        if (graph != null && onEnd != null)
        {
            graph.onEnd += onEnd;
        }

        return graph;
    }

    /// <summary>
    /// 执行一个已知图表
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="context">上下文，不同的图表需要不同的上下文，如ActionGraph_Buff需要提供ActionContext_Buff，Context需要使用对象池</param>
    public static void Run(ActionGraphBase graph, ActionContextBase context, Action<ActionGraphBase> onEnd = null)
    {
        if (graph == null)
        {
            ActionGraphLog.Error("执行图表错误:graph null");
            return;
        }

        if (ActionModule.Instance == null)
        {
            ActionGraphLog.Error("ActionModule尚未准备就绪");
            return;
        }

        ActionModule.Instance.Run(graph, context);
        if (graph != null && onEnd != null)
        {
            graph.onEnd += onEnd;
        }
    }

    /// <summary>
    /// 停止一个图表，如停止单位行为树，提前结束buff
    /// </summary>
    /// <param name="graph"></param>
    public static void Stop(ActionGraphBase graph)
    {
        if (ActionModule.Instance == null)
        {
            ActionGraphLog.Error("ActionModule尚未准备就绪");
            return;
        }

        ActionModule.Instance.Stop(graph);
    }

    public static Dictionary<string, ActionGraphBase> newGraphs = new();

    public static ActionGraphBase Get(string fileName, bool autoRelease = true)
    {
#if UNITY_EDITOR
        if (newGraphs.ContainsKey(fileName))
        {
            return (ActionGraphBase)newGraphs[fileName].Clone();
        }
#endif
        var graphSO = ActionGraphUtils.GetGraphSO(fileName);

#if UNITY_EDITOR
        if (graphSO == null)
        {
            graphSO = (GraphSO)EditorUtils.FindObjectWithName(nameof(GraphSO), fileName);
            if (graphSO != null)
            {
                ActionGraphLog.Error($"使用了编辑器的Graph:{fileName}");
            }
        }
#endif

        if (graphSO == null)
        {
            ActionGraphLog.Error($"加载Graph文件失败:{fileName}");
            return null;
        }

        var graph = GraphPoolUtils.Get(graphSO.graph);

        if (graph != null)
        {
            graph.graphName = fileName;
            InitGraph(graph);

            graph.autoReleaseToPool = autoRelease;
        }

        return graph;
    }

    private static bool _loadingFinish;
    /// <summary>
    /// 加载Graph资源
    /// </summary>
    [Obfuz.ObfuzIgnore]
    public static async UniTask Prewarm(Action onComplete = null)
    {
// #if !UNITY_EDITOR
//         List<GraphSO> list = new();
//         var allGraphs = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(GraphSO)}", new string[] {GraphSaveUtils.SavePath});
//         foreach (var v in allGraphs)
//         {
//             var so = UnityEditor.AssetDatabase.LoadAssetAtPath<GraphSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(v));
//             list.Add(so);
//         }
//         OnLoaded("", list, onComplete);
//         _loadingFinish = true;
// #else
        // var ret = await AssetUtils.LoadAssetsAsync<GraphSO>("Actions");
        var ret = Resources.LoadAll<GraphSO>("Actions");
        LogLogger.LogVerbose(LogTag.LOG_Asset, "Graph 数量 " + ret.Length);
        OnLoaded("", ret, onComplete);
        _loadingFinish = true;
        // AssetModule.LoadAsset<GraphListSO>(nameof(GraphListSO), (path, ret) =>
        // {
            // OnLoaded(path, ret.graphList, onComplete);
            // _loadingFinish = true;
        // });
// #endif
        await UniTask.WaitUntil(() => _loadingFinish);
    }

    #endregion

    /// <summary>
    /// 根据图标构建ExecuteArgs
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    internal static ExecuteArgs GetEventActionArgs(ActionGraphBase graph, ActionContextBase context = null)
    {
        var args = new ExecuteArgs()
        {
            context = context ?? graph.context,
            graph = graph,
            deltaTime = Time.deltaTime,
        };

        if (args.context == null)
        {
            ActionGraphLog.Error($"当前图表尚未运行，需要传入context {graph.graphName}");
        }
        return args;
    }

    internal static void InitGraph(ActionGraphBase graph)
    {
        graph.OnReset();
        // if (graph.defaultVariableList != null)
        // {
        //     foreach (var v in graph.defaultVariableList)
        //     {
        //         if (v == null) continue;
        //         v.OnReset();
        //     }
        // }
        //
        // if (graph.variableList != null)
        // {
        //     foreach (var v in graph.variableList)
        //     {
        //         if (v == null) continue;
        //         v.OnReset();
        //     }
        // }


        var root = graph.root;
        if (root != null)
        {
            RecursiveInitNode(root, graph);
        }

        if (graph.callbackRoots == null)
        {
            graph.callbackRoots = new();
        }
        else
        {
            graph.callbackRoots.Clear();
        }
        foreach (var v in graph.unconnectedNodes)
        {
            if (v is OutputActionBase eventAction)
            {
                RecursiveInitNode(eventAction, graph);
                graph.callbackRoots.Add(eventAction);
            }
        }
    }

    private static void RecursiveInitNode(NodeBase node, ActionGraphBase graph)
    {
        if (node != null)
        {
            node.belongGraph = graph;
            node.OnReset();
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    if (child == null)
                    {
                        ActionGraphLog.Error(graph, $"图表节点数据丢失");
                        continue;
                    }

                    child.parent = node;
                    RecursiveInitNode(child, graph);
                }
            }
        }
    }

    public static GraphSO GetGraphSO(string fileName)
    {
        _allGraphs.TryGetValue(fileName, out var ret);
        return ret;
    }

    
    private static void OnLoaded(string label, IList<GraphSO> configSO, Action onComplete)
    {
        foreach (var v in configSO)
        {
            if (v == null || v.graph == null) continue;

            var resPath = v.name;
            if (_allGraphs.ContainsKey(resPath))
            {
                ActionGraphLog.Error($"不支持多个图表使用相同名称：{resPath}");
            }

            if (v != null && v.graph != null)
            {
                v.graph.graphName = v.name;
            }
            _allGraphs[resPath] = v;
        }
        LogLogger.LogInfo($"loadFinish: graphNum={_allGraphs.Count}");
        onComplete?.Invoke();
        LogLogger.LogInfo("onComplete");
#if UNITY_EDITOR
        //检查Clone错误
        foreach (var v in _allGraphs)
        {
            var source = v.Value.graph;
            if (source != null)
            {
                try
                {
                    var clone = source.Clone() as ActionGraphBase;
                    GraphDebugUtils.CheckGraphClone(source, clone, v.Key);
                }
                catch (Exception e)
                {
                    Debug.LogError($"图表存在错误{v.Value.name} {e}");
                }
            }
        }
#endif
    }
}
