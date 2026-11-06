using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreRecolorShuffleVerification
    {
        private const string ReportPath = "live_core_recolor_shuffle_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Input = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard previousAttempt;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell from, to;
        private static OriginalGridCell[] cells;
        private static OriginalMatchBoardItem[] startingItems;
        private static OriginalMatchBoardItem source;
        private static Vector3[] lastMotionPositions;
        private static Report report;
        private static int step, frame, operationFrame, moves, refills, sample, targetColor, sourceColor, difficultyDraws;
        private static bool rollback;
        private static float deadline;

        public static void Run(FoundationController owner)
        {
            report = new Report();
            rollback = false;
            Begin(owner);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            Require(owner.CurrentLevelNumber == 55, "The sparse fixture uses the authored level 55 geometry.");
            controller = owner;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, owner.Gameplay);
            from = rollback ? board.Grid.GetCell(6, 4) : board.Grid.GetCell(3, 3);
            to = board.Grid.GetCell(3, 4);
            cells = rollback
                ? new[] { board.Grid.GetCell(2, 0), board.Grid.GetCell(6, 0), from }
                : new[] { board.Grid.GetCell(2, 4), board.Grid.GetCell(4, 4), from };
            var lightballCell = rollback ? board.Grid.GetCell(2, 2) : null;
            var views = new OriginalItemViewCreation(board);
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell == cells[0] || cell == cells[1] || !(cell.Mediation.CurrentItem is OriginalMatchBoardItem item)) continue;
                item.Mediator.ClearFromAllRegisteredCells();
                item.RecycleView();
                if (cell == lightballCell) owner.Gameplay.CreateItemAtCell(50, cell);
                else views.CreateItemAtCell(cell == from ? 1 : 26, cell);
            }
            startingItems = Array.ConvertAll(cells, cell => (OriginalMatchBoardItem)cell.Mediation.CurrentItem);
            source = startingItems[2];
            lastMotionPositions = new Vector3[cells.Length];
            targetColor = startingItems[0].MatchType;
            sourceColor = source.MatchType;
            Require(startingItems[1].MatchType == targetColor && sourceColor != targetColor, "The board starts with exactly one eligible recolor source.");
            Require(PossibleMoves() == (rollback ? 1 : 0), "The actual move finder must reject ordinary moves before recolor.");
            moves = owner.Gameplay.Moves.LeftMoves;
            refills = owner.Gameplay.CreatedRefills;
            difficultyDraws = owner.Gameplay.GetLevelRandom().DifficultySeedGenerationCount;
            board.State.Operations.OperationStart(2);
            operationFrame = board.Clock.FrameCount;
            deadline = Time.realtimeSinceStartup + 40f;
            step = sample = 0;
        }

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { Advance(); }
            catch (Exception exception) { report.failure = exception.ToString(); Finish(); }
        }

        private static void Advance()
        {
            Require(Time.realtimeSinceStartup < deadline, $"Recolor verification timed out at step {step}, rollback {rollback}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || !owner.CoreFlow.IsInitialized || owner.Gameplay == null ||
                    ReferenceEquals(owner.Gameplay, previousAttempt)) return;
                Begin(owner);
            }
            var game = controller.Gameplay;
            Require(game.State.IsPlaying() && controller.CoreFlow.NoPossibleMovesDialog == null, "The level must remain playable.");
            if (step == 0)
            {
                if (board.Clock.FrameCount - operationFrame < 12) return;
                board.State.Operations.OperationFinish(2);
                step++;
                return;
            }
            if (rollback)
            {
                if (!game.HasOnlyLightballMove) return;
                Require(!board.State.HasOperation(6) && game.Moves.LeftMoves == moves && source.MatchType == sourceColor &&
                    source.HasView && game.Session.NoPossibleMoveColoredShuffleCount == 0, "An exhausted recolor search must restore the old color and the lightball branch.");
                for (int i = 0; i < cells.Length; i++) VerifySlots(cells[i], startingItems[i]);
                report.rollback_difficulty_draws = game.GetLevelRandom().DifficultySeedGenerationCount - difficultyDraws;
                Require(report.rollback_difficulty_draws == 3002, "Three fixed-pair rounds and the final unforced round exhaust the native retry budget.");
                report.recolor_rollback = report.passed = true;
                Finish();
                return;
            }
            if (step == 1)
            {
                if (!board.State.HasOperation(6)) return;
                Require(!game.CanUserMakeMove() && source.MatchType == targetColor && source.HasView &&
                    game.Session.NoPossibleMoveColoredShuffleCount == 1, "The search changes the color immediately and locks input before presentation.");
                report.success_difficulty_draws = game.GetLevelRandom().DifficultySeedGenerationCount - difficultyDraws;
                Require(report.success_difficulty_draws == 251, "The fixed pair stays in place until attempt 249 allows the third tile to remain.");
                report.input_locked = true;
                step++;
            }
            if (step == 2)
            {
                if (source.HasView) return;
                Require(board.State.HasOperation(6) && source.Mediator.CurrentCell == null &&
                    source.Mediator.NextCell == null && source.Mediator.TargetCell == null, "The old model must release its view and all three cell slots in motion.");
                int replacements = 0;
                foreach (var cell in cells)
                {
                    var match = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
                    Require(match.MatchType == targetColor && match.HasView, "Every tile must keep its visual after recolor.");
                    VerifySlots(cell, match);
                    if (Array.IndexOf(startingItems, match) >= 0) continue;
                    Require(((OriginalMatchItemSpriteView)match.SwapView).GetSorting().Order == 11999,
                        "The new view inherits the in-flight recolor sorting.");
                    replacements++;
                }
                Require(replacements == 1, "Recolor replaces exactly one model and view.");
                report.replaced_during_animation = true;
                step++;
            }
            if (step == 3)
            {
                if (board.State.HasOperation(6))
                {
                    for (int i = 0; i < cells.Length; i++)
                        lastMotionPositions[i] = ((OriginalMatchItemSpriteView)cells[i].Mediation.CurrentItem.SwapView).transform.position;
                    return;
                }
                Require(game.CanUserMakeMove() && game.Moves.LeftMoves == moves && PossibleMoves() == 2 &&
                    !board.InitialShuffle.MatchManager.IsThereAnyMatch(false), "The settled board permits a match and has no immediate match or move charge.");
                foreach (var cell in cells)
                {
                    var match = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
                    var view = (OriginalMatchItemSpriteView)match.SwapView;
                    Require((view.transform.position - lastMotionPositions[Array.IndexOf(cells, cell)]).sqrMagnitude < 1e-10f &&
                        view.GetSorting().Order == 7000 - 200 * (cell.Y - board.Layout.CurrentBoard.Bottom),
                        "The shuffle view retains its last native motion sample and restores row sorting.");
                    report.maximum_final_cell_distance = Mathf.Max(report.maximum_final_cell_distance,
                        Vector3.Distance(view.transform.position, cell.GetViewPosition()));
                }
                report.last_motion_sample_retained = true;
                step++;
            }
            if (sample < 3)
            {
                Input.Set(sample == 0 ? from : to, sample++);
                listener.ManualUpdate();
                return;
            }
            Require(game.Moves.LeftMoves == moves - 1, "The actual touch swap spends exactly one move.");
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.CreatedRefills == refills) return;
            report.refills_after_touch_swap = game.CreatedRefills - refills;
            rollback = true;
            // Restart clears the saved sparse fixture before the rollback case.
            previousAttempt = game;
            controller.CoreFlow.RetryLevel();
            controller = null;
        }

        private static int PossibleMoves() => board.PossibleMoves.CheckBoard(new OriginalGridIterator(
            new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout))));

        private static void VerifySlots(OriginalGridCell cell, OriginalMatchBoardItem item)
        {
            var slots = cell.Mediation;
            Require(slots.CurrentItem == item && slots.NextItem == item && slots.TargetItem == item &&
                item.Mediator.CurrentCell == slots && item.Mediator.NextCell == slots && item.Mediator.TargetCell == slots,
                "All three mediation slots must agree with the item.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            report.status = "finished";
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable]
        private sealed class Report
        {
            public string status, failure;
            public bool passed, input_locked, replaced_during_animation, recolor_rollback, last_motion_sample_retained;
            public float maximum_final_cell_distance;
            public int refills_after_touch_swap, success_difficulty_draws, rollback_difficulty_draws;
        }
    }
}
