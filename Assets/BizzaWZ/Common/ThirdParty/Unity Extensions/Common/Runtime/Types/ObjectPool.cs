using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    public struct ObjectScope<T> : IDisposable
    {
        ObjectPool<T> _pool;
        T _object;

        public T value => _object;

        public ObjectScope(ObjectPool<T> pool, out T item)
        {
            item = pool.Spawn();
            _pool = pool;
            _object = item;
        }

        public void Dispose()
        {
            if (_pool != null)
            {
                _pool.Despawn(_object);
                _pool = null;
                _object = default;
            }
        }

    } // ObjectScope


    public struct ArrayScopeNoClear<T> : IDisposable
    {
        ArrayPool<T> _pool;
        T[] _object;

        public T[] value => _object;

        public ArrayScopeNoClear(ArrayPool<T> pool, out T[] item)
        {
            item = pool.Spawn();
            _pool = pool;
            _object = item;
        }

        public void Dispose()
        {
            if (_pool != null)
            {
                _pool.DespawnNoClear(_object);
                _pool = null;
                _object = default;
            }
        }

    } // ArrayScopeNoClear


    public abstract class ObjectPool<T> : IDisposable
#if UNITY_EDITOR
        , IObjectPool
#endif
    {
        Stack<T> _objects;

        public int count
        {
            get => _objects.Count;
            set
            {
                if (value < 0)
                    throw new Exception();
                else if (value > _objects.Count)
                {
                    Prepare(value - _objects.Count);
                }
                else
                {
                    while (_objects.Count > value)
                        _objects.Pop();
                }
            }
        }

        public ObjectPool(int quantity = 0)
        {
            _objects = new Stack<T>(quantity > 16 ? quantity : 16);
            Prepare(quantity);

#if UNITY_EDITOR
            ObjectPoolProfiler.Add(this);
#endif
        }

        public T Spawn()
        {
#if UNITY_EDITOR
            spawned++;
#endif
            if (_objects.Count > 0)
                return _objects.Pop();
            else
            {
#if UNITY_EDITOR
                created++;
#endif
                return Create();
            }
        }

        public ObjectScope<T> Spawn(out T item)
        {
            return new ObjectScope<T>(this, out item);
        }

        public virtual void Despawn(T item)
        {
#if UNITY_EDITOR
            if (_objects.Contains(item))
            {
                Debug.LogError("Object is already despawned.");
                return;
            }

            despawnd++;
#endif
            _objects.Push(item);
        }

        public void Prepare(int quantity)
        {
            while (quantity > 0)
            {
#if UNITY_EDITOR
                created++;
#endif
                _objects.Push(Create());
                quantity--;
            }
        }

        public void Clear()
        {
            _objects.Clear();
        }

        public void Dispose()
        {
            if (_objects != null)
            {
                _objects.Clear();
                _objects = null;

#if UNITY_EDITOR
                ObjectPoolProfiler.Remove(this);
#endif
            }
        }

        protected abstract T Create();

#if UNITY_EDITOR
        public abstract string name { get; }
        public uint created { get; private set; }
        public uint remained => (uint)_objects.Count;
        public uint spawned { get; private set; }
        public uint despawnd { get; private set; }
#endif

    } // class ObjectPool<T>


    public abstract class ObjectPool<TObject, TPool> : ObjectPool<TObject> where TPool : ObjectPool<TObject, TPool>, new()
    {
        public static readonly TPool global = new TPool();
    }


    public abstract class CommonPool<TObject, TPool> : ObjectPool<TObject, TPool> where TObject : new() where TPool : CommonPool<TObject, TPool>, new()
    {
        protected override TObject Create() => new TObject();
    }


    public sealed class CommonPool<T> : CommonPool<T, CommonPool<T>> where T : new()
    {
#if UNITY_EDITOR
        public override string name => $"CommonPool<{typeof(T).Name}>";
#endif
    }


    public class ListPool<T> : CommonPool<List<T>, ListPool<T>>
    {
#if UNITY_EDITOR
        public override string name => $"ListPool<{typeof(T).Name}>";
#endif
        public override void Despawn(List<T> item)
        {
            item.Clear();
            base.Despawn(item);
        }
    }


    public class HashSetPool<T> : CommonPool<HashSet<T>, HashSetPool<T>>
    {
#if UNITY_EDITOR
        public override string name => $"HashSetPool<{typeof(T).Name}>";
#endif
        public override void Despawn(HashSet<T> item)
        {
            item.Clear();
            base.Despawn(item);
        }
    }


    public class QueuePool<T> : CommonPool<Queue<T>, QueuePool<T>>
    {
#if UNITY_EDITOR
        public override string name => $"QueuePool<{typeof(T).Name}>";
#endif
        public override void Despawn(Queue<T> item)
        {
            item.Clear();
            base.Despawn(item);
        }
    }


    public class StackPool<T> : CommonPool<Stack<T>, StackPool<T>>
    {
#if UNITY_EDITOR
        public override string name => $"StackPool<{typeof(T).Name}>";
#endif
        public override void Despawn(Stack<T> item)
        {
            item.Clear();
            base.Despawn(item);
        }
    }


    public class DictionaryPool<TKey, TValue> : CommonPool<Dictionary<TKey, TValue>, DictionaryPool<TKey, TValue>>
    {
#if UNITY_EDITOR
        public override string name => $"DictionaryPool<{typeof(TKey).Name}, {typeof(TValue).Name}>";
#endif
        public override void Despawn(Dictionary<TKey, TValue> item)
        {
            item.Clear();
            base.Despawn(item);
        }
    }


    public class LinkedListNodePool<T> : ObjectPool<LinkedListNode<T>, LinkedListNodePool<T>>
    {
#if UNITY_EDITOR
        public override string name => $"LinkedListNodePool<{typeof(T).Name}>";
#endif

        protected override LinkedListNode<T> Create() => new LinkedListNode<T>(default);

        public override void Despawn(LinkedListNode<T> item)
        {
            item.Value = default;
            item.List?.Remove(item);
            base.Despawn(item);
        }

        public LinkedListNode<T> Spawn(T value)
        {
            var item = Spawn();
            item.Value = value;
            return item;
        }
    }


    public class StringBuilderPool : CommonPool<StringBuilder, StringBuilderPool>
    {
#if UNITY_EDITOR
        public override string name => $"StringBuilderPool";
#endif
        public override void Despawn(StringBuilder item)
        {
            item.Clear();
            base.Despawn(item);
        }
    }


    public class MaterialPropertyBlockPool : CommonPool<MaterialPropertyBlock, MaterialPropertyBlockPool>
    {
#if UNITY_EDITOR
        public override string name => $"MaterialPropertyBlockPool";
#endif
        public override void Despawn(MaterialPropertyBlock item)
        {
            item.Clear();
            base.Despawn(item);
        }
    }


    public class GUIContentPool : CommonPool<GUIContent, GUIContentPool>
    {
#if UNITY_EDITOR
        public override string name => $"GUIContentPool";
#endif
        public override void Despawn(GUIContent item)
        {
            item.image = null;
            item.text = null;
            item.tooltip = null;
            base.Despawn(item);
        }
    }


    public class ArrayPool<T> : ObjectPool<T[]>
    {
        int _length;
        public ArrayPool(int length) => _length = length;

        static ArrayPool<T> _global1;
        public static ArrayPool<T> global1 => _global1 ??= new ArrayPool<T>(1);

        static ArrayPool<T> _global4;
        public static ArrayPool<T> global4 => _global4 ??= new ArrayPool<T>(4);

        static ArrayPool<T> _global16;
        public static ArrayPool<T> global16 => _global16 ??= new ArrayPool<T>(16);

        static ArrayPool<T> _global64;
        public static ArrayPool<T> global64 => _global64 ??= new ArrayPool<T>(64);

        static ArrayPool<T> _global256;
        public static ArrayPool<T> global256 => _global256 ??= new ArrayPool<T>(256);

#if UNITY_EDITOR
        public override string name => $"ArrayPool<{typeof(T).Name}>[{_length}]";
#endif

        protected override T[] Create() => new T[_length];

        public ArrayScopeNoClear<T> SpawnNoClear(out T[] item)
        {
            return new ArrayScopeNoClear<T>(this, out item);
        }

        public override void Despawn(T[] item)
        {
            Array.Clear(item, 0, _length);
            base.Despawn(item);
        }

        public void DespawnNoClear(T[] item)
        {
            base.Despawn(item);
        }
    }


