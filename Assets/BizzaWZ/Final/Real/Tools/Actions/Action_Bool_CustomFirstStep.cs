#if BIZZA_REAL_WITHDRAW
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting;


[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "自定义引导结束", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class CustomFirstTutorial : Variable_Bool
{
    public override object Clone()
    {
        var clone = new CustomFirstTutorial();
        return clone;
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        // 补充你自定义的第一步判断, 也就是你自己的教程结束判断
        bool isGamePlayTutorialOver = SaveDataUtils.GameData.customTutorialEnd;
        return isGamePlayTutorialOver;
    }
}

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "进入自定义引导", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class EnterCustomTutorial : ActionNodeBase
{
    public override object Clone()
    {
        var clone = new EnterCustomTutorial();
        return clone;
    }
    
    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        SaveDataUtils.GameData.customTutorialEnd = false;
        return E_ExecuteState.Success;
    }
}

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "游戏界面显示", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
public class GamePanelShow : ActionNodeBase
{
    [GraphVariable("是否显示", true)]
    public BoolWrapper show;

    public override object Clone()
    {
        var clone = new GamePanelShow();
        clone.show = (BoolWrapper)show?.Clone();
        return clone;
    }
    
    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var gamePanel = UIModule.Instance.GetPage<RealGamePanel>();
        if (gamePanel != null)
        {
            var cg = gamePanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = show.GetValue(executeArgs) ? 1 : 0;
            }
            else
            {
                Debug.LogError("RealGamePanel prefab is missing CanvasGroup.");
            }
                
        }
        return E_ExecuteState.Success;
    }

}

#endif
