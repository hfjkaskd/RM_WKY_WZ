using System;
using System.Collections.Generic;
using Bizza;
using UnityEngine;

/// <summary>
/// Update接口
/// </summary>
public interface IUpdate
{
    bool Enabled { get; }
    void OnUpdate(float delta);
}

public interface IReleaseUpdate
{
    bool ReleaseTag { get; set; }
    void OnUpdate(float delta);

    void OnLateUpdate(float delta)
    {
    }

    void OnReleaseUpdater();
}

public abstract class AutoUpdater : MonoBehaviour, IReleaseUpdate
{
    protected bool _releaseTag;

    bool IReleaseUpdate.ReleaseTag
    {
        get => _releaseTag;
        set => _releaseTag = value;
    }

    public abstract void OnUpdate(float delta);
    public virtual void OnReleaseUpdater()
    {
    }

    protected virtual void OnEnable()
    {
        World.Current.AddUpdater(this);
    }

    protected virtual void OnDisable()
    {
        World.Current.RemoveUpdater(this);
    }
}

/// <summary>
/// LateUpdate接口
/// </summary>
public interface ILateUpdate
{
    bool Enabled { get; }
    void OnLateUpdate(float delta);
}

/// <summary>
/// Update循环处理单元，辅助进行Update更新
/// </summary>
public class UpdateProcesser
{
    List<IUpdate> _updateComponents = new();
    List<ILateUpdate> _lateUpdatesComponents = new();

    private void AddCompToList<T>(ref List<T> array, object baseComponent) where T : class
    {
        T targetComp = baseComponent as T;
        if (targetComp == null)
            return;
        if (array == null)
        {
            array = new List<T>();
        }

        array.Add(targetComp);
    }

    public void AddUpdater(object comp)
    {
        AddCompToList(ref _updateComponents, comp);
        AddCompToList(ref _lateUpdatesComponents, comp);
    }

    public void RemoveUpdater(object comp)
    {
        _updateComponents.Remove(comp as IUpdate);
        _lateUpdatesComponents.Remove(comp as ILateUpdate);
    }

    public void Clear()
    {
        _updateComponents.Clear();
        _lateUpdatesComponents.Clear();
    }

    public void OnUpdate(float delta)
    {
        if (_updateComponents is { Count: > 0 })
        {
            for (int i = 0; i < _updateComponents.Count; i++)
            {
                if (!_updateComponents[i].Enabled)
                {
                    continue;
                }


#if BIZZA_GAME_PROFILING
                UnityEngine.Profiling.Profiler.BeginSample(_updateComponents[i].GetType().Name);
#endif

                _updateComponents[i].OnUpdate(delta);

#if BIZZA_GAME_PROFILING
                UnityEngine.Profiling.Profiler.EndSample();
#endif
            }
        }
    }

    public void OnLateUpdate(float delta)
    {
        if (_lateUpdatesComponents is { Count: > 0 })
        {
            for (int i = 0; i < _lateUpdatesComponents.Count; i++)
            {
                if (!_lateUpdatesComponents[i].Enabled)
                {
                    continue;
                }

                _lateUpdatesComponents[i].OnLateUpdate(delta);
            }
        }
    }
}