using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[Serializable]
public class ActorListWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.ActorList;

    public override object Clone()
    {
        var clone = new ActorListWrapper();
        clone.internalValue = (Variable_ActorList) internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public List<GameActor> GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return null;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_ActorList).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue, ret);
#endif
        return ret;
    }
    
    // public List<GameActor> GetList(in ExecuteArgs executeArgs)
    // {
    //     if (internalValue == null)
    //     {
    //         ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
    //         return null;
    //     }
    //
    //     return (internalValue as Variable_ActorList).GetValue(executeArgs);
    // }
    //
    // public void SetValue(in ExecuteArgs executeArgs, GameActor value)
    // {
    //     if (internalValue == null)
    //     {
    //         ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
    //         return;
    //     }
    //
    //     if (internalValue is Variable_Actor_Direct direct)
    //     {
    //         direct.directValue = value;
    //     }
    // }

    public static implicit operator ActorListWrapper(Variable_ActorList var) => new() {internalValue = var};
}
