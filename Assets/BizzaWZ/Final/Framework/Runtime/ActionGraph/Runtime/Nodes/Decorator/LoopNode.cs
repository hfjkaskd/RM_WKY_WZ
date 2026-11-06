using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Text = "循环", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "如果子节点成功，则再次执行，直到次数结束\n如果子节点失败，则立即结束")]
[Obfuz.ObfuzIgnore]
public class LoopNode : DecoratorNodeBase
{
    public override string DebugName => $"[循环]";

    [GraphVariable(LabelText = "次数", TipsText = "-1为无限次", Required = true)]
    public FloatWrapper loopTimes = new Variable_Float_Direct() {directValue = 3};

    private int _curTime;
    private int _totalTimes;

    public override object Clone()
    {
        var clone = new LoopNode();
        clone.loopTimes = (FloatWrapper)loopTimes?.Clone();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(in executeArgs);
        _curTime = 0;
        _totalTimes = loopTimes.GetValueInt(executeArgs);
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }

        var child = children[0];
        if (child == null)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }
        var childRet = child.Execute(executeArgs);
        if (childRet == E_ExecuteState.Success)
        {
            _curTime++;
            //完成
            if (_totalTimes != -1 && _curTime >= _totalTimes)
            {
                return E_ExecuteState.Success;
            }
            else
            {
                return E_ExecuteState.Running;
            }
        }

        return childRet;
    }
}


[Preserve]
[GraphElementInfo(Text = "无限循环", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "无限循环，始终返回Running")]
[Obfuz.ObfuzIgnore]
public class InfiniteLoopNode : DecoratorNodeBase
{
    public override object Clone()
    {
        var clone = new InfiniteLoopNode();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }

        var child = children[0];
        if (child == null)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }
        var childRet = child.Execute(executeArgs);
        return E_ExecuteState.Running;
    }
}
