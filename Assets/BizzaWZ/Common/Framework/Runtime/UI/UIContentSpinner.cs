using UnityEngine;
using UnityEngine.UI;

/// <summary>A small local loading indicator, with no external texture or material dependency.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class UIContentSpinner : MaskableGraphic
{
    private void Update() => rectTransform.Rotate(0, 0, -240f * Time.unscaledDeltaTime);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float outer = Mathf.Min(rect.width, rect.height) * .5f;
        float inner = outer * .72f;
        const int segments = 36;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 1.6f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Color tint = color;
            tint.a *= Mathf.Lerp(.15f, 1f, i / (float)segments);
            vh.AddVert(rect.center + direction * inner, tint, Vector2.zero);
            vh.AddVert(rect.center + direction * outer, tint, Vector2.zero);
            if (i == 0) continue;
            int index = i * 2;
            vh.AddTriangle(index - 2, index - 1, index);
            vh.AddTriangle(index, index - 1, index + 1);
        }
    }
}
