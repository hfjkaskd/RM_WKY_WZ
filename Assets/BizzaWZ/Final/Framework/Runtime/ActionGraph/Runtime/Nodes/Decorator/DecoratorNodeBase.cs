using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "3.装饰节点")]
[Obfuz.ObfuzIgnore]
public abstract class DecoratorNodeBase : NodeBase
{
    public override bool HasControlInput => true;
    public override bool HasControlOutput => true;
    // protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    // {
    //     if (SingleChild == null)
    //     {
    //         return E_ExecuteState.Failed;
    //     }
    //
    //     return SingleChild.Execute(executeArgs);
    // }
}

