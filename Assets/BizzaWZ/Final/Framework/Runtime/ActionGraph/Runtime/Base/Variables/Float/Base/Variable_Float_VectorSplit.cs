using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
 [Obfuz.ObfuzIgnore]
public enum E_Axis
{
    X = 0,
    Y = 1,
    Z = 2,
}

[Preserve]
[GraphElementInfo(Category = "基础", Text = "Vector分量", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_VectorSplit : Variable_Float
{
    [GraphVariable("输入", true, "")]
    public Vector3Wrapper v;

    [GraphVariable("轴", true, "", typeof(E_Axis))]
    public EnumWrapper axis = new Variable_Enum_Direct()
    {
        enumType = typeof(E_Axis)
    };

    public override object Clone()
    {

        var clone = new Variable_Float_VectorSplit();
        clone.v = (Vector3Wrapper) v?.Clone();
        clone.axis = (EnumWrapper) axis?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        return v.GetValue(executeArgs)[axis.GetValue(executeArgs)];
    }
}
