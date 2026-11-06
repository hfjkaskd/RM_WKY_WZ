using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[GraphElementInfo(Text = "返回运行中", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "任何情况都返回运行中")]
[Obfuz.ObfuzIgnore]
public class RunningNode : DecoratorNodeBase
{
    public override object Clone()
    {
        var clone = new RunningNode();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }

        var child = children[0];
        child.Execute(executeArgs);
        return E_ExecuteState.Running;
    }
}
