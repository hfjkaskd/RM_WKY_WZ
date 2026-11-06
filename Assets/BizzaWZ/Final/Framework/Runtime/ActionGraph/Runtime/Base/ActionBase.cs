using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "2.动作节点")]
[Obfuz.ObfuzIgnore]
public abstract class ActionNodeBase : NodeBase
{
    public override bool HasControlInput => true;
    public override bool HasControlOutput => false;

    //让动作节点的连接按Sequence逻辑执行
    // /// <summary>
    // /// 执行节点
    // /// </summary>
    // /// <param name="executeArgs"></param>
    // /// <returns></returns>
    // public override E_ExecuteState Execute(in ExecuteArgs executeArgs)
    // {
    //     if (_curExecuteState == E_ExecuteState.None)
    //     {
    //         Enter(executeArgs);
    //     }
    //
    //     if (_curExecuteState == E_ExecuteState.Running)
    //     {
    //         var ret = OnExecute(executeArgs);
    //         ActionGraphLog.Assert(ret == E_ExecuteState.Running || ret == E_ExecuteState.Success || ret == E_ExecuteState.Failed, $"错误的返回状态:{GetType().ToString()}");
    //         _curExecuteState = ret;
    //         if (ret == E_ExecuteState.Success || ret == E_ExecuteState.Failed)
    //         {
    //             Exit(executeArgs);
    //         }
    //
    //
    //         if (children != null && children.Count == 1)
    //         {
    //             var child = children[0];
    //             var childRet = child.Execute(executeArgs);
    //             if (ret == E_ExecuteState.Failed)
    //             {
    //                 return E_ExecuteState.Failed;
    //             }
    //             else
    //             {
    //                 return childRet;
    //             }
    //         }
    //         else
    //         {
    //             return ret;
    //         }
    //     }
    //
    //     ActionGraphLog.Error("error");
    //     return E_ExecuteState.Failed;
    // }
}
