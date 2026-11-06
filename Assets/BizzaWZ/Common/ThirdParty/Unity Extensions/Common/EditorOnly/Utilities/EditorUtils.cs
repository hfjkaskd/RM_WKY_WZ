#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace UnityExtensions.Editor
{
    /// <summary>
    /// Utilities for editor.
    /// </summary>
    public static class EditorUtils
    {
        static double _lastTimeSinceStartup = -0.01;
        public static float deltaTime { get; private set; }

        static MethodInfo _stringFromPropertyNameMethod;
        static Dictionary<Type, string> _displayNames = new Dictionary<Type, string>();
        static Dictionary<Type, string> _categoryPaths = new Dictionary<Type, string>();

        public static event Action editModeUpdate;
        public static event Action delayCall;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update += () =>
            {
                deltaTime = (float)(EditorApplication.timeSinceStartup - _lastTimeSinceStartup);
                _lastTimeSinceStartup = EditorApplication.timeSinceStartup;

                if (!EditorApplication.isPlaying) editModeUpdate?.Invoke();

                if (delayCall != null)
                {
                    var temp = delayCall;
                    delayCall = null;
                    temp.Invoke();
                }
            };
        }


        public static PlayModeStateChange playMode
        {
            get
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (EditorApplication.isPlaying) return PlayModeStateChange.EnteredPlayMode;
                    else return PlayModeStateChange.ExitingEditMode;
                }
                else
                {
                    if (EditorApplication.isPlaying) return PlayModeStateChange.ExitingPlayMode;
                    else return PlayModeStateChange.EnteredEditMode;
                }
            }
        }

        public static bool IsArrayOrList(Type type)
        {
            return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
        }

        public static Type GetArrayOrListElementType(Type type)
        {
            return type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
        }

        public static IEnumerable<TObject> GetCorrespondingObjectsFromSources<TObject>(TObject obj) where TObject : UnityEngine.Object
        {
            while (obj)
            {
                yield return obj;
                obj = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            }
        }

        public static string StringFromPropertyName(PropertyName propertyName)
        {
            if (_stringFromPropertyNameMethod == null)
            {
                var propertyNameUtilsType = typeof(PropertyName).GetOtherTypeInSameAssembly("UnityEngine.PropertyNameUtils");
                _stringFromPropertyNameMethod = propertyNameUtilsType.GetStaticMethod("StringFromPropertyName");
            }

            using var _ = ArrayPool<object>.global1.Spawn(out var array);
            array[0] = propertyName;
            return (string)_stringFromPropertyNameMethod.Invoke(null, array);
        }

        public static string GetCallerFilePath([CallerFilePath] string filePath = "") => filePath;
        public static string GetCallerMemberName([CallerMemberName] string memberName = "") => memberName;
        public static int GetCallerLineNumber([CallerLineNumber] int lineNumber = 0) => lineNumber;

    } // struct EditorUtilities

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR