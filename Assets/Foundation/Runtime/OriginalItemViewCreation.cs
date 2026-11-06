using System;

namespace Whitebox.Foundation
{
    public interface IOriginalCreatedItemView : IOriginalFillingItemView { void DisableFillMask(); }
    public interface IOriginalItemViewCreationContext
    {
        OriginalBoardItem CreateItem(OriginalItemSettings settings);
        void CreateView(OriginalBoardItem item, OriginalSwapPoint position);
        IOriginalCreatedItemView GetItemView(OriginalBoardItem item);
    }
    public sealed class OriginalItemViewCreation
    {
        private readonly IOriginalItemViewCreationContext context;
        public OriginalItemViewCreation(IOriginalItemViewCreationContext context) { this.context = context; }
        public OriginalBoardItem CreateItemAt(int tiledId, OriginalSwapPoint position) =>
            CreateItemWithSettings(OriginalItemSettings.ForTiledId(tiledId), position); // 0x23f0038.
        public OriginalBoardItem CreateItemWithSettings(OriginalItemSettings settings, OriginalSwapPoint position) // 0x23f00bc.
        {
            var item = context.CreateItem(settings);
            if (item == null) throw new NullReferenceException();
            context.CreateView(item, position);
            var view = context.GetItemView(item);
            if (view == null) throw new NullReferenceException();
            view.DisableFillMask();
            return item;
        }
        public OriginalBoardItem CreateItemAtCell(int tiledId, OriginalGridCell cell) // 0x23f0154.
        {
            if (cell == null) throw new NullReferenceException();
            var item = CreateItemAt(tiledId, new OriginalSwapPoint(cell.X, cell.Y, 0f));
            cell.Mediation.ClearAllItems();
            cell.Mediation.SetAllItems(item);
            return item;
        }
        public IOriginalCreatedItemView GetItemView(OriginalBoardItem item) => context.GetItemView(item);
        public void CreateView(OriginalBoardItem item, OriginalSwapPoint position) => context.CreateView(item, position);
    }
    // Concrete composition of the original predefined/set/creation owners.
    // The application supplies their seed/session/tuning/factory dependencies.
    public sealed class OriginalFillingCreationBackend : IOriginalFillingCreationBackend
    {
        private readonly OriginalPredefinedItemContainer predefined;
        private readonly OriginalFillSetGeneration generation;
        private readonly OriginalItemViewCreation creation;
        public OriginalFillingCreationBackend(OriginalPredefinedItemContainer predefined,
            OriginalFillSetGeneration generation, OriginalItemViewCreation creation)
        { this.predefined = predefined; this.generation = generation; this.creation = creation; }
        public OriginalBoardItem GetPredefinedItem(OriginalFillingCell cell) => predefined.GetPredefinedItem(cell.X);
        public bool HasTarget(int column) => generation.HasTarget(column);
        public int GetRandomTargetColumnItem(OriginalFillingCell cell, int column) => generation.GetRandomTargetColumnItem(cell, column);
        public int GetRandomFillItem(OriginalFillingCell cell) => generation.GetRandomFillItem(cell);
        public int GetRandomColor(OriginalFillingCell cell) => generation.GetRandomColor(cell);
        public OriginalBoardItem CreateItemAt(int id, float x, float y, float z) => creation.CreateItemAt(id, new OriginalSwapPoint(x,y,z));
        public void OnNewItemCreated(int type) => generation.OnNewItemCreated(type);
        public IOriginalFillingItemView GetItemView(OriginalBoardItem item) => creation.GetItemView(item);
        public void CreateView(OriginalBoardItem item, float x, float y, float z) => creation.CreateView(item, new OriginalSwapPoint(x,y,z));
    }
}
