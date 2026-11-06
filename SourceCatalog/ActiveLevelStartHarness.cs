using System;
using System.Collections.Generic;
using System.Linq;
using Whitebox.Foundation;
static class ActiveLevelStartHarness
{
    sealed class Value : OriginalBeforeAfterData
    {
        readonly Input f; readonly int slot;
        public Value(Input f, int slot) { this.f = f; this.slot = slot; }
        public override void Init(int value) { f.Record("init," + slot + "," + value); base.Init(value); }
    }
    sealed class Archery : OriginalArcheryLevelData
    {
        readonly Input f; public Archery(Input f) { this.f = f; }
        public override void Init(int value) { f.Record("init,152," + value); base.Init(value); }
    }
    sealed class Progress : IOriginalSessionProgress
    {
        readonly Input f; readonly int id, value;
        public Progress(Input f, int id, int value) { this.f = f; this.id = id; this.value = value; }
        public int CurrentProgress { get { f.Record("progress," + id); return value; } }
    }
    sealed class RestartSnapshot : IOriginalRestartSnapshot, IOriginalRestartDifficultySnapshot, IOriginalClashLevelStart
    {
        readonly Input f; readonly string name;
        public RestartSnapshot(Input f, string name) { this.f = f; this.name = name; }
        public void Init(bool restart) { f.Record(name + "," + B(restart)); }
        public void Init(bool restart, int difficulty) { f.Record(name + "," + B(restart) + "," + difficulty); }
        public void Init(sbyte difficulty, bool multiplied, bool restored) { f.Record(name + "," + difficulty + "," + B(multiplied) + "," + B(restored)); }
    }
    sealed class Input : IOriginalActiveLevelStartContext, IOriginalClocheLevelStart, IOriginalMultiplierLevelStart,
        IOriginalUnlimitedBoosterTimes, IOriginalBalloonLevelStart, IOriginalArcheryLevelStart, IOriginalPropellerRushProgress,
        IOriginalSessionIntPreferences, IOriginalThreeColorLevelNames
    {
        public long[] H;
        public OriginalActiveLevelData Active;
        public int Missing => (int)H[9];
        public readonly List<string> Trace = new List<string>();
        public int Count, ClocheCalls, CanPlayCalls, InventoryCalls;
        public bool Seeding;
        public OriginalActiveLevelAnimationValues Animation;
        public OriginalLevelStartSnapshots Snapshots;
        public readonly Dictionary<int, OriginalBeforeAfterData> Values = new Dictionary<int, OriginalBeforeAfterData>();
        public void Record(string text) { if (Seeding) return; Trace.Add(text); if (Trace.Count == H[10]) throw new InvalidOperationException(); }
        public void Begin() { Trace.Clear(); ClocheCalls = 0; CanPlayCalls = 0; InventoryCalls = 0; }
        public OriginalLevelStartInventory GetInventory() => new OriginalLevelStartInventory { Coin = (int)H[4], Star = (int)H[5] };
        public IOriginalSessionProgress GetSuperBooster() { Record("unit,29"); return new Progress(this, 29, (int)H[6]); }
        public IOriginalSessionProgress GetSuperDuke() { Record("unit,50"); return new Progress(this, 50, (int)H[7]); }
        public IOriginalSessionProgress GetMagicDuke() { Record("unit,63"); return Missing == 3 ? null : new Progress(this, 63, 13); }
        public IOriginalSessionIntPreferences Preferences => this;
        public int GetInt(string key) { if (key != "SLTC") throw new Exception(); Record("get"); return Count; }
        public void SetInt(string key, int value) { if (key != "SLTC") throw new Exception(); Record("set," + value); Count = value; }
        public IOriginalClocheLevelStart GetCloche() { Record("unit,31"); ClocheCalls++; return Missing == ClocheCalls && Missing <= 2 ? null : this; }
        int IOriginalClocheLevelStart.GetCloche() { Record("cloche"); return 11; }
        public void SaveProgressData(int value) { Record("save_cloche," + value); }
        public IOriginalMultiplierLevelStart GetMultiplierSelection() { Record("unit,79"); return Missing == 4 ? null : this; }
        public bool CanPlayLevelWithMultiplier { get { Record("can_play"); return H[++CanPlayCalls == 1 ? 12 : 13] != 0; } }
        public int GetSelectedMultiplierValue() { Record("selected"); return (int)H[14]; }
        public bool CanMultiply(bool selected) { if (selected) throw new Exception(); Record("multiply"); return H[11] != 0; }
        public IOriginalUnlimitedBoosterTimes GetUnlimitedBoosterInventory() { InventoryCalls++; return Missing == InventoryCalls + 5 ? null : this; }
        public int RemainingTime(int booster) { Record("remaining," + booster); return (int)H[15 + booster - 4]; }
        public float RealtimeSinceStartup { get { Record("clock"); return 42.25f; } }
        public IOriginalBalloonLevelStart GetBalloonRise() { Record("unit,57"); return Missing == 10 ? null : this; }
        public int GetCollectibleCount() { Record("balloon"); return (int)H[19]; }
        public IOriginalArcheryLevelStart GetArcheryArena() { Record("unit,45"); return Missing == 11 ? null : this; }
        bool IOriginalArcheryLevelStart.ShouldProgress { get { Record("archery_progress"); return H[22] != 0; } }
        public OriginalArcheryStartStreak Streak => Missing == 12 ? null : new OriginalArcheryStartStreak { PackedProgress = H[23] };
        public OriginalArcheryStartRank Rank => Missing == 13 ? null : new OriginalArcheryStartRank { RankIndex = (int)H[24] };
        public bool IsIconActive { get { Record("icon"); return H[25] != 0; } }
        public IOriginalPropellerRushProgress PropellerManager() { Record("unit,66"); return Missing == 14 ? null : this; }
        bool IOriginalPropellerRushProgress.ShouldProgress { get { Record("propeller_progress"); return H[20] != 0; } }
        public bool IsTable() { Record("table"); return H[21] != 0; }
        public bool ShouldReportThreeColorExtension(int a, int b) => throw new Exception();
        public bool TryGetVersionOverride(int a, out int b) { b = 0; throw new Exception(); }
        public string GetLevelExtension() => throw new Exception();
        public OriginalBeforeAfterData V(int slot)
        { var value = new Value(this, slot); Values[slot] = value; return Missing == 1000 + slot ? null : value; }
        public T Entry<T>(int slot, T value) where T : class { return Missing == 1000 + slot ? null : value; }
        public void Create()
        {
            Count = (int)H[8]; var slb = new OriginalSlbMoveConfig(() => throw new Exception(), () => throw new Exception());
            var coin = V(40); var extra = V(48);
            Snapshots = new OriginalLevelStartSnapshots {
                Madness = Entry(64, new RestartSnapshot(this, "madness")), RoyalPass = Entry(72, new RestartSnapshot(this, "royal")), WeeklyPass = V(80),
                PinataParty = Entry(160, new RestartSnapshot(this, "pinata")), DailyLightningRush = V(136),
                PropellerRush = Entry(144, new OriginalPropellerRushLevelData(PropellerManager, IsTable, () => Missing == 16 ? null : Active)),
                SkyRace = V(56), KingsCup = V(24), TeamTreasure = V(88), BalloonRise = V(104), SpaceMission = V(96), TeamBattle = V(32), LeagueCrown = V(16),
                HiddenTemple = V(112), DragonNest = V(120), MagicCauldron = V(128), DukesFortune = V(168), TeamTournament = V(176), OceanOdyssey = V(192),
                MissionEvents = Entry(200, new OriginalMissionEventsStartData()), TrainJourney = V(184), PuzzleBreak = V(208), RobotRivals = V(216), RoyalWorldCup = V(240), MergeSmith = V(224),
                ClashOfChampions = Entry(232, new RestartSnapshot(this, "clash")), ShortExpeditionEvent = V(248), InviteFriends = Entry(256, new OriginalInviteFriendsStartData()), DukesTreats = V(264),
                ArcheryArena = Entry(152, new Archery(this))
            };
            Animation = new OriginalActiveLevelAnimationValues(coin, extra, Missing == 15 ? null : Snapshots);
            Active = new OriginalActiveLevelData(slb, this, Missing == 5 ? null : Animation) { StoryLevel = (int)H[2], Difficulty = (sbyte)H[3] };
        }
    }
    static string B(bool b) => b ? "1" : "0";
    static string ValueState(OriginalBeforeAfterData v) => v == null ? "null" : B(v.ShouldConsume) + "," + v.Before + "," + v.After;
    static string State(Input f)
    {
        var a = f.Active; var p = f.Snapshots.PropellerRush; var m = f.Snapshots.MissionEvents; var inv = f.Snapshots.InviteFriends; var ar = f.Snapshots.ArcheryArena;
        var r = a.RestartData; var s = a.SpecialItemCounts;
        var core = string.Join(",", a.State, a.EventMultiplier, B(a.WonAnyLevel), a.ShiftCount, a.EnteredLevelDifficulty, B(a.IsUsefulMoveMade), a.UselessMoveCount, a.MaxUselessMoveStreak, a.CurrentUselessMoveStreak, a.RemainingItemsSnapshots.Count,
            a.EgoPurchaseCount, a.NoPossibleMoveShuffleCount, a.NoPossibleMoveColoredShuffleCount, a.FalseSwipeCount, a.ShownHintCount, a.UsedHintCount, a.LevelStartTime, B(a.IsMultipliedInLevel), B(f.Animation.CanPlayLevelWithMultiplier),
            B(a.HasUnlimitedHammer), B(a.DidUseUnlimitedHammer), B(a.HasUnlimitedArrow), B(a.DidUseUnlimitedArrow), B(a.HasUnlimitedCannon), B(a.DidUseUnlimitedCannon), B(a.HasUnlimitedJester), B(a.DidUseUnlimitedJester), f.Count);
        var owned = string.Join("/", new[] { a.Star, a.Cloche, a.SuperBooster, a.SuperDuke, a.MagicDuke }.Select(ValueState));
        string counters = string.Join(",", r.Purchases.Count, r.Refunds.Count, s.Items.Count, s.Combos.Count, s.TappedLightballCount, s.SwappedItemCount, s.AutoTriggeredItemCount, s.SpecialItemCountCreatedByCrystal) + "," + string.Join(",", Enumerable.Range(1, 5).Select(a.MatchItemCounts.GetCount));
        return core + ":" + owned + ":" + counters + ":" + string.Join("/", f.Values.OrderBy(v => v.Key).Select(v => v.Key + "=" + ValueState(v.Value)))
            + ":" + (p == null ? "null" : ValueState(p) + "," + p.CollectedAmountAfterWin + "," + p.Difficulty)
            + ":" + (m == null ? "null" : string.Join(",", B(m.ShouldConsume), m.CreatedPropellerCount, m.CreatedRocketCount, m.CreatedTntCount, m.CreatedLightballCount, m.CreatedSuperLightballCount))
            + ":" + (inv == null ? "null" : B(inv.ShouldConsume) + "," + inv.EgoPrice)
            + ":" + (ar == null ? "null" : ValueState(ar) + "," + ar.StreakIndex + "," + ar.RankIndex + "," + B(ar.IconWasVisibleAtHome));
    }
    static void Dirty(Input f)
    {
        f.Seeding = true;
        var a = f.Active;
        foreach (var v in new[] { a.Star, a.Cloche, a.SuperBooster, a.SuperDuke, a.MagicDuke }.Concat(f.Values.OrderBy(v => v.Key).Select(v => v.Value)))
        { v.ShouldConsume = true; v.Before = 101; v.After = 202; }
        a.State = 5; a.EventMultiplier = 9; a.WonAnyLevel = true; a.ShiftCount = 88; a.EnteredLevelDifficulty = -8;
        a.IsUsefulMoveMade = false; a.UselessMoveCount = 4; a.MaxUselessMoveStreak = 9; a.CurrentUselessMoveStreak = 2;
        a.EgoPurchaseCount = 7; a.NoPossibleMoveShuffleCount = 3; a.NoPossibleMoveColoredShuffleCount = 4;
        a.FalseSwipeCount = 5; a.ShownHintCount = 6; a.UsedHintCount = 7; a.LevelStartTime = 8.5f; a.IsMultipliedInLevel = true;
        a.HasUnlimitedHammer = a.HasUnlimitedArrow = a.HasUnlimitedCannon = a.HasUnlimitedJester = true;
        a.DidUseUnlimitedHammer = a.DidUseUnlimitedArrow = a.DidUseUnlimitedCannon = a.DidUseUnlimitedJester = true;
        f.Animation.CanPlayLevelWithMultiplier = true;
        a.RemainingItemsSnapshots.Add(new OriginalRemainingItemSnapshot()); a.RestartData.Purchases[4] = 4; a.RestartData.Refunds[5] = 5;
        a.SpecialItemCounts.Items[1] = new OriginalSpecialItemCount(); a.SpecialItemCounts.Combos[2] = 3;
        a.SpecialItemCounts.TappedLightballCount = 11; a.SpecialItemCounts.SwappedItemCount = 12; a.SpecialItemCounts.AutoTriggeredItemCount = 13; a.SpecialItemCounts.SpecialItemCountCreatedByCrystal = 14;
        for (int i = 1; i <= 5; i++) a.MatchItemCounts.IncrementCount(i, i * 11);
        var p = f.Snapshots.PropellerRush; if (p != null) { p.ShouldConsume = true; p.Before = 101; p.After = 202; }
        var m = f.Snapshots.MissionEvents; if (m != null) { m.ShouldConsume = true; m.Init(5); }
        var inv = f.Snapshots.InviteFriends; if (inv != null) { inv.ShouldConsume = true; inv.EgoPrice = 55; }
        var ar = f.Snapshots.ArcheryArena; if (ar != null) { ar.Init(7, 3, 9, true); ar.ShouldConsume = true; ar.Before = 101; ar.After = 202; }
        f.Seeding = false;
    }
    public static void Main()
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var parts = line.Split('#'); var f = new Input { H = Array.ConvertAll(parts[0].Split(','), long.Parse) }; f.Create(); var output = new List<string>();
            foreach (var step in parts[1].Split(';'))
            {
                f.Begin(); string result = "-";
                try
                {
                    if (step == "d") { Dirty(f); f.Trace.Clear(); }
                    else if (step == "b") f.Active.Start(f);
                    else if (step == "r") f.Active.Restart(f);
                    else if (step.StartsWith("v,")) f.Active.SetStartValues(f, int.Parse(step.Substring(2)));
                    else if (step.StartsWith("flags,")) { var flags = step.Split(','); f.Active.IsBonus = flags[1] == "1"; f.Active.IsTableGame = flags[2] == "1"; }
                    else
                    {
                        var args = step.Split(',');
                        switch (args[0])
                        {
                            case "pi": f.Snapshots.PropellerRush.Init(int.Parse(args[1]), int.Parse(args[2])); break;
                            case "pm": f.Snapshots.PropellerRush.TryIncrementCount(int.Parse(args[1])); break;
                            case "pr": f.Snapshots.PropellerRush.Reset(); break;
                            case "pl": f.Snapshots.PropellerRush.LoadSaveData(long.Parse(args[1])); break;
                            case "pg": result = f.Snapshots.PropellerRush.GetSaveData().ToString(); break;
                            case "al": f.Snapshots.ArcheryArena.LoadSaveData(long.Parse(args[1])); break;
                            case "ag": result = f.Snapshots.ArcheryArena.GetSaveData().ToString(); break;
                            case "ai": f.Snapshots.ArcheryArena.Init(int.Parse(args[1]), int.Parse(args[2]), int.Parse(args[3]), args[4] == "1"); break;
                            case "mul": result = OriginalBeforeAfterData.GetMultiplierValueForDifficulty(int.Parse(args[1])).ToString(); break;
                        }
                    }
                }
                catch { result = "error"; }
                output.Add(result + ":" + State(f) + ":" + string.Join(";", f.Trace));
            }
            Console.WriteLine(string.Join("|", output));
        }
    }
}
