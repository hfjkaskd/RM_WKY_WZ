using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using Random = UnityEngine.Random;

[Preserve]
[GraphElementInfo(Category = "", Text = "随机方向（单位圆）", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Vector3_RandomOne : Variable_Vector3
{
    public override object Clone()
    {
        var clone = new Variable_Vector3_RandomOne();
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        Vector2 rd = UnityEngine.Random.insideUnitCircle;
        return rd.ToVector3();
    }
}