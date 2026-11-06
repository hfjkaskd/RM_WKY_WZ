using System;

namespace Whitebox.Foundation
{
    // CellModel.CanTapCurrentItem 0x319cda4. Output assignment happens before
    // every guard, including disabled touch, blocking overlays and reservation.
    public static class OriginalCellTap
    {
        public static bool CanTapCurrentItem(OriginalGridCell cell, out IOriginalTapItem tapItem)
        {
            tapItem = cell.Mediation.CurrentItem;
            if (!cell.IsTouchEnabled || cell.StaticTouch.HasTouchBlockingItem() || cell.Reservation.IsEnabled) return false;
            return cell.Mediation.HasCurrentItem() && tapItem != null && !tapItem.IsTapDisabled;
        }
    }
    public interface IOriginalLevelTapServices : IOriginalBoosterTouchSelection
    {
        void UseBooster(OriginalGridCell cell);
        void CheckForUsefulMove();
        void ClearSinglePropellerWinCalculationData();
        void IncrementTappedLightball();
        void OnSpecialItemTapped();
        void IncrementItemUse(int itemType, bool isCombo);
        void OnTntOrRocketItemTapped();
    }
    // LevelTouchManager.TapCell / CheckForBoosterOperation 0x1ee1ef8/22d8.
    // Required services own concrete booster, session and special activation;
    // successful taps must have both event subscribers, as in the original.
    public sealed class OriginalLevelTapRouter
    {
        private readonly IOriginalLevelTapServices services;
        public event Action<OriginalGridCell> OnTap;
        public event Action<int> OnMoveCompleted;
        public OriginalLevelTapRouter(IOriginalLevelTapServices services)
        { this.services = services ?? throw new ArgumentNullException(nameof(services)); }
        public bool TapCell(OriginalGridCell cell)
        {
            if (cell.IsNormalCell && cell.IsTouchEnabled && services.IsBoosterSelected)
            {
                services.UseBooster(cell);
                return false;
            }
            if (!OriginalCellTap.CanTapCurrentItem(cell, out IOriginalTapItem item)) return false;
            var special = item as OriginalSpecialBoardItem;
            if (special != null)
            {
                services.CheckForUsefulMove();
                services.ClearSinglePropellerWinCalculationData();
            }
            if (item is IOriginalLightballTapItem) services.IncrementTappedLightball();
            if (!item.Tap(OriginalExploder.Next(3))) return false;
            if (special != null)
            {
                services.OnSpecialItemTapped();
                services.IncrementItemUse(special.ItemType, false);
            }
            if (item is OriginalTntBoardItem || item is OriginalRocketBoardItem) services.OnTntOrRocketItemTapped();
            OnTap(cell);
            var completed = OnMoveCompleted;
            // The getter runs after OnTap; it must not be cached before an event.
            int moveType = item.GetMoveType;
            completed(moveType);
            return true;
        }
    }
}
