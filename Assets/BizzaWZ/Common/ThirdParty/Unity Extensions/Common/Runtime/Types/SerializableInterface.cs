using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    [System.Serializable]
    public struct SerializableInterface<T> where T : class
    {
        [SerializeField]
        Object _object;

        public T value
        {
            get => _object is T result ? result : null;
            set => _object = (Object)(object)value;
        }

        public static implicit operator T(SerializableInterface<T> a)
            => (T)(object)a._object;

        public static implicit operator SerializableInterface<T>(T a)
            => new SerializableInterface<T> { _object = (Object)(object)a };
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(SerializableInterface<>), true)]
    class SerializedInterfaceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property = property.FindPropertyRelative("_object");

            EditorGUI.BeginChangeCheck();

            var result = EditorGUI.ObjectField(
                position,
                label,
                property.objectReferenceValue,
                typeof(Object),
                true);

            if (EditorGUI.EndChangeCheck())
            {
                System.Type type = fieldInfo.FieldType;
                if (Editor.EditorUtils.IsArrayOrList(type)) type = Editor.EditorUtils.GetArrayOrListElementType(type);
                
                type = type.GetGenericArguments()[0];

                if (result)
                {
                    if (result is GameObject go)
                    {
                        result = go.GetComponent(type);
                        if (result) property.objectReferenceValue = result;
                    }
                    else if (result is Component cp)
                    {
                        result = cp.GetComponent(type);
                        if (result) property.objectReferenceValue = result;
                    }
                    else if (type.IsAssignableFrom(result.GetType()))
                    {
                        property.objectReferenceValue = result;
                    }
                }
                else property.objectReferenceValue = null;
            }
        }
    }

#endif
}