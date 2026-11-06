using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "选择（2选1）", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "")]
[Obfuz.ObfuzIgnore]
public class Variable_Float_SelectInTwo : Variable_Float
{
    [GraphVariable(LabelText = "条件1", Required = true)]
    public BoolWrapper if1;

    [GraphVariable(LabelText = "值1", Required = true)]
    public FloatWrapper value1;

    [GraphVariable(LabelText = "默认值", Required = true, TipsText = "条件都不满足时返回默认值")]
    public FloatWrapper elseValue;

    public override object Clone()
    {
        var clone = new Variable_Float_SelectInTwo();
        clone.if1 = (BoolWrapper)if1?.Clone();
        clone.value1 = (FloatWrapper)value1?.Clone();
        clone.elseValue = (FloatWrapper)elseValue?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        if (!if1.IsNull() && if1.GetValue(executeArgs))
        {
            return value1.GetValue(executeArgs);
        }
        else
        {
            return elseValue.GetValue(executeArgs);
        }
    }
}


[Preserve]
[GraphElementInfo(Category = "基础", Text = "选择（3选1）", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "")]
[Obfuz.ObfuzIgnore]
public class Variable_Float_SelectInThree : Variable_Float
{
    [GraphVariable(LabelText = "条件1", Required = true)]
    public BoolWrapper if1;

    [GraphVariable(LabelText = "值1", Required = true)]
    public FloatWrapper value1;

    [GraphVariable(LabelText = "条件2")]
    public BoolWrapper if2;

    [GraphVariable(LabelText = "值2")]
    public FloatWrapper value2;

    [GraphVariable(LabelText = "默认值", Required = true, TipsText = "条件都不满足时返回默认值")]
    public FloatWrapper elseValue;

    public override object Clone()
    {
        var clone = new Variable_Float_SelectInThree();
        clone.if1 = (BoolWrapper)if1?.Clone();
        clone.value1 = (FloatWrapper)value1?.Clone();
        clone.if2 = (BoolWrapper)if2?.Clone();
        clone.value2 = (FloatWrapper)value2?.Clone();
        clone.elseValue = (FloatWrapper)elseValue?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        if (!if1.IsNull() && if1.GetValue(executeArgs))
        {
            return value1.GetValue(executeArgs);
        }
        else if (!if2.IsNull() && if2.GetValue(executeArgs))
        {
            return value2.GetValue(executeArgs);
        }
        else
        {
            return elseValue.GetValue(executeArgs);
        }
    }
}