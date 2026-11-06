using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Explicit application/feature boundary inputs. No fixture is installed in
    // the running scene, and required production services are not replaced.
    public static class ActiveLevelStartVerification
    {
        private sealed class EventBoundary : IOriginalRestartSnapshot, IOriginalRestartDifficultySnapshot, IOriginalClashLevelStart
        {
            public int Calls, Difficulty;
            public bool Restart, Multiplied, Restored;
            public void Init(bool restart) { Calls++; Restart = restart; }
            public void Init(bool restart, int difficulty) { Calls++; Restart = restart; Difficulty = difficulty; }
            public void Init(sbyte difficulty, bool multiplied, bool restored)
            { Calls++; Difficulty = difficulty; Multiplied = multiplied; Restored = restored; }
        }
        private sealed class Inputs : IOriginalActiveLevelStartContext, IOriginalSessionProgress, IOriginalSessionIntPreferences,
            IOriginalClocheLevelStart, IOriginalMultiplierLevelStart, IOriginalUnlimitedBoosterTimes, IOriginalBalloonLevelStart,
            IOriginalArcheryLevelStart, IOriginalPropellerRushProgress, IOriginalThreeColorLevelNames, IOriginalSlbAbValues
        {
            public OriginalActiveLevelData Active;
            public bool MissingCloche, CanSelect = true, Multiplied = true;
            public int PreferenceCount = int.MaxValue, PreferenceWrites, ClocheQueries, SavedCloche;
            public int[] InventoryTypes = new int[4]; public int InventoryReads;
            public float StartTime;
            public OriginalLevelStartInventory GetInventory() => new OriginalLevelStartInventory { Star = 7, Coin = 1001 };
            public IOriginalSessionProgress GetSuperBooster() => this;
            public IOriginalSessionProgress GetSuperDuke() => this;
            public IOriginalSessionProgress GetMagicDuke() => this;
            public int CurrentProgress => 9;
            public IOriginalSessionIntPreferences Preferences => this;
            public int GetInt(string key) { Require(key == "SLTC", "Native story key"); return PreferenceCount; }
            public void SetInt(string key, int value) { Require(key == "SLTC", "Native story key"); PreferenceCount = value; PreferenceWrites++; }
            public IOriginalClocheLevelStart GetCloche() { ClocheQueries++; return MissingCloche ? null : this; }
            int IOriginalClocheLevelStart.GetCloche() => 2;
            public void SaveProgressData(int value) { SavedCloche = value; }
            public IOriginalMultiplierLevelStart GetMultiplierSelection() => this;
            public bool CanPlayLevelWithMultiplier => CanSelect;
            public int GetSelectedMultiplierValue() => 3;
            public bool CanMultiply(bool selected) { Require(!selected, "Native false CanMultiply argument"); return Multiplied; }
            public IOriginalUnlimitedBoosterTimes GetUnlimitedBoosterInventory() => this;
            public int RemainingTime(int type)
            { InventoryTypes[InventoryReads++ % 4] = type; return type == 4 ? 1 : type == 7 ? 30 : type == 6 ? -1 : 0; }
            public float RealtimeSinceStartup { get { StartTime = Time.realtimeSinceStartup; return StartTime; } }
            public IOriginalBalloonLevelStart GetBalloonRise() => this;
            public int GetCollectibleCount() => 17;
            public IOriginalArcheryLevelStart GetArcheryArena() => this;
            bool IOriginalArcheryLevelStart.ShouldProgress => true;
            public OriginalArcheryStartStreak Streak => new OriginalArcheryStartStreak { PackedProgress = 5L << 20 };
            public OriginalArcheryStartRank Rank => new OriginalArcheryStartRank { RankIndex = 4 };
            public bool IsIconActive => true;
            bool IOriginalPropellerRushProgress.ShouldProgress => true;
            public bool ShouldReportThreeColorExtension(int a, int b) => false;
            public bool TryGetVersionOverride(int a, out int b) { b = 0; throw new InvalidOperationException("Unexpected version input"); }
            public string GetLevelExtension() => throw new InvalidOperationException("Unexpected extension input");
            public int AbGroup => 0;
            public void SetAbValues() { }
        }
        private static OriginalLevelStartSnapshots Snapshots(Inputs input, EventBoundary madness, EventBoundary royal, EventBoundary pinata, EventBoundary clash)
        {
            return new OriginalLevelStartSnapshots {
                Madness = madness, RoyalPass = royal, PinataParty = pinata, ClashOfChampions = clash,
                WeeklyPass = new OriginalBeforeAfterData(), DailyLightningRush = new OriginalBeforeAfterData(),
                PropellerRush = new OriginalPropellerRushLevelData(() => input, () => false, () => input.Active),
                SkyRace = new OriginalBeforeAfterData(), KingsCup = new OriginalBeforeAfterData(), TeamTreasure = new OriginalBeforeAfterData(), BalloonRise = new OriginalBeforeAfterData(),
                SpaceMission = new OriginalBeforeAfterData(), TeamBattle = new OriginalBeforeAfterData(), LeagueCrown = new OriginalBeforeAfterData(), HiddenTemple = new OriginalBeforeAfterData(),
                DragonNest = new OriginalBeforeAfterData(), MagicCauldron = new OriginalBeforeAfterData(), DukesFortune = new OriginalBeforeAfterData(), TeamTournament = new OriginalBeforeAfterData(),
                OceanOdyssey = new OriginalBeforeAfterData(), MissionEvents = new OriginalMissionEventsStartData(), TrainJourney = new OriginalBeforeAfterData(), PuzzleBreak = new OriginalBeforeAfterData(),
                RobotRivals = new OriginalBeforeAfterData(), RoyalWorldCup = new OriginalBeforeAfterData(), MergeSmith = new OriginalBeforeAfterData(), ShortExpeditionEvent = new OriginalBeforeAfterData(),
                InviteFriends = new OriginalInviteFriendsStartData(), DukesTreats = new OriginalBeforeAfterData(), ArcheryArena = new OriginalArcheryLevelData()
            };
        }
        private sealed class EventInputs : IOriginalMadnessSessionManager, IOriginalRoyalPassSessionManager,
            IOriginalPinataSessionManager, IOriginalMadnessSessionPools, IOriginalPropellerRushProgress, IOriginalSessionAbValues
        {
            public int Id = 321, Type = 3, Count = 13, PoolCalls, LastPoolType, VariantReads;
            public bool Tutorial, SafeOrFinal, MissingPools;
            public int EventId => Id;
            public int EventType => Type;
            public bool IsEventActive => true;
            public bool IsEventPlayable => true;
            public bool ShouldShowIcon() => true;
            public bool ShouldShowFirstTimeTutorial() => Tutorial;
            public bool IsSafeFull() => SafeOrFinal;
            public bool CanClaimFinalStep() => SafeOrFinal;
            public int GetCount() => Count;
            public bool ShouldProgress => true;
            public int GetVariant(int id) { VariantReads++; return id == 225 ? 2 : 0; }
            public void CreateMadnessPools(int type) { PoolCalls++; LastPoolType = type; }
        }
        private static void VerifyConcreteEvents(Inputs input, OriginalLevelStartSnapshots snapshots)
        {
            var source = new EventInputs();
            var madness = new OriginalMadnessSessionData(() => source, () => source,
                () => source.MissingPools ? null : source, () => false);
            var royal = new OriginalRoyalPassLevelData(() => source);
            var pinata = new OriginalPinataPartyLevelData(() => source, () => source);
            var treats = new OriginalDukesTreatsLevelData();
            snapshots.Madness = madness; snapshots.RoyalPass = royal; snapshots.PinataParty = pinata; snapshots.DukesTreats = treats;
            input.MissingCloche = false; input.Active.StoryLevel = 0;
            input.Active.Start(input);
            Require(madness.EventId == 321 && madness.Before == 13 && madness.After == 43 && madness.ShouldConsume
                && royal.Before == 13 && pinata.Before == 13 && source.VariantReads == 2 && source.LastPoolType == 3,
                "Actual active Start initializes concrete Madness/RoyalPass/Pinata with original AB and difficulty branches");
            source.Id++;
            treats.ReduceCooldown(60);
            input.Active.Restart(input);
            Require(madness.Before == -1 && royal.Before == -1 && pinata.Before == -1
                && madness.EventId == -1 && royal.EventId == -1 && pinata.EventId == -1 && !treats.IsCooldownReduction,
                "Restart rejects changed event ids and dispatches DukesTreats virtual Init");
            source.Type = 1; input.Active.Start(input);
            var previous = madness.ExploderIdMatchItemCount; previous.Add(4, 9);
            madness.LoadSaveData(madness.GetSaveData());
            Require(madness.ExploderIdMatchItemCount != previous && madness.ExploderIdMatchItemCount.Count == 0
                && madness.Before == 13 && madness.EventId == source.Id, "Madness signed save restore recreates per-exploder state");
            var restoredRoyal = new OriginalRoyalPassLevelData(() => source); restoredRoyal.LoadSaveData(royal.GetSaveData());
            var restoredPinata = new OriginalPinataPartyLevelData(() => source, () => source); restoredPinata.LoadSaveData(pinata.GetSaveData());
            Require(restoredRoyal.IsEventValid() && restoredPinata.IsEventValid() && restoredRoyal.EventId == source.Id,
                "RoyalPass/Pinata save restore retains event identity and live validity");
            source.Tutorial = true; source.SafeOrFinal = true;
            input.Active.Start(input);
            Require(royal.Before == -1 && pinata.Before == 13, "Conversion AB retains positive Pinata progress through tutorial/final reward; RoyalPass skips");
            source.MissingPools = true; madness.Before = 77; madness.After = 99;
            bool failed = false; try { madness.Init(false, 1); } catch (NullReferenceException) { failed = true; }
            Require(failed && madness.EventId == source.Id && madness.Before == 77 && madness.After == 99
                && madness.ExploderIdMatchItemCount == null, "Missing pool dependency preserves original event-id write before virtual Init");
        }
        private sealed class DailyInputs : IOriginalDailyLightningRushSessionManager, IOriginalDailyLightningRushProgress,
            IOriginalDailyLightningRushPools, IOriginalSessionBundleManager, IOriginalSessionEventBundle
        {
            public bool ProgressEnabled = true, Downloaded = true, LoadSuccess = true, MissingPools;
            public int EnteredStep = 1, Creates, Clears, Recycles, Loads, Unloads;
            public bool UnloadAll;
            public bool ShouldProgress => ProgressEnabled;
            public bool IsAllBundlesDownloaded() => Downloaded;
            public IOriginalDailyLightningRushProgress Progress => this;
            public int LastEnteredStep => EnteredStep;
            public IOriginalSessionBundle GetBundle(string name)
            { Require(name == "daily_lightning_rush_gameplay_v2", "Original gameplay bundle key"); return this; }
            public bool LoadBundle() { Loads++; return LoadSuccess; }
            public void UnloadBundle(bool all) { Unloads++; UnloadAll = all; }
            public void CreateDailyLightningRushPool() { Creates++; }
            public void ClearDailyLightningRushPool() { Clears++; }
            public void RecycleAllSpawnedDailyLightningRushCollectViews() { Recycles++; }
        }
        private static void VerifyDailyLightning(Inputs input, OriginalLevelStartSnapshots snapshots)
        {
            var source = new DailyInputs();
            var daily = new OriginalDailyLightningRushLevelData(() => source, () => source,
                () => source.MissingPools ? null : source, () => input.Active, () => false);
            snapshots.DailyLightningRush = daily;
            input.MissingCloche = false; input.Active.StoryLevel = 0; input.Active.Difficulty = 1;
            input.Active.Start(input);
            Require(daily.Before == 0 && daily.After == 30 && daily.ShouldConsume && daily.Step == 1
                && source.Creates == 1 && source.Clears == 1 && ReferenceEquals(daily.GameplayBundle, source),
                "Concrete daily lightning initialization loads the native bundle and creates stage-specific pool before initial count");
            source.Downloaded = false; input.Active.Difficulty = 2; input.Active.Restart(input);
            Require(daily.Difficulty == 2 && daily.After == 30 && daily.Step == 1 && daily.ShouldConsume && source.Loads == 1,
                "Undownloaded daily lightning bundle retains old snapshot while updating difficulty");
            source.Downloaded = true; source.LoadSuccess = false; input.Active.Start(input);
            Require(daily.Before == -1 && daily.After == -1 && !daily.ShouldConsume && daily.GameplayBundle != null,
                "Failed daily lightning load invalidates snapshot but retains previous bundle owner");
            source.LoadSuccess = true; source.EnteredStep = 3; input.Active.Start(input);
            Require(daily.After == 50 && daily.Step == 3 && source.Creates == 2 && source.Clears == 2,
                "Changed step clears then recreates daily lightning pool with exact difficulty multiplier");
            long saved = daily.GetSaveData(); input.Active.Difficulty = 3; daily.LoadSaveData(saved);
            Require(daily.After == 50 && daily.Before == 0 && daily.Step == 3 && daily.Difficulty == 3
                && source.Creates == 3 && source.Clears == 2, "Daily lightning restore preserves counters and step, recovering actual active difficulty");
            source.ProgressEnabled = false; daily.Init(1);
            Require(daily.GameplayBundle == null && source.Recycles == 1 && source.Unloads == 1 && !source.UnloadAll
                && daily.Before == -1 && daily.After == -1 && daily.Step == 3, "Inactive daily lightning recycles/clears/unloads in order and retains step");
            source.ProgressEnabled = true; source.EnteredStep = 4; source.MissingPools = true;
            bool failed = false; try { daily.Init(2); } catch (NullReferenceException) { failed = true; }
            Require(failed && daily.Difficulty == 2 && daily.Step == 3 && daily.Before == -1 && daily.GameplayBundle == null,
                "Missing daily lightning pool preserves partial initialization before step assignment");
            source.MissingPools = false; input.Active.Difficulty = 1;
        }
        private sealed class ClashInputs : IOriginalClashSessionManager, IOriginalClashSessionHelper, IOriginalClashScoreConfig,
            IOriginalClashSessionPools, IOriginalSessionBundleManager, IOriginalSessionEventBundle,
            IOriginalClashGameplayAsset, IOriginalClashCollectViewPrefab
        {
            public bool InEvent = true, Downloaded = true, Locked, MissingHelper, ThrowUpdate;
            public int Creates, Clears, Unloads, Updates, LastScore, LastCombo;
            public IOriginalClashSessionHelper Helper => MissingHelper ? null : this;
            public IOriginalClashScoreConfig Config => this;
            public bool IsInEvent => InEvent;
            public bool IsAllBundlesDownloaded() => Downloaded;
            public bool ShouldProgress => true;
            public bool IsScoreLocked => Locked;
            public void UpdateLocalScore(int score, int combo)
            { Updates++; LastScore = score; LastCombo = combo; if (ThrowUpdate) throw new InvalidOperationException("Diagnostic score update failure"); }
            public IOriginalSessionBundle GetBundle(string name)
            { Require(name == "clash_of_champions_gameplay_v3" || name == "clash_of_champions_dialog_v2", "Original Clash bundle keys"); return this; }
            public bool LoadBundle() => true;
            public void UnloadBundle(bool all) { Require(all, "Clash unloads gameplay and dialog with true"); Unloads++; }
            public IOriginalClashGameplayAsset LoadAsset(IOriginalSessionEventBundle bundle, string name)
            { Require(ReferenceEquals(bundle, this) && name == "ClashOfChampionsGameplayCollectView", "Original Clash asset key/owner"); return this; }
            public IOriginalClashCollectViewPrefab GetCollectViewPrefab() => this;
            public void TryCreateClashOfChampionsGameplayCollectViewPool(IOriginalClashCollectViewPrefab prefab)
            { Require(ReferenceEquals(prefab, this), "Concrete asset component is forwarded to pool"); Creates++; }
            public void TryClearClashOfChampionsGameplayCollectViewPool() { Clears++; }
            public int GetMatchItemScore() => 11;
            public int GetPropellerItemScore() => 18;
            public int GetRocketItemScore() => 25;
            public int GetTntItemScore() => 32;
            public int GetLightballItemScore() => 39;
            public int GetPropellerPropellerComboScore() => 46;
            public int GetPropellerRocketComboScore() => 53;
            public int GetPropellerTntComboScore() => 60;
            public int GetRocketRocketComboScore() => 67;
            public int GetRocketTntComboScore() => 74;
            public int GetTntTntComboScore() => 81;
            public int GetLightballLightballComboMultiplier() => 88;
            public int GetRemainingMoveScore() => 95;
            public int GetWinBonus() => 102;
        }
        private static void VerifyClash(Inputs input, OriginalLevelStartSnapshots snapshots)
        {
            var source = new ClashInputs();
            var clash = new OriginalClashOfChampionsLevelData(() => source, () => source, () => source, source.LoadAsset, () => 3, () => input.Active);
            snapshots.ClashOfChampions = clash;
            input.MissingCloche = false; input.Active.StoryLevel = 0; input.Active.Difficulty = 1; input.Multiplied = true;
            input.Active.Start(input);
            Require(source.Creates == 1 && ReferenceEquals(clash.GameplayBundle, source), "Actual Start initializes Clash bundle/asset/component/pool before scoring");
            Require(clash.IncrementScoreForItem(1) == 99 && clash.IncrementScoreForItem(5) == 99
                && clash.IncrementScoreForCombo(2, 10) == 0 && source.LastCombo == 2 && source.Updates == 3
                && clash.After == 198 && clash.TotalLevelScoreForEvt == 198, "Native item fallback and zero-score combo update with difficulty/multiplier");
            clash.ResetDataOnRoundFinished();
            Require(clash.Before == 0 && clash.After == 0 && !clash.ShouldConsume && clash.TotalLevelScoreForEvt == 198, "Round reset retains total event score");
            source.ThrowUpdate = true; bool failed = false;
            try { clash.IncrementScoreForItem(2); } catch (InvalidOperationException) { failed = true; }
            Require(failed && clash.After == 0 && !clash.ShouldConsume && clash.TotalLevelScoreForEvt == 198,
                "Local score callback failure occurs before snapshot and total writes");
            source.ThrowUpdate = false; Require(clash.IncrementScoreForItem(2) == 225, "Native rocket score");
            long saved = clash.GetSaveData(); input.Active.Difficulty = 2; clash.LoadSaveData(saved);
            Require(clash.After == 225 && clash.TotalLevelScoreForEvt == 225 && source.Creates == 2
                && clash.IncrementScoreForItem(2) == 375, "Restore retains snapshot and seeds total from restored after, recovering live difficulty/multiplier");
            source.Downloaded = false; input.Active.Restart(input);
            Require(clash.After == 0 && clash.TotalLevelScoreForEvt == 0 && source.Creates == 2 && clash.GameplayBundle != null,
                "Undownloaded Clash retains old bundle ownership while fresh session counters reset");
            source.Downloaded = true; source.InEvent = false; input.Active.Start(input);
            Require(source.Clears == 1 && source.Unloads == 2 && clash.GameplayBundle == null, "Inactive Clash clears pool and unloads both gameplay and dialog bundles");
            source.Locked = true; clash.Init(0, false); source.Locked = false;
            Require(clash.IncrementScoreForItem(1) == 0 && clash.After == 0, "Score lock is captured for the level");
            source.MissingHelper = true; clash.AddExtra(17); failed = false;
            try { clash.Init(0, false, true); } catch (NullReferenceException) { failed = true; }
            Require(failed && clash.After == 17 && clash.ShouldConsume && clash.TotalLevelScoreForEvt == 17,
                "Missing helper retains restored count and total before late initialization failure");
            source.MissingHelper = false; input.Active.Difficulty = 1;
        }
        private sealed class CollectInputs : IOriginalLevelCollectState, IOriginalMissionSuperBooster
        {
            public bool ShouldCollectFromLevel { get; set; }
            public bool IsPlayingWithSuperBooster() => true;
        }
        private sealed class OwnedContext : IOriginalAnimationDataContext, IOriginalSessionBundleManager
        {
            public readonly EventInputs Events = new EventInputs();
            public readonly DailyInputs Daily = new DailyInputs();
            public readonly ClashInputs Clash = new ClashInputs();
            public readonly CollectInputs[] Collect = new CollectInputs[7];
            public readonly int[] CollectReads = new int[7];
            public readonly Inputs Input;
            public bool AllowResolve;
            public int Queries;
            public OwnedContext(Inputs input) { Input = input; for (int i = 0; i < Collect.Length; i++) Collect[i] = new CollectInputs(); }
            private void Query() { Require(AllowResolve, "Animation construction must not resolve application services"); Queries++; }
            private IOriginalLevelCollectState CollectState(int index) { Query(); CollectReads[index]++; return Collect[index]; }
            public IOriginalMadnessSessionManager GetMadness() { Query(); return Events; }
            public IOriginalMadnessSessionPools GetMadnessPools() { Query(); return Events; }
            public IOriginalPropellerRushProgress GetPropellerRush() { Query(); return Events; }
            public IOriginalRoyalPassSessionManager GetRoyalPass() { Query(); return Events; }
            public IOriginalPinataSessionManager GetPinataParty() { Query(); return Events; }
            public IOriginalSessionAbValues GetAbValues() { Query(); return Events; }
            public IOriginalDailyLightningRushSessionManager GetDailyLightningRush() { Query(); return Daily; }
            public IOriginalDailyLightningRushPools GetDailyLightningRushPools() { Query(); return Daily; }
            public IOriginalSessionBundleManager GetBundleManager() { Query(); return this; }
            public IOriginalSessionBundle GetBundle(string name)
            { return name == OriginalDailyLightningRushLevelData.GameplayBundleName ? Daily.GetBundle(name) : Clash.GetBundle(name); }
            public IOriginalClashSessionManager GetClashOfChampions() { Query(); return Clash; }
            public IOriginalClashSessionPools GetClashPools() { Query(); return Clash; }
            public IOriginalClashGameplayAsset LoadClashAsset(IOriginalSessionEventBundle bundle, string name) { Query(); return Clash.LoadAsset(bundle, name); }
            public IOriginalLevelCollectState GetWeeklyPassCollectState() => CollectState(0);
            public IOriginalLevelCollectState GetMagicCauldronCollectState() => CollectState(1);
            public IOriginalLevelCollectState GetDukesFortuneCollectState() => CollectState(2);
            public IOriginalLevelCollectState GetOceanOdysseyCollectState() => CollectState(3);
            public IOriginalLevelCollectState GetPuzzleBreakCollectState() => CollectState(4);
            public IOriginalLevelCollectState GetMergeSmithCollectState() => CollectState(5);
            public IOriginalLevelCollectState GetShortExpeditionCollectState() => CollectState(6);
            public IOriginalMissionSuperBooster GetMissionSuperBooster() { Query(); return Collect[0]; }
            public OriginalActiveLevelData GetActiveLevel() { Query(); return Input.Active; }
            public bool IsTableGameLevel() { Query(); return Input.Active.IsTableGame; }
            public int GetActiveAmountMultiplier() { Query(); return Input.Active.EventMultiplier; }
        }
        private sealed class SessionEnvironment : IOriginalUserSessionEnvironment
        {
            private readonly Inputs input;
            private Action paused;
            public int Subscribers, Scene = 2, CurrentQueries;
            public SessionEnvironment(Inputs input) { this.input = input; }
            public event Action OnApplicationPause
            { add { Subscribers++; paused += value; } remove { Subscribers--; paused -= value; } }
            public OriginalActiveLevelData GetCurrentActiveLevel() { CurrentQueries++; return input.Active; }
            public int GetCurrentScene() => Scene;
            public void Pause() => paused?.Invoke();
        }
        private static void VerifyOwnedAnimation(Whitebox.Foundation.ConfigData.FTiledLevel level)
        {
            var input = new Inputs(); var source = new OwnedContext(input);
            var slb = new OriginalSlbMoveConfig(() => input, () => throw new InvalidOperationException("Unexpected SLB lookup"));
            var environment = new SessionEnvironment(input);
            var session = new OriginalUserSessionData(source, slb, input, environment);
            var animation = session.AnimationData;
            input.Active = session.ActiveLevel; input.Active.Difficulty = 1;
            Require(ReferenceEquals(((IOriginalLevelLoadSession)session).ActiveLevel, input.Active)
                && ReferenceEquals(input.Active.AnimationValues, animation) && environment.Subscribers == 1
                && session.RoyalPassScrollPosition == -1f && session.GameSceneLoadCount == 0,
                "Actual user session owns the shared animation/active level and subscribes after construction");
            Require(ReferenceEquals(animation.StartSnapshots, animation) && source.Queries == 0,
                "Animation owns its original snapshots and defers all context queries");
            var entries = new object[] { animation.LeagueCrown, animation.KingsCup, animation.TeamBattle, animation.Coin,
                animation.CompletedNThousandLevelCoin, animation.SkyRace, animation.Madness, animation.RoyalPass, animation.WeeklyPass,
                animation.TeamTreasure, animation.SpaceMission, animation.BalloonRise, animation.HiddenTemple, animation.DragonNest,
                animation.MagicCauldron, animation.DailyLightningRush, animation.PropellerRush, animation.ArcheryArena, animation.PinataParty,
                animation.DukesFortune, animation.TeamTournament, animation.TrainJourney, animation.OceanOdyssey, animation.MissionEvents,
                animation.PuzzleBreak, animation.RobotRivals, animation.MergeSmith, animation.ClashOfChampions, animation.RoyalWorldCup,
                animation.ShortExpeditionEvent, animation.InviteFriends, animation.DukesTreats };
            var unique = new System.Collections.Generic.HashSet<object>(entries);
            Require(entries.Length == 32 && unique.Count == 32 && !unique.Contains(null)
                && animation.Boosters.Count == 0 && animation.UnlimitedBoosters.Count == 0 && animation.EventItems.Count == 0 && animation.Multipliers.Count == 0
                && !ReferenceEquals(animation.Boosters, animation.UnlimitedBoosters) && !ReferenceEquals(animation.Boosters, animation.EventItems)
                && !ReferenceEquals(animation.Boosters, animation.Multipliers) && !ReferenceEquals(animation.UnlimitedBoosters, animation.EventItems)
                && !ReferenceEquals(animation.UnlimitedBoosters, animation.Multipliers) && !ReferenceEquals(animation.EventItems, animation.Multipliers)
                && !animation.BoosterShouldConsume && !animation.CanPlayLevelWithMultiplier,
                "Thirty-two independent snapshots, four independent empty dictionaries and original flag defaults");
            var daily = (OriginalDailyLightningRushLevelData)animation.DailyLightningRush;
            var madness = (OriginalMadnessSessionData)animation.Madness;
            Require(daily.Step == -1 && madness.EventId == -1 && madness.ExploderIdMatchItemCount == null
                && ((OriginalRoyalPassLevelData)animation.RoyalPass).EventId == -1
                && ((OriginalPinataPartyLevelData)animation.PinataParty).EventId == -1,
                "Original derived constructor sentinels are retained");
            var collect = new OriginalCollectedLevelData[] { (OriginalWeeklyPassLevelData)animation.WeeklyPass,
                (OriginalMagicCauldronLevelData)animation.MagicCauldron, (OriginalDukesFortuneLevelData)animation.DukesFortune,
                (OriginalOceanOdysseyLevelData)animation.OceanOdyssey, (OriginalPuzzleBreakLevelData)animation.PuzzleBreak,
                (OriginalMergeSmithLevelData)animation.MergeSmith, (OriginalShortExpeditionEventLevelData)animation.ShortExpeditionEvent };
            for (int i = 0; i < collect.Length; i++) collect[i].SetShouldCollect(true);
            animation.MissionEvents.ShouldConsume = true; animation.MissionEvents.Init(17);
            animation.InviteFriends.SetEgoPrice(55);
            animation.Boosters.Add(4, 2);
            LevelLoadVerification.LoadFirstLevel(level, session, slb); source.AllowResolve = true;
            input.Active.Start(input);
            Require(madness.After == 43 && daily.After == 30 && source.Daily.Creates == 1 && source.Clash.Creates == 1
                && animation.PropellerRush.After == 30 && animation.BalloonRise.Before == 17 && animation.ArcheryArena.StreakIndex == 5
                && animation.MissionEvents.ShouldConsume && animation.MissionEvents.CreatedLightballCount == 0
                && !animation.InviteFriends.ShouldConsume && animation.InviteFriends.EgoPrice == 0,
                "One owned AnimationData follows actual Load -> Start with all concrete event snapshots");
            for (int i = 0; i < collect.Length; i++)
            {
                Require(collect[i].ShouldCollectFromLevel && collect[i].Before == 0 && collect[i].After == 0 && source.CollectReads[i] == 0,
                    "Inherited Init retains collect flag without manager queries");
                collect[i].Init(i == 0 ? -1 : 31 + i); collect[i].AddExtra(3);
                long saved = collect[i].GetSaveData(); collect[i].SetShouldCollect(false); collect[i].LoadSaveData(saved);
                Require(collect[i].ShouldCollectFromLevel && source.Collect[i].ShouldCollectFromLevel && source.CollectReads[i] == 1
                    && collect[i].Before == (i == 0 ? -1 : 31 + i) && collect[i].After == (i == 0 ? 2 : 34 + i),
                    "Each concrete save/restore synchronizes the correct feature owner");
            }
            animation.MissionEvents.AddExtraForItem(5, 2);
            Require(animation.MissionEvents.CreatedLightballCount == 2 && animation.MissionEvents.CreatedSuperLightballCount == 2,
                "Owned Mission forwards lightball count to the actual supplied super-booster service");
            long missionSaved = animation.MissionEvents.GetSaveData(); animation.MissionEvents.Consume(); animation.MissionEvents.LoadSaveData(missionSaved);
            Require(!animation.MissionEvents.ShouldConsume && animation.MissionEvents.CreatedSuperLightballCount == 2, "Mission packed restore retains consume flag");
            input.Active.Restart(input);
            Require(daily.After == 30 && source.Daily.Creates == 2 && source.Daily.Clears == 1 && source.Clash.Creates == 2 && animation.MissionEvents.CreatedLightballCount == 0
                && animation.Boosters[4] == 2, "Restart reuses snapshots, calls same-step daily create without clearing and retains pending booster dictionary");
            for (int i = 0; i < collect.Length; i++) Require(collect[i].ShouldCollectFromLevel && source.CollectReads[i] == 1, "Restart does not erase or resolve collect flags");
            source.AllowResolve = false;
            var secondSession = new OriginalUserSessionData(source, slb, input, environment);
            var second = secondSession.AnimationData;
            Require(!ReferenceEquals(animation.Coin, second.Coin) && !ReferenceEquals(animation.Madness, second.Madness)
                && second.Boosters.Count == 0 && !ReferenceEquals(animation.Boosters, second.Boosters)
                && !ReferenceEquals(session.ActiveLevel, secondSession.ActiveLevel) && environment.Subscribers == 2,
                "Separate user sessions retain subscriptions and share no owned snapshots/dictionaries/active data");
            VerifySessionReset(session, secondSession, source, environment, input);
        }
        private static void VerifySessionReset(OriginalUserSessionData session, OriginalUserSessionData other,
            OwnedContext source, SessionEnvironment environment, Inputs input)
        {
            var animation = session.AnimationData; var active = session.ActiveLevel; var prelevel = session.PrelevelSelection;
            prelevel.Toggle(1); prelevel.ToggleForTime(2, true); prelevel.SetNoPossibleMoveFailed(3);
            prelevel.DidTouchRocket = true; prelevel.DidTouchTnt = true; prelevel.DidTouchLightball = true; prelevel.SetLevelBoosterUsages();
            Require(prelevel.LevelRocketBoosterSource == 1 && prelevel.LevelTntBoosterSource == -1 && prelevel.LevelLightballBoosterSource == -1,
                "Original prelevel selected/timed/retry usage sources");
            animation.Coin.Init(700); animation.Coin.AddExtra(50);
            animation.UnlimitedBoosters[1] = 3; animation.EventItems[2] = 4; animation.Multipliers[3] = 5;
            animation.BoosterShouldConsume = true; animation.CanPlayLevelWithMultiplier = true;
            active.WonAnyLevel = true; session.RoyalPassScrollPosition = 33f; session.SessionTagData.SentKingsCupTag = 7;
            int queries = source.Queries, clears = source.Daily.Clears, unloads = source.Daily.Unloads;
            var bundle = ((OriginalDailyLightningRushLevelData)animation.DailyLightningRush).GameplayBundle;
            session.Reset();
            Require(active.State == 0 && !active.WonAnyLevel && active.MoveCount == 30 && session.RoyalPassScrollPosition == -1f
                && session.SessionTagData.SentKingsCupTag == 7 && animation.Coin.Before == 700 && animation.Coin.After == 750
                && !animation.Coin.ShouldConsume && !animation.BoosterShouldConsume && !animation.CanPlayLevelWithMultiplier
                && animation.Boosters.Count == 0 && animation.UnlimitedBoosters.Count == 0 && animation.EventItems.Count == 0 && animation.Multipliers.Count == 0,
                "Session reset retains progress and tags while clearing state, cumulative win, consumption flags and dictionaries");
            Require(!prelevel.ShouldUseRocket && !prelevel.ShouldUseTnt && !prelevel.ShouldUseLightBall
                && !prelevel.DidTouchRocket && !prelevel.DidTouchTnt && !prelevel.DidTouchLightball
                && prelevel.LevelRocketBoosterSource == 1 && prelevel.LevelTntBoosterSource == -1 && prelevel.LevelLightballBoosterSource == -1,
                "Prelevel reset clears twelve flags while retaining source integers");
            Require(source.Queries == queries && source.Daily.Clears == clears && source.Daily.Unloads == unloads
                && ReferenceEquals(((OriginalDailyLightningRushLevelData)animation.DailyLightningRush).GameplayBundle, bundle),
                "Reset performs no manager queries, pool work or bundle unload");
            input.Active = active; active.Number = 1; session.ResetOnSceneChange(2);
            Require(!session.IsGameSceneLoadedOnce && session.GameSceneLoadCount == 0, "Level1 Home transition is excluded");
            input.Active = other.ActiveLevel; other.ActiveLevel.Number = 2;
            for (int i = 0; i < 7; i++) session.ResetOnSceneChange(2);
            Require(active.Number == 1 && session.IsGameSceneLoadedOnce && session.IsHomeSceneLoadedMoreThanOnce && session.GameSceneLoadCount == 5,
                "Scene callback reads global current session and saturates original history at5");
            environment.Scene = 2; environment.Pause(); Require(session.IsGameSceneLoadedOnce, "Home pause retains game-loaded flag");
            environment.Scene = 3; environment.Pause();
            Require(!session.IsGameSceneLoadedOnce && session.IsHomeSceneLoadedMoreThanOnce && session.GameSceneLoadCount == 5,
                "Pause outside Home clears only game-loaded flag");
            session.Reset(); Require(session.IsHomeSceneLoadedMoreThanOnce && session.GameSceneLoadCount == 5 && environment.Subscribers == 2,
                "Session reset retains history and original pause subscriptions");
            File.WriteAllText(Path.Combine(Application.dataPath, "../user_session_unity_verification.json"), JsonUtility.ToJson(new SessionReport {
                passed = true, owned_snapshots = 32, owned_dictionaries = 4, pause_subscribers = environment.Subscribers,
                scene_history_count = session.GameSceneLoadCount, current_session_queries = environment.CurrentQueries,
                scope = "Actual UserSessionData constructs shared concrete AnimationData/ActiveLevel/prelevel/tag data; actual LevelManager.LevelLoad consumes this session through its typed contract before all concrete snapshots run through Start/Restart and actual session/animation/prelevel resets. Checks progress/tag/source/history retention, four dictionary clears, no manager or pool/bundle activity during reset, global-current versus receiver session scene reads, level1 Home2 exclusion, history cap5, pause flag semantics and retained subscriptions across two independent sessions. Explicit manager/scene/pause/resource transport inputs remain diagnostics. Active/prelevel full save restore, application/user/database composition and formal player input/HUD remain pending. SDK unchanged."
            }, true) + "\n");
        }
        [Serializable] private sealed class SessionReport
        {
            public bool passed;
            public int owned_snapshots, owned_dictionaries, pause_subscribers, scene_history_count, current_session_queries;
            public string scope;
        }
        // Reuse these explicit event inputs when testing the GameManager entry
        // owner with a real active-level Start implementation. Not a bootstrap.
        internal static IOriginalGameStartSession CreateGameEntrySession(out OriginalActiveLevelData active)
        {
            var level = OriginalLevelReader.Load("Levels/level_1");
            var input = new Inputs();
            var snapshots = Snapshots(input, new EventBoundary(), new EventBoundary(), new EventBoundary(), new EventBoundary());
            var animation = new OriginalActiveLevelAnimationValues(new OriginalBeforeAfterData(), new OriginalBeforeAfterData(), snapshots);
            var slb = new OriginalSlbMoveConfig(() => input, () => throw new InvalidOperationException("Unexpected SLB lookup"));
            active = new OriginalActiveLevelData(slb, input, animation) { Difficulty = 1 };
            input.Active = active;
            active.Load(1, level.Name, level.Move);
            return new OriginalActiveLevelGameStart(active, input);
        }
        [MenuItem("Recovery/Verify Active Level Start")]
        public static void Run()
        {
            var level = OriginalLevelReader.Load("Levels/level_1"); var input = new Inputs();
            var madness = new EventBoundary(); var royal = new EventBoundary(); var pinata = new EventBoundary(); var clash = new EventBoundary();
            var snapshots = Snapshots(input, madness, royal, pinata, clash);
            var animation = new OriginalActiveLevelAnimationValues(new OriginalBeforeAfterData(), new OriginalBeforeAfterData(), snapshots);
            var slb = new OriginalSlbMoveConfig(() => input, () => throw new InvalidOperationException("Unexpected SLB lookup"));
            var active = new OriginalActiveLevelData(slb, input, animation) { Difficulty = 1 }; input.Active = active;
            active.Load(1, level.Name, level.Move); active.WonAnyLevel = true; active.EgoPurchaseCount = 7; active.ShiftCount = 88;
            active.FalseSwipeCount = 4; active.UsedHintCount = 6; active.UselessMoveCount = 5; active.IsUsefulMoveMade = false;
            active.SpecialItemCounts.SwappedItemCount = 9; active.MatchItemCounts.IncrementCount(1, 7);
            active.RemainingItemsSnapshots.Add(new OriginalRemainingItemSnapshot()); snapshots.MissionEvents.ShouldConsume = true;
            snapshots.MissionEvents.Init(5); snapshots.InviteFriends.ShouldConsume = true; snapshots.InviteFriends.EgoPrice = 55;
            active.Start(input);
            Require(active.State == 2 && active.Name == level.Name && active.MoveCount == 30 && active.OriginalMoveCount == 30 && !active.WonAnyLevel,
                "Native Load then Start retains the adjusted moves and clears cumulative win");
            Require(input.ClocheQueries == 2 && input.SavedCloche == 2 && active.Cloche.Before == 2 && active.MagicDuke.After == 9,
                "Independent cloche resolves, progress persistence and magic duke snapshot");
            Require(animation.CanPlayLevelWithMultiplier && active.IsMultipliedInLevel && active.EventMultiplier == 3,
                "Multiplier availability and selected multiplier are separate ordered decisions");
            Require(input.InventoryReads == 4 && input.InventoryTypes[0] == 4 && input.InventoryTypes[1] == 5 && input.InventoryTypes[2] == 6 && input.InventoryTypes[3] == 7
                && active.HasUnlimitedHammer && !active.HasUnlimitedArrow && !active.HasUnlimitedCannon && active.HasUnlimitedJester && !active.DidUseUnlimitedHammer,
                "Four native in-game booster time queries and use flag reset");
            Require(active.LevelStartTime == input.StartTime && input.StartTime <= Time.realtimeSinceStartup && active.EgoPurchaseCount == 0 && active.ShiftCount == 0
                && active.FalseSwipeCount == 0 && active.UsedHintCount == 0 && active.IsUsefulMoveMade && active.UselessMoveCount == 0
                && active.MatchItemCounts.GetCount(1) == 0 && active.SpecialItemCounts.SwappedItemCount == 0 && active.RemainingItemsSnapshots.Count == 0,
                "Real Unity clock and native startup counter/snapshot resets");
            Require(madness.Calls == 1 && !madness.Restart && madness.Difficulty == 1 && !royal.Restart && !pinata.Restart && clash.Multiplied && !clash.Restored,
                "Required event initialization receives first-start arguments");
            Require(snapshots.PropellerRush.ShouldConsume && snapshots.PropellerRush.After == 30 && snapshots.BalloonRise.Before == 17
                && snapshots.ArcheryArena.StreakIndex == 5 && snapshots.ArcheryArena.RankIndex == 4 && snapshots.ArcheryArena.IconWasVisibleAtHome
                && snapshots.MissionEvents.ShouldConsume && snapshots.MissionEvents.CreatedRocketCount == 0 && !snapshots.InviteFriends.ShouldConsume && snapshots.InviteFriends.EgoPrice == 0,
                "Native propeller difficulty table, balloon/archery capture and different mission/invite reset semantics");
            active.WonAnyLevel = true; active.Restart(input);
            Require(active.WonAnyLevel && madness.Restart && royal.Restart && pinata.Restart && input.PreferenceWrites == 0,
                "Restart retains cumulative win and normal levels do not update story attempts");
            active.StoryLevel = -1; input.CanSelect = false; active.Start(input);
            Require(!animation.CanPlayLevelWithMultiplier && active.EventMultiplier == 2 && input.PreferenceCount == int.MinValue && input.PreferenceWrites == 1,
                "Story attempt overflow, restricted multiplier flag and native legacy multiplier branch");
            input.MissingCloche = true; active.EgoPurchaseCount = 7; active.FalseSwipeCount = 5; active.WonAnyLevel = true;
            bool failed = false; try { active.Restart(input); } catch (NullReferenceException) { failed = true; }
            Require(failed && active.State == 2 && active.EgoPurchaseCount == 0 && active.FalseSwipeCount == 5 && active.WonAnyLevel,
                "Missing start dependency follows ResetValues and initial ego reset while retaining later fields");
            var restored = new OriginalArcheryLevelData(); restored.LoadSaveData(snapshots.ArcheryArena.GetSaveData());
            Require(restored.StreakIndex == 5 && restored.RankIndex == 4 && restored.IconWasVisibleAtHome, "Native archery snapshot bit layout");
            VerifyDailyLightning(input, snapshots);
            VerifyClash(input, snapshots);
            VerifyConcreteEvents(input, snapshots);
            VerifyOwnedAnimation(level);
            string project = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllText(Path.Combine(project, "active_level_start_unity_verification.json"), JsonUtility.ToJson(new Result {
                passed = true, first_level_moves = 30, start_restart_order_checked = true, required_dependency_failure_checked = true,
                scope = "Actual first-level JSON into native active Load then Start/Restart/SetStartValues and actual ResetValues. Uses Unity realtime clock, original constructor/counters and concrete propeller/archery data. Checks first versus restart win state, four booster-time queries, multiplier branches, story attempt overflow, event arguments, snapshot reset differences and missing-cloche partial state. Runs concrete Madness/RoyalPass/Pinata/DukesTreats through active Start/Restart: event identity changes, AB 224/225 conversion/tutorial/final reward branches, Madness initial difficulty bonus, signed snapshot restore, per-exploder dictionary recreation and missing-pool partial writes. Also runs concrete DailyLightningRush through actual active Start/Restart: successful/undownloaded/failed bundle branches, changed-step pool lifecycle, initial difficulty count, signed restore retaining counters/step while recovering active difficulty, inactive recycle/clear/unload(false), and missing-pool partial writes. Concrete Clash also runs through active Start/Restart: exact resource keys and pool ownership, native item fallback and zero-score combo local updates, original difficulty/multiplier, round-reset total retention, score callback failure before counters, unsigned restore and live active-level recovery, undownloaded ownership retention, inactive two-bundle unload(true), captured lock and missing-helper partial writes. Additionally constructs owned AnimationData with 32 distinct concrete snapshots and four separate empty dictionaries without any feature lookup, then runs all concrete snapshots together through Load/Start/Restart. Checks derived sentinels, inherited collect-flag retention, seven correctly bound restore owners, Mission super-booster/save and Invite reset semantics, same-step daily pool create without clearing and independent AnimationData objects. Actual manager lifecycle/progress/inventory/PlayerPrefs/MultiplierUtil/bundle/pool implementations remain explicit required diagnostic boundaries. No fixture installed in production. Full application composition and player-driven resolution/fall/refill/HUD/settlement remain pending; SDK unchanged."
            }, true) + "\n");
            Debug.Log("Native active-level Start/Restart/SetStartValues Unity verification passed.");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Result
        { public bool passed, start_restart_order_checked, required_dependency_failure_checked; public int first_level_moves; public string scope; }
    }
}
