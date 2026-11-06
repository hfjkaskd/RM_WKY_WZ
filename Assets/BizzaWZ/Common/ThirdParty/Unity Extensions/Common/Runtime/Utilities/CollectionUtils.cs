using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// Extensions for Collections.
    /// </summary>
    public static class CollectionUtils
    {
        /// <summary>
        /// Set values of elements in an array.
        /// </summary>
        /// <param name="index"> Start index. </param>
        /// <param name="count"> A negative or zero value means all elements after start index. </param>
        public static void SetValues<T>(
            this T[] array,
            T value = default,
            int index = 0,
            int count = 0)
        {
            int lastIndex = count > 0 ? (index + count) : array.Length;
            while(index < lastIndex) array[index++] = value;
        }


        /// <summary>
        /// Set values of elements in a 2d array.
        /// </summary>
        /// <param name="beginRowIndex">  Start row index </param>
        /// <param name="beginColIndex"> Start col index </param>
        /// <param name="endRowIndex"> A negative or zero value means all elements after start index. </param>
        /// <param name="endColIndex"> A negative or zero value means all elements after start index. </param>
        public static void SetValues<T>(
            this T[,] array,
            T value = default,
            int beginRowIndex = 0,
            int beginColIndex = 0,
            int endRowIndex = 0,
            int endColIndex = 0)
        {
            if (endRowIndex <= 0) endRowIndex = array.GetLength(0)-1;
            if (endColIndex <= 0) endColIndex = array.GetLength(1)-1;

            for (int i = beginRowIndex; i <= endRowIndex; i++)
            {
                for (int j = beginColIndex; j <= endColIndex; j++)
                {
                    array[i,j] = value;
                }
            }
        }


        /// <summary>
        /// Find the nearest element with specific value in an array.
        /// </summary>
        public static int FindNearestIndex(this float[] items, float value)
        {
            if (IsNullOrEmpty(items)) return -1;

            int result = 0;
            float minError = Mathf.Abs(value - items[0]);
            float error;

            for (int i = 1; i < items.Length; i++)
            {
                error = Mathf.Abs(value - items[i]);
                if (error < minError)
                {
                    minError = error;
                    result = i;
                }
            }

            return result;
        }


        /// <summary>
        /// Change the size of the list.
        /// </summary>
        public static void Resize<T>(this List<T> list, int newSize, T newValue = default)
        {
            if (list.Count != newSize)
            {
                if (list.Count > newSize)
                {
                    list.RemoveRange(newSize, list.Count - newSize);
                }
                else
                {
                    int addCount = newSize - list.Count;

                    while (addCount > 0)
                    {
                        list.Add(newValue);
                        addCount--;
                    }
                }
            }
        }


        /// <summary>
        /// Change the size of the list.
        /// </summary>
        public static void Resize(this IList list, int newSize, object newValue = null)
        {
            if (list.Count != newSize)
            {
                if (list.Count > newSize)
                {
                    for (int i = list.Count - 1; i >= newSize; i--)
                    {
                        list.RemoveAt(i);
                    }
                }
                else
                {
                    int addCount = newSize - list.Count;

                    while (addCount > 0)
                    {
                        list.Add(newValue);
                        addCount--;
                    }
                }
            }
        }


        public static bool Equals(int[] a, int[] b)
        {
            if (a == b) return true;

            if (a == null || b == null || a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;

            return true;
        }


        /// <summary>
        /// Sort elements part of the list.
        /// </summary>
        public static void Sort<T>(this T[] array, int first, int last, Comparison<T> compare)
        {
            for (int i = first + 1; i <= last; i++)
            {
                var value = array[i];
                int position = i;

                while (position > first && compare(array[position - 1], value) > 0)
                {
                    array[position] = array[position - 1];
                    position--;
                }

                array[position] = value;
            }
        }


        public static TFind Find<TElement, TFind>(this IList<TElement> list)
        {
            foreach (var i in list)
            {
                if (i is TFind r) return r;
            }
            return default;
        }


        public static T Find<T>(this IList<T> list, Type type)
        {
            foreach (var i in list)
            {
                if (i != null && type.IsAssignableFrom(i.GetType())) return i;
            }
            return default;
        }


        public static void CopyTo<T, U>(this IList<T> list, U[] array, int index) where T : U
        {
            for (int i = 0; i < list.Count; i++)
            {
                array[index + i] = list[i];
            }
        }


        public static T Last<T>(this IList<T> list) => list[list.Count - 1];


        public static ref T Last<T>(this T[] array) => ref array[array.Length - 1];


        public static void Swap<T>(this IList<T> list, int firstIndex, int secondIndex)
        {
            var first = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = first;
        }


        public static void Add<T>(this IList<T> list, IList<T> elements)
        {
            if (elements != null)
            {
                int count = elements.Count;
                for (int i = 0; i < count; i++)
                    list.Add(elements[i]);
            }
        }


        public static void Add<T>(ref T[] array, T item)
        {
            if (array == null) array = new T[] { item };
            else
            {
                Array.Resize(ref array, array.Length + 1);
                array[array.Length - 1] = item;
            }
        }


        public static void Insert<T>(ref T[] array, T item, int index)
        {
            if (array == null || array.Length == 0)
                array = new T[] { item };
            else
            {
                var result = new T[array.Length + 1];
                if (index > 0) Array.Copy(array, 0, result, 0, index);
                if (index < array.Length) Array.Copy(array, index, result, index + 1, array.Length - index);
                result[index] = item;
                array = result;
            }
        }


        public static void RemoveAt<T>(ref T[] array, int index)
        {
            int length = array.Length - 1;
            var newArray = new T[length];
            if (index > 0) Array.Copy(array, 0, newArray, 0, index);
            if (index < length) Array.Copy(array, index + 1, newArray, index, length - index);
            array = newArray;
        }


        public static bool IsNullOrEmpty<T>(ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }


        public static List<T> Clone<T>(this List<T> list)
        {
            int count = list.Count;
            var result = new List<T>(count);
            for (int i = 0; i < count; i++)
                result.Add(list[i]);
            return result;
        }


        public static T[] Concat<T>(this T[] array1, T[] array2)
        {
            if (array2 == null)
                return array1;

            if (array1 == null)
                return array2;

            if (array2.Length == 0)
                return array1;

            if (array1.Length == 0)
                return array2;

            var result = new T[array1.Length + array2.Length];
            Array.Copy(array1, 0, result, 0, array1.Length);
            Array.Copy(array2, 0, result, array1.Length, array2.Length);
            return result;
        }


        /// <summary>
        /// Traverse any array.
        /// </summary>
        /// <param name="onElement"> param1 is dimension index, param2 is element indexes in every dimension </param>
        /// <param name="beginDimension"> param1 is dimension index, param2 is indexes in every dimension before this dimension </param>
        /// <param name="endDimension"> param1 is dimension index, param2 is indexes in every dimension before this dimension </param>
        public static void Traverse(
            this Array array,
            Action<int, int[]> onElement,
            Action<int, int[]> beginDimension = null,
            Action<int, int[]> endDimension = null)
        {
            if (array.Length != 0)
            {
                TraverseArrayDimension(0, new int[array.Rank]);
            }

            void TraverseArrayDimension(int dimension, int[] indices)
            {
                int size = array.GetLength(dimension);
                bool isFinal = (dimension + 1 == array.Rank);

                beginDimension?.Invoke(dimension, indices);

                for (int i = 0; i < size; i++)
                {
                    indices[dimension] = i;
                    if (isFinal)
                    {
                        onElement?.Invoke(dimension, indices);
                    }
                    else TraverseArrayDimension(dimension + 1, indices);
                }

                endDimension?.Invoke(dimension, indices);
            }
        }


        /// <summary>
        /// 二分查找，如果没找到，index 为目标位置相邻下标
        /// </summary>
        static bool BinarySearch<T>(this IList<T> list, T target, Comparison<T> compare, out int index)
        {
            int min = 0;
            int max = list.Count - 1;
            index = -1;

            while (min <= max)
            {
                index = (min + max) / 2;
                int comp = compare(target, list[index]);
                if (comp < 0) max = index - 1;
                else if (comp > 0) min = index + 1;
                else return true;
            }

            return false;
        }


        /// <summary>
        /// Get the text description of the array, the text is similar to C# code.
        /// </summary>
        public static string ToCodeString(this Array array, Func<object, string> elementToString = null)
        {
            if (array == null) return "Null";

            if (elementToString == null)
            {
                elementToString = obj =>
                {
                    if (ReferenceEquals(obj, null))
                    {
                        return "null";
                    }
                    if (obj.GetType() == typeof(string))
                    {
                        return string.Format("\"{0}\"", obj);
                    }
                    return obj.ToString();
                };
            }

            var builder = new System.Text.StringBuilder(array.Length * 4);

            Traverse(array,
                (d, i) =>
                {
                    if (i[d] != 0) builder.Append(',');
                    builder.Append(' ');
                    object obj = array.GetValue(i);
                    builder.Append(elementToString(obj));
                },

                (d, i) =>
                {
                    if (d != 0)
                    {
                        if(i[d - 1] != 0) builder.Append(',');
                        builder.Append('\n');
                        while(d != 0)
                        {
                            builder.Append('\t');
                            d--;
                        }
                    }
                    builder.Append('{');
                },

                (d, i) =>
                {
                    if (d + 1 == array.Rank) builder.Append(" }");
                    else
                    {
                        builder.Append('\n');
                        while (d != 0)
                        {
                            builder.Append('\t');
                            d--;
                        }
                        builder.Append('}');
                    }
                });

            return builder.ToString();
        }


        public static bool TryGetKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value, out TKey key)
        {
            var comparer = EqualityComparer<TValue>.Default;
            foreach (var kvp in dictionary)
            {
                if (comparer.Equals(value, kvp.Value))
                {
                    key = kvp.Key;
                    return true;
                }
            }
            key = default;
            return false;
        }

    } // class Extensions

} // namespace UnityExtensions