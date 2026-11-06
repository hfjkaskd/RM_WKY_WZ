using System;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "教学", Text = "当前无教学运行", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_NoRunningGraph : Variable_Bool
{
    public override string DebugName => "当前无教学运行";


    public override object Clone()
    {
        return new Variable_Bool_NoRunningGraph
        {
        };
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        return !TeachUtil.IsTeach;
    }
}
