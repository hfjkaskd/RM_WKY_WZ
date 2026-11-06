using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

 
[Preserve]
[GraphElementInfo(Category = "", Text = "教学条件", SupportTypes = new Type[] {typeof(ActionGraph_Teach) })]
[Obfuz.ObfuzIgnore]
public class EventAction_Teach_TeachStart : VariableOverrideBase
{
    public override string DebugName => "[教学条件]";

    // public override string EventName => nameof(EventAction_Teach_TeachStart);

    [GraphVariable(LabelText = "条件")]
    public BoolWrapper conditionOverride;

    [JsonIgnore]
    public override IEnumerable<(Type, string)> OutputVariables
    {
        get
        {
            yield break;
        }
    }

    public override object Clone()
    {
        var clone = new EventAction_Teach_TeachStart();
        clone.conditionOverride = (BoolWrapper) conditionOverride?.Clone();
        return clone;
    }
}
