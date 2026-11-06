using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

 
[Preserve]
[GraphElementInfo(Category = "", Text = "直接值", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Enum_Direct : Variable_Enum
{
    public override string DebugName => $"枚举";

    public override bool IsDirectValue => true;

    public override Type EnumType => null;


    [LabelWidth(ActionGraphDefine.LabelWidth)]
    [HideLabel][CustomValueDrawer("DrawEnum")]
    public int directValue;

    [NonSerialized]
    public Type enumType;

    #if UNITY_EDITOR
    private int DrawEnum(int inValue, GUIContent label)
    {
        string GetText(object e)
        {
            var ret = "";
            var attr = ((Enum) e).GetCustomAttributeEx<LabelTextAttribute>();
            if (attr != null) { ret = attr.Text; }
            else { ret = e.ToString(); }
            return ret;
        }

        if (enumType == null || !enumType.IsEnum)
        {
            var s = GUILayout.TextField(directValue.ToString());
            if (int.TryParse(s, out var val))
            {
                directValue = val;
            }
            return directValue;
        }

        var btnText = "";
        bool bFlag = enumType.GetCustomAttribute<FlagsAttribute>() != null;
        var values = enumType.GetEnumValues();
        int line = 0;
        for (int i = 0; i < values.Length; i++)
        {
            var v = values.GetValue(i);
            var vi = (int) v;
            if (!bFlag)
            {
                if (vi == directValue)
                {
                    btnText = GetText(v);
                }
            }
            else
            {
                if ((vi & (vi - 1)) != 0) continue;

                if ((directValue & vi) != 0)
                {
                    if (!string.IsNullOrEmpty(btnText)) btnText += "\n";
                    btnText += GetText(v);
                    line++;
                }
            }
        }

        if (string.IsNullOrEmpty(btnText)) btnText = "[None]";
        if (line > 5) btnText = "[组合]";

        if (GUILayout.Button(btnText))
        {
            GenericMenu menu = new GenericMenu();
            int idx = 0;
            foreach (var v in values)
            {
                var vi = (int) v;
                bool on;
                if (!bFlag) on = vi == directValue;
                else on = vi != 0 && (vi & directValue) == vi;
                menu.AddItem(new GUIContent(GetText(v)), on, OnSelectIdx, idx);
                idx++;
            }

            menu.ShowAsContext();
        }
        return directValue;
    }

    private void OnSelectIdx(object userData)
    {
        bool bFlag = enumType.GetCustomAttribute<FlagsAttribute>() != null;

        var idx = (int) userData;
        var values = enumType.GetEnumValues();
        var valueInt = (int) values.GetValue(idx);
        if (valueInt == 0)
        {
            directValue = 0;
            return;
        }

        if (!bFlag)
        {
            directValue = valueInt;
        }
        else
        {
            var enumValue = valueInt;
            if ((directValue & enumValue) == 0) directValue |= enumValue;
            else directValue &= ~enumValue;
        }
    }
    #endif

    public override object Clone()
    {
        var clone = new Variable_Enum_Direct();
        clone.directValue = directValue;
        return clone;
    }

    public override int GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }

}
