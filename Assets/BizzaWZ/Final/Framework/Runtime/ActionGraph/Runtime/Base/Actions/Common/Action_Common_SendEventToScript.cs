using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "通用", Text = "发送事件到代码", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_SendEventToScript : ActionNodeBase
{
    [GraphVariable(TipsText = "事件名", Required = true)]
    public StringWrapper eventName = new Variable_String_Direct()
    {
        directValue = "",
    };

    [GraphVariable(LabelText = "字符串参数", Required = false)]
    public StringWrapper stringArg;
    [GraphVariable(LabelText = "数字参数", Required = false)]
    public FloatWrapper floatArg;
    [GraphVariable(LabelText = "Actor参数", Required = false)]
    public ActorWrapper actorArg;

    public override object Clone()
    {
        var clone = new Action_Common_SendEventToScript();
        clone.eventName = (StringWrapper)eventName?.Clone();
        clone.stringArg = (StringWrapper)stringArg?.Clone();
        clone.floatArg = (FloatWrapper)floatArg?.Clone();
        clone.actorArg = (ActorWrapper)actorArg?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var eventNameValue = eventName.GetValue(executeArgs);
        BizzaEventSystem.Emit(EventDefine.GraphEvent.SendGraphEventToScript, eventNameValue,
            stringArg.GetValueWithDefault(executeArgs, ""),
            floatArg.GetValueWithDefault(executeArgs,0),
            actorArg.GetValueWithDefault(executeArgs, null));
        return E_ExecuteState.Success;
    }
}