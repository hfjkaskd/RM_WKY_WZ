using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // InitialItemAmountCalculator. Numeric identities and pool limits are native
    // logic, rather than artwork parameters. Avoid its Enum/LINQ constructor.
    public sealed class OriginalInitialItemAmounts
    {
        public readonly int[] ItemAmounts = new int[116];
        public readonly int[] StaticItemAmounts = new int[20];
        public int TotalGoalCountForStoryLevel { get; private set; }
        private readonly Dictionary<int, OriginalGoalTuple> goalPool = new Dictionary<int, OriginalGoalTuple>();
        private readonly Dictionary<int, int> countPool = new Dictionary<int, int>();
        private readonly Func<int, int> particleAmount;
        private readonly Func<bool> fillingHelmetPiggy;
        public OriginalInitialItemAmounts(Func<int, int> particleAmount, Func<bool> fillingHelmetPiggy)
        { this.particleAmount = particleAmount; this.fillingHelmetPiggy = fillingHelmetPiggy; }
        public int GetItemAmount(int type) => ItemAmounts[type]; // 0x33f2518.
        public int GetStaticItemAmount(int type) => StaticItemAmounts[type]; // 0x33f2548.
        public bool IsThereItemInLevel(int type) => GetItemAmount(type) > 0; // ItemType overload; numeric storage is shared.
        public bool IsThereStaticItemInLevel(int type) => GetStaticItemAmount(type) > 0; // 0x1e19780.
        public bool IsThereNonFallBlockingAboveStaticItemInLevel() => IsThereStaticItemInLevel(4) || IsThereStaticItemInLevel(7); // 0x1e197a8.
        internal int ItemCountDiff(int type, int target) => ItemAmounts[type] < target ? unchecked(target - ItemAmounts[type]) : 0;
        internal int StaticItemCountDiff(int type, int target) => StaticItemAmounts[type] < target ? unchecked(target - StaticItemAmounts[type]) : 0;
        internal void CalculateNonGoalItemAmounts(int type, bool isStatic, Dictionary<int, int> counts) // 0x33f2420.
        {
            counts.TryGetValue(type, out int amount);
            if (isStatic) StaticItemAmounts[unchecked(-type)] = amount;
            else ItemAmounts[type] = amount;
        }
        internal void CalculateItemAmountsBasedOnGoalType(int goal, int target, Dictionary<int, int> counts) // 0x33f11bc.
        {
            int amount = Math.Min(target, particleAmount(74));
            int count;
            switch (goal)
            {
                case 11: ItemAmounts[6] = amount; break;
                case 12: ItemAmounts[7] = amount; break;
                case 13: AddGrass(target); break;
                case 14:
                    StaticItemAmounts[4] = unchecked(StaticItemAmounts[4] + StaticItemCountDiff(4, amount));
                    counts.TryGetValue(-14, out count); StaticItemAmounts[14] = count; break;
                case 15: counts.TryGetValue(8, out count); ItemAmounts[8] = count; break;
                case 16: ItemAmounts[9] = amount; break;
                case 17: ItemAmounts[10] = amount; break;
                case 18: counts.TryGetValue(11, out count); ItemAmounts[11] = Math.Min(amount, count); break;
                case 19: counts.TryGetValue(12, out count); ItemAmounts[12] = count; break;
                case 20: ItemAmounts[14] = amount; break;
                case 21: counts.TryGetValue(15, out count); ItemAmounts[15] = Math.Min(count, 75); break;
                case 22: count = counts[13]; ItemAmounts[13] = count; AddGrass(unchecked(count << 2)); break;
                case 23:
                    ItemAmounts[16] = amount; counts.TryGetValue(26, out count);
                    if (count > 0) ItemAmounts[26] = count; break;
                case 24: ItemAmounts[17] = amount; break;
                case 25: ItemAmounts[18] = amount; break;
                case 26: ItemAmounts[19] = amount; break;
                case 27:
                    counts.TryGetValue(50, out count);
                    if (count > 0) ItemAmounts[50] = count;
                    else if (fillingHelmetPiggy()) ItemAmounts[50] = 5;
                    if (count <= amount) amount = unchecked(amount - count + 1);
                    ItemAmounts[20] = amount;
                    counts.TryGetValue(67, out count);
                    if (count > 0) { ItemAmounts[67] = count; ItemAmounts[50] = unchecked(count * 3); }
                    break;
                case 28: ItemAmounts[21] = amount; break;
                case 29:
                    amount = Math.Min(amount, 50); ItemAmounts[23] = amount;
                    StaticItemAmounts[1] = unchecked(StaticItemAmounts[1] + Math.Min(unchecked(amount << 1), 75)); break;
                case 30: StaticItemAmounts[1] = unchecked(StaticItemAmounts[1] + amount); break;
                case 31: counts.TryGetValue(24, out count); ItemAmounts[24] = count; break;
                case 32: counts.TryGetValue(25, out count); ItemAmounts[25] = count; break;
                case 34: ItemAmounts[27] = amount; break;
                case 35: counts.TryGetValue(28, out count); ItemAmounts[28] = count; break;
                case 36: counts.TryGetValue(30, out count); ItemAmounts[30] = count; break;
                case 37: ItemAmounts[31] = amount; break;
                case 38: ItemAmounts[32] = amount; break;
                case 39: counts.TryGetValue(33, out count); ItemAmounts[33] = Math.Min(amount, count); break;
                case 40: ItemAmounts[34] = amount; break;
                case 41: ItemAmounts[36] = amount; break;
                case 43: counts.TryGetValue(37, out count); ItemAmounts[37] = count; break;
                case 44: ItemAmounts[38] = amount; break;
                case 45: counts.TryGetValue(39, out count); ItemAmounts[39] = count; break;
                case 46: StaticItemAmounts[7] = amount; break;
                case 47: ItemAmounts[44] = amount; break;
                case 48: counts.TryGetValue(45, out count); ItemAmounts[45] = Math.Min(amount, count); break;
                case 50: ItemAmounts[46] = amount; break;
                case 51: counts.TryGetValue(47, out count); ItemAmounts[47] = count; break;
                case 54:
                    amount = Math.Min(amount, 50); ItemAmounts[49] = amount;
                    StaticItemAmounts[4] = unchecked(StaticItemAmounts[4] + StaticItemCountDiff(4, Math.Min(unchecked(amount << 1),75))); break;
                case 55: ItemAmounts[51] = amount; break;
                case 56: StaticItemAmounts[9] = amount; break;
                case 58: counts.TryGetValue(53, out count); ItemAmounts[53] = count; break;
                case 59: counts.TryGetValue(54, out count); ItemAmounts[54] = count; break;
                case 60: counts.TryGetValue(56, out count); ItemAmounts[56] = count; break;
                case 61: counts.TryGetValue(57, out count); ItemAmounts[57] = count; break;
                case 62: counts.TryGetValue(58, out count); ItemAmounts[58] = count; break;
                case 63: counts.TryGetValue(59, out count); ItemAmounts[59] = count; break;
                case 64: counts.TryGetValue(61, out count); ItemAmounts[61] = count; break;
                case 65: counts.TryGetValue(62, out count); ItemAmounts[62] = count; break;
                case 66:
                    StaticItemAmounts[10] = amount; counts.TryGetValue(63, out count);
                    ItemAmounts[63] = amount / 2 < count ? count : amount; break;
                case 67: counts.TryGetValue(64, out count); ItemAmounts[64] = count; break;
                case 68: ItemAmounts[65] = amount; break;
                case 69: counts.TryGetValue(66, out count); ItemAmounts[66] = count; break;
                case 70: StaticItemAmounts[11] = amount; break;
                case 71: ItemAmounts[68] = amount; break;
                case 72: counts.TryGetValue(69, out count); ItemAmounts[69] = count; break;
                case 73: counts.TryGetValue(71, out count); ItemAmounts[71] = count; break;
                case 74: ItemAmounts[72] = amount; break;
                case 75: ItemAmounts[74] = amount; break;
                case 76: ItemAmounts[73] = amount; break;
                case 77:
                    counts.TryGetValue(75, out count); ItemAmounts[75] = count;
                    counts.TryGetValue(76, out count); ItemAmounts[76] = count; break;
                case 78: ItemAmounts[78] = amount; break;
                case 79: ItemAmounts[79] = amount; break;
                case 80: counts.TryGetValue(80, out count); ItemAmounts[80] = count; break;
                case 81: ItemAmounts[81] = amount; break;
                case 82: ItemAmounts[83] = amount; break;
                case 83: ItemAmounts[84] = amount; break;
                case 84: StaticItemAmounts[17] = amount; break;
                case 85: counts.TryGetValue(85, out count); ItemAmounts[85] = count; break;
                case 86: ItemAmounts[86] = amount; break;
                case 87: ItemAmounts[87] = amount; break;
                case 88: ItemAmounts[90] = amount; ItemAmounts[89] = 1; break;
                case 89: ItemAmounts[94] = amount; break;
                case 90: ItemAmounts[95] = amount; break;
                case 91: ItemAmounts[96] = amount; break;
                case 92: ItemAmounts[97] = amount; break;
                case 93: ItemAmounts[98] = amount; break;
                case 94: ItemAmounts[100] = amount; break;
                case 95: counts.TryGetValue(102, out count); ItemAmounts[102] = count; break;
                case 96: ItemAmounts[103] = amount; break;
                case 97: counts.TryGetValue(104, out count); ItemAmounts[104] = count; break;
                case 98: ItemAmounts[105] = amount; break;
                case 99: counts.TryGetValue(106, out count); ItemAmounts[106] = count; break;
                case 100: ItemAmounts[108] = amount; break;
                case 101: counts.TryGetValue(109, out count); ItemAmounts[109] = count; break;
                case 102: counts.TryGetValue(110, out count); ItemAmounts[110] = count; break;
                case 103: counts.TryGetValue(112, out count); ItemAmounts[112] = count; break;
                case 104: counts.TryGetValue(113, out count); ItemAmounts[113] = count; break;
                case 105: counts.TryGetValue(114, out count); ItemAmounts[114] = Math.Min(amount, count); break;
            }
        }
        private void AddGrass(int target)
        {
            StaticItemAmounts[2] = unchecked(StaticItemAmounts[2] + StaticItemCountDiff(2, Math.Min(target,100)));
        }
        internal Dictionary<int, OriginalGoalTuple> AggregateGoalsForPooling(List<Dictionary<int, OriginalGoalTuple>> goals) // 0x33f0a48.
        {
            goalPool.Clear();
            foreach (var board in goals)
                foreach (var entry in board)
                    if (!goalPool.ContainsKey(entry.Key)) goalPool.Add(entry.Key, entry.Value);
                    else if (goalPool[entry.Key].TargetCount < entry.Value.TargetCount) goalPool[entry.Key] = entry.Value;
            return goalPool;
        }
        internal Dictionary<int, int> AggregateCountsForPooling(List<Dictionary<int, int>> counts) // 0x33f0e20.
        {
            countPool.Clear();
            foreach (var board in counts)
                foreach (var entry in board)
                    if (!countPool.ContainsKey(entry.Key)) countPool.Add(entry.Key, entry.Value);
                    else if (countPool[entry.Key] < entry.Value) countPool[entry.Key] = entry.Value;
            return countPool;
        }
        public void CreateItemLists(List<Dictionary<int, OriginalGoalTuple>> goals, List<Dictionary<int, int>> counts,
            int curtainCellCount, int drillCount, int chainCount, int jellyReceivableCellCount, bool isPouchAvailableFalling,
            int treasureMapCellCount, int coilTargetCount, int logCollectCount, int cryptexCellCount, int turtleTotalCount,
            int colorMixerTargetCount, int itemCurtainCellCount) // 0x33f0500.
        {
            Array.Clear(ItemAmounts, 0, ItemAmounts.Length); Array.Clear(StaticItemAmounts, 0, StaticItemAmounts.Length);
            var aggregatedGoals = AggregateGoalsForPooling(goals);
            var aggregatedCounts = AggregateCountsForPooling(counts);
            TotalGoalCountForStoryLevel = 0;
            foreach (var entry in aggregatedGoals)
            {
                CalculateItemAmountsBasedOnGoalType(entry.Value.GoalType, entry.Value.TargetCount, aggregatedCounts);
                TotalGoalCountForStoryLevel = unchecked(TotalGoalCountForStoryLevel + CollectPoolFactor(entry.Key) * entry.Value.TargetCount);
            }
            foreach (var entry in aggregatedCounts)
            {
                int goal = entry.Key < 0 ? OriginalGoalTypes.FromStaticItemType(unchecked(-entry.Key)) : OriginalGoalTypes.FromItemType(entry.Key);
                if (goal == 0) CalculateNonGoalItemAmounts(entry.Key, entry.Key < 0, aggregatedCounts);
                else if (!aggregatedGoals.ContainsKey(goal))
                {
                    CalculateItemAmountsBasedOnGoalType(goal, entry.Value, aggregatedCounts);
                    TotalGoalCountForStoryLevel = unchecked(TotalGoalCountForStoryLevel + CollectPoolFactor(goal) * entry.Value);
                }
            }
            StaticItemAmounts[5] = curtainCellCount; StaticItemAmounts[19] = itemCurtainCellCount;
            StaticItemAmounts[16] = cryptexCellCount; ItemAmounts[29] = drillCount;
            StaticItemAmounts[6] = chainCount; StaticItemAmounts[3] = jellyReceivableCellCount;
            if (isPouchAvailableFalling) ItemAmounts[37] = Math.Max(ItemAmounts[37], 15);
            StaticItemAmounts[8] = treasureMapCellCount; ItemAmounts[48] = treasureMapCellCount;
            ItemAmounts[80] = logCollectCount;
            ItemAmounts[62] = unchecked(ItemAmounts[62] + ItemCountDiff(62, coilTargetCount));
            ItemAmounts[110] = unchecked(ItemAmounts[110] + ItemCountDiff(110, turtleTotalCount));
            ItemAmounts[113] = unchecked(ItemAmounts[113] + ItemCountDiff(113, colorMixerTargetCount));
        }
        public static int CollectPoolFactor(int goal) => goal == 22 ? 17 : goal == 29 ? 10 : 1; // 0x2b455f4.
        public static int GetParticleAmount(int amount, bool lowEnd) => lowEnd ? Math.Max(amount / 2, 1) : amount; // 0x2b44b10.
    }
}
