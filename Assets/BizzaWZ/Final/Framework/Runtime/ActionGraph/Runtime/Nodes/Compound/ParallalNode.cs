using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;

[Preserve]
[GraphElementInfo(Text = "并行", SupportTypes = new Type[] {typeof(ActionGraphBase)},
    TipsText = "并行执行全部子节点")]
[Obfuz.ObfuzIgnore]
public class ParallalNode : CompoundNodeBase
{
     [Obfuz.ObfuzIgnore]
    public enum E_ParallaExecuteMode
    {
        [LabelText("总是成功")]
        AlwaysSuccess,

        [LabelText("任意成功")]
        AnySuccess,

        [LabelText("任意失败")]
        AnyFailure,
    }

    public override string DebugName => "[并行]";

    [LabelText("执行方式")] [Obfuz.ObfuzIgnore]
    [GraphVariable(LabelText = "执行方式", Required = true, EnumType = typeof(E_ParallaExecuteMode), TipsText =
               " 总是成功：全部完成时结束，返回成功" +
               "\n任意成功：任意节点成功时结束并返回成功，全部失败返回失败" +
               "\n任意失败：任意节点失败时结束并返回成功，全部成功返回失败")]
    public EnumWrapper parallaExecuteMode =  new Variable_Enum_Direct()
    {
        directValue = (int)E_ParallaExecuteMode.AlwaysSuccess,
        enumType = typeof(E_ParallaExecuteMode),
    };

    private E_ExecuteState[] _childExecuteRet;

    public override object Clone()
    {
        var clone = new ParallalNode();
        clone.parallaExecuteMode = (EnumWrapper)parallaExecuteMode?.Clone();
        return clone;
    }

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(in executeArgs);
        if (_childExecuteRet == null || _childExecuteRet.Length != children.Count)
        {
            _childExecuteRet = new E_ExecuteState[children.Count];
        }
        else
        {
            Array.Clear(_childExecuteRet, 0, _childExecuteRet.Length);
        }
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        if (children.Count == 0)
        {
            return E_ExecuteState.Success;
        }

        var parallaExecuteModeValue = (E_ParallaExecuteMode)parallaExecuteMode.GetValue(executeArgs);
        bool anyFailed = false;
        bool anySuccess = false;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (_childExecuteRet[i] == E_ExecuteState.None && child.CurExecuteState == E_ExecuteState.None || child.CurExecuteState == E_ExecuteState.Running)
            {
                var childRet = child.Execute(executeArgs);
                if (childRet == E_ExecuteState.Failed)
                {
                    anyFailed = true;
                    _childExecuteRet[i] = E_ExecuteState.Failed;
                    if (parallaExecuteModeValue == E_ParallaExecuteMode.AnyFailure)
                    {
                        break;
                    }
                }

                if (childRet == E_ExecuteState.Success)
                {
                    anySuccess = true;
                    _childExecuteRet[i] = E_ExecuteState.Success;
                    if (parallaExecuteModeValue == E_ParallaExecuteMode.AnySuccess)
                    {
                        break;
                    }
                }
            }
        }

        bool allFinish = true;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child.CurExecuteState == E_ExecuteState.Running)
            {
                allFinish = false;
            }
        }

        if (parallaExecuteModeValue == E_ParallaExecuteMode.AlwaysSuccess)
        {
            return allFinish ? E_ExecuteState.Success : E_ExecuteState.Running;
        }
        else if (parallaExecuteModeValue == E_ParallaExecuteMode.AnyFailure)
        {
            if (anyFailed)
            {
                return E_ExecuteState.Failed;
            }

            return allFinish ? E_ExecuteState.Success : E_ExecuteState.Running;
        }
        else if (parallaExecuteModeValue == E_ParallaExecuteMode.AnySuccess)
        {
            if (anySuccess)
            {
                return E_ExecuteState.Success;
            }

            return allFinish ? E_ExecuteState.Failed : E_ExecuteState.Running;
        }

        return E_ExecuteState.Success;
    }
}
