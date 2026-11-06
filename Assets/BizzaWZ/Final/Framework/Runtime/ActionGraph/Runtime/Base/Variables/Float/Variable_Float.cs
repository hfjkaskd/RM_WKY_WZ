using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[GraphElementInfo(Category = "变量：数字")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_Float : VariableBase<float>
{
    public override Type WrapperType => typeof(FloatWrapper);
    public override E_VariableType VariableType => E_VariableType.Float;
}