using System;
using System.Collections;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchExplodeAnimationView
    {
        bool IsSwapping { get; }
        OriginalSwapPoint RootScale { get; set; }
        void PlayExplodeParticle(bool extraEmit);
        void HideView();
        void RecycleView();
    }

    // MatchItemView.<ExplodeAnim>d__22.MoveNext, 0x21aa268. View recycling
    // stays after the last yielded frame, including when delta overshoots.
    public sealed class OriginalMatchExplodeAnimation : IEnumerator
    {
        private readonly IOriginalMatchExplodeAnimationView view;
        private readonly Func<float> deltaTime;
        private readonly Func<float, object> waitForSeconds;
        private readonly int trigger;
        private OriginalSwapPoint startScale, scaledDifference;
        private float elapsed, duration, particleDelay, ratio, angle, cosine, easing;
        private bool didExplodeParticle;
        private int state;
        public float Elapsed => elapsed;
        public object Current { get; private set; }

        public OriginalMatchExplodeAnimation(IOriginalMatchExplodeAnimationView view,
            Func<float> deltaTime, Func<float, object> waitForSeconds, int trigger)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.deltaTime = deltaTime ?? throw new ArgumentNullException(nameof(deltaTime));
            this.waitForSeconds = waitForSeconds ?? throw new ArgumentNullException(nameof(waitForSeconds));
            this.trigger = trigger;
        }

        public bool MoveNext()
        {
            int previousState = state;
            if (previousState != 0 && previousState != 1 && previousState != 2) return false;
            state = -1;
            if (previousState == 2)
            {
                view.RecycleView();
                return false;
            }
            if (previousState == 0)
            {
                if (trigger != 2 && trigger != 4)
                {
                    view.PlayExplodeParticle(true);
                    // The particle callback can change the swap state.
                    if (view.IsSwapping)
                    {
                        view.HideView();
                        Current = waitForSeconds(1f);
                        state = 2;
                        return true;
                    }
                    view.RecycleView();
                    return false;
                }
                elapsed = 0f;
                startScale = view.RootScale;
                // Literal float values in the native readonly-data tables.
                duration = trigger == 2 ? 0.13333334028720856f : 0.18333333730697632f;
                particleDelay = trigger == 2 ? 0.10000000149011612f : 0.15000000596046448f;
                didExplodeParticle = false;
            }
            if (!(elapsed < duration))
            {
                view.RecycleView();
                startScale = default(OriginalSwapPoint);
                return false;
            }
            elapsed += deltaTime();
            // ManualEasing.Sine.In 0x2c95c94: round each float operation
            // before calling cosine. Vector3.Lerp clamps the eased value.
            ratio = elapsed / duration;
            angle = ratio * 3.1415927410125732f;
            angle *= 0.5f;
            cosine = (float)Math.Cos(angle);
            easing = 1f - cosine;
            if (easing > 1f) easing = 1f;
            if (easing < 0f) easing = 0f;
            scaledDifference.X = -startScale.X * easing;
            scaledDifference.Y = -startScale.Y * easing;
            scaledDifference.Z = -startScale.Z * easing;
            view.RootScale = new OriginalSwapPoint(startScale.X + scaledDifference.X,
                startScale.Y + scaledDifference.Y, startScale.Z + scaledDifference.Z);
            if (particleDelay <= elapsed && !didExplodeParticle)
            {
                didExplodeParticle = true;
                view.PlayExplodeParticle(false);
            }
            Current = null;
            state = 1;
            return true;
        }

        public void Reset() { throw new NotSupportedException(); }
    }
}
