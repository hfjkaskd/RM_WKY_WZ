using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // These are native match-group and special-item boundaries, not a global
    // any-match shortcut. Implementations must supply the restored stages.
    public interface IOriginalSwapMatchContext
    {
        IReadOnlyList<IOriginalSwapMatchGroup> FindExtensiveMatchesInArea(OriginalGridCell center, int radius);
        bool HasSpecialItem(OriginalGridCell cell);
        bool CanComboWith(OriginalBoardItem special, OriginalBoardItem other);
    }

    // SwapAction.ShouldPlaySwapAnimation 0x1edfb40. Touch eligibility and
    // animation/explosion lifecycle remain separate native stages.
    public sealed class OriginalSwapDecision
    {
        private readonly IOriginalSwapMatchContext context;
        private bool fromMatchValid;
        private bool toMatchValid;
        private readonly OriginalSavedSwapGroup savedFrom = new OriginalSavedSwapGroup();
        private readonly OriginalSavedSwapGroup savedTo = new OriginalSavedSwapGroup();
        public IOriginalSwapMatchGroup FromMatch { get; private set; }
        public IOriginalSwapMatchGroup ToMatch { get; private set; }
        public OriginalGridCell FromSpecialCreationCell { get; private set; }
        public OriginalGridCell ToSpecialCreationCell { get; private set; }
        public bool HasMatchGroup => fromMatchValid || toMatchValid;
        internal IOriginalSwapMatchContext MatchContext => context;
        internal OriginalMatchGroupSnapshot SavedFromMatch => savedFrom.Snapshot;
        internal OriginalMatchGroupSnapshot SavedToMatch => savedTo.Snapshot;
        internal void ReplaceFromMatch(OriginalMatchGroupSnapshot value)
        {
            savedFrom.ReplaceSnapshot(value);
            FromMatch = value.Exists ? savedFrom : null;
            fromMatchValid = value.Exists;
            FromSpecialCreationCell = value.SpecialItemCreationCell;
        }
        internal void ReplaceToMatch(OriginalMatchGroupSnapshot value)
        {
            savedTo.ReplaceSnapshot(value);
            ToMatch = value.Exists ? savedTo : null;
            toMatchValid = value.Exists;
            ToSpecialCreationCell = value.SpecialItemCreationCell;
        }

        public OriginalSwapDecision(IOriginalSwapMatchContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Clear()
        {
            savedFrom.ReplaceSnapshot(default(OriginalMatchGroupSnapshot));
            savedTo.ReplaceSnapshot(default(OriginalMatchGroupSnapshot));
            FromMatch = null;
            ToMatch = null;
            fromMatchValid = false;
            toMatchValid = false;
            FromSpecialCreationCell = null;
            ToSpecialCreationCell = null;
        }

        // 0x1ee3ad8: clear the incoming item's old current/target/next slots
        // before clearing all destination slots. Existing occupants are detached.
        public static void SetAllItems(OriginalGridCell cell, OriginalBoardItem item)
        {
            if (item == null) throw new NullReferenceException();
            item.Mediator.ClearFromAllRegisteredCells();
            if (cell == null) throw new NullReferenceException();
            cell.Mediation.ClearAllItems();
            cell.Mediation.SetAllItems(item);
        }

        public bool ShouldPlaySwapAnimation(OriginalGridCell from, OriginalGridCell to, bool ignoreMatchRequirement)
        {
            if (from == null || to == null) throw new NullReferenceException();
            var fromItem = from.Mediation.CurrentItem;
            var toItem = to.Mediation.CurrentItem;
            if (toItem != null) SetAllItems(from, toItem);
            SetAllItems(to, fromItem);
            var groups = context.FindExtensiveMatchesInArea(to, 3);
            if (groups == null) throw new NullReferenceException();
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var item = to.Mediation.CurrentItem;
                if (item is OriginalMatchBoardItem && group.ContainsItem(item))
                {
                    ToMatch = Capture(group, savedTo);
                    toMatchValid = group.IsValid;
                    group.FreezeCells();
                    savedTo.SetSpecialItemCreationCell(to); // Native saved group +0x88.
                    ToSpecialCreationCell = to;
                    continue;
                }
                item = from.Mediation.CurrentItem;
                if (item is OriginalMatchBoardItem && group.ContainsItem(item))
                {
                    FromMatch = Capture(group, savedFrom);
                    fromMatchValid = group.IsValid;
                    group.FreezeCells();
                    savedFrom.SetSpecialItemCreationCell(from);
                    FromSpecialCreationCell = from;
                }
            }
            if (context.HasSpecialItem(from) && context.CanComboWith(from.Mediation.CurrentItem, to.Mediation.CurrentItem)) return true;
            if (context.HasSpecialItem(to) && context.CanComboWith(to.Mediation.CurrentItem, from.Mediation.CurrentItem)) return true;
            if (HasMatchGroup || ignoreMatchRequirement) return true;
            SetAllItems(from, fromItem);
            if (toItem != null) SetAllItems(to, toItem);
            return false;
        }
        private static IOriginalSwapMatchGroup Capture(IOriginalSwapMatchGroup group, OriginalSavedSwapGroup destination)
        {
            group.CopySnapshotTo(destination);
            return destination;
        }
    }
}
