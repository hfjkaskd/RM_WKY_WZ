using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Text = "选择", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "从头到尾执行子节点，直到有一个成功，如果有一个成功则返回成功，全部失败返回失败")]
[Obfuz.ObfuzIgnore]
public class SelectorNode : CompoundNodeBase
{
    public override string DebugName => "[选择]";

    private int _curIdx;

    public override object Clone()
    {
        var clone = new SelectorNode();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(in executeArgs);
        _curIdx = 0;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return E_ExecuteState.Failed;
        }

        while (_curIdx < children.Count)
        {
            var cur = children[_curIdx];
            var childRet = cur.Execute(executeArgs);
            if (childRet == E_ExecuteState.Success || childRet == E_ExecuteState.Running)
            {
                return childRet;
            }
            else if (childRet == E_ExecuteState.Failed)
            {
                _curIdx++;
            }
            // else if (childRet == E_ExecuteState.Running)
            // {
            //     break;
            // }
            else
            {
                ActionGraphLog.Error("SelectorNode.OnExecute error");
                return E_ExecuteState.Failed;
            }
        }

        return E_ExecuteState.Failed;
    }
}
