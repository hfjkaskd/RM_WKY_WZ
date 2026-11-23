using System;

namespace Whitebox.Foundation
{
    // The game assembly owns the language table. Foundation only consumes this
    // typed adapter, so scenes and players share the same localization path.
    public static class CoreLocalization
    {
        private static Func<string, string> resolver;
        public static event Action Changed;

        public static void Configure(Func<string, string> resolve)
        {
            resolver = resolve ?? throw new ArgumentNullException(nameof(resolve));
            NotifyLanguageChanged();
        }

        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;
            string value = resolver?.Invoke(key);
            return string.IsNullOrEmpty(value) || value == key ? fallback ?? string.Empty : value;
        }

        public static void NotifyLanguageChanged() => Changed?.Invoke();
    }
}
