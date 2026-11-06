using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation
{
    public interface IOriginalPoolable
    {
        int GetPoolId();
        void OnSpawn();
        void OnRecycle();
    }

    // GameObjectPool 0x2605a00/0x2605dfc/0x2605c54. The configured root
    // replaces only the empty container construction, keeping prefab-first.
    public sealed class OriginalGameObjectPool : IOriginalItemViewRecycleFactory
    {
        private readonly OriginalGameObjectPoolStack[] pools;
        private readonly List<IOriginalPoolable> recycleSnapshot = new List<IOriginalPoolable>();
        private bool recyclingAll;
        private Transform root;
        public OriginalGameObjectPool(int size, Transform root)
        {
            pools = new OriginalGameObjectPoolStack[size];
            this.root = root;
            Object.DontDestroyOnLoad(root);
        }
        public Transform GetPoolTransform() => root;
        public int PoolCount => pools.Length;
        public bool IsPoolAlive() => root != null;
        public bool HasPool(int id) => pools[id] != null;
        public int GetPoolLength(int id) => pools[id] == null ? 0 : pools[id].Count;
        public int GetSpawnedCount(int id) => pools[id] == null ? 0 : pools[id].Spawned.Count;

        public void CreatePool<T>(T prefab, int amount) where T : MonoBehaviour, IOriginalPoolable
        {
            int id = prefab.GetPoolId();
            var stack = pools[id];
            if (stack == null)
            {
                stack = new OriginalGameObjectPoolStack(prefab, amount, root);
                // Native re-queries the prefab id after constructing the stack.
                pools[prefab.GetPoolId()] = stack;
            }
            int existing = stack.Count;
            if (existing >= amount) return;
            int remaining = unchecked(amount - existing);
            for (int i = 0; i < remaining; i++)
            {
                var instance = Object.Instantiate(prefab, root);
                instance.gameObject.SetActive(false);
                stack.Push(instance);
            }
        }
        public T Spawn<T>(int id, bool activate) where T : MonoBehaviour, IOriginalPoolable
        {
            var stack = pools[id];
            if (stack == null) return null;
            var instance = (T)stack.Pop();
            if (instance is OriginalItemSwapView view) view.PoolUse = unchecked(view.PoolUse + 1);
            if (activate) instance.gameObject.SetActive(true);
            stack.Spawned.Add(instance);
            instance.OnSpawn();
            return instance;
        }
        public void Recycle<T>(T instance) where T : MonoBehaviour, IOriginalPoolable
        {
            var stack = pools[instance.GetPoolId()];
            if (stack == null) return;
            if (stack.Spawned.Remove(instance)) ReturnToPool(stack, instance);
            else if (!recyclingAll) Debug.LogError("Duplicate object returned to pool.");
        }
        void IOriginalItemViewRecycleFactory.Recycle(IOriginalItemRecycleView view) => Recycle((OriginalItemSwapView)view);
        private void ReturnToPool(OriginalGameObjectPoolStack stack, IOriginalPoolable instance)
        {
            if (instance is MonoBehaviour behaviour)
            {
                behaviour.gameObject.SetActive(false);
                behaviour.transform.SetParent(root);
            }
            instance.OnRecycle();
            stack.Push(instance);
        }
        public void RecycleAllSpawnedOfPoolId(int id)
        {
            var stack = pools[id];
            if (stack == null || stack.Spawned.Count == 0) return;
            recycleSnapshot.AddRange(stack.Spawned);
            RecycleSnapshot();
        }
        public void RecycleAllSpawned()
        {
            foreach (var stack in pools)
                if (stack != null) recycleSnapshot.AddRange(stack.Spawned);
            RecycleSnapshot();
        }
        private void RecycleSnapshot()
        {
            // Owners such as potion racks and TNT return their child views.
            // A child may already have been returned by this sweep.
            recyclingAll = true;
            try
            {
                foreach (var instance in recycleSnapshot)
                {
                    var stack = pools[instance.GetPoolId()];
                    if (stack.Spawned.Remove(instance)) ReturnToPool(stack, instance);
                }
            }
            finally
            {
                recycleSnapshot.Clear();
                recyclingAll = false;
            }
        }
        public void DecreasePool(int id, int amount)
        {
            pools[id]?.DecreaseCountTo(amount);
        }
        public void ClearPool(int id)
        {
            pools[id]?.Clear();
            pools[id] = null;
        }
        public void ClearAllPools()
        {
            for (int i = 0; i < pools.Length; i++) ClearPool(i);
        }
        public void Destroy()
        {
            Object.Destroy(root.gameObject);
            root = null;
        }
    }

    // GameObjectPoolStack 0x2d72318..0x2d72650.
    public sealed class OriginalGameObjectPoolStack
    {
        private MonoBehaviour prefab;
        private readonly Transform root;
        private readonly Stack<IOriginalPoolable> available;
        private readonly HashSet<int> availableHashes = new HashSet<int>();
        public readonly HashSet<IOriginalPoolable> Spawned;
        public int Count => available.Count;
        public OriginalGameObjectPoolStack(MonoBehaviour prefab, int size, Transform root)
        {
            this.prefab = prefab;
            this.root = root;
            available = new Stack<IOriginalPoolable>(size);
            Spawned = new HashSet<IOriginalPoolable>(size);
        }
        public void TrySetGameObject(MonoBehaviour value)
        {
            if (prefab == null) prefab = value;
        }
        public void Push(IOriginalPoolable instance)
        {
            int hash = instance.GetHashCode();
            if (availableHashes.Contains(hash))
            {
                // Original log reflects a type name. Keep its error outcome
                // without reflection, which this project explicitly forbids.
                Debug.LogError("Duplicate object returned to pool.");
                return;
            }
            availableHashes.Add(hash);
            available.Push(instance);
        }
        public IOriginalPoolable Pop()
        {
            if (available.Count == 0) return (IOriginalPoolable)Object.Instantiate(prefab, root);
            var instance = available.Pop();
            availableHashes.Remove(instance.GetHashCode());
            return instance;
        }
        public void DecreaseCountTo(int amount)
        {
            int remaining = unchecked(available.Count - amount);
            if (remaining < 1)
            {
                if (ReferenceEquals(prefab, null)) throw new NullReferenceException();
                Debug.Log("Pool already at or below requested size.");
                return;
            }
            for (int i = 0; i < remaining; i++)
            {
                var instance = available.Pop();
                availableHashes.Remove(instance.GetHashCode());
                DestroyItem(instance);
            }
        }
        public void Clear()
        {
            prefab = null;
            availableHashes.Clear();
            foreach (var instance in available) DestroyItem(instance);
            available.Clear();
        }
        private static void DestroyItem(IOriginalPoolable instance)
        {
            var behaviour = (MonoBehaviour)instance;
            if (behaviour != null && behaviour.gameObject != null) Object.Destroy(behaviour.gameObject);
        }
    }
}
