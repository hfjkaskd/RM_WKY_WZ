using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalFillSetGenerationContext
    {
        bool IsTuningEnabled { get; }
        int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell cell);
        int GetLimitCount(int goalType);
        int GetGoalTargetCount(int goalType);
        void ReportRandomColorFallback(int totalRatio);
    }
    // ItemCreator's original generation selection/state. Models, predefined
    // queues and creation are separate typed owners; tuning is never bypassed.
    public sealed partial class OriginalFillSetGeneration
    {
        private OriginalLevelSetSelection[] sets;
        private readonly List<OriginalLevelSetSelection> prioritized = new List<OriginalLevelSetSelection>();
        private readonly List<OriginalLevelSetSelection> columnPrioritized = new List<OriginalLevelSetSelection>();
        private readonly List<OriginalLevelSetSelection> candidates = new List<OriginalLevelSetSelection>();
        private readonly List<OriginalLevelSetSelection> filteredColumn = new List<OriginalLevelSetSelection>();
        private readonly Dictionary<int, int> createdCounts = new Dictionary<int, int>();
        private readonly Dictionary<int, int> ratios = new Dictionary<int, int>();
        private readonly OriginalLevelRandomSource random;
        private readonly OriginalTargetColumnSetContainer columns;
        private readonly IOriginalFillSetGenerationContext context;
        public IReadOnlyDictionary<int, int> FillRatios => ratios;
        public IReadOnlyDictionary<int, int> CreatedCounts => createdCounts;
        public OriginalFillSetGeneration(OriginalLevelRandomSource random, OriginalTargetColumnSetContainer columns, IOriginalFillSetGenerationContext context)
        { this.random = random; this.columns = columns; this.context = context; }
        public void AddAvailableSets(OriginalLevelSetSelection[] values) // 0x23eb73c.
        { sets = values; SetupAvailableFillItems(); }
        public void SetupAvailableFillItems() // 0x23eb758.
        {
            createdCounts.Clear(); prioritized.Clear();
            for (int i = 0; i < sets.Length; i++)
            {
                var set = sets[i]; set.InitFillAvailability();
                if (set.ShouldPrioritize) prioritized.Add(set);
            }
            CalculateRandomFactors();
        }
        public static bool IsAvailableForFill(OriginalLevelSetSelection set) // 0x23f02dc.
        { return set.IsAvailableForFill && !set.ShouldPrioritize && !set.HasAnyTargetColumn(); }
        public void CalculateRandomFactors() // 0x23eb28c; original CellFillType values1..4.
        {
            ratios.Clear();
            for (int i = 0; i < sets.Length; i++)
            {
                var set = sets[i]; set.CalculateFillRatioForItems();
                if (!IsAvailableForFill(set)) continue;
                for (int fill = 1; fill <= 4; fill++)
                {
                    ratios.TryGetValue(fill, out int previous);
                    ratios[fill] = unchecked(previous + set.GetFillRatio(fill));
                }
            }
        }
        private OriginalLevelSetSelection GetRandomPrioritizedSet(List<OriginalLevelSetSelection> collection) // 0x23f0328.
        {
            candidates.Clear(); int total = 0;
            for (int i = 0; i < collection.Count; i++)
            {
                var set = collection[i];
                if (!set.CanGetPrioritizedItem()) continue;
                candidates.Add(set); total = unchecked(total + set.CreateRatio);
            }
            int draw = random.DifficultyNext(0, total), cumulative = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative = unchecked(cumulative + candidates[i].CreateRatio);
                if (unchecked(draw + 1) <= cumulative) return candidates[i];
            }
            return null;
        }
        private int TryPrioritized(List<OriginalLevelSetSelection> collection)
        {
            var set = GetRandomPrioritizedSet(collection);
            if (set == null) return 0;
            int item = set.GetRandomItem();
            if (item != 0) set.ConsumePrioritizedItem();
            return item;
        }
        public bool HasTarget(int column) => columns.HasTarget(column);
        public int GetRandomTargetColumnItem(OriginalFillingCell cell, int column) // 0x23efa00.
        {
            if (prioritized.Count > 0)
            {
                columnPrioritized.Clear();
                for (int i = 0; i < prioritized.Count; i++) if (prioritized[i].HasTargetColumn(cell, column)) columnPrioritized.Add(prioritized[i]);
                int chosen = TryPrioritized(columnPrioritized);
                if (chosen != 0) return chosen;
            }
            int total = columns.GetTotalRatioForColumn(cell.FillType, column, filteredColumn);
            if (total == 0) return 0;
            var list = columns.GetSetsFromColumn(cell.FillType, column, filteredColumn);
            int draw = random.DifficultyNext(0, total), cumulative = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var set = list[i];
                if (!set.IsAvailableForFill || set.ShouldPrioritize) continue;
                cumulative = unchecked(cumulative + set.GetFillRatio(cell.FillType));
                if (unchecked(draw + 1) <= cumulative) return Select(set, cell);
            }
            return 0;
        }
        public int GetRandomFillItem(OriginalFillingCell cell) // 0x23efcec.
        {
            if (prioritized.Count > 0)
            {
                int chosen = TryPrioritized(prioritized);
                if (chosen != 0) return chosen;
            }
            ratios.TryGetValue(cell.FillType, out int total);
            int draw = random.DifficultyNext(0, total), cumulative = 0;
            for (int i = 0; i < sets.Length; i++)
            {
                var set = sets[i];
                if (!IsAvailableForFill(set)) continue;
                cumulative = unchecked(cumulative + set.GetFillRatio(cell.FillType));
                if (unchecked(draw + 1) <= cumulative) return Select(set, cell);
            }
            return 0;
        }
        private int Select(OriginalLevelSetSelection set, OriginalFillingCell cell) =>
            context.IsTuningEnabled && set.ContainsMatchItem() ? context.GetTunedItem(set, cell) : set.GetRandomItem();
        public int GetRandomColor(OriginalFillingCell cell) // 0x23efec0.
        {
            ratios.TryGetValue(cell.FillType, out int total);
            context.ReportRandomColorFallback(total);
            return random.DifficultyNext(1, 5);
        }
        public void OnNewItemCreated(int type) // 0x23ef5b0.
        {
            if (!IsFillingObstacle(type)) return;
            createdCounts.TryGetValue(type, out int previous); createdCounts[type] = unchecked(previous + 1);
            int count = createdCounts[type], goal = OriginalGoalTypes.FromItemType(type);
            int limit = context.GetLimitCount(goal), target = context.GetGoalTargetCount(goal);
            if (count != target && (limit == 0 || count != limit)) return;
            for (int i = 0; i < sets.Length; i++)
                if (sets[i].DisableFirstElementByItemType(type)) sets[i].UpdateFillAvailability();
            CalculateRandomFactors();
        }
        public static bool IsFillingObstacle(int type) // 0x2b452f8.
        {
            uint first = unchecked((uint)(type - 7)), second = unchecked((uint)(type - 78));
            return first <= 61 && ((0x20900e80e9116305UL >> (int)first) & 1) != 0
                || second <= 22 && ((0x450045u >> (int)second) & 1) != 0 || type == 110 || type == 113;
        }

        internal int HitRequiredForLevelProgression(int boardHeight) // ItemCreator 0x23e9350.
        {
            int hits = 0;
            foreach (var set in sets)
                for (int i = 0; i < set.ElementCount; i++)
                {
                    if (!set.IsElementAvailableForFill(i)) continue;
                    int tiledId = set.GetElementId(i);
                    int type = OriginalTiledItemTypes.AsItemType(tiledId);
                    if (!IsFillingObstacle(type)) continue;
                    int goalType = OriginalGoalTypes.FromItemType(type);
                    int target = context.GetGoalTargetCount(goalType);
                    if (target <= 0) continue;
                    createdCounts.TryGetValue(type, out int created);
                    int layers = goalType is 23 or 37 or 55 or 91
                        ? boardHeight : OriginalTiledItemLayers.AsLayerCount(tiledId);
                    hits += Math.Max(0, target - created) * layers;
                }
            return hits;
        }
    }
}
