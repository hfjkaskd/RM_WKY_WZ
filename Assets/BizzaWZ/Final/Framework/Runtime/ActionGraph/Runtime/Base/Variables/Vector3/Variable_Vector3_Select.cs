using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "基础", Text = "选择", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Vector3_SelectInTwo : Variable_Vector3
{
    [GraphVariable(LabelText = "条件1", Required = true)]
    public BoolWrapper if1;
    [GraphVariable(LabelText = "值1", Required = true)]
    public Vector3Wrapper value1;
    [GraphVariable(LabelText = "默认值", Required = true)]
    public Vector3Wrapper elseValue;

    public override object Clone()
    {
        var clone = new Variable_Vector3_SelectInTwo();
        clone.if1 = (BoolWrapper)if1?.Clone();
        clone.value1 = (Vector3Wrapper)value1?.Clone();
        clone.elseValue = (Vector3Wrapper)elseValue?.Clone();
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        var ifValue1 = if1.GetValue(executeArgs);
        if (ifValue1) return value1.GetValue(executeArgs);
        else return elseValue.GetValue(executeArgs);
    }
}