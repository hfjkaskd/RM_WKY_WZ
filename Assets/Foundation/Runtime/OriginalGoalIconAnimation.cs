using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // GoalUi.OnGoalUpdated 0x18cc5cc and its return callback 0x18ccc6c.
    // Unset tween ease uses the recovered DOTweenSettings Linear default.
    internal sealed class OriginalGoalIconAnimation
    {
        private readonly List<Pulse> pulses = new List<Pulse>(4);
        private Transform image;
        private Vector3 originalScale;
        private OriginalGoalUiAssets settings;
        internal bool IsFlyingActive { get; private set; }
        internal int ActiveCount => pulses.Count;

        internal void Initialize(Transform image, Vector3 originalScale, OriginalGoalUiAssets settings)
        {
            this.image = image != null ? image : throw new ArgumentNullException(nameof(image));
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            this.originalScale = originalScale;
            Clear();
        }

        internal void Clear()
        {
            pulses.Clear();
            IsFlyingActive = false;
        }

        internal bool Play(bool flying)
        {
            if (flying && IsFlyingActive) return false;
            pulses.Add(new Pulse
            {
                target = Vector3.Scale(originalScale, flying ? settings.flyingHitMultiplier : settings.settledHitMultiplier),
                duration = flying ? settings.flyingHitDuration : settings.settledHitDuration,
                returnDuration = flying ? settings.flyingReturnDuration : settings.settledHitDuration,
                flying = flying
            });
            if (flying) IsFlyingActive = true;
            return true;
        }

        internal void Tick(float delta)
        {
            if (delta == 0f) return; // TweenManager does not start or apply a paused tween.
            // TweenManager snapshots the active range before dispatch. A return
            // tween created by an OnComplete callback starts next dispatch.
            int count = pulses.Count;
            for (int processed = 0, index = 0; processed < count; processed++)
            {
                var pulse = pulses[index];
                if (!pulse.started)
                {
                    // DOScale captures the transform on startup, after older
                    // active tweens have written their pose in this dispatch.
                    pulse.start = image.localScale;
                    pulse.started = true;
                }
                pulse.elapsed += delta;
                if (pulse.flying && !pulse.returning && pulse.elapsed >= pulse.duration)
                {
                    image.localScale = Vector3.LerpUnclamped(pulse.start, pulse.target, 1f);
                    pulse.elapsed -= pulse.duration;
                    pulse.duration = pulse.returnDuration;
                    pulse.start = image.localScale;
                    pulse.target = originalScale;
                    pulse.returning = true;
                }
                float t = Mathf.Clamp01(pulse.elapsed / pulse.duration);
                image.localScale = Vector3.LerpUnclamped(pulse.start, pulse.target, t);
                if (t >= 1f)
                {
                    pulses.RemoveAt(index);
                    if (pulse.flying) IsFlyingActive = false;
                    else if (!pulse.returning)
                        pulses.Add(new Pulse { target = originalScale, duration = pulse.returnDuration, returning = true });
                }
                else pulses[index++] = pulse;
            }
        }

        private struct Pulse
        {
            internal Vector3 start, target;
            internal float elapsed, duration, returnDuration;
            internal bool started, returning, flying;
        }
    }
}
