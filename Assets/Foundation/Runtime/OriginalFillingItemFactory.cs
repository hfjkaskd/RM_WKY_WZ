using System;

namespace Whitebox.Foundation
{
    public interface IOriginalFillingItemView
    {
        void SetPosition(float x, float y, float z);
        void EnableFillMask();
    }

    // These boundaries must be implemented by restored containers, set selection
    // and prefab-backed item/view creation. They are not editor fallback hooks.
    public interface IOriginalFillingCreationBackend
    {
        OriginalBoardItem GetPredefinedItem(OriginalFillingCell cell);
        bool HasTarget(int column);
        int GetRandomTargetColumnItem(OriginalFillingCell cell, int column);
        int GetRandomFillItem(OriginalFillingCell cell);
        int GetRandomColor(OriginalFillingCell cell);
        OriginalBoardItem CreateItemAt(int tiledId, float x, float y, float z);
        void OnNewItemCreated(int itemType);
        IOriginalFillingItemView GetItemView(OriginalBoardItem item);
        void CreateView(OriginalBoardItem item, float x, float y, float z);
    }

    public interface IOriginalFillingPlacementSpace
    {
        float OffsetX { get; }
        float OffsetY { get; }
        void GetCellViewPosition(OriginalFillingCell cell, out float x, out float y);
    }

    // ItemCreator.CreateItemForFillingCellAt 0x23ef7cc. Container selection,
    // tuning, concrete item models and visual implementation remain boundaries.
    public sealed class OriginalFillingItemFactory : IOriginalFillingItemCreation
    {
        private readonly IOriginalColumnRefill columns;
        private readonly IOriginalFillingCreationBackend backend;
        private readonly IOriginalFillingPlacementSpace space;
        public OriginalFillingItemFactory(IOriginalColumnRefill columns,
            IOriginalFillingCreationBackend backend, IOriginalFillingPlacementSpace space)
        {
            this.columns = columns ?? throw new ArgumentNullException(nameof(columns));
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
            this.space = space ?? throw new ArgumentNullException(nameof(space));
        }

        public OriginalBoardItem CreateItemForFillingCellAt(float x, float y, OriginalFillingCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if (!cell.IsReadyToRefill(columns)) return null;
            OriginalBoardItem item = cell.GetQueuedItemFromTopBoard();
            if (item != null)
            {
                item.Mediator.ClearFromAllRegisteredCells();
                var queuedView = backend.GetItemView(item);
                space.GetCellViewPosition(cell, out float cellX, out float cellY);
                if (queuedView == null) throw new InvalidOperationException("Queued item has no view.");
                queuedView.SetPosition(cellX + space.OffsetX, cellY + space.OffsetY, 0f);
            }
            // The original overwrites the queued candidate even when the
            // predefined container returns null. Relocation already happened.
            if (cell.IsPredefined) item = backend.GetPredefinedItem(cell);
            if (item == null)
            {
                int id = 0;
                if (backend.HasTarget(cell.X)) id = backend.GetRandomTargetColumnItem(cell, cell.X);
                if (id == 0)
                {
                    id = backend.GetRandomFillItem(cell);
                    if (id == 0) id = backend.GetRandomColor(cell);
                }
                item = backend.CreateItemAt(id, x, y, 0f);
                if (item == null) throw new InvalidOperationException("Item creation returned null.");
                backend.OnNewItemCreated(item.ItemType);
            }
            else if (!item.HasView) backend.CreateView(item, x, y, 0f);

            var view = backend.GetItemView(item);
            if (view == null) throw new InvalidOperationException("Filling item has no view.");
            view.EnableFillMask();
            cell.AddCreatedItem(item);
            return item;
        }
    }
}
