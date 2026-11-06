using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "通用", Text = "等待至", SupportTypes = new Type[] { typeof(ActionGraphBase) },
    TipsText = "等待至条件达成，达成返回success，否则返回Running")]
[Obfuz.ObfuzIgnore]
public class Action_Common_WaitUntil : ActionNodeBase
{

    [GraphVariable(LabelText = "条件", Required = true)]
    public BoolWrapper condition;

    public override object Clone()
    {
        var clone = new Action_Common_WaitUntil();
        clone.condition = (BoolWrapper)condition?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        return condition.GetValue(executeArgs) ? E_ExecuteState.Success : E_ExecuteState.Running;
    }
}