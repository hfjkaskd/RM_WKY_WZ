using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
// [GraphElementInfo(Category = "变量")]
[Obfuz.ObfuzIgnore]
public abstract class VariableBase : GraphElementBase
{
    [HideInInspector][JsonIgnore][NonSerialized]
    public object debugValue;
    [HideInInspector][JsonIgnore][NonSerialized]
    public int lastHasDebugValueFrame;

    public override bool HasControlInput => false;
    public override bool HasControlOutput => false;
    public virtual bool IsDirectValue => false;

    public abstract Type WrapperType { get; }
    public abstract E_VariableType VariableType { get; }

    // public virtual void OnReset() {}
}

/// <summary>
/// ActionGraph变量基类
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
[Obfuz.ObfuzIgnore]
public abstract class VariableBase<T> : VariableBase
{
    public abstract T GetValue(in ExecuteArgs executeArgs);
}
