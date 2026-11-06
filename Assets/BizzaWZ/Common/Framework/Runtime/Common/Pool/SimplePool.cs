using System;
using System.Collections.Generic;

namespace Bizza
{
    public abstract class SimplePool
    {
        public abstract void Release(object obj);
        // public abstract void Release<T>(T obj) where T : class;
    }

    public class SimpleObjPool<TItem> : SimplePool where TItem : class
    {
        private int _maxSize = 0;

        public int MaxSize
        {
            get => _maxSize;
            set
            {
                if (_maxSize == value) return;
                _maxSize = value;
                if (value < 0) return;
                while (_pool.Count > _maxSize)
                {
                    var item = _pool.Dequeue();
                    _set.Remove(item);
                }
            }
        }

        protected readonly Func<TItem> _funcCreate;
        protected readonly Queue<TItem> _pool = new();
        protected readonly HashSet<TItem> _set = new();

        public SimpleObjPool(Func<TItem> funcCreate, int maxSize = 10)
        {
            _funcCreate = funcCreate;
            _maxSize = maxSize;
        }

        public TItem Get()
        {
            if (_pool.Count <= 0) return _funcCreate();
            var ret = _pool.Dequeue();
            _set.Remove(ret);
            return ret;
        }

        public void Release(TItem target)
        {
            if (target == null) return;

            if (_set.Contains(target))
            {
                LogLogger.LogVerbose(LogTag.LOG_Pool,"target is already released");
                return;
            }

            if (target is IOnRelease releasable)
            {
                releasable.OnRelease();
            }

            if (_maxSize >= 0 && _maxSize <= _pool.Count)
            {
                return;
            }

            _pool.Enqueue(target);
            _set.Add(target);
        }

        public override void Release(object obj)
        {
            if (obj is TItem target)
            {
                Release(target);
            }
        }

        // public override void Release<T>(T obj) where T : class
        // {
        //     if (obj is TItem target)
        //     {
        //         Release(target);
        //     }
        // }
    }

    public class SimplePool<TItem> : SimpleObjPool<TItem> where TItem : class, new()
    {
        public SimplePool(int maxSize = 10) : base(() => new TItem(), maxSize)
        {
        }
    }

    public static class StaticSimplePool
    {
        public static readonly Dictionary<Type, SimplePool> pools = new();

        public static SimplePool<T> GetPool<T>() where T : class, new()
        {
            if (pools.TryGetValue(typeof(T), out var obj))
            {
                return obj as SimplePool<T>;
            }

            var pool = StaticSimplePool<T>.Pool;
            pools.Add(typeof(T), pool);
            return pool;
        }

        public static void Release<T>(T obj) where T : class
        {
            if (obj != null && pools.TryGetValue(obj.GetType(), out var pool))
            {
                pool.Release(obj);
            }
        }
    }

    public static class StaticSimplePool<T> where T : class, new()
    {
        public static readonly SimplePool<T> Pool = new();

        static StaticSimplePool()
        {
            StaticSimplePool.pools.Add(typeof(T), Pool);
        }
    }
}