using System;

namespace Whitebox.Foundation
{
    public interface IOriginalItemReplacementContext
    {
        void RecycleReplacementView(OriginalBoardItem item);
        void CollectReplacementMadness(int matchType, OriginalSwapPoint position);
    }

    public partial class OriginalBoardItem
    {
        // ItemModel.ReplaceItem0x2b46f90. The factory is an explicit typed
        // owner, as in the other item services. This is not an explosion.
        public virtual void ReplaceItem(bool collectMadness, IOriginalItemReplacementContext context)
        {
            HasView = false;
            CollectAsGoal(true);
            (context ?? throw new NullReferenceException()).RecycleReplacementView(this);
        }
    }

    public sealed partial class OriginalMatchBoardItem
    {
        // MatchItemModel.ReplaceItem0x21a7168. Preserve goal/manager lookup
        // order, the captured pre-recycle position, counts and Madness gate.
        public override void ReplaceItem(bool collectMadness, IOriginalItemReplacementContext context)
        {
            HasView = false;
            var services = explosionServices ?? throw new NullReferenceException();
            if (MatchType == 5 && services.IsThereItemInLevel(22)) services.DecrementPurpleOnBoard();
            int goalsLeft = services.GetGoalUiLeftCount(GoalType);
            var position = GetViewPosition();
            if (goalsLeft > 0)
            {
                services.CollectToGoal(this);
                CollectAsGoal(true);
            }
            else
            {
                IOriginalMatchCollectManager manager = null;
                if (services.IsThereStaticItemInLevel(5)) manager = services.GetCurtainForMatchType(MatchType);
                if (manager == null && services.IsThereItemInLevel(29)) manager = services.GetDrillForMatchType(MatchType);
                if (manager == null && services.IsThereStaticItemInLevel(16)) manager = services.GetCryptexForMatchType(MatchType);
                if (manager == null)
                    (context ?? throw new NullReferenceException()).RecycleReplacementView(this);
                else
                {
                    services.CollectToManager(this, manager);
                    CollectAsGoal(true);
                }
            }
            services.MatchItemExploded(MatchType, 1);
            if (collectMadness)
                (context ?? throw new NullReferenceException()).CollectReplacementMadness(MatchType,
                    new OriginalSwapPoint(position.x, position.y, position.z));
        }
    }
}
