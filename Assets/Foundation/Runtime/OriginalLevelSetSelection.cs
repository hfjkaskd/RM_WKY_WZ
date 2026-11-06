using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Selection stage of LevelSet. The board availability stage must update each
    // element before recalculating weights; this class does not replace that stage.
    public sealed partial class OriginalLevelSetSelection
    {
        private int[] ids;
        private int[] weights;
        private bool[] available;
        private readonly OriginalLevelRandomSource random;
        private readonly bool canFall;
        private readonly Dictionary<int, int> maxItemCounts;
        private readonly List<int> targetColumns;
        private readonly int[] targetFills;
        private readonly int[] targetFillRatios;
        private readonly int createRatio;
        public IReadOnlyList<int> TargetColumns => targetColumns;
        internal int ElementCount => ids.Length;
        internal int GetElementId(int index) => ids[index];
        internal bool IsElementAvailableForFill(int index) => available[index];

        public bool IsAvailableForFill { get; private set; }
        public int TotalCreateRatio { get; private set; }
        public bool ShouldPrioritize => maxItemCounts.Count > 0;
        public IReadOnlyDictionary<int, int> MaxItemCounts => maxItemCounts;
        public int RemainingItemCount { get; private set; }

        public OriginalLevelSetSelection(OriginalParsedLevelSet set, OriginalLevelRandomSource random)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            ids = (int[])set.ElementIds.Clone();
            weights = (int[])set.ElementCreateRatios.Clone();
            available = new bool[ids.Length];
            canFall = set.CanFall;
            maxItemCounts = new Dictionary<int, int>(set.MaxItemCounts);
            targetColumns = new List<int>(set.TargetColumns);
            targetFills = (int[])set.TargetFills.Clone();
            targetFillRatios = (int[])set.TargetFillRatios.Clone();
            createRatio = set.CreateRatio;
        }

        public void AddTargetColumn(int column) => targetColumns.Add(column);
        public bool ContainsTargetFill(int fillIndex) => Array.IndexOf(targetFills, fillIndex) >= 0;

        // 0x1910d98: empty target fills default to one only for fill types 2..4.
        public int GetFillRatio(int fillType)
        {
            if (fillType == 1) return createRatio;
            if (unchecked((uint)(fillType - 2)) >= 3) return 0;
            if (targetFills.Length == 0) return 1;
            int index = Array.IndexOf(targetFills, unchecked(fillType - 1));
            return index < 0 ? 0 : targetFillRatios[index];
        }

        // 0x190f314. Preserve original dictionary mutation: the first matching
        // element keeps only its type's limit, including when other limits exist.
        public void InitFillAvailability()
        {
            IsAvailableForFill = canFall;
            TotalCreateRatio = 0;
            RemainingItemCount = 0;
            bool matched = false;
            for (int i = 0; i < ids.Length; i++)
            {
                available[i] = IsAvailableForFill;
                int type = OriginalTiledItemTypes.AsItemType(ids[i]);
                if (maxItemCounts.TryGetValue(type, out int limit))
                {
                    maxItemCounts.Clear();
                    maxItemCounts.Add(type, limit);
                    matched = true;
                }
            }
            if (!matched) maxItemCounts.Clear();
        }

        // InitFillAvailability's flag initialization only. Item-type limits and
        // board counts remain the responsibility of the original availability stage.
        public void InitializeAvailability(bool canFall)
        {
            IsAvailableForFill = canFall;
            TotalCreateRatio = 0;
            for (int i = 0; i < available.Length; i++) available[i] = canFall;
        }

        public void SetElementAvailability(int index, bool value) => available[index] = value;

        // 0x190f9cc: preserve unchecked accumulation and configuration order.
        public void CalculateFillRatioForItems()
        {
            int total = 0;
            for (int i = 0; i < available.Length; i++)
                if (available[i]) total = unchecked(total + weights[i]);
            TotalCreateRatio = total;
        }

        // 0x190fa8c: an available element leaves the set flag unchanged.
        public void UpdateFillAvailability()
        {
            for (int i = 0; i < available.Length; i++)
                if (available[i]) return;
            IsAvailableForFill = false;
        }

        // 0x1910528: even an empty set consumes a DifficultyNext(0, 0) call.
        public int GetRandomItem()
        {
            int draw = random.DifficultyNext(0, TotalCreateRatio);
            int cumulative = 0;
            for (int i = 0; i < available.Length; i++)
                if (available[i])
                {
                    cumulative = unchecked(cumulative + weights[i]);
                    if (unchecked(draw + 1) <= cumulative) return ids[i];
                }
            return 0;
        }

        // 0x19105fc: exhausted exclusions fall back to normal selection,
        // consuming a second sample and permitting a forbidden id.
        public int GetRandomItemExcept(List<int> forbiddenIds)
        {
            if (forbiddenIds == null) throw new ArgumentNullException(nameof(forbiddenIds));
            int total = 0;
            for (int i = 0; i < ids.Length; i++)
                if (!forbiddenIds.Contains(ids[i]) && available[i])
                    total = unchecked(total + weights[i]);
            int draw = random.DifficultyNext(0, total);
            int cumulative = 0;
            for (int i = 0; i < ids.Length; i++)
                if (available[i] && !forbiddenIds.Contains(ids[i]))
                {
                    cumulative = unchecked(cumulative + weights[i]);
                    if (unchecked(draw + 1) <= cumulative) return ids[i];
                }
            return GetRandomItem();
        }
        public bool ContainsSpecialItem() // 0x1910c68 examines ALL elements.
        {
            for (int i = 0; i < ids.Length; i++)
                if (ids[i] == 10 || ids[i] == 20 || ids[i] == 30 || ids[i] == 40 || ids[i] == 50) return true;
            return false;
        }
        public void DisableFilling() { IsAvailableForFill = false; }
        public void FillFourColors() // 0x1910830 retains existing elements and their weights.
        {
            var missing = new List<int>(4) { 1, 2, 3, 4 };
            int weight = 1;
            for (int i = 0; i < ids.Length; i++) { missing.Remove(ids[i]); weight = weights[i]; }
            int previous = ids.Length;
            Array.Resize(ref ids, previous + missing.Count);
            Array.Resize(ref weights, previous + missing.Count);
            Array.Resize(ref available, previous + missing.Count);
            for (int i = 0; i < missing.Count; i++) { ids[previous + i] = missing[i]; weights[previous + i] = weight; }
            InitFillAvailability(); CalculateFillRatioForItems();
        }
    }
}
