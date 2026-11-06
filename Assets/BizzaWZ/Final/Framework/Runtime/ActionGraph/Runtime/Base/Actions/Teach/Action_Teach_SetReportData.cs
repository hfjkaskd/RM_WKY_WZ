using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using UnityExtensions;



[Preserve]
[GraphElementInfo(Category = "教学", Text = "发送教学报告", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Action_Teach_SetReportData : ActionNodeBase
{
    [GraphVariable(TipsText = "报告状态", Required = true)]
    public FloatWrapper teachState = new Variable_Float_Direct();

    [GraphVariable(TipsText = "报告内容", Required = true)]
    public StringWrapper teachSetpName = new Variable_String_Direct();

    [GraphVariable(TipsText = "报告阶段", Required = true)]
    public FloatWrapper teachStep = new Variable_Float_Direct();

    public override object Clone()
    {
        var clone = new Action_Teach_SetReportData();
        clone.teachState = (FloatWrapper)teachState?.Clone();
        clone.teachSetpName = (StringWrapper)teachSetpName?.Clone();
        clone.teachStep = (FloatWrapper)teachStep?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        return E_ExecuteState.Success;
    }
}