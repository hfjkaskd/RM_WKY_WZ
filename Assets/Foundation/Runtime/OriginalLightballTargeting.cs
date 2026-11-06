using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Target ownership from LightballStrategy / DefaultLightballStrategy.
    // Strategy timing, ray flight and completion remain their own owners.
    public sealed class OriginalLightballTargeting
    {
        private readonly OriginalCellGrid grid;
        private readonly OriginalBoardLayout layout;
        private readonly HashSet<int> selectedColors;
        private readonly Action<OriginalMatchBoardItem> added, removed;
        private OriginalGridIterator iterator;
        public List<OriginalMatchBoardItem> Items { get; } = new List<OriginalMatchBoardItem>();
        public OriginalLightballTargeting(OriginalCellGrid grid, OriginalBoardLayout layout, HashSet<int> selectedColors,
            Action<OriginalMatchBoardItem> added = null, Action<OriginalMatchBoardItem> removed = null)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            this.selectedColors = selectedColors ?? throw new ArgumentNullException(nameof(selectedColors));
            this.added = added; this.removed = removed;
            iterator = new OriginalGridIterator(new OriginalTopLeftHorizontalStrategy(new OriginalBoardGridAccess(grid, layout)));
        }
        public static bool IsReadyForLightball(OriginalGridCell cell) // 0x212d5c8.
        {
            if (cell == null) throw new NullReferenceException();
            return !cell.StaticTouch.HasTouchBlockingItemExceptChain()
                && cell.Mediation.HasCurrentItem() && !cell.Reservation.IsEnabled;
        }
        public int FindMostCommonItem() // 0x212d958: first encountered color wins ties.
        {
            var counts = new Dictionary<int, int>();
            for (int x = 0; x < grid.Width; x++)
                for (int y = layout.CurrentBoard.Bottom; y <= layout.CurrentBoard.Top; y++)
                {
                    var cell = grid.GetCell(x, y);
                    if (!IsReadyForLightball(cell) || !(cell.Mediation.CurrentItem is OriginalMatchBoardItem item) || item.IsReserved) continue;
                    counts.TryGetValue(item.MatchType, out int count);
                    counts[item.MatchType] = unchecked(count + 1);
                }
            int type = 0, maximum = 0;
            foreach (int key in counts.Keys)
            {
                if (selectedColors.Contains(key)) continue;
                int count = counts[key];
                if (count <= maximum) continue;
                maximum = count; type = key;
            }
            return type;
        }
        public void FindItemsByType(int matchType) // 0x212d850 / 0x1f45724.
        {
            iterator.Reset();
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null)
            {
                if (!IsReadyForLightball(cell) || !(cell.Mediation.CurrentItem is OriginalMatchBoardItem item)
                    || item.IsReserved || item.MatchType != matchType) continue;
                Items.Add(item); item.Reserve(); added?.Invoke(item);
            }
        }
        public void RemoveFromItems(OriginalMatchBoardItem item) // 0x212d054.
        { Items.Remove(item); item.UnReserve(); removed?.Invoke(item); }
    }
}
