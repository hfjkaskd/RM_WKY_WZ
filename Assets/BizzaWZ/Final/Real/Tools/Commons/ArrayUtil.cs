#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;

namespace Bizza
{
    public static partial class ArrayUtil
    {
        public static int[] CreateByRange(int end)
        {
            return CreateByRange(0, end);
        }

        public static int[] CreateByRange(int start, int end)
        {
            int length = end - start;
            length = Math.Max(length, 1);
            var ret = new int[length];
            for (int i = 0, j = start; i < length; i++, j++)
            {
                ret[i] = j;
            }

            return ret;
        }

        public static int GetValueHashCode<T>(this IReadOnlyList<T> array)
        {
            HashCode hashCode = new();

            int length = array.Count;

            for (int i = 0; i < length; i++)
            {
                hashCode.Add(array[i]);
            }

            return hashCode.ToHashCode();
        }

        public static bool ValueEquals<T>(this IReadOnlyList<T> self, IReadOnlyList<T> other) where T : IEquatable<T>
        {
            if (self.Count != other.Count)
            {
                return false;
            }

            for (int i = 0, l = self.Count; i < l; i++)
            {
                if (!self[i].Equals(other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 通过索引数组一次性移除List中的元素
        /// 索引数组必须是有序的
        /// 这个方法的时间复杂度为O(m-n)
        /// m 为数组长度
        /// n 为第一个索引
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TIndexList"></typeparam>
        /// <param name="list"></param> 待处理List
        /// <param name="indexes"></param> 索引数组
        public static void RemoveByIndexes<TIndexList, TItem>(this List<TItem> list,
            TIndexList indexes) where TIndexList : IReadOnlyList<int>
        {
            if (indexes.Count == 0 || indexes[0] < 0)
            {
                return;
            }

            int len0 = list.Count;
            int len1 = indexes.Count;
            int j = 0;
            for (int i = indexes[0]; i < len0; i++)
            {
                if (j < len1 && i == indexes[j])
                {
                    j++;
                }
                else
                {
                    list[i - j] = list[i];
                }
            }

            list.RemoveRange(len0 - j, j);
        }


        public static void RemoveAllMatch<TItem, TEquals>(this List<TItem> list, TEquals equals)
            where TEquals : IEquatable<TItem>
        {
            int len0 = list.Count;
            int j = 0;
            for (int i = 0; i < len0; i++)
            {
                if (equals.Equals(list[i]))
                {
                    j++;
                }
                else
                {
                    list[i - j] = list[i];
                }
            }

            list.RemoveRange(len0 - j, j);
        }

        public static TItem FindMinItem<TItem>(this IReadOnlyList<TItem> list, Comparison<TItem> comparison)
        {
            if (list.Count == 0) return default;
            TItem ret = list[0];
            for (int i = 1, l = list.Count; i < l; i++)
            {
                if (comparison.Invoke(list[i], ret) < 0)
                {
                    ret = list[i];
                }
            }

            return ret;
        }

        public static TItem FindMinItem<TItem, TComparer>(this IReadOnlyList<TItem> list, TComparer comparer)
            where TComparer : IComparer<TItem>
        {
            if (list.Count == 0) return default;
            TItem ret = list[0];
            for (int i = 1, l = list.Count; i < l; i++)
            {
                if (comparer.Compare(list[i], ret) < 0)
                {
                    ret = list[i];
                }
            }

            return ret;
        }

        public static int IndexOf<TItem>(this IReadOnlyList<TItem> list, TItem item)
        {
            IEqualityComparer<TItem> comparer = EqualityComparer<TItem>.Default;
            for (int i = 0, l = list.Count; i < l; i++)
            {
                if (comparer.Equals(list[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool Contains<TItem>(this IReadOnlyList<TItem> list, TItem item)
        {
            return IndexOf(list, item) != -1;
        }

        public static int IndexOfSearch<TItem, TComparer>(this IReadOnlyList<TItem> array, TComparer value)
            where TComparer : IComparable<TItem>
        {
            int max = array.Count;
            for (int i = 0; i < max; i++)
            {
                if (value.CompareTo(array[i]) <= 0)
                {
                    return i;
                }
            }

            return max;
        }

        public static int SmartSearch<TItem, TComparer>(this IReadOnlyList<TItem> array, TComparer value)
            where TComparer : IComparable<TItem>
        {
            return array.Count > 8 ? BinarySearch(array, value) : IndexOfSearch(array, value);
        }

        public static void KeySort<T>(this T[] array, Func<T, float> compare)
        {
            float[] temp = new float[array.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = compare(array[i]);
            }

            System.Array.Sort(temp, array);
        }

        public static void MulKeySort<T>(this T[] array, Func<T, float>[] compares)
        {
            float[] temp = new float[array.Length];
            for (int j = 0; j < compares.Length; j++)
            {
                var compare = compares[j];
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = compare(array[i]);
                }

                Array.Sort(temp, array);
            }
        }

        public static void CompleteList<TItem>(this List<TItem> list, int count, TItem defaultValue)
            where TItem : struct
        {
            for (int i = list.Count; i < count; i++)
            {
                list.Add(defaultValue);
            }
        }

        public static void CompleteList<TItem>(this List<TItem> list, int count)
        {
            for (int i = list.Count; i < count; i++)
            {
                list.Add(default);
            }
        }

        public static void CompleteList<TItem>(this List<TItem> list, int count, Func<TItem> createFunc)
        {
            for (int i = list.Count; i < count; i++)
            {
                list.Add(createFunc());
            }
        }

        public static void CompleteListSimple<TItem>(this List<TItem> list, int count) where TItem : class, new()
        {
            for (int i = list.Count; i < count; i++)
            {
                list.Add(new TItem());
            }
        }

        public static void RemoveLast<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
            }
        }

        public static T Pop<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                var ret = list[^1];
                list.RemoveAt(list.Count - 1);
                return ret;
            }

            return default;
        }

        //  [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _SwapAndRemoveLast<TItem>(List<TItem> list, int index)
        {
            list[index] = list[^1];
            list.RemoveAt(list.Count - 1);
        }

        public static void SwapAndRemoveLast<T>(this List<T> list, int index)
        {
            if (list.HasIndex(index))
            {
                _SwapAndRemoveLast(list, index);
            }
        }

        public static void SwapAndRemoveLast<T>(this List<T> list, T t)
        {
            list.SwapAndRemoveLast(list.IndexOf(t));
        }

        // public static void SwapAndRemoveLast<T>(this List<T> list, T value)
        // {
        //     int index = list.IndexOf(value);
        //     if (index != -1) return;
        //     _SwapAndRemoveLast(list, index);
        // }

        public static void ShuffleSelf<T>(this Span<T> list, int start, int end, int count)
        {
            start = Math.Max(end - 1 - count, start);
            for (int i = end - 1; i > start; i--)
            {
                int index = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[index]) = (list[index], list[i]);
            }
        }

        public static void ShuffleSelf<T>(this IList<T> list, int start, int end, int count)
        {
            start = Math.Max(end - 1 - count, start);
            for (int i = end - 1; i > start; i--)
            {
                int index = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[index]) = (list[index], list[i]);
            }
        }

        public static void ShuffleSelf<T>(this Span<T> list)
        {
            list.ShuffleSelf(0, list.Length, list.Length);
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShuffleSelf<T>(this IList<T> list)
        {
            list.ShuffleSelf(0, list.Count, list.Count);
        }

        public static T Random<T>(this IReadOnlyList<T> array)
        {
            return array == null || array.Count == 0 ? default : array[UnityEngine.Random.Range(0, array.Count)];
        }

        public static void AddCollection<T>(this List<T> list, ICollection<T> collection)
        {
            list.AddRange(collection);
        }

        public static void CollectionCopyTo<T>(this ICollection<T> collection, T[] array, int index = 0)
        {
            collection.CopyTo(array, index);
        }

        public static void CopyToSafe<T>(IReadOnlyList<T> source, ref List<T> destination)
        {
            if (source == null) destination = null;
            else
            {
                if (destination == null) destination = new List<T>();
                else destination.Clear();
                foreach (var v in source)
                {
                    destination.Add(v);
                }
            }
        }

        public static void CopyTo<T>(this IReadOnlyList<T> source, IList<T> destination, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                destination[i] = source[i];
            }
        }

        public static void CopyToIList<T>(this IReadOnlyList<T> source, IList<T> destination)
        {
            CopyTo(source, destination, 0, Math.Min(source.Count, destination.Count));
        }

        public static int GetAppearCount<TItem, TEquals>(this IReadOnlyList<TItem> list, TEquals item)
            where TEquals : IEquatable<TItem>
        {
            int ret = 0;
            for (int i = 0, l = list.Count; i < l; i++)
            {
                if (item.Equals(list[i]))
                {
                    ret++;
                }
            }

            return ret;
        }

        /// <summary>
        /// 在数组二分搜索目标值
        /// 数组必须是有序的
        /// 会返回等于目标值的最小索引
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TComparer"></typeparam>
        /// <param name="array"></param> 数组
        /// <param name="value"></param> 搜索值
        /// <returns></returns>
        public static int BinarySearch<TItem, TComparer>(IReadOnlyList<TItem> array, TComparer value)
            where TComparer : IComparable<TItem>
        {
            int ret = array.Count;
            int left = 0, right = ret - 1;
            while (left <= right)
            {
                int mid = left + ((right - left) >> 1); // 防止越界
                if (value.CompareTo(array[mid]) <= 0)
                {
                    right = mid - 1;
                    ret = mid;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return ret;
        }
        
        /// <summary>
        /// 在数组二分搜索目标值
        /// 数组必须是有序的
        /// 会返回等于目标值的最小索引
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TList"></typeparam>
        /// <typeparam name="TComparer"></typeparam>
        /// <param name="array"></param> 数组
        /// <param name="value"></param> 搜索值
        /// <returns></returns>
        public static int BinarySearch<TList, TItem, TComparer>(TList array, TComparer value)
            where TComparer : IComparable<TItem> where TList : IReadOnlyList<TItem>
        {
            int outIndex = array.Count;
            int left = 0, right = outIndex - 1;
            while (left <= right)
            {
                int mid = left + ((right - left) >> 1); // 防止越界
                if (value.CompareTo(array[mid]) <= 0)
                {
                    right = mid - 1;
                    outIndex = mid;
                }
                else
                {
                    left = mid + 1;
                }
            }
        
            return outIndex;
        }
    }
}
#endif