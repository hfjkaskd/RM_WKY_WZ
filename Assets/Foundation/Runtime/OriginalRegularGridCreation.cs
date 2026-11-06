using System;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // Regular (non-shifter) data-model branch of CreateCells 0x320aee0 /
    // CreateCellAt 0x320b28c. Item/view creation and context mediators remain separate.
    public static class OriginalRegularGridCreation
    {
        public static OriginalCellGrid Create(FTiledGrid config, int[] transformedItems,
            bool scroll, bool water, bool wallLevel, out OriginalBoardLayout layout)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (transformedItems == null) throw new ArgumentNullException(nameof(transformedItems));
            // CreateCells always builds fresh boards before creating any cells.
            layout = OriginalBoardLayout.Create(config.Width, config.Height, transformedItems, scroll, water);
            var grid = new OriginalCellGrid(config.Width, config.Height);
            for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
            {
                int index = OriginalLevelRules.GetIndexFromPoint(x, y, grid.Width);
                int cellType = OriginalLevelRules.AsCellType(transformedItems[index]);
                var cellConfig = config.Cells[index];
                int boardId = layout.GetBoardId(x, y);
                OriginalGridCell cell;
                if (unchecked((uint)(cellConfig.FillType - 1)) < 4)
                    cell = new OriginalFillingCell(x, y, cellType, boardId, cellConfig.FillType,
                        cellConfig.IsPredefined, wallLevel, layout.Count - 1);
                else cell = new OriginalGridCell(x, y, cellType, boardId, cellConfig.FillType);
                grid.SetCell(x, y, cell);
                if (cellType == 1) layout[boardId].NormalCellCount = unchecked(layout[boardId].NormalCellCount + 1);
            }
            grid.CacheNeighbors();
            return grid;
        }
    }
}
