using System;
using Sirenix.OdinInspector;
using UnityEngine.Scripting;


[Preserve]
[GraphElementInfo(SupportTypes = new Type[]{})]
[Obfuz.ObfuzIgnore]
public class FrameNode : DecoratorNodeBase
{
    [LabelText("总帧数")]
    public int totalFrame;

    public override object Clone()
    {
        var clone = new FrameNode();
        clone.totalFrame = totalFrame;
        return clone;
    }

    public int CurFrame => _curFrame;
    private int _curFrame;

    protected override void OnEnter(in ExecuteArgs executeArgs)
    {
        base.OnEnter(in executeArgs);
        _curFrame = 0;
    }

    protected override E_ExecuteState OnExecute(in ExecuteArgs executeArgs)
    {
        _curFrame++;
        if (_curFrame >= totalFrame)
        {
            return E_ExecuteState.Success;
        }

        return E_ExecuteState.Running;
    }
}
