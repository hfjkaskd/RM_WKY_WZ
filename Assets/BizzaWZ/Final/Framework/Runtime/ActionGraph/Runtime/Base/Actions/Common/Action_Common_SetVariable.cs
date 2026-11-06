using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[Obfuz.ObfuzIgnore]
public abstract class Actin_Common_SetVariableBase : ActionNodeBase
{

}

[Preserve]
[GraphElementInfo(Category = "通用", Text = "设置变量(Bool)", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_SetVariable_Bool : ActionNodeBase
{
    [GraphVariable(LabelText = "输入变量", Required = true)]
    public BoolWrapper inputVar;

    [GraphVariable(LabelText = "目标变量名", Required = true)]
    public StringWrapper outputNameVar;

    [GraphVariable(LabelText = "作用域", Required = true, EnumType = typeof(E_VariableScope))]
    public EnumWrapper scopeVar = new Variable_Enum_Direct()
    {
        enumType = typeof(E_VariableScope),
        directValue = (int)E_VariableScope.Graph,
    };

    [GraphVariable(LabelText = "目标Actor", Required = false, TipsText = "Actor作用域时需要传入")]
    public ActorWrapper actor;


    public override object Clone()
    {
        var clone = new Action_Common_SetVariable_Bool();
        clone.inputVar = (BoolWrapper)inputVar?.Clone();
        clone.outputNameVar = (StringWrapper)outputNameVar?.Clone();
        clone.scopeVar = (EnumWrapper)scopeVar?.Clone();
        clone.actor = (ActorWrapper)actor?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var varName = outputNameVar.GetValue(executeArgs);
        var scopeValue = (E_VariableScope)scopeVar.GetValue(executeArgs);
        VariableUtils.Set(scopeValue, varName, executeArgs, inputVar, actor.GetValueWithDefault(executeArgs, null));

        return E_ExecuteState.Success;
    }
}


[Preserve]
[GraphElementInfo(Category = "通用", Text = "设置变量(Float)", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_SetVariable_Float : ActionNodeBase
{
    [GraphVariable(LabelText = "输入变量", Required = true)]
    public FloatWrapper inputVar;

    [GraphVariable(LabelText = "目标变量名", Required = true)]
    public StringWrapper outputNameVar;

    [GraphVariable(LabelText = "作用域", Required = true, EnumType = typeof(E_VariableScope))]
    public EnumWrapper scopeVar = new Variable_Enum_Direct()
    {
        enumType = typeof(E_VariableScope),
        directValue = (int)E_VariableScope.Graph,
    };

    [GraphVariable(LabelText = "目标Actor", Required = false, TipsText = "Actor作用域时需要传入")]
    public ActorWrapper actor;


    public override object Clone()
    {
        var clone = new Action_Common_SetVariable_Float();
        clone.inputVar = (FloatWrapper)inputVar?.Clone();
        clone.outputNameVar = (StringWrapper)outputNameVar?.Clone();
        clone.scopeVar = (EnumWrapper)scopeVar?.Clone();
        clone.actor = (ActorWrapper)actor?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var varName = outputNameVar.GetValue(executeArgs);
        var scopeValue = (E_VariableScope)scopeVar.GetValue(executeArgs);
        VariableUtils.Set(scopeValue, varName,  executeArgs, inputVar, actor.GetValueWithDefault(executeArgs, null));

        return E_ExecuteState.Success;
    }
}

[Preserve]
[GraphElementInfo(Category = "通用", Text = "设置变量(Vector3)", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_SetVariable_Vector3 : ActionNodeBase
{
    [GraphVariable(LabelText = "输入变量", Required = true)]
    public Vector3Wrapper inputVar;

    [GraphVariable(LabelText = "目标变量名", Required = true)]
    public StringWrapper outputNameVar;

    [GraphVariable(LabelText = "作用域", Required = true, EnumType = typeof(E_VariableScope))]
    public EnumWrapper scopeVar = new Variable_Enum_Direct()
    {
        enumType = typeof(E_VariableScope),
        directValue = (int)E_VariableScope.Graph,
    };

    [GraphVariable(LabelText = "目标Actor", Required = false, TipsText = "Actor作用域时需要传入")]
    public ActorWrapper actor;


    public override object Clone()
    {
        var clone = new Action_Common_SetVariable_Vector3();
        clone.inputVar = (Vector3Wrapper)inputVar?.Clone();
        clone.outputNameVar = (StringWrapper)outputNameVar?.Clone();
        clone.scopeVar = (EnumWrapper)scopeVar?.Clone();
        clone.actor = (ActorWrapper)actor?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var varName = outputNameVar.GetValue(executeArgs);
        var scopeValue = (E_VariableScope)scopeVar.GetValue(executeArgs);
        VariableUtils.Set(scopeValue, varName,  executeArgs, inputVar, actor.GetValueWithDefault(executeArgs, null));

        return E_ExecuteState.Success;
    }
}


[Preserve]
[GraphElementInfo(Category = "通用", Text = "设置变量(String)", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_SetVariable_String : ActionNodeBase
{
    [GraphVariable(LabelText = "输入变量", Required = true)]
    public StringWrapper inputVar;

    [GraphVariable(LabelText = "目标变量名", Required = true)]
    public StringWrapper outputNameVar;

    [GraphVariable(LabelText = "作用域", Required = true, EnumType = typeof(E_VariableScope))]
    public EnumWrapper scopeVar = new Variable_Enum_Direct()
    {
        enumType = typeof(E_VariableScope),
        directValue = (int)E_VariableScope.Graph,
    };

    [GraphVariable(LabelText = "目标Actor", Required = false, TipsText = "Actor作用域时需要传入")]
    public ActorWrapper actor;


    public override object Clone()
    {
        var clone = new Action_Common_SetVariable_String();
        clone.inputVar = (StringWrapper)inputVar?.Clone();
        clone.outputNameVar = (StringWrapper)outputNameVar?.Clone();
        clone.scopeVar = (EnumWrapper)scopeVar?.Clone();
        clone.actor = (ActorWrapper)actor?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var varName = outputNameVar.GetValue(executeArgs);
        var scopeValue = (E_VariableScope)scopeVar.GetValue(executeArgs);
        VariableUtils.Set(scopeValue, varName, executeArgs, inputVar, actor.GetValueWithDefault(executeArgs, null));

        return E_ExecuteState.Success;
    }
}


[Preserve]
[GraphElementInfo(Category = "通用", Text = "设置变量(Actor)", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_SetVariable_Actor : ActionNodeBase
{
    [GraphVariable(LabelText = "输入变量", Required = true)]
    public ActorWrapper inputVar;

    [GraphVariable(LabelText = "目标变量名", Required = true)]
    public StringWrapper outputNameVar;

    [GraphVariable(LabelText = "作用域", Required = true, EnumType = typeof(E_VariableScope))]
    public EnumWrapper scopeVar = new Variable_Enum_Direct()
    {
        enumType = typeof(E_VariableScope),
        directValue = (int)E_VariableScope.Graph,
    };

    [GraphVariable(LabelText = "目标Actor", Required = false, TipsText = "Actor作用域时需要传入")]
    public ActorWrapper actor;


    public override object Clone()
    {
        var clone = new Action_Common_SetVariable_Actor();
        clone.inputVar = (ActorWrapper)inputVar?.Clone();
        clone.outputNameVar = (StringWrapper)outputNameVar?.Clone();
        clone.scopeVar = (EnumWrapper)scopeVar?.Clone();
        clone.actor = (ActorWrapper)actor?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var varName = outputNameVar.GetValue(executeArgs);
        var scopeValue = (E_VariableScope)scopeVar.GetValue(executeArgs);
        VariableUtils.Set(scopeValue, varName,  executeArgs, inputVar, actor.GetValueWithDefault(executeArgs, null));

        return E_ExecuteState.Success;
    }
}