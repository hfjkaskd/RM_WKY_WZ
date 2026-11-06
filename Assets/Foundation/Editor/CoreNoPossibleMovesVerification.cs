using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class CoreNoPossibleMovesVerification
    {
        private const string ReportPath = "live_core_no_moves_verification.json";
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGridCell first, second;
        private static OriginalBoardItem firstItem, secondItem;
        private static Report report;
        private static int operationFrame, initialDraws;
        private static OriginalPlayableBoard initialAttempt;
        private static OriginalGameObjectPool initialPool;
        private static float started;
        private static bool preparing, waitingForShift, restarting;

        public static void Run(FoundationController initial)
        {
            controller = initial;
            initialAttempt = initial.Gameplay;
            started = Time.realtimeSinceStartup;
            preparing = true;
            waitingForShift = restarting = false;
            report = new Report();
            CoreFlowVerification.RequirePlayingMusic(initial);
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            initialPool = board.Presentation.Pool;
            first = board.Grid.GetCell(2, 0);
            second = board.Grid.GetCell(6, 4);
            firstItem = first.Mediation.CurrentItem;
            secondItem = second.Mediation.CurrentItem;
            Require(firstItem is OriginalMatchBoardItem && secondItem is OriginalMatchBoardItem,
                "The authored level supplies the two retained tiles.");
            var views = new OriginalItemViewCreation(board);
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell == first || cell == second || !(cell.Mediation.CurrentItem is OriginalMatchBoardItem tile)) continue;
                tile.Mediator.ClearFromAllRegisteredCells();
                tile.RecycleView();
                views.CreateItemAtCell(26, cell);
            }
            var iterator = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            Require(board.PossibleMoves.CheckBoard(iterator) == 0 && !board.Matches.IsThereAnyMatch(false),
                "The authored geometry now contains an unsolvable board without special items.");
            initialDraws = initial.Gameplay.GetLevelRandom().DifficultySeedGenerationCount;
            // Hold a real board operation across a sparse tick so the production
            // manager reevaluates the board when it settles.
            board.State.Operations.OperationStart(2);
            operationFrame = board.Clock.FrameCount;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Require(Time.realtimeSinceStartup - started < 30f, "No-moves restart timed out.");
                if (controller == null || controller.Gameplay == null || !controller.CoreFlow.IsInitialized) return;
                var game = controller.Gameplay;
                Require(controller.CurrentLevelNumber == 55 && controller.Progress.LevelToPlay == 55,
                    "The core no-moves route preserves level 55 progress.");
                if (preparing)
                {
                    if (board.Clock.FrameCount - operationFrame < 12) return;
                    board.State.SetShouldShift(true);
                    board.State.Operations.OperationFinish(2);
                    operationFrame = board.Clock.FrameCount;
                    preparing = false;
                    waitingForShift = true;
                    return;
                }
                if (waitingForShift)
                {
                    Require(board.State.ShouldTryToShiftCells() && !board.State.AreCellsBeingShifted() &&
                        !board.State.HasAnyOperation() && game.State.IsPlaying() &&
                        game.GetLevelRandom().DifficultySeedGenerationCount == initialDraws &&
                        controller.CoreFlow.NoPossibleMovesDialog == null,
                        "A pending shift defers shuffle and no-move handling before the shift animation starts.");
                    if (board.Clock.FrameCount - operationFrame < 12) return;
                    board.State.SetShouldShift(false);
                    waitingForShift = false;
                    report.pending_shift_deferred = true;
                    return;
                }
                if (!restarting)
                {
                    if (!game.State.IsNoPossibleMove()) return;
                    var dialog = controller.CoreFlow.NoPossibleMovesDialog;
                    if (!report.restored_board)
                    {
                        report.shuffle_difficulty_draws = game.GetLevelRandom().DifficultySeedGenerationCount - initialDraws;
                        Require(report.shuffle_difficulty_draws == 1002 && game.Session.NoPossibleMoveShuffleCount == 1 &&
                            game.Session.NoPossibleMoveColoredShuffleCount == 0 && !game.HasOnlyLightballMove &&
                            !board.State.HasOperation(6) && !game.CanUserMakeMove(),
                            "The actual ordinary search exhausted 500 checks, reset its operation and blocked play.");
                        VerifyRestoredItem(first, firstItem);
                        VerifyRestoredItem(second, secondItem);
                        report.restored_board = true;
                    }
                    if (dialog == null || !dialog.Lifecycle.IsShowing) return;
                    Require(game.State.IsNoPossibleMove() && game.Session.State == 5 && game.Moves.LeftMoves == 22,
                        "The real no-moves transition records session state 5 without spending moves.");
                    Require(controller.CoreFlow.FailDialog == null && controller.CoreFlow.BlocksBoardInput && game.IsUiTouching,
                        "The dedicated dialog blocks board input and owns the restart path.");
                    var music = controller.GetComponentInChildren<OriginalGameAudioHost>().MusicSource;
                    Require(!music.isPlaying && music.timeSamples == 0, "The no-moves dialog stops the board track.");
                    report.music_stopped = true;
                    CoreFlowVerification.RequireCurvedTitle(dialog, "No Possible Moves!", 8.9f);
                    report.title_curve_verified = true;
                    GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, "core_no_possible_moves.png");
                    restarting = true;
                    CoreFlowVerification.Click(dialog.RestartButton, controller.CoreFlow.UiCamera);
                    Require(controller.CoreFlow.IsTransitioning && game.State.IsNoGame() && !dialog.Lifecycle.IsShowing,
                        "Restart requests a new attempt and completes the dialog's own close before the board resets.");
                    report.closed_on_restart = true;
                    return;
                }
                if (ReferenceEquals(game, initialAttempt)) return;
                Require(ReferenceEquals(board.Presentation.Pool, initialPool) && board.Grid.GetCell(2, 0) != first &&
                    controller.GetComponent<OriginalContextUpdateHost>().Context != null,
                    "Restart rebuilds the cells and active context in the same scene and pool.");
                Require(game.State.IsPlaying() && game.Session.State == 2 && game.Moves.LeftMoves == 22 &&
                    controller.CoreFlow.Goals.GetGoalLeftCount(16) == 21 && controller.CoreFlow.Goals.GetGoalLeftCount(12) == 28 &&
                    controller.CoreFlow.NoPossibleMovesDialog == null,
                    "The standard Restart button restores the authored board and all its goals.");
                report.reused_scene_and_pool = true;
                CoreFlowVerification.RequirePlayingMusic(controller);
                report.music_restarted = true;
                Finish(true, "");
            }
            catch (Exception error)
            {
                Finish(false, error.ToString());
                Debug.LogException(error);
            }
        }

        private static void VerifyRestoredItem(OriginalGridCell cell, OriginalBoardItem item)
        {
            var slots = cell.Mediation;
            Require(slots.CurrentItem == item && slots.NextItem == item && slots.TargetItem == item &&
                item.Mediator.CurrentCell == slots && item.Mediator.NextCell == slots && item.Mediator.TargetCell == slots,
                "The exhausted search restores the original piece and all of its cell slots.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Finish(bool passed, string failure)
        {
            EditorApplication.update -= Tick;
            report.passed = passed;
            report.failure = failure;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable]
        private sealed class Report
        {
            public bool passed, restored_board, closed_on_restart, title_curve_verified, music_stopped, music_restarted, reused_scene_and_pool;
            public bool pending_shift_deferred;
            public int shuffle_difficulty_draws;
            public string status = "finished";
            public string failure;
            public string scope = "Production GameScene with an explicitly arranged level 55 board. The actual runtime shuffle exhausts its ordinary search, restores the board and enters the no-moves state. The visible standard Restart Button closes the dialog and rebuilds the authored level in the current scene.";
        }
    }
}
