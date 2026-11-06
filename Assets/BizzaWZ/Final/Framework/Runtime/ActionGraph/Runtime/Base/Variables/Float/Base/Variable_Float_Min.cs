using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "较小值", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_Min : Variable_Float
{
    public FloatWrapper f1;
    public FloatWrapper f2;

    public override object Clone()
    {
        var clone = new Variable_Float_Min();
        clone.f1 = (FloatWrapper) f1?.Clone();
        clone.f2 = (FloatWrapper) f2?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = f1.GetValue(executeArgs);
        var fv2 = f2.GetValue(executeArgs);
        return Mathf.Min(fv1, fv2);
    }
}
