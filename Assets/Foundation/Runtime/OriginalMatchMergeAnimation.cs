using System;
using System.Collections;

namespace Whitebox.Foundation
{
    [Serializable]
    public sealed class OriginalMatchMergeSettings
    {
        public float Duration;
        public float ExplodeCompleteRatio;
    }
    public interface IOriginalMatchMergeAnimationView
    {
        OriginalSwapPoint Position { get; set; }
        void StopMergeCoroutine();
    }

    // MatchItemView.<MergeCoroutine>d__30.MoveNext 0x21aa5ac. The complete
    // callback precedes handle lookup/stop, and its extra yield is preserved.
    public sealed class OriginalMatchMergeAnimation : IEnumerator
    {
        private readonly IOriginalMatchMergeAnimationView view;
        private readonly Func<float> deltaTime;
        private readonly OriginalMatchMergeSettings settings;
        private readonly float duration;
        private readonly OriginalSwapPoint end;
        private readonly bool extrapolate;
        private readonly Action onComplete;
        private OriginalSwapPoint start, weightedStart, weightedEnd;
        private float elapsed, ratio, angle, cosine, easing, inverse;
        private int state;
        public float Elapsed => elapsed;
        public object Current => null;
        public OriginalMatchMergeAnimation(IOriginalMatchMergeAnimationView view, Func<float> deltaTime,
            OriginalMatchMergeSettings settings, OriginalSwapPoint end, bool extrapolate, Action onComplete)
        {
            this.view = view;
            this.deltaTime = deltaTime;
            this.settings = settings;
            duration = settings.Duration;
            this.end = end;
            this.extrapolate = extrapolate;
            this.onComplete = onComplete;
        }
        public bool MoveNext()
        {
            int previous = state;
            if (unchecked((uint)previous) > 2u) return false;
            state = -1;
            if (previous == 1) { state = 2; return true; }
            if (previous == 0) { elapsed = 0f; start = view.Position; }
            if (!(elapsed < duration))
            {
                view.Position = end;
                return false;
            }
            elapsed += deltaTime();
            ratio = elapsed / duration;
            // Native ManualEasing.Sine.In (enum 2, 0x2c95c94).
            angle = ratio * 3.1415927410125732f;
            angle *= .5f;
            cosine = (float)Math.Cos(angle);
            easing = 1f - cosine;
            // VectorExtensions.Lerp 0x2d7bdf4 weights the two positions,
            // rather than using start + (end - start) * t.
            if (!extrapolate)
            {
                if (easing < 0f) easing = 0f;
                else if (easing > 1f) easing = 1f;
            }
            inverse = 1f - easing;
            weightedEnd = new OriginalSwapPoint(end.X * easing, end.Y * easing, end.Z * easing);
            weightedStart = new OriginalSwapPoint(start.X * inverse, start.Y * inverse, start.Z * inverse);
            view.Position = new OriginalSwapPoint(weightedEnd.X + weightedStart.X,
                weightedEnd.Y + weightedStart.Y, weightedEnd.Z + weightedStart.Z);
            ratio = elapsed / duration;
            if (ratio > settings.ExplodeCompleteRatio)
            {
                onComplete?.Invoke();
                view.StopMergeCoroutine();
                state = 1;
                return true;
            }
            state = 2;
            return true;
        }
        public void Reset() { throw new NotSupportedException(); }
    }
}
