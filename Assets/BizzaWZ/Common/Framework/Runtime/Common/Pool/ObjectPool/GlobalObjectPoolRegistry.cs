using System;
using System.Collections.Generic;

    /// <summary>
    /// <para>全局对象池注册类</para>
    /// <para>注：该静态类目前为internal类，外部请勿使用</para>
    /// </summary>
    internal static class GlobalObjectPoolRegistry
    {
        #region 内部字段

        /// <summary>
        /// 内部字段：线程锁
        /// </summary>
        private static readonly object s_LockHelper = new object();

        /// <summary>
        /// 内部字段：所有注册的对象池实例
        /// </summary>
        private static Dictionary<Type, IObjectPoolBase> s_AllObjectPools = new Dictionary<Type, IObjectPoolBase>();

        #endregion

        #region 公开方法

        /// <summary>
        /// 获取或创建给定类型的对象池实例。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static void Register(Type t, IObjectPoolBase pool)
        {
            lock (s_LockHelper)
            {
                if (s_AllObjectPools.ContainsKey(t))
                {
                    LogLogger.LogVerbose(LogTag.LOG_Pool,$"{nameof(Register)}: 禁止重复注册对象池实例。t = {t}");
                    return;
                }

                s_AllObjectPools.Add(t, pool);
            }
        }

        /// <summary>
        /// 获取或创建给定类型的对象池实例。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static IObjectPoolBase Get(Type t)
        {
            lock (s_LockHelper)
            {
                if (!s_AllObjectPools.TryGetValue(t, out var pool))
                {
                    return pool;
                }
            }

            return null;
        }

        /// <summary>
        /// 清空每个对象池。
        /// </summary>
        public static void Clear()
        {
            var allPools = new List<IObjectPoolBase>(s_AllObjectPools.Count);

            lock (s_LockHelper)
            {
                allPools.AddRange(s_AllObjectPools.Values);
            }

            foreach (var pool in allPools)
            {
                pool.Clear();
            }
        }

        #endregion
    }
