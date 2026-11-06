using System;
using System.Collections;

namespace Whitebox.Foundation
{
    public interface IOriginalLocalPositionTarget
    {
        OriginalSwapPoint LocalPosition { get; set; }
    }

    // MTween<Vector3>.Play.MoveNext 0x14e9970 and LocalPositionProcedure.Apply
    // 0x2c97e74. Explicit iterator preserves the native final yield: reaching
    // duration applies the final position; completion runs on the next resume.
    public sealed class OriginalLocalMoveTween : IEnumerator
    {
        private readonly Func<float> deltaTime;
        private IOriginalLocalPositionTarget target;
        private OriginalSwapPoint start, difference, scaledDifference;
        private float duration, elapsed, ratio, easeDifference;
        private int easeType;
        private int coroutineState = -1;
        private Action onComplete;
        public bool IsInUse { get; private set; }
        public float Elapsed => elapsed;
        public object Current => null;

        public OriginalLocalMoveTween(Func<float> deltaTime)
        {
            this.deltaTime = deltaTime ?? throw new ArgumentNullException(nameof(deltaTime));
        }
        public void Configure(IOriginalLocalPositionTarget target, OriginalSwapPoint end,
            float duration, Action onComplete, int easeType = 0)
        {
            if (IsInUse) throw new InvalidOperationException("Tween is already running.");
            // Normal swap uses native Unset(0), which evaluates as OutQuad(6).
            // Impossible swaps also use OutSine(3) / InOutSine(4).
            if (easeType != 0 && easeType != 1 && easeType != 3 && easeType != 4 && easeType != 6)
                throw new NotSupportedException("Tween ease is not restored: " + easeType);
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            start = target.LocalPosition;
            difference.X = end.X - start.X;
            difference.Y = end.Y - start.Y;
            difference.Z = end.Z - start.Z;
            this.duration = duration;
            this.onComplete = onComplete;
            this.easeType = easeType;
            elapsed = 0f;
            coroutineState = 0;
            IsInUse = true;
        }
        public bool MoveNext()
        {
            if (unchecked((uint)coroutineState) > 1u) return false;
            coroutineState = -1;
            if (!(elapsed < duration))
            {
                // Keep this slot occupied while completion can start a return
                // tween. Reusing the currently executing iterator is unsafe.
                try { onComplete?.Invoke(); }
                finally { Cancel(); }
                return false;
            }
            float next = elapsed + deltaTime();
            elapsed = next < duration ? next : duration;
            // Fields force single-precision rounding on Mono as on ARM64.
            ratio = elapsed / duration;
            if (easeType == 3)
            {
                easeDifference = ratio * 1.5707964f;
                ratio = (float)Math.Sin(easeDifference);
            }
            else if (easeType == 4)
            {
                // Native evaluator multiplies elapsed before dividing, rather
                // than multiplying the already rounded elapsed/duration ratio.
                easeDifference = elapsed * 3.1415927f;
                easeDifference /= duration;
                ratio = (float)Math.Cos(easeDifference);
                ratio -= 1f;
                ratio = -.5f * ratio;
            }
            else if (easeType != 1)
            {
                easeDifference = ratio - 2f;
                ratio = -ratio * easeDifference;
            }
            scaledDifference.X = difference.X * ratio;
            scaledDifference.Y = difference.Y * ratio;
            scaledDifference.Z = difference.Z * ratio;
            target.LocalPosition = new OriginalSwapPoint(start.X + scaledDifference.X,
                start.Y + scaledDifference.Y, start.Z + scaledDifference.Z);
            coroutineState = 1;
            return true;
        }
        internal void Cancel()
        {
            coroutineState = -1;
            target = null;
            onComplete = null;
            IsInUse = false;
        }
        public void Reset() { throw new NotSupportedException(); }
    }
}
