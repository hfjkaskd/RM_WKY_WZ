using System;

namespace Whitebox.Foundation
{
    // CellGrid 0x31950bc / 0x319513c. Out-of-range reads return null;
    // out-of-range writes are ignored, including negative coordinates.
    public sealed class OriginalCellGrid
    {
        private readonly OriginalGridCell[,] cells;
        public int Width { get; }
        public int Height { get; }

        public OriginalCellGrid(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new OriginalGridCell[width, height];
        }

        public OriginalGridCell GetCell(int x, int y)
        {
            if ((x | y) < 0 || x >= Width || y >= Height) return null;
            return cells[x, y];
        }

        public void SetCell(int x, int y, OriginalGridCell cell)
        {
            if ((x | y) < 0 || x >= Width || y >= Height) return;
            cells[x, y] = cell;
        }

        // CellGridManager.CacheNeighbors 0x320b3e8. References can cross boards;
        // only the downward search is restricted to the cell's own board.
        public void CacheNeighbors()
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                OriginalGridCell cell = GetCell(x, y);
                if (cell == null) continue;
                OriginalCellNeighbors neighbors = cell.Neighbors;
                for (int type = 0; type < 8; type++)
                {
                    neighbors.GetPoint(type, out int neighborX, out int neighborY);
                    neighbors.Set(type, GetCell(neighborX, neighborY));
                }
                neighbors.FirstNormalCellBelow = GetFirstNormalBelow(cell);
                cell.IsBottomCell = neighbors.FirstNormalCellBelow == null && cell.BoardId == 0;
            }
        }

        // CellGridManager.GetFirstNormalBelow 0x320b824. Preserve stop order:
        // null, different board, normal cell, filling cell, continue downward.
        public OriginalGridCell GetFirstNormalBelow(OriginalGridCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            int x = cell.X;
            int y = cell.Y;
            while (true)
            {
                OriginalGridCell below = GetCell(x, unchecked(y - 1));
                if (below == null || below.BoardId != cell.BoardId) return null;
                if (below.IsNormalCell) return below;
                if (below.IsFillingCell) return null;
                x = below.X;
                y = below.Y;
            }
        }
    }
}
