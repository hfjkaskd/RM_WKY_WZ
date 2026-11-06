using System.Collections.Generic;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    // Decoded-data portion of LevelParser.Parse 0x1c20110. This only prepares
    // the actual inputs to InitialItemAmountCalculator, not session startup.
    public static class OriginalLevelItemAmountPreparation
    {
        public static void Create(FTiledLevel level, OriginalInitialItemAmounts amounts,
            List<Dictionary<int, OriginalGoalTuple>> goals, List<Dictionary<int, int>> counts,
            List<Dictionary<int, OriginalLimitTuple>> limits)
        {
            int curtains = 0, itemCurtains = 0, cryptexes = 0, treasureMaps = 0, turtles = 0;
            if (level.Curtains != null) for (int i = 0; i < level.Curtains.Length; i++) curtains = unchecked(curtains + level.Curtains[i].CellCount);
            if (level.ItemCurtains != null) for (int i = 0; i < level.ItemCurtains.Length; i++) itemCurtains = unchecked(itemCurtains + level.ItemCurtains[i].CellCount);
            if (level.Cryptexes != null) for (int i = 0; i < level.Cryptexes.Length; i++) cryptexes = unchecked(cryptexes + level.Cryptexes[i].CellCount);
            if (level.Treasuremaps != null) for (int i = 0; i < level.Treasuremaps.Length; i++) treasureMaps = unchecked(treasureMaps + (level.Treasuremaps[i].Cells == null ? 0 : level.Treasuremaps[i].Cells.Length));
            // GetTurtleTotalCount 0x1c21dfc sums limits, not goals or current items.
            if (limits != null) for (int i = 0; i < limits.Count; i++) if (limits[i].TryGetValue(102, out var limit)) turtles = unchecked(turtles + limit.Limit);
            amounts.CreateItemLists(goals, counts, curtains, level.Drills == null ? 0 : level.Drills.Length,
                level.ChainCount, level.JellyReceivableCellCount, level.PouchConfig != null && level.PouchConfig.IsAvailableFalling,
                treasureMaps, TotalTarget(level.Coils), TotalTarget(level.Logs), cryptexes, turtles, TotalTarget(level.ColorMixers), itemCurtains);
        }
        // GetCoilsTotalTarget 0x1c219b0 / GetLogTotalTarget 0x1c21c44 /
        // GetColorMixersTotalTarget 0x1c21b68 all sum original target fields.
        private static int TotalTarget(FCurtainData[] values)
        {
            int total = 0;
            if (values != null) for (int i = 0; i < values.Length; i++) total = unchecked(total + values[i].Target);
            return total;
        }
    }
}
