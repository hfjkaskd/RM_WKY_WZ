using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "", Text = "图表开始", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class EventAction_Common_GraphStart : EventActionBase
{
    public override string DebugName => "[图表开始]";

    public override IEnumerable<(Type, string)> OutputVariables
    {
        get
        {
            yield break;
        }
    }

    public override int EventName => (int)E_GraphEvent_Common.OnStart;

    public override object Clone()
    {
        var clone = new EventAction_Common_GraphStart();
        return clone;
    }
}
