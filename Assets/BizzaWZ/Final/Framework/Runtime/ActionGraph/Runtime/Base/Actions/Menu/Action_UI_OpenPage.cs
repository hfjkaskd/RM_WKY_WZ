using System;
using System.Collections;
using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting;

[Obfuz.ObfuzIgnore]
public abstract class WaitPageCloseNode : ActionNodeBase
{
    protected PageId _pageName;
    protected bool _completed;

    protected void Register(PageId pageName)
    {
        _completed = false;
        _pageName = pageName;
        BizzaEventSystem.On(EventDefine.Frame.ClosePage, OnClosePage);
    }
    
    private void OnClosePage(PageId pageName)
    {
        if (_pageName == pageName)
        {
            _completed = true;
        }
    }
    
    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        return _completed ? E_ExecuteState.Success : E_ExecuteState.Running;
    }
    
    protected override void OnExit(in ExecuteArgs executeArgs, bool interrupt)
    {
        base.OnExit(in executeArgs, interrupt);
        BizzaEventSystem.Off(EventDefine.Frame.ClosePage, OnClosePage);
    }
}


[Preserve]
[GraphElementInfo(Category = "UI", Text = "打开界面", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Action_UI_OpenPage : WaitPageCloseNode
{
    [GraphVariable(LabelText = "界面Id", TipsText = "", Required = true)]
    public StringWrapper pageId = new Variable_String_Direct();

    [GraphVariable(LabelText = "是否等待界面关闭", Required = false)]
    public BoolWrapper suspend = new BoolWrapper();

    public override object Clone()
    {
        var clone = new Action_UI_OpenPage();
        clone.pageId = (StringWrapper)pageId?.Clone();
        clone.suspend = (BoolWrapper)suspend?.Clone();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(in executeArgs);
        var isSuspend = suspend.GetValueWithDefault(executeArgs, false);
        _pageName = new PageId(pageId.GetValue(executeArgs));
        if (UIModule.Instance.GetPage(_pageName) == null)
        {
            UIModule.Instance.OpenPage(_pageName).Forget();
            if (isSuspend)
            {
                Register(_pageName);
            }
            else
            {
                _completed = true;
            }
        }
        else
        {
            _completed = true;
        }
    }
}


 
[Preserve]
[GraphElementInfo(Category = "UI", Text = "关闭界面", SupportTypes = new Type[] { typeof(ActionGraphBase) })]
[Obfuz.ObfuzIgnore]
public class Action_UI_ClosePage : ActionNodeBase
{
    [GraphVariable(LabelText = "界面Id", TipsText = "", Required = true)]
    public StringWrapper pageId = new Variable_String_Direct();

    public override object Clone()
    {
        var clone = new Action_UI_ClosePage();
        clone.pageId = (StringWrapper)pageId?.Clone();
        return clone;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        var pageName = new PageId(pageId.GetValue(executeArgs));
        UIModule.Instance.ClosePage(pageName);
        return E_ExecuteState.Success;
    }
}
