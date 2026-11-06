using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[GraphElementInfo(SupportTypes = new Type[]{})]
[Obfuz.ObfuzIgnore]
public class Variable_String_EventName : Variable_String
{
    public override string GetValue(in ExecuteArgs executeArgs)
    {
        return executeArgs.context.eventName;
    }

    public override object Clone()
    {
        var clone = new Variable_String_EventName();
        return clone;
    }
}

[GraphElementInfo(SupportTypes = new Type[]{})]
[Obfuz.ObfuzIgnore]
public class Variable_String_EventArgStr1 : Variable_String
{
    public override string GetValue(in ExecuteArgs executeArgs)
    {
        return executeArgs.context.eventArgStr1;
    }

    public override object Clone()
    {
        var clone = new Variable_String_EventArgStr1();
        return clone;
    }
}


[GraphElementInfo(SupportTypes = new Type[]{})]
[Obfuz.ObfuzIgnore]
public class Variable_String_EventArgFloat1 : Variable_Float
{
    public override float GetValue(in ExecuteArgs executeArgs)
    {
        return executeArgs.context.eventArgFloat1;
    }

    public override object Clone()
    {
        var clone = new Variable_String_EventArgFloat1();
        return clone;
    }
}

[GraphElementInfo(SupportTypes = new Type[]{})]
[Obfuz.ObfuzIgnore]
public class Variable_String_EventArgActor1 : Variable_Actor
{
    public override GameActor GetValue(in ExecuteArgs executeArgs)
    {
        return executeArgs.context.eventArgActor1;
    }

    public override object Clone()
    {
        var clone = new Variable_String_EventArgActor1();
        return clone;
    }
}

[Preserve]
[GraphElementInfo(Category = "", Text = "接收事件", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class EventAction_Common_SendEvent : EventActionBase
{
    public override string DebugName => "[接收事件]";

    public override IEnumerable<(Type, string)> OutputVariables
    {
        get
        {
            yield return (typeof(Variable_String_EventName), "事件名");
            yield return (typeof(Variable_String_EventArgFloat1), "float参数");
            yield return (typeof(Variable_String_EventArgStr1), "string参数");
            yield return (typeof(Variable_String_EventArgActor1), "Actor参数");
        }
    }

    public override int EventName => (int)E_GraphEvent_Common.SendEvent;

    public override object Clone()
    {
        var clone = new EventAction_Common_SendEvent();
        return clone;
    }
}
