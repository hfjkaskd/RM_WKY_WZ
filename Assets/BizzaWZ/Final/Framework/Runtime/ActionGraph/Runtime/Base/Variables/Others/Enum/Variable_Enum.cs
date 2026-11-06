using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[GraphElementInfo(Category = "变量：其它")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_Enum : VariableBase<int>
{
    public override Type WrapperType => typeof(EnumWrapper);

    public abstract Type EnumType { get; }

    public override E_VariableType VariableType => E_VariableType.Enum;
}
