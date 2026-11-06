using System;
using System.Collections;
using System.Collections.Generic;

    public sealed class ListDictionary<TKey, TValue> : IEnumerable<IListDictionaryKeyValuePair<TKey, TValue>>
    {
        private sealed class ValueWrapper : IListDictionaryKeyValuePair<TKey, TValue>
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }

            public void Release()
            {
                Key = default;
                Value = default;
            }
        }

        #region Fields
        public TValue this[TKey key] { get => m_InnerDict[key].Value; set => Set(key, value); }

        public int Count => m_InnerList.Count;
        public IEnumerable<TKey> Keys => m_InnerDict.Keys;
        public IEnumerable<TValue> Values { get { foreach (var v in m_InnerDict.Values) { yield return v.Value; } } }
        #endregion

        #region Internal Fields
        private List<ValueWrapper> m_InnerList = new List<ValueWrapper>();
        private Dictionary<TKey, ValueWrapper> m_InnerDict = new Dictionary<TKey, ValueWrapper>();
        #endregion

        #region Public Functions
        public bool ContainsKey(TKey key) => m_InnerDict.ContainsKey(key);

        public bool Contains(TValue val) => m_InnerList.Find(v => Object.Equals(v.Value, val)) != null;

        public TValue GetAt(int index) => m_InnerList[index].Value;

        public bool TryGetValue(TKey key, out TValue val)
        {
            if (m_InnerDict.TryGetValue(key, out var v))
            {
                val = v.Value;
                return true;
            }

            val = default;
            return false;
        }

        public void Set(TKey key, TValue val)
        {
            if (!m_InnerDict.TryGetValue(key, out var v))
            {
                v = new ValueWrapper()
                {
                    Key = key,
                };

                m_InnerDict.Add(key, v);
                m_InnerList.Add(v);
            }

            v.Value = val;
        }

        public void Add(TKey key, TValue val)
        {
            var v = new ValueWrapper();

            m_InnerDict.Add(key, v);
            m_InnerList.Add(v);

            v.Value = val;
        }

        public bool RemoveByKey(TKey key)
        {
            if (m_InnerDict.TryGetValue(key, out var v))
            {
                m_InnerDict.Remove(key);
                m_InnerList.Remove(v);
                v.Release();
                return true;
            }

            return false;
        }

        public bool RemoveByValue(TValue val)
        {
            var listCount = m_InnerList.Count;

            for (var i = 0; i < listCount; i++)
            {
                var v = m_InnerList[i];

                if (Object.Equals(v, val))
                {
                    m_InnerDict.Remove(v.Key);
                    m_InnerList.RemoveAt(i);
                    v.Release();
                    return true;
                }
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            var v = m_InnerList[index];

            m_InnerList.RemoveAt(index);
            m_InnerDict.Remove(v.Key);

            v.Release();
        }

        public void Clear()
        {
            m_InnerDict.Clear();
            m_InnerList.Clear();
        }

        public IEnumerator<IListDictionaryKeyValuePair<TKey, TValue>> GetEnumerator() => m_InnerList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_InnerDict.GetEnumerator();
        #endregion
    }

    public interface IListDictionaryKeyValuePair<TKey, TValue>
    {
        TKey Key { get; }
        TValue Value { get; }
    }
