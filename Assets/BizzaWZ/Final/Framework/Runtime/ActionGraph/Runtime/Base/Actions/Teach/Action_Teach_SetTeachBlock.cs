using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "教学阻断输入", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Teach_SetTeachBlock : ActionNodeBase
{
    [GraphVariable(TipsText = "是否阻断", Required = true)]
    public BoolWrapper isBlock = new Variable_Bool_Direct()
    {
        directValue = true,
    };

    public override object Clone()
    {
        var clone = new Action_Teach_SetTeachBlock();
        clone.isBlock = (BoolWrapper)isBlock?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var isBlockValue = isBlock.GetValue(executeArgs);
        if(isBlockValue)
            TransparentBlock.AddBlock(nameof(Action_Teach_SetTeachBlock));
        else
            TransparentBlock.RemoveBlock(nameof(Action_Teach_SetTeachBlock));
        return E_ExecuteState.Success;
    }
}

