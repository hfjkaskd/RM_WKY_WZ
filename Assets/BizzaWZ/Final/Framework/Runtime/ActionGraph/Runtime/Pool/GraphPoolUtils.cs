using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GraphPoolUtils
{
    private static Dictionary<string, GraphPool> _graphPools = new();

    public static ActionGraphBase Get(ActionGraphBase origin)
    {
        if (origin == null)
        {
            ActionGraphLog.Error("release null");
            return null;
        }
        if (string.IsNullOrEmpty(origin.graphName))
        {
            ActionGraphLog.Error("graphName null");
            return null;
        }
        if (!_graphPools.TryGetValue(origin.graphName, out var pool))
        {
            pool = new GraphPool(origin);
            _graphPools.Add(origin.graphName, pool);
        }

        return pool.Get();
    }

    public static void Release(ActionGraphBase inst)
    {
        if (inst == null)
        {
            ActionGraphLog.Error("release null");
            return;
        }
        if (string.IsNullOrEmpty(inst.graphName))
        {
            ActionGraphLog.Error("graphName null");
            return;
        }

        if (!_graphPools.TryGetValue(inst.graphName, out var pool))
        {
            ActionGraphLog.Error($"pool not exist:{inst.graphName}");
            return;
        }

        pool.Release(inst);
    }

    public static void Clear()
    {
        if (_graphPools == null) return;
        foreach (var v in _graphPools)
        {
            v.Value.Clear();
        }
    }
}
