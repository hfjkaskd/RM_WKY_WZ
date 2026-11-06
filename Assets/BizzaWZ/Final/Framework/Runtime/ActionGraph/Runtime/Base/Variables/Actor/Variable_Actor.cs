using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Actor类型的变量
/// </summary>
[Serializable]
[GraphElementInfo(Category = "变量：Actor")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_Actor : VariableBase<GameActor>
{
    public override Type WrapperType => typeof(ActorWrapper);
    public override E_VariableType VariableType => E_VariableType.Actor;
}

[Serializable]
[GraphElementInfo(SupportTypes = new Type[]{})]
[Obfuz.ObfuzIgnore]
public class Variable_Actor_Direct : Variable_Actor
{
    [HideInInspector]
    public GameActor directValue;

    public override GameActor GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }

    public override object Clone()
    {
        var clone = new Variable_Actor_Direct();
        return clone;
    }
}