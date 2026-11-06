using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Preserve]
[GraphElementInfo(Category = "", Text = "【直接值】", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Vector3_Direct : Variable_Vector3
{
    public override bool IsDirectValue => true;

    [HideLabel][CustomValueDrawer("CustomValueDrawer")]
    public Vector3 directValue;

#if UNITY_EDITOR
    private static Vector3 CustomValueDrawer(Vector3 inValue, GUIContent label)
    {
        const float LabelWidth = 20;
        const float ValueWidth = ActionGraphDefine.DirectVariableWidth - LabelWidth - 10;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("X:", GUILayout.Width(LabelWidth));
        var x = EditorGUILayout.TextField(inValue.x.ToString(), GUILayout.Width(ValueWidth));
        if (float.TryParse(x, out var ox)) { inValue.x = ox; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Y:", GUILayout.Width(LabelWidth));
        var y = EditorGUILayout.TextField(inValue.y.ToString(), GUILayout.Width(ValueWidth));
        if (float.TryParse(y, out var oy)) { inValue.y = oy; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Z:", GUILayout.Width(LabelWidth));
        var z = EditorGUILayout.TextField(inValue.z.ToString(), GUILayout.Width(ValueWidth));
        if (float.TryParse(z, out var oz)) { inValue.z = oz; }
        EditorGUILayout.EndHorizontal();
        return inValue;
    }
#endif

    public override object Clone()
    {
        var clone = new Variable_Vector3_Direct();
        clone.directValue = directValue;
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        return directValue;
    }
}