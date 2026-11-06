using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BizzaCmpt<TOwner> : IBizzaComponent<TOwner>
{
    private bool _enabled = false;
    private bool _inited = false;
    private bool _running = false;

    public bool Enabled
    {
        get { return _running && _enabled && _inited; }
        set { _enabled = value; }
    }
    
    public bool Running
    {
        get { return _running; }
        set { _running = value; }
    }

    /// <summary>
    /// ��ʼ����
    /// </summary>
    public void InitBeginComponent_CallByActor(TOwner actor)
    {
        _inited = true;
        _running = true;
        _enabled = true;
        OnInitBegin(actor);
    }

    public void InitComponent_CallByActor()
    {
        OnInit();
    }

    /// <summary>
    /// ��ʼ�����
    /// </summary>
    public void InitEndComponent_CallByActor()
    {
        OnInitEnd();
    }

    public void ReleaseBeginComponent_CallByActor()
    {
        OnReleaseBegin();
    }

    public void ReleaseComponent_CallByActor()
    {
        OnRelease();
        _running = false;
        _inited = false;
    }

    public void ReleaseEndComponent_CallByActor()
    {
        OnReleaseEnd();
    }

    protected virtual void OnInitBegin(TOwner actor)
    {
    }

    protected virtual void OnInit()
    {
    }

    protected virtual void OnInitEnd()
    {
    }


    protected virtual void OnReleaseBegin()
    {
    }

    protected virtual void OnRelease()
    {
    }


    protected virtual void OnReleaseEnd()
    {
    }
}

public abstract class ActorCmpt<TActor> : BizzaCmpt<Actor> where TActor : Actor
{
    private TActor _owner;

    public TActor Owner
    {
        get { return _owner; }
    }

    protected override void OnInitBegin(Actor actor)
    {
        _owner = actor as TActor;
    }
}
