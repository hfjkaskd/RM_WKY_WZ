using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions
{
    [System.Serializable]
    public struct PolarVector2
    {
        public float rotation;
        public float magnitude;

        public static implicit operator Vector2(PolarVector2 p)
            => MathUtils.EulerAngleToDirection(p.rotation) * p.magnitude;
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PolarVector2))]
    class PolarVector2Drawer : PropertyDrawer
    {
        static GUIContent[] _subLabels = new GUIContent[]
        {
                new GUIContent("R"),
                new GUIContent("M"),
        };

        static float[] _values = new float[2];

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var r = property.FindPropertyRelative("rotation");
            var m = property.FindPropertyRelative("magnitude");

            _values[0] = r.floatValue;
            _values[1] = m.floatValue;

            using (var scope = ChangeCheckScope.New())
            {
                using (WideModeScope.New(true))
                {
                    EditorGUI.MultiFloatField(position, label, _subLabels, _values);
                    if (scope.changed)
                    {
                        r.floatValue = _values[0];
                        m.floatValue = _values[1];
                    }
                }
            }
        }
    }

#endif
}
