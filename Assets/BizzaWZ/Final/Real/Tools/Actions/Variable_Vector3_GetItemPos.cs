#if BIZZA_REAL_WITHDRAW
using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Obfuz.ObfuzIgnore]
[Preserve]
[GraphElementInfo(Category = "CustomAction", Text = "获取元素位置", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
public class Variable_Vector3_GetItemPos : Variable_Vector3
{
    [GraphVariable("行", true)]
    public FloatWrapper row;
    [GraphVariable("列", true)]
    public FloatWrapper col;

    public override object Clone()
    {
        var clone = new Variable_Vector3_GetItemPos();
        clone.row = (FloatWrapper)row?.Clone();
        clone.col = (FloatWrapper)col?.Clone();
        return clone;
    }

    public override Vector3 GetValue(in ExecuteArgs executeArgs)
    {
        Vector3 pos = Vector3.zero;
        // if (SpriteGridSpawner.Instance == null)
        // {
        //     LogLogger.LogError("SpriteGridSpawner.Instance == null");
        //     return Vector3.zero;
        // }
        // pos = SpriteGridSpawner.Instance.GetCustomGridTileElementPosition();
        return pos;
    }
}
#endif
