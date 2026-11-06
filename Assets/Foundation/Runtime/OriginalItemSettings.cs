namespace Whitebox.Foundation
{
    // Original 0x48-byte ItemSettings value contract. Managed references keep
    // normal C# value-copy semantics; no reflection or runtime layout tricks.
    public struct OriginalItemSettings
    {
        public readonly int TiledId, LayerCount, MatchType, GoalType;
        public int ItemType;
        public short SaveData;
        public int StaticItemType;
        public bool IsExtraPropeller, IsCreatedByLightball;
        public OriginalGridCell CellModel;
        public int[][] PotionColors;
        public int CurtainId;
        public bool IsDrillMaster, IsJellyViewActive;
        public int TreasureMapId;
        public bool IsHandGiven;
        public int CryptexId;
        public OriginalItemSettings(int tiledId, int itemType, int layerCount, int matchType, int goalType, short saveData = 0) : this() // 0x23f0210.
        {
            TiledId = tiledId; ItemType = itemType; LayerCount = layerCount; MatchType = matchType; GoalType = goalType;
            SaveData = saveData; StaticItemType = -1;
        }
        public static OriginalItemSettings ForTiledId(int tiledId, short saveData = 0) // 0x23ec140.
        {
            int itemType = OriginalTiledItemTypes.AsItemType(tiledId);
            int layers = OriginalTiledItemLayers.AsLayerCount(tiledId);
            int matchType = OriginalLevelRules.ToMatchType(tiledId);
            int goal = matchType == 0 ? OriginalGoalTypes.FromItemType(itemType) : OriginalGoalTypes.FromMatchType(matchType);
            return new OriginalItemSettings(tiledId, itemType, layers, matchType, goal, saveData);
        }
    }
}
