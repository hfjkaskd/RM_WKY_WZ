using System.Collections.Generic;
using Bizza;
using UnityEngine;

public static class PoolUtil
{
    public static GameObject GetGameObject(GameObject prefab)
    {
        return PrefabPoolModule.Instance.GetByPrefab(prefab, true);
    }

    public static GameObject GetGameObject<T>(T prefab) where T : Component
    {
        return PrefabPoolModule.Instance.GetByPrefab(prefab, true).gameObject;
    }

    public static void ReleaseGameObject(GameObject obj, bool destroyIfNotInPool = false)
    {
        PrefabPoolModule.Instance.ReleaseByInstanceID(obj, destroyIfNotInPool);
    }

    public static T GetComponent<T>(T prefab) where T : Component
    {
        return PrefabPoolModule.Instance.GetByPrefab(prefab, true);
    }

    public static void ReleaseComponent<T>(T cmpt, bool destroyIfNotInPool = false) where T : Component
    {
        PrefabPoolModule.Instance.ReleaseByInstanceID(cmpt, destroyIfNotInPool);
    }

    public static T GetClass<T>() where T : class, new()
    {
        var ret = StaticSimplePool<T>.Pool.Get();
        LogLogger.LogAssert(ret != null, $"GetClass Error:{typeof(T).Name}");
        return ret;
    }

    public static void ReleaseClass<T>(T item) where T : class
    {
        if (StaticSimplePool.pools.TryGetValue(item.GetType(), out SimplePool pool))
        {
            pool.Release(item);
        }
    }

    // public static void ReleaseClass<T>(T item) where T : class, new()
    // {
    //     StaticSimplePool<T>.Pool.Release(item);
    // }
}