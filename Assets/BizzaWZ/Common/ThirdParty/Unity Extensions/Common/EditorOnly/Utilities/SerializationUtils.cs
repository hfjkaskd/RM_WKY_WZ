#if UNITY_EDITOR

using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;

using UObject = UnityEngine.Object;

namespace UnityExtensions.Editor
{
    public struct SubFieldID : IEquatable<SubFieldID>
    {
        public UObject root;
        public string path;

        public SubFieldID(UObject root, string path)
        {
            this.root = root;
            this.path = path;
        }

        public SubFieldID(SerializedProperty property)
        {
            this.root = property.serializedObject.targetObject;
            this.path = property.propertyPath;
        }

        public static bool operator ==(SubFieldID a, SubFieldID b)
        {
            return a.root == b.root && a.path == b.path;
        }

        public static bool operator !=(SubFieldID a, SubFieldID b)
        {
            return a.root != b.root || a.path != b.path;
        }

        public bool Equals(SubFieldID other)
        {
            return root == other.root && path == other.path;
        }

        public override bool Equals(object obj) => throw new Exception("What are you doing?");

        public override int GetHashCode() => root.GetHashCode() ^ path.GetHashCode();
    }


    public struct SubFieldInfo
    {
        public FieldInfo fieldInfo; // null means array/list
        public int listIndex;       // -1 means non array/list
        public Type actualType;

        public object GetValue(object parent)
        {
            return fieldInfo != null ? fieldInfo.GetValue(parent) : ((IList)parent)[listIndex];
        }

        public void SetValue(object parent, object value)
        {
            if (fieldInfo != null)
                fieldInfo.SetValue(parent, value);
            else
                ((IList)parent)[listIndex] = value;
        }
    }


    /// <summary>
    /// Extensions for serialization.
    /// </summary>
    public static class SerializationUtils
    {
        static Dictionary<SubFieldID, SubFieldInfo[]> _subFieldsDictionary = new Dictionary<SubFieldID, SubFieldInfo[]>(256);
        static Stack<object> _parentObjects = new Stack<object>(16);

        public static object GetValue(this SerializedProperty property)
        {
            object value = null;
            GetSetSerializedPropertyValue(
                false,
                property.serializedObject.targetObject,
                property.propertyPath,
                ref value);
            return value;
        }

        public static object GetParentValue(this SerializedProperty property, int parent = 1)
        {
            object value = null;
            GetSetSerializedPropertyValue(
                false,
                property.serializedObject.targetObject,
                property.propertyPath,
                ref value,
                parent);
            return value;
        }

        public static object GetValue(this SerializedProperty property, UObject targetObject)
        {
            object value = null;
            GetSetSerializedPropertyValue(
                false,
                targetObject,
                property.propertyPath,
                ref value);
            return value;
        }

        public static object GetParentValue(this SerializedProperty property, UObject targetObject, int parent = 1)
        {
            object value = null;
            GetSetSerializedPropertyValue(
                false,
                targetObject,
                property.propertyPath,
                ref value,
                parent);
            return value;
        }

        public static void SetValue(this SerializedProperty property, object value)
        {
            GetSetSerializedPropertyValue(
                true,
                property.serializedObject.targetObject,
                property.propertyPath,
                ref value);
        }

        public static void SetParentValue(this SerializedProperty property, object value)
        {
            GetSetSerializedPropertyValue(
                true,
                property.serializedObject.targetObject,
                property.propertyPath,
                ref value,
                1);
        }

        public static void SetValue(this SerializedProperty property, UObject targetObject, object value)
        {
            GetSetSerializedPropertyValue(
                true,
                targetObject,
                property.propertyPath,
                ref value);
        }

        public static void SetParentValue(this SerializedProperty property, UObject targetObject, object value)
        {
            GetSetSerializedPropertyValue(
                true,
                targetObject,
                property.propertyPath,
                ref value,
                1);
        }

        public static void SetMultipleValues(this SerializedProperty property, object value)
        {
            foreach (var targetObject in property.serializedObject.targetObjects)
            {
                GetSetSerializedPropertyValue(
                    true,
                    targetObject,
                    property.propertyPath,
                    ref value);
            }
        }

