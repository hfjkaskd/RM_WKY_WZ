using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreOpeningCheckpointVerification
    {
        internal const string ReportPath = "live_core_opening_checkpoint_verification.json";
        internal static bool Reopening => Array.IndexOf(Environment.GetCommandLineArgs(), "-recoveryOpeningCheckpointRead") >= 0;
        private static FoundationController owner;
        private static OriginalInitialBoardView board;
        private static Report report;
        private static float deadline, enteredAt;

        internal static void Run(FoundationController controller)
        {
            owner = controller;
            board = owner.GetComponentInChildren<OriginalInitialBoardView>();
            report = new Report { level = owner.CurrentLevelNumber, reopened_in_new_process = Reopening };
            report.save_file = owner.Progress.DatabasePath + ".level";
            enteredAt = -1f;
            deadline = Time.realtimeSinceStartup + 15f;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Require(Time.realtimeSinceStartup < deadline, "Opening checkpoint timed out.");
                if (!Reopening)
                {
                    if (report.bytes == 0)
                    {
                        Require(owner.CoreFlow.IsEnteringLevel && owner.Gameplay == null,
                            "The opening snapshot must be published while the grid is still entering.");
                        Require(board.InitialPopulationAttempts > 0, "The first process starts on a newly populated board.");
                        if (!File.Exists(report.save_file)) return;
                        report.bytes = new FileInfo(report.save_file).Length;
                        Require(report.bytes > 0, "A real snapshot exists on disk during the entrance animation.");
                    }
                    if (owner.Gameplay == null) return;
                }
                else
                {
                    var game = owner.Gameplay;
                    if (game == null) return;
                    if (enteredAt < 0f) enteredAt = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - enteredAt < .25f) return;
                    Require(board.InitialPopulationAttempts == 0 && game.Moves.MadeMoves == 0 &&
                        game.Moves.LeftMoves == game.Session.OriginalMoveCount && board.CreatedViewCount > 0,
                        "A new Unity process loaded the stored pre-move board and created its normal views.");
                    var flow = owner.CoreFlow;
                    Require(flow.DirectedTutorial == null && flow.ModalTutorial == null,
                        "An authored introductory guide is omitted on a restored attempt.");
                    bool hammerPrompt = flow.HammerTutorial != null && flow.HammerTutorial.IsGuiding;
                    bool optionalPrompt = flow.OptionalBoosterTutorial != null && flow.OptionalBoosterTutorial.IsGuiding;
                    Require(hammerPrompt == (report.level == 8) && optionalPrompt ==
                        (report.level == 14 || report.level == 17 || report.level == 19),
                        "The unused free booster guide starts at the prompt on a zero-move restore.");
                    report.hammer_prompt = hammerPrompt;
                    report.optional_prompt = optionalPrompt;
                    report.restored_view_count = board.CreatedViewCount;
                    report.moves = game.Moves.LeftMoves;
                }
                var difficulty = owner.Gameplay.GetLevelRandom();
                report.level_try_count = owner.Progress.LevelTryCount;
                report.difficulty_seed = difficulty.DifficultySeed;
                report.difficulty_draws = difficulty.DifficultySeedGenerationCount;
                if (!Reopening) Require(report.level_try_count == 1, "A new opening registers its first attempt.");
                if (Reopening && report.level == 66)
                {
                    var arguments = Environment.GetCommandLineArgs();
                    int option = Array.IndexOf(arguments, "-recoveryExpectedVaseRefills");
                    int expectedVases = option < 0 ? 10 : int.Parse(arguments[option + 1]);
                    report.vase_refills = VerifyRefillQuota(owner.Gameplay, 0, 25, expectedVases);
                    report.egg_refills = VerifyRefillQuota(owner.Gameplay, 3, 26, 40);
                }
                report.passed = true;
                Finish();
            }
            catch (Exception error)
            {
                report.failure = error.ToString();
                Finish();
            }
        }

        private static int VerifyRefillQuota(OriginalPlayableBoard game, int column, int obstacleId, int expected)
        {
            var cell = (OriginalFillingCell)game.Grid.GetCell(column, game.Grid.Height - 1);
            int generated = 0;
            for (int i = 0; i < expected + 2; i++)
            {
                var item = game.CreateItemForFillingCellAt(cell.X, cell.Y + 1f, cell);
                Require(item != null, "The real filling source can still create a tile.");
                var view = (OriginalItemSwapView)item.SwapView;
                cell.RemoveCreatedItem();
                view.DisableFillMask();
                game.Pool.Recycle(view);
                if (item.TiledId == obstacleId) generated++;
                Require(i < expected ? item.TiledId == obstacleId : item is OriginalMatchBoardItem,
                    $"Column {column} generated tile {item.TiledId} at refill {i + 1}. The obstacle quota is {expected}.");
            }
            return generated;
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }

        [Serializable]
        private sealed class Report
        {
            public bool passed, reopened_in_new_process, hammer_prompt, optional_prompt;
            public int level, moves, restored_view_count, level_try_count, difficulty_seed, difficulty_draws;
            public int vase_refills, egg_refills;
            public long bytes;
            public string failure, save_file;
        }
    }
}
