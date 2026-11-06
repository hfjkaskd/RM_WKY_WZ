using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "是否为正数(>0)", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_Positive : Variable_Bool
{

    [GraphVariable(LabelText = "数字", Required = true)]
    public FloatWrapper f;

    public override object Clone()
    {
        var clone = new Variable_Bool_Positive();
        clone.f = (FloatWrapper)f?.Clone();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        return f.GetValue(executeArgs) > 0;
    }
}

[Preserve]
[GraphElementInfo(Category = "基础", Text = "是否为0", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_Zero : Variable_Bool
{

    [GraphVariable(LabelText = "数字", Required = true)]
    public FloatWrapper f;

    public override object Clone()
    {
        var clone = new Variable_Bool_Zero();
        clone.f = (FloatWrapper)f?.Clone();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        return f.GetValue(executeArgs) == 0;
    }
}