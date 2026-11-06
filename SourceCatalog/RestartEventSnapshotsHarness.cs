using System;
using System.Collections.Generic;
using Whitebox.Foundation;
static class RestartEventSnapshotsHarness
{
    sealed class Inputs : IOriginalMadnessSessionManager, IOriginalRoyalPassSessionManager, IOriginalPinataSessionManager,
        IOriginalPropellerRushProgress, IOriginalMadnessSessionPools, IOriginalSessionAbValues
    {
        public long[] H;
        public readonly List<string> Trace = new List<string>();
        public int Resolves, ManagerResolves;
        public void Record(string text) { Trace.Add(text); if (Trace.Count == H[1]) throw new InvalidOperationException(); }
        public bool Resolve(string name)
        { Record(name); Resolves++; if (name == "manager") ManagerResolves++; return Resolves != H[0]; }
        public int EventId { get { if (Kind == 2) Record("id"); return unchecked((int)H[6] + (H[12] != 0 ? ManagerResolves : 0)); } }
        public int EventType => (int)H[7];
        public int Kind;
        public bool ShouldShowIcon() { Record("show"); return H[2] != 0; }
        public bool IsEventActive { get { Record("active"); return H[2] != 0; } }
        public bool IsEventPlayable { get { Record("playable"); return H[2] != 0; } }
        public bool ShouldShowFirstTimeTutorial() { Record("tutorial"); return H[3] != 0; }
        public bool IsSafeFull() { Record("safe"); return H[4] != 0; }
        public bool CanClaimFinalStep() { Record("final"); return H[4] != 0; }
        public int GetCount() { Record("count"); return (int)H[5]; }
        public bool ShouldProgress { get { Record("progress"); return H[8] != 0; } }
        public void CreateMadnessPools(int type) { Record("create," + type); }
        public bool IsTable() { Record("table"); return H[9] != 0; }
        public int GetVariant(int id) { Record("ab," + id); return (int)H[id == 224 ? 10 : 11]; }
        public void Begin() { Trace.Clear(); Resolves = 0; ManagerResolves = 0; }
    }
    sealed class Madness : OriginalMadnessSessionData
    {
        readonly Inputs f;
        public Madness(Inputs f) : base(() => f.Resolve("manager") ? f : null, () => f.Resolve("propeller") ? f : null,
            () => f.Resolve("pools") ? f : null, f.IsTable) { this.f = f; }
        public override void Init(int start) { f.Record("init," + start); base.Init(start); }
    }
    sealed class Royal : OriginalRoyalPassLevelData
    {
        readonly Inputs f;
        public Royal(Inputs f) : base(() => f.Resolve("manager") ? f : null) { this.f = f; }
        public override void Init(int start) { f.Record("init," + start); base.Init(start); }
    }
    sealed class Pinata : OriginalPinataPartyLevelData
    {
        readonly Inputs f;
        public Pinata(Inputs f) : base(() => f.Resolve("manager") ? f : null, () => f.Resolve("ab") ? f : null) { this.f = f; }
        public override void Init(int start) { f.Record("init," + start); base.Init(start); }
    }
    static string State(OriginalBeforeAfterData data)
    {
        var e = data as OriginalRestartEventData; var m = data as OriginalMadnessSessionData; var d = data as OriginalDukesTreatsLevelData;
        return (data.ShouldConsume ? 1 : 0) + "," + data.Before + "," + data.After + "," + (e == null ? -1 : e.EventId) + "," +
            (m == null || m.ExploderIdMatchItemCount == null ? -1 : m.ExploderIdMatchItemCount.Count) + "," + (d != null && d.IsCooldownReduction ? 1 : 0);
    }
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#'); var f = new Inputs { Kind = int.Parse(parts[0]), H = Array.ConvertAll(parts[1].Split(','), long.Parse) };
            OriginalBeforeAfterData data = f.Kind == 0 ? (OriginalBeforeAfterData)new Madness(f) : f.Kind == 1 ? new Royal(f) : f.Kind == 2 ? new Pinata(f) : (OriginalBeforeAfterData)new OriginalDukesTreatsLevelData();
            var output = new List<string>();
            foreach (var step in parts[2].Split(';'))
            {
                var v = step.Split(','); f.Begin(); string result = "-";
                try
                {
                    switch (v[0])
                    {
                        case "i":
                            if (f.Kind == 0) ((OriginalMadnessSessionData)data).Init(v[1] == "1", int.Parse(v[2]));
                            else if (f.Kind == 1) ((OriginalRoyalPassLevelData)data).Init(v[1] == "1");
                            else if (f.Kind == 2) ((OriginalPinataPartyLevelData)data).Init(v[1] == "1");
                            else data.Init(int.Parse(v[1])); break;
                        case "g": result = data.GetSaveData().ToString(); break;
                        case "l": data.LoadSaveData(long.Parse(v[1])); break;
                        case "a": data.AddExtra(int.Parse(v[1])); break;
                        case "u": data.Update(int.Parse(v[1])); break;
                        case "d": data.ShouldConsume = true; data.Before = int.Parse(v[1]); data.After = int.Parse(v[2]); break;
                        case "v": result = (f.Kind == 0 ? ((OriginalMadnessSessionData)data).IsEventValid() : f.Kind == 1 ? ((OriginalRoyalPassLevelData)data).IsEventValid() : ((OriginalPinataPartyLevelData)data).IsEventValid()) ? "1" : "0"; break;
                        case "s": result = ((OriginalRoyalPassLevelData)data).ShouldSetData(v[1] == "1") ? "1" : "0"; break;
                        case "r": ((OriginalMadnessSessionData)data).InitAfterRestore(); break;
                        case "x": if (((OriginalMadnessSessionData)data).ExploderIdMatchItemCount != null) ((OriginalMadnessSessionData)data).ExploderIdMatchItemCount[42] = 7; break;
                        case "c": ((OriginalDukesTreatsLevelData)data).CollectTreat(int.Parse(v[1])); break;
                        case "t": ((OriginalDukesTreatsLevelData)data).ReduceCooldown(int.Parse(v[1])); break;
                        case "q": result = ((OriginalDukesTreatsLevelData)data).GetTreatCount().ToString(); break;
                    }
                }
                catch { result = "error"; }
                output.Add(result + ":" + State(data) + ":" + string.Join(";", f.Trace));
            }
            Console.WriteLine(string.Join("|", output));
        }
    }
}
