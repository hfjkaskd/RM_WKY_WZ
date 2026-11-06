using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public static class ActionCallback
{
    //
    // public static void TriggerEvent_Actor(E_GraphEvent_Unit eventName, ActionContext_Unit context, ActorCmpt_Action actionCmpt)
    // {
    //     TriggerEvent<ActionGraph_UnitBT>((int)eventName, context, actionCmpt);
    // }
    //
    // public static void TriggerEvent_Buff(E_GraphEvent_Buff eventName, ActionContext_Buff context, ActorCmpt_Action actionCmpt)
    // {
    //     TriggerEvent<ActionGraph_Buff>((int)eventName, context, actionCmpt);
    // }
    //
    // public static void TriggerEvent_Bullet(E_GraphEvent_Bullet eventName, ActionContext_Bullet context, ActorCmpt_Action actionCmpt)
    // {
    //     TriggerEvent<ActionGraph_BulletBT>((int)eventName, context, actionCmpt);
    // }
    //
    // public static void TriggerEvent_BulletBuff(E_GraphEvent_BulletBuff eventName, ActionContext_Buff context, ActorCmpt_Action actionCmpt)
    // {
    //     TriggerEvent<ActionGraph_BulletBuff>((int)eventName, context, actionCmpt);
    // }
    //
    // public static void TriggerEvent_Skill(E_GraphEvent_Skill eventName, ActionContext_Skill context, ActorCmpt_Action actionCmpt)
    // {
    //     TriggerEvent<ActionGraph_Skill>((int)eventName, context, actionCmpt);
    // }


    public static void TriggerEvent<T>(int eventName, ActionContextBase context, ActorCmpt_Action actionCmpt) where T : ActionGraphBase
    {
        context.self = actionCmpt.Owner;
        // ActionGraphLog.Verbose($"TriggerEvent {graphType.ToString()} {eventName}");
        for (var i = actionCmpt.runningGraphs.Count - 1; i >= 0; i--)
        {
            var graph = actionCmpt.runningGraphs[i];
            if (graph is not T)
            {
                continue;
            }
            TriggerEvent(graph, eventName, context);
        }
    }

    public static void TriggerEvent(ActionGraphBase graph, int eventName, ActionContextBase context)
    {
        if (graph == null || graph.callbackRoots == null || graph.callbackRoots.Count == 0)
        {
            // ActionGraphLog.Error($"event not exist:{eventName}");
            return;
        }

        OutputActionBase actionNode = null;
        foreach (var v in graph.callbackRoots)
        {
            if (v.EventName == eventName)
            {
                actionNode = v;
            }
        }

        if (actionNode == null)
        {
            return;
        }
        if (graph.CurState == E_ExecuteState.None || graph.CurState == E_ExecuteState.WaitToDestroy)
        {
            return;
        }

        try
        {
#if ENABLE_ACTION_GRAPH_PROFILER
            Profiler.BeginSample("ActionGraph");
            Profiler.BeginSample("ActionGraph:Event");
#endif
            actionNode.Execute(new ExecuteArgs()
            {
                graph = graph,
                context = context,
                deltaTime = Time.deltaTime,
            });
#if ENABLE_ACTION_GRAPH_PROFILER
            Profiler.EndSample();
            Profiler.EndSample();
#endif
        }
        catch (Exception e)
        {
            LogLogger.LogError($"TriggerEvent exception, graph:{graph.graphName} event:{eventName}: {e}");
        }
    }
}
