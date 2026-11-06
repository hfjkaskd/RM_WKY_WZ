using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "基础", Text = "组合", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Vector3_Combine : Variable_Vector3
{
    [GraphVariable("x", true)]
    public FloatWrapper x;
    [GraphVariable("y", true)]
    public FloatWrapper y;
    [GraphVariable("z", true)]
    public FloatWrapper z;

    public override object Clone()
    {
        var clone = new Variable_Vector3_Combine();
        clone.x = (FloatWrapper)x?.Clone();
        clone.y = (FloatWrapper)y?.Clone();
        clone.z = (FloatWrapper)z?.Clone();
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        return new Vector3(x.GetValue(executeArgs), y.GetValue(executeArgs), z.GetValue(executeArgs));
    }
}