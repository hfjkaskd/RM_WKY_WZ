using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// vector3类型变量基类
/// </summary>
[GraphElementInfo(Category = "变量：向量")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_Vector3 : VariableBase<Vector3>
{
    public override Type WrapperType => typeof(Vector3Wrapper);
    public override E_VariableType VariableType => E_VariableType.Vector3;
}