using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalLevelSetAvailabilityContext
    {
        int FrameCount { get; }
        OriginalGridIterator GetCurrentBoardHorizontalIterator();
        int GetFlyingThrowItemCount(int goalType);
    }
    public sealed partial class OriginalLevelSetSelection
    {
        private IOriginalLevelSetAvailabilityContext boardContext;
        private int availabilityFrame, emptyCellCount; // Native constructor leaves frame at zero.
        // Reuse the native temporary identity sets instead of allocating them
        // again each frame; item identities and resulting counts are unchanged.
        private readonly Dictionary<int, HashSet<int>> itemsInBoards = new Dictionary<int, HashSet<int>>();
        public int CreateRatio => createRatio;
        public bool HasAnyTargetColumn() => targetColumns.Count != 0; // 0x190fd14.
        public void BindAvailabilityContext(IOriginalLevelSetAvailabilityContext context) { boardContext = context; }
        public bool ContainsMatchItem() => OriginalLevelRules.ToMatchType(ids[0]) != 0; // 0x190fe1c: only the first element.
        public bool HasTargetColumn(OriginalFillingCell cell, int column) // 0x190fd64.
        {
            int fill = cell.FillType;
            if (fill != 1 && Array.IndexOf(targetFills, unchecked(fill - 1)) < 0 && targetFills.Length != 0) return false;
            return targetColumns.Contains(column);
        }
        public bool CanGetPrioritizedItem() // 0x190fe8c.
        {
            if (!IsAvailableForFill) return false;
            bool result = false;
            foreach (var entry in maxItemCounts) result = result || CanFillNewItemWithMaxCount(entry.Key);
            return result;
        }
        internal void ConsumePrioritizedItem() { RemainingItemCount = unchecked(RemainingItemCount - 1); }
        internal bool DisableFirstElementByItemType(int type)
        {
            for (int i = 0; i < ids.Length; i++)
                if (OriginalTiledItemTypes.AsItemType(ids[i]) == type) { available[i] = false; return true; }
            return false;
        }
        private bool CalculateRatio() => random.DifficultyNext(0, emptyCellCount) < RemainingItemCount; // 0x19103a8.
        private void AddToItemInBoards(int type, OriginalBoardItem item) // 0x19103e0.
        {
            if (!itemsInBoards.TryGetValue(type, out var identities)) itemsInBoards.Add(type, identities = new HashSet<int>());
            identities.Add(item.Id);
        }
        public bool CanFillNewItemWithMaxCount(int type) // 0x1910000.
        {
            if (availabilityFrame == boardContext.FrameCount) return CalculateRatio();
            var iterator = boardContext.GetCurrentBoardHorizontalIterator();
            availabilityFrame = boardContext.FrameCount;
            iterator.Reset(); RemainingItemCount = 0; emptyCellCount = 0;
            foreach (var entry in itemsInBoards) entry.Value.Clear();
            while (iterator.MoveNext() != null)
            {
                var cell = iterator.Cell;
                if (cell.IsNormalCell && !cell.Mediation.HasTargetItem()) emptyCellCount = unchecked(emptyCellCount + 1);
                if (cell.Mediation.HasTargetItem() && cell.Mediation.TargetItem.ItemType == type) AddToItemInBoards(type, cell.Mediation.TargetItem);
                if (cell.Mediation.HasCurrentItem() && cell.Mediation.CurrentItem.ItemType == type) AddToItemInBoards(type, cell.Mediation.CurrentItem);
                if (cell is OriginalFillingCell filling && filling.HasCreatedItem && filling.LastCreatedItem.Mediator.TargetCell == null
                    && filling.LastCreatedItem.ItemType == type) AddToItemInBoards(type, filling.LastCreatedItem);
            }
            int limit = maxItemCounts[type];
            int existing = itemsInBoards.TryGetValue(type, out var found) ? found.Count : 0;
            int flying = boardContext.GetFlyingThrowItemCount(OriginalGoalTypes.FromItemType(type));
            RemainingItemCount = unchecked(limit - existing - flying);
            return RemainingItemCount >= 1 && CalculateRatio();
        }
    }
}
