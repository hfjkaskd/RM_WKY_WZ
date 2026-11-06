using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class ActionGraphLog
{
    #region Log
    public static void Verbose(string text)
    {
#if ENABLE_ACTION_GRAPH_PROFILER
        return;
#endif
        if (!ActionGraphDefine.EnableLog)
        {
            return;
        }
        //  // LogUtil.Verbose(text);
        Debug.Log(text);
    }

    public static void Info(string text)
    {
#if ENABLE_ACTION_GRAPH_PROFILER
        return;
#endif
        if (!ActionGraphDefine.EnableLog)
        {
            return;
        }
        //  // LogUtil.Info(text);
        Debug.Log(text);
    }

    public static void Warning(string text)
    {
#if ENABLE_ACTION_GRAPH_PROFILER
        return;
#endif
        if (!ActionGraphDefine.EnableLog)
        {
            return;
        }

        //  // LogUtil.Warning(text);
        Debug.LogError(text);
    }

    public static void Error(ActionGraphBase graph, string text)
    {
#if ENABLE_ACTION_GRAPH_PROFILER
        return;
#endif
        // if (!ActionGraphDefine.EnableLog)
        // {
        //     return;
        // }
        LogLogger.LogError($"[CLICKABLE:{{ActionGraph}}$$${graph.graphName}$$$]" + text);
        // Debug.LogError(text);
    }

    public static void Error(string text)
    {
#if ENABLE_ACTION_GRAPH_PROFILER
        return;
#endif
        // if (!ActionGraphDefine.EnableLog)
        // {
        //     return;
        // }
        LogLogger.LogError(text);
        // Debug.LogError(text);
    }

    public static void Assert(bool condition, string text)
    {
#if ENABLE_ACTION_GRAPH_PROFILER
        return;
#endif
        // if (!ActionGraphDefine.EnableLog)
        // {
        //     return;
        // }
        LogLogger.LogAssert(condition, text);
        // Debug.Assert(condition, text);
    }
    #endregion
}