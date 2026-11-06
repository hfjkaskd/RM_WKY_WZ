namespace Whitebox.Foundation
{
    public sealed class OriginalWindowsSqliteConnection : OriginalNativeSqliteConnection
    {
        public OriginalWindowsSqliteConnection(string path) : base(path, new OriginalWindowsSqliteApi()) { }
    }
}
