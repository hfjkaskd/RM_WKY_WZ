using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;

 
[Preserve]
[GraphElementInfo(Category = "", Text = "取反", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_Not : Variable_Bool
{
    [GraphVariable(LabelText = "值", Required = true)]
    public BoolWrapper b1;

    public override object Clone()
    {
        var clone = new Variable_Bool_Not();
        clone.b1 = (BoolWrapper) b1?.Clone();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        var bv1 = b1.GetValue(executeArgs);
        return !bv1;
    }
}