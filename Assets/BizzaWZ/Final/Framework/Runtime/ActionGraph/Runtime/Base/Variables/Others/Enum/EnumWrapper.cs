using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[Serializable]
public class EnumWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.Enum;

    public override object Clone()
    {
        var clone = new EnumWrapper();
        clone.internalValue = (Variable_Enum)internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public int GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return 0;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_Enum).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue, ret);
#endif
        return ret;
    }

    public static implicit operator EnumWrapper(Variable_Enum var) => new() {internalValue = var};
}

public static class EnumWrapperExt
{
    public static int GetValueWithDefault(this EnumWrapper self, in ExecuteArgs executeArgs, int defaultValue)
    {
        if (self == null || self.internalValue == null) return defaultValue;
        return self.GetValue(executeArgs);
    }
}