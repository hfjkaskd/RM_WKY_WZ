namespace Whitebox.Foundation
{
    // Literal keys resolved from ELF relocations and ScriptString metadata.
    // This adapter deliberately has no fresh-user defaults: those belong to
    // the original account-loading constructors, not to reward persistence.
    public sealed class OriginalAccountStorage
    {
        public const string CoinsKey = "Coins", StarsKey = "Stars", EventInventoryKey = "EventInventory",
            LevelKey = "Level", LeagueLevelKey = "LeagueLevel", LevelTryCountKey = "LevelTryCount",
            HardLevelBackgroundIndexKey = "HardLevelBackgroundIndex";
        private readonly OriginalUserKeyValue values;
        public OriginalAccountStorage(OriginalUserKeyValue values) { this.values = values; }
        public void WriteCoin(long value) => values.SetLong(CoinsKey, value);
        public void WriteStar(int value) => values.SetInt(StarsKey, value);
        public void WriteEventInventory(long value) => values.SetLong(EventInventoryKey, value);
        public void WriteLevel(int value) => values.SetInt(LevelKey, value);
        public void WriteLeagueLevel(int value) => values.SetInt(LeagueLevelKey, value);
        public void ClearLevelTryCount() => values.SetInt(LevelTryCountKey, 0);
        public void ClearHardLevelBackgroundIndex() => values.SetInt(HardLevelBackgroundIndexKey, 0);
        public int ReadLevel(int defaultValue) => values.GetInt(LevelKey, defaultValue);
        public int ReadLeagueLevel(int defaultValue) => values.GetInt(LeagueLevelKey, defaultValue);
        public long ReadCoin(long defaultValue) => values.GetLong(CoinsKey, defaultValue);
        public int ReadStar(int defaultValue) => values.GetInt(StarsKey, defaultValue);
        public long ReadEventInventory(long defaultValue) => values.GetLong(EventInventoryKey, defaultValue);
    }
}
