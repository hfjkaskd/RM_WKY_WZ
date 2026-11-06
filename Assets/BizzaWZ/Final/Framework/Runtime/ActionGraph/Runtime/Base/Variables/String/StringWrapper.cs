using System;
using UnityEngine.Scripting;


[Preserve]
[Serializable]
public class StringWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.String;

    public override object Clone()
    {
        var clone = new StringWrapper();
        clone.internalValue = (Variable_String) internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public string GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return "";
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_String).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue, ret);
#endif
        return ret;
    }

    public void SetValue(in ExecuteArgs executeArgs, string value)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return;
        }

        if (internalValue is Variable_String_Direct direct)
        {
            direct.directValue = value;
        }
    }

    public static implicit operator StringWrapper(Variable_String var) => new() {internalValue = var};

}


public static class StringWrapperExt
{
    public static string GetValueWithDefault(this StringWrapper self, in ExecuteArgs executeArgs, string defaultValue)
    {
        if (self.IsNull()) return defaultValue;
        return self.GetValue(executeArgs);
    }

    public static string ToString(this StringWrapper self, in ExecuteArgs executeArgs)
    {
        if (self.IsNull()) return "Null";
        return self.GetValue(executeArgs).ToString();
    }
}