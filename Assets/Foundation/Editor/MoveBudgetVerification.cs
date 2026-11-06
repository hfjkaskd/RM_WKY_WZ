using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Diagnostic fixture only. No application/session defaults are installed by
    // this verification; the formal scene still awaits session startup composition.
    public static class MoveBudgetVerification
    {
        private sealed class AbValues : IOriginalSlbAbValues, IOriginalOneMoveRemovalAb,
            IOriginalThreeColorAbValues, IOriginalThreeColorNameContext,
            IOriginalThreeColorUserProgress, IOriginalThreeColorMoveContext
        {
            public int Group;
            public int AbGroup => Group;
            public void SetAbValues() { }
            public bool IsOneMoveRemoveAbEnabled() => false; // Actual 0x32be678.
            public int GetThreeColorLevelsAbGroup() => 0; // Explicit diagnostic AB input.
            public bool IsCurrentLevelThreeColor => false;
            public int GameType => 0;
            public bool IgnoreMoveConfig => false;
        }
        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        public static OriginalFreshLevelMoves FirstLevelMoves()
        {
            var level = OriginalLevelReader.Load("Levels/level_1");
            Require(level.Name == "00001v02_L" && level.Move == 30, "Original first-level fixture");
            var ab = new AbValues();
            var slb = new OriginalSlbMoveConfig(() => ab, ReadSnapshot);
            var config = ThreeColorMoveVerification.CreateSnapshotConfig(() => ab, () => ab);
            var active = CreateActive(slb, ab, config);
            var duke = new OriginalSuperDukeMoveRemoval(() => ab);
            var adjustment = new OriginalMoveBudgetAdjustment(() => duke, () => active, slb);
            LevelLoadVerification.LoadFirstLevel(level, active, config, ab, ab, adjustment);
            Require(active.MoveCount == 30 && active.OriginalMoveCount == 30
                && OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount == 0, "Native first-level baseline budget");
            return new OriginalFreshLevelMoves(() => active);
        }
        private static OriginalActiveLevelData CreateActive(OriginalSlbMoveConfig slb, AbValues ab, OriginalThreeColorMoveConfig config = null)
        {
            OriginalActiveLevelData active = null;
            if (config == null) config = ThreeColorMoveVerification.CreateSnapshotConfig(() => ab, () => ab);
            var names = new OriginalThreeColorLevelNames(() => active, () => ab, () => ab, () => ab, config);
            active = new OriginalActiveLevelData(slb, names);
            return active;
        }
        private static OriginalSlbMoveConfigData ReadSnapshot()
        {
            return JsonUtility.FromJson<OriginalSlbMoveConfigData>(File.ReadAllText(Path.Combine(ProjectPath, "SourceCatalog/slb_move_config_snapshot.json")));
        }
        [MenuItem("Recovery/Verify Move Budget")]
        public static void Run()
        {
            var ab = new AbValues(); int reads = 0;
            var slb = new OriginalSlbMoveConfig(() => ab, () => { reads++; return ReadSnapshot(); });
            int[] groups = { 3, 4, 6, 7, 8 }, counts = { 46, 111, 84, 53, 12 };
            int[] levels = { 162, 75, 111, 150, 212 }, adjustments = { 2, 3, 1, 1, 1 };
            for (int i = 0; i < groups.Length; i++)
            {
                ab.Group = groups[i];
                Require(slb.GetCachedData(groups[i]).Count == counts[i], "Recovered cached group entry count");
                Require(slb.TryGetEntry(levels[i], out var entry) && entry.Version == 2
                    && entry.MoveAdjustment == adjustments[i], "Recovered group lookup through actual JsonUtility");
                Require(!slb.TryGetEntry(1, out _) && slb.GetMoveAdjustment(231) == 0, "Native first-level/upper range exclusion");
            }
            Require(reads == 1, "Shared five-group cache reads storage once");
            string[] extensions = { "O", "P", "R", "S", "T" };
            var active = CreateActive(slb, ab);
            for (int i = 0; i < groups.Length; i++)
            {
                ab.Group = groups[i]; active.Load(levels[i], "fixturev99_L", 30);
                Require(active.Name == "fixturev02_" + extensions[i] && active.State == 1,
                    "Actual active-level Load uses recovered version/extension");
            }
            ab.Group = 3;
            Require(slb.TryGetLevelNameOverride(160, out int fallbackVersion, out string fallbackExtension)
                && fallbackVersion == 1 && fallbackExtension == "O", "In-range missing entry still overrides with version one");
            active.Load(162, "fixturev99_L", 30, true);
            Require(active.Name == "fixturev02_O" && active.IsAdmin, "Name selection precedes new admin flag");
            active.Load(163, "other_L", 31, false, true);
            Require(active.Name == "other_L" && !active.IsAdmin && active.IsOnline, "Previous admin flag bypasses name rewrite");
            active.Reset();
            Require(active.State == 0 && active.Name == "other_L" && active.MoveCount == 31
                && active.OriginalMoveCount == 31, "Reset retains loaded data");
            var fresh = FirstLevelMoves();
            Require(fresh.GetMaxMoves() == 30 && fresh.GetLeftMoves() == 30 && fresh.GetMadeMoves() == 0, "Actual fresh provider getters");
            File.WriteAllText(Path.Combine(ProjectPath, "move_budget_unity_verification.json"), JsonUtility.ToJson(new Result
            {
                passed = true, recovered_entries = 306, cache_reads = reads, actual_active_load_checked = true, actual_level_manager_load_checked = true,
                scope = "Actual Unity JsonUtility reads the recovered October 8 SLB snapshot, all five cache counts, lookups/range exclusions and active Load name overrides including missing-entry version one, old admin flag and Reset data retention. First-level fixture now runs full native LevelManager.LevelLoad routing through actual episode strategy, welcome-back move calculation, active Load/name selection, SuperDuke/SLB adjustment and fresh getters before MoveManager initialization. Three-color naming binds its concrete config cache. AB/progress, resource/parser transport and storage remain explicit fixture data. Application providers, full constructor/session Start/Restart/save and formal playable context remain pending."
            }, true) + "\n");
            Debug.Log("Move budget Unity snapshot and fresh-provider verification passed.");
        }
        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
        [Serializable]
        private sealed class Result
        {
            public bool passed;
            public bool actual_active_load_checked;
            public bool actual_level_manager_load_checked;
            public int recovered_entries, cache_reads;
            public string scope;
        }
    }
}
