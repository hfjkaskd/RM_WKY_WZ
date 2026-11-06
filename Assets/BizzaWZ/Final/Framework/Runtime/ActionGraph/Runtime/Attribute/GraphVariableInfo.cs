using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphVariableAttribute : Attribute
{
    /// <summary>
    /// 变量展示文本
    /// </summary>
    public string LabelText;

    /// <summary>
    /// 提示文本
    /// </summary>
    public string TipsText;

    /// <summary>
    /// 是否必要
    /// </summary>
    public bool Required;

    /// <summary>
    /// 枚举类型
    /// </summary>
    public Type EnumType;

    public GraphVariableAttribute()
    {

    }

    public GraphVariableAttribute(string labelText, bool required = false, string tipsText = "", Type enumType = null)
    {
        LabelText = labelText;
        TipsText = tipsText;
        Required = required;
        EnumType = enumType;
    }
}
