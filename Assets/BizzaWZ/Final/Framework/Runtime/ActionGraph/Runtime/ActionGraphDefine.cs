using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ActionGraphDefine
{
    public static E_ExecuteState ActionExceptionResult = E_ExecuteState.Success;//动作节点执行异常后的返回值，如播放动画但是没有配置Actor

    public const float InspectorWidth = 400;

    public const float LabelWidth = 60f;

    public const float DirectVariableWidth = 120f;

    public static bool EnableLog = false;
}

public static partial class EventDefine
{
    public static class GraphEvent
    {
        public static GameEvent<string, string, float, GameActor> SendGraphEventToScript = new();
        public static GameEvent<string> SendGraphEvent = new();
    }
}