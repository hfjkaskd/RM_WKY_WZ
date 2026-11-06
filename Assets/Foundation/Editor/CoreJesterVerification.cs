using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreJesterVerification
    {
        private const string ReportPath = "live_core_jester_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Samples = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalInGameBoosterSlot slot;
        private static OriginalGameTouchListener listener;
        private static Dictionary<OriginalGridCell, OriginalBoardItem> boardBefore;
        private static HashSet<OriginalBoardItem> candidates;
        private static OriginalBoardItem testRocket;
        private static OriginalGridCell rocketCell;
        private static OriginalPlayableBoard previousAttempt;
        private static int frame, attempt, step;
        private static float deadline, usedAt, settledAt;
        private static bool capturedHat;
        private static Report report;

        public static void Run(FoundationController owner)
        {
            attempt = 0;
            report = new Report();
            Begin(owner);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            slot = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>().Boosters[3];
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Samples, owner.Gameplay);
            Require(owner.CurrentLevelNumber == 19 && owner.Gameplay.Moves.LeftMoves == 27 &&
                board.Goals.GetGoalLeftCount(15) == 40 && board.Goals.GetGoalLeftCount(11) == 20 &&
                board.Goals.GetGoalLeftCount(13) == 32 && slot.BoosterType == OriginalBoosterType.JesterHat,
                "Use the formal level 19 layout, goals and fourth toolbar slot.");
            boardBefore = new Dictionary<OriginalGridCell, OriginalBoardItem>();
            candidates = new HashSet<OriginalBoardItem>();
            capturedHat = false;
            settledAt = -1f;
            deadline = Time.realtimeSinceStartup + 50f;
            frame = -1;
            step = 0;
        }

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { Advance(); }
            catch (Exception error) { report.failure = error.ToString(); Finish(); }
        }

        private static void Advance()
        {
            Require(Time.realtimeSinceStartup < deadline, $"Jester timed out in attempt {attempt}, step {step}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.Gameplay == null || ReferenceEquals(owner.Gameplay, previousAttempt)) return;
                Begin(owner);
            }
            var game = controller.Gameplay;
            var guide = controller.CoreFlow.OptionalBoosterTutorial;
            var camera = controller.CoreFlow.UiCamera;
            if (step == 0)
            {
                if (guide == null || !guide.DismissButton.interactable) return;
                Require(guide.IsGuiding && !guide.IsSelected && !board.Grid.GetCell(4, 0).IsTouchEnabled,
                    "The optional prompt initially guards the board.");
                if (attempt == 0) GrassGameplayVerification.Capture(camera, "core_jester_level19_prompt.png");
                else
                {
                    CoreFlowVerification.Click(guide.DismissButton, camera, false);
                    Require(!guide.IsGuiding && slot.Button.interactable, "Dismissal retains the free Jester Hat.");
                    // Fixture on the second attempt only: replace one opening color with an existing rocket.
                    rocketCell = FindOpeningMatch();
                    var match = (OriginalMatchBoardItem)rocketCell.Mediation.CurrentItem;
                    match.Mediator.ClearFromAllRegisteredCells();
                    board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)match.SwapView);
                    testRocket = (OriginalBoardItem)game.CreateItemAtCell(20, rocketCell);
                    Canvas.ForceUpdateCanvases();
                }
                Snapshot();
                CoreFlowVerification.Click(slot.Button, camera, false);
                step++;
                return;
            }
            if (step == 1)
            {
                Require(guide.IsSelected && guide.ConfirmationButton.gameObject.activeInHierarchy &&
                    listener.TouchDisableCount == 1 && !board.State.HasOperation(7), "Selection only shows the standard Shuffle Button.");
                if (attempt == 0) GrassGameplayVerification.Capture(camera, "core_jester_level19_confirm.png");
                else if (!report.dismiss_cancel_reselect_verified)
                {
                    Gesture(board.Grid.GetCell(4, 0), board.Grid.GetCell(4, 0));
                    Require(!guide.IsSelected && !guide.ConfirmationButton.gameObject.activeInHierarchy &&
                        slot.Button.interactable && listener.TouchDisableCount == 0 && game.Moves.LeftMoves == 27,
                        "A board tap cancels selection and preserves the free use.");
                    CoreFlowVerification.Click(slot.Button, camera, false);
                    report.dismiss_cancel_reselect_verified = true;
                    return;
                }
                Canvas.ForceUpdateCanvases();
                CoreFlowVerification.Click(guide.ConfirmationButton, camera, false);
                usedAt = board.Clock.Time;
                Require(!guide.IsSelected && !slot.Button.interactable && listener.TouchDisableCount == 1 &&
                    board.State.HasOperation(6) && board.State.HasOperation(7) && game.Moves.LeftMoves == 27,
                    "Only the actual Shuffle Button starts both operations and reserves the free use.");
                step++;
                return;
            }
            if (attempt == 0 && !capturedHat && board.Clock.Time - usedAt >= .25f)
            {
                var effect = UnityEngine.Object.FindObjectOfType<OriginalJesterHatBoosterView>();
                var hat = effect != null ? effect.GetComponentInChildren<OriginalJesterRigView>() : null;
                Require(hat != null && hat.BoneCount == 37 && hat.PartCount == 10 && Mathf.Abs(hat.ExitDuration - .5833333f) < .0001f,
                    "The original 37-bone rig retains all ten normal/multiply runs and its original exit clip.");
                GrassGameplayVerification.Capture(camera, "core_jester_level19_hat.png");
                capturedHat = true;
            }
            if (step == 2)
            {
                int moved = CheckPermutation();
                if (moved == 0) return;
                Require(moved == 16 && listener.TouchDisableCount == 1 && board.State.HasOperation(6) &&
                    game.Moves.MadeMoves == 0, "All sixteen eligible models move in the locked shuffle phase.");
                if (attempt == 0) report.guided_pieces_relocated = moved;
                else
                {
                    Require(testRocket.Mediator.CurrentCell.Owner != rocketCell,
                        "The special piece moves using the same live model.");
                    report.special_piece_relocated = true;
                }
                step++;
                return;
            }
            if (step == 3)
            {
                if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch)
                { settledAt = -1f; return; }
                if (settledAt < 0f) settledAt = board.Clock.Time;
                if (board.Clock.Time - settledAt < .35f) return;
                var audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
                Require(listener.TouchDisableCount == 0 && !slot.Button.interactable && game.Moves.LeftMoves == 27 &&
                    audio.Playback.GetClipLastPlayedTime(1315) > 0f && audio.Playback.GetClipLastPlayedTime(1316) > 0f,
                    "Both hat sounds play and the board resumes without charging a move.");
                report.moves_after_shuffle = game.Moves.LeftMoves;
                if (attempt == 0)
                {
                    GrassGameplayVerification.Capture(camera, "core_jester_level19_shuffled.png");
                    // Start the next targeting case on a fresh attempt through the production retry.
                    attempt++;
                    previousAttempt = game;
                    controller.CoreFlow.RetryLevel();
                    controller = null;
                    return;
                }
                var directed = controller.CoreFlow.DirectedTutorial;
                if (directed != null && directed.IsGuiding)
                {
                    var focus = directed.FocusCell;
                    Require(focus != null, "The active power tutorial supplies its legal target.");
                    Gesture(focus, directed.SecondCell ?? focus);
                }
                else
                {
                    var cell = testRocket.Mediator.CurrentCell?.Owner;
                    Require(cell != null && cell.IsTouchEnabled && !testRocket.IsTapDisabled &&
                        game.CanUserMakeMove() && !game.IsUiTouching,
                        "The relocated rocket is playable after the shuffle releases input.");
                    Gesture(cell, cell);
                }
                step++;
                return;
            }
            if (game.Moves.MadeMoves == 0) return;
            Require(game.Moves.MadeMoves == 1 && game.Moves.LeftMoves == 26, "The next real player move is accepted and charged once.");
            report.normal_play_resumes = report.passed = true;
            Finish();
        }

        private static OriginalGridCell FindOpeningMatch()
        {
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell?.Mediation.CurrentItem is OriginalMatchBoardItem) return cell;
            }
            throw new InvalidOperationException("The opening contains no match item for the special-piece fixture.");
        }

        private static void Snapshot()
        {
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell == null) continue;
                var item = cell.Mediation.CurrentItem;
                boardBefore.Add(cell, item);
                if (item != null && (item.IsMatchItem || item.IsSpecialItem)) candidates.Add(item);
            }
            Require(candidates.Count == 16, "The original opening has sixteen eligible models.");
        }

        private static int CheckPermutation()
        {
            int moved = 0;
            var remaining = new HashSet<OriginalBoardItem>(candidates);
            foreach (var entry in boardBefore)
            {
                var current = entry.Key.Mediation.CurrentItem;
                if (entry.Value == null || !candidates.Contains(entry.Value))
                    Require(current == entry.Value, "Blockers and holes remain in their original positions during shuffle.");
                else
                {
                    Require(remaining.Remove(current) && current.Mediator.CurrentCell.Owner == entry.Key,
                        "Each eligible model belongs to exactly one original playable cell.");
                    if (current != entry.Value) moved++;
                }
            }
            return moved;
        }

        private static void Gesture(OriginalGridCell from, OriginalGridCell to)
        {
            Samples.Set(from, 0); listener.ManualUpdate();
            if (from != to) { Samples.Set(to, 1); listener.ManualUpdate(); }
            Samples.Set(to, 2); listener.ManualUpdate();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            report.status = "finished";
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable] private sealed class Report
        {
            public bool passed, dismiss_cancel_reselect_verified, special_piece_relocated, normal_play_resumes;
            public int guided_pieces_relocated, moves_after_shuffle;
            public string status, failure;
        }
    }
}
