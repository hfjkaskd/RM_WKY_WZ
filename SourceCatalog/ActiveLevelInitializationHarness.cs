using System;
using System.Collections.Generic;
using System.Linq;
using Whitebox.Foundation;
namespace Whitebox.Foundation { public interface IOriginalBoxExplosionServices { void UsefulMoveMade(); } }
static class ActiveLevelInitializationHarness
{
    sealed class Names : IOriginalThreeColorLevelNames
    {
        public bool ShouldReportThreeColorExtension(int a, int b) => throw new Exception();
        public bool TryGetVersionOverride(int a, out int b) { b = 0; throw new Exception(); }
        public string GetLevelExtension() => throw new Exception();
    }
    sealed class Values : OriginalBeforeAfterData
    {
        readonly Fixture f; readonly string label;
        public Values(Fixture f, string label) { this.f = f; this.label = label; }
        public override void Init(int start) { f.Trace.Add(label + "," + start); if (f.Fail == (label == "coin" ? 5 : 6)) throw new Exception(); base.Init(start); }
    }
    sealed class Progress : IOriginalSessionProgress
    {
        readonly Fixture f; readonly bool duke;
        public Progress(Fixture f, bool duke) { this.f = f; this.duke = duke; }
        public int CurrentProgress { get { f.Trace.Add(duke ? "duke_progress" : "booster_progress"); if (f.Fail == (duke ? 2 : 1)) throw new Exception(); return f.H[duke ? 7 : 6]; } }
    }
    sealed class Fixture : IOriginalActiveLevelResetContext, IOriginalSessionIntPreferences, IOriginalKingRelief
    {
        public int[] H; public int Fail => H[1]; public readonly List<string> Trace = new List<string>();
        public OriginalActiveLevelData Active; public int Count;
        public OriginalLevelStartInventory GetInventory() => H[0] == 1 ? null : new OriginalLevelStartInventory { Coin = H[4], Star = H[5] };
        public IOriginalSessionProgress GetSuperBooster() { Trace.Add("booster"); return H[0] == 5 ? null : new Progress(this, false); }
        public IOriginalSessionProgress GetSuperDuke() { Trace.Add("duke"); return H[0] == 6 ? null : new Progress(this, true); }
        public IOriginalSessionIntPreferences Preferences => this;
        public int GetInt(string key) { if (key != "SLTC") throw new Exception(); Trace.Add("get"); if (Fail == 3) throw new Exception(); return Count; }
        public void SetInt(string key, int value) { if (key != "SLTC") throw new Exception(); Trace.Add("set," + value); if (Fail == 4) throw new Exception(); Count = value; }
        public void TryKingRelief() { Trace.Add("king," + B(Active.IsUsefulMoveMade)); if (Fail == 7) throw new Exception(); }
    }
    static string B(bool b) => b ? "1" : "0";
    static string Snapshot(OriginalBeforeAfterData v) => v == null ? "null" : B(v.ShouldConsume) + "," + v.Before + "," + v.After;
    static string State(Fixture f, OriginalBeforeAfterData coin, OriginalBeforeAfterData extra)
    {
        var a = f.Active; var r = a.RestartData; var s = a.SpecialItemCounts;
        return string.Join(":", a.State, a.EventMultiplier, B(a.WonAnyLevel), a.ShiftCount, a.EnteredLevelDifficulty,
            B(a.IsUsefulMoveMade), a.UselessMoveCount, a.MaxUselessMoveStreak, a.CurrentUselessMoveStreak, a.RemainingItemsSnapshots.Count,
            Snapshot(a.Star), Snapshot(a.Cloche), Snapshot(a.SuperBooster), Snapshot(a.SuperDuke), Snapshot(a.MagicDuke), Snapshot(coin), Snapshot(extra),
            r.Purchases.Count, r.Refunds.Count, string.Concat(Enumerable.Range(1, 8).Select(n => B(r.HadUnlimitedPrelevel(n)))),
            string.Concat(Enumerable.Range(1, 8).Select(n => B(r.IsInGameBoosterAnimationActive(n)))),
            string.Join(",", Enumerable.Range(0, 8).Select(a.MatchItemCounts.GetCount)),
            s.Items.Count, s.Combos.Count, s.TappedLightballCount, s.SwappedItemCount, s.AutoTriggeredItemCount, s.SpecialItemCountCreatedByCrystal, f.Count);
    }
    static void Dirty(OriginalActiveLevelData a, OriginalBeforeAfterData coin, OriginalBeforeAfterData extra)
    {
        int n = 0;
        foreach (var v in new[] { a.Star, a.Cloche, a.SuperBooster, a.SuperDuke, a.MagicDuke, coin, extra })
        { if (v != null) { v.ShouldConsume = true; v.Before = 100 + n; v.After = 200 + n; } n++; }
        a.State = 5; a.EventMultiplier = 9; a.WonAnyLevel = true; a.ShiftCount = 88; a.EnteredLevelDifficulty = -8;
        a.IsUsefulMoveMade = false; a.UselessMoveCount = 4; a.MaxUselessMoveStreak = 9; a.CurrentUselessMoveStreak = 2;
        a.RemainingItemsSnapshots.Add(new OriginalRemainingItemSnapshot()); a.RemainingItemsSnapshots.Add(new OriginalRemainingItemSnapshot());
        a.RestartData.Purchases[4] = 4; a.RestartData.Refunds[5] = 5;
        for (int i = 1; i <= 8; i++) { a.RestartData.OnStartedWithUnlimitedPrelevel(i); a.RestartData.SetIsInGameBoosterAnimationActive(i, true); }
        a.SpecialItemCounts.Items[1] = new OriginalSpecialItemCount(); a.SpecialItemCounts.Combos[2] = 3;
        a.SpecialItemCounts.TappedLightballCount = 11; a.SpecialItemCounts.SwappedItemCount = 12;
        a.SpecialItemCounts.AutoTriggeredItemCount = 13; a.SpecialItemCounts.SpecialItemCountCreatedByCrystal = 14;
        for (int i = 1; i <= 6; i++) a.MatchItemCounts.IncrementCount(i, i * 11);
    }
    public static void Main()
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#'); var f = new Fixture { H = Array.ConvertAll(parts[0].Split(','), int.Parse) }; f.Count = f.H[8];
            var coin = f.H[0] == 3 ? null : new Values(f, "coin"); var extra = f.H[0] == 4 ? null : new Values(f, "extra");
            var animation = f.H[0] == 2 ? null : new OriginalActiveLevelAnimationValues(coin, extra);
            var slb = new OriginalSlbMoveConfig(() => throw new Exception(), () => throw new Exception());
            var a = new OriginalActiveLevelData(slb, new Names(), animation) { StoryLevel = f.H[2], Difficulty = (sbyte)f.H[3] }; f.Active = a;
            var useful = new OriginalUsefulMoveState(() => f, a);
            var outputs = new List<string>();
            foreach (string step in parts[1].Split(';'))
            {
                f.Trace.Clear(); string result = "-"; var fields = step.Split(','); int v = fields.Length > 1 ? int.Parse(fields[1]) : 0;
                try
                {
                    switch (fields[0])
                    {
                        case "r": a.ResetValues(f); break;
                        case "t": a.IncrementStoryLevelTryCount(f); break;
                        case "c": a.ResetCumulativeProgress(); break;
                        case "d": Dirty(a, coin, extra); break;
                        case "u": useful.UsefulMoveMade(); break;
                        case "i": useful.IncreaseUselessMoveCount(); break;
                        case "z": useful.ResetIsUsefulMoveMade(); break;
                        case "s": useful.ResetUselessMoveStreak(); break;
                        case "x": a.MatchItemCounts.Reset(); break;
                        case "p": a.SpecialItemCounts.Reset(); break;
                        case "m": a.MatchItemCounts.IncrementCount(v, int.Parse(fields[2])); break;
                        case "n": a.RestartData.OnStartedWithUnlimitedPrelevel(v); break;
                        case "h": a.RestartData.SetIsInGameBoosterAnimationActive(v, fields[2] == "1"); break;
                    }
                }
                catch { result = "error"; }
                outputs.Add(result + ":" + State(f, coin, extra) + ":" + string.Join(";", f.Trace));
            }
            Console.WriteLine(string.Join("|", outputs));
        }
    }
}
