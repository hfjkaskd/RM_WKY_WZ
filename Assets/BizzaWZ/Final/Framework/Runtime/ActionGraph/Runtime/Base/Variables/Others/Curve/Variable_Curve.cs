using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[GraphElementInfo(Category = "变量：其它")]
[Obfuz.ObfuzIgnore]
public abstract class Variable_Curve : VariableBase<AnimationCurve>
{
    public override Type WrapperType => typeof(CurveWrapper);

    public override E_VariableType VariableType => E_VariableType.Curve;
}
