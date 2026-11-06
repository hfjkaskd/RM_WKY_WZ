using System;
using System.Reflection;
using System.Linq;
using UnityEngine;

public static class AttributeExtension
{
    public static T GetCustomAttribute<T>(this Type type) where T : Attribute
    {
        return type.GetCustomAttribute<T>(inherit: false);
    }

    public static T GetCustomAttribute<T>(this Type type, bool inherit) where T : Attribute
    {
        object[] customAttributes = type.GetCustomAttributes(typeof(T), inherit);
        if (customAttributes.Length == 0)
        {
            return null;
        }

        return customAttributes[0] as T;
    }

    /// <summary>
    /// 获取枚举上的Attribute信息扩展方法
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <param name="e"></param>
    /// <param name="inherit"></param>
    /// <returns></returns>
    public static TAttribute GetCustomAttributeEx<TAttribute>(this Enum e, bool inherit = false)
    {
        return GetCustomAttribute<TAttribute>(e, inherit);
    }

    /// <summary>
    /// 获取枚举上的Attribute信息
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <param name="e"></param>
    /// <param name="inherit"></param>
    /// <returns></returns>
    public static TAttribute GetCustomAttribute<TAttribute>(Enum e, bool inherit = false)
    {
        var o = e.GetType().GetMember(e.ToString())
            ?.FirstOrDefault()
            ?.GetCustomAttributes(typeof(TAttribute), inherit)
            ?.FirstOrDefault();

        if (o is TAttribute a)
        {
            return a;
        }

        return default;
    }

}
