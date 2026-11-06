using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "通用", Text = "输出日志", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_Log : ActionNodeBase
{
    [GraphVariable(TipsText = "文本", Required = true)]
    public StringWrapper text = new Variable_String_Direct()
    {
        directValue = "msg",
    };

    public override object Clone()
    {
        var clone = new Action_Common_Log();
        clone.text = (StringWrapper)text?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var textValue = text.GetValue(executeArgs);
        LogLogger.LogInfo($"[{Time.frameCount}]<color=cyan>{textValue}</color>");
        return E_ExecuteState.Success;
    }
}