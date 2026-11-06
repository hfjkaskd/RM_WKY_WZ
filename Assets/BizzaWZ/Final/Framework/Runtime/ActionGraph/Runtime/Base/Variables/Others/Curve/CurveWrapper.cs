using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Sirenix.OdinInspector.HideMonoScript]
[Preserve]
[Serializable]
public class CurveWrapper : VariableWrapperBase
{
    public override E_VariableType VariableType => E_VariableType.Curve;

    public override object Clone()
    {
        var clone = new CurveWrapper();
        clone.internalValue = (Variable_Curve)internalValue?.Clone();
        clone.idxInList = idxInList;
        clone.nodeIdx = nodeIdx;
        clone.nodeVarIdx = nodeVarIdx;
        return clone;
    }

    public AnimationCurve GetValue(in ExecuteArgs executeArgs)
    {
        if (internalValue == null)
        {
            ActionGraphLog.Error($"internalValue null:{executeArgs.graph.graphName}");
            return null;
        }
#if ENABLE_ACTION_GRAPH_PROFILER
            UnityEngine.Profiling.Profiler.BeginSample(internalValue.ProfilerDebugName);
#endif
        var ret = (internalValue as Variable_Curve).GetValue(executeArgs);
#if ENABLE_ACTION_GRAPH_PROFILER
        UnityEngine.Profiling.Profiler.EndSample();
#endif
#if UNITY_EDITOR && !ENABLE_ACTION_GRAPH_PROFILER
        OnGetValue(internalValue, ret);
#endif
        return ret;
    }

    public static implicit operator CurveWrapper(Variable_Curve var) => new() {internalValue = var};
}

public static class CurveWrapperExt
{
    public static AnimationCurve GetValueWithDefault(this CurveWrapper self, in ExecuteArgs executeArgs, AnimationCurve defaultValue)
    {
        if (self == null || self.internalValue == null) return defaultValue;
        return self.GetValue(executeArgs);
    }
}