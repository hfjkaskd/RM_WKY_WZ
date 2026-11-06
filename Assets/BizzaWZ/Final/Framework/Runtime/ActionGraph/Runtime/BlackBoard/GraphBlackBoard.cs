using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityExtensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class GraphBlackBoard : ICloneable
{
    public IReadOnlyList<Variable> Variables => _variables;

    [ShowInInspector, LabelText("变量列表"), OnInspectorGUI("Editor_AddCustomButton"), ListDrawerSettings(HideAddButton = true)]
    [SerializeField]
    private List<Variable> _variables = new();

    public bool GetBool(string name, bool defaultValue = false)
    {
        var v = Get(name);
        if (v == null) return defaultValue;
        return v.GetValue_Bool(default);
    }

    public Variable Get(string name)
    {
        foreach (var v in _variables)
        {
            if (v.varName == name)
            {
                return v;
            }
        }

        return null;
    }

    public void Set(Variable variable)
    {
        var old = Get(variable.varName);
        //替换
        if (old != null)
        {
            var idx = _variables.IndexOf(old);
            _variables[idx] = variable;
            return;
        }

        //新增
        _variables.Add(variable);
    }

    public object Clone()
    {
        var clone = new GraphBlackBoard();
        clone._variables = _variables == null ? null : new List<Variable>();
        foreach (var v in _variables)
        {
            clone._variables.Add((Variable)v.Clone());
        }

        return clone;
    }

    public void CopyFrom(GraphBlackBoard target)
    {
        //TODO:优化GC
        _variables.Clear();
        foreach (var v in target._variables)
        {
            _variables.Add((Variable)v.Clone());
        }
    }

    public void Clear()
    {
        _variables.Clear();
    }

#if UNITY_EDITOR
    private void Editor_AddCustomButton()
    {
        var shouldSetDirty = false;
        var addFuncBtn = EditorGUIUtility.TrIconContent("Animation.AddEvent");
        addFuncBtn.text = "添加";

        if (EditorGUILayout.DropdownButton(addFuncBtn, FocusType.Keyboard, GUILayout.MinWidth(10)))
        {
            var m_Menu = new GenericMenu();
            foreach (E_VariableType varType in Enum.GetValues(typeof(E_VariableType)))
            {
                if (varType == E_VariableType.None) continue;
                m_Menu.AddItem(new GUIContent(varType.ToString()), false, Editor_OnValueChanged,
                    Variable.CreateDirectValue(varType).GetType());
            }

            foreach (var type in TypeCache.GetTypesDerivedFrom<VariableBase>())
            {
                var atr = type.GetCustomAttribute<LabelTextAttribute>();
                if (atr != null)
                {

                    m_Menu.AddItem(new GUIContent(atr.Text), false, Editor_OnValueChanged, type);
                }
            }

            m_Menu.ShowAsContext();
        }
    }

    /// <summary>
    /// 选中方法Item后触发的回调
    /// </summary>
    /// <param name="value"></param>
    private void Editor_OnValueChanged(object value)
    {
        var m_ItemTypeToAdd = value as Type;

        if (m_ItemTypeToAdd != null)
        {
            if (Activator.CreateInstance(m_ItemTypeToAdd) is VariableBase inst)
            {
                _variables.Add(Variable.Create("", inst));
            }
        }
    }
#endif

}
