using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphPool
{
    private ActionGraphBase _origin;

    public GraphPool(ActionGraphBase origin)
    {
        _origin = origin;
    }

    /// <summary>
    /// 所有可用的实例集合
    /// </summary>
    private LinkedList<ActionGraphBase> _availableInstances = new();

    /// <summary>
    /// 所有使用中的实例集合
    /// </summary>
    private HashSet<ActionGraphBase> _inUseInstances = new();

    public ActionGraphBase Get()
    {
        if (_availableInstances.Count == 0)
        {
            for (var i = 0; i < 1; i++)
            {
                var inst = InternalCreateInstance();
                _availableInstances.AddLast(inst);
            }
        }

        var go = _availableInstances.First.Value;
        _availableInstances.RemoveFirst();
        _inUseInstances.Add(go);
        go.GetFromPool();
        return go;
    }

    public void Release(ActionGraphBase graph)
    {
        _availableInstances.AddLast(graph);
        InternalReleaseInstance(graph);
    }

    public void Clear()
    {
        _availableInstances.Clear();
    }

    private ActionGraphBase InternalCreateInstance()
    {
        return (ActionGraphBase)_origin.Clone();
    }

    private void InternalReleaseInstance(ActionGraphBase graph)
    {
        graph.ReleaseToPool();
    }
}
