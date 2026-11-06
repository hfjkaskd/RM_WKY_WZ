using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// bool变量基类
/// </summary>
[GraphElementInfo(Category = "变量：布尔")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_Bool : VariableBase<bool>
{
    public override Type WrapperType => typeof(BoolWrapper);
    public override E_VariableType VariableType => E_VariableType.Bool;
}