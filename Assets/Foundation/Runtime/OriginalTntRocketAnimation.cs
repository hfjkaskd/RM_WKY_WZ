using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable] public sealed class OriginalTntRocketAnimationSettings
    {
        public AnimationCurve positionEasing;
        public float firstPositionDuration, secondPositionDuration, finalPositionDuration;
        public float firstRotationDuration, secondRotationDuration, thirdRotationDuration, finalRotationDuration;
        public float smallScaleDuration, largeScaleDuration, rotationAngle, firstDistance, secondDistance;
        public float largeScale, mediumScale, smallScale, dimColor, freezeDuration;
        public OriginalTntShakeConfig shake;
    }

    // Typed Unity transport for the seven independent manual sequences built by
    // AnimateWithTime.MoveNext 0x1e32098. Original DOTweenSettings: linear default,
    // manual update; TweenManager.GetDeltaTime uses level delta while level is active.
    // No DOTween assembly, reflection, dynamic scene hierarchy or per-frame allocations.
    public sealed class OriginalTntRocketAnimation
    {
        private readonly OriginalSpecialBoardItem to, from;
        private readonly Transform toTransform, fromTransform;
        private readonly SpriteRenderer toBase, fromBase;
        private readonly Vector3 center, fromStart, fromScale, toScale, firstOffset, secondOffset;
        private readonly OriginalGridCell centerCell, emptyCell;
        private readonly OriginalTntRocketAnimationSettings settings;
        private readonly OriginalPlayableBoard game;
        private readonly OriginalLevelPresentation presentation;
        private readonly OriginalTntRocketComboUseParticles use;
        private readonly Action completed;
        private readonly float angle;
        private float elapsed;
        private int sortingStage;
        public bool IsComplete { get; private set; }
        public OriginalTntRocketAnimation(OriginalPlayableBoard game, OriginalLevelPresentation presentation,
            OriginalGridCell centerCell, OriginalGridCell emptyCell, OriginalSpecialBoardItem to,
            OriginalSpecialBoardItem from, int swipe, Action completed)
        {
            this.game = game; this.presentation = presentation; this.centerCell = centerCell; this.emptyCell = emptyCell;
            this.to = to; this.from = from; this.completed = completed; settings = presentation.TntRocketAnimationSettings;
            fromTransform = ((Component)from.View).transform; toTransform = ((Component)to.View).transform;
            fromBase = GetBase(from); toBase = GetBase(to);
            // The destination item's view stays at the visual merge center.
            // The moving view begins at its actual pose when the swipe is accepted.
            center = toTransform.localPosition; fromStart = fromTransform.localPosition;
            fromScale = fromTransform.localScale; toScale = toTransform.localScale;
            bool positive = swipe == 0 || swipe == 3;
            float a = positive ? settings.firstDistance : -settings.firstDistance;
            float b = positive ? -settings.secondDistance : settings.secondDistance;
            firstOffset = swipe > 1 ? new Vector3(a, 0f, 0f) : new Vector3(0f, a, 0f);
            secondOffset = swipe > 1 ? new Vector3(b, 0f, 0f) : new Vector3(0f, b, 0f);
            angle = swipe > 1 ? (swipe == 3 ? -settings.rotationAngle : settings.rotationAngle) : 0f;
            emptyCell.FreezeFall(); centerCell.FreezeFall(); ArrangeSorting(true);
            use = presentation.Pool.Spawn<OriginalTntRocketComboUseParticles>(21, true);
            use.transform.position = toTransform.position; use.Init(swipe > 1); use.Play();
        }
        private static SpriteRenderer GetBase(OriginalSpecialBoardItem item) =>
            item is OriginalTntBoardItem tnt ? tnt.ItemView.baseView : ((OriginalRocketBoardItem)item).ItemView.baseView;
        private static void Sort(OriginalSpecialBoardItem item, OriginalSwapSorting sorting)
        {
            if (item is OriginalTntBoardItem tnt) tnt.ItemView.UpdateSortingForAnimation(sorting, false);
            else ((OriginalRocketBoardItem)item).ItemView.UpdateSortingForAnimation(sorting, false);
        }
        private void ArrangeSorting(bool front)
        {
            var s = new OriginalSwapSorting(game.UiSortingLayer, unchecked(150 + game.NextPropellerSortingOffset()), true);
            Sort(from, s); Sort(to, new OriginalSwapSorting(s.Layer, unchecked(s.Order + (front ? -10 : 10)), s.SortEverything));
        }
        public void Tick(float delta)
        {
            if (IsComplete) return;
            elapsed += delta;
            float a = settings.firstPositionDuration, b = a + settings.secondPositionDuration;
            if (sortingStage == 0 && elapsed >= a) { ArrangeSorting(false); sortingStage = 1; }
            if (sortingStage == 1 && elapsed >= b) { ArrangeSorting(true); sortingStage = 2; }
            // The six native DOTween tracks retain their visual roles: the
            // destination item takes the positive offset and grows first.
            toTransform.localEulerAngles = new Vector3(0f, 0f, Rotation(angle));
            fromTransform.localEulerAngles = new Vector3(0f, 0f, Rotation(-angle));
            toTransform.localPosition = Position(center, center + firstOffset, center + secondOffset);
            fromTransform.localPosition = Position(fromStart, center - firstOffset, center - secondOffset);
            toTransform.localScale = Scale(toScale, toScale * settings.largeScale,
                toScale * settings.mediumScale, fromScale * settings.smallScale);
            fromTransform.localScale = Scale(fromScale, fromScale * settings.smallScale,
                fromScale * settings.largeScale, toScale * settings.largeScale);
            float d = settings.smallScaleDuration, c = settings.largeScaleDuration;
            toBase.color = Dim(elapsed, d); fromBase.color = elapsed < 2f * d ? Color.white : Dim(elapsed - 2f * d, c);
            if (elapsed < b + settings.finalPositionDuration) return;
            IsComplete = true; fromBase.color = Color.white; toBase.color = Color.white;
            presentation.Pool.Recycle(use);
            var blast = presentation.Pool.Spawn<OriginalTntRocketComboParticles>(20, true);
            blast.transform.position = toTransform.position; blast.Play();
            emptyCell.UnFreezeFall(); centerCell.UnFreezeFall();
            emptyCell.FreezeFallForDuration(settings.freezeDuration); centerCell.FreezeFallForDuration(settings.freezeDuration);
            // Native 0x1e32064: shake after freezing the cells and before starting the rocket wave.
            game.ShakeGrid(settings.shake);
            completed();
        }
        private Vector3 Position(Vector3 start, Vector3 first, Vector3 second)
        {
            float a = settings.firstPositionDuration, b = settings.secondPositionDuration;
            if (elapsed < a) return Vector3.LerpUnclamped(start, first, settings.positionEasing.Evaluate(elapsed / a));
            if (elapsed < a + b) return Vector3.LerpUnclamped(first, second, settings.positionEasing.Evaluate((elapsed - a) / b));
            float t = Mathf.Clamp01((elapsed - a - b) / settings.finalPositionDuration);
            return Vector3.LerpUnclamped(second, center, t * t * t * t * t); // InQuint14.
        }
        private float Rotation(float first)
        {
            float t = elapsed, a = settings.firstRotationDuration, b = settings.secondRotationDuration, c = settings.thirdRotationDuration;
            if (t < a) return Mathf.LerpUnclamped(0f, first, Mathf.Sin(t / a * Mathf.PI * .5f));
            t -= a;
            if (t < b) return Mathf.LerpUnclamped(first, -first, Mathf.Sin(t / b * Mathf.PI * .5f));
            t -= b;
            if (t < c) return Mathf.LerpUnclamped(-first, first, Mathf.Sin(t / c * Mathf.PI * .5f));
            t -= c;
            return Mathf.LerpUnclamped(first, 0f, 1f - Mathf.Cos(Mathf.Clamp01(t / settings.finalRotationDuration) * Mathf.PI * .5f));
        }
        private Vector3 Scale(Vector3 start, Vector3 a, Vector3 b, Vector3 c)
        {
            float t = elapsed, d = settings.smallScaleDuration, e = settings.largeScaleDuration;
            if (t < d) return Vector3.LerpUnclamped(start, a, t / d);
            t -= d; if (t < d) return Vector3.LerpUnclamped(a, b, t / d);
            t -= d; if (t < e) return Vector3.LerpUnclamped(b, c, t / e);
            return Vector3.LerpUnclamped(c, Vector3.one * settings.mediumScale, Mathf.Clamp01((t - e) / e));
        }
        private Color Dim(float t, float duration)
        {
            float value = t < duration ? Mathf.Lerp(1f, settings.dimColor, t / duration) :
                Mathf.Lerp(settings.dimColor, 1f, (t - duration) / duration);
            return new Color(value, value, value, 1f);
        }
    }
}
