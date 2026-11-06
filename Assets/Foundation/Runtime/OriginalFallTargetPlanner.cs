using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public partial class OriginalGridCell
    {
        public bool ShouldFindCross { get; internal set; } // CellModel +0x30.
    }
    public interface IOriginalMultipleFallableItem
    {
        void SetCellAsTarget(OriginalGridCell targetCell);
        bool ShouldDisableCrossFallsForTargetCell(OriginalGridCell toCell);
    }
    // Native concrete IceCube fake-class membership used by ShouldFindCross.
    public interface IOriginalIceCubeFallFake { }
    public interface IOriginalIceCubeFallMaster { }
    public interface IOriginalFallIceCubeHelper
    {
        bool HasIceCubeAbove(int x, int y);
        OriginalGridCell GetNeighborUnderIce(OriginalGridCell cell);
        bool HasIceCubeAtBottomInColumn(int x);
    }
    public interface IOriginalFallTargetServices
    {
        int CurrentBoardIndex { get; }
        bool IsMultipleBoardLevel { get; }
        bool CanCurrentBoardCrossFall(int x);
        bool IsLevelWon { get; }
        bool IsFallBlocked(OriginalGridCell cell);
        bool IsAvailableForFall(OriginalBoardItem item, OriginalGridCell toCell);
        bool CanFallCross(OriginalBoardItem item);
        bool IsSpecialItem(OriginalBoardItem item);
        bool IsCrossFallBlocking(OriginalBoardItem item);
        bool IsThereIceCubeInLevel();
        IOriginalFallIceCubeHelper GetIceCubeHelper();
        bool HasCrossFallFreeze(OriginalGridCell cell);
        bool HasIncomingExploder(OriginalGridCell cell);
        IReadOnlyDictionary<int, OriginalFillingCell> CurrentBoardFillingCells { get; }
    }
    public interface IOriginalFallTargetPlanning : IOriginalRemainingColumnQueue
    {
        void ClearVisitCells();
        void FindTargetItemOrFillingCell(OriginalGridCell cell, out OriginalFillingCell fillingCell);
        void FindCrossItem(OriginalGridCell cell);
    }

    // FallManager target/column search, 0x3210e48..0x32120bc. Actual
    // item/static-item/ice/board services are required; no permissive defaults.
    public sealed class OriginalFallTargetPlanner : IOriginalFallTargetPlanning
    {
        private readonly IOriginalFallTargetServices services;
        private readonly OriginalFallRefillColumns columns;
        private readonly Stack<OriginalGridCell> crossLeftVisitCells = new Stack<OriginalGridCell>();
        private readonly Stack<OriginalGridCell> crossRightVisitCells = new Stack<OriginalGridCell>();
        public OriginalFallTargetPlanner(IOriginalFallTargetServices services, OriginalFallRefillColumns columns)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.columns = columns ?? throw new ArgumentNullException(nameof(columns));
        }
        // Stack portion of FallManager.Clear 0x32102d4.
        public void ClearVisitCells() { crossLeftVisitCells.Clear(); crossRightVisitCells.Clear(); }
        private bool IsInCurrentBoard(OriginalGridCell cell)
        { if (cell == null) throw new NullReferenceException(); return cell.BoardId == services.CurrentBoardIndex; }
        public OriginalBoardItem GetNextItemOfCell(OriginalGridCell fromCell, OriginalGridCell toCell) // 0x32115cc.
        {
            if (fromCell == null) throw new NullReferenceException();
            var mediation = fromCell.Mediation;
            if (!mediation.HasNextItem()) return null;
            var item = mediation.NextItem; if (item == null) throw new NullReferenceException();
            if (!services.IsAvailableForFall(item, toCell)) return null;
            item = mediation.NextItem; if (item == null) throw new NullReferenceException();
            var target = item.Mediator.TargetCell;
            if (target == null || ReferenceEquals(target.Owner, fromCell)) return mediation.NextItem;
            return null;
        }
        public bool IsFallPossibleFromCellToCell(OriginalGridCell fromCell, OriginalGridCell toCell) // 0x3211684.
        {
            if (fromCell == null) throw new NullReferenceException();
            if (services.IsFallBlocked(fromCell)) return false;
            if (!fromCell.Mediation.HasCurrentItem()) return true;
            var item = fromCell.Mediation.CurrentItem; if (item == null) throw new NullReferenceException();
            return services.IsAvailableForFall(item, toCell);
        }
        public void FindTargetItemOrFillingCell(OriginalGridCell cell, out OriginalFillingCell fillingCell) // 0x3210e48.
        {
            if (cell == null) throw new NullReferenceException();
            cell.ShouldFindCross = false; fillingCell = null;
            int boardId = cell.BoardId;
            if (cell.Mediation.HasTargetItem() || services.IsFallBlocked(cell)) return;
            var item = GetNextItemOfCell(cell, cell);
            if (item != null)
            {
                var multiple = item as IOriginalMultipleFallableItem;
                if (multiple != null) multiple.SetCellAsTarget(cell);
                else cell.Mediation.SetTargetItem(cell.Mediation.NextItem);
                return;
            }
            if (cell.IsFallFrozen) return;
            if (cell.IsFillingCell && IsInCurrentBoard(cell))
            {
                fillingCell = cell as OriginalFillingCell; if (fillingCell == null) throw new InvalidCastException();
                if (fillingCell.IsReadyToRefill(columns)) return;
            }
            var visiting = cell;
            while (true)
            {
                visiting = visiting.Neighbors.Get(1);
                if (visiting == null) { cell.ShouldFindCross = ShouldFindCross(cell, null); return; }
                if (visiting.BoardId > unchecked(services.CurrentBoardIndex + 1) || visiting.IsFallFrozen) return;
                if (visiting.IsNormalCell && cell.CellType == 0) return;
                if (!IsFallPossibleFromCellToCell(visiting, cell))
                { cell.ShouldFindCross = ShouldFindCross(cell, visiting); return; }
                item = GetNextItemOfCell(visiting, cell);
                if (item != null)
                {
                    if (fillingCell != null && fillingCell.ContainsInTopQueue(item)) return;
                    var current = item.Mediator.CurrentCell;
                    if (current == null || current.Owner == null) throw new NullReferenceException();
                    if (current.Owner.BoardId != boardId && fillingCell == null)
                    { cell.ShouldFindCross = ShouldFindCross(cell, visiting); return; }
                    if (columns.IsTopBoardCheckPending(visiting.X) && !IsInCurrentBoard(current.Owner)
                        && fillingCell != null && IsInCurrentBoard(fillingCell)) fillingCell.AddItemToComingFromTopBoard(item);
                    var multiple = item as IOriginalMultipleFallableItem;
                    if (multiple != null) multiple.SetCellAsTarget(cell);
                    else cell.Mediation.SetTargetItem(item);
                    return;
                }
                if (!visiting.IsFillingCell) continue;
                if (!IsInCurrentBoard(visiting)) return;
                fillingCell = visiting as OriginalFillingCell; if (fillingCell == null) throw new InvalidCastException();
                if (fillingCell.IsReadyToRefill(columns)) return;
            }
        }
        public bool ShouldFindCross(OriginalGridCell cell, OriginalGridCell fallBlockedCell) // 0x3211768.
        {
            if (services.IsMultipleBoardLevel)
            {
                if (cell == null) throw new NullReferenceException();
                if (!services.CanCurrentBoardCrossFall(cell.X)) return false;
            }
            if (services.IsLevelWon) return false;
            if (!services.IsThereIceCubeInLevel()) return true;
            if (fallBlockedCell != null && fallBlockedCell.Mediation.HasCurrentItem())
            {
                var multiple = fallBlockedCell.Mediation.CurrentItem as IOriginalMultipleFallableItem;
                if (multiple != null && multiple.ShouldDisableCrossFallsForTargetCell(cell)) return false;
            }
            var ice = services.GetIceCubeHelper();
            if (cell == null || ice == null) throw new NullReferenceException();
            if (ice.HasIceCubeAbove(cell.X, cell.Y))
            {
                var neighbor = ice.GetNeighborUnderIce(cell);
                if (services.HasCrossFallFreeze(cell) || (neighbor != null && services.HasCrossFallFreeze(neighbor))) return false;
            }
            if (ice.HasIceCubeAtBottomInColumn(cell.X)) return false;
            var above = cell.Neighbors.Get(1);
            if (above == null) return true;
            var item = above.Mediation.CurrentItem;
            if (item == null || !services.IsCrossFallBlocking(item)) return true;
            int direction;
            if (item is IOriginalIceCubeFallMaster) direction = 3;
            else if (item is IOriginalIceCubeFallFake) direction = 7;
            else return true;
            var side = cell.Neighbors.Get(direction);
            return side == null || (!side.IsFallFrozen && !services.HasIncomingExploder(side));
        }
        public static bool IsCellTargetedByMultipleFallableItem(OriginalGridCell cell) // 0x3211ab8.
        {
            if (cell == null) throw new NullReferenceException();
            var target = cell.Mediation.TargetItem;
            return target is IOriginalMultipleFallableItem && !ReferenceEquals(cell.Mediation.TargetItem, cell.Mediation.NextItem);
        }
        private void AddToCrossVisitCells(OriginalGridCell cell, ref bool continueLooking, int neighborType) // 0x3211b4c.
        {
            if (!continueLooking) return;
            if (cell == null) throw new NullReferenceException();
            var side = cell.Neighbors.Get(neighborType);
            if (side != null && side.IsFallFrozen) { continueLooking = false; return; }
            (neighborType == 3 ? crossRightVisitCells : crossLeftVisitCells).Push(cell);
        }
        public void FindCrossItem(OriginalGridCell currentCell) // 0x3211494.
        {
            crossLeftVisitCells.Clear(); crossRightVisitCells.Clear();
            if (currentCell == null) throw new NullReferenceException();
            int board = currentCell.BoardId;
            bool left = true, right = true;
            var visiting = currentCell;
            while (true)
            {
                if (visiting.IsFallFrozen || !IsFallPossibleFromCellToCell(visiting, null)
                    || IsCellTargetedByMultipleFallableItem(visiting) || visiting.BoardId != board) break;
                if (visiting.CellType != 0)
                {
                    AddToCrossVisitCells(visiting, ref right, 3);
                    AddToCrossVisitCells(visiting, ref left, 7);
                }
                visiting = visiting.Neighbors.Get(1);
                if (visiting == null || (!left && !right)) break;
            }
            FindCrossItemInLeftOrRightColumn(currentCell);
        }
        private void FindCrossItemInLeftOrRightColumn(OriginalGridCell targetCell) // 0x3211c08.
        {
            while (true)
            {
                OriginalBoardItem item;
                if (crossLeftVisitCells.Count > 0)
                {
                    item = FindTargetItemFromCrossCell(targetCell, crossLeftVisitCells.Pop(), 0);
                    if (item != null && !services.IsSpecialItem(item)) { SetTarget(targetCell, item); return; }
                }
                else
                {
                    if (crossRightVisitCells.Count <= 0) return;
                    item = null;
                }
                if (crossRightVisitCells.Count > 0)
                {
                    var right = FindTargetItemFromCrossCell(targetCell, crossRightVisitCells.Pop(), 2);
                    if (right != null) item = right;
                }
                if (item == null) continue;
                SetTarget(targetCell, item); return;
            }
        }
        private static void SetTarget(OriginalGridCell target, OriginalBoardItem item)
        { if (target == null) throw new NullReferenceException(); target.Mediation.SetTargetItem(item); }
        public OriginalBoardItem FindTargetItemFromCrossCell(OriginalGridCell targetCell, OriginalGridCell visitingCell, int neighborType) // 0x3211d28.
        {
            if (targetCell == null) throw new NullReferenceException();
            var above = targetCell.Neighbors.Get(1);
            if (above != null)
            {
                var aboveTarget = above.Mediation.TargetItem;
                var next = aboveTarget == null ? null : aboveTarget.Mediator.NextCell;
                aboveTarget = above.Mediation.TargetItem;
                var current = aboveTarget == null ? null : aboveTarget.Mediator.CurrentCell;
                if (above.CellType != 0 && !services.IsFallBlocked(above))
                {
                    bool checkCurrent = next == null;
                    if (!checkCurrent)
                    { if (visitingCell == null) throw new NullReferenceException(); checkCurrent = visitingCell.Y <= next.Owner.Y; }
                    if (checkCurrent)
                    {
                        if (current == null) return null;
                        if (visitingCell == null) throw new NullReferenceException();
                        if (visitingCell.Y <= current.Owner.Y) return null;
                    }
                }
            }
            if (visitingCell == null) throw new NullReferenceException();
            var source = visitingCell.Neighbors.Get(neighborType);
            if (source == null || !source.Mediation.HasCurrentItem()) return null;
            var item = source.Mediation.CurrentItem; if (item == null) throw new NullReferenceException();
            if (!services.IsAvailableForFall(item, null)) return null;
            item = source.Mediation.CurrentItem; if (item == null) throw new NullReferenceException();
            if (!services.CanFallCross(item) || source.IsFallFrozen) return null;
            item = source.Mediation.CurrentItem; if (item == null) throw new NullReferenceException();
            var mediator = item.Mediator;
            if (mediator.TargetCell != null && !ReferenceEquals(mediator.TargetCell, mediator.CurrentCell)) return null;
            return source.Mediation.CurrentItem;
        }
        public bool AddRemainingItemsToFillingCellQueue(int column) // 0x3210d84.
        {
            var filling = services.CurrentBoardFillingCells; if (filling == null) throw new NullReferenceException();
            return !filling.ContainsKey(column) || AddTopBoardRemainingItemsToFillingCell(filling[column]);
        }
        public bool AddTopBoardRemainingItemsToFillingCell(OriginalFillingCell fillingCell) // 0x3211f24.
        {
            if (fillingCell == null) throw new NullReferenceException();
            var visiting = fillingCell.Neighbors.Get(1);
            while (visiting != null)
            {
                if (visiting.IsFallFrozen) return false;
                var upperFilling = visiting as OriginalFillingCell;
                if (upperFilling != null && upperFilling.LastCreatedItem != null)
                { fillingCell.AddToTopBoardItemQueue(upperFilling.LastCreatedItem); upperFilling.RemoveCreatedItem(); }
                var mediation = visiting.Mediation;
                if (mediation.HasNextItem())
                {
                    var item = mediation.NextItem; if (item == null) throw new NullReferenceException();
                    if (services.IsAvailableForFall(item, fillingCell))
                    {
                        item = mediation.NextItem; if (item == null) throw new NullReferenceException();
                        var target = item.Mediator.TargetCell;
                        if (target == null || !IsInCurrentBoard(target.Owner)) fillingCell.AddToTopBoardItemQueue(mediation.NextItem);
                    }
                }
                visiting = visiting.Neighbors.Get(1);
            }
            return true;
        }
    }
}
