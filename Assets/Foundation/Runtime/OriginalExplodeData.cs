namespace Whitebox.Foundation
{
    public struct OriginalCellPoint
    {
        public readonly int X, Y;
        public static readonly OriginalCellPoint Default = new OriginalCellPoint(-1, -1); // 0x343bac8.
        public OriginalCellPoint(int x, int y) { X = x; Y = y; }
    }
    public struct OriginalItemIdentifier
    {
        public int ItemType, StaticItemType, ItemGroupId;
        public static OriginalItemIdentifier CreateDefault() // 0x2b48818.
        {
            return new OriginalItemIdentifier { ItemType = 0, StaticItemType = -1, ItemGroupId = -1 };
        }
    }
    // ExplodeData ctor 0x2eeb8ac. Keep the native value-copy semantics when
    // passing a trigger to multiple cells; default identifier is not all zero.
    public struct OriginalExplodeData
    {
        public OriginalCellPoint Point;
        public readonly int Trigger, Id, MatchType;
        public bool CanSpreadJelly, IsHorizontalRocket;
        public int ComboId;
        public OriginalItemIdentifier LastTargetItemIdentifier;
        public int ExploderIndex;
        public float IncomingX, IncomingY, IncomingZ;

        public OriginalExplodeData(int trigger, int id, OriginalCellPoint point, int matchType,
            bool canSpreadJelly, bool isHorizontalRocket = false, int comboId = -1, int exploderIndex = -1)
        {
            Point = point; Trigger = trigger; Id = id; MatchType = matchType;
            CanSpreadJelly = canSpreadJelly; IsHorizontalRocket = isHorizontalRocket;
            ComboId = comboId; ExploderIndex = exploderIndex;
            LastTargetItemIdentifier = OriginalItemIdentifier.CreateDefault();
            IncomingX = IncomingY = IncomingZ = 0f;
        }
        public bool ShouldExplodeAll => Trigger == 25; // 0x2eeb93c.
    }

    // Exploder 0x2eebbc0..0x2eebf70: one shared counter across all sources.
    public static class OriginalExploder
    {
        private static int id;
        public static void Reset() { id = 0; }
        public static bool IsBooster(int trigger, bool includeAdminHammer = false) // 0x318d000.
        {
            return trigger == 17 ? includeAdminHammer : unchecked((uint)(trigger - 18)) < 3;
        }
        public static OriginalExplodeData Next(int trigger) => Next(trigger, OriginalCellPoint.Default);
        public static OriginalExplodeData Next(int trigger, OriginalCellPoint point)
        {
            id = unchecked(id + 1);
            return new OriginalExplodeData(trigger, id, point, 0, IsBooster(trigger));
        }
        public static OriginalExplodeData NextWithMatchType(int trigger, int matchType)
        {
            id = unchecked(id + 1);
            return new OriginalExplodeData(trigger, id, OriginalCellPoint.Default, matchType, IsBooster(trigger));
        }
        public static OriginalExplodeData Next(int trigger, bool canSpreadJelly) => Next(trigger, canSpreadJelly, OriginalCellPoint.Default);
        public static OriginalExplodeData Next(int trigger, bool canSpreadJelly, OriginalCellPoint point)
        {
            id = unchecked(id + 1);
            return new OriginalExplodeData(trigger, id, point, 0, canSpreadJelly);
        }
    }
}
