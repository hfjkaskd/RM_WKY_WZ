namespace Whitebox.Foundation
{
    // ItemExtensions lookup tables, read by the original ARM64 bodies.
    // These are original numeric type identities, not designer tuning values.
    public static class OriginalGoalTypes
    {
        private static readonly int[] ItemGoals =
        {
            7,8,9,10,11,12,15,16,17,18,19,22,20,21,23,24,25,26,27,28,
            0,29,31,32,0,34,35,0,36,37,38,39,40,0,41,43,44,45,0,
            47,47,47,47,48,50,51,52,54,27,55,0,58,59,0,60,61,62,63,
            0,64,65,0,67,68,69,0,71,72,0,73,74,76,75,77,0,0,78,79,
            80,81,0,82,83,85,86,87,0,0,88,0,0,0,89,90,91,92,93,0,
            94,0,95,96,97,98,99,0,100,101,102,0,103,104,105
        };
        private static readonly int[] MatchGoals = { 1, 2, 4, 6, 5, 3 };
        private static readonly int[] StaticGoals = { 30,13,49,14,0,42,46,53,56,66,70,0,0,14,0,0,84 };

        public static int FromItemType(int itemType) // 0x2b452a0.
        {
            uint index = unchecked((uint)(itemType - 2));
            return index < 113u ? ItemGoals[(int)index] : 0;
        }
        public static int FromMatchType(int matchType) // 0x2b452c4.
        {
            uint index = unchecked((uint)(matchType - 1));
            return index < 6u ? MatchGoals[(int)index] : 0;
        }
        public static int FromStaticItemType(int itemType) // 0x2b45064.
        {
            uint index = unchecked((uint)(itemType - 1));
            return index < 17u ? StaticGoals[(int)index] : 0;
        }
        // Goal selection in ItemCreator.GetSettingsForTileId 0x23ec140.
        public static int FromTiledId(int tiledId)
        {
            int matchType = OriginalLevelRules.ToMatchType(tiledId);
            return matchType == 0 ? FromItemType(OriginalTiledItemTypes.AsItemType(tiledId)) : FromMatchType(matchType);
        }
        public static bool ShouldPlayParticleOnCollect(int goalType) // 0x2b45614.
        {
            if (goalType < 56)
            {
                if (goalType < 22) return InMask(goalType, 1, 21, 0x12003fu);
                if (goalType < 29) return goalType == 23 || goalType == 28;
                return InMask(goalType, 34, 22, 0x296069u);
            }
            if (goalType < 89) return InMask(goalType, 67, 22, 0x208f21u);
            if (goalType < 96) return (goalType | 4) == 95;
            return goalType == 99 || goalType == 105;
        }
        private static bool InMask(int goalType, int first, uint length, uint mask)
        {
            uint index = unchecked((uint)(goalType - first));
            return index < length && ((mask >> (int)index) & 1u) != 0;
        }
    }
}
