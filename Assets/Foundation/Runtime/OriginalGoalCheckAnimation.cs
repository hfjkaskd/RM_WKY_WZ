using System;

namespace Whitebox.Foundation
{
    // Typed transport of GoalUi.ShowCheckImage's delayed linear sequence
    // 0x18cc914, start closure0x18ccca4 and final closure0x18ccf64.
    // Native DOTween default ease is Linear; no reflected Invoke or third-party runtime.
    public sealed class OriginalGoalCheckAnimation
    {
        private readonly float delay, initialMultiplier;
        private readonly OriginalGoalCheckStage[] stages;
        private readonly Action started, particles, finished;
        private float elapsed;
        private int stage = -1;
        public bool IsComplete { get; private set; }
        public float Scale { get; private set; } = 1f;
        public OriginalGoalCheckAnimation(float delay, float initialMultiplier, OriginalGoalCheckStage[] stages,
            Action started, Action particles, Action finished)
        { this.delay = delay; this.initialMultiplier = initialMultiplier; this.stages = stages; this.started = started; this.particles = particles; this.finished = finished; }
        public void Tick(float delta)
        {
            if (IsComplete) return;
            elapsed += delta;
            if (stage < 0)
            {
                if (elapsed < delay) return;
                elapsed -= delay; stage = 0; Scale = initialMultiplier; started();
            }
            while (stage < stages.Length && elapsed >= stages[stage].duration)
            {
                elapsed -= stages[stage].duration; Scale = stages[stage].scale;
                if (++stage == 2) particles();
            }
            if (stage == stages.Length) { IsComplete = true; finished(); return; }
            float from = stage == 0 ? initialMultiplier : stages[stage - 1].scale;
            Scale = from + (stages[stage].scale - from) * (elapsed / stages[stage].duration);
        }
    }
}
