using System;

namespace BizzaCommon
{
    /// <summary>
    /// <para>通用对象池。该池实现了对任意引用类型的对象池支持，并对实现了IPoolObject接口的类型提供了额外的支持。</para>
    /// <para>注1：对于List/Dictionary/HashSet等集合类型，请使用ListPool/DictPool/HashSetPool等对应的对象池类。</para>
    /// <para>注2：对于GameObject类型，请使用GameObjectPool。</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class GenericObjectPool<T>
        where T : class, new()
    {
        #region 内部字段
        private static readonly ObjectPoolBase<T> s_SingletonPool;
        #endregion

        #region 构造方法
        static GenericObjectPool()
        {
            var pool = GlobalObjectPoolRegistry.Get(typeof(T));

            if (pool != null)
            {
                s_SingletonPool = (ObjectPoolBase<T>)pool;
            }
            else
            {
                s_SingletonPool = new ObjectPoolBase<T>(o => (o as IPoolObject)?.GetFromPool(), o => (o as IPoolObject)?.ReleaseToPool());
                GlobalObjectPoolRegistry.Register(typeof(T), s_SingletonPool);
            }
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 从对象池中获取实例
        /// </summary>
        /// <returns></returns>
        public static T Get() => s_SingletonPool.Get();

        /// <summary>
        /// <para>【请一定结合using语句使用本方法】</para>
        /// <para>从池中获取实例。如果池中无实例，则池会创建新的实例。</para>
        /// <para>这个重载将所请求的对象实例放在obj形参中，并返回一个与该实例关联的句柄。</para>
        /// <para>当句柄被卸载，即触发了Dispose()方法时，实例将被自动回收至对象池。</para>
        /// <para>使用这个重载获取的obj实例无需手动释放回池中。</para>
        /// </summary>
        /// <returns></returns>
        public static ObjectPoolBase<T>.PoolObjectHandle Get(out T obj) => s_SingletonPool.Get(out obj);

        /// <summary>
        /// 释放给定实例到对象池
        /// /// </summary>
        /// <param name="toRelease"></param>
        public static void Release(T toRelease) => s_SingletonPool.Release(toRelease);

        /// <summary>
        /// <para>释放给定实例到对象池（安全版）。</para>
        /// <para>这个版本Release方法会先将传入的字段置空，之后再释放对象。
        /// /// </summary>
        /// <param name="toRelease"></param>
        public static void SafeRelease(ref T toRelease)
        {
            if (toRelease != null)
            {
                var r = toRelease;
                toRelease = null;
                s_SingletonPool.Release(r);
            }
        }

        /// <summary>
        /// <para>设置创建实例的回调</para>
        /// <para>当类型的实例不能通过Activator.CreateInstane()直接创建时，需要使用这个方法来指定创建实例的回调</para>
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static void SetInstanceCallbacks(Func<T> createInstanceCallback, Action<T> destroyInstanceCallback)
            => s_SingletonPool.SetInstanceCallbacks(createInstanceCallback, destroyInstanceCallback);
       

        /// <summary>
        /// <para>设置对象池回调</para>
        /// <para>当类型T没有实现IPoolObject时，会需要使用这个方法来指定放入池和取出池的回调</para>
        /// </summary>
        /// <param name="actionOnGet">获取实例时的回调</param>
        /// <param name="actionOnRelease">释放实例时的回调</param>
        public static void SetPoolCallbacks(Action<T> actionOnGet, Action<T> actionOnRelease)
            => s_SingletonPool.SetPoolCallbacks(actionOnGet, actionOnRelease);
        #endregion
    }
   
}