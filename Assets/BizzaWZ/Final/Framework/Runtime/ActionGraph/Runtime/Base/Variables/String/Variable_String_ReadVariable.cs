using System;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "", Text = "读取变量(String)", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "")]
[Obfuz.ObfuzIgnore]
public class Variable_String_ReadVariable : Variable_String
{
    // [LabelText("变量名"), ValueDropdown("@VariableUtils.GetVariableNamesByType_Actor")]
    // public string varName;
    public override string DebugName => "读取变量";

    [GraphVariable(TipsText = "变量名", Required = true)]
    public StringWrapper varName;

    [GraphVariable(TipsText = "作用域", Required = true, EnumType = typeof(E_VariableScope))]
    public EnumWrapper varScope = new Variable_Enum_Direct()
    {
        enumType = typeof(E_VariableScope),
        directValue = (int) E_VariableScope.Graph,
    };

    [GraphVariable(LabelText = "目标actor", Required = false, TipsText = "作用域为Actor时需要，默认为自身")]
    public ActorWrapper actor;

    public override string GetValue(in ExecuteArgs executeArgs)
    {
        var varNameValue = varName.GetValue(executeArgs);
        var varScopeValue = (E_VariableScope)varScope.GetValue(executeArgs);
        var variable = VariableUtils.Get(varScopeValue, varNameValue,  executeArgs, actor.GetValueWithDefault(executeArgs, null));
        if (variable == null) return "";
        return variable.GetValue_String(executeArgs);
    }

    public override object Clone()
    {
        var clone = new Variable_String_ReadVariable();
        clone.varName = (StringWrapper)varName?.Clone();
        clone.varScope = (EnumWrapper)varScope?.Clone();
        clone.actor = (ActorWrapper) actor?.Clone();
        return clone;
    }
}