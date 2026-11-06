using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[GraphElementInfo(Text = "返回成功", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class SuccessNode : DecoratorNodeBase
{
    public override string DebugName => "返回成功";

    public override object Clone()
    {
        var clone = new SuccessNode();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }

        var child = children[0];
        var childRet = child.Execute(executeArgs);
        if (childRet == E_ExecuteState.Failed)
        {
            childRet = E_ExecuteState.Success;
        }

        return childRet;
    }
}
