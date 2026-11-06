using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "显示提示", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Action_Teach_ShowTip : ActionNodeBase
{
    [GraphVariable(LabelText = "文本内容", Required = true)]
    public StringWrapper content = new Variable_String_Direct()
    {
        directValue = "在这里输入提示文本",
    };

    [GraphVariable(LabelText = "是否阻断", Required = true)]
    public BoolWrapper block = new Variable_Bool_Direct()
    {
        directValue = true,
    };

    [GraphVariable(LabelText = "位置索引", Required = false, TipsText = "-1时使用位置高度")]
    public FloatWrapper posIdx = new Variable_Float_Direct()
    {
        directValue = -1,
    };

    [GraphVariable(LabelText = "位置高度(0底~1顶)", Required = false)]
    public FloatWrapper height = new Variable_Float_Direct()
    {
        directValue = 0.5f,
    };

    [GraphVariable(LabelText = "背景透明度(默认0)", Required = false)]
    public FloatWrapper alpha = new Variable_Float_Direct()
    {
        directValue = 0,
    };

    public override object Clone()
    {
        var clone = new Action_Teach_ShowTip();
        clone.content = (StringWrapper)content?.Clone();
        clone.block = (BoolWrapper)block?.Clone();
        clone.posIdx = (FloatWrapper)posIdx?.Clone();
        clone.alpha = (FloatWrapper)alpha?.Clone();
        clone.height = (FloatWrapper)height?.Clone();
        return clone;
    }

    private bool isOpenPage = false;

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        var contentValue = content.GetValue(executeArgs);
        var blockValue = block.GetValue(executeArgs);
        var posIdxValue = posIdx.GetValueIntWithDefault(executeArgs, -1);
        var alphaValue = alpha.GetValueWithDefault(executeArgs, 0);
        var heightValue = height.GetValueWithDefault(executeArgs, 0.5f);
        UITeachTipsPage.InitParam param = new()
        {
            content = contentValue,
            block = blockValue,
            posIdx = posIdxValue,
            alpha = alphaValue,
            heightValue = heightValue,
        };
        UIModule.Instance.OpenPage(UIPageIds.UI_TeachTip, param).Forget();
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var blockValue = block.GetValue(executeArgs);
        if (blockValue)
        {
            if (UIModule.Instance.GetPage<UITeachTipsPage>())
                isOpenPage = true;
            else if (isOpenPage && !UIModule.Instance.GetPage<UITeachTipsPage>())
                return E_ExecuteState.Success;
        }
        else
        {
            if (UIModule.Instance.GetPage(UIPageIds.UI_TeachTip) != null)
            {
                return E_ExecuteState.Success;
            }
        }
        return E_ExecuteState.Running;
    }
}
