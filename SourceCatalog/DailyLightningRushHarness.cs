using System;
using System.Collections.Generic;
using Whitebox.Foundation;
static class DailyLightningRushHarness
{
    sealed class OtherBundle : IOriginalSessionBundle
    { public bool LoadBundle() => throw new Exception(); public void UnloadBundle(bool all) => throw new Exception(); }
    sealed class Bundle : IOriginalSessionEventBundle
    {
        readonly Inputs f; readonly int id;
        public Bundle(Inputs f, int id) { this.f = f; this.id = id; }
        public bool LoadBundle() { f.Record("load"); return f.H[4] != 0; }
        public void UnloadBundle(bool all) { f.Record("unload," + id + "," + (all ? 1 : 0)); }
    }
    sealed class Data : OriginalDailyLightningRushLevelData
    {
        public Data(Inputs f) : base(() => f.Resolve("manager") ? f : null, () => f.Resolve("owner") ? f : null,
            () => f.Resolve("pools") ? f : null, () => f.H[10] != 0 ? null : f.Active, f.IsTable) { }
        public void Dirty(Inputs f)
        { ShouldConsume = true; Before = 101; After = 202; step = 55; lightningRushIconCount = 7; difficulty = -9; collectView = f; isCollectViewReady = true; gameplayBundle = f.Old; }
        public void ClearBundle() { gameplayBundle = null; }
        public bool Increment(int amount) => IncrementCount(amount);
    }
    sealed class Inputs : IOriginalDailyLightningRushSessionManager, IOriginalDailyLightningRushProgress,
        IOriginalDailyLightningRushPools, IOriginalSessionBundleManager, IOriginalDailyLightningRushCollectView, IOriginalThreeColorLevelNames
    {
        public long[] H;
        public int Resolves, ProgressReads, StepReads;
        public readonly List<string> Trace = new List<string>();
        public Bundle Old, New;
        public readonly IOriginalSessionBundle Wrong = new OtherBundle();
        public OriginalActiveLevelData Active;
        public Data Data;
        public void Record(string label) { Trace.Add(label); if (Trace.Count == H[1]) throw new InvalidOperationException(); }
        public bool Resolve(string name) { Record(name); return ++Resolves != H[0]; }
        public bool ShouldProgress { get { Record("progress"); return H[++ProgressReads == 1 ? 2 : 12] != 0; } }
        public bool IsAllBundlesDownloaded() { Record("downloaded"); return H[3] != 0; }
        public IOriginalDailyLightningRushProgress Progress => H[9] != 0 ? null : this;
        public int LastEnteredStep { get { Record("step"); return (int)H[++StepReads == 1 ? 5 : 6]; } }
        public IOriginalSessionBundle GetBundle(string name)
        { if (name != OriginalDailyLightningRushLevelData.GameplayBundleName) throw new Exception(); return !Resolve("bundle") ? null : H[11] != 0 ? Wrong : New; }
        public void RecycleAllSpawnedDailyLightningRushCollectViews() { Record("recycle"); }
        public void ClearDailyLightningRushPool() { Record("clear"); if (H[13] != 0) Data.ClearBundle(); }
        public void CreateDailyLightningRushPool() { Record("create"); }
        public bool IsTable() { Record("table"); return H[8] != 0; }
        public bool ShouldReportThreeColorExtension(int a, int b) => throw new InvalidOperationException();
        public bool TryGetVersionOverride(int a, out int b) { b = 0; throw new InvalidOperationException(); }
        public string GetLevelExtension() => throw new InvalidOperationException();
        public void Begin() { Resolves = 0; ProgressReads = 0; StepReads = 0; Trace.Clear(); }
    }
    static string State(Inputs f)
    {
        var d = f.Data;
        return string.Join(",", new object[] { d.ShouldConsume ? 1 : 0, d.Before, d.After, d.Step, d.LightningRushIconCount,
            d.Difficulty, d.CollectView == null ? 0 : 1, d.IsCollectViewReady ? 1 : 0,
            d.GameplayBundle == null ? 0 : ReferenceEquals(d.GameplayBundle, f.Old) ? 1 : 2 });
    }
    static void Main()
    {
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var p = line.Split('#'); var f = new Inputs { H = Array.ConvertAll(p[0].Split(','), long.Parse) };
            f.Old = new Bundle(f, 1); f.New = new Bundle(f, 2);
            var slb = new OriginalSlbMoveConfig(() => throw new InvalidOperationException(), () => throw new InvalidOperationException());
            f.Active = new OriginalActiveLevelData(slb, f, null) { Difficulty = (sbyte)f.H[7] }; f.Data = new Data(f);
            var output = new List<string>();
            foreach (var command in p[1].Split(';'))
            {
                f.Begin(); var v = command.Split(','); var mode = v[0]; string result = "-";
                try
                {
                    switch (mode)
                    {
                        case "d": f.Data.Dirty(f); break;
                        case "i": f.Data.Init(int.Parse(v[1])); break;
                        case "r": f.Data.InitForRestore(int.Parse(v[1])); break;
                        case "l": f.Data.LoadSaveData(long.Parse(v[1])); break;
                        case "g": result = f.Data.GetSaveData().ToString(); break;
                        case "v": result = f.Data.IsEventValid() ? "1" : "0"; break;
                        case "n": result = f.Data.Increment(int.Parse(v[1])) ? "1" : "0"; break;
                        case "h": f.H[int.Parse(v[1])] = long.Parse(v[2]); break;
                        case "b": f.Data.Before = int.Parse(v[1]); f.Data.After = int.Parse(v[2]); f.Data.ShouldConsume = false; break;
                    }
                }
                catch { result = "error"; }
                output.Add(result + ":" + State(f) + ":" + string.Join(";", f.Trace));
            }
            Console.WriteLine(string.Join("|", output));
        }
    }
}
