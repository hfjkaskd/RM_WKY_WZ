using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "", Text = "随机", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_Random : Variable_Bool
{
    [GraphVariable(LabelText = "概率(0~1)", Required = true)]
    public FloatWrapper percent = new Variable_Float_Direct() {directValue = 0};

    public override object Clone()
    {
        var clone = new Variable_Bool_Random();
        clone.percent = (FloatWrapper) percent?.Clone();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        return percent.GetValue(executeArgs) > UnityEngine.Random.value;
    }
}