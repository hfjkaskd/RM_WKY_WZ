namespace Whitebox.Foundation
{
    // Allocation-free four-corner form of the source ear/decomposition rules.
    // Animated flag masks have exactly four unweighted vertices. Static masks
    // retain their authored convex polygons in all previous rig formats.
    public sealed class OriginalQuadClip
    {
        private readonly int[] order = new int[4];
        private readonly int[] remaining = new int[3];
        private readonly bool[] concave = new bool[4];
        private readonly int[][] single = { new int[4] };
        private readonly int[][] pair = { new int[3], new int[3] };
        private float[] points;

        public int[][] Build(float[] vertices)
        {
            points = vertices;
            for (int i = 0; i < 4; i++) order[i] = i;
            Clockwise(order);
            for (int i = 0; i < 4; i++)
                concave[i] = !Positive(order[(i + 3) % 4], order[i], order[(i + 1) % 4]);

            int previous = 3, ear = 0, next = 1;
            while (true)
            {
                bool valid = !concave[ear];
                if (valid)
                {
                    int a = order[previous], b = order[ear], c = order[next];
                    for (int other = (next + 1) % 4; other != previous; other = (other + 1) % 4)
                    {
                        if (!concave[other]) continue;
                        int p = order[other];
                        if (Positive(c, a, p) && Positive(a, b, p) && Positive(b, c, p)) { valid = false; break; }
                    }
                }
                if (valid) break;
                if (next == 0)
                {
                    do { if (!concave[ear]) break; ear--; } while (ear > 0);
                    break;
                }
                previous = ear; ear = next; next = (next + 1) % 4;
            }
            int[] first = pair[0], second = pair[1];
            first[0] = order[(ear + 3) % 4]; first[1] = order[ear]; first[2] = order[(ear + 1) % 4];
            for (int i = 0, n = 0; i < 4; i++) if (i != ear) remaining[n++] = order[i];
            second[0] = remaining[2]; second[1] = remaining[0]; second[2] = remaining[1];

            // The initial triangle fan and the later fan merge use different
            // cyclic winding evaluations in the source, including degenerate
            // float cases. Preserve both tests and their original order.
            if (first[0] == second[0] &&
                Merge(first, second, Winding(first[0], first[1], first[2])))
                return FinishSingle(first, second);
            if (CanMerge(first, second)) return FinishSingle(first, second);
            if (CanMerge(second, first)) return FinishSingle(second, first);
            Clockwise(first); Clockwise(second);
            return pair;
        }

        private bool CanMerge(int[] first, int[] second) => first[0] == second[0] && first[2] == second[1] &&
            Merge(first, second, Winding(first[1], first[2], first[0]));

        private bool Merge(int[] first, int[] second, int winding) =>
            Winding(first[1], first[2], second[2]) == winding &&
            Winding(second[2], first[0], first[1]) == winding;

        private int[][] FinishSingle(int[] first, int[] second)
        {
            var result = single[0];
            result[0] = first[0]; result[1] = first[1]; result[2] = first[2]; result[3] = second[2];
            Clockwise(result);
            return single;
        }

        private void Clockwise(int[] polygon)
        {
            int last = polygon[polygon.Length - 1] * 2, first = polygon[0] * 2;
            float area = points[last] * points[first + 1] - points[first] * points[last + 1];
            for (int i = 0; i < polygon.Length - 1; i++)
            {
                int a = polygon[i] * 2, b = polygon[i + 1] * 2;
                area += points[a] * points[b + 1] - points[b] * points[a + 1];
            }
            if (area < 0f) return;
            for (int i = 0; i < polygon.Length / 2; i++)
            {
                int other = polygon.Length - 1 - i, value = polygon[i];
                polygon[i] = polygon[other]; polygon[other] = value;
            }
        }

        private bool Positive(int a, int b, int c)
        {
            a *= 2; b *= 2; c *= 2;
            return points[a] * (points[c + 1] - points[b + 1]) +
                points[b] * (points[a + 1] - points[c + 1]) +
                points[c] * (points[b + 1] - points[a + 1]) >= 0f;
        }

        private int Winding(int a, int b, int c)
        {
            a *= 2; b *= 2; c *= 2;
            float x = points[b] - points[a], y = points[b + 1] - points[a + 1];
            return points[c] * y - points[c + 1] * x + x * points[a + 1] - points[a] * y >= 0f ? 1 : -1;
        }
    }
}
