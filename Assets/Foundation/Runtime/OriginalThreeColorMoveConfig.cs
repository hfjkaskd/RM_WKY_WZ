using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalThreeColorMoveContext { bool IgnoreMoveConfig { get; } }
    public interface IOriginalThreeColorConfigStorage
    {
        string GetString(string key);
        int GetInt(string key);
        void SetString(string key, string value);
        void SetInt(string key, int value);
        void DeleteKey(string key);
    }
    [Serializable]
    public sealed class OriginalThreeColorMoveConfigData
    {
        public string moves;
        public string[] b, c, d, e;
    }
    public readonly struct OriginalThreeColorLevelEntry
    {
        public readonly int Version, MoveAdjustment;
        public OriginalThreeColorLevelEntry(int version, int moveAdjustment)
        { Version = version; MoveAdjustment = moveAdjustment; }
    }
    // One cache owner per application, matching the original static helper.
    // AppKeyValue transport and JsonUtility parsing are required services.
    public sealed class OriginalThreeColorMoveConfig : IOriginalThreeColorVersionEntries
    {
        public const string ConfigKey = "ThreeColorMoveConfig";
        public const string VersionKey = "ThreeColorMoveConfigVersion";
        private readonly Func<IOriginalThreeColorMoveContext> currentManager;
        private readonly Func<IOriginalThreeColorAbValues> currentAb;
        private readonly IOriginalThreeColorConfigStorage storage;
        private readonly Func<string, OriginalThreeColorMoveConfigData> fromJson;
        private Dictionary<int, OriginalThreeColorLevelEntry> b, c, d, e;
        private int minMoveCount;

        public OriginalThreeColorMoveConfig(Func<IOriginalThreeColorMoveContext> currentManager,
            Func<IOriginalThreeColorAbValues> currentAb, IOriginalThreeColorConfigStorage storage,
            Func<string, OriginalThreeColorMoveConfigData> fromJson)
        {
            this.currentManager = currentManager ?? throw new ArgumentNullException(nameof(currentManager));
            this.currentAb = currentAb ?? throw new ArgumentNullException(nameof(currentAb));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.fromJson = fromJson ?? throw new ArgumentNullException(nameof(fromJson));
        }
        public bool ShouldIgnoreConfig() // 0x283fd9c: manager byte 0x23.
        {
            var manager = currentManager(); if (manager == null) throw new NullReferenceException();
            return manager.IgnoreMoveConfig;
        }
        public static bool IsConfigInvalid(OriginalThreeColorMoveConfigData data) // 0x283f62c.
        { return data == null || (data.b == null && data.c == null && data.d == null && data.e == null); }
        public bool TryParseConfig(string json, out OriginalThreeColorMoveConfigData data) // 0x283f25c.
        { data = null; data = fromJson(json); return !IsConfigInvalid(data); }
        public static bool TryParseLevelVersionToken(string token, out int level, out int version) // 0x283fc04.
        {
            level = 0; version = 0;
            if (string.IsNullOrEmpty(token)) return false;
            int index = token.IndexOf('v');
            if (index <= 0 || index >= token.Length - 1) return false;
            return int.TryParse(token.Substring(0, index), out level)
                && int.TryParse(token.Substring(index + 1), out version);
        }
        public static Dictionary<int, OriginalThreeColorLevelEntry> BuildGroupDictionary(string[] data) // 0x283f668.
        {
            if (data == null || (data.Length & 1) != 0) return new Dictionary<int, OriginalThreeColorLevelEntry>();
            var result = new Dictionary<int, OriginalThreeColorLevelEntry>(data.Length / 2);
            for (int i = 0; i < data.Length; i += 2)
            {
                if (!TryParseLevelVersionToken(data[i], out int level, out int version)
                    || !int.TryParse(data[i + 1], out int adjustment)) continue;
                result[level] = new OriginalThreeColorLevelEntry(version, adjustment);
            }
            return result;
        }
        public static int ParseMinMoveCount(string moves) // 0x283f968.
        { return !string.IsNullOrEmpty(moves) && int.TryParse(moves, out int count) && count > 0 ? count : 0; }
        public void BuildDictionaries(OriginalThreeColorMoveConfigData data) // 0x283f518: preserve write order.
        {
            b = BuildGroupDictionary(data == null ? null : data.b);
            c = BuildGroupDictionary(data == null ? null : data.c);
            d = BuildGroupDictionary(data == null ? null : data.d);
            e = BuildGroupDictionary(data == null ? null : data.e);
            minMoveCount = ParseMinMoveCount(data == null ? null : data.moves);
        }
        public void EnsureCacheLoaded() // 0x283efbc.
        {
            if (b != null && c != null && d != null && e != null) return;
            OriginalThreeColorMoveConfigData data = null;
            string json = storage.GetString(ConfigKey);
            if (!string.IsNullOrEmpty(json) && !TryParseConfig(json, out data))
            { InvalidatePersistedConfig(); data = null; }
            BuildDictionaries(data);
        }
        public void InvalidatePersistedConfig() // 0x283fac4: deletion does not reset in-memory caches.
        { storage.DeleteKey(ConfigKey); storage.DeleteKey(VersionKey); }
        public int ConfigVersion // 0x283ef70.
        { get { EnsureCacheLoaded(); return storage.GetInt(VersionKey); } }
        public void TryUpdateConfig(int version, string json) // 0x283f07c.
        {
            if (string.IsNullOrEmpty(json) || ConfigVersion == version || !TryParseConfig(json, out var data)) return;
            BuildDictionaries(data);
            storage.SetString(ConfigKey, json);
            storage.SetInt(VersionKey, version);
        }
        public Dictionary<int, OriginalThreeColorLevelEntry> GetCachedData(int group) // 0x283fe4c: ensure even for unknown group.
        {
            EnsureCacheLoaded();
            switch (group)
            {
                case 2: return b;
                case 3: return c;
                case 4: return d;
                case 5: return e;
                default: return null;
            }
        }
        public bool TryGetEntry(int level, out OriginalThreeColorLevelEntry entry) // 0x283fcdc.
        {
            entry = default;
            if (ShouldIgnoreConfig()) return false;
            var ab = currentAb(); if (ab == null) throw new NullReferenceException();
            var cache = GetCachedData(ab.GetThreeColorLevelsAbGroup());
            return cache != null && cache.TryGetValue(level, out entry);
        }
        public bool TryGetVersion(int level, out int version) // 0x283fe14.
        {
            bool found = TryGetEntry(level, out var entry);
            version = found ? entry.Version : 0;
            return found;
        }
        public int GetMoveAdjustment(int level) // 0x283daa0.
        { return TryGetEntry(level, out var entry) ? entry.MoveAdjustment : 0; }
        public int GetMinMoveCount(int defaultCount) // 0x283dac4: cache loads BEFORE ignore flag is read.
        {
            EnsureCacheLoaded();
            return !ShouldIgnoreConfig() && minMoveCount > 0 ? minMoveCount : defaultCount;
        }
    }
}
