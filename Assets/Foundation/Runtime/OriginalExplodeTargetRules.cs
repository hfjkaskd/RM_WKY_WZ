namespace Whitebox.Foundation
{
    public static class OriginalExplodeTargetRules
    {
        // Original ItemExtensions.ExplodeScore 0x2b45368, int[115] at 0x4047238.
        // Numeric identities/weights are read from the binary, not UI tuning.
        private static readonly int[] scores =
        {
            676,676,676,676,676,456976,456976,456976,456976,456976,
            456976,456976,456976,456976,456976,0,0,456976,456976,456976,
            456976,456976,456976,456976,456976,456976,456976,456976,0,456976,
            0,456976,456976,456976,456976,456976,26,456976,456976,456976,
            456976,456976,456976,456976,456976,456976,456976,456976,456976,456976,
            456976,0,456976,456976,0,456976,456976,456976,26,0,
            456976,456976,456976,456976,456976,456976,456976,456976,456976,456976,
            456976,456976,456976,456976,456976,456976,456976,456976,456976,456976,
            456976,0,456976,0,456976,456976,456976,456976,0,456976,
            0,0,456976,456976,456976,456976,456976,456976,0,0,
            456976,456976,456976,456976,456976,456976,456976,456976,456976,0,
            456976,456976,456976,456976,456976
        };
        public static int ItemScore(int itemType)
        {
            uint index = unchecked((uint)(itemType - 1));
            return index < 115 ? scores[(int)index] : 0;
        }
        public static int AsGuidedType(int trigger) // 0x318d028.
        {
            switch (trigger)
            {
                case 10: return 1;
                case 11: return 2;
                case 13: return 3;
                case 27: return 4;
                case 30: return 5;
                case 31: return 6;
                case 35: return 7;
                case 36: return 8;
                default: return 0;
            }
        }
        public static bool IsSingleExploderStrategy(int type) => type == 0 || type == 4 || type == 5 || type == 7; // 0x2b4432c.
        public static bool IsObstacle(int itemType) // 0x2b47a38.
        {
            if (itemType > 16) return itemType != 31 && itemType != 110;
            uint index = unchecked((uint)(itemType - 1));
            return index >= 16 || ((0x7fe0 >> (int)index) & 1) != 0;
        }
    }
}
