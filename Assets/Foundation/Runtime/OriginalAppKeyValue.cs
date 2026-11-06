using System;

namespace Whitebox.Foundation
{
    // AppKeyValue0x25d4844..0x25d4f94, with the same cached application DB,
    // shared typed table and local sync flags. No account-specific connection.
    public sealed class OriginalAppKeyValue : IOriginalLifeClockStorage
    {
        private readonly Func<OriginalDatabaseService> resolve;
        private OriginalDatabaseService cached;
        private OriginalDatabaseService Database => cached ?? (cached = resolve());
        public OriginalAppKeyValue(Func<OriginalDatabaseService> resolve) { this.resolve = resolve; }
        public void Save() => Database.Save();
        public bool HasKey(string key) => OriginalKeyValueTable.HasKey(Database, key);
        public bool DeleteKey(string key) => OriginalKeyValueTable.DeleteKey(Database, key);
        public bool SetString(string key, string value) => OriginalKeyValueTable.SetString(Database, key, value);
        public string GetString(string key, string defaultValue) => OriginalKeyValueTable.GetString(Database, key, defaultValue);
        public bool SetInt(string key, int value) => OriginalKeyValueTable.SetInt(Database, key, value);
        public int GetInt(string key, int defaultValue = 0) => OriginalKeyValueTable.GetInt(Database, key, defaultValue);
        public bool SetLong(string key, long value) => OriginalKeyValueTable.SetLong(Database, key, value);
        public long GetLong(string key, long defaultValue = 0) => OriginalKeyValueTable.GetLong(Database, key, defaultValue);
        public int IncrementInt(string key, int increment = 1, int defaultValue = 0)
        { int value = unchecked(GetInt(key, defaultValue) + increment); SetInt(key, value); return value; }
        public long IncrementLong(string key, long increment, long defaultValue = 0)
        { long value = unchecked(GetLong(key, defaultValue) + increment); SetLong(key, value); return value; }
    }
}
