using System;using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

/// <summary>
/// 代码中使用的变量
/// 可以选择具体的实现
/// </summary>
[Preserve]
[Serializable]
public abstract class VariableWrapperBase : ICloneable
{
    // [HideInInspector]
    // public Vector2 editorPos;
    // public string nodeGuid;

    // [ShowInInspector]
    public abstract E_VariableType VariableType { get; }
    public string DebugName => GetInternalValue() == null ? "null" : GetInternalValue().DebugName;

    [SerializeReference][HideLabel][HideReferenceObjectPicker][InlineProperty]
    public VariableBase internalValue;
    [HideInInspector]
    public int idxInList = -1;
    [HideInInspector]
    public int nodeIdx = -1;//针对事件中的输出节点
    [HideInInspector]
    public int nodeVarIdx = -1;//针对事件中的输出节点

    public void Reset()
    {
        idxInList = -1;
        nodeIdx = -1;
        nodeVarIdx = -1;
        SetInternalValue(null);
    }

    [Conditional("UNITY_EDITOR")]
    protected void OnGetValue(VariableBase variable, object o)
    {
        variable.debugValue = o;
        variable.lastHasDebugValueFrame = Time.frameCount;
    }

    public void SetInternalValue(VariableBase variable)
    {
        internalValue = variable;
    }

    public VariableBase GetInternalValue()
    {
        return internalValue;
    }
    public abstract object Clone();
}

public static class VariableWrapperBaseExt
{
    public static bool IsNull(this VariableWrapperBase wrapperBase)
    {
        if (wrapperBase == null || wrapperBase.internalValue == null)
        {
            return true;
        }
        return false;
    }
}

#if UNITY_EDITOR

public class VariableWrapperHideProcessor : OdinAttributeProcessor<FieldInfo>
{
    public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
        MemberInfo member,
        List<Attribute> attributes)
    {
        if (member.DeclaringType.IsSubclassOf(typeof(VariableWrapperBase)))
        {
            // 动态添加 [HideInInspector]
            attributes.Add(new HideInInspector());
        }
    }
}
#endif