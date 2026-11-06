using System;

namespace Whitebox.Foundation
{
    // The native grid/level owners supply these values. Portal layouts use the
    // current-board strategies (0/1); ordinary layouts use total-grid (4/5).
    public interface IOriginalFallTraversal
    {
        bool IsPortalLevel { get; }
        int BoardStartY { get; }
        IOriginalGridIteratorStrategy GetIteratorStrategy(int type);
    }
    public interface IOriginalFallUpdateComponent : IOriginalFillingFallComponent, IOriginalItemFallReadiness
    {
        void ManualUpdate();
    }

    // FallManager update orchestration. Target planning, native item creation
    // and per-item motion retain their own concrete implementations/owners.
    public sealed class OriginalFallManager
    {
        private readonly OriginalFallRefillColumns columns;
        private readonly IOriginalFallTargetPlanning targets;
        private readonly IOriginalFillingItemCreation creator;
        private readonly OriginalGameOperations operations;
        private IOriginalFallTraversal grid;
        private OriginalGridIterator targetIterator;
        private OriginalGridIterator updateIterator;
        private bool enabled = true; // Native constructor 0x3206aa8.
        public bool IsAllItemsSteady { get; private set; } // 0x32102c0.

        public OriginalFallManager(OriginalFallRefillColumns columns, IOriginalFallTargetPlanning targets,
            IOriginalFillingItemCreation creator, OriginalGameOperations operations)
        {
            this.columns = columns ?? throw new ArgumentNullException(nameof(columns));
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
            this.creator = creator;
            this.operations = operations;
        }
        public void InitializeGrid(IOriginalFallTraversal grid) // 0x3210494.
        {
            if (grid == null) throw new NullReferenceException();
            this.grid = grid;
            bool portal = grid.IsPortalLevel;
            targetIterator = new OriginalGridIterator(grid.GetIteratorStrategy(portal ? 0 : 4));
            updateIterator = new OriginalGridIterator(grid.GetIteratorStrategy(portal ? 1 : 5));
        }
        public void Enable(bool enable) { enabled = enable; } // 0x3210e3c.
        public void Clear(bool gameExit) // 0x32102d4: neither flag is reset.
        { targets.ClearVisitCells(); columns.Clear(); }
        private bool IsInBelowBoards(OriginalGridCell cell) // CellModel 0x319c908.
        {
            if (cell == null || grid == null) throw new NullReferenceException();
            return cell.Y < grid.BoardStartY;
        }
        public void ManualUpdate() // 0x32106f0.
        {
            if (!enabled)
            {
                if (operations == null) throw new NullReferenceException();
                operations.OperationReset(0);
                return;
            }
            ClearTargetItems();
            CalculateStraightFalls();
            CalculateCrossFalls();
            CallFallUpdates();
            columns.ProcessPendingColumns(targets);
        }
        public void ClearTargetItems() // 0x32109f0.
        {
            targetIterator.Reset();
            while (targetIterator.MoveNext() != null)
            {
                var cell = targetIterator.Cell;
                if (cell == null) throw new NullReferenceException();
                var mediation = cell.Mediation;
                if (mediation.HasTargetItem() && !ReferenceEquals(mediation.NextItem, mediation.TargetItem))
                    mediation.ClearTargetItem();
            }
        }
        public void CalculateStraightFalls() // 0x3210a88.
        {
            targetIterator.Reset();
            while (targetIterator.MoveNext() != null)
            {
                var cell = targetIterator.Cell;
                if (IsInBelowBoards(cell)) continue;
                targets.FindTargetItemOrFillingCell(cell, out var filling);
                if (!cell.Mediation.HasTargetItem() && filling != null)
                    OriginalFillingItemPlacement.CreateItemFromFillingCell(columns, filling, cell, creator);
            }
        }
        public void CalculateCrossFalls() // 0x3210b24.
        {
            targetIterator.Reset();
            while (targetIterator.MoveNext() != null)
            {
                var cell = targetIterator.Cell;
                if (cell == null) throw new NullReferenceException();
                if (cell.IsNormalCell && cell.ShouldFindCross && !cell.Mediation.HasTargetItem()
                    && !IsInBelowBoards(cell)) targets.FindCrossItem(cell);
            }
        }
        public void CallFallUpdates() // 0x3210bb4.
        {
            if (operations == null) throw new NullReferenceException();
            operations.OperationReset(0);
            IsAllItemsSteady = true;
            updateIterator.Reset();
            while (updateIterator.MoveNext() != null)
            {
                var cell = updateIterator.Cell;
                if (cell == null) throw new NullReferenceException();
                if (!cell.Mediation.HasTargetItem() || IsInBelowBoards(cell)) continue;
                var item = cell.Mediation.TargetItem;
                if (item == null) throw new NullReferenceException();
                var fall = item.FallComponent as IOriginalFallUpdateComponent;
                if (fall == null) throw new NullReferenceException();
                fall.ManualUpdate();
                bool previous = IsAllItemsSteady;
                bool steady = fall.IsItemSteady; // Always query, including after an unsteady item.
                IsAllItemsSteady = previous & steady;
            }
            if (!IsAllItemsSteady) operations.OperationStart(0);
        }
    }
}
