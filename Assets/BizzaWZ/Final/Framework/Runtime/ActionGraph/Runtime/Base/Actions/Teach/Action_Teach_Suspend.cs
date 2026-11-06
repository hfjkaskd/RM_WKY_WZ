using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

public static partial class EventDefine
{
    public static partial class Teach
    {
        public static readonly GameEvent<string> EndSuspend = new();
    }
}


[Preserve]
[GraphElementInfo(Category = "教学", Text = "中断并等待", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Action_Teach_Suspend : ActionNodeBase
{
    [GraphVariable(LabelText = "Tag", Required = true)]
    public StringWrapper tag = new StringWrapper();

    public override object Clone()
    {
        var clone = new Action_Teach_Suspend();
        clone.tag = (StringWrapper)tag?.Clone();
        return clone;
    }

    private bool _success = false;
    private static string _tag = string.Empty;

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(in executeArgs);
        _success = false;
        _tag = tag.GetValue(executeArgs);
        BizzaEventSystem.On(EventDefine.Teach.EndSuspend, OnEndSuspend);
    }

    private void OnEndSuspend(string t)
    {
        if (t == _tag)
        {
            _success = true;
        }
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (_success)
        {
            return E_ExecuteState.Success;
        }

        return E_ExecuteState.Running;
    }

    protected override void OnExit(in ExecuteArgs executeArgs, bool interrupt)
    {
        base.OnExit(in executeArgs, interrupt);
        BizzaEventSystem.Off(EventDefine.Teach.EndSuspend, OnEndSuspend);
    }
}