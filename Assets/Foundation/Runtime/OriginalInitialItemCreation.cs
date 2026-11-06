using System;

namespace Whitebox.Foundation
{
    public interface IOriginalInitialItemCreationContext
    {
        int[][] PotionColors { get; }
        bool ContainsDrillMasterPoint(OriginalGridCell cell);
        OriginalBoardItem CreateItem(OriginalItemSettings settings);
        void OnNewItemCreated(int itemType);
    }
    // ItemCreator start/predefined model orchestration. The complete concrete
    // model factory and application services remain required typed owners.
    public sealed class OriginalInitialItemCreation : IOriginalPredefinedItemCreator
    {
        private readonly OriginalLevelSetSelection[] sets;
        private readonly OriginalLevelRandomSource random;
        private readonly OriginalPredefinedItemContainer predefined;
        private readonly IOriginalInitialItemCreationContext context;
        public OriginalInitialItemCreation(OriginalLevelSetSelection[] sets, OriginalLevelRandomSource random,
            OriginalPredefinedItemContainer predefined, IOriginalInitialItemCreationContext context)
        { this.sets = sets; this.random = random; this.predefined = predefined; this.context = context; }
        public static bool IsASet(int tiledId) => unchecked((uint)(tiledId - 14)) < 3; // 0x191159c.
        public static int GetSetIndex(int tiledId) => IsASet(tiledId) ? unchecked(tiledId - 14) : -1; // 0x1911738.
        public int GetRandomItemTypeFromSet(int tiledId) // 0x23ec07c; all elements, unweighted, regardless of fill availability.
        {
            var set = sets[GetSetIndex(tiledId)];
            int index = random.DifficultyNext(0, set.ElementCount);
            return set.GetElementId(index);
        }
        public OriginalBoardItem CreateItemModelForStart(int tiledId, int originalTiledId, OriginalGridCell cell) // 0x23ebf0c.
        {
            OriginalItemSettings settings;
            if (IsASet(tiledId)) settings = OriginalItemSettings.ForTiledId(GetRandomItemTypeFromSet(tiledId));
            else
            {
                settings = OriginalItemSettings.ForTiledId(tiledId);
                settings.CellModel = cell;
                settings.PotionColors = context.PotionColors;
                if (cell != null) settings.IsDrillMaster = context.ContainsDrillMasterPoint(cell);
            }
            settings.IsHandGiven = !IsASet(originalTiledId);
            var item = context.CreateItem(settings);
            if (item == null) throw new NullReferenceException();
            context.OnNewItemCreated(item.ItemType);
            return item;
        }
        public void AddPredefinedItem(int column, OriginalBoardItem item) => predefined.AddPredefinedItem(column, item); // 0x23eb8c4.
    }
}
