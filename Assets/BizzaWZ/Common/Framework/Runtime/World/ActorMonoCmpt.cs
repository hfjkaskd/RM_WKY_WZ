using Sirenix.OdinInspector;
using UnityEngine;

public interface IBizzaComponent<in TOwner>
{
    void InitBeginComponent_CallByActor(TOwner owner);

    void InitComponent_CallByActor();

    void InitEndComponent_CallByActor();

    void ReleaseBeginComponent_CallByActor();
    void ReleaseComponent_CallByActor();
    void ReleaseEndComponent_CallByActor();
}

// [RequireComponent(typeof(Actor))]
public abstract class ActorMonoCmpt : MonoBehaviour, IBizzaComponent<Actor>
{
    public bool Enabled => _running && enabled && _inited;
    private bool _inited = false;
    private bool _running = false;

    public bool Running
    {
        get { return _running; }
        set { _running = value; }
    }

    /// <summary>
    /// ��ʼ����
    /// </summary>
    void IBizzaComponent<Actor>.InitBeginComponent_CallByActor(Actor actor)
    {
        _inited = true;
        _running = true;
        OnInitBegin(actor);
    }

    void IBizzaComponent<Actor>.InitComponent_CallByActor()
    {
        OnInit();
    }

    /// <summary>
    /// ��ʼ�����
    /// </summary>
    void IBizzaComponent<Actor>.InitEndComponent_CallByActor()
    {
        OnInitEnd();
    }

    public void ReleaseBeginComponent_CallByActor()
    {
        OnReleaseBegin();
    }

    void IBizzaComponent<Actor>.ReleaseComponent_CallByActor()
    {
        OnRelease();
        _running = false;
        _inited = false;
    }

    public void ReleaseEndComponent_CallByActor()
    {
        OnReleaseEnd();
    }

    protected virtual void OnInitBegin(Actor actor)
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

public abstract class ActorMonoCmpt<T> : ActorMonoCmpt where T : Actor
{
    private T _owner;

    [ShowInInspector]
    public T Owner
    {
        get { return _owner; }
    }

    protected sealed override void OnInitBegin(Actor actor)
    {
        _owner = actor as T;
    }
}