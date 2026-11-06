using UnityEngine;

namespace Whitebox.Foundation
{
    // LevelStatisticsSaveDataHelper 0x23e1238. The 794-bit layout comes from
    // the initializer at 0x23e66e4. The scoped levels have no multiplier or charger.
    internal sealed class OriginalLevelStatisticsSaveData : OriginalGenericSaveDataHelper
    {
        internal const int SavedWordCount = 13;
        private static readonly OriginalGenericSaveDataConfig Layout = new OriginalGenericSaveDataConfig(new[]
        {
            7, 7, 16, 16, 10, 1, 10, 10, 10, 1,
            62, 62, 62, 62, 7, 7, 16,
            16, 16, 16, 16, 16, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            64, 7, 63, 63, 63, 3, 6, 6, 3
        });
        private static readonly int[] SpecialTypes = { 4, 5, 2, 3 };
        private static readonly int[] ComboTypes = { 1, 2, 4, 3, 5, 6, 7, 8, 9, 10 };

        internal OriginalLevelStatisticsSaveData() => SetConfig(Layout);

        internal long[] Capture(OriginalActiveLevelData session, OriginalKingBoardReaction king)
        {
            var special = session.SpecialItemCounts;
            var values = new long[Layout.GetDataCount()];
            values[0] = session.NoPossibleMoveShuffleCount;
            values[1] = session.NoPossibleMoveColoredShuffleCount;
            values[2] = session.FalseSwipeCount;
            values[3] = session.ShownHintCount;
            values[4] = session.UsedHintCount;
            values[5] = session.IsUsefulMoveMade ? 1 : 0;
            values[6] = session.UselessMoveCount;
            values[7] = session.CurrentUselessMoveStreak;
            values[8] = session.MaxUselessMoveStreak;
            for (int i = 0; i < SpecialTypes.Length; i++)
                if (special.Items.TryGetValue(SpecialTypes[i], out var count)) values[10 + i] = count.GetSaveData();
            values[14] = special.TappedLightballCount;
            values[15] = special.SwappedItemCount;
            values[16] = special.AutoTriggeredItemCount;
            for (int color = 1; color <= 5; color++) values[16 + color] = session.MatchItemCounts.GetCount(color);
            for (int i = 0; i < ComboTypes.Length; i++)
                if (special.Combos.TryGetValue(ComboTypes[i], out int count)) values[22 + i] = count;
            values[32] = Mathf.RoundToInt(Time.realtimeSinceStartup - session.LevelStartTime);
            values[33] = special.SpecialItemCountCreatedByCrystal;
            values[37] = king.WorryStartMove;
            values[38] = king.TotalHappyCount;
            values[39] = king.TotalExcitedCount;
            values[40] = king.ReliefMove;
            return PrepareData(values);
        }

        internal void Restore(long[] data, OriginalActiveLevelData session, OriginalKingBoardReaction king)
        {
            if (data.Length == 0) return; // Older local saves omitted the statistics vector.
            SetData(data);
            session.NoPossibleMoveShuffleCount = (int)GetData(0);
            session.NoPossibleMoveColoredShuffleCount = (int)GetData(1);
            session.FalseSwipeCount = (int)GetData(2);
            session.ShownHintCount = (int)GetData(3);
            session.UsedHintCount = (int)GetData(4);
            session.IsUsefulMoveMade = GetData(5) != 0;
            session.UselessMoveCount = (int)GetData(6);
            session.CurrentUselessMoveStreak = (int)GetData(7);
            session.MaxUselessMoveStreak = (int)GetData(8);
            var special = session.SpecialItemCounts;
            for (int i = 0; i < SpecialTypes.Length; i++)
            {
                long saved = GetData(10 + i);
                if (saved == 0) continue;
                var count = new OriginalSpecialItemCount();
                count.LoadSaveData(saved);
                special.Items.Add(SpecialTypes[i], count);
            }
            special.TappedLightballCount = (int)GetData(14);
            special.SwappedItemCount = (int)GetData(15);
            special.AutoTriggeredItemCount = (int)GetData(16);
            for (int color = 1; color <= 5; color++) session.MatchItemCounts.IncrementCount(color, (int)GetData(16 + color));
            for (int i = 0; i < ComboTypes.Length; i++)
            {
                int count = (int)GetData(22 + i);
                if (count > 0) special.Combos.Add(ComboTypes[i], count);
            }
            session.LevelStartTime = Time.realtimeSinceStartup - GetData(32);
            special.SpecialItemCountCreatedByCrystal = (int)GetData(33);
            king.WorryStartMove = (int)GetData(37);
            king.TotalHappyCount = (int)GetData(38);
            king.TotalExcitedCount = (int)GetData(39);
            king.ReliefMove = (int)GetData(40);
        }
    }
}
