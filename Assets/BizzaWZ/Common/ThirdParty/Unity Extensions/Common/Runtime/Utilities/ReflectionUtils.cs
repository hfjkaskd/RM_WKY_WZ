using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace UnityExtensions
{
    /// <summary>
    /// Extensions for Reflection.
    /// </summary>
    public static class ReflectionUtils
    {
        //static IEnumerable<Type> _allTypes;

        ///// <summary>
        ///// allTypes
        ///// </summary>
        //public static IEnumerable<Type> allTypes
        //{
        //    get
        //    {
        //        if (_allTypes == null)
        //        {
        //            _allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(
        //                a => a.GetTypesSafely());
        //        }
        //        return _allTypes;
        //    }
        //}

        /// <summary>
        /// Get other type in the same assembly, fullName contains name of namespace.
        /// </summary>
        public static Type GetOtherTypeInSameAssembly(this Type type, string fullName)
        {
            return type.Assembly.GetType(fullName, false, false);
        }

        public static Type[] GetTypesSafely(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        #region Instance Reflection

        public static FieldInfo GetPublicInstanceField(this Type type, string name)
            => type.GetField(name, BindingFlags.Instance | BindingFlags.Public);

        public static FieldInfo GetNonPublicInstanceField(this Type type, string name)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            do
            {
                var result = type.GetField(name, bindingFlags);
                if (result != null) return result;
                type = type.BaseType;
            }
            while (type != null);
            return null;
        }

        public static FieldInfo GetInstanceField(this Type type, string name)
            => type.GetNonPublicInstanceField(name) ?? type.GetPublicInstanceField(name);

        public static PropertyInfo GetPublicInstanceProperty(this Type type, string name)
            => type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

        public static PropertyInfo GetNonPublicInstanceProperty(this Type type, string name)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            do
            {
                var result = type.GetProperty(name, bindingFlags);
                if (result != null) return result;
                type = type.BaseType;
            }
            while (type != null);
            return null;
        }

        public static PropertyInfo GetInstanceProperty(this Type type, string name)
            => type.GetNonPublicInstanceProperty(name) ?? type.GetPublicInstanceProperty(name);

        public static MethodInfo GetPublicInstanceMethod(this Type type, string name)
            => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);

        public static MethodInfo GetNonPublicInstanceMethod(this Type type, string name)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            do
            {
                var result = type.GetMethod(name, bindingFlags);
                if (result != null) return result;
                type = type.BaseType;
            }
            while (type != null);
            return null;
        }

        public static MethodInfo GetInstanceMethod(this Type type, string name)
            => type.GetNonPublicInstanceMethod(name) ?? type.GetPublicInstanceMethod(name);

        /// <summary>
        /// Set instance field value.
        /// </summary>
        public static void SetFieldValue(this object instance, string fieldName, object value)
        {
            instance.GetType().GetInstanceField(fieldName).SetValue(instance, value);
        }

        /// <summary>
        /// Get instance field value.
        /// </summary>
        public static object GetFieldValue(this object instance, string fieldName)
        {
            return instance.GetType().GetInstanceField(fieldName).GetValue(instance);
        }

        /// <summary>
        /// Set instance property value.
        /// </summary>
        public static void SetPropertyValue(this object instance, string propertyName, object value)
        {
            instance.GetType().GetInstanceProperty(propertyName).SetValue(instance, value);
        }

        /// <summary>
        /// Get instance property value.
        /// </summary>
        public static object GetPropertyValue(this object instance, string propertyName)
        {
            return instance.GetType().GetInstanceProperty(propertyName).GetValue(instance);
        }

        /// <summary>
        /// Invoke an instance method.
        /// </summary>
        public static object InvokeMethod(this object instance, string methodName, params object[] parameters)
        {
            return instance.GetType().GetInstanceMethod(methodName).Invoke(instance, parameters);
        }

        /// <summary>
        /// 按定义的顺序遍历所有实例字段
        /// </summary>
        public static IEnumerable<FieldInfo> GetInstanceFields(this Type type, Type baseTypeExclusive = null)
        {
            using (StackPool<Type>.global.Spawn(out var typeStack))
            {
                do
                {
                    typeStack.Push(type);
                    type = type.BaseType;
                }
                while (type != baseTypeExclusive);

                var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                while (typeStack.Count > 0)
                {
                    foreach (var field in typeStack.Pop().GetFields(bindingFlags))
                        yield return field;
                }
            }
        }

        #endregion Instance Reflection

        #region Static Reflection

        public static FieldInfo GetPublicStaticField(this Type type, string name)
            => type.GetField(name, BindingFlags.Static | BindingFlags.Public);

        public static FieldInfo GetNonPublicStaticField(this Type type, string name)
        {
            var bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            do
            {
                var result = type.GetField(name, bindingFlags);
                if (result != null) return result;
                type = type.BaseType;
            }
            while (type != null);
            return null;
        }

        public static FieldInfo GetStaticField(this Type type, string name)
            => type.GetNonPublicStaticField(name) ?? type.GetPublicStaticField(name);

        public static PropertyInfo GetPublicStaticProperty(this Type type, string name)
            => type.GetProperty(name, BindingFlags.Static | BindingFlags.Public);

        public static PropertyInfo GetNonPublicStaticProperty(this Type type, string name)
        {
            var bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            do
            {
                var result = type.GetProperty(name, bindingFlags);
                if (result != null) return result;
                type = type.BaseType;
            }
            while (type != null);
            return null;
        }

        public static PropertyInfo GetStaticProperty(this Type type, string name)
            => type.GetNonPublicStaticProperty(name) ?? type.GetPublicStaticProperty(name);

        public static MethodInfo GetPublicStaticMethod(this Type type, string name)
            => type.GetMethod(name, BindingFlags.Static | BindingFlags.Public);

        public static MethodInfo GetNonPublicStaticMethod(this Type type, string name)
        {
            var bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            do
            {
                var result = type.GetMethod(name, bindingFlags);
                if (result != null) return result;
                type = type.BaseType;
            }
            while (type != null);
            return null;
        }

        public static MethodInfo GetStaticMethod(this Type type, string name)
            => type.GetNonPublicStaticMethod(name) ?? type.GetPublicStaticMethod(name);

        /// <summary>
        /// Set static field value.
        /// </summary>
        public static void SetFieldValue(this Type type, string fieldName, object value)
        {
            type.GetStaticField(fieldName).SetValue(null, value);
        }

        /// <summary>
        /// Get static field value.
        /// </summary>
        public static object GetFieldValue(this Type type, string fieldName)
        {
            return type.GetStaticField(fieldName).GetValue(null);
        }

        /// <summary>
        /// Set static property value.
        /// </summary>
        public static void SetPropertyValue(this Type type, string propertyName, object value)
        {
            type.GetStaticProperty(propertyName).SetValue(null, value);
        }

        /// <summary>
        /// Get static property value.
        /// </summary>
        public static object GetPropertyValue(this Type type, string propertyName)
        {
            return type.GetStaticProperty(propertyName).GetValue(null);
        }

        /// <summary>
        /// Invoke an static method.
        /// </summary>
        public static object InvokeMethod(this Type type, string methodName, params object[] parameters)
        {
            return type.GetStaticMethod(methodName).Invoke(null, parameters);
        }

        #endregion Static Reflection

    } // class Extensions

} // namespace UnityExtensions