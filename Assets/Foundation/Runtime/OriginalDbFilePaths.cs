using System;
using System.IO;

namespace Whitebox.Foundation
{
    public interface IOriginalStorageDirectoryContext
    {
        string ReadAndroidApplicationDataPath();
        string PersistentDataPath { get; }
    }
    // FileHelper.cctor0x2d7a8c0 and DbFileHelper0x25d03b4/0x25d1ad8.
    // One instance belongs to application startup; the resulting root is cached.
    // The exception fallback is the original cross-platform path, not Editor-only.
    public sealed class OriginalDbFilePaths
    {
        public string ApplicationDataPath { get; }
        public OriginalDbFilePaths(IOriginalStorageDirectoryContext context)
        {
            try { ApplicationDataPath=context.ReadAndroidApplicationDataPath(); }
            catch(Exception) { ApplicationDataPath=Path.GetFullPath(context.PersistentDataPath); }
        }
        public string GetAppDbPath()=>Path.Combine(ApplicationDataPath,"App");
        public string GetUserDbPath(long userId)=>Path.Combine(ApplicationDataPath,"U_"+userId.ToString());
    }
}
