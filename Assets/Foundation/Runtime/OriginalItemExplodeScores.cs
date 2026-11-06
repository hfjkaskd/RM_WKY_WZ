using System;

namespace Whitebox.Foundation
{
    public interface IOriginalItemTargetScoreContext
    {
        int GetGoalLeftCount(int goalType);
        void RequireSupportedScoring();
    }

    public partial class OriginalBoardItem
    {
        private IOriginalItemTargetScoreContext targetScoreContext;
        protected IOriginalItemTargetScoreContext TargetScoreContext => targetScoreContext ?? throw new NullReferenceException();
        public void BindTargetScoreContext(IOriginalItemTargetScoreContext context)
        { targetScoreContext = context ?? throw new ArgumentNullException(nameof(context)); }
        public virtual int GetExplodeScore(int guidedType) => OriginalExplodeTargetRules.ItemScore(ItemType); // 0x2b4629c.
        public bool HasExplodeScore(OriginalExplodeData data) => GetExplodeScore(OriginalExplodeTargetRules.AsGuidedType(data.Trigger)) > 0; // 0x2b462b8.
        public virtual bool IsObstacle => OriginalExplodeTargetRules.IsObstacle(ItemType); // 0x2b47a38.

        protected int GetFourCellExplodeScore(int guidedType) // 0x2b4c428 / 0x2b4538c.
        {
            if (guidedType is 3 or 6) return 0x37c88;
            if (RemainingExplodeCount() != 1) return OriginalExplodeTargetRules.ItemScore(ItemType);
            return OriginalExplodeTargetRules.IsSingleExploderStrategy(guidedType) ? 4 * 0x25305 + 0x4a60b : 0x6f910;
        }
    }

    public sealed partial class OriginalMatchBoardItem
    {
        // MatchItemModel 0x21a5670 and its calculator 0x21a428c /
        // ShouldCalculateScore 0x21a4698. The formal first board has no active
        // hat/curtain/drill/cryptex/ice/frog/bird/turtle/shuttle helpers.
        public override int GetExplodeScore(int guidedType)
        {
            if (selectedByColorMixer) return 0;
            var context = TargetScoreContext;
            context.RequireSupportedScoring();
            int left = context.GetGoalLeftCount(GoalType);
            if (left <= 0) return 0;
            int result;
            if (MatchType == 5) result = left <= 2 ? 456976 : 182788;
            else result = OriginalExplodeTargetRules.IsSingleExploderStrategy(guidedType) ? 182788 : 17576;
            // Calculator.Reset 0x21a4244 sets its preferred match type to zero.
            // No owner changes it in this admitted first-board composition.
            return MatchType == 0 ? unchecked(result + 456976) : result;
        }
    }

    public abstract partial class OriginalSpecialBoardItem
    {
        public override int GetExplodeScore(int guidedType) // 0x27b4378, no-helper branch.
        {
            if (IsPreparedForSpecialUse) return -1;
            TargetScoreContext.RequireSupportedScoring();
            return -4569;
        }
    }
}
