using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Geometry portion of Board. Goals, item counters and views are restored separately.
    public sealed class OriginalBoardRegion
    {
        public int Left { get; }
        public int Bottom { get; }
        public int Right { get; }
        public int Top { get; }
        public int Width { get; }
        public int Height { get; }
        public int NormalCellCount { get; internal set; }
        public OriginalBoardRegion(int left, int bottom, int width, int height)
        {
            Left = left;
            Bottom = bottom;
            Width = width;
            Height = height;
            Right = unchecked(left + width - 1);
            Top = unchecked(bottom + height - 1);
        }
        public bool Contains(int x, int y) => y >= Bottom && y <= Top && x >= Left && x <= Right;
    }

    public sealed class OriginalBoardLayout
    {
        private readonly OriginalBoardRegion[] boards;
        public int Count => boards.Length;
        public int CurrentBoardIndex { get; private set; }
        public OriginalBoardRegion CurrentBoard => boards[CurrentBoardIndex];
        public OriginalBoardRegion this[int index] => boards[index];
        public bool IsFinalBoard => CurrentBoardIndex == 0;

        private OriginalBoardLayout(OriginalBoardRegion[] boards, int currentBoardIndex)
        {
            this.boards = boards;
            CurrentBoardIndex = currentBoardIndex;
        }

        // BoardManager.CreateBoard 0x320830c: geometry and initial board index.
        // Caller supplies already transformed item IDs and original parser mode flags.
        public static OriginalBoardLayout Create(int width, int height, int[] items, bool scroll, bool water)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (!scroll && !water)
                return new OriginalBoardLayout(new[] { new OriginalBoardRegion(0, 0, width, height) }, 0);

            var starts = new List<int> { 0 };
            var ends = new List<int>();
            bool closingBoundary = true;
            for (int y = 0; y < height; y++)
            {
                int id = items[OriginalLevelRules.GetIndexFromPoint(0, y, width)];
                if (id == 17 || id == 320)
                {
                    int upperBound = GetBoardUpperBound(width, items, y);
                    if (closingBoundary) ends.Add(upperBound);
                    else starts.Add(unchecked(y + 1));
                    closingBoundary = !closingBoundary;
                }
                else if (id == 12)
                {
                    ends.Add(unchecked(y - 1));
                    break;
                }
                else if (y == unchecked(height - 1)) ends.Add(y);
            }

            var regions = new OriginalBoardRegion[starts.Count];
            for (int i = 0; i < regions.Length; i++)
                regions[i] = new OriginalBoardRegion(0, starts[i], width, unchecked(ends[i] - starts[i] + 1));
            return new OriginalBoardLayout(regions, unchecked(regions.Length - 1));
        }

        // 0x3208dc4: highest normal cell found below startIndex across columns.
        public static int GetBoardUpperBound(int width, int[] items, int startIndex)
        {
            int maximum = 0;
            for (int x = 0; x < width; x++)
            for (int y = unchecked(startIndex - 1); y >= 0; y--)
            {
                if (OriginalLevelRules.AsCellType(items[OriginalLevelRules.GetIndexFromPoint(x, y, width)]) != 1) continue;
                if (y > maximum) maximum = y;
                break;
            }
            return maximum;
        }

        // 0x3209820 deliberately ignores x and returns the first matching board.
        public int GetBoardId(int x, int y)
        {
            for (int i = 0; i < boards.Length; i++)
                if (y >= boards[i].Bottom && y <= boards[i].Top) return i;
            return -1;
        }

        // 0x320988c: board index decreases toward the final board.
        public void ChangeBoard()
        {
            if (CurrentBoardIndex != 0) CurrentBoardIndex = unchecked(CurrentBoardIndex - 1);
        }
    }

    // Runtime adapter replaces test-supplied traversal membership/first-cell hooks.
    public sealed class OriginalBoardGridAccess : IOriginalBoardEndGridAccess
    {
        private readonly OriginalCellGrid grid;
        private readonly OriginalBoardLayout layout;
        public OriginalBoardGridAccess(OriginalCellGrid grid, OriginalBoardLayout layout)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }
        public OriginalGridCell CurrentBoardBottomLeftCell => grid.GetCell(layout.CurrentBoard.Left, layout.CurrentBoard.Bottom);
        public int BoardStartY => layout.CurrentBoard.Bottom; // CellGridManager 0x320a4e4.
        public OriginalGridCell CurrentBoardTopLeftCell => grid.GetCell(layout.CurrentBoard.Left, layout.CurrentBoard.Top);
        public OriginalGridCell GetCell(int x, int y) => grid.GetCell(x, y);
        // CellGridManager.IsInCurrentBoard 0x3196c8c uses board identity.
        public bool IsInCurrentBoard(OriginalGridCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            return cell.BoardId == layout.CurrentBoardIndex;
        }
    }
}
