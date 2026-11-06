using System;
using System.Collections.Generic;

namespace Whitebox.Foundation
{
    public interface IOriginalSlbAbValues
    {
        void SetAbValues();
        int AbGroup { get; }
    }
    [Serializable]
    public sealed class OriginalSlbMoveConfigData
    {
        public string[] c, d, f, g, h;
    }
    public readonly struct OriginalSlbLevelEntry
    {
        public readonly int Version, MoveAdjustment;
        public OriginalSlbLevelEntry(int version, int moveAdjustment)
        { Version = version; MoveAdjustment = moveAdjustment; }
    }
    // SLB lookup/cache portion. Storage/JSON and SuperBooster AB initialization
    // remain required services; download/persistence is not replaced here.
    public sealed class OriginalSlbMoveConfig
    {
        private readonly Func<IOriginalSlbAbValues> currentBooster;
        private readonly Func<OriginalSlbMoveConfigData> readStoredConfig;
        private Dictionary<int, OriginalSlbLevelEntry> c, d, f, g, h;
        public OriginalSlbMoveConfig(Func<IOriginalSlbAbValues> currentBooster,
            Func<OriginalSlbMoveConfigData> readStoredConfig)
        {
            this.currentBooster = currentBooster ?? throw new ArgumentNullException(nameof(currentBooster));
            this.readStoredConfig = readStoredConfig ?? throw new ArgumentNullException(nameof(readStoredConfig));
        }
        public static bool TryParseLevelVersionToken(string token, out int level, out int version) // 0x221e2d0.
        {
            level = 0; version = 0;
            if (string.IsNullOrEmpty(token)) return false;
            int index = token.IndexOf('v');
            if (index <= 0 || index >= token.Length - 1) return false;
            return int.TryParse(token.Substring(0, index), out level)
                && int.TryParse(token.Substring(index + 1), out version);
        }
        public static Dictionary<int, OriginalSlbLevelEntry> BuildGroupDictionary(string[] data) // 0x221dfac.
        {
            if (data == null || (data.Length & 1) != 0) return new Dictionary<int, OriginalSlbLevelEntry>();
            var result = new Dictionary<int, OriginalSlbLevelEntry>(data.Length / 2);
            for (int i = 0; i < data.Length; i += 2)
            {
                if (!TryParseLevelVersionToken(data[i], out int level, out int version)
                    || !int.TryParse(data[i + 1], out int adjustment)) continue;
                result[level] = new OriginalSlbLevelEntry(version, adjustment);
            }
            return result;
        }
        public void EnsureCacheLoaded() // 0x221e3a8, AppKeyValue/JSON supplied at the storage boundary.
        {
            if (c != null && d != null && f != null && g != null && h != null) return;
            var data = readStoredConfig();
            c = BuildGroupDictionary(data == null ? null : data.c);
            d = BuildGroupDictionary(data == null ? null : data.d);
            f = BuildGroupDictionary(data == null ? null : data.f);
            g = BuildGroupDictionary(data == null ? null : data.g);
            h = BuildGroupDictionary(data == null ? null : data.h);
        }
        public bool TryGetEntry(int level, out OriginalSlbLevelEntry entry) // 0x221e66c.
        {
            var booster = currentBooster(); if (booster == null) throw new NullReferenceException();
            booster.SetAbValues();
            int group = booster.AbGroup;
            bool inRange;
            switch (group)
            {
                case 3: inRange = unchecked((uint)(level - 160)) <= 70; break;
                case 4: inRange = unchecked((uint)(level - 75)) <= 155; break;
                case 6:
                case 7: inRange = unchecked((uint)(level - 111)) <= 119; break;
                case 8: inRange = unchecked((uint)(level - 211)) <= 19; break;
                default: inRange = false; break;
            }
            if (!inRange) { entry = default; return false; }
            EnsureCacheLoaded();
            var values = SelectCache(group);
            if (values == null) throw new NullReferenceException();
            return values.TryGetValue(level, out entry);
        }
        public int GetMoveAdjustment(int level) // 0x221e5f8.
        { return TryGetEntry(level, out var entry) ? entry.MoveAdjustment : 0; }
        public bool TryGetLevelNameOverride(int level, out int version, out string extension) // 0x221e868.
        {
            var booster = currentBooster(); if (booster == null) throw new NullReferenceException();
            booster.SetAbValues();
            int group = booster.AbGroup;
            switch (group)
            {
                case 3:
                    if (unchecked((uint)(level - 160)) > 70) break;
                    extension = "O"; version = GetVersionOrDefault(level, GetCachedData(group)); return true;
                case 4:
                    if (unchecked((uint)(level - 75)) > 155) break;
                    extension = "P"; version = GetVersionOrDefault(level, GetCachedData(group)); return true;
                case 6:
                    if (unchecked((uint)(level - 111)) > 119) break;
                    extension = "R"; version = GetVersionOrDefault(level, GetCachedData(group)); return true;
                case 7:
                    if (unchecked((uint)(level - 111)) > 119) break;
                    extension = "S"; version = GetVersionOrDefault(level, GetCachedData(group)); return true;
                case 8:
                    if (unchecked((uint)(level - 211)) > 19) break;
                    extension = "T"; version = GetVersionOrDefault(level, GetCachedData(group)); return true;
            }
            version = 0; extension = null; return false;
        }
        public static int GetVersionOrDefault(int level, Dictionary<int, OriginalSlbLevelEntry> values) // 0x221ec04.
        { return values != null && values.TryGetValue(level, out var entry) ? entry.Version : 1; }
        public Dictionary<int, OriginalSlbLevelEntry> GetCachedData(int group) // 0x221eaf8: ensure even for unknown group.
        { EnsureCacheLoaded(); return SelectCache(group); }
        private Dictionary<int, OriginalSlbLevelEntry> SelectCache(int group)
        {
            switch (group)
            {
                case 3: return c;
                case 4: return d;
                case 6: return f;
                case 7: return g;
                case 8: return h;
                default: return null;
            }
        }
    }
}
