using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalMailGoalFlight : MonoBehaviour, IOriginalPoolable
    {
        [SerializeField] private OriginalMailRigView rig;
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private Transform viewParent;
        [SerializeField] private string flightClip;
        [SerializeField] private Vector3 startViewOffset, startShadowOffset, endViewOffset;
        [SerializeField] private float launchDelay, elasticDuration, flightDuration, visualScale, controlDrop;
        private OriginalDeltaTimeManager clock;
        private OriginalGameObjectPool pool;
        private OriginalMailTracer tracer;
        private Action arrived;
        private Transform shadowTransform;
        private Vector3 target;
        private float launchTime, offsetTime, offsetDuration;
        private bool launched, curveStarted;
        public float Delay { get; set; }
        public int GetPoolId() => 56;
        public void OnSpawn() { }
        public void OnRecycle()
        {
            clock = null;
            pool = null;
            tracer = null;
            arrived = null;
        }

        public void Initialize(Transform mailbox, OriginalKingRigData data, Material material, int layer)
        {
            Delay = -1f;
            transform.SetParent(mailbox, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            viewParent.localPosition = startViewOffset;
            viewParent.localScale = new Vector3(0f, 0f, 1f);
            shadowTransform = shadow.transform;
            shadowTransform.localPosition = startShadowOffset;
            rig.Bind(data, material);
            rig.Renderer.sortingLayerID = shadow.sortingLayerID = layer;
            rig.Renderer.sortingOrder = 2200;
            shadow.sortingOrder = 2190;
        }

        internal void BeginFlight(Vector3 target, OriginalDeltaTimeManager clock, OriginalGameObjectPool pool, Action arrived)
        {
            this.target = target;
            this.clock = clock;
            this.pool = pool;
            this.arrived = arrived;
            launchTime = offsetTime = offsetDuration = 0f;
            launched = curveStarted = false;
        }

        // InitAndMove 0x24ae908 inserts the launch and flight callbacks into
        // the application tween update. CurveToGoal also starts its local tweens there.
        internal bool TickAnimation(float deltaTime)
        {
            if (!curveStarted)
            {
                launchTime += deltaTime;
                if (!launched && launchTime >= launchDelay)
                {
                    launched = true;
                    viewParent.localScale = Vector3.one * visualScale;
                    rig.Play(flightClip);
                    tracer = pool.Spawn<OriginalMailTracer>(57, true);
                    tracer.Follow(rig.Envelope);
                }
                if (launchTime >= elasticDuration)
                {
                    curveStarted = true;
                    StartCoroutine(Fly());
                }
                return false;
            }

            if (offsetDuration <= 0f) return false;
            offsetTime = Mathf.Min(offsetTime + deltaTime, offsetDuration);
            GetOffsetTweenRatios(offsetTime, offsetDuration, out float shadowRatio, out float viewRatio);
            shadowTransform.localPosition = Vector3.LerpUnclamped(startShadowOffset, Vector3.zero, shadowRatio);
            viewParent.localPosition = Vector3.LerpUnclamped(startViewOffset, endViewOffset, viewRatio);
            return offsetTime >= offsetDuration;
        }

        private IEnumerator Fly() // CurveToGoal 0x24afbe8, portrait path.
        {
            if (Delay >= 0f) yield return new OriginalGameplayWait(Delay, () => clock.DeltaTime);

            var start = transform.position;
            var control = start + new Vector3(Mathf.Clamp(target.x - start.x, -.5f, .5f), controlDrop, 0f);
            float duration = GetFlightDuration(start, control, target, flightDuration);
            offsetDuration = duration;
            float time = 0f;
            float speed = 1f;
            while (time < duration)
            {
                time += clock.DeltaTime * speed;
                float t = Mathf.Clamp01(time / duration);
                var position = OriginalCubicBezierCurve.GetValue(start, control, target, t);
                transform.position = position;
                speed = Mathf.Lerp(.6f, 1f, Vector3.Distance(target, position) / 5f);
                yield return null;
            }
            tracer.StopFollowing();
            arrived();
        }

        internal static float GetFlightDuration(Vector3 from, Vector3 control, Vector3 to, float baseDuration)
        {
            // CalculateArcLength 0x2d804dc averages the chord and the control
            // polygon. Mail uses the same point for both control handles.
            float polygon = Vector3.Distance(from, control) + Vector3.Distance(control, to);
            float length = .5f * (polygon + Vector3.Distance(from, to));
            return baseDuration * (1f + .5f * Mathf.Clamp01(length / 15f));
        }

        internal static void GetOffsetTweenRatios(float elapsed, float duration, out float shadowRatio, out float viewRatio)
        {
            float angle = (elapsed / duration) * 1.5707964f;
            shadowRatio = 1f - (float)Math.Cos(angle); // InSine.
            viewRatio = (float)Math.Sin(angle); // OutSine.
        }
    }
}
