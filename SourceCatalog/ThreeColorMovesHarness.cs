using System;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;

static class ThreeColorMovesHarness
{
    sealed class Fixture : IOriginalThreeColorMoveContext, IOriginalThreeColorAbValues, IOriginalThreeColorConfigStorage
    {
        public int Flags, Group, Ignore, League, Offset, ConfiguredOffset, LeagueMinimum, Version;
        public string Stored;
        public OriginalThreeColorMoveConfigData[] Profiles;
        public readonly List<string> Trace = new List<string>();
        public bool IgnoreMoveConfig => Ignore != 0;
        public int GetThreeColorLevelsAbGroup()
        { Trace.Add("ab"); if ((Flags & 32) != 0) throw new Exception(); return Group; }
        public IOriginalThreeColorMoveContext Manager()
        { Trace.Add("manager"); return (Flags & 1) != 0 ? this : null; }
        public IOriginalThreeColorAbValues Ab() => (Flags & 6) == 6 ? this : null;
        public string GetString(string key)
        { Trace.Add("read"); if ((Flags & 64) != 0) throw new Exception(); return Stored; }
        public int GetInt(string key)
        { Trace.Add("version"); if ((Flags & 64) != 0) throw new Exception(); return Version; }
        public void SetString(string key, string value)
        { Trace.Add("write," + value); if ((Flags & 2048) != 0) throw new Exception(); Stored = value; }
        public void SetInt(string key, int value)
        { Trace.Add("writeversion," + value); if ((Flags & 4096) != 0) throw new Exception(); Version = value; }
        public void DeleteKey(string key)
        {
            bool config = key == OriginalThreeColorMoveConfig.ConfigKey;
            Trace.Add(config ? "delete" : "deleteversion");
            if ((Flags & (config ? 8192 : 16384)) != 0) throw new Exception();
            if (config) Stored = null; else Version = 0;
        }
        public OriginalThreeColorMoveConfigData Parse(string value)
        { Trace.Add("json," + value); if ((Flags & 128) != 0) throw new Exception(); return Profiles[int.Parse(value)]; }
        public int LeagueOffset()
        { Trace.Add("offset"); if ((Flags & 256) != 0) throw new Exception(); return Offset; }
        public int Configured(int level)
        { Trace.Add("configured," + level); if ((Flags & 512) != 0) throw new Exception(); return ConfiguredOffset; }
        public int Minimum(int count)
        { Trace.Add("minimum," + count); if ((Flags & 1024) != 0) throw new Exception(); return LeagueMinimum; }
    }
    static string Decode(string value) => value == "~" ? null : Encoding.UTF8.GetString(Convert.FromBase64String(value));
    static string[] Array(string value)
    { if (value == "~") return null; if (value == "") return new string[0]; return System.Array.ConvertAll(value.Split(','), Decode); }
    static OriginalThreeColorMoveConfigData Profile(string value)
    {
        if (value == "~") return null;
        var fields = value.Split(';');
        return new OriginalThreeColorMoveConfigData { moves = Decode(fields[0]), b = Array(fields[1]), c = Array(fields[2]), d = Array(fields[3]), e = Array(fields[4]) };
    }
    static string Dict(Dictionary<int, OriginalThreeColorLevelEntry> values)
    {
        if (values == null) return "null";
        var keys = new List<int>(values.Keys); keys.Sort(); var rows = new List<string>();
        foreach (int k in keys) rows.Add(k + "," + values[k].Version + "," + values[k].MoveAdjustment);
        return string.Join("/", rows);
    }
    public static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#'); var h = System.Array.ConvertAll(parts[0].Split(','), int.Parse);
            var f = new Fixture { Flags = h[0], Group = h[1], Ignore = h[2], League = h[3], Offset = h[4], ConfiguredOffset = h[5], LeagueMinimum = h[6], Stored = h[7] < 0 ? null : h[7].ToString(), Version = h[8], Profiles = System.Array.ConvertAll(parts[1].Split('!'), Profile) };
            var config = new OriginalThreeColorMoveConfig(f.Manager, f.Ab, f, f.Parse);
            var active = new OriginalActiveLevelData(new OriginalSlbMoveConfig(() => null, () => null), new UnusedNames()); active.LeagueIndex = f.League;
            var moves = new OriginalThreeColorMoves(config, f.Ab, () => (f.Flags & 26) == 26 ? active : null, f.LeagueOffset, f.Configured, f.Minimum);
            var output = new List<string>();
            foreach (string text in parts[2].Split(';'))
            {
                var a = text.Split(','); string result = "-"; f.Trace.Clear();
                int v = 0, x = 0; if (a.Length > 1 && a[0] != "t" && a[0] != "n") x = int.Parse(a[1]);
                try
                {
                    switch (a[0])
                    {
                        case "e": bool found = config.TryGetEntry(x, out var entry); result = (found ? "1" : "0") + "," + entry.Version + "," + entry.MoveAdjustment; break;
                        case "v": result = (config.TryGetVersion(x, out v) ? "1" : "0") + "," + v; break;
                        case "a": result = config.GetMoveAdjustment(x).ToString(); break;
                        case "m": result = config.GetMinMoveCount(x).ToString(); break;
                        case "k": result = Dict(config.GetCachedData(x)); break;
                        case "c": result = config.ConfigVersion.ToString(); break;
                        case "u": config.TryUpdateConfig(x, int.Parse(a[2]) < 0 ? null : a[2]); break;
                        case "d": config.InvalidatePersistedConfig(); break;
                        case "b": f.Stored = x < 0 ? null : x.ToString(); break;
                        case "g": f.Group = x; break;
                        case "i": f.Ignore = x; break;
                        case "q": f.Flags = x; break;
                        case "r": result = moves.GetReducedMoveCount(x, a[2] == "1", a[3] == "1", a[4] == "1").ToString(); break;
                        case "p": result = moves.ApplyMoveConfigAdjustment(x, int.Parse(a[2])).ToString(); break;
                        case "f": result = moves.GetAdjustedMoveCount(x, int.Parse(a[2]), a[3] == "1", a[4] == "1", a[5] == "1").ToString(); break;
                        case "t": bool parsed = OriginalThreeColorMoveConfig.TryParseLevelVersionToken(Decode(a[1]), out int level, out int version); result = (parsed ? "1" : "0") + "," + level + "," + version; break;
                        case "n": result = OriginalThreeColorMoveConfig.ParseMinMoveCount(Decode(a[1])).ToString(); break;
                        case "s": result = OriginalThreeColorMoves.ScaleMoveCount(x, int.Parse(a[2])).ToString(); break;
                        case "j": result = OriginalThreeColorMoveConfig.IsConfigInvalid(f.Profiles[x]) ? "1" : "0"; break;
                        case "l": config.BuildDictionaries(f.Profiles[x]); break;
                        case "h": config.EnsureCacheLoaded(); break;
                        case "x": result = moves.GetGroupMoveMultiplier().ToString(); break;
                    }
                }
                catch { result = "error"; }
                output.Add(result + ":" + (f.Stored ?? "~") + ":" + f.Version + ":" + f.Group + ":" + f.Ignore + ":" + string.Join(",", f.Trace));
            }
            Console.WriteLine(string.Join("|", output));
        }
    }
    sealed class UnusedNames : IOriginalThreeColorLevelNames
    {
        public bool ShouldReportThreeColorExtension(int level, int leagueIndex) { throw new Exception(); }
        public bool TryGetVersionOverride(int level, out int version) { throw new Exception(); }
        public string GetLevelExtension() { throw new Exception(); }
    }
}
