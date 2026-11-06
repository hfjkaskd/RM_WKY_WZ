using System;
using System.Collections.Generic;
using UnityEngine;

public class PrefabPoolModule : BaseGameModule<PrefabPoolModule>
{
    private const int GameObjectPrefabPoolMaxNum = 1024;
    private readonly Dictionary<object, PrefabPool> _pools = new(); //预制体
    private readonly Dictionary<string, Action> _onPrefabLoaded = new(); //预制体加载事件，用于异步实例化
    private readonly Dictionary<GameObject, PrefabPool> _objLinkPool = new();

#if UNITY_EDITOR && NEVER
    [ShowInInspector, ReadOnly]
    public List<int> InstanceIDPools => _instanceIDPools.Keys.ToList();
#endif

    private int _nextPoolId;
    private int NextPoolId => _nextPoolId++;

    public void OnLoaded(string address, Action onLoad)
    {
        if (_pools.ContainsKey(address))
        {
            onLoad();
            return;
        }

        if (_onPrefabLoaded.TryGetValue(address, out var action) && action != null)
        {
            _onPrefabLoaded[address] = action + onLoad;
            return;
        }

        _onPrefabLoaded[address] = onLoad;
    }

    /// <summary>
    /// 加载预制体并且创建对应的GameObject对象池
    /// 创建的对象池Key为address
    /// </summary>
    /// <param name="address">预制体地址</param>
    /// <param name="onLoad">加载完成回调</param>
    public void LoadPrefab(string address, Action onLoad = null)
    {
        if (_pools.ContainsKey(address))
        {
            onLoad?.Invoke();
            return;
        }

        if (_onPrefabLoaded.TryGetValue(address, out var action) && action != null)
        {
            _onPrefabLoaded[address] = action + onLoad;
            return;
        }

        _onPrefabLoaded[address] = onLoad;
        AssetUtils.LoadAsset<GameObject>(address, OnPrefabLoaded);
        return;

        void OnPrefabLoaded(string address, GameObject prefab)
        {
            if (_onPrefabLoaded.TryGetValue(address, out var action))
            {
                var cmpt = prefab;
                CreatePool(address, cmpt);
                action?.Invoke();
                _onPrefabLoaded.Remove(address);
            }
        }
    }

    /// <summary>
    /// 加载预制体并且创建对应的PrefabType对象池
    /// 创建的对象池Key为address
    /// </summary>
    /// <param name="address">预制体地址</param>
    /// <param name="onLoad">加载完成回调</param>
    /// <typeparam name="PrefabType">预制体类型</typeparam>
    public void LoadPrefab<PrefabType>(string address, Action onLoad = null) where PrefabType : Component
    {
        if (_pools.ContainsKey(address))
        {
            onLoad?.Invoke();
            return;
        }

        if (_onPrefabLoaded.TryGetValue(address, out var action) && action != null)
        {
            _onPrefabLoaded[address] = action + onLoad;
            return;
        }

        _onPrefabLoaded[address] = onLoad;
        AssetUtils.LoadAsset<GameObject>(address, OnPrefabLoaded);
        return;

        void OnPrefabLoaded(string address, GameObject prefab)
        {
            if (_onPrefabLoaded.TryGetValue(address, out var action))
            {
                var cmpt = prefab.GetComponent<PrefabType>();
                CreatePool(address, cmpt);
                action?.Invoke();
                _onPrefabLoaded.Remove(address);
            }
        }
    }

    /// <summary>
    /// 通过预制体创建GameObject对象池
    /// </summary>
    /// <param name="key">对象池Key</param>
    /// <param name="prefab">预制体</param>
    /// <returns></returns>
    public PrefabPool CreatePool(object key, GameObject prefab)
    {
        if (_pools.TryGetValue(key, out var pool)) return pool;
        var go = new GameObject($"pool_{NextPoolId:D5}_{prefab.name}");
        go.transform.SetParent(transform);
        PrefabPool prefabPool = new PrefabObjPool(prefab, go.transform, GameObjectPrefabPoolMaxNum);
        _pools[key] = prefabPool;
        return prefabPool;
    }

    /// <summary>
    /// 通过预制体创建T对象池
    /// </summary>
    /// <param name="key">对象池Key</param>
    /// <param name="prefab">预制体</param>
    /// <typeparam name="T">预制体类型</typeparam>
    /// <returns></returns>
    public PrefabPool<T> CreatePool<T>(object key, T prefab) where T : Component
    {
        if (_pools.TryGetValue(key, out var pool)) return pool as PrefabPool<T>;
        var go = new GameObject($"pool_{NextPoolId:D5}_{prefab.name}");
        go.transform.SetParent(transform);
        PrefabPool<T> prefabPool = new(prefab, go.transform, GameObjectPrefabPoolMaxNum);
        _pools[key] = prefabPool;
        return prefabPool;
    }

