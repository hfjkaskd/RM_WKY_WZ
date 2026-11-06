using System;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalFirstBoardExplodeTargets : IOriginalAreaExplodeTargetServices
    {
        private OriginalCellGrid areaGrid;
        private Func<bool> isBonusLevel;
        private int[] goalDependentCounts;
        public void BindAreaContext(OriginalCellGrid grid, Func<bool> isBonus)
        { areaGrid = grid ?? throw new ArgumentNullException(nameof(grid)); isBonusLevel = isBonus ?? throw new ArgumentNullException(nameof(isBonus)); }
        public void RequireSupportedArea(OriginalExplodeData data)
        {
            if (data.Trigger != 13 || data.CanSpreadJelly)
                throw new NotSupportedException("This area composition requires the native normal TNT/no-jelly branch.");
            if (areaGrid == null || isBonusLevel == null) throw new InvalidOperationException("Area target context is not bound.");
            RequireSupportedScoring();
            OriginalGoalTuple[] finalGoals = goals.GetFinalGoals();
            for (int i = 0; i < finalGoals.Length; i++)
                if (finalGoals[i].IsSpecificWinConditionalGoal())
                    throw new NotSupportedException("Area winning selection requires this goal's original conditional-win owner.");
        }
        public OriginalGridCell GetCell(int x, int y) => areaGrid.GetCell(x, y);
        public bool IsUncoveredMatchOrSpecial(OriginalGridCell cell) => cell.StaticTouch.AboveItems.Count == 0 &&
            cell.Mediation.CurrentItem is OriginalMatchBoardItem or OriginalSpecialBoardItem; // 0x1c95ad8..0x1c95b14.
        private static bool IsLightball(OriginalGridCell cell) => cell.Mediation.CurrentItem is OriginalLightballBoardItem;

        // The area loop still looks at the Lightball if the cover has a pending final hit.
        public bool ShouldPenalizeAreaLightball(OriginalGridCell cell) =>
            IsLightball(cell) && !(Honey(cell)?.RemainingExplodeCount() > 0);
        public void BeginAreaScore()
        {
            int count = goals.GetCurrentGoals().Length;
            if (goalDependentCounts == null || goalDependentCounts.Length != count) goalDependentCounts = new int[count];
            else Array.Clear(goalDependentCounts, 0, count);
        }
        public bool ShouldAddAreaScore(OriginalGridCell cell) // 0x1c9763c; goal-dependent targets share the remaining count.
        {
            var item = cell.Mediation.CurrentItem;
            if (item is not (OriginalMatchBoardItem or OriginalMailBoxBoardItem) ||
                cell.StaticTouch.HasTouchBlockingItem() || Grass(cell) != null) return true;
            int index = goals.GetGoalIndex(item.GoalType);
            if (index < 0) return true;
            if (goals.GetGoalLeftCount(item.GoalType) <= goalDependentCounts[index]) return false;
            goalDependentCounts[index] = unchecked(goalDependentCounts[index] + 1); return true;
        }
        public bool IsAreaWinning(OriginalCellPoint point, OriginalExplodeData data) // WinCalculationData 0x1ca42f0.
        {
            if (goals.HasAnyPrerequisite() || isBonusLevel()) return false;
            var finalGoals = BeginGoalHitScan();
            for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
            {
                OriginalGridCell cell = areaGrid.GetCell(unchecked(point.X + dx), unchecked(point.Y + dy));
                if (cell == null) continue;
                CountPropellerGoalHits(cell, finalGoals, areaGoalHits);
            }
            return HasWinningGoalHits(finalGoals, areaGoalHits);
        }

        internal bool IsComboWinning(int comboType, OriginalCellPoint center) // 0x23e7148 / 0x1c9d2c4.
        {
            if (goals.HasAnyPrerequisite() || isBonusLevel()) return false;
            var finalGoals = BeginGoalHitScan();
            var region = lineLayout.CurrentBoard;
            bool cross = comboType is 6 or 7;
            int radius = comboType switch { 1 => 11, 5 => 4, 6 => 1, _ => 0 };
            int left = Math.Max(region.Left, center.X - radius), right = Math.Min(region.Right, center.X + radius);
            int bottom = Math.Max(region.Bottom, center.Y - radius), top = Math.Min(region.Top, center.Y + radius);
            // The native iterators start at the bottom left and visit each
            // included cell once, including the intersection of the rockets.
            for (int x = cross ? region.Left : left; x <= (cross ? region.Right : right); x++)
            {
                bool fullColumn = cross && x >= left && x <= right;
                for (int y = fullColumn ? region.Bottom : bottom; y <= (fullColumn ? region.Top : top); y++)
                {
                    var cell = areaGrid.GetCell(x, y);
                    if (cell != null) CountPropellerGoalHits(cell, finalGoals, areaGoalHits);
                }
            }
            return HasWinningGoalHits(finalGoals, areaGoalHits);
        }

        internal bool IsSinglePropellerWinning(OriginalGridCell cell) // WinCalculationData 0x1ca4aa0.
        {
            if (goals.HasAnyPrerequisite() || isBonusLevel()) return false;
            var finalGoals = goals.GetFinalGoals();
            if (singlePropellerGoalHits.Hits.Length != finalGoals.Length)
                singlePropellerGoalHits.Reset(finalGoals.Length);
            // Ordinary first-100 obstacles use the retained master and goal
            // counts. The native per-hit cumulative list serves later items.
            CountPropellerGoalHits(cell, finalGoals, singlePropellerGoalHits);
            return HasWinningGoalHits(finalGoals, singlePropellerGoalHits);
        }

        internal void ClearSinglePropellerGoalHits() =>
            singlePropellerGoalHits.Reset(singlePropellerGoalHits.Hits.Length);

        private OriginalGoalTuple[] BeginGoalHitScan()
        {
            var finalGoals = goals.GetFinalGoals();
            areaGoalHits.Reset(finalGoals.Length);
            return finalGoals;
        }

        private static bool HasWinningGoalHits(OriginalGoalTuple[] finalGoals, GoalHitTotals totals)
        {
            for (int i = 0; i < finalGoals.Length; i++)
                if (!finalGoals[i].IsSpecificWinConditionalGoal() && finalGoals[i].LeftCount > totals.Hits[i]) return false;
            return true;
        }
    }
}
