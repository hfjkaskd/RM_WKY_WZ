using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "", Text = "并", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_And : Variable_Bool
{
    [LabelText("值1")]
    public BoolWrapper b1;

    [LabelText("值2")]
    public BoolWrapper b2;

    public override object Clone()
    {
        var clone = new Variable_Bool_And();
        clone.b1 = (BoolWrapper) b1?.Clone();
        clone.b2 = (BoolWrapper) b2?.Clone();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        var bv1 = b1.GetValue(executeArgs);
        if (!bv1)
        {
            return false;
        }

        var bv2 = b2.GetValue(executeArgs);
        return bv2;
    }
}