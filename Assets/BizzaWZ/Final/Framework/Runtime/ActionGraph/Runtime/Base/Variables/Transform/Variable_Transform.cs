using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[GraphElementInfo(Category = "变量：Transform")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_Transform : VariableBase<Transform>
{
    public override Type WrapperType => typeof(TransformWrapper);
    public override E_VariableType VariableType => E_VariableType.Transform;
}
