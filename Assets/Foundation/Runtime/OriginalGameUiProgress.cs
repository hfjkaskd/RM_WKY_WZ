using System;

namespace Whitebox.Foundation
{
    public interface IOriginalProgressLevelContext
    {
        bool IsStoryLevel { get; }
        bool IsFinalBoard { get; }
        bool IsPortalLevel { get; }
    }
    public interface IOriginalStoryGoalProgress
    {
        void IncrementProgress(int goalType);
    }
    public interface IOriginalBoardGoalProgress
    {
        void IncrementProgress(int goalType, IOriginalMultipleBoardProgressTarget subGoalDelegate);
    }

    // GameUi.IncrementProgress 0x162c0e8. Story-room and board-specific
    // presentation are required only on their original branches.
    public sealed class OriginalGameUiProgress : IOriginalGoalProgress
    {
        private readonly IOriginalProgressLevelContext context;
        private readonly IOriginalStoryGoalProgress story;
        private readonly IOriginalBoardGoalProgress board;
        public OriginalGameUiProgress(IOriginalProgressLevelContext context,
            IOriginalStoryGoalProgress story, IOriginalBoardGoalProgress board)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.story = story;
            this.board = board;
        }
        public void IncrementProgress(int goalType, IOriginalMultipleBoardProgressTarget subGoalDelegate)
        {
            if (context.IsStoryLevel)
            {
                if (story == null) throw new NullReferenceException();
                story.IncrementProgress(goalType);
            }
            if (context.IsFinalBoard && !context.IsPortalLevel) return;
            if (board == null) throw new NullReferenceException();
            board.IncrementProgress(goalType, subGoalDelegate);
        }
    }
}
