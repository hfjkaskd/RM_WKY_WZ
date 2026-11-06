using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 条件基类
/// </summary>
[GraphElementInfo(Category = "4.条件节点")]
[Obfuz.ObfuzIgnore]
public abstract class ConditionNodeBase : NodeBase
{
    public override bool HasControlInput => true;
    public override bool HasControlOutput => true;

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var ret = GetResult(executeArgs);
        if (!ret)
        {
            return E_ExecuteState.Failed;
        }

        if (SingleChild == null)
        {
            return E_ExecuteState.Success;
        }

        return SingleChild.Execute(executeArgs);
    }

    public abstract bool GetResult(in ExecuteArgs executeArgs);
}
