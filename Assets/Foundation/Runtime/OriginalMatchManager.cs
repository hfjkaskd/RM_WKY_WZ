using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Search orchestration: 0x12e1038, 0x12e105c, 0x12e11f0,
    // 0x12e1328 and 0x12e1448. Results are borrowed until the next search.
    public sealed class OriginalMatchManager
    {
        private readonly OriginalCellGrid grid;
        private readonly OriginalBoardLayout layout;
        private readonly OriginalMatchSearch search;
        private readonly OriginalMatchGroups groups;
        private readonly IOriginalMatchBlockingState blocking;
        private OriginalGridIterator iterator;
        private readonly List<OriginalTempMatch> threes = new List<OriginalTempMatch>();
        private readonly List<OriginalPossibleMatch> candidates = new List<OriginalPossibleMatch>();

        public OriginalMatchManager(OriginalCellGrid grid, OriginalBoardLayout layout,
            IOriginalMatchBlockingState blocking, IOriginalMatchFreezeOperations freeze)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            this.blocking = blocking ?? throw new ArgumentNullException(nameof(blocking));
            var strategy = new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(grid, layout));
            iterator = new OriginalGridIterator(strategy);
            search = new OriginalMatchSearch(strategy, blocking);
            groups = new OriginalMatchGroups(grid.Width, grid.Height, freeze);
        }

        public IReadOnlyList<OriginalResolvedMatchGroup> FindAllMatches()
        {
            threes.Clear();
            iterator.Reset();
            OriginalGridCell cell;
            while ((cell = iterator.MoveNext()) != null) search.CollectForCell(cell, false, threes);
            var result = Build();
            groups.ExtendGroups(blocking, false);
            return result;
        }

        public IReadOnlyList<OriginalResolvedMatchGroup> FindMatchesInArea(int x, int y,
            int radius, bool excludeFallingItems = false)
        {
            int left = Math.Max(unchecked(x - radius), 0);
            int right = Math.Min(unchecked(x + radius), grid.Width - 1);
            int bottom = Math.Max(unchecked(y - radius), layout.CurrentBoard.Bottom);
            int top = Math.Min(unchecked(y + radius), layout.CurrentBoard.Top);
            threes.Clear();
            // Native area search is column-major and bounds only pattern origins.
            // Neighbor patterns may reach beyond the search rectangle.
            for (int column = left; column <= right; column++)
            for (int row = bottom; row <= top; row++)
                search.CollectForCell(grid.GetCell(column, row), excludeFallingItems, threes);
            return Build();
        }

        public IReadOnlyList<OriginalResolvedMatchGroup> FindExtensiveMatchesInArea(int x, int y, int radius)
        {
            // ARM64 0x12e144c explicitly supplies excludeFallingItems=true.
            var result = FindMatchesInArea(x, y, radius, true);
            groups.ExtendGroups(blocking, true);
            return result;
        }

        public bool IsThereAnyMatch(bool excludeFallingItems) => search.IsThereAnyMatch(excludeFallingItems);

        private IReadOnlyList<OriginalResolvedMatchGroup> Build()
        {
            OriginalMatchCandidates.Build(threes, candidates);
            OriginalMatchCandidates.Sort(candidates);
            return groups.Build(threes, candidates);
        }
    }
}
