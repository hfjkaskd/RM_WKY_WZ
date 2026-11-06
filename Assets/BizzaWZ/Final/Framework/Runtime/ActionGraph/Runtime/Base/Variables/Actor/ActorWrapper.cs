using System;
using UnityEngine.Scripting;


[Preserve]
[Serializable]
public class ActorWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.Actor;

    public override object Clone()
    {
        var clone = new ActorWrapper();
        clone.internalValue = (Variable_Actor) internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public GameActor GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return null;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret= (internalValue as Variable_Actor).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue, ret);
#endif
        return ret;
    }

    public void SetValue(in ExecuteArgs executeArgs, GameActor value)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return;
        }

        if (internalValue is Variable_Actor_Direct direct)
        {
            direct.directValue = value;
        }
    }

    public static implicit operator ActorWrapper(Variable_Actor var) => new() {internalValue = var};
}


public static class ActorWrapperExt
{
    public static GameActor GetValueWithDefault(this ActorWrapper self, in ExecuteArgs executeArgs, GameActor defaultValue)
    {
        if (self.IsNull()) return defaultValue;
        return self.GetValue(executeArgs);
    }

    public static string ToString(this ActorWrapper self, in ExecuteArgs executeArgs)
    {
        if (self.IsNull()) return "Null";
        return self.GetValue(executeArgs).ToString();
    }
}