#if UNITY_EDITOR

    interface IObjectPool : INamed
    {
        uint created { get; }
        uint remained { get; }
        uint spawned { get; }
        uint despawnd { get; }
    }

    static class ObjectPoolProfiler
    {
        static List<IObjectPool> _pools = new List<IObjectPool>();

        internal static int poolCount => _pools.Count;
        internal static IObjectPool GetPool(int index) => _pools[index];

        internal static void Add(IObjectPool pool) => _pools.Add(pool);
        internal static void Remove(IObjectPool pool) => _pools.Remove(pool);
    }

    class ObjectPoolProfilerWindow : EditorWindow
    {
        float _time;

        [MenuItem("Window/Unity Extensions/Object Pool Profiler")]
        static void ShowWindow()
        {
            var window = GetWindow<ObjectPoolProfilerWindow>("Object Pool Profiler");
            window.Show();
        }

        void OnEnable()
        {
            _time = 0;
            EditorApplication.update += OnUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
        }

        void OnUpdate()
        {
            _time += Editor.EditorUtils.deltaTime;
            if (_time >= 0.2f)
            {
                _time = 0f;
                Repaint();
            }
        }

        void OnGUI()
        {
            var rect = EditorGUILayout.GetControlRect();

            var nameRect = rect;
            nameRect.width = rect.width * 0.36f;

            var createdRect = new Rect(nameRect.xMax, rect.y, rect.width * 0.16f, rect.height);
            var remainedRect = new Rect(createdRect.xMax, rect.y, rect.width * 0.16f, rect.height);
            var spawnedRect = new Rect(remainedRect.xMax, rect.y, rect.width * 0.16f, rect.height);
            var despawndRect = new Rect(spawnedRect.xMax, rect.y, rect.width * 0.16f, rect.height);

            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.05f));
            GUI.Label(nameRect, "Pool Name", EditorStyles.boldLabel);
            GUI.Label(createdRect, "Created", EditorStyles.boldLabel);
            GUI.Label(remainedRect, "Remained", EditorStyles.boldLabel);
            GUI.Label(spawnedRect, "Spawned", EditorStyles.boldLabel);
            GUI.Label(despawndRect, "Despawnd", EditorStyles.boldLabel);

            for (int i = 0; i < ObjectPoolProfiler.poolCount; i++)
            {
                rect = EditorGUILayout.GetControlRect();

                nameRect.y = rect.y;
                createdRect.y = rect.y;
                remainedRect.y = rect.y;
                spawnedRect.y = rect.y;
                despawndRect.y = rect.y;

                EditorGUI.DrawRect(rect, (i % 2) == 0 ? new Color(1, 1, 1, 0.05f) : new Color(0, 0, 0, 0.05f));

                var pool = ObjectPoolProfiler.GetPool(i);

                GUI.Label(nameRect, pool.name, EditorStyles.label);
                GUI.Label(createdRect, pool.created.ToString(), EditorStyles.label);
                GUI.Label(remainedRect, pool.remained.ToString(), EditorStyles.label);
                GUI.Label(spawnedRect, pool.spawned.ToString(), EditorStyles.label);
                GUI.Label(despawndRect, pool.despawnd.ToString(), EditorStyles.label);
            }
        }
    }

#endif

} // namespace UnityExtensions