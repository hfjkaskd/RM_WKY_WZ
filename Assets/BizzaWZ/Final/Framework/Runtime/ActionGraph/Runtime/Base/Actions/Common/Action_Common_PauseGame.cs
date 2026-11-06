using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "通用", Text = "暂停游戏", SupportTypes = new Type[] { typeof(ActionGraphBase) },
    TipsText = "True暂停，False回复")]
[Obfuz.ObfuzIgnore]
public class Action_Common_PauseGame : ActionNodeBase
{
    [GraphVariable(LabelText = "是否暂停", Required = true)]
    public BoolWrapper pause;

    public override object Clone()
    {
        var clone = new Action_Common_PauseGame();
        clone.pause = (BoolWrapper)pause?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        bool isPause = pause.GetValueWithDefault(executeArgs, false);
        if (isPause)
        {
            World.Current.Pause(nameof(Action_Common_PauseGame));
        }
        else
        {
            World.Current.Resume(nameof(Action_Common_PauseGame));
        }
        return E_ExecuteState.Success;
    }
}