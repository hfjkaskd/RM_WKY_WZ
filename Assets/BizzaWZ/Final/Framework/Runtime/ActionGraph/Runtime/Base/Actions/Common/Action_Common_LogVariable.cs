using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "通用", Text = "输出变量日志", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Action_Common_LogVariable : ActionNodeBase
{
    [GraphVariable(LabelText = "bool变量", Required = false)]
    public BoolWrapper boolVar;

    [GraphVariable(LabelText = "float变量", Required = false)]
    public FloatWrapper floatVar;

    [GraphVariable(LabelText = "vector3变量", Required = false)]
    public Vector3Wrapper vector3Var;

    [GraphVariable(LabelText = "string变量", Required = false)]
    public StringWrapper stringVar;

    [GraphVariable(LabelText = "actor变量", Required = false)]
    public ActorWrapper actorVar;


    public override object Clone()
    {
        var clone = new Action_Common_LogVariable();
        clone.boolVar = (BoolWrapper)boolVar?.Clone();
        clone.floatVar = (FloatWrapper)floatVar?.Clone();
        clone.vector3Var = (Vector3Wrapper)vector3Var?.Clone();
        clone.stringVar = (StringWrapper)stringVar?.Clone();
        clone.actorVar = (ActorWrapper)actorVar?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        LogLogger.LogInfo($"[{Time.frameCount}]<color=cyan>" +
                     $"   bool:{boolVar.ToString(executeArgs)}" +
                     $"   float:{floatVar.ToString(executeArgs)}" +
                     $"   string:{stringVar.ToString(executeArgs)}" +
                     $"   vector3:{vector3Var.ToString(executeArgs)}" +
                     $"   actor:{actorVar.ToString(executeArgs)}</color>");
        return E_ExecuteState.Success;
    }
}