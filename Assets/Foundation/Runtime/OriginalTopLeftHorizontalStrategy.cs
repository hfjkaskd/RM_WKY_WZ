using System;

namespace Whitebox.Foundation
{
    public interface IOriginalBoardEndGridAccess : IOriginalBoardStartGridAccess
    {
        OriginalGridCell CurrentBoardTopLeftCell { get; }
    }
    // Native iterator3, used by lightball targeting; row order is top to bottom.
    public sealed class OriginalTopLeftHorizontalStrategy : IOriginalGridIteratorStrategy
    {
        private readonly IOriginalBoardEndGridAccess grid;
        public OriginalTopLeftHorizontalStrategy(IOriginalBoardEndGridAccess grid)
        { this.grid = grid ?? throw new ArgumentNullException(nameof(grid)); }
        public OriginalGridCell FirstCell() => grid.CurrentBoardTopLeftCell; // 0x319c080.
        public OriginalGridCell GetNext(OriginalGridCell cell) // 0x319c0b8.
        {
            if (cell == null) throw new NullReferenceException();
            var next = cell.Neighbors.Get(3) ?? grid.GetCell(0, unchecked(cell.Y - 1));
            return next != null && grid.IsInCurrentBoard(next) ? next : null;
        }
    }
}
