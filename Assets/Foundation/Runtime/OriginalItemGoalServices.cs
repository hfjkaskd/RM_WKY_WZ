using System;

namespace Whitebox.Foundation
{
    // Identity contract for IMultipleBoardProgressDelegate. The item/game-UI
    // dispatch passes this object through; board presentation owns its methods.
    public interface IOriginalMultipleBoardProgressTarget { }
    public interface IOriginalGoalProgress
    {
        void IncrementProgress(int goalType, IOriginalMultipleBoardProgressTarget subGoalDelegate);
    }

    // Concrete ItemModel goal-manager/UI dispatch. Progress resolves the current
    // GameUi at collection time, as GameContext.CurrentGame does in the original.
    public sealed class OriginalItemGoalServices : IOriginalItemGoalServices
    {
        private readonly OriginalGoalManager goals;
        private readonly Func<IOriginalGoalProgress> currentProgress;
        public OriginalItemGoalServices(OriginalGoalManager goals, Func<IOriginalGoalProgress> currentProgress)
        {
            this.goals = goals ?? throw new ArgumentNullException(nameof(goals));
            this.currentProgress = currentProgress ?? throw new ArgumentNullException(nameof(currentProgress));
        }
        public void DecreaseGoal(int goalType) => goals.DecreaseGoal(goalType);
        public void CollectAsGoalUi(OriginalBoardItem item) => CollectAsGoalUi(item, null);
        public void CollectAsGoalUi(OriginalBoardItem item, IOriginalMultipleBoardProgressTarget subGoalDelegate) // 0x2b46c80.
        {
            goals.DecreaseGoalUi(item.GoalType);
            // A goal notification can change item state. Preserve native reads
            // after the notification and after resolving the current game UI.
            if (!OriginalGoalTypes.ShouldPlayParticleOnCollect(item.GoalType)) return;
            var progress = currentProgress(); // TryIncrementStoryLevelProgress 0x2b46cd4.
            if (progress == null) throw new NullReferenceException();
            progress.IncrementProgress(item.GoalType, subGoalDelegate);
        }
    }
}
