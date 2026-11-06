using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // TargetColumnSetContainer 0x1c93228..0x1c93784. A null column differs
    // from an empty column. Clear replaces lists rather than mutating old ones.
    public sealed class OriginalTargetColumnSetContainer
    {
        private readonly List<OriginalLevelSetSelection>[] columns = new List<OriginalLevelSetSelection>[9];
        public bool HasTarget(int column) => columns[column] != null;

        public void Clear()
        {
            for (int i = 0; i < columns.Length; i++) columns[i] = new List<OriginalLevelSetSelection>();
        }

        public void AddSetToColumn(OriginalLevelSetSelection set, int column)
        {
            if (set == null) throw new NullReferenceException();
            set.AddTargetColumn(column);
            Insert(set, column);
        }

        public void AddConfiguredSet(OriginalLevelSetSelection set)
        {
            var targets = set.TargetColumns;
            for (int i = 0; i < targets.Count; i++) Insert(set, targets[i]);
        }

        private void Insert(OriginalLevelSetSelection set, int column)
        {
            var list = columns[column];
            if (list == null) columns[column] = list = new List<OriginalLevelSetSelection>();
            list.Add(set);
        }

        // Preserve native null/all/filter results. Caller owns filteredBuffer:
        // reuse it to avoid native FindAll's per-call closure/list allocations.
        public IReadOnlyList<OriginalLevelSetSelection> GetSetsFromColumn(int fillType, int column,
            List<OriginalLevelSetSelection> filteredBuffer)
        {
            var list = columns[column];
            if (fillType == 1) return list;
            if (list == null) throw new NullReferenceException();
            if (filteredBuffer == null) throw new ArgumentNullException(nameof(filteredBuffer));
            filteredBuffer.Clear();
            for (int i = 0; i < list.Count; i++)
                if (list[i].ContainsTargetFill(unchecked(fillType - 1))) filteredBuffer.Add(list[i]);
            return filteredBuffer;
        }

        public int GetTotalRatioForColumn(int fillType, int column,
            List<OriginalLevelSetSelection> filteredBuffer)
        {
            var sets = GetSetsFromColumn(fillType, column, filteredBuffer);
            int total = 0;
            if (sets != null)
                for (int i = 0; i < sets.Count; i++)
                    if (sets[i].IsAvailableForFill && !sets[i].ShouldPrioritize)
                        total = unchecked(total + sets[i].GetFillRatio(fillType));
            return total;
        }
    }
}
