using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "设置存档状态", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Teach_SetTeachState : ActionNodeBase
{
    [GraphVariable(LabelText = "存档序号", Required = false, TipsText = "不填默认是表格名字,这里得填教学表里的ID")]
    public StringWrapper teachIndex;
    [GraphVariable(TipsText = "存档状态", Required = true)]
    public FloatWrapper teachState = new Variable_Float_Direct()
    {
        directValue = 1,
    };

    public override object Clone()
    {
        var clone = new Action_Teach_SetTeachState();
        clone.teachIndex = (StringWrapper)teachIndex?.Clone();
        clone.teachState = (FloatWrapper)teachState?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var indexValue = teachIndex.GetValueWithDefault(executeArgs, executeArgs.graph.graphName);
        var stateValue = teachState.GetValueInt(executeArgs);
        SaveDataUtils.TeachData.SetState(indexValue, stateValue);
        SaveDataUtils.TeachSaveData.SaveData();
        return E_ExecuteState.Success;
    }
}