using System;
using System.Collections;
using System.Collections.Generic;
using Bizza;
using UnityEngine;


/// <summary>
/// Actor列表类型的变量
/// </summary>
[Serializable]
[GraphElementInfo(Category = "变量：Actor列表")]
public abstract class Variable_ActorList : VariableBase<List<GameActor>>
{
    public override Type WrapperType => typeof(ActorListWrapper);
    public override E_VariableType VariableType => E_VariableType.ActorList;

    protected static List<GameActor> GetFormPool()
    {
        var ret = StaticSimplePool<List<GameActor>>.Pool.Get();
        ret.Clear();
        return ret;
    }
}


[Serializable]
[GraphElementInfo(SupportTypes = new Type[]{})]
public class Variable_ActorList_Direct : Variable_ActorList
{
    [HideInInspector]
    public List<GameActor> directValue = new();

    public override List<GameActor> GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }

    public override object Clone()
    {
        var clone = new Variable_ActorList_Direct();
        return clone;
    }
}