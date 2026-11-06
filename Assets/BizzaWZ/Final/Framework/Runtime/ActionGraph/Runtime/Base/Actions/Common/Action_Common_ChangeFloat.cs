using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "通用", Text = "修改float变量", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_ChangeFloat : ActionNodeBase
{
    [GraphVariable(LabelText = "变量名", Required = true)]
    public StringWrapper varName = new Variable_String_Direct()
    {
        directValue = "",
    };

    [GraphVariable(LabelText = "增加值", Required = true, TipsText = "可以为负数")]
    public FloatWrapper delta = new Variable_Float_Direct()
    {
        directValue = 0,
    };

    [GraphVariable(LabelText = "作用域", Required = false, EnumType = typeof(E_VariableScope), TipsText = "默认为Graph")]
    public EnumWrapper varScope;

    [GraphVariable(LabelText = "目标actor", Required = false, TipsText = "作用域为Actor时需要，默认为自身")]
    public ActorWrapper actor;

    [GraphVariable(LabelText = "初始值", Required = true, TipsText = "")]
    public FloatWrapper origin = new Variable_Float_Direct()
    {
        directValue = 0,
    };

    public override object Clone()
    {
        var clone = new Action_Common_ChangeFloat();
        clone.varName = (StringWrapper) varName?.Clone();
        clone.delta = (FloatWrapper) delta?.Clone();
        clone.varScope = (EnumWrapper) varScope?.Clone();
        clone.actor = (ActorWrapper) actor?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var varNameValue = varName.GetValue(executeArgs);
        var varScopeValue = (E_VariableScope)varScope.GetValue(executeArgs);
        var variable = VariableUtils.Get(varScopeValue, varNameValue,  executeArgs, actor.GetValueWithDefault(executeArgs, null));
        if (variable == null)
        {
            variable = VariableUtils.Set(varScopeValue, varNameValue, executeArgs, origin, actor.GetValueWithDefault(executeArgs, null));
        }

        var cur = variable.GetValue_Float(executeArgs);
        variable.SetValue_Float(executeArgs, cur + delta.GetValue(executeArgs));
        return E_ExecuteState.Success;
    }
}