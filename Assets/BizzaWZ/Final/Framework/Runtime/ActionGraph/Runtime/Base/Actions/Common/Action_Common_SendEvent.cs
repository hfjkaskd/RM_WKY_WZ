using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "通用", Text = "发送事件", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_SendEvent : ActionNodeBase
{
    [GraphVariable(LabelText = "目标", Required = true)]
    public ActorWrapper target;

    [GraphVariable(LabelText = "事件名", Required = true)]
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
        var clone = new Action_Common_SendEvent();
        clone.target = (ActorWrapper)target?.Clone();
        clone.eventName = (StringWrapper)eventName?.Clone();
        clone.stringArg = (StringWrapper)stringArg?.Clone();
        clone.floatArg = (FloatWrapper)floatArg?.Clone();
        clone.actorArg = (ActorWrapper)actorArg?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var targetValue = target.GetValue(executeArgs);
        if (targetValue == null)
        {
            ActionGraphLog.Error(executeArgs.graph, "target null");
            return E_ExecuteState.Success;
        }
        var eventNameValue = eventName.GetValue(executeArgs);
        var context = PoolUtil.GetClass<ActionContext_Common>();
        context.eventName = eventNameValue;
        context.eventArgStr1 = stringArg.GetValueWithDefault(executeArgs, "");
        context.eventArgFloat1 = floatArg.GetValueWithDefault(executeArgs, 0);
        context.eventArgActor1 = actorArg.GetValueWithDefault(executeArgs, null);
        var actionCmpt = targetValue.GetComponent<ActorCmpt_Action>();
        if (actionCmpt == null)
        {
            ActionGraphLog.Error(executeArgs.graph, "target action component null");
            PoolUtil.ReleaseClass(context);
            return E_ExecuteState.Success;
        }

        ActionCallback.TriggerEvent<ActionGraphBase>((int)E_GraphEvent_Common.SendEvent, context, actionCmpt);
        PoolUtil.ReleaseClass(context);
        return E_ExecuteState.Success;
    }
}
