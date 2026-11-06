using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "加法", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_Add : Variable_Float
{
    public FloatWrapper f1;
    public FloatWrapper f2;

    public override object Clone()
    {
        var clone = new Variable_Float_Add();
        clone.f1 = (FloatWrapper) f1?.Clone();
        clone.f2 = (FloatWrapper) f2?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = f1.GetValue(executeArgs);
        var fv2 = f2.GetValue(executeArgs);
        return fv1 + fv2;
    }
}



[Preserve]
[GraphElementInfo(Category = "基础", Text = "减法", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_Sub : Variable_Float
{
    public FloatWrapper f1;
    public FloatWrapper f2;

    public override object Clone()
    {
        var clone = new Variable_Float_Sub();
        clone.f1 = (FloatWrapper) f1?.Clone();
        clone.f2 = (FloatWrapper) f2?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = f1.GetValue(executeArgs);
        var fv2 = f2.GetValue(executeArgs);
        return fv1 - fv2;
    }
}
