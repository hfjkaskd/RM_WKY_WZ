using TMPro;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCurvedDialogTitle : MonoBehaviour
    {
        [SerializeField] private TextMeshPro text;
        [SerializeField] private AnimationCurve vertexCurve;
        [SerializeField] private float curveScale;
        [SerializeField] private float twoDigitLevelOffset, threeDigitLevelOffset;
        private float levelOffset;

        // AbstractLevelDialog.PrepareTitle 0x11715b8.
        internal void SetLevelTitle(string value, int level)
        {
            levelOffset = level >= 100 ? threeDigitLevelOffset : level >= 10 ? twoDigitLevelOffset : 0f;
            text.text = value;
            text.havePropertiesChanged = true;
        }

        private void OnEnable()
        {
            text.OnPreRenderText += ApplyCurve;
            text.havePropertiesChanged = true;
        }

        private void OnDisable()
        {
            if (text != null) text.OnPreRenderText -= ApplyCurve;
        }

        private void ApplyCurve(TMP_TextInfo info)
        {
            if (text.text.Length <= 3) return;
            OriginalTextCurve.Apply(info, vertexCurve, curveScale + levelOffset);
        }
    }

    internal static class OriginalTextCurve
    {
        public static void Apply(TMP_TextInfo info, AnimationCurve curve, float scale)
        {
            GetHorizontalExtent(info, out float minimum, out float width);
            for (int i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible) continue;
                var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                int vertex = character.vertexIndex;
                GetGlyphPose(curve, scale, minimum, width,
                    character, vertices, out var pivot, out var center, out var rotation);
                for (int j = vertex; j < vertex + 4; j++)
                    vertices[j] = center + rotation * (vertices[j] - pivot);
            }
        }

        public static void GetHorizontalExtent(TMP_TextInfo info, out float minimum, out float width)
        {
            minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            for (int i = 0; i < info.characterCount; i++)
            {
                var character = info.characterInfo[i];
                if (!character.isVisible) continue;
                var vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                minimum = Mathf.Min(minimum, vertices[character.vertexIndex].x);
                maximum = Mathf.Max(maximum, vertices[character.vertexIndex + 2].x);
            }
            width = maximum - minimum;
        }

        // TextProOnAnAnimationCurve 0x29dc4a4. Apply the arc and tangent
        // around the original midpoint on the baseline.
        public static void GetGlyphPose(AnimationCurve curve, float scale, float minimum, float width,
            TMP_CharacterInfo character, Vector3[] vertices, out Vector3 pivot, out Vector3 center, out Quaternion rotation)
        {
            int vertex = character.vertexIndex;
            pivot = new Vector3((vertices[vertex].x + vertices[vertex + 2].x) * .5f, character.baseLine, 0f);
            float t = width > 0f ? (pivot.x - minimum) / width : .5f;
            const float tangentStep = .0001f;
            float y = curve.Evaluate(t) * scale;
            float nextY = curve.Evaluate(t + tangentStep) * scale;
            float angle = width > 0f ? Mathf.Atan2(nextY - y, width * tangentStep) * Mathf.Rad2Deg : 0f;
            center = pivot + Vector3.up * y;
            rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
