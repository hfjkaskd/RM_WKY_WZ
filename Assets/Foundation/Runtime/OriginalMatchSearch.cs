using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Supplied by restored static/incoming item mediators. No assumption that
    // a cell is unblocked when its obstacle implementation is not yet present.
    public interface IOriginalMatchBlockingState
    {
        bool HasTouchBlockingItemExceptChain(OriginalGridCell cell);
        bool HasIncomingTouchBlockingItem(OriginalGridCell cell);
    }

    public sealed partial class OriginalMatchBoardItem : OriginalBoardItem
    {
        public int MatchType { get; private set; }
        internal void SetShuffleMatchType(int color) => MatchType = color;
        public OriginalMatchGroupSnapshot MatchGroup;
        public byte NativeMatchGroupId { get => MatchGroup.Id; internal set => MatchGroup.Id = value; }
        public OriginalMatchBoardItem(int id, int itemType, int matchType) : base(id, itemType)
        {
            MatchType = matchType;
        }
    }

    // MatchManager.IsValidToPatternSearch 0x12e1630 and IsThereAnyMatch
    // 0x12e1470. Match grouping, explosion and special-item creation are later
    // stages; this search does not substitute for those stages.
    public sealed class OriginalMatchSearch
    {
        private OriginalGridIterator iterator;
        private readonly IOriginalMatchBlockingState blocking;

        public OriginalMatchSearch(IOriginalGridIteratorStrategy strategy, IOriginalMatchBlockingState blocking)
        {
            iterator = new OriginalGridIterator(strategy);
            this.blocking = blocking ?? throw new ArgumentNullException(nameof(blocking));
        }

        public bool IsValidToPatternSearch(OriginalGridCell cell, OriginalGridCell next,
            int direction, bool excludeFallingItems)
        {
            if (cell == null || next == null) return false;
            var first = cell.Mediation.Current.Item as OriginalMatchBoardItem;
            if (first == null) return false;
            if (blocking.HasTouchBlockingItemExceptChain(cell)) return false;
            if (blocking.HasIncomingTouchBlockingItem(cell)) return false;
            var second = next.Mediation.Current.Item as OriginalMatchBoardItem;
            if (second == null) return false;
            if (blocking.HasIncomingTouchBlockingItem(next)) return false;
            if (blocking.HasTouchBlockingItemExceptChain(next)) return false;
            // Original checks the second item's current/next cells only.
            if (excludeFallingItems && second.Mediator.CurrentCell != second.Mediator.NextCell) return false;
            if (direction == 2 && cell.Y != next.Y) return false;
            if (direction == 1 && cell.X != next.X) return false;
            return first.MatchType == second.MatchType;
        }

        public bool IsThereAnyMatch(bool excludeFallingItems)
        {
            iterator.Reset();
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                var right = cell.Neighbors.Get(3);
                if (IsValidToPatternSearch(cell, right, 2, excludeFallingItems) &&
                    IsValidToPatternSearch(right, right.Neighbors.Get(3), 2, excludeFallingItems)) return true;
                var top = cell.Neighbors.Get(1);
                if (IsValidToPatternSearch(cell, top, 1, excludeFallingItems) &&
                    IsValidToPatternSearch(top, top.Neighbors.Get(1), 1, excludeFallingItems)) return true;
                if (IsValidToPatternSearch(cell, top, 0, excludeFallingItems) &&
                    IsValidToPatternSearch(cell, right, 0, excludeFallingItems) &&
                    IsValidToPatternSearch(cell, cell.Neighbors.Get(2), 0, excludeFallingItems)) return true;
            }
            return false;
        }

        // Pattern-emission portion of 0x12e1848; output is supplied/reused by
        // the manager. Per-item MatchGroup.Reset is part of final grouping.
        public void CollectForCell(OriginalGridCell cell, bool excludeFallingItems, List<OriginalTempMatch> output)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (cell == null || !(cell.Mediation.CurrentItem is OriginalMatchBoardItem item)) return;
            if (excludeFallingItems && item.Mediator.CurrentCell != item.Mediator.NextCell) return;
            item.MatchGroup.Reset(); // MatchGroup.Reset 0x12df088.
            var right = cell.Neighbors.Get(3);
            if (IsValidToPatternSearch(cell, right, 2, excludeFallingItems))
            {
                var third = right.Neighbors.Get(3);
                if (IsValidToPatternSearch(right, third, 2, excludeFallingItems))
                    output.Add(new OriginalTempMatch(2, cell, right, third));
            }
            var top = cell.Neighbors.Get(1);
            if (IsValidToPatternSearch(cell, top, 1, excludeFallingItems))
            {
                var third = top.Neighbors.Get(1);
                if (IsValidToPatternSearch(top, third, 1, excludeFallingItems))
                    output.Add(new OriginalTempMatch(1, cell, top, third));
            }
            if (!IsValidToPatternSearch(cell, top, 0, excludeFallingItems) ||
                !IsValidToPatternSearch(cell, right, 0, excludeFallingItems)) return;
            var diagonal = cell.Neighbors.Get(2);
            if (IsValidToPatternSearch(cell, diagonal, 0, excludeFallingItems))
                output.Add(new OriginalTempMatch(0, cell, top, right, diagonal));
        }
    }
}
