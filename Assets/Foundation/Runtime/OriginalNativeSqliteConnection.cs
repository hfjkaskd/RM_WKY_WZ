using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Whitebox.Foundation
{
    // Shared C# SQL/binding/transaction/exception flow for native OS adapters.
    // Each OS/library still requires runtime verification; no Editor fallback.
    public class OriginalNativeSqliteConnection : IOriginalKeyValueConnection
    {
        internal const string CreateTableSql = "create table if not exists \"KeyValue\" (\"Key\" varchar primary key not null, \"Value\" varchar, \"Synced\" integer, \"SyncBlocked\" integer)";
        internal const string FindSql = "select \"Key\",\"Value\",\"Synced\",\"SyncBlocked\" from \"KeyValue\" where \"Key\" = ? limit 1";
        internal const string UpsertSql = "insert or replace into \"KeyValue\" (\"Key\",\"Value\",\"Synced\",\"SyncBlocked\") values (?,?,?,?)";
        internal const string DeleteSql = "delete from \"KeyValue\" where \"Key\" = ?";
        internal const string SyncedSql = "UPDATE KeyValue SET Synced = ? WHERE Key = ?";
        internal const string SyncBlockedSql = "UPDATE KeyValue SET SyncBlocked = ? WHERE Key = ?";
        private IntPtr database;
        private bool transaction;
        private static readonly IntPtr Transient = new IntPtr(-1);
        private readonly IOriginalNativeSqliteApi api;
        public OriginalNativeSqliteConnection(string path, IOriginalNativeSqliteApi api)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Database path must not be empty.", nameof(path));
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            int result = api.Open(Utf8(path), out database, 6, IntPtr.Zero); // Native open flags: read/write + create.
            if (result != 0)
            {
                string message = ErrorMessage();
                if (database != IntPtr.Zero) api.Close(database);
                database = IntPtr.Zero;
                throw new InvalidOperationException("SQLite open " + result + ": " + message);
            }
            try
            {
                Check(api.BusyTimeout(database, 100)); // Native 0.1 seconds at ELF 0x403fa78.
                // Schema from KeyValue's four properties and primary-key
                // metadata; no migration/deletion of an existing database.
                Execute(CreateTableSql);
            }
            catch { Dispose(); throw; }
        }
        public OriginalKeyValue Find(string key)
        {
            IntPtr statement = Prepare(FindSql);
            try
            {
                Bind(statement, 1, key);
                int result = api.Step(statement);
                if (result == 101) return default;
                Check(result, 100);
                return new OriginalKeyValue { Key = ReadText(statement, 0), Value = ReadText(statement, 1),
                    Synced = api.ColumnInt(statement, 2), SyncBlocked = api.ColumnInt(statement, 3) };
            }
            finally { api.FinalizeStatement(statement); }
        }
        public int InsertOrReplace(OriginalKeyValue row)
        {
            IntPtr statement = Prepare(UpsertSql);
            try
            {
                Bind(statement, 1, row.Key); Bind(statement, 2, row.Value);
                Check(api.BindInt(statement, 3, row.Synced)); Check(api.BindInt(statement, 4, row.SyncBlocked));
                Check(api.Step(statement), 101); return api.Changes(database);
            }
            finally { api.FinalizeStatement(statement); }
        }
        public int Delete(string key) => ExecuteKey(DeleteSql, key, 0, false);
        public int UpdateSynced(string key, int value) => ExecuteKey(SyncedSql, key, value, true);
        public int UpdateSyncBlocked(string key, int value) => ExecuteKey(SyncBlockedSql, key, value, true);
        private int ExecuteKey(string sql, string key, int value, bool update)
        {
            IntPtr statement = Prepare(sql);
            try
            {
                if (update) Check(api.BindInt(statement, 1, value));
                Bind(statement, update ? 2 : 1, key);
                Check(api.Step(statement), 101); return api.Changes(database);
            }
            finally { api.FinalizeStatement(statement); }
        }
        public void BeginTransaction()
        {
            if (transaction) return; // Original SQLiteConnection.CompareExchange.
            transaction = true;
            try { Execute("begin transaction"); }
            catch { transaction = false; throw; }
        }
        public void Commit()
        {
            if (!transaction) return;
            transaction = false; // Original exchanges transaction depth before SQL.
            Execute("commit");
        }
        private void Execute(string sql)
        {
            IntPtr statement = Prepare(sql);
            try { Check(api.Step(statement), 101); }
            finally { api.FinalizeStatement(statement); }
        }
        private IntPtr Prepare(string sql)
        {
            if (database == IntPtr.Zero) throw new ObjectDisposedException(nameof(OriginalNativeSqliteConnection));
            int result = api.PrepareNative(database, Utf8(sql), -1, out IntPtr statement, IntPtr.Zero);
            if (result != 0)
            {
                if (statement != IntPtr.Zero) api.FinalizeStatement(statement);
                Check(result);
            }
            return statement;
        }
        private void Bind(IntPtr statement, int index, string value)
        {
            if (value == null) { Check(api.BindNull(statement, index)); return; }
            // Original BindParameter0x321d0d8 uses UTF-16 and n=-1, so an
            // embedded NUL terminates text. Preserve that native behavior.
            Check(api.BindText(statement, index, value, -1, Transient));
        }
        private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value + "\0");
        private string ReadText(IntPtr statement, int column)
        {
            IntPtr value = api.ColumnText(statement, column);
            if (value == IntPtr.Zero) return null;
            return Marshal.PtrToStringUni(value); // Original ColumnString0x321e0f0.
        }
        private string ErrorMessage()
        {
            IntPtr value = database == IntPtr.Zero ? IntPtr.Zero : api.Error(database);
            if (value == IntPtr.Zero) return "No database handle";
            int count = 0; while (Marshal.ReadByte(value, count) != 0) count++;
            byte[] bytes = new byte[count]; Marshal.Copy(value, bytes, 0, count);
            return Encoding.UTF8.GetString(bytes);
        }
        private void Check(int result, int expected = 0)
        { if (result != expected) throw new InvalidOperationException("SQLite " + result + ": " + ErrorMessage()); }
        public void Dispose()
        {
            if (database == IntPtr.Zero) return;
            // Closing an uncommitted connection rolls back, as SQLite does.
            Check(api.Close(database)); database = IntPtr.Zero; transaction = false;
        }
    }
}
