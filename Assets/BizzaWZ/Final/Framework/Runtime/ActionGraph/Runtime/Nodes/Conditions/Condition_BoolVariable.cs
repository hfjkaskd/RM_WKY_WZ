using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "", Text = "bool变量(每帧检查)", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Condition_BoolVariable : ConditionNodeBase
{
    [GraphVariable(LabelText = "条件", Required = true)]
    public BoolWrapper boolVar;

    public override object Clone()
    {
        var clone = new Condition_BoolVariable();
        clone.boolVar = (BoolWrapper) boolVar?.Clone();
        return clone;
    }

    public override bool GetResult(in ExecuteArgs executeArgs)
    {
        var value = boolVar.GetValue(executeArgs);
        return value;
    }
}
