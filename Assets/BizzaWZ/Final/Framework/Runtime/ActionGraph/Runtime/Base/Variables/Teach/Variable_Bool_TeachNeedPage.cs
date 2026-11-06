using System;
using UnityEngine.Scripting;

 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "教学打开界面条件", SupportTypes = new Type[] {typeof(ActionGraphBase)})]
[Obfuz.ObfuzIgnore]
public class Variable_Bool_TeachNeedPage : Variable_Bool
{
    [GraphVariable(LabelText = "界面Id", TipsText = "", Required = true)]
    public StringWrapper pageId = new Variable_String_Direct();

    public override string DebugName => "教学打开界面条件";


    public override object Clone()
    {
        return new Variable_Bool_TeachNeedPage
        {
            pageId = pageId.Clone() as StringWrapper,
        };
    }

    public override bool GetValue(in ExecuteArgs executeArgs)
    {
        var pageName = new PageId(pageId.GetValue(executeArgs));
        return UIModule.Instance.GetPage(pageName);
    }
}
