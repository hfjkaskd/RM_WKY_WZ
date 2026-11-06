using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // WaitForGameplaySeconds 0x12de540/0x12de578. Each poll consumes the
    // level delta, including repeated polls in the same rendered frame.
    public sealed class OriginalGameplayWait : CustomYieldInstruction
    {
        private readonly float targetSeconds;
        private readonly Func<float> deltaTime;
        private float elapsed;
        public float ElapsedSeconds => elapsed;
        public OriginalGameplayWait(float targetSeconds, Func<float> deltaTime)
        {
            this.targetSeconds = targetSeconds;
            this.deltaTime = deltaTime ?? throw new ArgumentNullException(nameof(deltaTime));
        }
        public override bool keepWaiting
        {
            get
            {
                elapsed += deltaTime();
                // Native FCMP/CSET LT includes the unordered (NaN) flags.
                return float.IsNaN(elapsed) || float.IsNaN(targetSeconds) || elapsed < targetSeconds;
            }
        }
    }
}
