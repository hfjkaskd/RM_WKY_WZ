using System;

    /// <summary>
    /// 对象池工具类
    /// </summary>
    public static class ObjectPoolUtility
    {
        #region 常量

        public const string ObjectPoolLogTag = "ObjectPool";

        #endregion

        #region 公开方法

        //todo:补全
        public static T GetFromPool<T>() where T : new()
        {
            var pool = GlobalObjectPoolRegistry.Get(typeof(T));

            if (pool == null)
            {
                return new T();
            }

            var t = (T)pool.Get();
            (t as IPoolObject)?.GetFromPool();
            return t;
        }

        /// <summary>
        /// <para>返回任意类型的给定对象到对象池。</para>
        /// <para>注：ObjectPoolUtility特意没有提供对应的GetFromPool()方法，因为任何对象池类型，应以强类型的方式被创建，否则其将被Strip掉</para>
        /// </summary>
        /// <param name="instance"></param>
        /// <returns>true:存在该对象的类型对应的对象池，且对象已释放到对象池; false:不存在该对象的类型对应的对象池</returns>
        public static bool ReleaseToPool(object instance)
        {
            if (instance == null)
            {
                LogLogger.LogVerbose(LogTag.LOG_Pool,$"{ObjectPoolLogTag} | {nameof(ReleaseToPool)}: 要释放的实例为null。操作已被忽略。");
                return false;
            }

            var pool = GlobalObjectPoolRegistry.Get(instance.GetType());

            if (pool == null)
            {
                return false;
            }

            (instance as IPoolObject)?.ReleaseToPool();
            pool.Release(instance);

            return true;
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public static void ClearAllPools()
        {
            GlobalObjectPoolRegistry.Clear();
        }

        #endregion
    }
