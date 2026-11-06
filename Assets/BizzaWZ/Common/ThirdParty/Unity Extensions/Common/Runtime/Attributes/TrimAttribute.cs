using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    /// <summary>
    /// Remove white space characters at start & end of string
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class TrimAttribute : PropertyAttribute
    {
        bool _delayed;

        /// <summary>
        /// Remove white space characters at start & end of string
        /// </summary>
        public TrimAttribute(bool delayed = true)
        {
            _delayed = delayed;
        }

#if UNITY_EDITOR

        float _min;
        float _max;

        [CustomPropertyDrawer(typeof(TrimAttribute))]
        class TrimDrawer : BasePropertyDrawer<TrimAttribute>
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.String:
                        using (MixedValueScope.New(property.hasMultipleDifferentValues))
                        {
                            using (var scope = ChangeCheckScope.New())
                            {
                                string newValue;

                                if (attribute._delayed)
                                    newValue = EditorGUI.DelayedTextField(position, label, property.stringValue);
                                else
                                    newValue = EditorGUI.TextField(position, label, property.stringValue);

                                if (scope.changed)
                                    property.stringValue = newValue?.Trim();
                            }
                            break;
                        }
                    
                    default:
                        {
                            EditorGUI.LabelField(position, label.text, "Use TrimAttribute with string field.");
                            break;
                        }
                }
            }

        } // class TrimDrawer

#endif // UNITY_EDITOR

    } // class TrimAttribute

} // namespace UnityExtensions