using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Text = "直接值", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Float_Direct : Variable_Float
{
    public override string DebugName => $"数字";

    public override bool IsDirectValue => true;

    [LabelWidth(ActionGraphDefine.LabelWidth)]
    // [LabelText("值")]
    [HideLabel]
    public float directValue;

    public override object Clone()
    {
        var clone = new Variable_Float_Direct();
        clone.directValue = directValue;
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }
}