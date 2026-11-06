using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;

public class ActionModule : BaseGameModule<ActionModule>, IUpdate
{
    [HideLabel][InlineProperty]
    public GraphBlackBoard blackBoard = new GraphBlackBoard();

    [HideLabel][InlineProperty]
    public GraphBlackBoard battleGlobal = new GraphBlackBoard();

    private ActionGraphManager _graphManager;

    private Action<ActionGraphBase> _onGraphEnd;

    [ShowInInspector][LabelText("正在运行的图表数量")]
    public int DebugRunningGraphNum
    {
        get
        {
            if (_graphManager == null) return 0;
            return _graphManager.AllRunningGraphs.Count;
        }
    }

    [CustomValueDrawer("CustomValueDrawer")]
    [ShowInInspector]
    public List<ActionGraphBase> runningGraphs
    {
        get => _graphManager.AllRunningGraphs;
        set {}
    }

#if UNITY_EDITOR
    private static ActionGraphBase CustomValueDrawer(ActionGraphBase v, GUIContent label)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(v.graphName))
        {
            GraphDebugUtils.DebugGraph(v, v.context.self);
        }

        GUILayout.EndHorizontal();
        return v;
    }
#endif
    // [ShowInInspector][LabelText("正在运行的图表")]
    // public List<string> debugGraphs
    // {
    //     get
    //     {
    //         if (_graphManager == null) return new List<string>();
    //         List<string> ret = new List<string>();
    //         foreach (var v in _graphManager.AllRunningGraphs)
    //         {
    //             ret.Add(v.graphName);
    //         }
    //
    //         return ret;
    //     }
    // }



    public override void InitGameModule()
    {
        base.InitGameModule();
        _graphManager = new ActionGraphManager();
        _onGraphEnd = OnGraphEnd;
#if UNITY_EDITOR

        foreach (var v in ActionGraphUtils.AllGraphs)
        {
            v.Value.graph.CheckValid();
        }

        Application.logMessageReceived += HandleLog;
#endif
    }

    public override void ReleaseGameModule()
    {
        base.ReleaseGameModule();
#if UNITY_EDITOR
        Application.logMessageReceived -= HandleLog;
#endif
    }

    private void HandleLog(string condition, string stacktrace, LogType type)
    {
        if (condition.Contains("ActionGraph"))
        {
            var idx1 = condition.IndexOf("$$$", StringComparison.Ordinal);
            var idx2 = condition.IndexOf("$$$", idx1 + 3, StringComparison.Ordinal);
            if (idx1 != -1 && idx2 != -1)
            {
                var graphName = condition.Substring(idx1 + 3, idx2 - idx1 - 3);
                Debug.LogError($"图表出现错误=>{graphName},ctrl+alt+w快速打开报错图表");
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetString("ActionGraph_LastOpenFileName", graphName);
#endif
            }
        }
    }

    protected override void SetListener(bool addOrRemove)
    {
        base.SetListener(addOrRemove);
        BizzaEventSystem.Set(EventDefine.Frame.ChangeGameMode, OnGameModeChange, addOrRemove);
    }

    private void OnGameModeChange(E_GameModeType newMode, E_GameModeType oldMode)
    {
        GraphPoolUtils.Clear();
    }


    public bool Enabled => true;
    public void OnUpdate(float delta)
    {
#if ENABLE_ACTION_GRAPH_PROFILER
        Profiler.BeginSample("ActionGraph");
        Profiler.BeginSample("ActionGraph:Update");
#endif

        _graphManager?.Update(delta);

#if ENABLE_ACTION_GRAPH_PROFILER
        Profiler.EndSample();
        Profiler.EndSample();
#endif
    }

    public ActionGraphBase Run(string fileName, ActionContextBase context)
    {
        if (_graphManager == null)
        {
            ActionGraphLog.Error("_graphManager null");
            return null;
        }

        if (string.IsNullOrEmpty(fileName))
        {
            ActionGraphLog.Error("FileName null");
            return null;
        }

        if (context == null)
        {
            ActionGraphLog.Error("context is null");
            return null;
        }

        var graph = ActionGraphUtils.Get(fileName);
        if (graph == null)
        {
            ActionGraphLog.Error($"读取Graph文件失败:{fileName}，请检查文件名是否正确/文件是否存在");
            return null;
        }

        Run(graph, context);

        return graph;
    }

    public void Run(ActionGraphBase graph, ActionContextBase context)
    {
        graph.context = context;
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample("ActionGraph:Run");
#endif
        _graphManager?.Run(graph, context);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
        graph.onEnd += _onGraphEnd;
    }

    private void OnGraphEnd(ActionGraphBase graph)
    {
        if (graph == null) return;

        if (graph.context != null && graph.context.self != null)
        {
            graph.context.self.GetComponent<ActorCmpt_Action>()?.OnGraphEnd(graph);
        }

        // if (graph == GraphDebugUtils.curDebugGraph)
        // {
            // GraphDebugUtils.curDebugGraph = null;
        // }
    }

    public void Stop(object owner)
    {
        _graphManager?.Stop(owner);
    }

    public void Stop(ActionGraphBase graph)
    {
        _graphManager?.Stop(graph);
    }

}
