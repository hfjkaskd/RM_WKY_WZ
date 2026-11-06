using System;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[Serializable]
public class TransformWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.Transform;

    public override object Clone()
    {
        var clone = new TransformWrapper();
        clone.internalValue = (Variable_Transform) internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public Transform GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return null;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_Transform).GetValue(executeArgs);
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

    public static implicit operator TransformWrapper(Variable_Transform var) => new() {internalValue = var};

}


public static class TransformWrapperExt
{
    public static Transform GetValueWithDefault(this TransformWrapper self, in ExecuteArgs executeArgs, Transform defaultValue)
    {
        if (self.IsNull()) return defaultValue;
        return self.GetValue(executeArgs);
    }

    public static string ToString(this TransformWrapper self, in ExecuteArgs executeArgs)
    {
        if (self.IsNull()) return "Null";
        return self.GetValue(executeArgs).ToString();
    }
}