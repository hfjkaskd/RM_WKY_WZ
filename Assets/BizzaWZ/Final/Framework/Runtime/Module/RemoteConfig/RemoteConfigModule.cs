#if BIZZA_REAL_WITHDRAW
using Cysharp.Threading.Tasks;
using UnityEngine;

public class RemoteConfigModule : BaseGameModule<RemoteConfigModule>
{
    public static string curVersion => Application.version;
    public static string reviewVersion;
    public static int dataValue = 1;
    public static bool reviewMode = true;
    public static bool isTestMode = false;

    public UniTask LoadRemoteConfig()
    {
        return UniTask.CompletedTask;
    }
}
#endif
