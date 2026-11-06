using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
 [Obfuz.ObfuzIgnore]

public enum E_ScalarCompareType
{
    None,
    [LabelText("等于")]
    Equal,
    [LabelText("大于")]
    Bigger,
    [LabelText("大于等于")]
    BiggerEqual,
    [LabelText("小于")]
    Less,
    [LabelText("小于等于")]
    LessEqual,
    [LabelText("不等于")]
    NotEqual,
}

[Preserve]
[GraphElementInfo(Category = "", Text = "数值对比", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_ScalarCompare : Variable_Bool
{
    [GraphVariable(LabelText = "数1", Required = true)]
    public FloatWrapper f1;

    [GraphVariable(LabelText = "对比方式", Required = true, EnumType = typeof(E_ScalarCompareType))]
    public EnumWrapper compareType = new Variable_Enum_Direct()
    {
        enumType = typeof(E_ScalarCompareType),
        directValue = (int)E_ScalarCompareType.Equal,
    };

    [GraphVariable(LabelText = "数2", Required = true)]
    public FloatWrapper f2;


    public override object Clone()
    {
        var clone = new Variable_Bool_ScalarCompare();
        clone.compareType = compareType;
        clone.f1 = (FloatWrapper)f1?.Clone();
        clone.f2 = (FloatWrapper)f2?.Clone();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        var fv1 = this.f1.GetValue(executeArgs);
        var fv2 = this.f2.GetValue(executeArgs);

         var compareTypeValue = (E_ScalarCompareType)compareType.GetValue(executeArgs);
         bool ret = false;
         switch (compareTypeValue)
        {
            case E_ScalarCompareType.Equal:
                ret = fv1 == fv2;
                break;
            case E_ScalarCompareType.Bigger:
                ret = fv1 > fv2;
                break;
            case E_ScalarCompareType.BiggerEqual:
                ret = fv1 >= fv2;
                break;
            case E_ScalarCompareType.Less:
                ret = fv1 < fv2;
                break;
            case E_ScalarCompareType.LessEqual:
                ret = fv1 <= fv2;
                break;
            case E_ScalarCompareType.NotEqual:
                ret = fv1 != fv2;
                break;
        }

        return ret;
    }
}