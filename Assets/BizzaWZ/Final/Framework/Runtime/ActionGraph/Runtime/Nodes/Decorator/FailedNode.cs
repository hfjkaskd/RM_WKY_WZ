using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Text = "返回失败", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class FailedNode : DecoratorNodeBase
{
    public override string DebugName => "返回失败";

    public override object Clone()
    {
        var clone = new FailedNode();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return E_ExecuteState.Failed;
        }

        var child = children[0];
        var childRet = child.Execute(executeArgs);
        if (childRet == E_ExecuteState.Success)
        {
            childRet = E_ExecuteState.Failed;
        }

        return childRet;
    }
}
