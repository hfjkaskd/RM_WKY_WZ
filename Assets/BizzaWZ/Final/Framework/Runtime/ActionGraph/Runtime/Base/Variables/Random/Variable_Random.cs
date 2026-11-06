using System;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "基础", Text = "随机：float", SupportTypes = new Type[] { typeof(ActionGraphBase)}, TipsText = "不填参数为在0~1之间随机")]
[Obfuz.ObfuzIgnore]
public class Variable_Float_Random : Variable_Float
{
    [GraphVariable(LabelText = "下限", Required = false)]
    public FloatWrapper min;
    [GraphVariable(LabelText = "上限", Required = false)]
    public FloatWrapper max;

    public override object Clone()
    {
        var clone = new Variable_Float_Random
        {
            min = (FloatWrapper)min?.Clone(),
            max = (FloatWrapper)max?.Clone()
        };
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var _min = min.GetValueWithDefault(executeArgs, 0);
        var _max = max.GetValueWithDefault(executeArgs, 1);
        return UnityEngine.Random.Range(_min, _max);
    }
}


[Preserve]
[GraphElementInfo(Category = "基础", Text = "随机：int", SupportTypes = new Type[] { typeof(ActionGraphBase)}, TipsText = "不会包含上限")]
[Obfuz.ObfuzIgnore]
public class Variable_Float_RandomInt : Variable_Float
{
    [GraphVariable(LabelText = "下限", Required = true)]
    public FloatWrapper min;
    [GraphVariable(LabelText = "上限", Required = true)]
    public FloatWrapper max;

    public override object Clone()
    {
        var clone = new Variable_Float_RandomInt
        {
            min = (FloatWrapper)min?.Clone(),
            max = (FloatWrapper)max?.Clone()
        };
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var _min = min.GetValueInt(executeArgs);
        var _max = max.GetValueInt(executeArgs);
        return UnityEngine.Random.Range(_min, _max);
    }
}

[Preserve]
[GraphElementInfo(Category = "基础", Text = "随机", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Variable_Vector3_Random : Variable_Vector3
{
    public Vector3Wrapper min;
    public Vector3Wrapper max;

    public override object Clone()
    {
        var clone = new Variable_Vector3_Random
        {
            min = (Vector3Wrapper)min?.Clone(),
            max = (Vector3Wrapper)max?.Clone()
        };
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        var _min = min.GetValueWithDefault(executeArgs, Vector3.zero);
        var _max = max.GetValueWithDefault(executeArgs, Vector3.one);
        return new Vector3
        {
            x = UnityEngine.Random.Range(_min.x, _max.x),
            y = UnityEngine.Random.Range(_min.y, _max.y),
            z = UnityEngine.Random.Range(_min.z, _max.z)
        };
    }
}
