using System.Collections;
using System.Collections.Generic;

namespace Bizza
{
    public abstract class CollectionPool<TCollection, TItem> : IPool<TCollection>
        where TCollection : class, ICollection<TItem>
    {
        private readonly List<int> _capacity = new();
        private readonly List<TCollection> _pool = new();

        public int MaxCapacity { get; set; } = 128;

        protected abstract TCollection Create(int capacity);
        protected abstract int GetCapacity(TCollection collection);

        private int GetIndex(int capacity)
        {
            int index = _capacity.BinarySearch(capacity);
            if (index < 0) index = ~index;
            return index;
        }

        public TCollection Get(int capacity)
        {
            int index = GetIndex(capacity);
            if (index >= _pool.Count) return Create(capacity);
            _capacity.RemoveAt(index);
            var ret = _pool[index];
            _pool.RemoveAt(index);
            return ret;
        }

        public TCollection Get() => Get(0);

        public void Release(TCollection collection)
        {
            if (collection == null) return;
            collection.Clear();
            int capacity = GetCapacity(collection);
            if (capacity > MaxCapacity) return;
            int index = GetIndex(capacity);
            _pool.Insert(index, collection);
            _capacity.Insert(index, capacity);
        }
    }

    public class ListPool<T> : CollectionPool<List<T>, T>
    {
        protected override List<T> Create(int capacity) => new(capacity);
        protected override int GetCapacity(List<T> collection) => collection.Capacity;

        public static readonly ListPool<T> Instance = new();
    }
}