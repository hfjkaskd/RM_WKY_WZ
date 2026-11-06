using System;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "通用", Text = "停止图表", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "停止当前图表")]
[Obfuz.ObfuzIgnore]
public class Action_Common_StopGraph : ActionNodeBase
{
    public override string DebugName => "停止图表";

    public override object Clone()
    {
        var clone = new Action_Common_StopGraph();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        ActionModule.Instance.Stop(executeArgs.graph);
        return E_ExecuteState.Success;
    }
}