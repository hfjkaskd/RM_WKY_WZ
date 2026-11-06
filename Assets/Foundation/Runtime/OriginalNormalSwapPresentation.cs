using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Required downstream game services; the presentation adapter supplies no
    // placeholder resolution, audio or progress behavior.
    public interface IOriginalNormalSwapGameplay
    {
        void ClearSinglePropellerWinCalculationData();
        void PlaySfx(int id, float delay);
        void PlayHaptic(int id, int type, float strength, float duration, bool isReplay);
        void ExplodeMatchGroups(OriginalSwapDecision decision, OriginalBoardItem other, OriginalBoardItem moving);
        void ExplodeSpecialItems(OriginalBoardItem other, OriginalBoardItem moving);
    }

    public sealed class OriginalNormalSwapPresentation : IOriginalNormalSwapServices
    {
        private readonly Transform boardRoot;
        private readonly OriginalLevelPresentation presentation;
        private readonly OriginalLocalMovePlayer localMoves;
        private readonly IOriginalNormalSwapGameplay gameplay;
        private readonly int itemLayer;
        public OriginalNormalSwapPresentation(Transform boardRoot, OriginalLevelPresentation presentation,
            OriginalLocalMovePlayer localMoves, IOriginalNormalSwapGameplay gameplay, int itemLayer)
        {
            this.boardRoot = boardRoot != null ? boardRoot : throw new ArgumentNullException(nameof(boardRoot));
            this.presentation = presentation != null ? presentation : throw new ArgumentNullException(nameof(presentation));
            this.localMoves = localMoves != null ? localMoves : throw new ArgumentNullException(nameof(localMoves));
            this.gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
            this.itemLayer = itemLayer;
        }
        public IOriginalSwapAnimationView GetView(OriginalBoardItem item) => (IOriginalSwapAnimationView)item.SwapView;
        public OriginalSwapSorting ArrangeSelfSorting(IOriginalSwapAnimationView view)
        {
            if (view is OriginalMatchItemSpriteView match) return match.ArrangeSelfSorting();
            SetSorting(view, OriginalItemSortingRules.Default(itemLayer));
            return GetSorting(view);
        }
        public void SetSorting(IOriginalSwapAnimationView view, OriginalSwapSorting sorting)
        {
            if (view is IOriginalItemFallSortingSurface item) item.UpdateSorting(sorting);
            else ((IOriginalSpecialCreationView)view).UpdateSorting(sorting);
        }
        public void SetSortingForAnimation(IOriginalSwapAnimationView view, OriginalSwapSorting sorting)
        {
            if (view is OriginalTntItemSpriteView tnt) tnt.UpdateSortingForAnimation(sorting, true);
            else if (view is OriginalRocketItemSpriteView rocket) rocket.UpdateSortingForAnimation(sorting, true);
            else SetSorting(view, sorting);
        }
        public void UpdateParticleSorting(object particle, OriginalSwapSorting sorting) =>
            ((OriginalMatchItemSwapParticles)particle).UpdateSorting(sorting);
        public OriginalSwapPoint GetCellLocalPosition(OriginalGridCell cell) => new OriginalSwapPoint(cell.X, cell.Y, 0f);
        public void PlayCellSwapFlash(OriginalGridCell cell) =>
            presentation.PlayCellSwapFlash(boardRoot.TransformPoint(new Vector3(cell.X, cell.Y, 0f)));
        public OriginalSwapSorting GetSwapStartSorting() => OriginalItemSortingRules.SwapStart(itemLayer);
        public object PlaySwapParticle(IOriginalSwapAnimationView view, int direction) =>
            presentation.PlaySwapParticle(((Component)view).transform, GetSorting(view), direction);
        public bool IsViewMissing(IOriginalSwapAnimationView view) => (Component)view == null;
        public void MoveToLocal(IOriginalSwapAnimationView view, OriginalSwapPoint target, float duration, Action onComplete) =>
            localMoves.MoveToLocal(((Component)view).transform, target, duration, onComplete);
        public void MovePropellerComboToLocal(IOriginalSwapAnimationView view, OriginalGridCell destination,
            float duration, Action onComplete) =>
            localMoves.MoveToLocalOnLevelClock(((Component)view).transform, destination.GetViewLocalPosition(),
                duration, onComplete);
        public void ClearSinglePropellerWinCalculationData() => gameplay.ClearSinglePropellerWinCalculationData();
        public void PlaySfx(int id, float delay) => gameplay.PlaySfx(id, delay);
        public void PlayHaptic(int id, int type, float strength, float duration, bool isReplay) =>
            gameplay.PlayHaptic(id, type, strength, duration, isReplay);
        public void ExplodeMatchGroups(OriginalSwapDecision decision, OriginalBoardItem other, OriginalBoardItem moving) =>
            gameplay.ExplodeMatchGroups(decision, other, moving);
        public void ExplodeSpecialItems(OriginalBoardItem other, OriginalBoardItem moving) => gameplay.ExplodeSpecialItems(other, moving);
        private static OriginalSwapSorting GetSorting(IOriginalSwapAnimationView view)
        {
            if (view is IOriginalItemFallSortingSurface item) return item.GetSorting();
            if (view is OriginalRocketItemSpriteView rocket) return rocket.GetSorting();
            if (view is OriginalTntItemSpriteView tnt) return tnt.GetSorting();
            if (view is OriginalLightballItemSpriteView lightball) return lightball.GetSorting();
            if (view is OriginalPropellerItemSpriteView propeller) return propeller.GetSorting();
            throw new NotSupportedException("Concrete item view sorting is required.");
        }
    }
}
