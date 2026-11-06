using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


/// <summary>
/// 根节点
/// </summary>
 
[GraphElementInfo(SupportTypes = new Type[]{}, TipsText = "根节点，图表的入口\n可以理解为update，会一直运行到返回Failed或被外部打断(比如buff持续时间)")]
[Obfuz.ObfuzIgnore]
public class RootNode : NodeBase
{
    public override string DebugName => "[根节点]";
    public override bool HasControlInput => false;
    public override bool HasControlOutput => true;

    public override object Clone()
    {
        var clone = new RootNode();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (SingleChild == null)
        {
            return E_ExecuteState.Failed;
        }

        return SingleChild.Execute(executeArgs);
    }
}
