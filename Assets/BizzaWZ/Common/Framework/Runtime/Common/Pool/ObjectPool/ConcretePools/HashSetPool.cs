using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BizzaCommon
{
    /// <summary>
    /// HashSet对象池。这个静态类内部封装了HashSet类型的对象池单例。
    /// </summary>
    public static class HashSetPool<T>
    {
        #region 内部字段
        /// <summary>
        /// 内部字段：内部对象池（单例)
        /// </summary>
        private static readonly ObjectPoolBase<HashSet<T>> s_SingletonPool;
        #endregion

        #region 构造方法
        static HashSetPool()
        {
            s_SingletonPool = new ObjectPoolBase<HashSet<T>>(null, o => o.Clear());
            GlobalObjectPoolRegistry.Register(typeof(ObjectPoolBase<HashSet<T>>), s_SingletonPool);
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 从对象池中获取实例
        /// </summary>
        /// <returns></returns>
        public static HashSet<T> Get() => s_SingletonPool.Get();

        /// <summary>
        /// <para>【请一定结合using语句使用本方法】</para>
        /// <para>从池中获取实例。如果池中无实例，则池会创建新的实例。</para>
        /// <para>这个重载将所请求的对象实例放在obj形参中，并返回一个与该实例关联的句柄。</para>
        /// <para>当句柄被卸载，即触发了Dispose()方法时，实例将被自动回收至对象池。</para>
        /// <para>使用这个重载获取的obj实例无需手动释放回池中。</para>
        /// </summary>
        /// <returns></returns>
        public static ObjectPoolBase<HashSet<T>>.PoolObjectHandle Get(out HashSet<T> obj) => s_SingletonPool.Get(out obj);

        /// <summary>
        /// 释放给定实例到对象池
        /// /// </summary>
        /// <param name="toRelease"></param>
        public static void Release(HashSet<T> toRelease) => s_SingletonPool.Release(toRelease);

        /// <summary>
        /// <para>释放给定实例到对象池（安全版）。</para>
        /// <para>这个版本Release方法会先将传入的字段置空，之后再释放对象。
        /// /// </summary>
        /// <param name="toRelease"></param>
        public static void SafeRelease(ref HashSet<T> toRelease)
        {
            if (toRelease != null)
            {
                var r = toRelease;
                toRelease = null;
                s_SingletonPool.Release(r);
            }
        }
        #endregion
    }
}