using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "基础", Text = "乘", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Vector3_Mul : Variable_Vector3
{
    public Vector3Wrapper v;
    public FloatWrapper f;

    public override object Clone()
    {
        var clone = new Variable_Vector3_Mul();
        clone.v = (Vector3Wrapper)v?.Clone();
        clone.f = (FloatWrapper)f?.Clone();
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        var vv = v.GetValue(executeArgs);
        var fv = f.GetValue(executeArgs);
        return vv * fv;
    }
}