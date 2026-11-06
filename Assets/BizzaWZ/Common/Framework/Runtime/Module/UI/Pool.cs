using System;
using System.Collections.Generic;
using Obfuz;

public class Pool<T> where T : IPoolElement
{
    private Queue<T> pool;

    private Func<T> m_createInstance;

    public Pool()
    {
        pool = new Queue<T>();
        IPoolElement
            m_createInstance = null;
    }

    public Pool(Func<T> createInstance)
    {
        pool = new Queue<T>();
        m_createInstance = createInstance;
    }

    public T Pop()
    {
        if (pool.Count == 0)
        {
            if (m_createInstance != null)
                return m_createInstance.Invoke();
            else
                return default;
        }

        T t = pool.Dequeue();

        return t;
    }

    public void Push(T t)
    {
        if (pool == null)
        {
            return;
        }

        t.PoolReset();
        pool.Enqueue(t);
    }

    public int Count()
    {
        return pool.Count;
    }

    public void Clear()
    {
        pool.Clear();
    }
}

/// <summary>
/// 池元素
/// </summary>
 
public interface IPoolElement
{
    /// <summary>
    /// 回池时重置
    /// </summary>
    void PoolReset();
}