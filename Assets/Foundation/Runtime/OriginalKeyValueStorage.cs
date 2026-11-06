using System;

namespace Whitebox.Foundation
{
    public struct OriginalKeyValue
    {
        public string Key, Value;
        public int Synced, SyncBlocked;
    }

    // Strongly typed boundary replacing the original reflection-based ORM.
    // A missing row is default(OriginalKeyValue), including its null Key.
    public interface IOriginalKeyValueConnection : IDisposable
    {
        OriginalKeyValue Find(string key);
        int InsertOrReplace(OriginalKeyValue row);
        int Delete(string key);
        int UpdateSynced(string key, int value);
        int UpdateSyncBlocked(string key, int value);
        void BeginTransaction();
        void Commit();
    }

    public sealed class OriginalDatabaseService
    {
        public const int UserSaveInterval = 61, AppSaveInterval = 67;
        private readonly int saveInterval;
        private readonly Action cleanCorruption;
        public IOriginalKeyValueConnection Connection { get; internal set; }
        public bool ShouldSave { get; private set; }
        public OriginalDatabaseService(IOriginalKeyValueConnection connection, int saveInterval, Action cleanCorruption)
        { Connection = connection; this.saveInterval = saveInterval; this.cleanCorruption = cleanCorruption; }
        public OriginalKeyValue Find(string key) => Connection.Find(key); // 0x1fdfcf4.
        public int InsertOrReplace(OriginalKeyValue row) // 0x25ce920.
        { ShouldSave = true; return Connection.InsertOrReplace(row); }
        public int Delete(string key) // 0x1fdf0a4: dirty even for a missing key.
        { ShouldSave = true; return Connection.Delete(key); }
        public int UpdateSynced(string key, int value)
        { ShouldSave = true; return Connection.UpdateSynced(key, value); }
        public int UpdateSyncBlocked(string key, int value)
        { ShouldSave = true; return Connection.UpdateSyncBlocked(key, value); }
        public void ManualUpdate(int frameCount) // 0x25d21f8.
        {
            if (!ShouldSave) return;
            // ARM64 SDIV+MSUB: zero divisor and MIN/-1 do not throw.
            int remainder = saveInterval == 0 ? frameCount :
                saveInterval == -1 ? 0 : frameCount % saveInterval;
            if (remainder == 0) Save();
        }
        public void Save() // 0x25d2234: clear dirty BEFORE commit, then re-read connection.
        { ShouldSave = false; Connection.Commit(); Connection.BeginTransaction(); }
        public void OnApplicationResume() => Connection.BeginTransaction(); // 0x25d2268.
        public void OnApplicationPause() { Connection.Commit(); cleanCorruption?.Invoke(); } // 0x25d2284.
        public void OnApplicationQuit() { Connection.Commit(); cleanCorruption?.Invoke(); } // 0x25d23ec.
    }

    public static class OriginalKeyValueTable
    {
        public static bool HasKey(OriginalDatabaseService db, string key) => db.Find(key).Key != null;
        public static bool DeleteKey(OriginalDatabaseService db, string key) => db.Delete(key) > 0;
        public static bool SetString(OriginalDatabaseService db, string key, string value, bool synced = false)
        {
            return db.InsertOrReplace(new OriginalKeyValue { Key = key, Value = value,
                Synced = synced ? 1 : 0, SyncBlocked = synced ? 0 : 1 }) > 0;
        }
        public static string GetString(OriginalDatabaseService db, string key, string defaultValue) => db.Find(key).Value ?? defaultValue;
        // The original uses the current culture overloads, including TryParse's
        // whitespace/sign/overflow behavior. Do not replace with invariant parsing.
        public static bool SetInt(OriginalDatabaseService db, string key, int value, bool synced = false) => SetString(db, key, value.ToString(), synced);
        public static int GetInt(OriginalDatabaseService db, string key, int defaultValue)
        { return int.TryParse(db.Find(key).Value, out int value) ? value : defaultValue; }
        public static bool SetLong(OriginalDatabaseService db, string key, long value, bool synced = false) => SetString(db, key, value.ToString(), synced);
        public static long GetLong(OriginalDatabaseService db, string key, long defaultValue)
        { return long.TryParse(db.Find(key).Value, out long value) ? value : defaultValue; }
        public static bool GetSyncBlocked(OriginalDatabaseService db, string key) => db.Find(key).SyncBlocked == 1;
        public static bool SetSyncBlocked(OriginalDatabaseService db, string key, bool blocked) => db.UpdateSyncBlocked(key, blocked ? 1 : 0) > 0;
        public static bool SetSynced(OriginalDatabaseService db, string key, bool synced)
        {
            if (synced) db.UpdateSyncBlocked(key, 0);
            return db.UpdateSynced(key, synced ? 1 : 0) > 0;
        }
    }

    // One instance belongs to the user context, as the original static cache
    // does. The context owner must ResetConnection when switching accounts.
    public sealed class OriginalUserKeyValue
    {
        private readonly Func<OriginalDatabaseService> resolve;
        private OriginalDatabaseService cached;
        private OriginalDatabaseService Database => cached ?? (cached = resolve());
        public OriginalUserKeyValue(Func<OriginalDatabaseService> resolve) { this.resolve = resolve; }
        public void ResetConnection() { cached = null; }
        public void Save() => Database.Save();
        public bool HasKey(string key) => OriginalKeyValueTable.HasKey(Database, key);
        public bool DeleteKey(string key) => OriginalKeyValueTable.DeleteKey(Database, key);
        public bool SetSynced(string key, bool synced) => OriginalKeyValueTable.SetSynced(Database, key, synced);
        public bool SetString(string key, string value) => OriginalKeyValueTable.SetString(Database, key, value);
        public string GetString(string key, string defaultValue) => OriginalKeyValueTable.GetString(Database, key, defaultValue);
        public bool SetInt(string key, int value) => OriginalKeyValueTable.SetInt(Database, key, value);
        public int GetInt(string key, int defaultValue = 0) => OriginalKeyValueTable.GetInt(Database, key, defaultValue);
        public int IncrementInt(string key, int amount = 1, int defaultValue = 0) => SetAndReturn(key, unchecked(GetInt(key, defaultValue) + amount));
        private int SetAndReturn(string key, int value) { SetInt(key, value); return value; }
        public bool SetLong(string key, long value) => OriginalKeyValueTable.SetLong(Database, key, value);
        public long GetLong(string key, long defaultValue = 0) => OriginalKeyValueTable.GetLong(Database, key, defaultValue);
    }
}
