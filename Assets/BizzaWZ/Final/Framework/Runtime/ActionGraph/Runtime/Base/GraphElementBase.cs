using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
[Serializable]
public abstract class GraphElementBase : ICloneable
{
    private string _profilerDebugName;
    [JsonIgnore]
    internal string ProfilerDebugName
    {
        get
        {
            if (string.IsNullOrEmpty(_profilerDebugName))
            {
                _profilerDebugName = GetType().Name;
            }

            return _profilerDebugName;
        }
    }

    [HideInInspector][JsonIgnore]
    public Vector2 editorPos;

    public abstract object Clone();

    [JsonIgnore]
    public virtual string DebugName { get; }

    [JsonIgnore]
    public abstract bool HasControlInput { get; }

    [JsonIgnore]
    public abstract bool HasControlOutput { get; }

    public virtual bool CheckValid(StringBuilder sb)
    {
        return true;
    }

    public string GetTipsText()
    {
        var tipsText = "";
        var attr = GetType().GetCustomAttribute<GraphElementInfoAttribute>();

        //
        var fields = GetType().GetFields();
        foreach (var v in fields)
        {
            var varAttr = v.GetCustomAttribute<GraphVariableAttribute>();
            if (varAttr != null)
            {
                tipsText += "\n";
                tipsText += "\n";
                tipsText += $"<b>-[{(varAttr.Required ? "<color=#ffff00>必要</color>":"<color=grey>可空</color>")}]{varAttr.LabelText}：</b>{varAttr.TipsText}";
            }
        }

        if (!string.IsNullOrEmpty(tipsText) || (attr != null && !string.IsNullOrEmpty(attr.TipsText)))
        {
            if (attr != null)
            {
                var titleText = "";
                titleText = $"<size=20><b><color=#ff8844>{attr.Text}</color></b></size>";

                if (attr.TipsText != null)
                {
                    titleText += "\n";
                    titleText += $"<color=#bb8833>{attr.TipsText}</color>";
                }

                titleText += "\n-----------------------------------------------";
                tipsText = titleText + tipsText;
            }
        }

        return tipsText;
    }
}
