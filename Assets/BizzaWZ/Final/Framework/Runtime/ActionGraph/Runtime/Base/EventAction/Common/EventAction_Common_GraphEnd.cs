using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "", Text = "图表结束", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class EventAction_Common_GraphEnd : EventActionBase
{
    public override string DebugName => "[图表结束]";

    public override IEnumerable<(Type, string)> OutputVariables
    {
        get
        {
            // yield return (typeof(Variable_Bool_Interrupt), "是否被打断");
            yield break;
        }
    }

    public override int EventName => (int)E_GraphEvent_Common.OnEnd;

    public override object Clone()
    {
        var clone = new EventAction_Common_GraphEnd();
        return clone;
    }
}
