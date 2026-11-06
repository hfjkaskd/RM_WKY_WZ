using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalAreaExplodeTargetServices
    {
        void RequireSupportedArea(OriginalExplodeData data);
        OriginalGridCell GetCell(int x, int y);
        int GetScore(OriginalGridCell cell, int guidedType, bool canSpreadJelly);
        bool IsUncoveredMatchOrSpecial(OriginalGridCell cell);
        bool ShouldPenalizeAreaLightball(OriginalGridCell cell);
        bool HasTargetableBelowItemUnderLightball(OriginalGridCell cell);
        void BeginAreaScore();
        bool ShouldAddAreaScore(OriginalGridCell cell);
        bool IsAreaWinning(OriginalCellPoint point, OriginalExplodeData data);
        IOriginalExplodeTarget AddIncomingExploder(OriginalGridCell cell, OriginalExplodeData data);
    }

    // ExplodeTargetFinder's trigger13 branch. Static/item-helper, jelly and
    // multiple-hit areas require their own score/win owners before admission.
    public sealed class OriginalAreaExplodeTargetFinder
    {
        private readonly IOriginalLevelSeedRandom random;
        private readonly IOriginalAreaExplodeTargetServices targets;
        private OriginalGridIterator iterator;
        private readonly List<OriginalCellPoint> areaScores = new List<OriginalCellPoint>();
        private readonly List<OriginalCellPoint> zeroScoreCenters = new List<OriginalCellPoint>();
        private readonly List<OriginalCellPoint> usefulNegativeScores = new List<OriginalCellPoint>();
        public event Action<bool> WinningComboSelected;
        public OriginalAreaExplodeTargetFinder(IOriginalGridIteratorStrategy strategy, IOriginalLevelSeedRandom random,
            IOriginalAreaExplodeTargetServices targets)
        {
            iterator = new OriginalGridIterator(strategy);
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
        }
        public IOriginalExplodeTarget FindForTnt(OriginalExplodeData data)
        {
            OriginalCellPoint point = FindHighestScore(data);
            OriginalGridCell cell = targets.GetCell(point.X, point.Y);
            return cell == null ? null : targets.AddIncomingExploder(cell, data); // 0x1c95018.
        }
        public int CalculateScore(OriginalCellPoint point, OriginalExplodeData data, out bool hasPositive) // 0x1c9ba28.
        {
            targets.RequireSupportedArea(data);
            targets.BeginAreaScore(); hasPositive = false;
            int result = 0;
            for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
            {
                OriginalGridCell cell = targets.GetCell(unchecked(point.X + dx), unchecked(point.Y + dy));
                if (cell == null) continue;
                int score = targets.GetScore(cell, 3, false);
                hasPositive |= score > 0 || targets.HasTargetableBelowItemUnderLightball(cell);
                if (score > 0 && targets.ShouldPenalizeAreaLightball(cell))
                    result = unchecked(result + (int)0xffe7985d);
                if (score != 0 && targets.ShouldAddAreaScore(cell)) result = unchecked(result + score);
            }
            return result;
        }
        public OriginalCellPoint FindHighestScore(OriginalExplodeData data, IComparer<OriginalCellPoint> comparer = null,
            bool isUserAction = true) // 0x1c95804, normal no-jelly branch.
        {
            targets.RequireSupportedArea(data);
            areaScores.Clear(); zeroScoreCenters.Clear(); usefulNegativeScores.Clear(); iterator.Reset();
            int highest = int.MinValue, highestUsefulNegative = int.MinValue;
            bool winningFound = false;
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                if (!cell.IsNormalCell) continue;
                int centerScore = targets.GetScore(cell, 3, false);
                bool matchOrSpecial = targets.IsUncoveredMatchOrSpecial(cell);
                bool targetableBelowLightball = targets.HasTargetableBelowItemUnderLightball(cell);
                if (centerScore <= 0 && !matchOrSpecial && !targetableBelowLightball) continue;
                var point = new OriginalCellPoint(cell.X, cell.Y);
                int score = CalculateScore(point, data, out bool hasPositive);
                bool zeroCenter = matchOrSpecial && centerScore < 1;
                // Native +0x18 (preferred) receives positive/obstacle centers;
                // +0x20 receives zero/negative match-or-special centers.
                List<OriginalCellPoint> centers = zeroCenter ? zeroScoreCenters : areaScores;
                if (targets.IsAreaWinning(point, data))
                {
                    WinningComboSelected?.Invoke(isUserAction);
                    if (!winningFound) { areaScores.Clear(); zeroScoreCenters.Clear(); usefulNegativeScores.Clear(); }
                    centers.Add(point); winningFound = true; continue;
                }
                if (winningFound) continue;
                if (score >= 0 || targetableBelowLightball)
                {
                    if (score == highest) centers.Add(point);
                    else if (score > highest)
                    {
                        areaScores.Clear(); zeroScoreCenters.Clear(); centers.Add(point); highest = score;
                    }
                }
                // Native useful-negative list is separate from the shared best
                // score and only admits a positive center in this no-jelly path.
                if (highest <= 0 && hasPositive && centerScore > 0)
                {
                    if (score == highestUsefulNegative) usefulNegativeScores.Add(point);
                    else if (score > highestUsefulNegative)
                    { usefulNegativeScores.Clear(); usefulNegativeScores.Add(point); highestUsefulNegative = score; }
                }
            }
            List<OriginalCellPoint> options = highest <= 0 && usefulNegativeScores.Count > 0 ? usefulNegativeScores :
                areaScores.Count > 0 ? areaScores : zeroScoreCenters;
            if (options.Count == 0) return OriginalCellPoint.Default;
            if (comparer == null) return options[random.Next(0, options.Count)]; // Includes the one-candidate draw.
            for (int i = 0; i < options.Count - 1; i++)
            { int j = random.Next(i, options.Count); var value = options[j]; options[j] = options[i]; options[i] = value; }
            options.Sort(comparer); return options[0]; // 0x1c9d160.
        }
    }
}