    /// <summary>
    /// 通过Func<T>创建T对象池
    /// </summary>
    /// <param name="key">对象池Key</param>
    /// <param name="func">创建函数</param>
    /// <typeparam name="T">预制体类型</typeparam>
    /// <returns></returns>
    public PrefabFuncPool<T> CreatePool<T>(object key, Func<T> func) where T : Component
    {
        if (_pools.TryGetValue(key, out var pool)) return pool as PrefabFuncPool<T>;
        var go = new GameObject($"pool_{NextPoolId:D5}_{key}");
        go.transform.SetParent(transform);
        PrefabFuncPool<T> prefabPool = new(func, go.transform, GameObjectPrefabPoolMaxNum);
        _pools[key] = prefabPool;
        return prefabPool;
    }

    public bool RegisterPool(object key, PrefabPool prefab)
    {
        return _pools.TryAdd(key, prefab);
    }

    public PrefabPool GetPool(object key)
    {
        _pools.TryGetValue(key, out var pool);
        return pool;
    }

    public PrefabPool<T> GetPool<T>(object key) where T : Component
    {
        _pools.TryGetValue(key, out var pool);
        return pool as PrefabPool<T>;
    }

    public PrefabPool GetPoolByInstanceID(GameObject obj)
    {
        _objLinkPool.TryGetValue(obj, out var pool);
        return pool;
    }

    /// <summary>
    /// 移除对象池
    /// </summary>
    /// <param name="key">对象池Key</param>
    /// <param name="destroyParent">是否销毁对象池节点</param>
    public void RemovePool(object key, bool destroyParent = false)
    {
        if (_pools.TryGetValue(key, out var pool))
        {
            List<GameObject> temp = new List<GameObject>();
            foreach (var pair in _objLinkPool)
            {
                if (pair.Value == pool)
                {
                    temp.Add(pair.Key);
                }
            }

            foreach (var obj in temp)
            {
                _objLinkPool.Remove(obj);
            }

            pool.Clear();
            _pools.Remove(key);
            if (destroyParent)
            {
                Destroy(pool.Parent);
            }
        }
    }

    public void RemoveAllPool<T>() where T : Component
    {
        List<object> temp = new();
        foreach (var pair in _pools)
        {
            if (pair.Value is PrefabPool<T>)
            {
                temp.Add(pair.Key);
            }
        }

        foreach (var key in temp)
        {
            RemovePool(key);
        }
    }

    /// <summary>
    /// 检查是否存在指定Key的对象池
    /// </summary>
    /// <param name="key">对象池key</param>
    /// <returns></returns>
    public bool Contains(object key)
    {
        return _pools.ContainsKey(key);
    }

    public void SetIdPool(PrefabPool pool, GameObject obj)
    {
        _objLinkPool[obj] = pool;
    }

    public void SetIdPool<T>(PrefabPool pool, T cpt) where T : Component
    {
        _objLinkPool[cpt.gameObject] = pool;
    }

    /// <summary>
    /// 获取1个指定key的对象池的GameObject对象实例
    /// 没有对应对象池会返回空
    /// 对象池没有缓存会创建一个
    /// 注册实例ID可以通过实例ID释放对象
    /// 注册实例ID后必须要释放，不能直接销毁
    /// </summary>
    /// <param name="key">对象池key</param>
    /// <param name="registerInstanceID">是否注册实例ID</param>
    /// <returns></returns>
    public GameObject GetGameObj(object key, bool registerInstanceID = false)
    {
        if (_pools.TryGetValue(key, out var prefabPool))
        {
            var item = prefabPool.GetGameObject();
            
            if (registerInstanceID)
            {
                _objLinkPool[item] = prefabPool;
            }

            return item;
        }

        return null;
    }

    /// <summary>
    /// 获取1个指定key的对象池的T对象实例
    /// 没有对应对象池会返回空
    /// 对象池没有缓存会创建一个
    /// 注册实例ID可以通过实例ID释放对象
    /// 注册实例ID后必须要释放，不能直接销毁
    /// </summary>
    /// <param name="key">对象池key</param>
    /// <param name="registerInstanceID">是否注册实例ID</param>
    /// <typeparam name="T">实例类型</typeparam>
    /// <returns></returns>
    public T Get<T>(object key, bool registerInstanceID = false) where T : Component
    {
        if (_pools.TryGetValue(key, out var pool)
            && pool is PrefabPool<T> prefabPool)
        {
            T item = prefabPool.Get();

            var obj = item.gameObject;
            if (registerInstanceID)
            {
                _objLinkPool[obj] = prefabPool;
            }

            return item;
        }

        return null;
    }

