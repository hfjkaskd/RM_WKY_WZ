using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;


 
[Preserve]
[GraphElementInfo(Category = "教学", Text = "显示遮罩(Path)", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Action_Teach_ShowMaskByPath : ActionNodeBase
{
    [GraphVariable(LabelText = "节点路径", Required = false)]
    public StringWrapper path = new Variable_String_Direct();

    [GraphVariable(LabelText = "世界坐标", Required = false)]
    public Vector3Wrapper worldPos;

    [GraphVariable(LabelText = "是否阻断", Required = true)]
    public BoolWrapper block = new Variable_Bool_Direct();

    [GraphVariable(LabelText = "遮罩图形(默认矩形)", Required = false, EnumType = typeof(UITeachMaskPage.Shape))]
    public EnumWrapper shape = new Variable_Enum_Direct()
    {
        enumType = typeof(UITeachMaskPage.Shape),
        directValue = (int)UITeachMaskPage.Shape.Rect,
    };

    [GraphVariable(LabelText = "点击背景关闭", Required = false)]
    public BoolWrapper bgBlock;

    [GraphVariable(LabelText = "遮罩宽", Required = false)]
    public FloatWrapper width;

    [GraphVariable(LabelText = "遮罩高", Required = false)]
    public FloatWrapper height;

    [GraphVariable(LabelText = "背景透明度(默认220)", Required = false)]
    public FloatWrapper alpha;

    [GraphVariable(LabelText = "显示手指", Required = false)]
    public BoolWrapper showHand;


    public override object Clone()
    {
        var clone = new Action_Teach_ShowMaskByPath();
        clone.path = (StringWrapper)path?.Clone();
        clone.worldPos = (Vector3Wrapper)worldPos?.Clone();
        clone.block = (BoolWrapper)block?.Clone();
        clone.shape = (EnumWrapper)shape?.Clone();
        clone.bgBlock = (BoolWrapper)bgBlock?.Clone();
        clone.width = (FloatWrapper)width?.Clone();
        clone.height = (FloatWrapper)height?.Clone();
        clone.alpha = (FloatWrapper)alpha?.Clone();
        clone.showHand = (BoolWrapper)showHand?.Clone();
        return clone;
    }

    private bool _completed;

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        var pathValue = path.GetValueWithDefault(executeArgs, "");
        var blockValue = block.GetValue(executeArgs);
        var shapeValue = (UITeachMaskPage.Shape)shape.GetValueWithDefault(executeArgs, (int)UITeachMaskPage.Shape.Rect);
        var bgBlockValue = bgBlock.GetValueWithDefault(executeArgs, false);
        var widthValue = width.GetValueWithDefault(executeArgs, -1);
        var heightValue = height.GetValueWithDefault(executeArgs, -1);
        var alphaValue = alpha.GetValueWithDefault(executeArgs, 120);
        var showHandValue = showHand.GetValueWithDefault(executeArgs, true);
        UITeachMaskPage.InitParam param = new()
        {
            type = string.IsNullOrEmpty(pathValue) ? UITeachMaskPage.Type.Pos : UITeachMaskPage.Type.Path,
            path = pathValue,
            worldPos = worldPos.GetValueWithDefault(executeArgs, default),
            block = blockValue,
            shape = shapeValue,
            clickCD = 0,
            bgBlock = bgBlockValue,
            width = widthValue,
            height = heightValue,
            alpha = alphaValue,
            retryCount = 5,
            retryInterval = 0.3f,
            showHand = showHandValue,
        };
        _completed = false;
        UIModule.Instance.OpenPage(UIPageIds.UI_TeachMask, param).Forget();
        // _completed = !blockValue;
        // if (blockValue)
        // {
        //     BizzaEventSystem.On(EventDefine.Frame.ClosePage, OnClosePage);
        // }
       
    }

    private void OnClosePage(PageId pageName)
    {
        if (UIPageIds.UI_TeachMask == pageName)
        {
            _completed = true;
        }
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (UIModule.Instance.GetPage(UIPageIds.UI_TeachMask) != null)
        {
            _completed = true;
        }

        return _completed ? E_ExecuteState.Success : E_ExecuteState.Running;
    }

    protected override void OnExit(in ExecuteArgs executeArgs, bool interrupt)
    {
        base.OnExit(in executeArgs, interrupt);
        BizzaEventSystem.Off(EventDefine.Frame.ClosePage, OnClosePage);
    }
}
