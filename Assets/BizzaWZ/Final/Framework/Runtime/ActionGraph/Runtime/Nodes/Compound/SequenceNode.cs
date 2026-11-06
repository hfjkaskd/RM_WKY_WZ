using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

 
[Preserve]
[GraphElementInfo(Text = "序列", SupportTypes = new Type[] {typeof(ActionGraphBase)},
    TipsText = "依次执行全部子节点，如果某个子节点失败，则立即结束并返回失败，全部子节点完成返回成功")]
[Obfuz.ObfuzIgnore]
public class SequenceNode : CompoundNodeBase
{
    public override string DebugName => "[序列]";

    private int _curIdx;

    public override object Clone()
    {
        var clone = new SequenceNode();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(executeArgs);
        _curIdx = 0;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return E_ExecuteState.Failed;
        }

        ActionGraphLog.Assert(_runningChildren.Count <= 1, "internal error");
        while (_curIdx < children.Count)
        {
            var runningChild = children[_curIdx];

            var childRet = E_ExecuteState.None;
            if (runningChild != null)
            {
                childRet = runningChild.Execute(executeArgs);
            }
            else
            {
                ActionGraphLog.Error($"节点丢失：{executeArgs.graph.graphName}");
                childRet = ActionGraphDefine.ActionExceptionResult;
            }

            ActionGraphLog.Assert(childRet != E_ExecuteState.None, "child ret is none");
            if (childRet == E_ExecuteState.Failed)
            {
                return E_ExecuteState.Failed;
            }
            else if (childRet == E_ExecuteState.Running)
            {
                return E_ExecuteState.Running;
            }
            else if (childRet == E_ExecuteState.Success)
            {
                _curIdx++;

                if (_curIdx >= children.Count)
                {
                    // runningChild.Exit(executeArgs);
                    return E_ExecuteState.Success;
                }
            }
            else
            {
                ActionGraphLog.Error("SequenceNode.OnExecute error");
                return E_ExecuteState.Failed;
            }
        }

        return E_ExecuteState.Success;
    }
}
