using System;
using UnityEngine.Scripting;

[Preserve]
[Serializable]
public class BoolWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.Bool;

    public override object Clone()
    {
        var clone = new BoolWrapper();
        clone.internalValue = (Variable_Bool)internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public bool GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return false;
        }

#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_Bool).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue, ret);
#endif
        return ret;
    }

    public void SetValue(in ExecuteArgs executeArgs, bool value)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return;
        }

        if (internalValue is Variable_Bool_Direct direct)
        {
            direct.directValue = value;
        }
    }

    public static implicit operator BoolWrapper(Variable_Bool var) => new() {internalValue = var};
}

public static class BoolWrapperExt
{
    public static bool GetValueWithDefault(this BoolWrapper self, in ExecuteArgs executeArgs, bool defaultValue)
    {
        if (self.IsNull()) return defaultValue;
        return self.GetValue(executeArgs);
    }

    public static string ToString(this BoolWrapper self, in ExecuteArgs executeArgs)
    {
        if (self.IsNull()) return "Null";
        return self.GetValue(executeArgs).ToString();
    }
}