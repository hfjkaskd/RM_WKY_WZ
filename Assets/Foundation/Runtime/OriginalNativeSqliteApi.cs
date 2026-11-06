using System;
using System.Runtime.InteropServices;

namespace Whitebox.Foundation
{
    public interface IOriginalNativeSqliteApi
    {
        int Open(byte[] path, out IntPtr db, int flags, IntPtr vfs);
        int Close(IntPtr db);
        int PrepareNative(IntPtr db, byte[] sql, int bytes, out IntPtr statement, IntPtr tail);
        int Step(IntPtr statement);
        int FinalizeStatement(IntPtr statement);
        int BindText(IntPtr statement, int index, string text, int length, IntPtr destructor);
        int BindNull(IntPtr statement, int index);
        int BindInt(IntPtr statement, int index, int value);
        IntPtr ColumnText(IntPtr statement, int column);
        int ColumnInt(IntPtr statement, int column);
        int Changes(IntPtr db);
        IntPtr Error(IntPtr db);
        int BusyTimeout(IntPtr db, int milliseconds);
    }

    internal sealed class OriginalWindowsSqliteApi : IOriginalNativeSqliteApi
    {
        private const string Library = "winsqlite3";
        public int Open(byte[] path, out IntPtr db, int flags, IntPtr vfs) => NativeOpen(path, out db, flags, vfs);
        [DllImport(Library, EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeOpen(byte[] path, out IntPtr db, int flags, IntPtr vfs);
        public int Close(IntPtr db) => NativeClose(db);
        [DllImport(Library, EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeClose(IntPtr db);
        public int PrepareNative(IntPtr db, byte[] sql, int bytes, out IntPtr statement, IntPtr tail) => NativePrepareNative(db, sql, bytes, out statement, tail);
        [DllImport(Library, EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)] private static extern int NativePrepareNative(IntPtr db, byte[] sql, int bytes, out IntPtr statement, IntPtr tail);
        public int Step(IntPtr statement) => NativeStep(statement);
        [DllImport(Library, EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeStep(IntPtr statement);
        public int FinalizeStatement(IntPtr statement) => NativeFinalizeStatement(statement);
        [DllImport(Library, EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeFinalizeStatement(IntPtr statement);
        public int BindText(IntPtr statement, int index, string text, int length, IntPtr destructor) => NativeBindText(statement, index, text, length, destructor);
        [DllImport(Library, EntryPoint = "sqlite3_bind_text16", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)] private static extern int NativeBindText(IntPtr statement, int index, string text, int length, IntPtr destructor);
        public int BindNull(IntPtr statement, int index) => NativeBindNull(statement, index);
        [DllImport(Library, EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeBindNull(IntPtr statement, int index);
        public int BindInt(IntPtr statement, int index, int value) => NativeBindInt(statement, index, value);
        [DllImport(Library, EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeBindInt(IntPtr statement, int index, int value);
        public IntPtr ColumnText(IntPtr statement, int column) => NativeColumnText(statement, column);
        [DllImport(Library, EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr NativeColumnText(IntPtr statement, int column);
        public int ColumnInt(IntPtr statement, int column) => NativeColumnInt(statement, column);
        [DllImport(Library, EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeColumnInt(IntPtr statement, int column);
        public int Changes(IntPtr db) => NativeChanges(db);
        [DllImport(Library, EntryPoint = "sqlite3_changes", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeChanges(IntPtr db);
        public IntPtr Error(IntPtr db) => NativeError(db);
        [DllImport(Library, EntryPoint = "sqlite3_errmsg", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr NativeError(IntPtr db);
        public int BusyTimeout(IntPtr db, int milliseconds) => NativeBusyTimeout(db, milliseconds);
        [DllImport(Library, EntryPoint = "sqlite3_busy_timeout", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeBusyTimeout(IntPtr db, int milliseconds);
    }
}
