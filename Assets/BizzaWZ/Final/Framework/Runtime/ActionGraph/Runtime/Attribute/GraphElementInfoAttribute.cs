using System;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
#endif

/// <summary>
/// 标识ActionGraph元素信息：分类，名称，作用域
/// </summary>
public class GraphElementInfoAttribute : Attribute
{
    /// <summary>
    /// 类型在分类上的路径，通过斜杠"/"分割路径，如"通用Action类别/XXXAction"。
    /// </summary>
    public string Category;

    /// <summary>
    /// 类型显示的名称，支持表达式。
    /// </summary>
    public string Text;

    /// <summary>
    /// 支持的图表类型，null||Length=0代表全都不支持，>0个元素时支持配置的图表及其子类，如全部支持可以配置为typeof(ActionGraphBase)
    /// </summary>
    public Type[] SupportTypes;

    /// <summary>
    /// 提示文本
    /// </summary>
    public string TipsText;
}

