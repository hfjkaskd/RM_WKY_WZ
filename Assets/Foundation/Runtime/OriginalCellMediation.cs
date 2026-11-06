using System;

namespace Whitebox.Foundation
{
    public enum OriginalMediationStyle { Current = 0, Next = 1, Target = 2 }

    // Item identity and cell mediation only; item-specific gameplay is restored separately.
    public partial class OriginalBoardItem
    {
        public int Id { get; }
        public virtual int ItemType { get; }
        public OriginalItemMediation Mediator { get; }
        public virtual bool HasView { get; protected set; } // 0x2b45d70 / 0x2b45d78
        internal void MarkViewCreated() => HasView = true;
        public event Action OnExplodeEvent;
        public void InvokeExplodeEvent() => OnExplodeEvent?.Invoke(); // 0x2b46b38

        public OriginalBoardItem(int id, int itemType)
        {
            Id = id;
            ItemType = itemType;
            Mediator = new OriginalItemMediation();
        }
    }

    // Original ItemMediator cell delegates, 0x2b45968 through 0x2b45c94.
    public sealed class OriginalItemMediation
    {
        private OriginalCellMediationItem current;
        private OriginalCellMediationItem next;
        private OriginalCellMediationItem target;
        public OriginalCellMediation CurrentCell => current == null ? null : current.Cell;
        public OriginalCellMediation NextCell => next == null ? null : next.Cell;
        public OriginalCellMediation TargetCell => target == null ? null : target.Cell;

        internal OriginalCellMediation GetCell(OriginalMediationStyle style)
        {
            switch (style)
            {
                case OriginalMediationStyle.Current: return CurrentCell;
                case OriginalMediationStyle.Next: return NextCell;
                case OriginalMediationStyle.Target: return TargetCell;
                default: return null;
            }
        }

        internal void Clear(OriginalMediationStyle style)
        {
            switch (style)
            {
                case OriginalMediationStyle.Current: current = null; break;
                case OriginalMediationStyle.Next: next = null; break;
                case OriginalMediationStyle.Target: target = null; break;
            }
        }

        // ItemMediator.ClearFromAllRegisteredCells 0x2b45cd4: current, target,
        // then next; each cell slot clears its reverse item delegate.
        public void ClearFromAllRegisteredCells()
        {
            if (current != null) CurrentCell.ClearCurrentItem();
            if (target != null) TargetCell.ClearTargetItem();
            if (next != null) NextCell.ClearNextItem();
        }

        internal void Set(OriginalMediationStyle style, OriginalCellMediationItem value)
        {
            // Original setter clears the old cell slot before changing its delegate.
            switch (style)
            {
                case OriginalMediationStyle.Current:
                    if (current != null) current.Cell.ClearCurrentItem();
                    current = value;
                    break;
                case OriginalMediationStyle.Next:
                    if (next != null) next.Cell.ClearNextItem();
                    next = value;
                    break;
                case OriginalMediationStyle.Target:
                    if (target != null) target.Cell.ClearTargetItem();
                    target = value;
                    break;
            }
        }
    }

    // CellMediatorItem 0x3448684 / 0x34486ac. No scene or presentation structure.
    public sealed class OriginalCellMediationItem
    {
        private readonly OriginalMediationStyle style;
        public OriginalCellMediation Cell { get; }
        public OriginalBoardItem Item { get; private set; }

        internal OriginalCellMediationItem(OriginalCellMediation cell, OriginalMediationStyle style)
        {
            Cell = cell;
            this.style = style;
        }

        public void ClearItem()
        {
            if (Item != null) Item.Mediator.Clear(style);
            Item = null;
        }

        public bool SetItem(OriginalBoardItem item)
        {
            if (item == null)
            {
                // Original reports an error and rejects null; it never clears the slot.
                UnityEngine.Debug.LogError("Cannot assign a null item to a cell.");
                return false;
            }
            if (!ReferenceEquals(Item, item))
            {
                if (Item != null) return false;
                Item = item;
                if (!ReferenceEquals(item.Mediator.GetCell(style), Cell))
                    item.Mediator.Set(style, this);
            }
            return true;
        }
    }

    // CellMediator 0x3448428..0x3448824: three independent, bidirectional slots.
    public sealed class OriginalCellMediation
    {
        public OriginalGridCell Owner { get; }
        public OriginalCellMediationItem Current { get; }
        public OriginalCellMediationItem Next { get; }
        public OriginalCellMediationItem Target { get; }
        public OriginalBoardItem CurrentItem => Current.Item;
        public OriginalBoardItem NextItem => Next.Item;
        public OriginalBoardItem TargetItem => Target.Item;

        public OriginalCellMediation(OriginalGridCell owner = null)
        {
            Owner = owner;
            Current = new OriginalCellMediationItem(this, OriginalMediationStyle.Current);
            Next = new OriginalCellMediationItem(this, OriginalMediationStyle.Next);
            Target = new OriginalCellMediationItem(this, OriginalMediationStyle.Target);
        }

        public bool HasCurrentItem() => Current.Item != null;
        public bool HasNextItem() => Next.Item != null;
        public bool HasTargetItem() => Target.Item != null;
        public bool SetCurrentItem(OriginalBoardItem item) => Current.SetItem(item);
        public bool SetNextItem(OriginalBoardItem item) => Next.SetItem(item);
        public bool SetTargetItem(OriginalBoardItem item) => Target.SetItem(item);
        public void ClearCurrentItem() => Current.ClearItem();
        public void ClearNextItem() => Next.ClearItem();
        public void ClearTargetItem() => Target.ClearItem();
        public void ClearAllItems()
        {
            Current.ClearItem();
            Next.ClearItem();
            Target.ClearItem();
        }

        public void SetAllItems(OriginalBoardItem item)
        {
            Current.SetItem(item);
            Next.SetItem(item);
            Target.SetItem(item);
        }

        public void Reset() => ClearAllItems();
    }
}
