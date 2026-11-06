using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "乘法", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_Mutl : Variable_Float
{
    public FloatWrapper f1;
    public FloatWrapper f2;

    public override object Clone()
    {
        var clone = new Variable_Float_Mutl();
        clone.f1 = (FloatWrapper) f1?.Clone();
        clone.f2 = (FloatWrapper) f2?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = f1.GetValue(executeArgs);
        var fv2 = f2.GetValue(executeArgs);
        return fv1 * fv2;
    }
}

[Preserve]
[GraphElementInfo(Category = "基础", Text = "除法", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_Divide : Variable_Float
{
    public FloatWrapper f1;
    public FloatWrapper f2;

    public override object Clone()
    {
        var clone = new Variable_Float_Divide();
        clone.f1 = (FloatWrapper) f1?.Clone();
        clone.f2 = (FloatWrapper) f2?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = f1.GetValue(executeArgs);
        var fv2 = f2.GetValue(executeArgs);
        if (fv2 == 0)
        {
            return 0;
        }
        return fv1 / fv2;
    }
}