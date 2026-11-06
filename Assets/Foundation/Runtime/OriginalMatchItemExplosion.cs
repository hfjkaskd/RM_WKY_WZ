using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchCollectManager { }
    public interface IOriginalMatchItemExplosionServices
    {
        int GetGoalUiLeftCount(int goalType);
        bool IsThereStaticItemInLevel(int type);
        bool IsThereItemInLevel(int type);
        IOriginalMatchCollectManager GetCurtainForMatchType(int matchType);
        IOriginalMatchCollectManager GetDrillForMatchType(int matchType);
        IOriginalMatchCollectManager GetCryptexForMatchType(int matchType);
        void DecrementPurpleOnBoard();
        void TryDamageNeighborIceBorders(OriginalMatchBoardItem item);
        void CollectToGoal(OriginalMatchBoardItem item);
        void CollectToManager(OriginalMatchBoardItem item, IOriginalMatchCollectManager manager);
        void StartShifterSpecialOperation();
        void ExplodeAndMoveToPosition(OriginalMatchBoardItem item, OriginalGridCell creationCell, int index, Action completed);
        void RecycleMatchView(OriginalMatchBoardItem item);
        void FinishShifterSpecialOperation();
        void StartExplodeAnimation(OriginalMatchBoardItem item, int trigger);
        void UsefulMoveMade();
        void ExplodeTopMostBelowItem(OriginalGridCell cell, OriginalExplodeData data);
        void HandleMadness(OriginalMatchBoardItem item, OriginalExplodeData data);
        void MatchItemExploded(int matchType, int count);
    }

    public sealed partial class OriginalMatchBoardItem
    {
        private IOriginalMatchItemExplosionServices explosionServices;
        private Action mergeCompleted;
        private bool selectedByColorMixer;
        public void SelectByColorMixer() { selectedByColorMixer = true; } // 0x21a7a44.
        public void UnselectByColorMixer() { selectedByColorMixer = false; } // 0x21a7a50.
        public void BindExplosionServices(IOriginalMatchItemExplosionServices services)
        {
            explosionServices = services ?? throw new ArgumentNullException(nameof(services));
            mergeCompleted = HandleMergeCompleted;
        }
        protected override void TryExplode(OriginalExplodeData data) // 0x21a5d90.
        {
            if (selectedByColorMixer) return;
            var services = explosionServices;
            if (services == null) throw new NullReferenceException();
            int goalsLeft = services.GetGoalUiLeftCount(GoalType);
            IOriginalMatchCollectManager curtain = null, drill = null, cryptex = null;
            if (services.IsThereStaticItemInLevel(5)) curtain = services.GetCurtainForMatchType(MatchType);
            if (services.IsThereItemInLevel(29)) drill = services.GetDrillForMatchType(MatchType);
            if (services.IsThereStaticItemInLevel(16)) cryptex = services.GetCryptexForMatchType(MatchType);
            if (MatchType == 5 && services.IsThereItemInLevel(22)) services.DecrementPurpleOnBoard();
            if (data.Trigger == 18 && services.IsThereStaticItemInLevel(17)) services.TryDamageNeighborIceBorders(this);

            if (SpecialItemCreationData.ShouldCreateSpecialItem)
            {
                if (goalsLeft > 0) services.CollectToGoal(this);
                else
                {
                    var manager = curtain ?? drill ?? cryptex;
                    if (manager != null) services.CollectToManager(this, manager);
                }
                services.StartShifterSpecialOperation();
                var current = Mediator.CurrentCell;
                int index = SpecialItemCreationData.GetIndex(current == null ? null : current.Owner);
                var creation = SpecialItemCreationData.GetCreationCell();
                if (creation == null) throw new NullReferenceException();
                services.ExplodeAndMoveToPosition(this, creation, index, mergeCompleted);
            }
            else if (goalsLeft < 1)
            {
                var manager = curtain ?? drill ?? cryptex;
                if (manager == null) services.StartExplodeAnimation(this, data.Trigger);
                else services.CollectToManager(this, manager);
            }
            else
            {
                services.UsefulMoveMade();
                services.CollectToGoal(this);
            }
            var cell = CurrentExplosionCell();
            services.ExplodeTopMostBelowItem(cell, data);
            float freezeDuration = data.Trigger == 4 && !SpecialItemCreationData.ShouldCreateSpecialItem ? 0.2f : 0.15f;
            services.HandleMadness(this, data);
            FinalExplodeCompleted(freezeDuration);
            services.MatchItemExploded(MatchType, 1);
        }
        protected override void CollectAsGoal(bool updateUi = true) // 0x21a6b74 ignores updateUi.
        {
            GoalServices.DecreaseGoal(GoalType);
        }
        private OriginalGridCell CurrentExplosionCell()
        {
            var mediation = Mediator.CurrentCell;
            if (mediation == null || mediation.Owner == null) throw new NullReferenceException();
            return mediation.Owner;
        }
        private void HandleMergeCompleted() // 0x21a7d18.
        {
            var services = explosionServices;
            if (services == null) throw new NullReferenceException();
            services.RecycleMatchView(this);
            services.FinishShifterSpecialOperation();
        }
        public void RecycleView() // ItemModel.RecycleView 0x2b46db0; conversion is not a match explosion.
        {
            if (!HasView) return;
            HasView = false;
            explosionServices.RecycleMatchView(this);
        }
    }
}
