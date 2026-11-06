using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "", Text = "直接值", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_Direct : Variable_Bool
{
    public override string DebugName => $"布尔";

    public override bool IsDirectValue => true;

    [LabelWidth(ActionGraphDefine.LabelWidth)]
    [HideLabel][CustomValueDrawer("CustomValueDrawer")]
    public bool directValue;

    public override object Clone()
    {
        var clone = new Variable_Bool_Direct();
        clone.directValue = directValue;
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }

    private static bool CustomValueDrawer(bool inValue, GUIContent label)
    {
        if (GUILayout.Button(inValue ? "是" : "否"))
        {
            inValue = !inValue;
        }

        return inValue;
    }
}