using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "", Text = "世界是否暂停", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_WorldPause : Variable_Bool
{
    public override object Clone()
    {
        var clone = new Variable_Bool_WorldPause();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        return World.Current.IsPause;
    }
}