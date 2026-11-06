using System;
using UnityEngine;
using UnityEngine.Scripting;


[Sirenix.OdinInspector.HideMonoScript]
[Preserve]
[Serializable]
public class FloatWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.Float;

    public override object Clone()
    {
        var clone = new FloatWrapper();
        clone.internalValue = (Variable_Float)internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public int GetValueInt(in ExecuteArgs executeArgs)
    {
        return Mathf.RoundToInt(GetValue(executeArgs));
    }

    public float GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return 0;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_Float).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue, ret);
#endif
        return ret;
    }

    public void SetValue(in ExecuteArgs executeArgs, float value)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return;
        }

        if (internalValue is Variable_Float_Direct direct)
        {
            direct.directValue = value;
        }
    }

    public static implicit operator FloatWrapper(Variable_Float var) => new() {internalValue = var};

}

public static class FloatWrapperExt
{
    public static float GetValueWithDefault(this FloatWrapper self, in ExecuteArgs executeArgs, float defaultValue)
    {
        if (self.IsNull()) return defaultValue;
        return self.GetValue(executeArgs);
    }

    public static int GetValueIntWithDefault(this FloatWrapper self, in ExecuteArgs executeArgs, float defaultValue)
    {
        return Mathf.RoundToInt(GetValueWithDefault(self, executeArgs, defaultValue));
    }

    public static string ToString(this FloatWrapper self, in ExecuteArgs executeArgs)
    {
        if (self.IsNull()) return "Null";
        return self.GetValue(executeArgs).ToString();
    }
}