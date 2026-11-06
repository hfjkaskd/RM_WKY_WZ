using System;

namespace Whitebox.Foundation
{
    public interface IOriginalGridItemPopulationContext
    {
        OriginalBoardItem CreateItemModelForStart(int tiledId, int originalTiledId, OriginalGridCell cell);
        short GetRestoredItemData(int x, int y);
        void LoadItemSaveData(OriginalBoardItem item, short data);
    }
    // CellGridManager.CreateItems 0x320f5a0. Static items and views have their
    // own initialization stages. The total-grid iterator is retained/reset,
    // preserving traversal and live callback behavior on repeated population.
    public sealed class OriginalGridItemPopulation
    {
        private OriginalGridIterator totalIterator;
        private readonly IOriginalGridItemPopulationContext context;
        public event Action OnItemModelsInitialized;
        public OriginalGridItemPopulation(IOriginalGridIteratorStrategy strategy, IOriginalGridItemPopulationContext context)
        { totalIterator = new OriginalGridIterator(strategy); this.context = context; }

        // CellGridManager.CreateItems 0x320f728. Empty is a normal cell that
        // receives falling items later. Fill and None are non-normal cells.
        internal static bool ShouldCreateItem(int cellType, int tiledId, int origin) =>
            (cellType != 0 || origin == 3) && tiledId != 0 && tiledId != 13;

        public void CreateItems(IOriginalGridItemPopulationData data, int origin)
        {
            totalIterator.Reset();
            OriginalGridCell cell;
            while ((cell = totalIterator.MoveNext()) != null)
            {
                int tiledId = data.GetItemAtCell(cell.X, cell.Y);
                int originalId = data.GetItemOriginalIdAtCell(cell.X, cell.Y);
                if (!ShouldCreateItem(cell.CellType, tiledId, origin)) continue;
                var item = context.CreateItemModelForStart(tiledId, originalId, cell);
                cell.Mediation.ClearAllItems();
                cell.Mediation.SetAllItems(item);
                if (origin == 3)
                {
                    short saved = context.GetRestoredItemData(cell.X, cell.Y);
                    if (item == null) throw new NullReferenceException();
                    context.LoadItemSaveData(item, saved);
                }
            }
            OnItemModelsInitialized?.Invoke();
        }
    }
}
