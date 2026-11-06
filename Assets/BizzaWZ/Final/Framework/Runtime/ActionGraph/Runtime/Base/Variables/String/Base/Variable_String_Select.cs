using System;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "选择", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_String_Select : Variable_String
{
    [GraphVariable(LabelText = "条件1", Required = true)]
    public BoolWrapper if1;

    [GraphVariable(LabelText = "值1", Required = true)]
    public StringWrapper value1;

    [GraphVariable(LabelText = "默认值", Required = true, TipsText = "条件都不满足时返回默认值")]
    public StringWrapper elseValue;

    public override object Clone()
    {
        var clone = new Variable_String_Select();
        clone.if1 = (BoolWrapper) if1?.Clone();
        clone.value1 = (StringWrapper) value1?.Clone();
        clone.elseValue = (StringWrapper) elseValue?.Clone();
        return clone;
    }

    public override string GetValue(in ExecuteArgs executeArgs)
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