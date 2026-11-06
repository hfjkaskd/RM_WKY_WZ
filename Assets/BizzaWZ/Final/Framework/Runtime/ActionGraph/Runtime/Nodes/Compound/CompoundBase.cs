using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[GraphElementInfo(Category = "1.复合节点")]
[Obfuz.ObfuzIgnore]
public abstract class CompoundNodeBase : NodeBase
{
    public override bool HasControlInput => true;
    public override bool HasControlOutput => true;
    public override bool multChild => true;
    protected List<NodeBase> _runningChildren = new();
}
