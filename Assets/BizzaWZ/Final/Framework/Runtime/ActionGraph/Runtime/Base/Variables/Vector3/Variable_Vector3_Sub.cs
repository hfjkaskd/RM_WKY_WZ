using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "基础", Text = "减", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Vector3_Sub : Variable_Vector3
{
    public override string DebugName => "减";
    public Vector3Wrapper v1;
    public Vector3Wrapper v2;

    public override object Clone()
    {
        var clone = new Variable_Vector3_Sub();
        clone.v1 = (Vector3Wrapper)v1?.Clone();
        clone.v2 = (Vector3Wrapper)v2?.Clone();
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        var v1v = v1.GetValue(executeArgs);
        var v2v = v2.GetValue(executeArgs);
        return v1v - v2v;
    }
}