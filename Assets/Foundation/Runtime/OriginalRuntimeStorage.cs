using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Required production storage composition point. The application must
    // register the original current user/database lifecycle before using it.
    // Windows uses its native C API and Android uses its public SQLite API.
    public static class OriginalRuntimeStorage
    {
        private static readonly OriginalDbFilePaths paths=new OriginalDbFilePaths(new OriginalUnityStorageDirectory());
        public static OriginalDbFilePaths Paths=>paths;
        public static IOriginalKeyValueConnection OpenApp()=>Open(paths.GetAppDbPath());
        public static IOriginalKeyValueConnection OpenUser(long userId)=>Open(paths.GetUserDbPath(userId));
        public static IOriginalKeyValueConnection Open(string path)
        {
            switch(Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:return new OriginalWindowsSqliteConnection(path);
                case RuntimePlatform.Android:return new OriginalAndroidSqliteConnection(path);
                default:throw new PlatformNotSupportedException("Native SQLite transport is not installed for this OS.");
            }
        }
    }
}
