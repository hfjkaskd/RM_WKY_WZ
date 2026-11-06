using System;

namespace Whitebox.Foundation
{
    // Real optional mechanic/collection owners are required when their native
    // branch runs. The normal trigger2/4 path needs none of these mechanics.
    public interface IOriginalMatchItemExplosionSystems
    {
        IOriginalMatchCollectManager GetCurtainForMatchType(int type);
        IOriginalMatchCollectManager GetDrillForMatchType(int type);
        IOriginalMatchCollectManager GetCryptexForMatchType(int type);
        void DecrementPurpleOnBoard();
        void TryDamageNeighborIceBorders(OriginalMatchBoardItem item);
        void CollectToGoal(OriginalMatchBoardItem item);
        void CollectToManager(OriginalMatchBoardItem item, IOriginalMatchCollectManager manager);
        void HandleMadness(OriginalMatchBoardItem item, OriginalExplodeData data);
    }
    // Connect MatchItemModel.TryExplode to the actual board, goal counters,
    // static dispatch, native view animation/merge/pool and current session counts.
    public sealed class OriginalBoardMatchItemExplosionServices : IOriginalMatchItemExplosionServices
    {
        private readonly OriginalInitialBoardView board;
        private readonly Func<OriginalMatchItemCounts> currentCounts;
        private readonly IOriginalBoxExplosionServices useful;
        private readonly IOriginalMatchItemExplosionSystems systems;
        public OriginalBoardMatchItemExplosionServices(OriginalInitialBoardView board, Func<OriginalMatchItemCounts> currentCounts,
            IOriginalBoxExplosionServices useful, IOriginalMatchItemExplosionSystems systems)
        {
            this.board = board != null ? board : throw new ArgumentNullException(nameof(board));
            this.currentCounts = currentCounts ?? throw new ArgumentNullException(nameof(currentCounts));
            this.useful = useful;
            this.systems = systems;
        }
        public int GetGoalUiLeftCount(int goal) => board.Goals.GetGoalUiLeftCount(goal);
        public bool IsThereStaticItemInLevel(int type) => board.InitialItemAmounts.IsThereStaticItemInLevel(type);
        public bool IsThereItemInLevel(int type) => board.InitialItemAmounts.IsThereItemInLevel(type);
        public IOriginalMatchCollectManager GetCurtainForMatchType(int type) => Systems.GetCurtainForMatchType(type);
        public IOriginalMatchCollectManager GetDrillForMatchType(int type) => Systems.GetDrillForMatchType(type);
        public IOriginalMatchCollectManager GetCryptexForMatchType(int type) => Systems.GetCryptexForMatchType(type);
        public void DecrementPurpleOnBoard() => Systems.DecrementPurpleOnBoard();
        public void TryDamageNeighborIceBorders(OriginalMatchBoardItem item) => Systems.TryDamageNeighborIceBorders(item);
        public void CollectToGoal(OriginalMatchBoardItem item) => Systems.CollectToGoal(item);
        public void CollectToManager(OriginalMatchBoardItem item, IOriginalMatchCollectManager manager) => Systems.CollectToManager(item, manager);
        public void StartShifterSpecialOperation() => board.State.StartShifterSpecialOperation();
        public void FinishShifterSpecialOperation() => board.State.FinishShifterSpecialOperation();
        public void ExplodeAndMoveToPosition(OriginalMatchBoardItem item, OriginalGridCell cell, int index, Action complete)
        {
            if (cell == null) throw new NullReferenceException();
            View(item).ExplodeAndMoveToPosition(cell.GetViewPosition(), index, complete);
        }
        public void RecycleMatchView(OriginalMatchBoardItem item) => board.Presentation.RecycleView(View(item));
        public void StartExplodeAnimation(OriginalMatchBoardItem item, int trigger) => View(item).StartExplodeAnimation(trigger);
        public void UsefulMoveMade() => (useful ?? throw new NullReferenceException()).UsefulMoveMade();
        public void ExplodeTopMostBelowItem(OriginalGridCell cell, OriginalExplodeData data) => cell.StaticTouch.ExplodeTopMostBelowItem(data);
        public void HandleMadness(OriginalMatchBoardItem item, OriginalExplodeData data)
        {
            // Native HandleMadness 0x21a6834 returns before any context lookup.
            if (data.Trigger == 2 || data.Trigger == 4) return;
            Systems.HandleMadness(item, data);
        }
        public void MatchItemExploded(int type, int count) // Static ItemExploded 0x21a3fb0.
        {
            var counts = currentCounts();
            if (counts == null) throw new NullReferenceException();
            counts.IncrementCount(type, count);
        }
        private IOriginalMatchItemExplosionSystems Systems => systems ?? throw new InvalidOperationException("The original match collection/mechanic owner is required for this branch.");
        private static OriginalMatchItemSpriteView View(OriginalMatchBoardItem item) => item.SwapView as OriginalMatchItemSpriteView ?? throw new NullReferenceException();
    }
}
