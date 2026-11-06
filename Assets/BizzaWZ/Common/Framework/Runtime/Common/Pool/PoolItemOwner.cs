using System;

namespace Bizza
{
    public readonly struct PoolItemOwner<T> : IDisposable where T : class
    {
        public readonly IPool<T> pool;
        public readonly T item;

        public PoolItemOwner(T item, IPool<T> pool)
        {
            this.item = item;
            this.pool = pool;
        }

        public void Dispose()
        {
            pool.Release(item);
        }
    }
}
