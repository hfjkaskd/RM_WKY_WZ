using System;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Category = "通用", Text = "震动", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Action_Common_Vibrate : ActionNodeBase
{
    [GraphVariable(TipsText = "长震动")]
    public BoolWrapper isLong = new BoolWrapper();

    public override object Clone()
    {
        var clone = new Action_Common_Vibrate
        {
            isLong = (BoolWrapper)isLong?.Clone()
        };
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var _isLong = isLong.GetValueWithDefault(executeArgs, false);
        VibrationUtils.Vibrate(E_VibrateType.Medium);
        return E_ExecuteState.Success;
    }
}
