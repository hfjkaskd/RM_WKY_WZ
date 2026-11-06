using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionGraphManager : SingletonCSharp<ActionGraphManager>
{
    public List<ActionGraphBase> AllRunningGraphs => _runningGraphs;
    private List<ActionGraphBase> _runningGraphs = new();

    public void Run(ActionGraphBase graph, ActionContextBase context, Dictionary<string, object> inParam = null)
    {
        if (graph == null)
        {
            ActionGraphLog.Error("graph null");
            return;
        }

        if (context == null)
        {
            ActionGraphLog.Error("context null");
            return;
        }

        var actionCmpt = context.self != null ? context.self.GetComponent<ActorCmpt_Action>() : null;
        if (context.self != null && actionCmpt == null)
        {
            ActionGraphLog.Error($"context.self.actionCmpt null {context.self.gameObject.name}");
            return;
        }

        // ActionGraphLog.Verbose($"开始执行图表:{graph.graphName} {graph.GetType().Name} {context.GetType().Name}");

#if UNITY_EDITOR
        if (_runningGraphs.Contains(graph))
        {
            LogLogger.LogError("error:重复运行graph");
            return;
        }
#endif

        ActionGraphUtils.InitGraph(graph);
        _runningGraphs.Add(graph);

        if (actionCmpt != null)
        {
            actionCmpt.OnGraphStart(graph);
        }
        graph.TriggerEnterEvent(ActionGraphUtils.GetEventActionArgs(graph, context));
    }

    public void Stop(object owner)
    {
        for (var i = _runningGraphs.Count - 1; i >= 0; i--)
        {
            var v = _runningGraphs[i];
            if (v.context == null || v.context.self == null)
            {
                continue;
            }

            if ((object)v.context.self == owner)
            {
                InternalStop(v, true);
            }
        }
    }

    public void Stop(ActionGraphBase graph)
    {
        if (graph == null)
        {
            ActionGraphLog.Warning("Stop graph null");
            return;
        }

        if (graph.CurState == E_ExecuteState.None)
        {
            return;
        }

        // graph.SetStopState();
        var idx = _runningGraphs.IndexOf(graph);

        if (idx != -1)
        {
            var v = _runningGraphs[idx];
            InternalStop(v, true);
        }
        // graph.root.Exit(ActionGraphUtils.GetEventActionArgs(graph));
    }

    private void InternalStop(ActionGraphBase graph, bool interrupt)
    {
        if (graph == null || graph.CurState == E_ExecuteState.WaitToDestroy || graph.CurState == E_ExecuteState.None)
        {
            return;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample("ActionGraph:Stop");
#endif
        if (!graph.autoReleaseToPool)
        {
            RemoveByGraph(graph);
        }
        graph.__InternalStop(interrupt);
        if (graph.autoReleaseToPool)
        {
            _toProcessList.Add((graph, false));
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
    }

    private List<(ActionGraphBase, bool)> _toProcessList = new();

    public void Update(float dt)
    {
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        if (GraphDebugUtils.debugGraphName != null)
        {
            GraphDebugUtils.actionDebugState.Clear();
        }
#endif
        for (var i = 0; i < _runningGraphs.Count; i++)
        {
            var v = _runningGraphs[i];
            if (!v.IsValid)
            {
                _toProcessList.Add((v, false));
                continue;
            }

            if (World.Current.IsPause && !v.UpdateInGamePause)
            {
                continue;
            }

            var ret = UpdateGraph(v, dt);
            if (ret == E_ExecuteState.Success || ret == E_ExecuteState.Failed)
            {
                InternalStop(v, false);
            }
        }

        if (_toProcessList.Count != 0)
        {
            for (var i = _toProcessList.Count - 1; i >= 0; i--)
            {
                var v = _toProcessList[i];
                if (!v.Item2)
                {
                    RemoveByGraph(v.Item1);
                }
            }
        }
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        GraphDebugUtils.subDataToEditor?.Invoke();
#endif

        _toProcessList.Clear();
    }

    private void RemoveByGraph(ActionGraphBase graph)
    {
        if (graph == null)
        {
            ActionGraphLog.Warning("Remove graph null");
            return;
        }
        _runningGraphs.Remove(graph);
        if (graph.autoReleaseToPool)
        {
            GraphPoolUtils.Release(graph);
        }
    }

    private E_ExecuteState UpdateGraph(ActionGraphBase graph, float dt)
    {
        // if (graph is ActionGraphBattle battleGraph && (graph.context == null || graph.context.self == null))
        // {
            //  // LogUtil.Error($"graph update error null:{graph.graphName} {(graph.context == null)} {graph.CurState}");
            // Stop(graph);
            // return E_ExecuteState.Failed;
        // }

        try
        {
            //数据检查
            return graph.Execute(graph.context, dt);
        }
        catch (Exception e)
        {
            ActionGraphLog.Error(graph, e.ToString());
            return E_ExecuteState.Failed;
        }
    }
}
