using System;

namespace Whitebox.Foundation
{
    public partial class OriginalGridCell
    {
        private OriginalGameOperations gameOperations;
        public int FallFreezeCount { get; private set; }
        public bool IsFallFrozen => FallFreezeCount > 0; // 0x319d014.
        public int CrossFallFreezeCount { get; private set; }
        public void CrossFallFreeze() { CrossFallFreezeCount = unchecked(CrossFallFreezeCount + 1); } // 0x3192018.
        public void CrossFallUnFreeze() { CrossFallFreezeCount = unchecked(CrossFallFreezeCount - 1); } // 0x3192238.

        // Bound with the level's shared state manager during cell initialization.
        public void BindGameOperations(OriginalGameOperations operations)
        {
            gameOperations = operations ?? throw new ArgumentNullException(nameof(operations));
        }
        public void FreezeFall()
        {
            FallFreezeCount = unchecked(FallFreezeCount + 1);
            if (gameOperations == null) throw new NullReferenceException();
            gameOperations.OperationStart(1);
        }
        public void UnFreezeFall()
        {
            FallFreezeCount = unchecked(FallFreezeCount - 1);
            if (gameOperations == null) throw new NullReferenceException();
            gameOperations.OperationFinish(1);
            if (FallFreezeCount < 0) UnityEngine.Debug.LogError("Cell fall freeze count is negative.");
        }
    }

    public sealed class OriginalMatchFreezeOperations : IOriginalMatchFreezeOperations
    {
        public void FreezeFall(OriginalGridCell cell) => cell.FreezeFall();
    }
}
