using System;

namespace Whitebox.Foundation
{
    public interface IOriginalBoosterTouchSelection { bool IsBoosterSelected { get; } }
    public interface IOriginalStaticPressItem
    {
        bool PlayTouchDownAnimation();
        void PlayInvalidMoveAnimation();
    }
    // CellModel.TouchDown 0x319d290 and IsTouchingMatchItem 0x319d340.
    public static class OriginalCellPress
    {
        public static bool IsTouchingMatchItem(OriginalGridCell cell)
        {
            if (cell.StaticTouch.HasTouchBlockingItem()) return false;
            return cell.Mediation.CurrentItem is OriginalMatchBoardItem;
        }
        public static void TouchDown(OriginalGridCell cell)
        {
            var statics = cell.StaticTouch;
            if (statics.AboveItems.Count > 0) RequireStatic(statics.GetTopMostAboveItem()).PlayTouchDownAnimation();
            else if (cell.Mediation.CurrentItem != null) cell.Mediation.CurrentItem.PlayTouchDownAnimation();
            else if (statics.BelowItems.Count > 0) RequireStatic(statics.GetTopMostBelowItem()).PlayTouchDownAnimation();
        }
        // StaticItemMediator.TryPlayInvalidMoveAnimationForSwap 0x344108c.
        public static void TryPlayInvalidMoveAnimationForSwap(OriginalGridCell cell)
        {
            var above = cell.StaticTouch.GetTopMostAboveItem();
            if (above != null) RequireStatic(above).PlayInvalidMoveAnimation();
        }
        private static IOriginalStaticPressItem RequireStatic(IOriginalStaticTouchItem item)
        {
            if (item == null) throw new NullReferenceException();
            var press = item as IOriginalStaticPressItem;
            if (press == null) throw new NotSupportedException("Static item press/invalid animation is not restored.");
            return press;
        }
    }
    // LevelTouchManager.TouchCell / CancelTouchedItemAnimation, 0x1ee1df0/ebc.
    // The full manager's tap/swap/events/context Clear are composed separately.
    public sealed class OriginalLevelPressRouter
    {
        private readonly IOriginalBoosterTouchSelection booster;
        public OriginalBoardItem TouchedItem { get; private set; }
        public OriginalLevelPressRouter(IOriginalBoosterTouchSelection booster)
        { this.booster = booster ?? throw new ArgumentNullException(nameof(booster)); }
        public void TouchCell(OriginalGridCell cell)
        {
            if (!cell.IsTouchEnabled) return;
            if (OriginalCellPress.IsTouchingMatchItem(cell))
            {
                OriginalCellPress.TouchDown(cell);
                TouchedItem = cell.Mediation.CurrentItem;
            }
            else if (!booster.IsBoosterSelected) OriginalCellPress.TouchDown(cell);
        }
        public void CancelTouchedItemAnimation()
        {
            if (TouchedItem == null) return;
            TouchedItem.StopTouchDownAnimation();
            TouchedItem = null;
        }
        // Only the touched-field subset of LevelTouchManager.Clear. Native
        // Clear forgets this field without calling StopTouchDownAnimation.
        public void ClearTouchedItemState() { TouchedItem = null; }
    }
}
