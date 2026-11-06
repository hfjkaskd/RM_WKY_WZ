using System;

namespace Whitebox.Foundation
{
    public interface IOriginalGoalConditionServices
    {
        bool HasOperation(int operation);
        bool IsGoalReached(int goalType);
    }

    // GoalTuple 0x2ee7ef0..0x2ee8408. Model and display counts intentionally
    // have independent lifetimes; a visual collection finishes after damage.
    public sealed class OriginalGoalTuple
    {
        private readonly IOriginalGoalConditionServices conditions;
        public int GoalType { get; }
        public int TargetCount { get; private set; }
        public int LeftCount { get; private set; }
        public int LeftCountUi { get; private set; }
        public bool IsFromSettings { get; }
        public int ClaimedCount { get; private set; }

        public OriginalGoalTuple(int goalType, int targetCount, bool isFromSettings,
            IOriginalGoalConditionServices conditions)
        {
            GoalType = goalType;
            TargetCount = LeftCount = LeftCountUi = targetCount;
            IsFromSettings = isFromSettings;
            this.conditions = conditions;
            if ((goalType == 13 || goalType == 14 || goalType == 30) && conditions == null)
                throw new ArgumentNullException(nameof(conditions));
        }
        public int DecreaseGoal() => LeftCount = Nonnegative(unchecked(LeftCount - 1));
        public int DecreaseGoalUi() => LeftCountUi = Nonnegative(unchecked(LeftCountUi - 1));
        public int IncreaseGoal() => LeftCount = unchecked(LeftCount + 1);
        public int IncreaseGoalUi() => LeftCountUi = unchecked(LeftCountUi + 1);
        public void Reset() { LeftCount = LeftCountUi = TargetCount; }
        public bool IsFullyClaimed() => ClaimedCount > 0 && ClaimedCount >= TargetCount;
        public bool IsGoalCompleted()
        {
            if (GoalType == 21) return false; // CoinGoalCondition 0x2ee844c.
            if (LeftCount > 0) return false;
            switch (GoalType)
            {
                case 13: return !conditions.HasOperation(12);
                case 14: return !conditions.HasOperation(16) && conditions.IsGoalReached(54);
                case 30: return !conditions.HasOperation(12) && conditions.IsGoalReached(29);
                default: return true;
            }
        }
        public bool IsBottomCollect() => GoalType == 23 || GoalType == 37 || GoalType == 55 || GoalType == 91;
        public bool IsSpecificWinConditionalGoal()
        {
            uint shift = unchecked((uint)(GoalType - 37));
            return GoalType == 23 || (shift <= 62 && ((1UL << (int)shift) & 0x4040000000040001UL) != 0);
        }
        public int UpdateTargetCount(int amount)
        {
            TargetCount = unchecked(TargetCount + amount);
            LeftCount = unchecked(LeftCount + amount);
            LeftCountUi = unchecked(LeftCountUi + amount);
            return LeftCount;
        }
        public void SetGoal(int leftCount) { LeftCount = LeftCountUi = Nonnegative(leftCount); }
        internal void RestoreGoal(int target, int left)
        {
            TargetCount = target;
            SetGoal(left);
        }
        public void AddClaim(int claim) { ClaimedCount = unchecked(ClaimedCount + Nonnegative(claim)); }
        public void ResetTargetCount() { TargetCount = 0; }
        private static int Nonnegative(int value) => value < 0 ? 0 : value;
    }

    public sealed class OriginalLimitTuple
    {
        public int GoalType { get; }
        public int Limit { get; }
        public OriginalLimitTuple(int goalType, int limit) { GoalType = goalType; Limit = limit; }
    }
}
