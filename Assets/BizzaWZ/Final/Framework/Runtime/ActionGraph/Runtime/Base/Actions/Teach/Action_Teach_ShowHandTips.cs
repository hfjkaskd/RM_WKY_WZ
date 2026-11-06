using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "显示手指提示", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Teach_ShowHandTips : ActionNodeBase
{
    [GraphVariable(LabelText = "开始坐标", Required = false, TipsText = "世界坐标")]
    public Vector3Wrapper startPos;

    [GraphVariable(LabelText = "结束坐标", Required = false, TipsText = "世界坐标")]
    public Vector3Wrapper endPos;

    [GraphVariable(LabelText = "时长", Required = false, TipsText = "")]
    public FloatWrapper duration;


    public override object Clone()
    {
        var clone = new Action_Teach_ShowHandTips();
        clone.startPos = (Vector3Wrapper)startPos?.Clone();
        clone.endPos = (Vector3Wrapper)endPos?.Clone();
        clone.duration = (FloatWrapper)duration?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        UIModule.Instance.OpenPage(UIPageIds.UI_TeachFinterMove, new UITeachFingerMovePage.MoveArgs()
        {
            worldStartPos = startPos.GetValue(executeArgs),
            worldEndPos = endPos.GetValue(executeArgs),
            duration = duration.GetValue(executeArgs),
        });
        return E_ExecuteState.Success;
    }
}
