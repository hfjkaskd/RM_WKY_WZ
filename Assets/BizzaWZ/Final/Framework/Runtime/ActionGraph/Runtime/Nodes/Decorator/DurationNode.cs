using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Text = "持续n秒", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText =
    "限制子节点的运行时间最多为n秒，子节点可用提前结束，但不能超出，提前结束返回成功")]
[Obfuz.ObfuzIgnore]
public class DurationNode : DecoratorNodeBase
{
    public override string DebugName => $"持续n秒";

    [GraphVariable(LabelText = "秒", Required = true, TipsText = "<=0时为无限")]
    public FloatWrapper durationVar = new Variable_Float_Direct() {directValue = 5};

    private float _elapseTime;

    public override object Clone()
    {
        var clone = new DurationNode();
        clone.durationVar = (FloatWrapper)durationVar?.Clone();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(in executeArgs);
        _elapseTime = 0;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return ActionGraphDefine.ActionExceptionResult;
        }

        _elapseTime += executeArgs.deltaTime;
        var child = children[0];
        var childRet = child.Execute(executeArgs);

        var duration = durationVar.GetValue(executeArgs);
        if (duration > 0 && _elapseTime >= duration && childRet == E_ExecuteState.Running)
        {
            return E_ExecuteState.Success;
        }

        return childRet;
    }
}
