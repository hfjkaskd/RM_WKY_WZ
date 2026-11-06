using System;

namespace Whitebox.Foundation
{
    // Traversal-facing cell state. Cell creation, neighbor linking and board
    // membership must be supplied by the restored grid manager.
    public partial class OriginalGridCell
    {
        public int X { get; }
        public int Y { get; }
        public int CellType { get; }
        public int BoardId { get; }
        public int ShifterGridId { get; set; } // CellModel +0xd8, distinct from boardId.
        public bool IsShifter { get; set; } // CellModel +0xa8.
        public int FillType { get; set; }
        public OriginalCellNeighbors Neighbors { get; }
        public OriginalGridCell Right { get => Neighbors.Get(3); set => Neighbors.Set(3, value); }
        public bool IsBottomCell { get; internal set; }
        public OriginalCellMediation Mediation { get; }
        internal int GuidedScoreFrame, GuidedScoreType, GuidedScore;
        public bool IsNormalCell => CellType == 1; // 0x3195f9c
        public virtual bool IsFillingCell => unchecked((uint)(FillType - 1)) < 4; // 0x319cfdc

        public OriginalGridCell(int x, int y, int cellType, int boardId = 0, int fillType = 0)
        {
            X = x;
            Y = y;
            CellType = cellType;
            BoardId = boardId;
            FillType = fillType;
            Neighbors = new OriginalCellNeighbors(x, y);
            Mediation = new OriginalCellMediation(this);
        }
    }

    // CellNeighbors: 0x31953a4 / 0x319541c / 0x319547c / 0x31932c8.
    public sealed class OriginalCellNeighbors
    {
        // CellNeighbors..cctor 0x31955ec, original 32-byte field FF1F6EE5...
        public static readonly int[] Sides = { 0, 1, 2, 3, 4, 5, 6, 7 };
        // Native TouchingNeighborTypes field, SHA256 49C16B6F...236F8F855.
        public static readonly int[] TouchingNeighborTypes = { 1, 3, 5, 7 };
        private static readonly int[] DeltaX = { -1, 0, 1, 1, 1, 0, -1, -1 };
        private static readonly int[] DeltaY = { 1, 1, 1, 0, -1, -1, -1, 0 };
        private readonly OriginalGridCell[] neighbors = new OriginalGridCell[8];
        private int x;
        private int y;
        public OriginalGridCell FirstNormalCellBelow { get; set; }
        public OriginalGridCell NextCell { get; set; }

        public OriginalCellNeighbors(int x, int y) { UpdateCellPoint(x, y); }
        public void UpdateCellPoint(int x, int y) { this.x = x; this.y = y; }
        public OriginalGridCell Get(int type) => neighbors[type];
        public void Set(int type, OriginalGridCell cell) { neighbors[type] = cell; }

        // 0x31954e4 -> CellPoint.GetNeighborPoint 0x343ba2c.
        // Out-of-range directions return the stored point, unlike Get/Set.
        public void GetPoint(int type, out int pointX, out int pointY)
        {
            bool valid = unchecked((uint)type) < 8;
            pointX = unchecked(x + (valid ? DeltaX[type] : 0));
            pointY = unchecked(y + (valid ? DeltaY[type] : 0));
        }

        // 0x3195550 returns the direction from the supplied point back to
        // this cell. Swap particles use it for the trail behind a moving item.
        public int GetNeighborType(int pointX, int pointY)
        {
            int dx = unchecked(x - pointX), dy = unchecked(y - pointY);
            if (dx == 1 && unchecked((uint)(dy + 1)) < 3) return 3 - dy;
            if (dx == 0) return dy == 1 ? 1 : dy == -1 ? 5 : -1;
            if (dx == -1 && unchecked((uint)(dy + 1)) < 3)
                return dy == -1 ? 6 : dy == 0 ? 7 : 0;
            return -1;
        }

        // 0x31955c8 clears only the eight references; cached below/next survive.
        public void Clear() { Array.Clear(neighbors, 0, neighbors.Length); }
    }

    public interface IOriginalGridAccess
    {
        OriginalGridCell CurrentBoardBottomLeftCell { get; }
        OriginalGridCell GetCell(int x, int y);
        bool IsInCurrentBoard(OriginalGridCell cell);
    }

    public interface IOriginalGridIteratorStrategy
    {
        OriginalGridCell FirstCell();
        OriginalGridCell GetNext(OriginalGridCell cell);
    }

    // 0x319b9b8 / 0x319b9f0: right-neighbor chain, then (0, y + 1).
    public sealed class OriginalBottomLeftHorizontalStrategy : IOriginalGridIteratorStrategy
    {
        private readonly IOriginalGridAccess grid;
        public OriginalBottomLeftHorizontalStrategy(IOriginalGridAccess grid)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }
        public OriginalGridCell FirstCell() => grid.CurrentBoardBottomLeftCell;
        public OriginalGridCell GetNext(OriginalGridCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            OriginalGridCell candidate = cell.Right;
            if (candidate == null)
            {
                candidate = grid.GetCell(0, unchecked(cell.Y + 1));
                if (candidate == null) return null;
            }
            return grid.IsInCurrentBoard(candidate) ? candidate : null;
        }
    }

    // 0x319c000 / 0x318ec2c / 0x318ecc8. The first MoveNext returns
    // the cell already obtained by the constructor or Reset, without advancing.
    public struct OriginalGridIterator
    {
        private readonly IOriginalGridIteratorStrategy strategy;
        private bool didStart;
        public OriginalGridCell Cell { get; private set; }
        public OriginalGridIterator(IOriginalGridIteratorStrategy strategy)
        {
            this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            Cell = strategy.FirstCell();
            didStart = false;
        }
        public void Reset()
        {
            Cell = strategy.FirstCell();
            didStart = false;
        }
        public OriginalGridCell MoveNext()
        {
            if (!didStart) didStart = true;
            else Cell = strategy.GetNext(Cell);
            return Cell;
        }
    }
}
