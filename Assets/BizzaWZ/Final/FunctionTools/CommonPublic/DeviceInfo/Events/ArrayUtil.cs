using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

    public static class ArrayUtil
    {
        public static int GetValueHashCode<T>(this IReadOnlyList<T> array)
        {
            if (array.Count == 0)
            {
                return 0;
            }
            int code = array[0].GetHashCode();
            for (int i = 1; i < array.Count; i++)
            {
                code ^= array[i].GetHashCode() << 2;
            }
            return code;
        }

        public static bool ValueEquals<T>(this IReadOnlyList<T> self, IReadOnlyList<T> other) where T : IEquatable<T>
        {
            if (self.Count != other.Count)
            {
                return false;
            }
            for (int i = 0; i < self.Count; i++)
            {
                if (!self[i].Equals(other[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool HasIndex<T>(this List<T> array, int index)
        {
            return index > -1 && index < array.Count;
        }
        public static bool HasIndex<T>(this T[] array, int index)
        {
            return index > -1 && index < array.Length;
        }
        public static bool HasIndex<T>(this IList<T> array, int index)
        {
            return index > -1 && index < array.Count;
        }
        public static bool HasIndex<T>(this IReadOnlyList<T> array, int index)
        {
            return index > -1 && index < array.Count;
        }

        public static bool HasIndex<T>(this T[,] array, int firstIndex, int secondIndex)
        {
            return firstIndex > -1 && firstIndex < array.GetLength(0)
                && secondIndex > -1 && secondIndex < array.GetLength(1);
        }

        public static bool TryGetItem<T>(this IReadOnlyList<T> array, int index, out T outT)
        {
            if (array.HasIndex(index))
            {
                outT = array[index];
                return true;
            }
            outT = default;
            return false;
        }

        public static bool TryGetItem<T>(this T[,] array, int firstIndex, int secondIndex, out T outT)
        {
            if (array.HasIndex(firstIndex, secondIndex))
            {
                outT = array[firstIndex, secondIndex];
                return true;
            }
            outT = default;
            return false;
        }

        public static T GetItemOrDefault<T>(this IReadOnlyList<T> array, int index)
        {
            array.TryGetItem(index, out T @out);
            return @out;
        }

        public static T GetItemOrDefault<T>(this T[,] array, int firstIndex, int secondIndex)
        {
            array.TryGetItem(firstIndex, secondIndex, out T @out);
            return @out;
        }

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


        /// <summary>
        /// 通过索引数组一次性移除List中的元素
        /// 索引数组必须是有序的
        /// 这个方法的时间复杂度为O(m-n)
        /// m 为数组长度
        /// n 为第一个索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param> 待处理List
        /// <param name="indexs"></param> 索引数组
        public static void RemoveByIndexs<T>(this List<T> list, IReadOnlyList<int> indexs)
        {
            if (indexs.Count == 0 || indexs[0] < 0) { return; }
            int j = 0;
            for (int i = indexs[0]; i < list.Count && j < indexs.Count; i++)
            {
                if (i == indexs[j])
                {
                    j++;
                }
                else
                {
                    list[i - j] = list[i];
                }
            }
            list.RemoveRange(list.Count - j, j);
        }
        //public static void RemoveByIndexs<T>(this IList<T> list, IReadOnlyList<int> indexs)
        //{
        //    if (indexs.Count == 0 || indexs[0] < 0) { return; }
        //    int j = 0;
        //    for (int i = indexs[0]; i < list.Count && j < indexs.Count; i++)
        //    {
        //        if (i == indexs[j])
        //        {
        //            j++;
        //        }
        //        else
        //        {
        //            list[i - j] = list[i];
        //        }
        //    }
        //    int min = list.Count - j - 1;
        //    for (int i = list.Count - 1; i > min; i--)
        //    {
        //        list.RemoveAt(i);
        //    }
        //}

        /// <summary>
        /// 在数组二分搜索目标值
        /// 数组必须是有序的
        /// 会返回等于目标值的最小索引
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param> 数组
        /// <param name="value"></param> 搜索值
        /// <returns></returns>
        public static int BinarySearch<T>(this IReadOnlyList<T> array, T value) where T : IComparable<T>
        {
            int outIndex = array.Count;
            int left = 0, right = outIndex - 1;
            while (left <= right)
            {
                int mid = left + ((right - left) >> 1);// 防止越界
                int compare = array[mid].CompareTo(value);
                if (compare >= 0)
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

        public static int IndexOfSearch<T>(this IReadOnlyList<T> array, T value) where T : IComparable<T>
        {
            int outIndex = array.Count;
            for (int i = outIndex - 1; i > -1; outIndex--, i--)
            {
                if (value.CompareTo(array[i]) > 0) { break; }
            }
            return outIndex;
        }

        public static int SmartSearch<T>(this IReadOnlyList<T> array, T value) where T : IComparable<T>
        {
            return array.Count > 8 ? BinarySearch(array, value) : IndexOfSearch(array, value);
        }

        public static float[] GetSumArray(this IReadOnlyList<float> array)
        {
            float[] outArray = new float[array.Count];
            if (array.Count == 0) { return outArray; }
            outArray[0] = array[0];
            for (int i = 1; i < array.Count; i++)
            {
                outArray[i] = outArray[i - 1] + array[i];
            }
            return outArray;
        }

        public static int[] GetSumArray(this IReadOnlyList<int> array)
        {
            int[] outArray = new int[array.Count];
            if (array.Count == 0) { return outArray; }
            outArray[0] = array[0];
            for (int i = 1; i < array.Count; i++)
            {
                outArray[i] = outArray[i - 1] + array[i];
            }
            return outArray;
        }

        public static int RandomBySumArray(this IReadOnlyList<float> sumArray)
        {
            if (sumArray.Count == 0) { return -1; }
            float max = sumArray[^1];
            if (max <= 0) { return -1; }
            float random = UnityEngine.Random.Range(0, max);
            return BinarySearch(sumArray, random);
        }

        public static int RandomBySumArray(this IReadOnlyList<int> sumArray)
        {
            if (sumArray.Count == 0) { return -1; }
            int max = sumArray[^1];
            if (max <= 0) { return -1; }
            int random = UnityEngine.Random.Range(0, max);
            return BinarySearch(sumArray, random + 1);
        }

        public static T Random<T>(this IReadOnlyList<T> array)
        {
            if (array == null || array.Count == 0)
            {
                return default(T);
            }

            return array[UnityEngine.Random.Range(0, array.Count)];
        }

        public static int RandomByWeight(this IReadOnlyList<float> weightArray)
        {
            var total = 0f;
            foreach (var v in weightArray)
            {
                total += v;
            }

            if (total <= 0)
            {
                return -1;
            }

            var rd = UnityEngine.Random.Range(1, total + 1);
            for (var i = 0; i < weightArray.Count; i++)
            {
                var v = weightArray[i];
                rd -= v;
                if (rd <= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<int> _cachedList = new();
        public static T RandomByWeight<T>(this IReadOnlyList<T> dataArray, Func<T, int> getWeightFunc)
        {
            if (dataArray == null || dataArray.Count == 0)
            {
                return default;
            }

            _cachedList.Clear();
            for (var i = 0; i < dataArray.Count; i++)
            {
                var weight = getWeightFunc(dataArray[i]);
                _cachedList.Add(weight);
            }

            // Debug.Log(string.Join(',', _cachedList));
            var idx = RandomByWeight(_cachedList);
            if (idx == -1)
            {
                return default;
            }

            return dataArray[idx];
        }

        public static int RandomByWeight(this IReadOnlyList<int> weightArray)
        {
            var total = 0;
            foreach (var v in weightArray)
            {
                total += v;
            }

            if (total <= 0)
            {
                return -1;
            }

            var rd = UnityEngine.Random.Range(1, total + 1);
            for (var i = 0; i < weightArray.Count; i++)
            {
                var v = weightArray[i];
                rd -= v;
                if (rd <= 0)
                {
                    return i;
                }
            }

            return -1;
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
                System.Array.Sort(temp, array);
            }
        }

        public static void CompleteList<T>(this List<T> list, int count, T defaultValue = default)
        {
            for (int i = list.Count; i < count; i++)
            {
                list.Add(defaultValue);
            }
        }

        public static void RemoveLast<T>(this IList<T> list)
        {
            if (list.Count > 0)
            {
                list.RemoveAt(list.Count - 1);
            }
        }

        public static void SwapAndRemoveLast<T>(this IList<T> list, int index)
        {
            if (list.HasIndex(index))
            {
                list[index] = list[^1];
                list.RemoveAt(list.Count - 1);
            }
        }

        public static void ShuffleSelf<T>(this IList<T> list, int count)
        {
            count = Math.Max(list.Count - 1 - count, 0);
            Random random = new();
            for (int i = list.Count - 1; i > count; i--)
            {
                int index = random.Next(i + 1);
                (list[i], list[index]) = (list[index], list[i]);
            }
        }

        public static void ShuffleSelf<T>(this IList<T> list)
        {
            list.ShuffleSelf(list.Count);
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
    }

