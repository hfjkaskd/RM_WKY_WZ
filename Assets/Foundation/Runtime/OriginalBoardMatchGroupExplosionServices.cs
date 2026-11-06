using System;

namespace Whitebox.Foundation
{
    public interface IOriginalMatchGroupSystems
    {
        bool IsAutoMatchDisabledForWaterLevel { get; }
        void ShuffleCellsBasedOnDifficulty(OriginalMatchCells cells);
        void CreateSpecialItemForMatchGroup(OriginalMatchGroupSnapshot group, OriginalGridCell cell, bool isAuto);
        void PlayMatchExplode(bool isSwap);
        void PlayHaptic(int id, int type, float strength, float duration, bool replay);
    }
    // Concrete grid/static/item/near-match and active-session dispatch for the
    // existing native group orchestration. No cell removal is simulated here.
    public sealed class OriginalBoardMatchGroupExplosionServices : IOriginalMatchGroupExplosionServices
    {
        private readonly OriginalInitialBoardView board;
        private readonly OriginalMatchGroupJellySpread jelly;
        private readonly Func<OriginalActiveLevelData> currentLevel;
        private readonly IOriginalMatchGroupSystems systems;
        private readonly OriginalCellStaticExplosionServices statics = new OriginalCellStaticExplosionServices();
        public OriginalBoardMatchGroupExplosionServices(OriginalInitialBoardView board, IOriginalMatchJellyViews jellyViews,
            Func<OriginalActiveLevelData> currentLevel, IOriginalMatchGroupSystems systems)
        {
            this.board = board != null ? board : throw new ArgumentNullException(nameof(board));
            this.currentLevel = currentLevel ?? throw new ArgumentNullException(nameof(currentLevel));
            this.systems = systems ?? throw new ArgumentNullException(nameof(systems));
            jelly = new OriginalMatchGroupJellySpread(board.InitialItemAmounts, jellyViews);
        }
        public bool IsAutoMatchDisabledForWaterLevel => systems.IsAutoMatchDisabledForWaterLevel;
        public bool HasStaticItem(OriginalGridCell cell, int type) => cell.StaticTouch.HasStaticItem(type);
        public void ShuffleCellsBasedOnDifficulty(OriginalMatchCells cells) => systems.ShuffleCellsBasedOnDifficulty(cells);
        public bool InitJellySpreadData(ref OriginalMatchGroupSnapshot group, OriginalExplodeData data) => jelly.Init(group, ref data);
        public void ExplodeTopMostAboveItem(OriginalGridCell cell, OriginalExplodeData data) => cell.StaticTouch.ExplodeTopMostAboveItem(data);
        public void ExplodeMatchItem(OriginalMatchBoardItem item, OriginalExplodeData data) => item.Explode(data);
        public OriginalGridCell GetCell(OriginalCellPoint point) => board.Grid.GetCell(point.X, point.Y);
        public void TryCollectMadness(OriginalGridCell origin, int type, int count, int delayInFrames)
        {
            var session = currentLevel(); if (session == null) throw new NullReferenceException();
            var position = origin.GetViewPosition();
            session.TryCollectMadness(type, new OriginalSwapPoint(position.x, position.y, position.z), count, delayInFrames, null, false);
        }
        public void CreateSpecialItemForMatchGroup(OriginalMatchGroupSnapshot group, OriginalGridCell cell, bool isAuto) => systems.CreateSpecialItemForMatchGroup(group, cell, isAuto);
        public void ExplodeCurrentItemByNearMatch(OriginalGridCell cell, OriginalExplodeData data) => OriginalCellNearMatchExplosion.ExplodeCurrentItemByNearMatch(cell, data, statics);
        public void PlayMatchExplode(bool swap) => systems.PlayMatchExplode(swap);
        public void PlayHaptic(int id, int type, float strength, float duration, bool replay) => systems.PlayHaptic(id, type, strength, duration, replay);
    }
}
