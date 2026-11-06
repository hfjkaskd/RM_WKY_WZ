using UnityEngine;

namespace Whitebox.Foundation
{
    // The native coin and mail paths use the same point for both control handles.
    internal static class OriginalCubicBezierCurve
    {
        internal static Vector3 GetValue(Vector3 from, Vector3 control, Vector3 to, float t) // 0x2d802c0.
        {
            float u = 1f - t;
            var weightedControl = (Vector2)control * 3f;
            var start = (Vector2)from * Mathf.Pow(u, 3f);
            var end = (Vector2)to * Mathf.Pow(t, 3f);
            var point = end + (start + weightedControl * t * (u * u) + weightedControl * (t * t) * u);
            return new Vector3(point.x, point.y, from.z);
        }

        internal static Vector3 CalculateMin(Vector3 from, Vector3 control, Vector3 to, float duration) // 0x2d80190.
        {
            var lowest = from;
            float step = duration / 50f;
            if (step <= 0f) return lowest;

            // The native search samples the unnormalized duration range.
            for (float sample = step; ; sample += step)
            {
                var point = GetValue(from, control, to, sample);
                if (point.y > lowest.y) return lowest;
                lowest = point;
                if (sample >= duration) return lowest;
            }
        }
    }
}
