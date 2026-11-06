using System;

namespace Whitebox.Foundation
{
    public interface IOriginalOneMoveRemoval { bool ShouldRemoveOneMove(); }
    public interface IOriginalOneMoveRemovalAb { bool IsOneMoveRemoveAbEnabled(); }
    public sealed class OriginalSuperDukeMoveRemoval : IOriginalOneMoveRemoval
    {
        private readonly Func<IOriginalOneMoveRemovalAb> currentAb;
        public OriginalSuperDukeMoveRemoval(Func<IOriginalOneMoveRemovalAb> currentAb)
        { this.currentAb = currentAb ?? throw new ArgumentNullException(nameof(currentAb)); }
        public bool ShouldRemoveOneMove() // 0x22301a8: AB query result is discarded in this original binary.
        {
            var ab = currentAb(); if (ab == null) throw new NullReferenceException();
            ab.IsOneMoveRemoveAbEnabled();
            return false;
        }
    }
    // Move fields shared with OriginalActiveLevelData.Load/name selection.
    // Full constructor, start/restart/save and cumulative state remain separate.
    public class OriginalActiveLevelMoveData
    {
        public int Number, MoveCount, OriginalMoveCount;
        public void UpdateMoveCount(int calculatedMoveCount) { MoveCount = calculatedMoveCount; } // 0x32b6dd4.
    }
    public sealed class OriginalMoveBudgetAdjustment
    {
        private readonly Func<IOriginalOneMoveRemoval> currentSuperDuke;
        private readonly Func<OriginalActiveLevelMoveData> currentLevel;
        private readonly OriginalSlbMoveConfig slb;
        public static int LastAddedExtraMoveCount;
        public OriginalMoveBudgetAdjustment(Func<IOriginalOneMoveRemoval> currentSuperDuke,
            Func<OriginalActiveLevelMoveData> currentLevel, OriginalSlbMoveConfig slb)
        {
            this.currentSuperDuke = currentSuperDuke ?? throw new ArgumentNullException(nameof(currentSuperDuke));
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
            this.slb = slb ?? throw new ArgumentNullException(nameof(slb));
        }
        public int GetMoveRemoveAmount(int moveCount) // 0x1eda6d8.
        {
            LastAddedExtraMoveCount = 0;
            if (moveCount <= 20) return 0;
            var duke = currentSuperDuke(); if (duke == null) throw new NullReferenceException();
            bool remove = duke.ShouldRemoveOneMove();
            int adjustment = LastAddedExtraMoveCount;
            if (remove) { adjustment = unchecked(adjustment - 1); LastAddedExtraMoveCount = adjustment; }
            var level = currentLevel(); if (level == null) throw new NullReferenceException();
            adjustment = unchecked(adjustment - slb.GetMoveAdjustment(level.Number));
            int minimum = unchecked(20 - moveCount);
            LastAddedExtraMoveCount = adjustment; // Keep the unclamped adjustment.
            return minimum <= adjustment ? adjustment : minimum;
        }
    }
    // Fresh LevelParser ILevelDataProvider getters. Each getter resolves current
    // active session independently. Restore providers must use their saved data.
    public sealed class OriginalFreshLevelMoves
    {
        private readonly Func<OriginalActiveLevelMoveData> currentLevel;
        public OriginalFreshLevelMoves(Func<OriginalActiveLevelMoveData> currentLevel)
        { this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel)); }
        public int GetMaxMoves() // 0x1c20058.
        { var level = currentLevel(); if (level == null) throw new NullReferenceException(); return level.MoveCount; }
        public int GetLeftMoves() // 0x1c200b4: same adjusted moveCount, not active-level leftMoves.
        { var level = currentLevel(); if (level == null) throw new NullReferenceException(); return level.MoveCount; }
        public int GetMadeMoves() => 0; // 0x1c26790.
    }
}
