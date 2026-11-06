using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

    /// <summary>
    /// 接口：对象池基类接口。任何对象池需要实现该接口，才能被ObjectPool支持。
    /// </summary>
    public interface IObjectPoolBase
    {
        /// <summary>
        /// 从池中取出实例
        /// </summary>
        /// <returns></returns>
        object Get();

        /// <summary>
        /// 返回实例到池中
        /// </summary>
        /// <param name="instance"></param>
        void Release(object instance);

        /// <summary>
        /// 清空池
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 对象池基类。这个类实现了一个对象池所需的所有核心功能。每个具体的对象池类型一般情况下只需继承该类即可。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPoolBase<T> : IObjectPoolBase
        where T : class, new()
    {
        #region 内部类型

        public struct PoolObjectHandle : IDisposable
        {
            private readonly ObjectPoolBase<T> pool;
            public readonly T obj;

            public PoolObjectHandle(ObjectPoolBase<T> pool, T obj)
            {
                this.pool = pool;
                this.obj = obj;
            }

            public void Dispose()
            {
                pool.Release(obj);
            }
        }

        #endregion

        #region 公开字段

        /// <summary>
        /// 本对象池创建过的实例总数
        /// </summary>
        public int countAll => m_CountAll;

        private int m_CountAll;

        /// <summary>
        /// 本对象池当前缓存的实例总数
        /// </summary>
        public int countInPool => m_Stack.Count;

        #endregion

        #region 内部字段

        /// <summary>
        /// 线程锁
        /// </summary>
        private readonly object m_LockHelper = new object();

        /// <summary>
        /// 内部对象池，栈
        /// </summary>
        private readonly Stack<T> m_Stack = new Stack<T>();

        /// <summary>
        /// 内部对象池，哈希集合
        /// </summary>
        private readonly HashSet<T> m_HashSet = new HashSet<T>();

        /// <summary>
        /// 创建实例的方法
        /// </summary>
        private Func<T> m_CreateInstanceCallback;

        /// <summary>
        /// 创建实例的方法
        /// </summary>
        private Action<T> m_DestroyInstanceCallback;

        /// <summary>
        /// 获取实例时的回调
        /// </summary>
        private Action<T> m_ActionOnGet;

        /// <summary>
        /// 释放实例时的回调
        /// </summary>
        private Action<T> m_ActionOnRelease;

        #endregion

        #region 构造方法

        public ObjectPoolBase()
        {
        }

        public ObjectPoolBase(Action<T> actionOnGet, Action<T> actionOnRelease)
        {
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 从池中获取实例。如果池中无实例，则池会创建新的实例。
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            T t = null;

            if (m_Stack.Count > 0)
            {
                lock (m_LockHelper)
                {
                    if (m_Stack.Count > 0)
                    {
                        t = m_Stack.Pop();
                        m_HashSet.Remove(t);
                    }
                }
            }

            if (t == null)
            {
                if (m_CreateInstanceCallback != null)
                {
                    t = m_CreateInstanceCallback();
                }
                else
                {
                    t = new T();
                }

                Interlocked.Increment(ref m_CountAll);
            }

            m_ActionOnGet?.Invoke(t);

            return t;
        }

        /// <summary>
        /// <para>从池中获取实例。如果池中无实例，则池会创建新的实例。</para>
        /// <para>这个重载将所请求的对象实例放在obj形参中，并返回一个与该实例关联的句柄。</para>
        /// <para>当句柄被卸载，即触发了Dispose()方法时，实例将被自动回收至对象池。</para>
        /// <para>使用这个重载获取的obj实例无需手动释放回池中。</para>
        /// </summary>
        /// <returns></returns>
        public PoolObjectHandle Get(out T obj)
        {
            obj = Get();
            return new PoolObjectHandle(this, obj);
        }

        /// <summary>
        /// 返回一个实例到池中。
        /// </summary>
        /// <param name="element"></param>
        public void Release(T element)
        {
            if (element == null)
            {
                LogLogger.LogVerbose(LogTag.LOG_Pool,
                    $"{ObjectPoolUtility.ObjectPoolLogTag}<{nameof(Release)}>{typeof(T).Name}: 要释放的实例为null。操作已被忽略。");
                return;
            }

            m_ActionOnRelease?.Invoke(element);

            lock (m_LockHelper)
            {
                if (m_HashSet.Contains(element))
                {
                    LogLogger.LogVerbose(LogTag.LOG_Pool,
                        $"{ObjectPoolUtility.ObjectPoolLogTag}<{nameof(Release)}> {typeof(T).Name}: 禁止重复释放同一实例到池中。操作已被忽略。");
                    return;
                }

                m_Stack.Push(element);
                m_HashSet.Add(element);
            }
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            if (m_DestroyInstanceCallback != null)
            {
                var list = new List<T>();

                lock (m_LockHelper)
                {
                    list.AddRange(m_Stack);
                    m_Stack.Clear();
                    m_HashSet.Clear();
                }

                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        m_DestroyInstanceCallback(list[i]);
                    }
                    catch (Exception e)
                    {
                        LogLogger.LogVerbose(LogTag.LOG_Pool,$"{ObjectPoolUtility.ObjectPoolLogTag}<{nameof(Clear)}>{typeof(T).Name}: {e}");
                    }
                }
            }
            else
            {
                lock (m_LockHelper)
                {
                    m_Stack.Clear();
                    m_HashSet.Clear();
                }
            }
        }

        /// <summary>
        /// <para>设置创建实例的回调</para>
        /// <para>当类型的实例不能通过Activator.CreateInstane()直接创建时，需要使用这个方法来指定创建实例的回调</para>
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public void SetInstanceCallbacks(Func<T> createInstanceCallback, Action<T> destroyInstanceCallback)
        {
            lock (m_LockHelper)
            {
                m_CreateInstanceCallback = createInstanceCallback;
                m_DestroyInstanceCallback = destroyInstanceCallback;
            }
        }

        /// <summary>
        /// <para>设置对象池回调</para>
        /// <para>当类型T没有实现IPoolObject时，会需要使用这个方法来指定放入池和取出池的回调</para>
        /// </summary>
        /// <param name="actionOnGet">获取实例时的回调</param>
        /// <param name="actionOnRelease">释放实例时的回调</param>
        public void SetPoolCallbacks(Action<T> actionOnGet, Action<T> actionOnRelease)
        {
            lock (m_LockHelper)
            {
                m_ActionOnGet = actionOnGet;
                m_ActionOnRelease = actionOnRelease;
            }
        }

        #endregion

        #region IObjectPoolBase 成员

        object IObjectPoolBase.Get() => Get();
        void IObjectPoolBase.Release(object instance) => Release((T)instance);
        void IObjectPoolBase.Clear() => Clear();

        #endregion
    }
