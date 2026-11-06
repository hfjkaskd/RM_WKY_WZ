using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "取模", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_ModInt : Variable_Float
{
    [GraphVariable("数字")]
    public FloatWrapper f1;
    [GraphVariable("模")]
    public FloatWrapper f2;

    public override object Clone()
    {
        var clone = new Variable_Float_ModInt();
        clone.f1 = (FloatWrapper) f1?.Clone();
        clone.f2 = (FloatWrapper) f2?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = f1.GetValueInt(executeArgs);
        var fv2 = f2.GetValueInt(executeArgs);
        return fv1 % fv2;
    }
}