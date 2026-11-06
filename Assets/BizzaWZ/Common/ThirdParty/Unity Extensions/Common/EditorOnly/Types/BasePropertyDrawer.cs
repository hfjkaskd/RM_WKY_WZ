#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;

namespace UnityExtensions.Editor
{
    public class BasePropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// BasePropertyDrawer
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            using (WideModeScope.New(true))
                return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (WideModeScope.New(true))
                EditorGUI.PropertyField(position, property, label, true);
        }

        public Type GetFieldOrElementType()
        {
            var type = fieldInfo.FieldType;
            if (EditorUtils.IsArrayOrList(type)) type = EditorUtils.GetArrayOrListElementType(type);
            return type;
        }

    } // BasePropertyDrawer

    /// <summary>
    /// BasePropertyDrawer<T>
    /// </summary>
    public class BasePropertyDrawer<T> : BasePropertyDrawer where T : PropertyAttribute
    {
        protected new T attribute => (T)base.attribute;

    } // class BasePropertyDrawer<T>

} // namespace UnityExtensions.Editor

#endif // UNITY_EDITOR