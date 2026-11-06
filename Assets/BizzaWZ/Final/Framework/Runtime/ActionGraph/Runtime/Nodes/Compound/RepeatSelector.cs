using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Text = "优先级选择", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class PrioritySelectorNode : CompoundNodeBase
{
    public override string DebugName => "[优先级选择]";

    private int _prevIdx;

    public override object Clone()
    {
        var clone = new PrioritySelectorNode();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return E_ExecuteState.Failed;
        }

        var _curIdx = 0;
        while (_curIdx < children.Count)
        {
            var cur = children[_curIdx];
            var childRet = cur.Execute(executeArgs);
            if (childRet == E_ExecuteState.Success || childRet == E_ExecuteState.Running)
            {
                if (_prevIdx != -1 && _prevIdx != _curIdx)
                {
                    children[_prevIdx].Exit(executeArgs);
                    _prevIdx = _curIdx;
                }
                return childRet;
            }
            else if (childRet == E_ExecuteState.Failed)
            {
                _curIdx++;
            }
            else
            {
                ActionGraphLog.Error("SelectorNode.OnExecute error");
                return E_ExecuteState.Failed;
            }
        }

        return E_ExecuteState.Failed;
    }
}
