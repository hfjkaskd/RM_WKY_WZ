using UnityEngine;
using Whitebox.Foundation;

// The gameplay assembly receives localized text without depending on framework internals.
internal static class WkyGameplayLocalization
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        CoreLocalization.Configure(Resolve);
        BizzaEventSystem.Off(EventDefine.Frame.LanguageChange, Refresh);
        BizzaEventSystem.On(EventDefine.Frame.LanguageChange, Refresh);
    }

    private static string Resolve(string key) => LanguageUtils.GetText(key);
    private static void Refresh() => CoreLocalization.NotifyLanguageChanged();
}
