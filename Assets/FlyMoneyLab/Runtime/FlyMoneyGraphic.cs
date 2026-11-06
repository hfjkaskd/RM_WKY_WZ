using UnityEngine;
using UnityEngine.UI;

namespace Bizza.FlyMoney
{
    internal sealed class FlyMoneySprite
    {
        public Sprite sprite;
        public Texture texture;
        public Vector2[] vertices;
        public Vector2[] uv;
        public ushort[] triangles;

        public static FlyMoneySprite Prepare(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return null;
            Vector2[] vertices = sprite.vertices;
            Vector2[] uv = sprite.uv;
            ushort[] triangles = sprite.triangles;
            // This cap keeps 64 sprites safely inside UGUI's 65k vertex limit.
            if (vertices.Length == 0 || vertices.Length > 256 || uv.Length != vertices.Length ||
                triangles.Length == 0 || triangles.Length > 1536 || triangles.Length % 3 != 0) return null;
            for (int i = 0; i < triangles.Length; i++)
                if (triangles[i] >= vertices.Length) return null;
            float dimension = Mathf.Max(sprite.rect.width, sprite.rect.height);
            if (!FlyMoneySettings.Finite(dimension) || dimension <= 0f) return null;
            Vector2 pivotOffset = sprite.pivot - sprite.rect.size * 0.5f;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = (vertices[i] * sprite.pixelsPerUnit + pivotOffset) / dimension;
                if (!FlyMoneySettings.Finite(vertices[i]) || !FlyMoneySettings.Finite(uv[i])) return null;
            }
            return new FlyMoneySprite { sprite = sprite, texture = sprite.texture, vertices = vertices, uv = uv, triangles = triangles };
        }
    }

    [AddComponentMenu("")]
    public sealed class FlyMoneyGraphic : MaskableGraphic
    {
        private static readonly Vector2[] CloudContour = CreateCloudContour();
        internal FlyMoneyPlayer.Slot slot;
        internal int layer;
        internal FlyMoneySprite geometry;
        public int VertexCount { get; private set; }
        public override Texture mainTexture => geometry != null && geometry.texture != null ? geometry.texture : Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            VertexCount = 0;
            if (slot == null || !slot.active) return;
            if (layer == 1) { DrawBurst(helper); DrawTrails(helper); }
            else if (geometry != null && geometry.texture != null)
            {
                FlyMoneyFrame[] frames = layer == 0 ? slot.rainFrames : slot.flyFrames;
                int count = layer == 0 ? slot.simulation.Settings.rainCount : slot.simulation.Settings.flyCount;
                for (int i = 0; i < count; i++)
                    if (frames[i].Visible) DrawSprite(helper, frames[i]);
            }
            VertexCount = helper.currentVertCount;
        }

        private void DrawSprite(VertexHelper helper, FlyMoneyFrame frame)
        {
            int first = helper.currentVertCount;
            float radians = frame.angle * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians) * frame.size;
            float sine = Mathf.Sin(radians) * frame.size;
            Color32 tint = new Color(1f, 1f, 1f, frame.alpha);
            for (int i = 0; i < geometry.vertices.Length; i++)
            {
                Vector2 point = geometry.vertices[i];
                Vector2 position = frame.position + new Vector2(point.x * cosine - point.y * sine, point.x * sine + point.y * cosine);
                helper.AddVert(position, tint, geometry.uv[i]);
            }
            for (int i = 0; i < geometry.triangles.Length; i += 3)
                helper.AddTriangle(first + geometry.triangles[i], first + geometry.triangles[i + 1], first + geometry.triangles[i + 2]);
        }

        private void DrawTrails(VertexHelper helper)
        {
            FlyMoneySettings settings = slot.simulation.Settings;
            if (!settings.trails) return;
            float tailTime = settings.trailTime * settings.duration / 1.7f;
            for (int i = 0; i < settings.flyCount; i++)
            {
                FlyMoneyFrame head = slot.flyFrames[i];
                if (!head.Visible || !head.flying) continue;
                Vector2 previous = head.position;
                for (int segment = 1; segment <= settings.trailSegments; segment++)
                {
                    float end = segment / (float)settings.trailSegments;
                    FlyMoneyFrame tail = slot.simulation.SampleFly(i, slot.elapsed - tailTime * end);
                    if (!tail.Visible || !tail.flying) break;
                    Vector2 delta = tail.position - previous;
                    if (delta.sqrMagnitude < 0.01f) continue;
                    float begin = (segment - 1f) / settings.trailSegments;
                    Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
                    float width = settings.trailWidth * head.size / Mathf.Max(1f, settings.flySize) * 0.5f;
                    Vector2 firstWidth = normal * width * (1f - begin);
                    Vector2 lastWidth = normal * width * (1f - end);
                    Color firstColor = settings.trailColor;
                    Color lastColor = firstColor;
                    firstColor.a *= (1f - begin) * head.alpha;
                    lastColor.a *= (1f - end) * head.alpha;
                    int first = helper.currentVertCount;
                    helper.AddVert(previous - firstWidth, firstColor, Vector2.zero);
                    helper.AddVert(previous + firstWidth, firstColor, Vector2.zero);
                    helper.AddVert(tail.position + lastWidth, lastColor, Vector2.zero);
                    helper.AddVert(tail.position - lastWidth, lastColor, Vector2.zero);
                    helper.AddTriangle(first, first + 1, first + 2);
                    helper.AddTriangle(first + 2, first + 3, first);
                    previous = tail.position;
                }
            }
        }

        // Cloud, rays and stars share the existing white-texture trail batch: at most 506 vertices.
        private void DrawBurst(VertexHelper helper)
        {
            FlyMoneyBurstFrame burst = slot.simulation.SampleBurst(slot.elapsed);
            if (!burst.Visible) return;
            Vector2 origin = slot.simulation.Origin;
            for (int i = 0; i < 4 && burst.cloudAlpha > 0.001f; i++)
            {
                float angle = (i + 0.38f) * Mathf.PI * 0.5f;
                Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.8f);
                Vector2 point = origin + radial * burst.cloudSpread;
                float size = burst.cloudSize * (i % 2 == 0 ? 0.56f : 0.43f);
                DrawCloud(helper, point, size, burst.cloudSquash, burst.cloudAlpha, i * 0.7f, 24);
            }
            if (burst.coreAlpha > 0.001f)
                DrawCloud(helper, origin, burst.cloudSize, burst.cloudSquash * 0.9f, burst.coreAlpha, 0.25f, 48);
            for (int i = 0; i < 6 && burst.rayAlpha > 0.001f; i++)
            {
                float angle = (i + 0.2f) * Mathf.PI * 2f / 6f;
                Vector2 radial = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 normal = new Vector2(-radial.y, radial.x);
                float radius = burst.radius * (i % 2 == 0 ? 1f : 0.88f);
                Vector2 waist = origin + radial * radius * 0.86f;
                Vector2 width = normal * 2.6f * burst.scale;
                Color tint = i % 2 == 0 ? new Color(1f, 0.72f, 0.12f, burst.rayAlpha) :
                    new Color(1f, 0.84f, 0.32f, burst.rayAlpha);
                int first = helper.currentVertCount;
                helper.AddVert(origin + radial * radius * 0.68f, new Color(tint.r, tint.g, tint.b, 0f), Vector2.zero);
                helper.AddVert(waist - width, tint, Vector2.zero);
                helper.AddVert(origin + radial * radius, tint, Vector2.zero);
                helper.AddVert(waist + width, tint, Vector2.zero);
                helper.AddTriangle(first, first + 1, first + 2);
                helper.AddTriangle(first + 2, first + 3, first);
            }
            for (int i = 0; i < 5 && burst.sparkleAlpha > 0.001f; i++)
            {
                float angle = (i + 0.65f) * Mathf.PI * 2f / 5f;
                Vector2 point = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * burst.radius * (i % 2 == 0 ? 0.95f : 1.08f);
                float size = (i % 2 == 0 ? 9f : 6f) * burst.scale * burst.sparkleAlpha;
                Color tint = i % 2 == 0 ? new Color(1f, 0.69f, 0.08f, burst.sparkleAlpha) : slot.simulation.Settings.trailColor;
                tint.a = burst.sparkleAlpha;
                int center = helper.currentVertCount;
                helper.AddVert(point, tint, Vector2.zero);
                for (int j = 0; j < 8; j++)
                {
                    float rotation = j * Mathf.PI / 4f;
                    float radius = size * (j % 2 == 0 ? 1f : 0.25f);
                    helper.AddVert(point + new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * radius, tint, Vector2.zero);
                    helper.AddTriangle(center, center + 1 + j, center + 1 + (j + 1) % 8);
                }
            }
        }

        private static void DrawCloud(VertexHelper helper, Vector2 position, float size, float squash,
            float alpha, float phase, int segments)
        {
            if (size < 0.1f) return;
            int center = helper.currentVertCount;
            float cosine = Mathf.Cos(phase), sine = Mathf.Sin(phase);
            helper.AddVert(position, new Color(1f, 1f, 1f, alpha), Vector2.zero);
            for (int i = 0; i < segments; i++)
            {
                Vector2 unit = CloudContour[i * (CloudContour.Length / segments)];
                Vector2 rotated = new Vector2(unit.x * cosine - unit.y * sine, unit.x * sine + unit.y * cosine);
                Vector2 contour = new Vector2(rotated.x, rotated.y * squash) * size;
                float shade = Mathf.Clamp01(rotated.y * 0.5f + 0.5f);
                Color fill = Color.Lerp(new Color(0.87f, 0.92f, 0.96f, alpha), new Color(1f, 1f, 1f, alpha), shade);
                Color edge = new Color(0.66f, 0.77f, 0.83f, alpha * 0.72f);
                helper.AddVert(position + contour * 0.91f, fill, Vector2.zero);
                helper.AddVert(position + contour, edge, Vector2.zero);
                helper.AddVert(position + contour * 1.055f, new Color(edge.r, edge.g, edge.b, 0f), Vector2.zero);
                int current = center + 1 + i * 3;
                int next = center + 1 + (i + 1) % segments * 3;
                helper.AddTriangle(center, current, next);
                for (int ring = 0; ring < 2; ring++)
                {
                    helper.AddTriangle(current + ring, current + ring + 1, next + ring + 1);
                    helper.AddTriangle(next + ring + 1, next + ring, current + ring);
                }
            }
        }

        // Cache the outside of overlapping circles once, so clouds have round lobes without internal outlines.
        private static Vector2[] CreateCloudContour()
        {
            Vector2[] centers =
            {
                new Vector2(-0.5f, -0.1f), new Vector2(-0.32f, 0.43f), new Vector2(0.25f, 0.52f),
                new Vector2(0.60f, 0.14f), new Vector2(0.30f, -0.45f), new Vector2(-0.28f, -0.48f)
            };
            float[] radii = { 0.5f, 0.5f, 0.49f, 0.46f, 0.45f, 0.4f };
            Vector2[] contour = new Vector2[48];
            for (int i = 0; i < contour.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / contour.Length;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float distance = 0.7f;
                for (int j = 0; j < centers.Length; j++)
                {
                    float projection = Vector2.Dot(direction, centers[j]);
                    float discriminant = radii[j] * radii[j] - centers[j].sqrMagnitude + projection * projection;
                    if (discriminant >= 0f) distance = Mathf.Max(distance, projection + Mathf.Sqrt(discriminant));
                }
                contour[i] = direction * distance;
            }
            return contour;
        }
    }
}
