using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 字符串变量基类
/// </summary>
[GraphElementInfo(Category = "变量：字符串")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_String : VariableBase<string>
{
    public override Type WrapperType => typeof(StringWrapper);
    public override E_VariableType VariableType => E_VariableType.String;
}