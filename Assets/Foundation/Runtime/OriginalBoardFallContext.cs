using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Supplied by the active-level data and WinFailManager, rather than inferred
    // from a preview board or GameStateManager's separate win state.
    public interface IOriginalFallLevelStatus
    {
        bool IsPortalLevel { get; }
        bool IsMultipleBoardLevel { get; }
        bool IsLevelWon { get; }
    }
    public interface IOriginalFallIceState
    {
        IOriginalFallIceCubeHelper GetIceCubeHelper();
        bool HasCrossFallFreeze(OriginalGridCell cell);
        bool HasIncomingExploder(OriginalGridCell cell);
    }

    // Board.CacheFillingCells 0x3207b00 and CanCrossFall 0x3207c54.
    // Only the highest filling cell in each column belongs to this cache.
    public sealed class OriginalBoardFillingCells
    {
        private readonly Dictionary<int, OriginalFillingCell> cells = new Dictionary<int, OriginalFillingCell>();
        public IReadOnlyDictionary<int, OriginalFillingCell> Cells => cells;
        public void Cache(OriginalBoardRegion region, OriginalCellGrid grid)
        {
            for (int x = region.Left; x <= region.Right; x++)
            for (int y = region.Top; y >= region.Bottom; y--)
            {
                var cell = grid.GetCell(x, y);
                if (cell == null) throw new NullReferenceException();
                if (!cell.IsFillingCell) continue;
                cells.Add(x, (OriginalFillingCell)cell);
                break;
            }
        }
        public bool CanCrossFall(int column)
        {
            bool result = true;
            foreach (var entry in cells)
            {
                if (unchecked(entry.Key - 1) > column || unchecked(entry.Key + 1) < column) continue;
                var cell = entry.Value;
                if (cell == null) throw new NullReferenceException();
                result &= cell.IncomingItemCountFromTopBoard < 1;
            }
            return result;
        }
    }

    // Concrete services for the existing native target planner and fall manager.
    // No item availability, static blocker or board membership is simulated.
    public sealed class OriginalBoardFallContext : IOriginalFallTargetServices, IOriginalFallTraversal
    {
        private readonly OriginalBoardLayout layout;
        private readonly OriginalInitialItemAmounts amounts;
        private readonly IOriginalFallLevelStatus level;
        private readonly IOriginalFallIceState ice;
        private readonly OriginalBoardGridAccess access;
        private readonly OriginalBoardFillingCells[] filling;
        private readonly IOriginalGridIteratorStrategy vertical, horizontal, totalVertical, totalHorizontal;
        public OriginalBoardFallContext(OriginalCellGrid grid, OriginalBoardLayout layout,
            OriginalInitialItemAmounts amounts, IOriginalFallLevelStatus level, IOriginalFallIceState ice)
        {
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
            this.amounts = amounts ?? throw new ArgumentNullException(nameof(amounts));
            this.level = level ?? throw new ArgumentNullException(nameof(level));
            this.ice = ice;
            access = new OriginalBoardGridAccess(grid, layout);
            filling = new OriginalBoardFillingCells[layout.Count];
            for (int i = 0; i < filling.Length; i++)
            {
                filling[i] = new OriginalBoardFillingCells();
                filling[i].Cache(layout[i], grid);
            }
            vertical = new OriginalBottomLeftVerticalStrategy(access);
            horizontal = new OriginalBottomLeftHorizontalStrategy(access);
            totalVertical = new OriginalTotalBottomLeftVerticalStrategy(access);
            totalHorizontal = new OriginalTotalBottomLeftHorizontalStrategy(access);
        }
        public bool IsPortalLevel => level.IsPortalLevel;
        public int BoardStartY => access.BoardStartY;
        public IOriginalGridIteratorStrategy GetIteratorStrategy(int type)
        {
            switch (type)
            {
                case 0: return vertical;
                case 1: return horizontal;
                case 4: return totalVertical;
                case 5: return totalHorizontal;
                default: throw new NotSupportedException("Only fall traversal strategies are composed here.");
            }
        }
        public int CurrentBoardIndex => layout.CurrentBoardIndex;
        public bool IsMultipleBoardLevel => level.IsMultipleBoardLevel;
        public bool CanCurrentBoardCrossFall(int x) => filling[layout.CurrentBoardIndex].CanCrossFall(x);
        public bool IsLevelWon => level.IsLevelWon;
        public bool IsFallBlocked(OriginalGridCell cell) => cell.IsFallBlocked(false, false);
        public bool IsAvailableForFall(OriginalBoardItem item, OriginalGridCell toCell) => item.IsAvailableForFall(toCell);
        public bool CanFallCross(OriginalBoardItem item) => item.CanFallCross;
        public bool IsSpecialItem(OriginalBoardItem item) => item.IsSpecialItem;
        public bool IsCrossFallBlocking(OriginalBoardItem item) => item.IsCrossFallBlocking;
        public bool IsThereIceCubeInLevel() => amounts.IsThereItemInLevel(51);
        public IOriginalFallIceCubeHelper GetIceCubeHelper() => RequireIce().GetIceCubeHelper();
        public bool HasCrossFallFreeze(OriginalGridCell cell) => RequireIce().HasCrossFallFreeze(cell);
        public bool HasIncomingExploder(OriginalGridCell cell) => RequireIce().HasIncomingExploder(cell);
        public IReadOnlyDictionary<int, OriginalFillingCell> CurrentBoardFillingCells => filling[layout.CurrentBoardIndex].Cells;
        private IOriginalFallIceState RequireIce() => ice ?? throw new InvalidOperationException("Ice level requires its actual helper, cell freeze and incoming-exploder owners.");
    }
}
