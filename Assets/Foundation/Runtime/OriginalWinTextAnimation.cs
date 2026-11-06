using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Whitebox.Foundation
{
    // CurvedTextAnimator 0x29d994c and 0x29d95ac. The outline layers share
    // one glyph clock and each retains its own authored TextMeshPro geometry.
    public sealed class OriginalWinTextAnimation : MonoBehaviour
    {
        [SerializeField] private TextMeshPro[] texts;
        [SerializeField] private AnimationCurve vertexCurve;
        [SerializeField] private float curveScale;
        [SerializeField] private int framesBetweenCharacters;
        [SerializeField] private float startScale;
        [SerializeField] private OriginalGoalCheckStage[] bounce;

        private TextLayer[] layers;
        private Character[] characters;
        private Coroutine animation;

        internal void Prepare()
        {
            if (animation != null)
            {
                StopCoroutine(animation);
                animation = null;
            }
            if (layers == null)
            {
                layers = new TextLayer[texts.Length];
                for (int i = 0; i < texts.Length; i++)
                    layers[i] = new TextLayer(this, texts[i]);
            }
            int count = 0;
            foreach (var layer in layers)
            {
                layer.Text.ForceMeshUpdate();
                count = Mathf.Max(count, layer.Text.textInfo.characterCount);
            }
            if (characters == null || characters.Length != count) characters = new Character[count];
            else Array.Clear(characters, 0, count);
            RefreshVertices();
        }

        internal void Play() => animation = StartCoroutine(AnimateCharacters());

        private IEnumerator AnimateCharacters()
        {
            int started = 0, completed = 0;
            int nextCharacter = Time.frameCount + framesBetweenCharacters;
            while (completed < characters.Length)
            {
                yield return null;
                if (started < characters.Length && Time.frameCount >= nextCharacter)
                {
                    started++;
                    nextCharacter = Time.frameCount + framesBetweenCharacters;
                }
                float delta = Time.deltaTime;
                for (int i = completed; i < started; i++)
                    if (characters[i].Advance(delta, startScale, bounce)) completed++;
                if (started > 0) RefreshVertices();
            }
            animation = null;
        }

        private void RefreshVertices()
        {
            foreach (var layer in layers)
            {
                layer.Apply(layer.Text.textInfo);
                layer.Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            }
        }

        private void OnDestroy()
        {
            if (layers == null) return;
            foreach (var layer in layers) layer.Release();
        }

        private struct Character
        {
            private int phase;
            private float elapsed;
            public float Scale { get; private set; }

            public bool Advance(float delta, float start, OriginalGoalCheckStage[] stages)
            {
                if (elapsed >= stages[phase].duration)
                {
                    phase++;
                    elapsed = 0f;
                    if (phase == stages.Length) return true;
                }
                var stage = stages[phase];
                elapsed += delta;
                float t = elapsed / stage.duration;
                if (phase == 0) t = Mathf.Sin(t * (Mathf.PI * .5f));
                else if (phase == 1) t = 1f - Mathf.Cos(t * (Mathf.PI * .5f));
                float from = phase == 0 ? start : stages[phase - 1].scale;
                Scale = Mathf.Lerp(from, stage.scale, t);
                return false;
            }
        }

        private sealed class TextLayer
        {
            private readonly OriginalWinTextAnimation owner;
            private Glyph[] glyphs = Array.Empty<Glyph>();
            public TextMeshPro Text { get; }

            public TextLayer(OriginalWinTextAnimation owner, TextMeshPro text)
            {
                this.owner = owner;
                Text = text;
                Text.OnPreRenderText += CaptureLayout;
            }

            public void Release()
            {
                if (Text != null) Text.OnPreRenderText -= CaptureLayout;
            }

            private void CaptureLayout(TMP_TextInfo info)
            {
                int count = info.characterCount;
                if (glyphs.Length != count) glyphs = new Glyph[count];
                OriginalTextCurve.GetHorizontalExtent(info, out float minimum, out float width);
                for (int i = 0; i < count; i++)
                {
                    var c = info.characterInfo[i];
                    if (!c.isVisible) { glyphs[i] = default; continue; }
                    var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                    int vertex = c.vertexIndex;
                    OriginalTextCurve.GetGlyphPose(owner.vertexCurve, owner.curveScale, minimum, width, c, vertices,
                        out var pivot, out var center, out var rotation);
                    glyphs[i] = new Glyph
                    {
                        vertices = vertices,
                        vertex = vertex,
                        center = center,
                        bottomLeft = rotation * (vertices[vertex] - pivot),
                        topLeft = rotation * (vertices[vertex + 1] - pivot),
                        topRight = rotation * (vertices[vertex + 2] - pivot),
                        bottomRight = rotation * (vertices[vertex + 3] - pivot)
                    };
                }
                Apply(info);
            }

            public void Apply(TMP_TextInfo info)
            {
                if (owner.characters == null) return;
                for (int i = 0; i < info.characterCount; i++)
                {
                    ref var glyph = ref glyphs[i];
                    if (glyph.vertices == null) continue;
                    float scale = i < owner.characters.Length ? owner.characters[i].Scale : 1f;
                    glyph.vertices[glyph.vertex] = glyph.center + glyph.bottomLeft * scale;
                    glyph.vertices[glyph.vertex + 1] = glyph.center + glyph.topLeft * scale;
                    glyph.vertices[glyph.vertex + 2] = glyph.center + glyph.topRight * scale;
                    glyph.vertices[glyph.vertex + 3] = glyph.center + glyph.bottomRight * scale;
                }
            }

            private struct Glyph
            {
                public Vector3[] vertices;
                public int vertex;
                public Vector3 center, bottomLeft, topLeft, topRight, bottomRight;
            }
        }
    }
}
