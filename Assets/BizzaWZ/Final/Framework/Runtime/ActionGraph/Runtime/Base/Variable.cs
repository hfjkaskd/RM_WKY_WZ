using System;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 变量类型
/// </summary>
  [Obfuz.ObfuzIgnore]
public enum E_VariableType
{
    None,
    Bool = 1,
    Float = 2,
    // Vector2 = 3,
    Vector3 = 4,
    String = 5,
    Actor = 6,
    Enum = 7,
    Curve = 8,
    ActorList = 9,
    Transform = 10,
}

/// <summary>
/// 变量作用域
/// </summary>
  [Obfuz.ObfuzIgnore]

public enum E_VariableScope
{
    Graph,
    Actor,
    Global,
    BattleGlobal,
}

[Serializable]
public partial class Variable : ICloneable//TODO:修改为联合体
{
    [LabelText("变量名")] public string varName;
    [ReadOnly]
    [LabelText("类型")] public E_VariableType varType;
    // [LabelText("作用域")] public E_VariableScope scopeType;

    [ShowIf("@varType==E_VariableType.Bool")][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public BoolWrapper boolValue;

    [ShowIf("@varType==E_VariableType.Float")][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public FloatWrapper floatValue;

    [ShowIf("@varType==E_VariableType.Vector3")][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public Vector3Wrapper vector3Value;

    [ShowIf("@varType==E_VariableType.String")][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public StringWrapper stringValue;

    [ShowIf("@varType==E_VariableType.Actor")][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public ActorWrapper actorValue;

    [ShowIf("@varType==E_VariableType.Enum")][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public EnumWrapper enumValue;

    [ShowIf("@varType==E_VariableType.Curve")][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public CurveWrapper curveValue;

    public object Clone()
    {
        var clone = new Variable();
        clone.varName = varName;
        clone.varType = varType;
        clone.boolValue = (BoolWrapper)boolValue?.Clone();
        clone.floatValue = (FloatWrapper)floatValue?.Clone();
        clone.vector3Value = (Vector3Wrapper)vector3Value?.Clone();
        clone.stringValue = (StringWrapper)stringValue?.Clone();
        clone.actorValue = (ActorWrapper)actorValue?.Clone();
        clone.enumValue = (EnumWrapper)enumValue?.Clone();
        clone.curveValue = (CurveWrapper)curveValue?.Clone();
        return clone;
    }

    public static VariableBase CreateDirectValue(E_VariableType type)
    {
        VariableBase variable = null;
        switch (type)
        {
            case E_VariableType.Bool:
                variable = new Variable_Bool_Direct();
                break;
            case E_VariableType.Float:
                variable = new Variable_Float_Direct();
                break;
            case E_VariableType.Vector3:
                variable = new Variable_Vector3_Direct();
                break;
            case E_VariableType.String:
                variable = new Variable_String_Direct();
                break;
            case E_VariableType.Actor:
                variable = new Variable_Actor_Direct();
                break;
            case E_VariableType.Enum:
                variable = new Variable_Enum_Direct();
                break;
            case E_VariableType.Curve:
                variable = new Variable_Curve_Direct();
                break;
            case E_VariableType.ActorList:
                variable = new Variable_ActorList_Direct();
                break;
        }

        return variable;
    }

    public static Variable Create(string name, VariableBase internalValue)
    {
        var type = internalValue.VariableType;
        var variable = new Variable();
        variable.varType = type;
        variable.varName = name;
        switch (type)
        {
            case E_VariableType.Bool:
                variable.boolValue = new();
                variable.boolValue.SetInternalValue(internalValue);
                break;
            case E_VariableType.Float:
                variable.floatValue = new();
                variable.floatValue.SetInternalValue(internalValue);
                break;
            case E_VariableType.Vector3:
                variable.vector3Value = new();
                variable.vector3Value.SetInternalValue(internalValue);
                break;
            case E_VariableType.String:
                variable.stringValue = new();
                variable.stringValue.SetInternalValue(internalValue);
                break;
            case E_VariableType.Actor:
                variable.actorValue = new();
                variable.actorValue.SetInternalValue(internalValue);
                break;
            case E_VariableType.Enum:
                variable.enumValue = new();
                variable.enumValue.SetInternalValue(internalValue);
                break;
            case E_VariableType.Curve:
                variable.curveValue = new();
                variable.curveValue.SetInternalValue(internalValue);
                break;
        }

        return variable;
    }

    public bool GetValue_Bool(in ExecuteArgs executeArgs)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Bool, "variable type error");
        return boolValue.GetValue(executeArgs);
    }

    public float GetValue_Float(in ExecuteArgs executeArgs)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Float, "variable type error");
        return floatValue.GetValue(executeArgs);
    }

    public Vector3 GetValue_Vector3(in ExecuteArgs executeArgs)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Vector3, "variable type error");
        return vector3Value.GetValue(executeArgs);
    }

    public string GetValue_String(in ExecuteArgs executeArgs)
    {
        ActionGraphLog.Assert(varType == E_VariableType.String, "variable type error");
        return stringValue.GetValue(executeArgs);
    }

    public GameActor GetValue_Actor(in ExecuteArgs executeArgs)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Actor, "variable type error");
        return actorValue.GetValue(executeArgs);
    }



    public void SetValue_Bool(in ExecuteArgs executeArgs, bool value)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Bool, "variable type error");
        boolValue.SetValue(executeArgs, value);
    }

    public void SetValue_Float(in ExecuteArgs executeArgs, float value)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Float, "variable type error");
        floatValue.SetValue(executeArgs, value);
    }

    public void SetValue_Vector3(in ExecuteArgs executeArgs, Vector3 value)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Vector3, "variable type error");
        vector3Value.SetValue(executeArgs, value);
    }

    public void SetValue_String(in ExecuteArgs executeArgs, string value)
    {
        ActionGraphLog.Assert(varType == E_VariableType.String, "variable type error");
        stringValue.SetValue(executeArgs, value);
    }

    public void SetValue_Actor(in ExecuteArgs executeArgs, GameActor value)
    {
        ActionGraphLog.Assert(varType == E_VariableType.Actor, "variable type error");
        actorValue.SetValue(executeArgs, value);
    }
}