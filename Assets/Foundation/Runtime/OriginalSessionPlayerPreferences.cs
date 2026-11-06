using UnityEngine;

namespace Whitebox.Foundation
{
    // The original story try counter uses Unity PlayerPrefs on every platform.
    public sealed class OriginalSessionPlayerPreferences : IOriginalSessionIntPreferences
    {
        public int GetInt(string key) => PlayerPrefs.GetInt(key);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    }
}
