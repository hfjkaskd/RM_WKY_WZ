using System;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;

internal static class ActiveLevelLoadHarness
{
    private sealed class Context : IOriginalSlbAbValues, IOriginalThreeColorAbValues,
        IOriginalThreeColorNameContext, IOriginalThreeColorUserProgress, IOriginalThreeColorVersionEntries
    {
        internal int Flags, SlbGroup, ThreeGroup, GroupDelta, UserType, FoundVersion, OverrideVersion;
        internal bool Forced;
        internal OriginalActiveLevelData Active;
        internal OriginalSlbMoveConfigData Data;
        internal readonly List<string> Trace = new List<string>();
        public int AbGroup => SlbGroup;
        public bool IsCurrentLevelThreeColor => Forced;
        public int GameType => UserType;
        public void SetAbValues()
        { Trace.Add("abvalues"); if ((Flags & 64) != 0) throw new InvalidOperationException(); }
        public int GetThreeColorLevelsAbGroup()
        {
            Trace.Add("ab," + Active.Number + "," + Active.MoveCount + "," + (Active.IsAdmin ? 1 : 0) + "," + Active.State);
            if ((Flags & 1024) != 0) throw new InvalidOperationException();
            int value = ThreeGroup; ThreeGroup = unchecked(ThreeGroup + GroupDelta); return value;
        }
        public bool TryGetVersion(int level, out int version)
        {
            Trace.Add("version," + level);
            if ((Flags & 2048) != 0) throw new InvalidOperationException();
            version = OverrideVersion; return FoundVersion != 0;
        }
        internal OriginalSlbMoveConfigData Read()
        { Trace.Add("storage"); if ((Flags & 128) != 0) throw new InvalidOperationException(); return Data; }
    }
    private static string Decode(string s) => s == "~" ? null : Encoding.UTF8.GetString(Convert.FromBase64String(s));
    private static string Encode(string s) => s == null ? "~" : Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
    private static string[] Array(string s)
    {
        if (s == "~") return null;
        if (s == "") return new string[0];
        var a = s.Split(','); for (int i = 0; i < a.Length; i++) a[i] = Decode(a[i]); return a;
    }
    private static string Bool(bool v) => v ? "1" : "0";
    private static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var sections = line.Split('#'); var h = sections[0].Split(',');
            var c = new Context { Flags = int.Parse(h[0]), SlbGroup = int.Parse(h[1]), ThreeGroup = int.Parse(h[2]), GroupDelta = int.Parse(h[3]), Forced = h[4] == "1", UserType = int.Parse(h[5]), FoundVersion = int.Parse(h[6]), OverrideVersion = int.Parse(h[7]) };
            var arrays = sections[1].Split(';');
            if (arrays.Length == 5) c.Data = new OriginalSlbMoveConfigData { c = Array(arrays[0]), d = Array(arrays[1]), f = Array(arrays[2]), g = Array(arrays[3]), h = Array(arrays[4]) };
            var slb = new OriginalSlbMoveConfig(() => (c.Flags & 32) != 0 ? c : null, c.Read);
            var names = new OriginalThreeColorLevelNames(() => (c.Flags & 14) == 14 ? c.Active : null,
                () => (c.Flags & 18) == 18 ? c : null, () => (c.Flags & 256) != 0 ? c : null,
                () => (c.Flags & 514) == 514 ? c : null, c);
            var active = new OriginalActiveLevelData(slb, names); c.Active = active;
            var output = new List<string>();
            foreach (string step in sections[2].Split(';'))
            {
                c.Trace.Clear(); var x = step.Split(','); string result = "-"; int version = -777; string extension = "seed";
                int a = x.Length > 1 && x[0] != "n" && x[0] != "t" ? int.Parse(x[1]) : 0;
                try
                {
                    switch (x[0])
                    {
                        case "l": active.Load(a, Decode(x[2]), int.Parse(x[3]), x[4] == "1", x[5] == "1"); break;
                        case "n": result = Encode(active.GetFixedLevelName(Decode(x[1]))); break;
                        case "t": result = Encode(OriginalActiveLevelData.StripVersionSegment(Decode(x[1]))); break;
                        case "v": result = Bool(slb.TryGetLevelNameOverride(a, out version, out extension)); break;
                        case "s": result = Bool(names.ShouldReportThreeColorExtension(a, int.Parse(x[2]))); break;
                        case "x": result = Encode(names.GetLevelExtension()); break;
                        case "u": result = Bool(names.IsEnabled()); break;
                        case "h": result = Bool(names.IsHardLevelExcludedForGroup()); break;
                        case "d": result = names.GetLevelDifficulty(a).ToString(); break;
                        case "e": result = Bool(names.IsLevelAvailableForExtreme(a)); break;
                        case "j": result = Bool(OriginalThreeColorLevelNames.IsItemTutorialOrSpecialLevel(a)); break;
                        case "z": active.Reset(); break;
                        case "f":
                            active.IsAdmin = (a & 1) != 0; active.IsOnline = (a & 2) != 0;
                            active.IsReactivation = (a & 4) != 0; active.IsTableGame = (a & 8) != 0; break;
                        case "b": active.StoryLevel = a; active.LeagueIndex = int.Parse(x[2]); break;
                        case "g": c.ThreeGroup = a; c.SlbGroup = int.Parse(x[2]); break;
                        case "m": active.UpdateMoveCount(a); break;
                    }
                }
                catch (Exception) { result = "error"; }
                output.Add(result + ":" + version + ":" + Encode(extension) + ":" + active.Number + ":" + active.MoveCount + ":" + active.OriginalMoveCount
                    + ":" + Encode(active.Name) + ":" + Bool(active.IsAdmin) + ":" + Bool(active.IsOnline) + ":" + active.State + ":" + c.ThreeGroup + ":" + string.Join(";", c.Trace.ToArray()));
            }
            Console.WriteLine(string.Join("|", output.ToArray()));
        }
    }
}
