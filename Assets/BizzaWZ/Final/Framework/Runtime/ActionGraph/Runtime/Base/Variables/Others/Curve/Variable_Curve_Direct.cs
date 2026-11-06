using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "", Text = "曲线", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Curve_Direct : Variable_Curve
{
    public override bool IsDirectValue => true;


    [LabelWidth(ActionGraphDefine.LabelWidth)]
    [HideLabel]
    [JsonIgnore]
    public AnimationCurve directValue;

    public override object Clone()
    {
        var clone = new Variable_Curve_Direct();
        clone.directValue = directValue;//曲线不复制
        return clone;
    }

    public override AnimationCurve GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }

}
