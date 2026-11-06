using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;


 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "设置UI可点击状态", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Teach_SetUIInteractive : ActionNodeBase
{
    [FormerlySerializedAs("isBlock")] [GraphVariable(LabelText = "是否可点击", Required = true)]
    public BoolWrapper canInteractive = new Variable_Bool_Direct()
    {
        directValue = false,
    };

    public override object Clone()
    {
        var clone = new Action_Teach_SetUIInteractive();
        clone.canInteractive = (BoolWrapper)canInteractive?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var canInter = canInteractive.GetValue(executeArgs);
        var group = UIModule.Instance.GetUICanvas().GetComponent<CanvasGroup>();
        if (group == null)
        {
            Debug.LogError("GameCanvas prefab is missing CanvasGroup.");
            return E_ExecuteState.Failed;
        }

        group.interactable = canInter;
        group.blocksRaycasts = canInter;
        return E_ExecuteState.Success;
    }
}

