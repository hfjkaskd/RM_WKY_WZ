using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation.Editor
{
    // Diagnostic input data and transport only. No fixture is installed in the scene.
    public static class LevelLoadVerification
    {
        private sealed class Parser : IOriginalLevelLoadParser
        {
            private readonly FTiledLevel level;
            public Parser(FTiledLevel level) { this.level = level; }
            public int MoveCount => level.Move;
            public string LevelName => level.Name;
            public bool ShouldForceReloadLevel() => false; // Explicit parser transport input.
        }
        private sealed class Progress : IOriginalWelcomeBackGiftProgress
        {
            public int TierValue, Start, Date = 42, Writes;
            public bool Reward = true;
            public int Tier => TierValue;
            public int StartLevel => Start;
            public void SetStartLevel(int value) { Start = value; Writes++; }
            public void SetTier(int value) { TierValue = value; Writes++; }
            public void SetOfflineGiftDeservedDate(int value) { Date = value; Writes++; }
            public void SetShouldGiveReward(bool value) { Reward = value; Writes++; }
        }
        private sealed class Inputs : IOriginalThreeColorAbValues, IOriginalThreeColorNameContext,
            IOriginalThreeColorMoveContext, IOriginalThreeColorUserProgress, IOriginalSlbAbValues, IOriginalOneMoveRemovalAb
        {
            public int Group, SlbGroup;
            public bool ThreeColor;
            public int GetThreeColorLevelsAbGroup() => Group;
            public bool IsCurrentLevelThreeColor => ThreeColor;
            public bool IgnoreMoveConfig => false;
            public int GameType => 0;
            public int AbGroup => SlbGroup;
            public void SetAbValues() { }
            public bool IsOneMoveRemoveAbEnabled() => false;
        }
        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        private static OriginalWelcomeBackMoveTiers Tiers() => JsonUtility.FromJson<OriginalWelcomeBackMoveTiers>(
            File.ReadAllText(Path.Combine(ProjectPath, "SourceCatalog/welcome_back_move_tiers_snapshot.json")));

        public static void LoadFirstLevel(FTiledLevel level, OriginalActiveLevelData active, OriginalThreeColorMoveConfig config,
            IOriginalThreeColorAbValues ab, IOriginalThreeColorNameContext manager, OriginalMoveBudgetAdjustment adjustment)
        {
            Compose(level, active, config, ab, manager, adjustment, 1, 1, new Progress(), new OriginalLeagueUserProgress(),
                out _, out _).LevelLoad();
        }
        public static void LoadFirstLevel(FTiledLevel level, IOriginalLevelLoadSession session, OriginalSlbMoveConfig slb)
        {
            var input = new Inputs(); var active = session.ActiveLevel;
            var config = ThreeColorMoveVerification.CreateSnapshotConfig(() => input, () => input);
            var duke = new OriginalSuperDukeMoveRemoval(() => input);
            var adjustment = new OriginalMoveBudgetAdjustment(() => duke, () => active, slb);
            Compose(level, active, config, input, input, adjustment, 1, 1, new Progress(), new OriginalLeagueUserProgress(),
                out _, out _, session).LevelLoad();
        }
        private static OriginalLevelLoad Compose(FTiledLevel level, OriginalActiveLevelData active, OriginalThreeColorMoveConfig config,
            IOriginalThreeColorAbValues ab, IOriginalThreeColorNameContext manager, OriginalMoveBudgetAdjustment adjustment,
            int number, int userNumber, Progress giftProgress, OriginalLeagueUserProgress leagueProgress,
            out OriginalWelcomeBackMoves gift, out OriginalLeagueLevelMoves league, IOriginalLevelLoadSession ownedSession = null)
        {
            // This small list is a diagnostic input, not a substitute for the original LeagueLevels default.
            var leagueLevels = new OriginalLeagueLevelMoves(() => "111:-2,222:3"); league = leagueLevels;
            var context = new OriginalLeagueMoveContext(() => active, () => leagueLevels, () => leagueProgress);
            var episode = new OriginalLevelEpisodeHelper(new OriginalSemiDynamicDifficulty(context));
            var user = new OriginalWelcomeBackUserProgress { Level = userNumber };
            gift = new OriginalWelcomeBackMoves(() => user, () => active, _ => { }, Tiers()) { Progress = giftProgress, Rewards = new object() };
            var moves = new OriginalThreeColorMoves(config, () => ab, () => active, context.GetLeagueMoveOffset,
                _ => throw new InvalidOperationException("Separate league-config offset is not installed in this diagnostic."),
                _ => throw new InvalidOperationException("Separate league-config minimum is not installed in this diagnostic."));
            var session = ownedSession ?? new OriginalLevelLoadSession { ActiveLevel = active };
            return new OriginalLevelLoad(_ => throw new InvalidOperationException("Unexpected resource reload in this diagnostic."),
                () => userNumber, () => session, moves, adjustment)
            { Parser = new Parser(level), LevelNumber = number, Episode = episode, ThreeColorContext = manager, WelcomeBack = gift };
        }
        [MenuItem("Recovery/Verify Level Load")]
        public static void Run()
        {
            var level = OriginalLevelReader.Load("Levels/level_1");
            var baseline = MoveBudgetVerification.FirstLevelMoves();
            Require(baseline.GetMaxMoves() == 30 && baseline.GetLeftMoves() == 30 && baseline.GetMadeMoves() == 0, "Native first-level loading path");
            var input = new Inputs();
            var config = ThreeColorMoveVerification.CreateSnapshotConfig(() => input, () => input);
            var slb = new OriginalSlbMoveConfig(() => input, () => JsonUtility.FromJson<OriginalSlbMoveConfigData>(File.ReadAllText(Path.Combine(ProjectPath, "SourceCatalog/slb_move_config_snapshot.json"))));
            OriginalActiveLevelData active = null;
            var names = new OriginalThreeColorLevelNames(() => active, () => input, () => input, () => input, config);
            active = new OriginalActiveLevelData(slb, names);
            var duke = new OriginalSuperDukeMoveRemoval(() => input);
            var adjustment = new OriginalMoveBudgetAdjustment(() => duke, () => active, slb);
            int[] budgets = { 15, 18, 21, 18 }; string[] extensions = { "U", "V", "W", "X" };
            input.ThreeColor = true;
            for (int i = 0; i < 4; i++)
            {
                input.Group = i + 2;
                var loader = Compose(level, active, config, input, input, adjustment, 1, 1, new Progress(), new OriginalLeagueUserProgress(), out _, out _);
                loader.LevelLoad();
                Require(active.State == 1 && active.MoveCount == budgets[i] && active.OriginalMoveCount == budgets[i]
                    && active.Name == "00001v02_" + extensions[i], "Actual three-color preprocessing through LevelManager.Load");
            }
            input.ThreeColor = false; input.Group = 0;
            active.LeagueIndex = 1;
            var leagueLoader = Compose(level, active, config, input, input, adjustment, 1, 1, new Progress(), new OriginalLeagueUserProgress { LeagueLevel = 2 }, out _, out var league);
            leagueLoader.LevelLoad();
            Require(active.MoveCount == 33 && active.OriginalMoveCount == 33 && league.GetLeagueLevel(3).Moves == -2, "Native normal episode league offset and list wrap");
            active.LeagueIndex = 0; active.Difficulty = 3; input.SlbGroup = 3;
            var giftLoader = Compose(level, active, config, input, input, adjustment, 162, 162, new Progress { TierValue = 3, Start = 162 }, new OriginalLeagueUserProgress(), out _, out _);
            giftLoader.LevelLoad();
            Require(active.OriginalMoveCount == 50 && active.MoveCount == 48 && active.Name == "00001v02_O"
                && OriginalMoveBudgetAdjustment.LastAddedExtraMoveCount == -2, "Welcome-back moves precede active Load and SLB post-adjustment");
            input.SlbGroup = 0; active.Difficulty = 0;
            var expired = new Progress { TierValue = 3, Start = 100 };
            var expiryLoader = Compose(level, active, config, input, input, adjustment, 125, 125, expired, new OriginalLeagueUserProgress(), out var gift, out _);
            expiryLoader.LevelLoad();
            Require(active.MoveCount == 30 && expired.Start == 0 && expired.TierValue == 0 && expired.Date == 0
                && !expired.Reward && expired.Writes == 4 && gift.Rewards == null, "Exactly 25 levels invokes the original reset sequence");
            File.WriteAllText(Path.Combine(ProjectPath, "level_load_unity_verification.json"), JsonUtility.ToJson(new Result
            {
                passed = true, baseline_moves = 30, three_color_groups = 4, welcome_original_moves = 50, slb_adjusted_moves = 48,
                scope = "Actual original first-level JSON and native LevelManager.LevelLoad composition, episode/SemiDynamicDifficulty with concrete league CSV parsing and wrap, recovered three-color config/actual preprocessing for four groups feeding active Load/name, metadata-derived welcome-back tiers and hard minimum before SLB post-adjustment, exact 25-level reset. Resource/parser force/name transport, AB/user/session/progress persistence, league input list and storage remain explicit fixture boundaries. Separate three-color league config is deliberately unavailable. Formal constructor/session Start/Restart/save/LevelTouch/GameContext and full playable resolution remain pending."
            }, true) + "\n");
            Debug.Log("Native LevelManager.Load, episode/league/welcome-back and move ordering Unity verification passed.");
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [Serializable] private sealed class Result
        { public bool passed; public int baseline_moves, three_color_groups, welcome_original_moves, slb_adjusted_moves; public string scope; }
    }
}
