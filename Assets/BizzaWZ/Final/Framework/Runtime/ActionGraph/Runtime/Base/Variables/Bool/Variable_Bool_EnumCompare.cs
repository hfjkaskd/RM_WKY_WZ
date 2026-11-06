using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "", Text = "枚举对比", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_EnumCompare : Variable_Bool
{
    [LabelText("数值1")] public EnumWrapper e1;
    [LabelText("数值2")] public EnumWrapper e2;

    public override string DebugName => "枚举对比";

    public override object Clone()
    {
        var clone = new Variable_Bool_EnumCompare();
        clone.e1 = (EnumWrapper) e1?.Clone();
        clone.e2 = (EnumWrapper) e2?.Clone();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = this.e1.GetValue(executeArgs);
        var fv2 = this.e2.GetValue(executeArgs);

        bool ret = fv1 == fv2;
        return ret;
    }
}
