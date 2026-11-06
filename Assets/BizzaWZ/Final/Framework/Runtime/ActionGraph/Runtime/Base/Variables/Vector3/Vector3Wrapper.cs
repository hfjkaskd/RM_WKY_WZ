using System;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[Serializable]
public class Vector3Wrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.Vector3;

    public override object Clone()
    {
        var clone = new Vector3Wrapper();
        clone.internalValue = (Variable_Vector3) internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return Vector3.zero;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_Vector3).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue,ret);
#endif
        return ret;
    }

    public void SetValue(in ExecuteArgs executeArgs, Vector3 value)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return;
        }

        if (internalValue is Variable_Vector3_Direct direct)
        {
            direct.directValue = value;
        }
    }

    public static implicit operator Vector3Wrapper(Variable_Vector3 var) => new Vector3Wrapper() {internalValue = var};
}

public static class Vector3WrapperExt
{
    public static Vector3 GetValueWithDefault(this Vector3Wrapper self, in ExecuteArgs executeArgs, Vector3 defaultValue)
    {
        if (self.IsNull()) return defaultValue;
        return self.GetValue(executeArgs);
    }

    public static string ToString(this Vector3Wrapper self, in ExecuteArgs executeArgs)
    {
        if (self.IsNull()) return "Null";
        return self.GetValue(executeArgs).ToString();
    }
}