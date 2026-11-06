using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PrefabPool
{
    protected Transform _parent;
    public Transform Parent => _parent;

    public abstract Type Type { get; }

    public abstract int Size { get; }
    
    protected int _maxSize;
    public virtual int MaxSize
    {
        get => _maxSize;
        set => _maxSize = value;
    }
    
    protected bool _checkExist = true;
    public virtual bool CheckExist
    {
        get => _checkExist;
        set => _checkExist = value;
    }
    
    public bool AutoSetParent = true;
    public bool AutoDisableActive = true;

    public abstract GameObject GetGameObject();

    public abstract void ReleaseObj(GameObject obj);

    public abstract void Clear();
}

public abstract class PrefabObjPoolBase<TPrefab> : PrefabPool where TPrefab : UnityEngine.Object
{
    protected Queue<TPrefab> _pool;
    protected readonly HashSet<TPrefab> _set = new();
    protected bool _inited;

    protected PrefabObjPoolBase()
    {
    }

    protected void Init(Transform parent, int maxSize = 10)
    {
        _pool = new(maxSize);
        _parent = parent;
        _maxSize = maxSize;
        _inited = true;
    }

    public override Type Type => typeof(TPrefab);

    public override int Size => _pool.Count;

    public override int MaxSize
    {
        set
        {
            if (_maxSize == value) return;
            _maxSize = value;
            while (_pool.Count > value)
            {
                var item = _pool.Dequeue();
                _set.Remove(item);
                DestroyItem(item);
            }
        }
    }
    
    public override bool CheckExist
    {
        set
        {
            if (_checkExist == value) return;
            _checkExist = value;
            if (!value) _set.Clear();
        }
    }

    protected abstract TPrefab Create();
    protected abstract void DestroyItem(TPrefab prefab);
    protected abstract void ReleaseItem(TPrefab prefab);

    public virtual TPrefab Get()
    {
        TPrefab ret;
        if (_pool.Count > 0)
        {
            ret = _pool.Dequeue();
            if (CheckExist)
            {
                _set.Remove(ret);
            }
        }
        else
        {
            ret = Create();
        }

        return ret;
    }

    public void Release(TPrefab item)
    {
        if (item == null) return;

        if (item is IOnRelease releasable)
        {
            releasable.OnRelease();
        }

        if (_inited && _pool.Count < _maxSize)
        {
            if (CheckExist && _set.Contains(item))
            {
                LogLogger.LogVerbose(LogTag.LOG_Pool,"item has existed in pool");
                return;
            }

            ReleaseItem(item);
            _pool.Enqueue(item);
            _set.Add(item);
        }
        else
        {
            DestroyItem(item);
        }
    }

    public void Release(List<TPrefab> list)
    {
        foreach (var item in list)
        {
            Release(item);
        }
        list.Clear();
    }

    public override void Clear()
    {
        MaxSize = 0;
        _pool.Clear();
        _set.Clear();
        _parent = null;
        _inited = false;
    }
}

public abstract class PrefabPoolBase<TPrefab> : PrefabObjPoolBase<TPrefab> where TPrefab : Component
{
    protected override void ReleaseItem(TPrefab prefab)
    {
        if (AutoSetParent)
        {
            prefab.transform.SetParent(_parent, false);
        }

        if (AutoDisableActive)
        {
            prefab.gameObject.SetActive(false);
        }
    }

    protected override void DestroyItem(TPrefab prefab)
    {
        UnityEngine.Object.Destroy(prefab.gameObject);
    }

    public override GameObject GetGameObject()
    {
        return Get().gameObject;
    }

    public override void ReleaseObj(GameObject obj)
    {
        if (obj == null) return;
        if (obj.TryGetComponent(out TPrefab prefab))
        {
            Release(prefab);
        }
        else
        {
            UnityEngine.Object.Destroy(obj);
        }
    }
}

public class PrefabObjPool : PrefabObjPoolBase<GameObject>
{
    protected GameObject _prefab;

    public PrefabObjPool(GameObject prefab, Transform parent, int maxSize = 10)
    {
        Init(prefab, parent, maxSize);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="parent">对象池父节点</param>
    /// <param name="maxSize">最大容量</param>
    public void Init(GameObject prefab, Transform parent, int maxSize = 10)
    {
        _prefab = prefab;
        _pool = new(maxSize);
        _parent = parent;
        _maxSize = maxSize;
        _inited = true;
    }

    protected override GameObject Create()
    {
        return UnityEngine.Object.Instantiate(_prefab, _parent);
    }

    protected override void DestroyItem(GameObject prefab)
    {
        UnityEngine.Object.Destroy(prefab);
    }

    protected override void ReleaseItem(GameObject prefab)
    {
        if (_parent != null)
        {
            prefab.transform.SetParent(_parent, false);
        }

        if (AutoDisableActive)
        {
            prefab.gameObject.SetActive(false);
        }
    }

    public override GameObject GetGameObject()
    {
        return Get();
    }

    public override void ReleaseObj(GameObject obj)
    {
        Release(obj);
    }

    public override void Clear()
    {
        _prefab = null;
        base.Clear();
    }
}

public class PrefabPool<TPrefab> : PrefabPoolBase<TPrefab> where TPrefab : Component
{
    protected TPrefab _prefab;

    public PrefabPool(TPrefab prefab, Transform parent, int maxSize = 10)
    {
        Init(prefab, parent, maxSize);
    }

    public void Init(TPrefab prefab, Transform parent, int maxSize = 10)
    {
        _prefab = prefab;
        _pool = new(maxSize);
        _parent = parent;
        _maxSize = maxSize;
        _inited = true;
    }

    protected override TPrefab Create()
    {
        return UnityEngine.Object.Instantiate(_prefab, _parent);
    }

    public override void Clear()
    {
        _prefab = null;
        base.Clear();
    }
}

public class PrefabFuncPool<TPrefab> : PrefabPoolBase<TPrefab> where TPrefab : Component
{
    protected Func<TPrefab> _funcCreate;

    public PrefabFuncPool(Func<TPrefab> func, Transform parent, int maxSize = 10)
    {
        Init(func, parent, maxSize);
    }

    public void Init(Func<TPrefab> func, Transform parent, int maxSize = 10)
    {
        _funcCreate = func;
        _pool = new(maxSize);
        _parent = parent;
        _maxSize = maxSize;
        _inited = true;
    }

    protected override TPrefab Create()
    {
        return _funcCreate();
    }

    public override void Clear()
    {
        _funcCreate = null;
        base.Clear();
    }
}

public interface IOnRelease
{
    void OnRelease();
}

public class PrefabPoolManager<PrefabType> : MonoBehaviour where PrefabType : MonoBehaviour, IOnRelease
{
    public PrefabPool<PrefabType> _prefabPool;
    public PrefabType _prefab = null;

    public PrefabType Get()
    {
        return _prefabPool.Get();
    }

    public void Release(PrefabType prefab)
    {
        _prefabPool.Release(prefab);
    }

    private void Awake()
    {
        _prefabPool = new(_prefab, transform);
    }
}