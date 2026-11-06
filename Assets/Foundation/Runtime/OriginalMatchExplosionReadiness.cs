using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalItemFallTiming : IOriginalItemFallReadiness
    {
        float StateChangedTime { get; set; }
    }
    public sealed partial class OriginalFallComponentState : IOriginalItemFallTiming
    {
        // FallComponent 0x2ee8840 / 0x2ee8848; native construction leaves zero.
        // The full fall-state transition stage updates this timestamp separately.
        public float StateChangedTime { get; set; }
    }
    public sealed partial class OriginalMatchBoardItem
    {
        private int reserveCounter;
        public bool IsSelectedByLightball { get; private set; }
        public void SelectByLightball() { IsSelectedByLightball = true; } // 0x21a7a30.
        public void UnselectByLightball() { IsSelectedByLightball = false; } // 0x21a7a3c.
        public void Reserve() { reserveCounter = unchecked(reserveCounter + 1); } // 0x21a5a74.
        public void UnReserve() { reserveCounter = unchecked(reserveCounter - 1); } // 0x21a5a84.
        public bool IsReserved => reserveCounter > 0; // 0x21a5a94.
    }

    // MatchGroup.CanAutoExplode / AllCellsReadyForExplode and match-item
    // readiness, including the wait for matching targets above the top item.
    public sealed class OriginalMatchExplosionReadiness
    {
        // Native MatchGroup.topItems is shared, cleared and reused per query.
        // Dictionary enumeration preserves its own version checks on recursion.
        private static readonly Dictionary<int, OriginalMatchBoardItem> topItems = new Dictionary<int, OriginalMatchBoardItem>(14);
        private readonly Func<float> readLevelTime;
        private float waitDeadline;

        public OriginalMatchExplosionReadiness(Func<float> readLevelTime)
        {
            this.readLevelTime = readLevelTime ?? throw new ArgumentNullException(nameof(readLevelTime));
        }
        public static bool IsReadyForAutoExplode(OriginalMatchBoardItem item) // 0x21a5aa4.
        {
            var view = item.SwapView;
            if (view == null) throw new NullReferenceException();
            if (view.IsSwapping) return false;
            var fall = item.FallComponent as IOriginalItemFallReadiness;
            if (fall == null) throw new NullReferenceException();
            return fall.IsItemSteady && !item.IsReserved;
        }
        public bool CanAutoExplode(ref OriginalMatchGroupSnapshot group) // 0x12df568.
        {
            if (!group.ExplosionCalculated)
            {
                group.CanExplode = AllCellsReadyForExplode(group);
                group.ExplosionCalculated = true;
            }
            return group.CanExplode;
        }
        private bool AllCellsReadyForExplode(OriginalMatchGroupSnapshot group) // 0x12df5e8.
        {
            topItems.Clear();
            for (int i = 0; i < group.Cells.Count; i++)
            {
                var cell = group.Cells.Get(i);
                var item = cell.Mediation.CurrentItem as OriginalMatchBoardItem;
                if (item == null || !IsReadyForAutoExplode(item)) return false;
                if (topItems.TryGetValue(cell.X, out var previous) && CurrentCell(previous).Y >= cell.Y) continue;
                topItems[cell.X] = item;
            }
            foreach (var pair in topItems)
                if (ShouldWaitForFallingMatchItems(pair.Value)) return false;
            return true;
        }
        public bool ShouldWaitForFallingMatchItems(OriginalMatchBoardItem item) // 0x21a5b70.
        {
            while (true)
            {
                var above = CurrentCell(item).Neighbors.Get(1);
                if (above == null) return false;
                var target = above.Mediation.TargetItem as OriginalMatchBoardItem;
                if (target == null || target.MatchType != item.MatchType) return false;
                var targetFall = target.FallComponent as IOriginalItemFallReadiness;
                if (targetFall == null) throw new NullReferenceException();
                if (!targetFall.IsItemSteady)
                {
                    float time = readLevelTime();
                    var ownFall = item.FallComponent as IOriginalItemFallTiming;
                    if (ownFall == null) throw new NullReferenceException();
                    // Store as Single before comparing, like native fadd s.
                    waitDeadline = ownFall.StateChangedTime + 0.25f;
                    // ARM64 fcmp / cset LE also accepts unordered operands.
                    return !(time > waitDeadline);
                }
                item = target;
                if (item.MatchGroup.Exists) return !CanAutoExplode(ref item.MatchGroup);
            }
        }
        private static OriginalGridCell CurrentCell(OriginalMatchBoardItem item)
        {
            var mediator = item.Mediator.CurrentCell;
            if (mediator == null || mediator.Owner == null) throw new NullReferenceException();
            return mediator.Owner;
        }
    }
}
