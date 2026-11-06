using UnityEditor;
using UnityEngine;

namespace Bizza.FlyMoney.Editor
{
    [CustomPropertyDrawer(typeof(FlyMoneySettings))]
    public sealed class FlyMoneySettingsDrawer : PropertyDrawer
    {
        private static readonly string[] Fields =
        {
            nameof(FlyMoneySettings.rainCount), nameof(FlyMoneySettings.flyCount),
            nameof(FlyMoneySettings.duration), nameof(FlyMoneySettings.rainSize),
            nameof(FlyMoneySettings.flySize), nameof(FlyMoneySettings.scatterRadius),
            nameof(FlyMoneySettings.fallSpeed), nameof(FlyMoneySettings.trails),
            nameof(FlyMoneySettings.trailSegments), nameof(FlyMoneySettings.trailTime),
            nameof(FlyMoneySettings.trailWidth), nameof(FlyMoneySettings.trailColor)
        };

        private static readonly GUIContent[] Labels =
        {
            new GUIContent("散落图片数量", "每组动画中向下飘落、不飞向目标的背景图片数量。"),
            new GUIContent("飞向目标数量", "每组动画飞向目标的图片数量；金币与钞票各自计算，不影响奖励金额。"),
            new GUIContent("动画总时长（秒）"),
            new GUIContent("散落图片大小"),
            new GUIContent("飞行图片大小"),
            new GUIContent("爆开范围"),
            new GUIContent("自然下落速度", "飞向目标前的持续下落速度；0 表示关闭下落和轻微飘摆。"),
            new GUIContent("启用拖尾"),
            new GUIContent("拖尾分段数", "分段越多越平滑，绘制开销也越高。"),
            new GUIContent("拖尾长度（秒）", "拖尾保留的运动轨迹时长，越大越长。"),
            new GUIContent("拖尾宽度"),
            new GUIContent("通用拖尾颜色", "A 控制透明度。正式流程启用国家配色后，RGB 使用该国家的选择，A 仍使用这里的设置。")
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
                for (int i = 0; i < Fields.Length; i++)
                    height += EditorGUIUtility.standardVerticalSpacing +
                        EditorGUI.GetPropertyHeight(property.FindPropertyRelative(Fields[i]), Labels[i], true);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            int indent = EditorGUI.indentLevel;
            try
            {
                position.height = EditorGUIUtility.singleLineHeight;
                property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
                if (!property.isExpanded) return;
                EditorGUI.indentLevel = indent + 1;
                for (int i = 0; i < Fields.Length; i++)
                {
                    position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                    SerializedProperty child = property.FindPropertyRelative(Fields[i]);
                    position.height = EditorGUI.GetPropertyHeight(child, Labels[i], true);
                    EditorGUI.PropertyField(position, child, Labels[i], true);
                }
            }
            finally
            {
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }
        }
    }
}
