using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions.Tween
{
    [System.Serializable]
    public abstract class TweenColor<TTarget> : TweenFromTo<Color, TTarget> where TTarget : Object
    {
        [ToggleButton("Working Mode", "Gradient", "From-To")]
        public bool useGradient;
        public Gradient gradient;
        public bool toggleRGB;
        public bool toggleAlpha;


        public override Color InterpolateFromTo(float factor)
        {
            if (useGradient)
            {
                return gradient.Evaluate(factor);
            }
            else
            {
                return (to - from) * factor + from;
            }
        }

        public sealed override void SetCurrentState(TTarget target, Color value)
        {
            if (toggleRGB || toggleAlpha)
            {
                if (!toggleRGB || !toggleAlpha)
                {
                    var t = GetCurrentState(target);

                    if (!toggleRGB)
                    {
                        value.r = t.r;
                        value.g = t.g;
                        value.b = t.b;
                    }
                    if (!toggleAlpha)
                    {
                        value.a = t.a;
                    }
                }

                SetCurrentStateRaw(target, value);
            }
        }

        public abstract void SetCurrentStateRaw(TTarget target, Color value);


#if UNITY_EDITOR

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);

            useGradient = false;
            gradient = null;
            toggleRGB = false;
            toggleAlpha = false;
        }


        protected virtual bool hdr => false;


        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            base.OnPropertiesGUI(player, property);

            EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(useGradient)));

            var toggleRGBProp = property.FindPropertyRelative(nameof(toggleRGB));
            var toggleAlphaProp = property.FindPropertyRelative(nameof(toggleAlpha));

            if (useGradient)
            {
                var rect = EditorGUILayout.GetControlRect();
                using (DisabledScope.New(!toggleRGB && !toggleAlpha))
                {
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(nameof(gradient)), EditorGUIUtils.TempContent(" "));
                }

                rect.width = rect.height + EditorStyles.label.CalcSize(EditorGUIUtils.TempContent("RGB")).x;
                toggleRGBProp.boolValue = EditorGUI.ToggleLeft(rect, "RGB", toggleRGBProp.boolValue);

                rect.x = rect.xMax + rect.height / 2;
                rect.width = rect.height + EditorStyles.label.CalcSize(EditorGUIUtils.TempContent("A")).x;
                toggleAlphaProp.boolValue = EditorGUI.ToggleLeft(rect, "A", toggleAlphaProp.boolValue);
            }
            else
            {
                var rect = EditorGUILayout.GetControlRect();
                float labelWidth = EditorGUIUtility.labelWidth;

                var fromRect = new Rect(rect.x + labelWidth, rect.y, (rect.width - labelWidth - 8) / 2, rect.height);
                var toRect = new Rect(rect.xMax - fromRect.width, fromRect.y, fromRect.width, fromRect.height);
                rect.width = labelWidth - 8;

                toggleRGBProp.boolValue = EditorGUI.ToggleLeft(rect, "RGB", toggleRGBProp.boolValue);

                var (fromProp, toProp) = GetFromToProperties(property);

                using (DisabledScope.New(!toggleRGB))
                {
                    using (LabelWidthScope.New(12))
                    {
                        fromProp.colorValue = EditorGUI.ColorField(fromRect, EditorGUIUtils.TempContent("F"), fromProp.colorValue, false, false, hdr);
                        toProp.colorValue = EditorGUI.ColorField(toRect, EditorGUIUtils.TempContent("T"), toProp.colorValue, false, false, hdr);
                    }
                }

                FromToFieldLayout("A",
                    fromProp.FindPropertyRelative(nameof(Color.a)),
                    toProp.FindPropertyRelative(nameof(Color.a)),
                    property.FindPropertyRelative(nameof(toggleAlpha)),
                    0, 1);
            }
        }

#endif // UNITY_EDITOR

    } // class TweenColor<TTarget>

} // namespace UnityExtensions.Tween