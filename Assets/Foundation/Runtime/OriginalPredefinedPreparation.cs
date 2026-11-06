using System;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    public interface IOriginalPredefinedDataProvider
    {
        int PredefinedCount { get; }
        int GetPredefinedColumn(int entry);
        int GetPredefinedItemCount(int entry);
        int GetPredefinedItem(int entry, int index);
    }
    public interface IOriginalPredefinedItemCreator
    {
        OriginalBoardItem CreateItemModelForStart(int tiledId, int originalTiledId, OriginalGridCell cell);
        void AddPredefinedItem(int column, OriginalBoardItem item);
    }
    public interface IOriginalPredefinedPreparationContext
    {
        OriginalLevelRandomSource GetLevelRandom();
        int SelectMatchTileColor(int tiledId, OriginalLevelSetSelection firstSet, OriginalLevelRandomSource random);
        IOriginalPredefinedItemCreator ItemCreator { get; }
    }
    // Decoded original data access. Y is intentionally absent from the parser
    // contract: ParsePredefined uses source entry/Ids order within each column.
    public sealed class OriginalDecodedPredefinedData : IOriginalPredefinedDataProvider
    {
        private readonly FTiledLevel level;
        public OriginalDecodedPredefinedData(FTiledLevel level) { this.level = level; }
        public int PredefinedCount => level.Predefined == null ? 0 : level.Predefined.Length;
        public int GetPredefinedColumn(int entry) => level.Predefined[entry].X;
        public int GetPredefinedItemCount(int entry) => level.Predefined[entry].Ids == null ? 0 : level.Predefined[entry].Ids.Length;
        public int GetPredefinedItem(int entry, int index) => level.Predefined[entry].Ids[index];
    }
    public static class OriginalPredefinedPreparation
    {
        // LevelParser.ParsePredefined 0x1c24788. Model construction and the
        // shared placeholder-color cache retain application-owned typed services.
        public static void Parse(IOriginalPredefinedDataProvider data, OriginalLevelSetSelection[] sets,
            int mustHaveColor, int replacedColor, IOriginalPredefinedPreparationContext context)
        {
            var random = context.GetLevelRandom(); // Retrieved even for null/empty data.
            if (data == null) throw new NullReferenceException();
            for (int entry = 0; entry < data.PredefinedCount; entry++)
            {
                int column = data.GetPredefinedColumn(entry);
                for (int index = 0; index < data.GetPredefinedItemCount(entry); index++)
                {
                    int id = OriginalLevelRules.ReplacePredefinedColor(data.GetPredefinedItem(entry, index), mustHaveColor, replacedColor);
                    if (sets == null) throw new NullReferenceException(); // Native reads sets before the placeholder check.
                    if (unchecked((uint)(id - 211)) < 3 && sets.Length != 0)
                        id = context.SelectMatchTileColor(id, sets[0], random);
                    var item = context.ItemCreator.CreateItemModelForStart(id, id, null);
                    // Re-read the creator after model construction, as in the original.
                    context.ItemCreator.AddPredefinedItem(column, item);
                }
            }
        }
    }
}
