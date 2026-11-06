using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // ItemCreator low-match selection0x23f0540..0x23f0cfc. Target mediation,
    // not visual/current cell occupancy, determines forbidden refill colors.
    public sealed class OriginalLowMatchTuning
    {
        private readonly OriginalLevelRandomSource random;
        private readonly List<int> forbidden = new List<int>(3);
        private int[] ratios;
        public bool IsEnabled { get; private set; }
        public int TotalRatio { get; private set; }
        public OriginalLowMatchTuning(OriginalLevelRandomSource random) { this.random = random; }
        public void UpdateLowMatchRatios(int[] values) // 0x23f0a34; diagnostic formatting omitted.
        {
            if (values == null || values.Length < 3)
            { IsEnabled = false; TotalRatio = 0; ratios = null; return; }
            IsEnabled = true; TotalRatio = 0;
            for (int i = 0; i < values.Length; i++) TotalRatio = unchecked(TotalRatio + values[i]);
            ratios = values; // Native borrows the array; it does not clone it.
        }
        public int GetLowMatchNeighborCount() // 0x23f07dc: draw occurs before ratios access.
        {
            int draw = (random ?? throw new NullReferenceException()).DifficultyNext(0, TotalRatio);
            if (ratios == null) throw new NullReferenceException();
            if (draw < ratios[0]) return 0;
            return draw < unchecked(ratios[0] + ratios[1]) ? 1 : 2;
        }
        public int GetTunedItem(OriginalLevelSetSelection set, OriginalFillingCell fillingCell) // 0x23f0540.
        {
            int count = GetLowMatchNeighborCount();
            if (count == 0) return (set ?? throw new NullReferenceException()).GetRandomItem();
            OriginalGridCell target = fillingCell ?? throw new NullReferenceException();
            var below = target.Neighbors.Get(5);
            while (below != null)
            {
                if (below.Mediation.HasTargetItem()) break;
                target = below; below = below.Neighbors.Get(5);
            }
            forbidden.Clear();
            // Native calculates all three results before appending any result.
            int down = count == 1 ? GetForbiddenTypeForOneNeighbor(target, 5) : GetForbiddenTypeForTwoNeighbors(target, 5);
            int left = count == 1 ? GetForbiddenTypeForOneNeighbor(target, 7) : GetForbiddenTypeForTwoNeighbors(target, 7);
            int right = count == 1 ? GetForbiddenTypeForOneNeighbor(target, 3) : GetForbiddenTypeForTwoNeighbors(target, 3);
            if (down != 0) forbidden.Add(down);
            if (left != 0) forbidden.Add(left);
            if (right != 0) forbidden.Add(right); // Preserve duplicates and direction order.
            return (set ?? throw new NullReferenceException()).GetRandomItemExcept(forbidden);
        }
        public static int GetForbiddenTypeForOneNeighbor(OriginalGridCell cell, int direction) // 0x23f084c.
        {
            if (cell == null) throw new NullReferenceException();
            var first = cell.Neighbors.Get(direction);
            if (first == null || !first.Mediation.HasTargetItem() || !(first.Mediation.TargetItem is OriginalMatchBoardItem)) return 0;
            return OriginalLevelRules.ToTiledId(((OriginalMatchBoardItem)first.Mediation.TargetItem).MatchType);
        }
        public static int GetForbiddenTypeForTwoNeighbors(OriginalGridCell cell, int direction) // 0x23f08e0.
        {
            if (cell == null) throw new NullReferenceException();
            var first = cell.Neighbors.Get(direction);
            if (first == null) return 0;
            var second = first.Neighbors.Get(direction);
            if (second == null || !first.Mediation.HasTargetItem() || !(first.Mediation.TargetItem is OriginalMatchBoardItem)
                || !second.Mediation.HasTargetItem() || !(second.Mediation.TargetItem is OriginalMatchBoardItem)) return 0;
            int type = ((OriginalMatchBoardItem)first.Mediation.TargetItem).MatchType;
            if (type != ((OriginalMatchBoardItem)second.Mediation.TargetItem).MatchType) return 0;
            return OriginalLevelRules.ToTiledId(((OriginalMatchBoardItem)first.Mediation.TargetItem).MatchType);
        }
    }
}
