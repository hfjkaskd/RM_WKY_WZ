using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalImpossibleSwapGameplay
    {
        void PlaySfx(int id, float delay);
        void PlayHaptic(int id, int type, float strength, float duration, bool isReplay);
        void TryPlayStaticInvalidMoveAnimation(OriginalCellPoint destination);
    }
    // Concrete Unity transform/sorting adapter for the currently restored
    // normal match views. Static-item/audio/haptic services remain required.
    public sealed class OriginalImpossibleSwapPresentation : IOriginalImpossibleSwapServices
    {
        private readonly OriginalLocalMovePlayer tweens;
        private readonly IOriginalImpossibleSwapGameplay gameplay;
        private readonly int itemLayer;
        public OriginalImpossibleSwapPresentation(OriginalLocalMovePlayer tweens, IOriginalImpossibleSwapGameplay gameplay, int itemLayer)
        {
            this.tweens = tweens != null ? tweens : throw new ArgumentNullException(nameof(tweens));
            this.gameplay = gameplay ?? throw new ArgumentNullException(nameof(gameplay));
            this.itemLayer = itemLayer;
        }
        public IOriginalSwapAnimationView GetView(OriginalBoardItem item) => (IOriginalSwapAnimationView)item.SwapView;
        public OriginalSwapPoint GetCellLocalPosition(OriginalGridCell cell) => new OriginalSwapPoint(cell.X, cell.Y, 0f);
        public OriginalSwapPoint GetLocalScale(IOriginalSwapAnimationView view)
        { var p = ((Component)view).transform.localScale; return new OriginalSwapPoint(p.x, p.y, p.z); }
        public OriginalSwapSorting GetSwapStartSorting() => OriginalItemSortingRules.SwapStart(itemLayer);
        public void SetSorting(IOriginalSwapAnimationView view, OriginalSwapSorting sorting)
        {
            if (view is IOriginalItemFallSortingSurface item) item.UpdateSorting(sorting);
            else ((IOriginalSpecialCreationView)view).UpdateSorting(sorting);
        }
        public void ArrangeSelfSorting(IOriginalSwapAnimationView view)
        {
            if (view is OriginalMatchItemSpriteView match) match.ArrangeSelfSorting();
            else SetSorting(view, OriginalItemSortingRules.Default(itemLayer));
        }
        public void MoveToLocal(IOriginalSwapAnimationView view, OriginalSwapPoint target, float duration, int ease, Action complete) =>
            tweens.MoveToLocal(((Component)view).transform, target, duration, complete, ease);
        public void ScaleToLocal(IOriginalSwapAnimationView view, OriginalSwapPoint target, float duration, int ease, Action complete) =>
            tweens.ScaleToLocal(((Component)view).transform, target, duration, complete, ease);
        public void PlaySfx(int id, float delay) => gameplay.PlaySfx(id, delay);
        public void PlayHaptic(int id, int type, float strength, float duration, bool isReplay) =>
            gameplay.PlayHaptic(id, type, strength, duration, isReplay);
        public void TryPlayStaticInvalidMoveAnimation(OriginalCellPoint target) => gameplay.TryPlayStaticInvalidMoveAnimation(target);
    }
}