        public static int GetIndexInArray(this SerializedProperty property, out object array)
        {
            array = null;
            GetSetSerializedPropertyValue(
                false,
                property.serializedObject.targetObject,
                property.propertyPath,
                ref array,
                1);
            return _subFieldsDictionary[new SubFieldID(property.serializedObject.targetObject, property.propertyPath)].Last().listIndex;
        }

        public static object GetValueRelativeToTarget(this SerializedProperty property, int depth)
        {
            object result = null;
            GetSetSerializedPropertyValue(false, property.serializedObject.targetObject, property.propertyPath, ref result, depth, true);
            return result;
        }

        /// <summary>
        /// Supports [SerializeReference]
        /// </summary>
        /// <param name="root"> the unity object </param>
        /// <param name="path"> "a.b.Array.data[3].c" </param>
        /// <param name="depth"> 0-self, 1-parent, 2-grandparent, ... </param>
        /// <returns></returns>
        static void GetSetSerializedPropertyValue(bool set, object root, string path, ref object value, int depth = 0, bool depthRelativeToRoot = false)
        {
            SubFieldID id = new SubFieldID((UObject)root, path);
            if (set) _parentObjects.Clear();

            if (_subFieldsDictionary.TryGetValue(id, out var subFields))
            {
                if (!depthRelativeToRoot)
                    depth = subFields.Length - depth;

                for (int i = 0; i < depth; i++)
                {
                    if (set) _parentObjects.Push(root);

                    ref var subField = ref subFields[i];

                    root = subField.GetValue(root);

                    var type = root?.GetType();
                    if (type != subField.actualType)
                    {
                        subField.actualType = type;

                        // Field actual type changed, need update all sub-fields info
                        for (i++; i < depth; i++)
                        {
                            if (set) _parentObjects.Push(root);

                            subField = ref subFields[i];

                            if (subField.fieldInfo != null)
                                subField.fieldInfo = type.GetInstanceField(subField.fieldInfo.Name);

                            root = subField.GetValue(root);

                            subField.actualType = type = root?.GetType();
                        }

                        break;
                    }
                }

                if (!set) value = root;
            }
            else
            {
                var names = path.Replace("Array.data[", "[").Split('.');
                subFields = new SubFieldInfo[names.Length];
                var type = root.GetType();

                if (!depthRelativeToRoot)
                    depth = subFields.Length - depth;

                for (int i = 0; i < subFields.Length; i++)
                {
                    if (set)
                    {
                        if (i < depth) _parentObjects.Push(root);
                    }
                    else
                    {
                        if (i == depth) value = root;
                    }

                    ref var subField = ref subFields[i];
                    var name = names[i];

                    if (name[0] == '[')
                        subField.listIndex = int.Parse(name.Substring(1, name.Length - 2));
                    else
                    {
                        subField.fieldInfo = type.GetInstanceField(name);
                        subField.listIndex = -1;
                    }

                    root = subField.GetValue(root);

                    subField.actualType = type = root?.GetType();
                }

                _subFieldsDictionary.Add(id, subFields);

                if (!set && depth == subFields.Length) value = root;
            }

            if (set)
            {
                var fieldValue = value;
                for (int i = depth - 1; i >= 0; i--)
                {
                    root = _parentObjects.Pop();
                    subFields[i].SetValue(root, fieldValue);
                    fieldValue = root;
                }
            }
        }

        public static object GetSerializedPropertyValue(object root, string path, int parent = 0)
        {
            object value = null;
            GetSetSerializedPropertyValue(false, root, path, ref value, parent);
            return value;
        }

        public static void SetSerializedPropertyValue(object root, string path, object value, int parent = 0)
        {
            GetSetSerializedPropertyValue(true, root, path, ref value, parent);
        }

        public static bool IsSerializableForUnity(this Type type)
        {
            return !type.IsInterface && !type.IsAbstract && !type.IsGenericType && type.IsSerializable &&
                (type.IsValueType || type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null);
        }

        public static void FromJsonOverwriteSafely(string json, object target)
        {
            try { EditorJsonUtility.FromJsonOverwrite(json, target); } catch { }
        }

    } // class SerializationUtils

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR