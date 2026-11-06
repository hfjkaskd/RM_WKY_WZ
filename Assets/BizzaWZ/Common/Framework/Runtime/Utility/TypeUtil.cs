using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

    public static class TypeUtil
    {
        public static List<Type> SearchAllDerive(Type type)
        {
            List<Type> ret = new List<Type>();
            foreach (Type t in Assembly.GetAssembly(type).GetTypes())
            {
                if (!t.IsAbstract && type.IsAssignableFrom(t))
                {
                    ret.Add(t);
                }
            }

            return ret;
        }

        public static List<Type> SearchAllDerive(Type type, ReadOnlySpan<Assembly> assemblies,
            List<Type> outList = null)
        {
            outList ??= new List<Type>();
            foreach (var assembly in assemblies)
            {
                foreach (Type t in assembly.GetTypes())
                {
                    if (!t.IsAbstract && type.IsAssignableFrom(t))
                    {
                        outList.Add(t);
                    }
                }
            }

            return outList;
        }

        public static List<Type> SearchAllDeriveByInterface(Type type)
        {
            List<Type> ret = new List<Type>();
            foreach (Type t in Assembly.GetCallingAssembly().GetTypes())
            {
                foreach (Type item in t.GetInterfaces())
                {
                    if (item == type)
                    {
                        ret.Add(t);
                    }
                }
            }

            return ret;
        }

        public static string GetName(Type type)
        {
            if (type.IsGenericType)
            {
                return type.Name[..type.Name.LastIndexOf('`')];
            }

            return type.Name;
        }

        public static string GetName<T>()
        {
            Type type = typeof(T);
            if (type.IsGenericType)
            {
                return type.Name[..type.Name.LastIndexOf('`')];
            }

            return type.Name;
        }

        public static string GetName<T>(T obj)
        {
            return GetName(obj.GetType());
        }
        //
        // public static MapListSet<string> AddTypeNameList(ReadOnlySpan<Assembly> assemblies, Type type,
        //     MapListSet<string> ret = null)
        // {
        //     ret ??= new MapListSet<string>();
        //     if (type.IsGenericType && type.ContainsGenericParameters && type.IsGenericTypeDefinition)
        //     {
        //         foreach (var assembly in assemblies)
        //         {
        //             foreach (Type t in assembly.GetTypes())
        //             {
        //                 foreach (Type _t in t.GetInterfaces())
        //                 {
        //                     if (_t.IsGenericType && _t.MetadataToken == type.MetadataToken)
        //                     {
        //                         ret.TryAdd(GetName(t));
        //                         break;
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //
        //     return ret;
        // }

        public static List<TTarget> CreateInstances<TTarget, TData>(Assembly assembly, Type genericType,
            List<TTarget> ret = null)
            where TTarget : class
        {
            ret ??= new List<TTarget>();

            Type targetType = typeof(TTarget);

            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsAbstract)
                {
                    continue;
                }

                if (type.IsGenericType && type.ContainsGenericParameters && type.IsGenericTypeDefinition)
                {
                    foreach (Type t in type.GetInterfaces())
                    {
                        if (t.IsGenericType && t.GetGenericTypeDefinition() == genericType)
                        {
                            Type final = type.MakeGenericType(typeof(TData));
                            if (Activator.CreateInstance(final) is TTarget target)
                            {
                                ret.Add(target);
                            }

                            break;
                        }
                    }
                }
                else
                {
                    foreach (Type t in type.GetInterfaces())
                    {
                        if (t == targetType)
                        {
                            if (Activator.CreateInstance(type) is TTarget target)
                            {
                                ret.Add(target);
                            }

                            break;
                        }
                    }
                }
                // else if (type.IsAssignableFrom(targetType)
                //                               && Activator.CreateInstance(type) is TTarget target)
                //     ret.Add(target);
            }

            return ret;
        }
    }
