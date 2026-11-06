using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "", Text = "直接值", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_String_Direct : Variable_String
{
    public override string DebugName => $"字符串";

    public override bool IsDirectValue => true;

    [LabelWidth(ActionGraphDefine.LabelWidth)]
    // [LabelText("值")]
    [HideLabel]
    public string directValue;

    public override object Clone()
    {
        var clone = new Variable_String_Direct();
        clone.directValue = directValue;
        return clone;
    }

    public override string GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }
}