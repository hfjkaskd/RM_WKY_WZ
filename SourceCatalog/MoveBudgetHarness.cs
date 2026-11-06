using System;
using System.Collections.Generic;
using System.Text;
using Whitebox.Foundation;

internal static class MoveBudgetHarness
{
    private sealed class Context : IOriginalSlbAbValues, IOriginalOneMoveRemovalAb, IOriginalOneMoveRemoval
    {
        internal int Flags, Group, ForcedGroup, DukeMode;
        internal OriginalActiveLevelMoveData Level = new OriginalActiveLevelMoveData();
        internal OriginalSlbMoveConfigData Data;
        internal readonly List<string> Trace = new List<string>();
        public int AbGroup => Group;
        public void SetAbValues()
        {
            Trace.Add("abvalues");
            if ((Flags & 64) != 0) throw new InvalidOperationException();
            if (ForcedGroup >= 0) Group = ForcedGroup;
        }
        public bool IsOneMoveRemoveAbEnabled() { Trace.Add("oneab"); return true; }
        public bool ShouldRemoveOneMove() { Trace.Add("duke"); return DukeMode == 1; }
        internal OriginalActiveLevelMoveData CurrentLevel() => (Flags & 14) == 14 ? Level : null;
        internal OriginalSlbMoveConfigData Read()
        { Trace.Add("storage"); if ((Flags & 128) != 0) throw new InvalidOperationException(); return Data; }
    }
    private static string Decode(string s) => s == "~" ? null : Encoding.UTF8.GetString(Convert.FromBase64String(s));
    private static string[] Array(string s)
    {
        if (s == "~") return null;
        if (s == "") return new string[0];
        var a = s.Split(','); for (int i = 0; i < a.Length; i++) a[i] = Decode(a[i]); return a;
    }
    private static string Dictionary(Dictionary<int, OriginalSlbLevelEntry> d)
    {
        if (d == null) return "null";
        var keys = new List<int>(d.Keys); keys.Sort(); var parts = new List<string>();
        foreach (int key in keys) parts.Add(key + "," + d[key].Version + "," + d[key].MoveAdjustment);
        return string.Join("/", parts.ToArray());
    }
    private static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var sections = line.Split('#'); var h = sections[0].Split(',');
            var c = new Context { Flags = int.Parse(h[0]), Group = int.Parse(h[1]), ForcedGroup = int.Parse(h[2]), DukeMode = int.Parse(h[3]) };
            var arrays = sections[1].Split(';');
            if (arrays.Length == 5) c.Data = new OriginalSlbMoveConfigData { c = Array(arrays[0]), d = Array(arrays[1]), f = Array(arrays[2]), g = Array(arrays[3]), h = Array(arrays[4]) };
            var slb = new OriginalSlbMoveConfig(() => (c.Flags & 32) != 0 ? c : null, c.Read);
            var duke = new OriginalSuperDukeMoveRemoval(() => (c.Flags & 18) == 18 ? c : null);
            var adjustment = new OriginalMoveBudgetAdjustment(() =>
            {
                c.Trace.Add("getduke");
                return (c.Flags & 1) == 0 ? null : c.DukeMode == 0 ? (IOriginalOneMoveRemoval)duke : c;
            }, c.CurrentLevel, slb);
            var fresh = new OriginalFreshLevelMoves(c.CurrentLevel);
            OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount = 0;
            var output = new List<string>();
            foreach (string step in sections[2].Split(';'))
            {
                c.Trace.Clear(); var x = step.Split(','); string result = "-"; int a = x.Length > 1 && x[0] != "v" ? int.Parse(x[1]) : 0;
                try
                {
                    switch (x[0])
                    {
                        case "a": result = adjustment.GetMoveRemoveAmount(a).ToString(); break;
                        case "q": result = slb.GetMoveAdjustment(a).ToString(); break;
                        case "e":
                            bool found = slb.TryGetEntry(a, out var entry);
                            result = (found ? "1" : "0") + "," + entry.Version + "," + entry.MoveAdjustment; break;
                        case "k": result = Dictionary(slb.GetCachedData(a)); break;
                        case "g": c.Group = a; break;
                        case "n": c.Level.Number = a; break;
                        case "m": c.Level.UpdateMoveCount(a); break;
                        case "p": result = fresh.GetMaxMoves().ToString(); break;
                        case "l": result = fresh.GetLeftMoves().ToString(); break;
                        case "z": result = fresh.GetMadeMoves().ToString(); break;
                        case "r": result = duke.ShouldRemoveOneMove() ? "1" : "0"; break;
                        case "t": OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount = a; break;
                        case "v":
                            bool parsed = OriginalSlbMoveConfig.TryParseLevelVersionToken(Decode(x[1]), out int level, out int version);
                            result = (parsed ? "1" : "0") + "," + level + "," + version; break;
                    }
                }
                catch (Exception) { result = "error"; }
                output.Add(result + ":" + OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount + ":" + c.Group + ":" + c.Level.Number + ":" + c.Level.MoveCount + ":" + string.Join(",", c.Trace.ToArray()));
            }
            Console.WriteLine(string.Join("|", output.ToArray()));
        }
    }
}
