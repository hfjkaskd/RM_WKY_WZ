using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(Category = "基础", Text = "获取列表数量", SupportTypes = new Type[] {typeof(ActionGraphBase)}, TipsText = "null返回0")]
[Obfuz.ObfuzIgnore]
public class Variable_Float_ListCount : Variable_Float
{
    [GraphVariable("列表", true)]
    public ActorListWrapper list;

    public override object Clone()
    {
        var clone = new Variable_Float_ListCount();
        clone.list = (ActorListWrapper) list?.Clone();
        return clone;
    }

    public override float GetValue(in ExecuteArgs executeArgs)
    {
        var ret = list.GetValue(executeArgs);
        if (ret == null) return 0;
        return ret.Count;
    }
}
