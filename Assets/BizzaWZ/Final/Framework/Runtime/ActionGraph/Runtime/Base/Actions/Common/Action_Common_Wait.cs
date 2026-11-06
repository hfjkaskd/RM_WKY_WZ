using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

 
[Preserve]
[GraphElementInfo(Category = "通用", Text = "等待", SupportTypes = new Type[] { typeof(ActionGraphBase) },
    TipsText = "-1为无限时长")]
[Obfuz.ObfuzIgnore]
public class Action_Common_Wait : ActionNodeBase
{
    public override string DebugName => $"等待";

    [GraphVariable(LabelText = "时长", Required = true)]
    public FloatWrapper duraitonVar = new Variable_Float_Direct() { directValue = 2 };

    private float _timer;

    public override object Clone()
    {
        var clone = new Action_Common_Wait();
        clone.duraitonVar = (FloatWrapper)duraitonVar?.Clone();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(executeArgs);
        _timer = duraitonVar.GetValue(executeArgs);
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (_timer == -1)
        {
            return E_ExecuteState.Running;
        }

        _timer -= executeArgs.deltaTime;
        return _timer <= 0 ? E_ExecuteState.Success : E_ExecuteState.Running;
    }
}