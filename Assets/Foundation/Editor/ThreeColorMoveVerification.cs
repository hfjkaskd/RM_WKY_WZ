using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Explicit storage/AB/league fixture; never installed in the application scene.
    public static class ThreeColorMoveVerification
    {
        private sealed class Inputs : IOriginalThreeColorMoveContext, IOriginalThreeColorAbValues,
            IOriginalThreeColorNameContext, IOriginalThreeColorUserProgress, IOriginalSlbAbValues
        {
            public int Group = 2;
            public bool Ignore;
            public int GetThreeColorLevelsAbGroup() => Group;
            public bool IgnoreMoveConfig => Ignore;
            public bool IsCurrentLevelThreeColor => true;
            public int GameType => 0;
            public int AbGroup => 0;
            public void SetAbValues() { }
        }
        private sealed class Storage : IOriginalThreeColorConfigStorage
        {
            public string Config;
            public int Version, Reads;
            public string GetString(string key) { Require(key == OriginalThreeColorMoveConfig.ConfigKey, "Config key"); Reads++; return Config; }
            public int GetInt(string key) { Require(key == OriginalThreeColorMoveConfig.VersionKey, "Version key"); return Version; }
            public void SetString(string key, string value) { Require(key == OriginalThreeColorMoveConfig.ConfigKey, "Config write key"); Config = value; }
            public void SetInt(string key, int value) { Require(key == OriginalThreeColorMoveConfig.VersionKey, "Version write key"); Version = value; }
            public void DeleteKey(string key)
            { if (key == OriginalThreeColorMoveConfig.ConfigKey) Config = null; else { Require(key == OriginalThreeColorMoveConfig.VersionKey, "Version delete key"); Version = 0; } }
        }
        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        private static string Snapshot() => File.ReadAllText(Path.Combine(ProjectPath, "SourceCatalog/three_color_move_config_snapshot.json"));
        public static OriginalThreeColorMoveConfig CreateSnapshotConfig(Func<IOriginalThreeColorMoveContext> manager, Func<IOriginalThreeColorAbValues> ab)
        { return new OriginalThreeColorMoveConfig(manager, ab, new Storage { Config = Snapshot() }, JsonUtility.FromJson<OriginalThreeColorMoveConfigData>); }

        [MenuItem("Recovery/Verify Three Color Moves")]
        public static void Run()
        {
            var input = new Inputs(); var storage = new Storage { Config = Snapshot(), Version = 1 };
            var config = new OriginalThreeColorMoveConfig(() => input, () => input, storage, JsonUtility.FromJson<OriginalThreeColorMoveConfigData>);
            Require(config.ConfigVersion == 1, "Recovered config version fixture");
            for (int i = 2; i <= 5; i++) Require(config.GetCachedData(i).Count == 0, "Recovered empty b/missing c/d/e cache");
            Require(storage.Reads == 1 && !config.TryGetVersion(1, out _) && config.GetMinMoveCount(10) == 10, "Single snapshot read and native empty defaults");
            const string update = "{\"moves\":\"17\",\"b\":[\"1v2\",\"-9\",\"1v7\",\"-12\"],\"c\":[\"1v3\",\"2\"],\"d\":[\"1v4\",\"-3\"],\"e\":[\"1v5\",\"-4\"]}";
            config.TryUpdateConfig(2, update);
            Require(storage.Config == update && storage.Version == 2 && storage.Reads == 1, "Actual JsonUtility update and shared warm cache");
            OriginalActiveLevelData active = null;
            var slb = new OriginalSlbMoveConfig(() => input, () => null);
            var names = new OriginalThreeColorLevelNames(() => active, () => input, () => input, () => input, config);
            active = new OriginalActiveLevelData(slb, names);
            var moves = new OriginalThreeColorMoves(config, () => input, () => active, () => 0, _ => -3, _ => 12);
            string[] extensions = { "U", "V", "W", "X" };
            int[] budgets = { 15, 20, 18, 17 };
            for (int i = 0; i < extensions.Length; i++)
            {
                input.Group = i + 2;
                int adjusted = moves.GetAdjustedMoveCount(1, 30, false, false, false);
                active.Load(1, "00001v02_L", adjusted);
                Require(adjusted == budgets[i] && active.Name == "00001v0" + (i == 0 ? 7 : i + 2) + "_" + extensions[i]
                    && active.MoveCount == adjusted && active.OriginalMoveCount == adjusted, "Actual adjusted budget and active Load/name integration");
            }
            input.Group = 2;
            Require(moves.GetReducedMoveCount(31, false, false, false) == 16, "Native ceiling scale");
            Require(moves.ApplyMoveConfigAdjustment(1, 30) == 18 && moves.ApplyMoveConfigAdjustment(1, 16) == 16, "Configured negative adjustment lower bound");
            active.LeagueIndex = 1;
            Require(moves.GetAdjustedMoveCount(1, 30, false, false, false) == 12, "Native final league minimum");
            input.Ignore = true;
            Require(!config.TryGetVersion(1, out _) && config.GetMinMoveCount(10) == 10, "Ignore config flag"); input.Ignore = false;
            config.InvalidatePersistedConfig();
            Require(storage.Config == null && storage.Version == 0 && config.TryGetVersion(1, out int version) && version == 7, "Invalidation retains memory cache");
            var invalidStorage = new Storage { Config = "{\"moves\":\"17\"}", Version = 9 };
            var invalid = new OriginalThreeColorMoveConfig(() => input, () => input, invalidStorage, JsonUtility.FromJson<OriginalThreeColorMoveConfigData>);
            Require(invalid.ConfigVersion == 0 && invalidStorage.Config == null && invalid.GetMinMoveCount(10) == 10, "Invalid persisted config removes both keys");
            File.WriteAllText(Path.Combine(ProjectPath, "three_color_moves_unity_verification.json"), JsonUtility.ToJson(new Result
            {
                passed = true, snapshot_reads = storage.Reads, active_load_groups = 4,
                scope = "Actual Unity JsonUtility, recovered b-empty snapshot, four shared caches, native config update/version storage key ordering, concrete version-entry/name selection, four group move budgets feeding active Load, float ceiling, negative lower bounds, league final clamp with explicit offset/minimum fixture, ignore and invalidation cache retention. The physical formal scene remains NoGame; application AppKeyValue/AB/league data, episode/welcome-back, constructor/session startup and resolution are not replaced by these fixtures."
            }, true) + "\n");
            Debug.Log("Three color move config, adjusted budget and active Load Unity verification passed.");
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Result { public bool passed; public int snapshot_reads, active_load_groups; public string scope; }
    }
}
