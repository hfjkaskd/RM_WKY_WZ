using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Scoped diagnostic providers. These are never installed into the scene or
    // used as substitutes for the application/event initialization services.
    public static class ActiveLevelInitializationVerification
    {
        private sealed class Inputs : IOriginalActiveLevelResetContext, IOriginalSessionProgress,
            IOriginalSessionIntPreferences, IOriginalThreeColorLevelNames, IOriginalSlbAbValues, IOriginalKingRelief
        {
            public OriginalActiveLevelData Active;
            public int Count = int.MaxValue, Writes, KingCalls;
            public bool FailDuke, FailSave, UsefulAtKing;
            public OriginalLevelStartInventory GetInventory() => new OriginalLevelStartInventory { Star = 7, Coin = 1001 };
            public IOriginalSessionProgress GetSuperBooster() => this;
            public IOriginalSessionProgress GetSuperDuke() => FailDuke ? null : this;
            public int CurrentProgress => 9;
            public IOriginalSessionIntPreferences Preferences => this;
            public int GetInt(string key) { Require(key == "SLTC", "Original story try key"); return Count; }
            public void SetInt(string key, int value)
            { Require(key == "SLTC", "Original story try key"); if (FailSave) throw new InvalidOperationException("Diagnostic storage failure"); Count = value; Writes++; }
            public bool ShouldReportThreeColorExtension(int a, int b) => false;
            public bool TryGetVersionOverride(int a, out int b) { b = 0; throw new InvalidOperationException("Unexpected version query"); }
            public string GetLevelExtension() => throw new InvalidOperationException("Unexpected extension query");
            public int GameType => 0;
            public int AbGroup => 0;
            public void SetAbValues() { }
            public void TryKingRelief() { UsefulAtKing = Active.IsUsefulMoveMade; KingCalls++; }
        }
        [MenuItem("Recovery/Verify Active Level Initialization")]
        public static void Run()
        {
            var level = OriginalLevelReader.Load("Levels/level_1");
            var input = new Inputs();
            var slb = new OriginalSlbMoveConfig(() => input, () => throw new InvalidOperationException("Unexpected SLB config lookup"));
            var coin = new OriginalBeforeAfterData(); var extra = new OriginalBeforeAfterData();
            var active = new OriginalActiveLevelData(slb, input, new OriginalActiveLevelAnimationValues(coin, extra)); input.Active = active;
            Require(active.EventMultiplier == 1 && active.IsUsefulMoveMade && active.RemainingItemsSnapshots.Count == 0
                && !ReferenceEquals(active.Star, active.Cloche) && !ReferenceEquals(active.SuperBooster, active.SuperDuke), "Native constructor ownership and defaults");
            active.Load(1, level.Name, level.Move);
            active.Difficulty = 3; active.StoryLevel = -1; active.WonAnyLevel = true; active.ShiftCount = 88;
            active.RemainingItemsSnapshots.Add(new OriginalRemainingItemSnapshot());
            active.RestartData.Purchases[4] = 3; active.RestartData.Refunds[5] = 2;
            active.RestartData.OnStartedWithUnlimitedPrelevel(1); active.RestartData.SetIsInGameBoosterAnimationActive(4, true);
            active.MatchItemCounts.IncrementCount(1, 11); active.SpecialItemCounts.SwappedItemCount = 6;
            var oldSnapshots = active.RemainingItemsSnapshots.GetEnumerator();
            active.ResetValues(input);
            Require(active.State == 2 && active.MoveCount == 30 && active.OriginalMoveCount == 30 && active.WonAnyLevel,
                "ResetValues advances only active-session state and retains loaded moves/cumulative win");
            Require(active.Star.Before == 7 && active.Star.After == 7 && coin.Before == 1001 && extra.After == 0
                && active.SuperBooster.After == 9 && active.SuperDuke.After == 9, "Inventory and manager snapshots");
            Require(input.Count == int.MinValue && input.Writes == 1 && active.ShiftCount == 0 && active.EnteredLevelDifficulty == 3
                && active.RemainingItemsSnapshots.Count == 0 && active.RestartData.Purchases.Count == 0 && active.RestartData.Refunds.Count == 0
                && !active.RestartData.HadUnlimitedPrelevel(1) && !active.RestartData.IsInGameBoosterAnimationActive(4), "Original counter overflow, restart and snapshot clear");
            bool invalidated = false; try { oldSnapshots.MoveNext(); } catch (InvalidOperationException) { invalidated = true; }
            Require(invalidated && active.MatchItemCounts.GetCount(1) == 11 && active.SpecialItemCounts.SwappedItemCount == 6,
                "Snapshot list version changes; match and special statistics await SetStartValues");
            var useful = new OriginalUsefulMoveState(() => input, active); useful.ResetIsUsefulMoveMade(); useful.IncreaseUselessMoveCount(); useful.UsefulMoveMade();
            Require(active.IsUsefulMoveMade && active.UselessMoveCount == 1 && active.MaxUselessMoveStreak == 1
                && input.KingCalls == 1 && !input.UsefulAtKing, "Useful move services share the actual active session");
            active.State = 5; active.ShiftCount = 66; active.EnteredLevelDifficulty = -8;
            active.RestartData.Purchases[4] = 1; active.RemainingItemsSnapshots.Add(new OriginalRemainingItemSnapshot()); input.FailDuke = true;
            bool failed = false; try { active.ResetValues(input); } catch (NullReferenceException) { failed = true; }
            Require(failed && active.State == 5 && active.ShiftCount == 66 && active.RestartData.Purchases.Count == 1
                && active.RemainingItemsSnapshots.Count == 1 && active.Star.After == 7, "Missing manager preserves preceding writes and later state");
            input.FailDuke = false; input.FailSave = true; failed = false;
            try { active.ResetValues(input); } catch (InvalidOperationException) { failed = true; }
            Require(failed && active.State == 2 && active.RestartData.Purchases.Count == 0 && active.ShiftCount == 66
                && active.EnteredLevelDifficulty == -8 && active.RemainingItemsSnapshots.Count == 1, "Storage failure follows state/restart reset, before shift/snapshot clear");
            input.FailSave = false; active.StoryLevel = 0; int writes = input.Writes; active.ResetValues(input);
            Require(input.Writes == writes, "Non-story levels do not increment story attempts");
            active.ResetCumulativeProgress(); Require(!active.WonAnyLevel, "Explicit cumulative reset");
            var value = new OriginalBeforeAfterData(); value.Init(7); value.AddExtra(3); value.Update(2);
            var restored = new OriginalBeforeAfterData(); restored.LoadSaveData(value.GetSaveData());
            Require(restored.ShouldConsume && restored.Before == 9 && restored.After == 12 && restored.GetCollectedAmount() == 3, "Native packed before/after data");
            var counts = new OriginalSpecialItemCount { CreationCount = 8192, AutoCreationCount = -1, UseCount = 4096, AutoTriggerCount = 7, LightballCreationCount = 8 };
            var loaded = new OriginalSpecialItemCount(); loaded.LoadSaveData(counts.GetSaveData());
            Require(loaded.CreationCount == 8191 && loaded.AutoCreationCount == 8191 && loaded.UseCount == 4095
                && loaded.AutoTriggerCount == 7 && loaded.LightballCreationCount == 8, "Native special count bit lengths and upper-only clamp");
            string project = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllText(Path.Combine(project, "active_level_initialization_unity_verification.json"), JsonUtility.ToJson(new Result
            {
                passed = true, first_level_loaded_moves = 30, shared_useful_counters_checked = true, partial_failure_writes_checked = true,
                scope = "Actual first-level JSON loaded into native active data constructor/Load/ResetValues with fixture inventory, manager progress, PlayerPrefs transport and King callback. Validates owned snapshot defaults, unchanged loaded moves, restart flags/counts, story attempt overflow, collection invalidation, retained statistics, missing-manager and storage-failure partial writes, shared useful counters and native snapshot packing. No fixture installed in production. SetStartValues/full AnimationData/events/Start/Restart/save/application composition and complete playable resolution remain pending. SDK unchanged."
            }, true) + "\n");
            Debug.Log("Native active-level constructor, ResetValues, shared counters and packed snapshots Unity verification passed.");
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Result
        { public bool passed, shared_useful_counters_checked, partial_failure_writes_checked; public int first_level_loaded_moves; public string scope; }
    }
}
