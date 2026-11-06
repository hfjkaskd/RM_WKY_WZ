#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "框架教程中", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class SetCardClick : ActionNodeBase
{
    public BoolWrapper b1;
    
    public override object Clone()
    {
        var clone = new SetCardClick();
        clone.b1 = (BoolWrapper) b1?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        bool iscan = b1.GetValue(executeArgs);
       // SaveDataUtils.GameData.isInFrameTutorial = iscan;
        // Raycast.isInFrameTutorial = iscan;
        return E_ExecuteState.Success;
    }
}

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "关闭所有弹窗界面", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class OnCloseAllPopPanel : ActionNodeBase
{
    public override object Clone()
    {
        var clone = new OnCloseAllPopPanel();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
            UIModule.Instance.CloseLayerAllPage((int)E_UILayer.PopupLayer);
        return E_ExecuteState.Success;
    }
}
 
#endif