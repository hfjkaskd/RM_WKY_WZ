using System;
using System.Collections.Generic;
using Bizza;
using Sirenix.OdinInspector;
using UnityEngine;

//[DisallowMultipleComponent]
public class Actor : MonoBehaviour, IReleaseUpdate
{
    public bool inited { get; private set; }
    public World CurrentWorld { get; private set; }
    public bool Available { get; set; }

    #region GUID

    private static ulong nextGuid = 1;

    public static void ClearGuid()
    {
        nextGuid = 1;
    }

    private void InitGuid()
    {
        if (nextGuid == ulong.MaxValue)
        {
            throw new Exception("over limit");
        }

        _guid = nextGuid++;
        spawnedTime = World.Current.GameTime;
    }

    [ShowInInspector]
    public float spawnedDuration
    {
        get
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) return 0;
#endif
            return World.Current.GameTime - spawnedTime;
        }
    }

    public float spawnedTime;
    protected ulong _guid;
    [ShowInInspector] public ulong GUID => _guid;

    public static bool IsValid(Actor actor)
    {
        return (object)actor != null && actor.inited;
    }

    #endregion

    #region Cmpt

    protected List<IBizzaComponent<Actor>> _components = new();

    public void AddComponent(IBizzaComponent<Actor> component)
    {
        if (!_components.Contains(component))
        {
            _components.Add(component);
        }
    }

    #endregion

    public void ActorUpdate(float delta)
    {
        if (!inited) return;
        OnUpdate(delta);
        _updateProcesser.OnUpdate(delta);
    }

    public void ActorLateUpdate(float delta)
    {
        if (!inited) return;
        OnLateUpdate(delta);
        _updateProcesser.OnLateUpdate(delta);
    }

    #region Private

    public void InitActor(World world)
    {
        if (inited)
        {
            return;
        }

        InitGuid();

        CurrentWorld = world;
        _updateProcesser ??= new UpdateProcesser();

        OnInit();

        GetComponents();

        foreach (var cpt in _components)
        {
            _updateProcesser.AddUpdater(cpt);
            cpt.InitBeginComponent_CallByActor(this);
        }

        foreach (var cpt in _components)
        {
            cpt.InitComponent_CallByActor();
        }

        // 所有组件的初始化完成后 依次调用初始化完成
        foreach (var cpt in _components)
        {
            cpt.InitEndComponent_CallByActor();
        }

        inited = true;
        Available = true;
        LateInit();
    }

    protected virtual void GetComponents()
    {
        var tempList = StaticSimplePool<List<ActorMonoCmpt>>.Pool.Get();
        GetComponents(tempList);
        foreach (var cmpt in tempList)
        {
            _components.Add(cmpt);
        }

        tempList.Clear();
        StaticSimplePool<List<ActorMonoCmpt>>.Pool.Release(tempList);
    }

    public void PreReleaseActor()
    {
        if (!Available) return;

        Available = false;

        OnPreRelease();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    protected virtual void OnInit()
    {
    }

    /// <summary>
    /// 晚初始化
    /// </summary>
    protected virtual void LateInit()
    {
    }

    /// <summary>
    /// 释放 - 即将从World移除的时候会调用
    /// </summary>
    protected virtual void OnPreRelease()
    {
    }

    /// <summary>
    /// 释放 - 真正从World移除的时候会调用
    /// </summary>
    protected virtual void OnRelease()
    {
        Destroy(gameObject);
    }

    #endregion

    #region Update

    bool IReleaseUpdate.ReleaseTag
    {
        get => !inited || !Available;
        set
        {
            if (value)
            {
                // inited = false;
                // Runing = false;
                PreReleaseActor();
            }
        }
    }

    void IReleaseUpdate.OnUpdate(float delta)
    {
        ActorUpdate(delta);
    }

    void IReleaseUpdate.OnLateUpdate(float delta)
    {
        ActorLateUpdate(delta);
    }

    void IReleaseUpdate.OnReleaseUpdater()
    {
        if (!inited) return;
        inited = false;

        for (int i = _components.Count - 1; i > -1; i--)
        {
            _components[i].ReleaseBeginComponent_CallByActor();
        }

        // 所有组件以初始化相反的顺序 依次调用卸载
        for (int i = _components.Count - 1; i > -1; i--)
        {
            _components[i].ReleaseComponent_CallByActor();
        }

        for (int i = _components.Count - 1; i > -1; i--)
        {
            _components[i].ReleaseEndComponent_CallByActor();
        }

        _updateProcesser.Clear();
        _components.Clear();

        OnRelease();
    }

    private UpdateProcesser _updateProcesser = new();

    protected virtual void OnUpdate(float delta)
    {
    }

    protected virtual void OnLateUpdate(float delta)
    {
    }

    #endregion
}