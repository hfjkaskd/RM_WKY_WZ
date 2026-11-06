using System;
using UnityEngine;
using static Whitebox.Foundation.OriginalNativeSqliteConnection;

namespace Whitebox.Foundation
{
    // Use Android's public SQLite API through Unity JNI with the same SQL and save path.
    public sealed class OriginalAndroidSqliteConnection : IOriginalKeyValueConnection
    {
        private AndroidJavaObject database;
        private bool transaction;

        public OriginalAndroidSqliteConnection(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Database path must not be empty.", nameof(path));
            using (var sqlite = new AndroidJavaClass("android.database.sqlite.SQLiteDatabase"))
                database = sqlite.CallStatic<AndroidJavaObject>("openOrCreateDatabase", path, null);
            try { database.Call("execSQL", CreateTableSql); }
            catch { Dispose(); throw; }
        }

        private AndroidJavaObject Database => database ?? throw new ObjectDisposedException(nameof(OriginalAndroidSqliteConnection));

        public OriginalKeyValue Find(string key)
        {
            var connection = Database;
            if (key == null) return default;
            using (var cursor = connection.Call<AndroidJavaObject>("rawQuery", FindSql, new[] { TruncateAtNull(key) }))
            {
                try
                {
                    if (!cursor.Call<bool>("moveToFirst")) return default;
                    return new OriginalKeyValue
                    {
                        Key = cursor.Call<string>("getString", 0), Value = cursor.Call<string>("getString", 1),
                        Synced = cursor.Call<int>("getInt", 2), SyncBlocked = cursor.Call<int>("getInt", 3)
                    };
                }
                finally { cursor.Call("close"); }
            }
        }

        public int InsertOrReplace(OriginalKeyValue row)
        {
            using (var statement = Database.Call<AndroidJavaObject>("compileStatement", UpsertSql))
            {
                try
                {
                    Bind(statement, 1, row.Key);
                    Bind(statement, 2, row.Value);
                    statement.Call("bindLong", 3, (long)row.Synced);
                    statement.Call("bindLong", 4, (long)row.SyncBlocked);
                    return statement.Call<long>("executeInsert") == -1 ? 0 : 1;
                }
                finally { statement.Call("close"); }
            }
        }

        public int Delete(string key) => ExecuteKey(DeleteSql, key, 0, false);
        public int UpdateSynced(string key, int value) => ExecuteKey(SyncedSql, key, value, true);
        public int UpdateSyncBlocked(string key, int value) => ExecuteKey(SyncBlockedSql, key, value, true);

        private int ExecuteKey(string sql, string key, int value, bool update)
        {
            using (var statement = Database.Call<AndroidJavaObject>("compileStatement", sql))
            {
                try
                {
                    if (update) statement.Call("bindLong", 1, (long)value);
                    Bind(statement, update ? 2 : 1, key);
                    return statement.Call<int>("executeUpdateDelete");
                }
                finally { statement.Call("close"); }
            }
        }

        private static void Bind(AndroidJavaObject statement, int index, string value)
        {
            if (value == null) statement.Call("bindNull", index);
            else statement.Call("bindString", index, TruncateAtNull(value));
        }

        private static string TruncateAtNull(string value)
        {
            int end = value.IndexOf('\0');
            return end < 0 ? value : value.Substring(0, end);
        }

        public void BeginTransaction()
        {
            if (transaction) return;
            Database.Call("beginTransactionNonExclusive");
            transaction = true;
        }

        public void Commit()
        {
            if (!transaction) return;
            transaction = false;
            try { Database.Call("setTransactionSuccessful"); }
            finally { Database.Call("endTransaction"); }
        }

        public void Dispose()
        {
            var connection = database;
            if (connection == null) return;
            database = null;
            try
            {
                if (transaction) connection.Call("endTransaction");
            }
            finally
            {
                transaction = false;
                try { connection.Call("close"); }
                finally { connection.Dispose(); }
            }
        }
    }
}
