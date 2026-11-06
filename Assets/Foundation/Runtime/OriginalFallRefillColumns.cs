using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalRemainingColumnQueue
    {
        bool AddRemainingItemsToFillingCellQueue(int column);
    }

    // FallManager column state (0x3210618 / 0x3210670 / 0x32120bc).
    // Full ManualUpdate traversal/fall calculations are restored separately.
    public sealed class OriginalFallRefillColumns : IOriginalColumnRefill
    {
        private readonly HashSet<int> disabledTopColumns = new HashSet<int>();
        private readonly HashSet<int> pendingTopColumns = new HashSet<int>();
        private readonly List<int> completedColumns = new List<int>();

        public void DisableReFillForTopColumn(int column) { disabledTopColumns.Add(column); }
        public void EnableReFillForTopColumn(int column)
        {
            disabledTopColumns.Remove(column);
            pendingTopColumns.Add(column);
        }
        public bool IsTopColumnDisabled(int column) => disabledTopColumns.Contains(column);
        public bool IsTopBoardCheckPending(int column) => pendingTopColumns.Contains(column); // 0x3211710.
        public bool CanRefillFromColumn(int column) => !disabledTopColumns.Contains(column) && !pendingTopColumns.Contains(column);

        // Tail of ManualUpdate 0x32106f0: remove successful columns only after
        // enumerating the pending set. Reuse temporary storage outside Update.
        public void ProcessPendingColumns(IOriginalRemainingColumnQueue queue)
        {
            if (pendingTopColumns.Count == 0) return;
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            completedColumns.Clear();
            foreach (int column in pendingTopColumns)
                if (queue.AddRemainingItemsToFillingCellQueue(column)) completedColumns.Add(column);
            for (int i = 0; i < completedColumns.Count; i++) pendingTopColumns.Remove(completedColumns[i]);
        }
        // Column-state portion of Clear 0x32102d4.
        public void Clear()
        {
            pendingTopColumns.Clear();
            disabledTopColumns.Clear();
            completedColumns.Clear();
        }
    }
}
