using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalSingleExplodeTargetServices
    {
        int GetScore(OriginalGridCell cell, int guidedType, bool canSpreadJelly);
        int RemainingExplodeCount(OriginalGridCell cell, OriginalExplodeData data);
        OriginalGridCell TryRedirect(OriginalGridCell cell, OriginalExplodeData data, List<OriginalGridCell> remainingOptions);
        IOriginalExplodeTarget AddIncomingExploder(OriginalGridCell cell, OriginalExplodeData data);
    }

    // Normal single-propeller path of ExplodeTargetFinder. The owner supplies
    // the original horizontal iterator and REGULAR LevelRandomManager.Next.
    // Rocket/TNT/boomerang/combo/win-condition selectors are separate paths.
    public sealed class OriginalSingleExplodeTargetFinder
    {
        private readonly IOriginalLevelSeedRandom random;
        private readonly IOriginalSingleExplodeTargetServices targets;
        private OriginalGridIterator iterator;
        private readonly SortedDictionary<int, List<OriginalGridCell>> scores = new SortedDictionary<int, List<OriginalGridCell>>();
        internal event Action<OriginalGridCell> SingleTargetSelected;
        public OriginalSingleExplodeTargetFinder(IOriginalGridIteratorStrategy strategy, IOriginalLevelSeedRandom random,
            IOriginalSingleExplodeTargetServices targets)
        {
            iterator = new OriginalGridIterator(strategy);
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
        }
        public void FillScores(int guidedType, bool canSpreadJelly) // 0x1c950d0, admitted first-board branch.
        {
            foreach (var bucket in scores) bucket.Value.Clear(); // ClearLists(false), 0x1c948e8.
            iterator.Reset();
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                if (!cell.IsNormalCell) continue;
                int key = unchecked(-targets.GetScore(cell, guidedType, canSpreadJelly));
                List<OriginalGridCell> cells;
                if (!scores.TryGetValue(key, out cells)) { cells = new List<OriginalGridCell>(); scores.Add(key, cells); }
                cells.Add(cell);
            }
        }
        public bool FindSingleTargetCell(OriginalExplodeData data, IOriginalExplodeTarget currentTarget, out OriginalGridCell result) // 0x1c97808.
        {
            if (data.Trigger == 35) throw new NotSupportedException("Boomerang has a separate native target selector.");
            result = null;
            foreach (var bucket in scores)
            {
                // This check deliberately precedes Count: retained, empty zero
                // buckets still keep the existing target without an RNG draw.
                if (currentTarget != null && bucket.Key == 0) return true;
                List<OriginalGridCell> cells = bucket.Value;
                while (cells.Count > 0)
                {
                    int index = random.Next(0, cells.Count);
                    OriginalGridCell cell = cells[index];
                    if (cell.X == data.Point.X && cell.Y == data.Point.Y && cells.Count >= 2)
                    { index = index == 0 ? 1 : index - 1; cell = cells[index]; }
                    int count = targets.RemainingExplodeCount(cell, data);
                    if (count <= 1) cells.RemoveAt(index);
                    if (count < 1) continue;
                    result = targets.TryRedirect(cell, data, cells);
                    return false;
                }
            }
            return false;
        }
        public IOriginalExplodeTarget FindForPropeller(OriginalExplodeData data, IOriginalExplodeTarget currentTarget) // 0x1c94b9c / 0x1c956c0.
        {
            if (data.Trigger != 8 && data.Trigger != 30)
                throw new NotSupportedException("Only the normal propeller target path is composed here.");
            FillScores(OriginalExplodeTargetRules.AsGuidedType(data.Trigger), data.CanSpreadJelly);
            return ResolveSingleTarget(data, currentTarget);
        }
        internal IOriginalExplodeTarget ResolveSingleTarget(OriginalExplodeData data, IOriginalExplodeTarget currentTarget)
        {
            if (FindSingleTargetCell(data, currentTarget, out var cell)) return currentTarget;
            if (cell == null) return null;
            SingleTargetSelected?.Invoke(cell);
            return targets.AddIncomingExploder(cell, data);
        }
        public void Clear() => scores.Clear();
    }
}
