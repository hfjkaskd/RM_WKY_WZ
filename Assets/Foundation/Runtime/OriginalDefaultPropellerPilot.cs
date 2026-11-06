using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public enum OriginalGuidedExploderFlyState { Stable = 0, ComboExpand = 1, Flying = 2 }
    public enum OriginalGuidedExploderScaleState { None = 0, ScaleUp = 1, ScaleDown = 2 }
    public interface IOriginalGuidedExploderPilot
    {
        bool ShouldPlaySfxInLoop { get; }
        OriginalGuidedExploderFlyState FlyState { get; }
        void StartFlying(IOriginalPropellerPilotView view, IOriginalGuidedExploderViewDelegate viewDelegate);
        void TargetChanged();
        void Move();
    }
    public interface IOriginalGuidedExploderViewDelegate
    {
        bool IsTargetValid();
        Vector3 GetTargetPosition();
        IOriginalGuidedExploderPilot GetNewPilot();
        void ForceTargetChange();
        void FakeTargetReached();
        bool IsTargetAmountReached();
        void FinishGuidedExplodeOperation();
    }
    public interface IOriginalPropellerPilotView
    {
        Transform PilotTransform { get; }
        Transform PilotAnimatorTransform { get; }
        Transform PilotFanTransform { get; }
        Transform PilotShadowTransform { get; }
        void PlayStartFlyAnimation(bool isSwipe, bool shouldPlayAudio);
        void UpdatePilotSorting(bool scaleDown, bool sortEverything);
        void TriggerTargetReached();
        void TriggerFakeTargetReached();
        void ExplodeSelf(bool propellerRocketCombo);
    }
    public interface IOriginalPropellerPilotContext
    {
        int FrameCount { get; }
        float LevelTime { get; }
        float LevelDeltaTime { get; }
        float UnityDeltaTime { get; }
        float RandomValue();
        void ReportLargeDelta(float delta);
        void ReportStalledFlight(bool forcedExplosion);
    }

    // DefaultPropellerPilot 0x27c1180..0x27c36d4. Original compiled constants
    // and state transitions; no tween/alternate Editor flight path.
    public class OriginalDefaultPropellerPilot : IOriginalGuidedExploderPilot
    {
        protected static readonly Vector3 maxShadowDistance = new Vector3(.5f, -.6f, 0f);
        protected readonly IOriginalPropellerPilotContext context;
        protected IOriginalPropellerPilotView itemView;
        protected Transform transform;
        protected IOriginalGuidedExploderViewDelegate viewDelegate;
        protected float outDegree, outSpeed, elapsed, moveCanceledAtTime, maxOutDegreeVar, startOutSpeed, minOutSpeed, maxOutSpeed;
        protected Vector3 outVectorType, maxReachedShadowDistance, combinedVelocity;
        protected bool isOnEdge;
        protected OriginalGuidedExploderFlyState flyState;
        private bool hasTriedForceRedirect, updateSortingForScaleDown;
        private int currentStaticFrameCount, lastRunFrame;
        private float rotationSmoothVel, rotationFanSmoothVel, goSpeed, maxReachedScale, scaleDownStartDistance;
        private OriginalGuidedExploderScaleState scaleState;
        private Vector3 combinedVelocitySmoother;
        protected virtual float ScaleUpSpeedConst => 4f;
        protected virtual float MinOutSpeedConst => .8f;
        protected virtual float MaxOutSpeedConst => 1f;
        protected virtual float GoSpeedMultiplierConst => 20f;
        protected virtual float MaxGoSpeedConst => 50f;
        protected virtual float MinDistanceToScaleDown => 1f;
        public virtual bool ShouldPlaySfxInLoop => true;
        public OriginalGuidedExploderFlyState FlyState => flyState;
        public OriginalDefaultPropellerPilot(IOriginalPropellerPilotContext context)
        { this.context = context ?? throw new ArgumentNullException(nameof(context)); }

        public virtual void StartFlying(IOriginalPropellerPilotView itemView, IOriginalGuidedExploderViewDelegate viewDelegate) // 0x27c1180.
        {
            this.itemView = itemView ?? throw new ArgumentNullException(nameof(itemView));
            transform = itemView.PilotTransform;
            this.viewDelegate = viewDelegate ?? throw new ArgumentNullException(nameof(viewDelegate));
            flyState = GetStartState(); lastRunFrame = 0;
            InitializeCurveVariables((viewDelegate.GetTargetPosition() - transform.position).normalized);
            Move();
        }
        protected virtual OriginalGuidedExploderFlyState GetStartState() => OriginalGuidedExploderFlyState.Flying;
        public virtual void TargetChanged() // 0x27c142c.
        {
            flyState = OriginalGuidedExploderFlyState.Flying;
            elapsed = Mathf.Min(elapsed, .5f); goSpeed = 0f; moveCanceledAtTime = context.LevelTime;
            if (scaleState == OriginalGuidedExploderScaleState.ScaleDown) scaleState = OriginalGuidedExploderScaleState.ScaleUp;
        }
        public virtual void Move() // 0x27c1478.
        {
            if (lastRunFrame == context.FrameCount) return;
            lastRunFrame = context.FrameCount;
            elapsed = (float)(elapsed + context.LevelDeltaTime);
            if (viewDelegate.IsTargetValid()) MoveToTarget();
        }
        protected virtual void MoveToTarget() // 0x27c157c.
        {
            Vector3 targetVector = viewDelegate.GetTargetPosition() - transform.position;
            float distance = targetVector.magnitude;
            Vector3 targetNormalized = targetVector.normalized;
            ArrangeScale(distance);
            if (ArrangePosition(targetVector, targetNormalized, distance)) currentStaticFrameCount = 0;
            else
            {
                currentStaticFrameCount = unchecked(currentStaticFrameCount + 1);
                if (currentStaticFrameCount >= 45)
                {
                    if (hasTriedForceRedirect) TargetReached(true);
                    else { viewDelegate.ForceTargetChange(); hasTriedForceRedirect = true; currentStaticFrameCount = 0; }
                    context.ReportStalledFlight(hasTriedForceRedirect && currentStaticFrameCount >= 45);
                }
            }
            Vector3 current = transform.position, target = viewDelegate.GetTargetPosition();
            if (elapsed < GetMinFlyTime()) return;
            distance = Vector3.Distance(current, target);
            if (distance < .1f || Approximately(distance, 0f)) TargetReached(false);
        }
        protected virtual float GetMinFlyTime() => .5f;
        protected virtual void InitializeCurveVariables(Vector3 targetNormalized) // 0x27c217c.
        {
            itemView.PlayStartFlyAnimation(false, true);
            goSpeed = 0f; scaleState = OriginalGuidedExploderScaleState.ScaleUp; outDegree = 100f;
            rotationSmoothVel = rotationFanSmoothVel = elapsed = moveCanceledAtTime = 0f;
            combinedVelocity = combinedVelocitySmoother = Vector3.zero;
            startOutSpeed = .2f; minOutSpeed = MinOutSpeedConst; maxOutSpeed = MaxOutSpeedConst;
            float localX = transform.localPosition.x;
            isOnEdge = localX < 2f || localX > 6f;
            Vector3 targetLocal = transform.parent.InverseTransformPoint(viewDelegate.GetTargetPosition());
            if (isOnEdge)
            {
                maxOutDegreeVar = Vector3.Angle(targetNormalized, Vector3.up);
                outVectorType = localX < 2f ? Vector3.forward : Vector3.back;
            }
            else
            {
                maxOutDegreeVar = (float)((float)(context.RandomValue() * 80f) + 20f);
                outVectorType = Vector3.forward;
                if (targetLocal.x < 1f) outVectorType = Vector3.forward;
                else if (targetLocal.x > 7f) outVectorType = Vector3.back;
                else outVectorType = context.RandomValue() < .5f ? Vector3.forward : Vector3.back;
            }
            itemView.UpdatePilotSorting(false, IsPropSpecialCombo());
        }
        protected virtual void TargetReached(bool fakeTargetReach) // 0x27c2654.
        {
            flyState = OriginalGuidedExploderFlyState.Stable;
            if (fakeTargetReach) itemView.TriggerFakeTargetReached(); else itemView.TriggerTargetReached();
            itemView.ExplodeSelf(IsPropRocketCombo());
        }
        protected virtual bool IsPropRocketCombo() => false;
        protected virtual bool IsPropSpecialCombo() => false;
        protected virtual bool ArrangePosition(Vector3 targetVector, Vector3 targetNormalized, float distance) // 0x27c26cc.
        {
            float delta = context.UnityDeltaTime;
            if (delta > .5f) { context.ReportLargeDelta(delta); delta = .5f; }
            float progress = Mathf.Min((float)(elapsed / .75f), 1f);
            outSpeed = progress < .7f ? Lerp(startOutSpeed, maxOutSpeed, (float)(progress / .7f)) :
                Lerp(maxOutSpeed, minOutSpeed, (float)((float)(progress - .7f) / .3f));
            outDegree = (float)(maxOutDegreeVar + (float)(Mathf.Max(progress, 0f) * (float)(20f - maxOutDegreeVar)));
            Vector3 curveVelocity = Quaternion.AngleAxis(outDegree, outVectorType) * targetVector;
            curveVelocity *= (float)(delta * outSpeed);
            Vector3 targetVelocity = Vector3.zero;
            if (elapsed > .525f)
            {
                goSpeed = Mathf.Clamp((float)(goSpeed + (float)(delta * GoSpeedMultiplierConst)), 0f, MaxGoSpeedConst);
                targetVelocity = targetNormalized * (float)(delta * goSpeed);
                if (targetVelocity.magnitude > distance) targetVelocity = targetVector;
            }
            Vector3 desired = curveVelocity + targetVelocity;
            if (combinedVelocity.sqrMagnitude < 9.99999944e-11f || scaleState == OriginalGuidedExploderScaleState.ScaleDown)
                combinedVelocity = desired;
            else
            {
                float coefficient = CalculateAngleSmoothCoefficient();
                float angular = Lerp(0f, .35f, (float)(Vector3.Angle(desired, combinedVelocity) / 90f));
                float distanceTerm = Lerp(0f, .1f, (float)((float)(distance - 2f) * .5f));
                float smoothTime = (float)((float)(coefficient * angular) + distanceTerm);
                combinedVelocity = Vector3.SmoothDamp(combinedVelocity, desired, ref combinedVelocitySmoother,
                    smoothTime, float.PositiveInfinity, context.UnityDeltaTime);
                if (MinDistanceToScaleDown > distance && scaleState == OriginalGuidedExploderScaleState.None)
                {
                    scaleDownStartDistance = distance; maxReachedScale = transform.localScale.x;
                    maxReachedShadowDistance = itemView.PilotShadowTransform.localPosition;
                    scaleState = OriginalGuidedExploderScaleState.ScaleDown;
                    if (!IsPropSpecialCombo()) updateSortingForScaleDown = true;
                }
            }
            Vector3 previous = transform.position;
            transform.position += combinedVelocity;
            ArrangeRotation(curveVelocity, targetVelocity, targetNormalized, distance);
            Vector3 current = transform.position;
            // Native NumberExtensions.Vector3 Approximately intentionally checks x/y only.
            return !(Approximately(previous.x, current.x) && Approximately(previous.y, current.y));
        }
        protected virtual float CalculateAngleSmoothCoefficient() =>
            (float)(1f - Mathf.Clamp01((float)((float)(context.LevelTime - moveCanceledAtTime) / .4f))); // 0x27c30f0.
        private void ArrangeRotation(Vector3 curveVelocity, Vector3 targetVelocity, Vector3 targetNormalized, float distance) // 0x27c2e0c.
        {
            float x = Approximately(goSpeed, 0f) ? curveVelocity.x : targetVelocity.x;
            float coefficient = Lerp(0f, 1f, (float)(distance - 1.8f));
            float ratio = (float)((float)((float)((float)(x * 20f) * coefficient) + 1f) * .5f);
            float targetAngle = scaleState == OriginalGuidedExploderScaleState.ScaleDown ? 0f : Lerp(35f, -20f, ratio);
            float smoothTime = (float)((float)(coefficient * .336f) + .064f);
            float current = itemView.PilotAnimatorTransform.eulerAngles.z;
            if (current > 180f) current = (float)(current - 360f);
            float angle = Mathf.SmoothDamp(current, targetAngle, ref rotationSmoothVel, smoothTime,
                float.PositiveInfinity, context.UnityDeltaTime);
            itemView.PilotAnimatorTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            float targetFanAngle = Lerp(-8f, 8f, (float)((float)(targetNormalized.x + 1f) * .5f));
            current = itemView.PilotFanTransform.localRotation.eulerAngles.z;
            if (scaleState == OriginalGuidedExploderScaleState.ScaleDown) current = 0f;
            if (current > 180f) current = (float)(current - 360f);
            smoothTime = (float)((float)(coefficient * .18599999f) + .064f);
            angle = Mathf.SmoothDamp(current, targetFanAngle, ref rotationFanSmoothVel, smoothTime,
                float.PositiveInfinity, context.UnityDeltaTime);
            itemView.PilotFanTransform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        protected void ArrangeScale(float distance) // 0x27c1f44.
        {
            if (scaleState == OriginalGuidedExploderScaleState.ScaleUp)
            {
                Vector3 targetScale = Vector3.one * 1.5f, itemScale = transform.localScale;
                float ratio = (float)((float)(targetScale.x - itemScale.x) / (float)(targetScale.x - 1f));
                float speed = (float)(Mathf.Clamp(ratio, .3f, 1f) * ScaleUpSpeedConst); // ManualEasing.Linear is identity.
                ArrangeScaleUp(targetScale, itemScale, speed);
            }
            else if (scaleState == OriginalGuidedExploderScaleState.ScaleDown)
            {
                float ratio = (float)((float)(scaleDownStartDistance - distance) / scaleDownStartDistance);
                if (ratio >= .3f && updateSortingForScaleDown)
                { updateSortingForScaleDown = false; itemView.UpdatePilotSorting(true, IsPropSpecialCombo()); }
                ArrangeScaleDown(ratio);
            }
        }
        protected virtual void ArrangeScaleDown(float ratio) // 0x27c3130.
        {
            if (ratio < 0f) scaleState = OriginalGuidedExploderScaleState.None;
            transform.localScale = Vector3.Lerp(Vector3.one * maxReachedScale, Vector3.one, Mathf.Max(Mathf.Clamp01(ratio), 0f));
            Vector3 shadow = Vector3.MoveTowards(maxReachedShadowDistance, Vector3.zero, ratio);
            shadow.x = Mathf.Min(shadow.x, maxShadowDistance.x); shadow.y = Mathf.Max(shadow.y, maxShadowDistance.y);
            itemView.PilotShadowTransform.localPosition = shadow;
        }
        protected virtual void ArrangeScaleUp(Vector3 targetScale, Vector3 itemScale, float speed) // 0x27c3384.
        {
            transform.localScale = Vector3.MoveTowards(itemScale, targetScale, (float)(context.UnityDeltaTime * speed));
            var shadow = itemView.PilotShadowTransform;
            shadow.localPosition = Vector3.MoveTowards(shadow.localPosition, maxShadowDistance, (float)(context.UnityDeltaTime * speed));
            if (itemScale.x > 1.5f || Approximately(itemScale.x, 1.5f)) scaleState = OriginalGuidedExploderScaleState.None;
        }
        protected static float Lerp(float from, float to, float ratio) =>
            (float)(from + (float)(Mathf.Clamp01(ratio) * (float)(to - from)));
        protected static bool Approximately(float value, float target) => value < (float)(target + .001f) && value > (float)(target - .001f);
    }
}
