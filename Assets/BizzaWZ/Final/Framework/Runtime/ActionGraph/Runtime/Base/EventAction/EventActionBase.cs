using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IOutputVariableNode
{
    public IEnumerable<(Type, string)> OutputVariables { get; }
}

[GraphElementInfo(Category = "", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public abstract class OutputActionBase : NodeBase, IOutputVariableNode
{
    public abstract int EventName { get; }

    public override bool HasControlInput => false;
    public override bool HasControlOutput => !IsVariableOverride;

    public abstract IEnumerable<(Type, string)> OutputVariables { get; }

    /// <summary>
    /// 覆盖节点
    /// </summary>
    public abstract bool IsVariableOverride { get; }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0) return E_ExecuteState.Failed;

        var child = children[0];
        var ret = child.Execute(executeArgs);
        if (!ret.IsReturnState())
        {
            var errorNodes = new List<NodeBase>();
            GetErrorNode(this, errorNodes);
            ActionGraphLog.Error($"事件节点不支持持续动作:{belongGraph.graphName}，问题节点:{string.Join(',', errorNodes.Select(x=>x.ProfilerDebugName))}");
        }
        return ret;
    }

    private static void GetErrorNode(NodeBase node, List<NodeBase> errorNodes)
    {
        bool selfError = !node.CurExecuteState.IsReturnState();
        if (!selfError)
        {
            return;
        }

        bool childError = false;
        foreach (var v in node.children)
        {
            if (!v.CurExecuteState.IsReturnState())
            {
                childError = true;
                break;
            }
        }

        if (!childError)
        {
            errorNodes.Add(node);
        }
        else
        {
            foreach (var v in node.children)
            {
                GetErrorNode(v, errorNodes);
            }
        }
    }
}

[GraphElementInfo(Category = "5.事件节点")]
[Obfuz.ObfuzIgnore]
public abstract class EventActionBase : OutputActionBase
{
    public override bool IsVariableOverride => false;
}

[GraphElementInfo(Category = "6.变量覆盖")]
[Obfuz.ObfuzIgnore]
public abstract class VariableOverrideBase : OutputActionBase
{
    public override bool IsVariableOverride => true;
    public override int EventName => 0;
}
