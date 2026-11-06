using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace UnityExtensions
{
    /// <summary>
    /// RuntimeUtilities
    /// </summary>
    public static class RuntimeUtils
    {
        static int _lastTypeID = 0;

        struct TypeInfo<T>
        {
            public static readonly int id = ++_lastTypeID;
        }

        public static int TypeID<T>() => TypeInfo<T>.id;

        public static bool AreSame<T1, T2>() => TypeInfo<T1>.id == TypeInfo<T2>.id;

        public static void Swap<T>(ref T a, ref T b)
        {
            T c = a;
            a = b;
            b = c;
        }

        public static T2 CastStruct<T1, T2>(this T1 value) where T2 : struct
        {
            if (value is T2 value2) return value2;
            throw new InvalidCastException();
        }

        public static void CastStruct<T1, T2>(this T1 value, out T2 result) where T2 : struct
        {
            if (value is T2 value2) result = value2;
            throw new InvalidCastException();
        }

        public static T2 CastClass<T1, T2>(this T1 value) where T2 : class
        {
            return (T2)(object)value;
        }

        public static void CastClass<T1, T2>(this T1 value, out T2 result) where T2 : class
        {
            result = (T2)(object)value;
        }

        public static T2 Cast<T1, T2>(this T1 value)
        {
            return value is T2 value2 ? value2 : (T2)(object)value;
        }

        public static void Cast<T1, T2>(this T1 value, out T2 result)
        {
            result = value is T2 value2 ? value2 : (T2)(object)value;
        }

        /// <summary>
        /// No exception
        /// </summary>
        public static Type GetGenericTypeDefinitionSafely(this Type type)
        {
            return type.IsGenericType ? type.GetGenericTypeDefinition() : null;
        }

        /// <summary>
        /// Do not support interface
        /// </summary>
        public static Type GetSameBaseType(Type a, Type b)
        {
            if (a == null || b == null || a.IsInterface || b.IsInterface)
                return null;

            if (a.IsAssignableFrom(b))
                return a;

            while (!b.IsAssignableFrom(a))
                b = b.BaseType;

            return b;
        }

        public static bool IsAnyAssignableFrom(this IList<Type> types, Type otherType)
        {
            foreach (var t in types)
                if (t.IsAssignableFrom(otherType))
                    return true;

            return false;
        }

    } // struct RuntimeUtilities

} // namespace UnityExtensions