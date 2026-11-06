using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityExtensions.Tween
{
    [System.Serializable]
    public abstract class TweenVector3<TTarget> : TweenFromTo<Vector3, TTarget> where TTarget : Object
    {
        public bool3 toggle;

        public override Vector3 InterpolateFromTo(float factor)
        {
            return (to - from) * factor + from;
        }

        public sealed override void SetCurrentState(TTarget target, Vector3 value)
        {
            if (toggle.anyTrue)
            {
                if (!toggle.allTrue)
                {
                    var t = GetCurrentState(target);

                    if (!toggle.x) value.x = t.x;
                    if (!toggle.y) value.y = t.y;
                    if (!toggle.z) value.z = t.z;
                }

                SetCurrentStateRaw(target, value);
            }
        }

        public abstract void SetCurrentStateRaw(TTarget target, Vector3 value);

#if UNITY_EDITOR

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            toggle = default;
        }


        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            base.OnPropertiesGUI(player, property);

            var (fromProp, toProp) = GetFromToProperties(property);
            var toggleProp = property.FindPropertyRelative(nameof(toggle));

            FromToFieldLayout("X",
                fromProp.FindPropertyRelative(nameof(Vector3.x)),
                toProp.FindPropertyRelative(nameof(Vector3.x)),
                toggleProp.FindPropertyRelative(nameof(bool3.x)));
            FromToFieldLayout("Y",
                fromProp.FindPropertyRelative(nameof(Vector3.y)),
                toProp.FindPropertyRelative(nameof(Vector3.y)),
                toggleProp.FindPropertyRelative(nameof(bool3.y)));
            FromToFieldLayout("Z",
                fromProp.FindPropertyRelative(nameof(Vector3.z)),
                toProp.FindPropertyRelative(nameof(Vector3.z)),
                toggleProp.FindPropertyRelative(nameof(bool3.z)));
        }

#endif // UNITY_EDITOR

    } // class TweenVector3<TTarget>

} // namespace UnityExtensions.Tween