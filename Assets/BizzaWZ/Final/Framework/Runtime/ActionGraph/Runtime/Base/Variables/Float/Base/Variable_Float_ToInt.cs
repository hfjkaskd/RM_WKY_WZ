using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "取整", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "移除小数部分")]
[Obfuz.ObfuzIgnore]
public class Variable_Float_ToInt : Variable_Float
{
    [GraphVariable(LabelText = "数字", Required = true)]
    public FloatWrapper f;

    public override object Clone()
    {
        var clone = new Variable_Float_ToInt();
        clone.f = (FloatWrapper) f?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var fv = f.GetValue(executeArgs);
        return (int)fv;
    }
}

