using System;

namespace Whitebox.Foundation
{
    public interface IOriginalBoardStartGridAccess : IOriginalGridAccess
    {
        int BoardStartY { get; }
    }
    // 0x319bc54 / 0x319bc8c. Portal fall planning stays on the current
    // board and starts each new column at the current board's bottom Y.
    public sealed class OriginalBottomLeftVerticalStrategy : IOriginalGridIteratorStrategy
    {
        private readonly IOriginalBoardStartGridAccess grid;
        public OriginalBottomLeftVerticalStrategy(IOriginalBoardStartGridAccess grid)
        { this.grid = grid ?? throw new ArgumentNullException(nameof(grid)); }
        public OriginalGridCell FirstCell() => grid.CurrentBoardBottomLeftCell;
        public OriginalGridCell GetNext(OriginalGridCell cell)
        {
            if (cell == null) throw new NullReferenceException();
            var above = cell.Neighbors.Get(1);
            if (above != null && grid.IsInCurrentBoard(above)) return above;
            return grid.GetCell(unchecked(cell.X + 1), grid.BoardStartY);
        }
    }
    // 0x319c360 / 0x319c3a8: full-grid, column first, with no board gate.
    public sealed class OriginalTotalBottomLeftVerticalStrategy : IOriginalGridIteratorStrategy
    {
        private readonly IOriginalGridAccess grid;
        public OriginalTotalBottomLeftVerticalStrategy(IOriginalGridAccess grid)
        { this.grid = grid ?? throw new ArgumentNullException(nameof(grid)); }
        public OriginalGridCell FirstCell() => grid.GetCell(0, 0);
        public OriginalGridCell GetNext(OriginalGridCell cell)
        {
            if (cell == null) throw new NullReferenceException();
            return cell.Neighbors.Get(1) ?? grid.GetCell(unchecked(cell.X + 1), 0);
        }
    }
    // 0x319c27c / 0x319c2c4: full-grid, row first, with no board gate.
    public sealed class OriginalTotalBottomLeftHorizontalStrategy : IOriginalGridIteratorStrategy
    {
        private readonly IOriginalGridAccess grid;
        public OriginalTotalBottomLeftHorizontalStrategy(IOriginalGridAccess grid)
        { this.grid = grid ?? throw new ArgumentNullException(nameof(grid)); }
        public OriginalGridCell FirstCell() => grid.GetCell(0, 0);
        public OriginalGridCell GetNext(OriginalGridCell cell)
        {
            if (cell == null) throw new NullReferenceException();
            return cell.Neighbors.Get(3) ?? grid.GetCell(0, unchecked(cell.Y + 1));
        }
    }
}
