using System;

namespace Whitebox.Foundation
{
    // GameOperationContainer 0x1ee4fb0..0x1ee5438. Native has 28 counters.
    public sealed class OriginalGameOperationContainer
    {
        internal const int FallAndSwapMask = (1 << 1) | (1 << 2);
        private readonly int[] counts = new int[28];
        private int lastErrorOperation = -1;
        public int Length => counts.Length;
        public void Reset(int operation) { counts[operation] = 0; }
        public void Start(int operation) { counts[operation] = unchecked(counts[operation] + 1); }
        public void Finish(int operation)
        {
            int count = counts[operation];
            if (count != 0) counts[operation] = unchecked(count - 1);
            else if (lastErrorOperation != operation)
            {
                lastErrorOperation = operation;
                UnityEngine.Debug.LogError("Finishing an inactive game operation: " + operation);
            }
        }
        public int GetCount(int operation) => counts[operation];
        public bool Has(int operation) => counts[operation] > 0;
        public bool HasAny()
        {
            for (int i = 0; i < counts.Length; i++) if (counts[i] > 0) return true;
            return false;
        }
        public bool HasMask(int mask)
        {
            int current = 0;
            for (int i = 0; i < counts.Length; i++) if (counts[i] > 0) current |= 1 << i;
            return current == mask; // Native compares equality, not bit overlap.
        }
        public void Clear()
        {
            lastErrorOperation = -1;
            Array.Clear(counts, 0, counts.Length);
        }
    }

    // Operation portion of GameStateManager. State transitions are separate.
    public sealed class OriginalGameOperations
    {
        private readonly Func<int> frameCount;
        public OriginalGameOperationContainer Container { get; } = new OriginalGameOperationContainer();
        public int LastOperationFrame { get; private set; }
        public OriginalGameOperations(Func<int> frameCount)
        {
            this.frameCount = frameCount ?? throw new ArgumentNullException(nameof(frameCount));
        }
        public void OperationStart(int operation)
        {
            LastOperationFrame = -1;
            Container.Start(operation);
        }
        public void OperationFinish(int operation)
        {
            Container.Finish(operation);
            if (!Container.HasAny() && LastOperationFrame < 0) LastOperationFrame = frameCount();
        }
        public void OperationReset(int operation)
        {
            Container.Reset(operation);
            if (!Container.HasAny() && LastOperationFrame < 0) LastOperationFrame = frameCount();
        }
        public void Clear()
        {
            LastOperationFrame = 0;
            Container.Clear();
        }
    }
}