    public T GetByPrefab<T>(T prefab, bool registerInstanceID = false) where T : Component
    {
        PrefabPool<T> prefabPool = CreatePool(prefab, prefab);
        if (prefabPool == null)
        {
            LogLogger.LogAssert(false, "Has Existed Same Key Pool, But Type not match!!!");
            return null;
        }

        T item = null;
        while (item == null) 
        {
            item = prefabPool.Get();
        }
        
        var obj = item.gameObject;
        if (registerInstanceID)
        {
            _objLinkPool[obj] = prefabPool;
        }

        obj.SetActive(false);
        return item;
    }
    
    public GameObject GetByPrefab(GameObject prefab, bool registerInstanceID = false)
    {
        PrefabPool prefabPool = CreatePool(prefab, prefab);
        if (prefabPool == null)
        {
            LogLogger.LogAssert(false, "Has Existed Same Key Pool, But Type not match!!!");
            return null;
        }

        GameObject ret = prefabPool.GetGameObject();
        
        if (registerInstanceID)
        {
            _objLinkPool[ret] = prefabPool;
        }

        return ret;
    }

    public T GetByPrefabOld<T>(T prefab) where T : Component
    {
        return GetByPrefab(prefab, true);
    }

    /// <summary>
    /// 释放一个GameObject实例
    /// 没有对应对象池会销毁实例
    /// </summary>
    /// <param name="key">对象池key</param>
    /// <param name="obj">实例</param>
    public bool Release(object key, GameObject obj)
    {
        if (obj == null) return false;

        _objLinkPool.Remove(obj);

        if (_pools.TryGetValue(key, out var pool))
        {
            pool.ReleaseObj(obj);
            return true;
        }

        {
            Destroy(obj);
            return false;
        }
    }

    /// <summary>
    /// 释放一个T实例
    /// </summary>
    /// <param name="key">对象池key</param>
    /// <param name="cmpt">实例</param>
    /// <typeparam name="T">实例类型</typeparam>
    public bool Release<T>(object key, T cmpt) where T : Component
    {
        if (cmpt == null) return false;

        var obj = cmpt.gameObject;
        
        _objLinkPool.Remove(obj);

        if (_pools.TryGetValue(key, out var pool)
            && pool is PrefabPool<T> prefabPool)
        {
            prefabPool.Release(cmpt);
            return true;
        }

        {
            Destroy(obj);
            return false;
        }
    }

    /// <summary>
    /// 通过实例ID释放T实例
    /// 必须在获取时注册实例ID
    /// </summary>
    /// <param name="cmpt">实例</param>
    /// <param name="destroy">如果没有对应pool是否销毁</param>
    /// <typeparam name="T">实例类型</typeparam>
    public bool ReleaseByInstanceID<T>(T cmpt, bool destroy = false) where T : Component
    {
        if (cmpt == null) return false;

        var obj = cmpt.gameObject;

        if (_objLinkPool.Remove(obj, out var pool) &&
            pool is PrefabPool<T> prefabPool)
        {
            prefabPool.Release(cmpt);
            return true;
        }

        if (destroy)
        {
            Destroy(obj);
        }
        else
        {
            LogLogger.LogVerbose(LogTag.LOG_Pool,$"目标对象不在对象池:{obj.name}");
        }

        return false;
    }

    /// <summary>
    /// 通过实例ID释放GameObject实例
    /// 必须在获取时注册实例ID
    /// </summary>
    /// <param name="obj">实例</param>
    /// <param name="destroy">如果没有对应pool是否销毁</param>
    public bool ReleaseByInstanceID(GameObject obj, bool destroy = false)
    {
        if (obj == null) return false;

        if (_objLinkPool.Remove(obj, out var pool))
        {
            pool.ReleaseObj(obj);
            return true;
        }

        if (destroy)
        {
            Destroy(obj);
        }
        else
        {
            LogLogger.LogVerbose(LogTag.LOG_Pool,$"目标对象不在对象池:{obj.name}");
        }

        return false;
    }

    public void RemoveInstanceID(GameObject obj)
    {
        if (obj == null) return;
        _objLinkPool.Remove(obj);
    }

    public void RemoveInstanceID(Component cmpt)
    {
        if (cmpt == null) return;
        _objLinkPool.Remove(cmpt.gameObject);
    }

    public override void InitGameModule()
    {
    }

    public override void ReleaseGameModule()
    {
        foreach (var pair in _pools)
        {
            pair.Value.Clear();
        }

        _pools.Clear();

        _objLinkPool.Clear();
    }
}
