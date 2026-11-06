using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Text = "仅执行一次", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText =
    "整个图表的生命周期内仅执行一次，如果已经执行过，返回失败")]
[Obfuz.ObfuzIgnore]
public class OnceNode : DecoratorNodeBase
{
    private bool _executed;

    public override void OnReset()
    {
        base.OnReset();
        _executed = false;
    }

    protected override void OnExit(in ExecuteArgs executeArgs, bool interrupt)
    {
        base.OnExit(in executeArgs, interrupt);
        _executed = true;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }

        if (_executed)
        {
            return E_ExecuteState.Failed;
        }

        if (SingleChild == null)
        {
            return E_ExecuteState.Success;
        }
        var childRet = SingleChild.Execute(executeArgs);
        return childRet;
    }

    public override object Clone()
    {
        var clone = new OnceNode();
        return clone;
    }
}
