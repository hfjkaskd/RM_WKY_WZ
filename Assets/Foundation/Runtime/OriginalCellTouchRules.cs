using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // StaticItemMediator touch-query portion. Concrete static item models supply
    // their native virtual results; chain is not inferred from an enum name.
    public interface IOriginalStaticTouchItem
    {
        bool IsTouchBlocking { get; }
        bool IsTouchBlockingExceptChain { get; }
    }

    public sealed partial class OriginalStaticTouchMediator
    {
        public List<IOriginalStaticTouchItem> AboveItems { get; } = new List<IOriginalStaticTouchItem>();
        public List<IOriginalStaticTouchItem> BelowItems { get; } = new List<IOriginalStaticTouchItem>();
        public IOriginalStaticTouchItem GetTopMostAboveItem() => AboveItems.Count == 0 ? null : AboveItems[AboveItems.Count - 1]; // 0x343c6cc.
        public IOriginalStaticTouchItem GetTopMostBelowItem() => BelowItems.Count == 0 ? null : BelowItems[BelowItems.Count - 1]; // 0x343c794.
        public bool HasTouchBlockingItem()
        {
            for (int i = 0; i < AboveItems.Count; i++) if (AboveItems[i].IsTouchBlocking) return true;
            return false;
        }
        public bool HasTouchBlockingItemExceptChain()
        {
            for (int i = 0; i < AboveItems.Count; i++) if (AboveItems[i].IsTouchBlockingExceptChain) return true;
            return false;
        }
    }

    // DisableCounter 0x2d7c9ac..0x2d7c9e8; the native naming is intentional.
    public sealed class OriginalDisableCounter
    {
        public int Count { get; private set; }
        public bool IsEnabled => Count > 0;
        public void Enable() { Count = unchecked(Count + 1); }
        public void Disable() { if (Count != 0) Count = unchecked(Count - 1); }
        public void Reset() { Count = 0; }
    }

    public partial class OriginalGridCell
    {
        public int TouchDisableCount { get; private set; }
        public bool IsTouchEnabled => TouchDisableCount == 0; // 0x319c9c4.
        public OriginalDisableCounter Reservation { get; } = new OriginalDisableCounter();
        public OriginalStaticTouchMediator StaticTouch { get; } = new OriginalStaticTouchMediator();
        public bool HasMostlyPlacedRoofTile { get; set; } // Native field +0x44.
        public void DisableTouch() { TouchDisableCount = unchecked(TouchDisableCount + 1); }
        public void EnableTouch() { if (TouchDisableCount > 0) TouchDisableCount--; }
    }

    public interface IOriginalHalfGridHoneyViews
    {
        bool HasHalfGridHoneyView(OriginalGridCell cell);
    }

    public sealed class OriginalMatchBlockingState : IOriginalMatchBlockingState
    {
        private readonly bool hasHoneyInLevel;
        private readonly IOriginalHalfGridHoneyViews honeyViews;
        public OriginalMatchBlockingState(bool hasHoneyInLevel, IOriginalHalfGridHoneyViews honeyViews)
        {
            if (hasHoneyInLevel && honeyViews == null) throw new ArgumentNullException(nameof(honeyViews));
            this.hasHoneyInLevel = hasHoneyInLevel;
            this.honeyViews = honeyViews;
        }
        public bool HasTouchBlockingItemExceptChain(OriginalGridCell cell) => cell.StaticTouch.HasTouchBlockingItemExceptChain();
        public bool HasIncomingTouchBlockingItem(OriginalGridCell cell)
        {
            // CellModel.HasIncomingTouchBlockingItem 0x319dc64 checks level
            // item type 49 before querying the half-grid honey view.
            return (hasHoneyInLevel && honeyViews.HasHalfGridHoneyView(cell)) || cell.HasMostlyPlacedRoofTile;
        }
        public bool CanEnterToMatch(OriginalGridCell cell)
        {
            return !HasTouchBlockingItemExceptChain(cell) && !HasIncomingTouchBlockingItem(cell);
        }
    }

    // ItemModel.IsAvailableForSwap includes fall and view animation state.
    // Its lifecycle must be supplied, rather than treating every match as idle.
    public interface IOriginalItemSwapAvailability
    {
        bool IsAvailableForSwap(OriginalBoardItem item);
    }

    public static class OriginalCellTouchRules
    {
        public static bool HasSwappableItem(OriginalGridCell cell, IOriginalItemSwapAvailability items)
        {
            if (!cell.IsTouchEnabled || cell.StaticTouch.HasTouchBlockingItem()) return false;
            var item = cell.Mediation.CurrentItem;
            return item != null && items.IsAvailableForSwap(item) && !cell.Reservation.IsEnabled;
        }
        public static bool IsEmptyCell(OriginalGridCell cell)
        {
            return cell.IsTouchEnabled && cell.IsNormalCell &&
                !cell.Mediation.HasCurrentItem() && !cell.Mediation.HasNextItem();
        }
        public static bool CanSwapFrom(OriginalGridCell cell, IOriginalItemSwapAvailability items) => HasSwappableItem(cell, items);
        public static bool CanSwapTo(OriginalGridCell cell, IOriginalItemSwapAvailability items)
        {
            if (IsEmptyCell(cell) && !cell.StaticTouch.HasTouchBlockingItem()) return true;
            return HasSwappableItem(cell, items);
        }
    }
}
