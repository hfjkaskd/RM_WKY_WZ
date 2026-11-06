using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;


/// <summary>
/// 变量列表/黑板
/// </summary>
[Serializable]
public class VariableContainer
{
    public VariableContainer()
    {
    }

    [LabelText("变量列表")]
    public List<Variable> variables = new();

    public void Set(Variable value)
    {
        var key = value.varName;
        var old = Get(key);
        if (old != null)
        {
            variables[variables.IndexOf(old)] = value;
        }
        else
        {
            variables.Add(value);
        }
    }

    public Variable Get(string key)
    {
        foreach (var v in variables)
        {
            if (v.varName == key)
            {
                return v;
            }
        }

        return null;
    }
}