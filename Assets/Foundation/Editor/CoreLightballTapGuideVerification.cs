using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreLightballTapGuideVerification
    {
        private const string ReportPath = "live_core_lightball_tap_guide_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Input = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalPlayableBoard restartingAttempt;
        private static OriginalInitialBoardView board;
        private static OriginalGridCell focus;
        private static CoreLevelSettingsMenu settingsMenu;
        private static OriginalLightballBoardItem ball;
        private static OriginalGameTouchListener listener;
        private static OriginalBoardItem[] originalItems;
        private static Report report;
        private static int scenario, step, frame, operationStartFrame, moves, refills, sample;
        private static float deadline, onlyLightballAt, shownAt, dismissedAt;
        private static float operationFinishedAt, settingsClosedAt;

        public static void Run(FoundationController owner)
        {
            report = new Report();
            Begin(owner);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            Require(owner.CurrentLevelNumber == 55, "This fixture uses the authored level 55 checkerboard.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, owner.Gameplay);
            focus = board.Grid.GetCell(2, 2);
            settingsMenu = controller.GetComponentInChildren<CoreLevelSettingsMenu>();
            var views = new OriginalItemViewCreation(board);
            // Preserve the authored geometry. Use its egg prefab to leave two
            // separated match tiles, so even the real shuffle cannot form a trio.
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell == focus || (y == 4 && (x == 2 || x == 4)) ||
                    !(cell.Mediation.CurrentItem is OriginalMatchBoardItem tile)) continue;
                RemoveMatch(tile);
                views.CreateItemAtCell(26, cell);
            }
            RemoveMatch((OriginalMatchBoardItem)focus.Mediation.CurrentItem);
            ball = (OriginalLightballBoardItem)owner.Gameplay.CreateItemAtCell(50, focus);
            moves = owner.Gameplay.Moves.LeftMoves;
            refills = owner.Gameplay.CreatedRefills;
            int width = board.Grid.Width;
            originalItems = new OriginalBoardItem[width * board.Grid.Height];
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < width; x++)
                originalItems[y * width + x] = board.Grid.GetCell(x, y).Mediation.CurrentItem;

            var iterator = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            Require(board.PossibleMoves.CheckBoard(iterator) == 1, "The original possible-move finder classifies the fixture as lightball-only.");
            // Mark the runtime board dirty across one sparse update so the
            // actual shuffle search runs before the dynamic tutorial.
            board.State.Operations.OperationStart(2);
            operationStartFrame = board.Clock.FrameCount;
            deadline = Time.realtimeSinceStartup + 40f;
            step = sample = 0;
        }

        private static void RemoveMatch(OriginalMatchBoardItem tile)
        {
            tile.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)tile.SwapView);
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
            Require(Time.realtimeSinceStartup < deadline, $"Lightball guide timed out in scenario {scenario}, step {step}.");
            if (restartingAttempt != null)
            {
                var restarted = controller.Gameplay;
                if (restarted == null || ReferenceEquals(restarted, restartingAttempt)) return;
                restartingAttempt = null;
                Begin(controller);
            }

            var game = controller.Gameplay;
            var flow = controller.CoreFlow;
            var guide = flow.DirectedTutorial;
            Require(flow.NoPossibleMovesDialog == null && game.State.IsPlaying(), "A usable lightball keeps the actual level playing.");
            if (step == 0)
            {
                if (board.Clock.FrameCount - operationStartFrame < 12) return;
                board.State.Operations.OperationFinish(2);
                step++;
                return;
            }
            if (step == 1)
            {
                if (!game.HasOnlyLightballMove) return;
                Require(!board.State.HasOperation(6) && game.Session.NoPossibleMoveShuffleCount == 0 &&
                    game.Session.NoPossibleMoveColoredShuffleCount == 0, "The exhausted search preserves the lightball branch and move budget.");
                VerifyRestoredBoard();
                onlyLightballAt = board.Clock.Time;
                report.restored_boards++;
                step++;
            }
            if (step == 2)
            {
                if (board.Clock.Time - onlyLightballAt < 2f || board.Clock.FrameCount % 10 != 2) return;
                // The single operation fits between sparse updates. Joint fall and swap spans one.
                if (scenario == 0) board.State.Operations.OperationStart(4);
                else
                {
                    board.State.Operations.OperationStart(1);
                    board.State.Operations.OperationStart(2);
                }
                operationStartFrame = board.Clock.FrameCount;
                step++;
                return;
            }
            if (step == 3)
            {
                if (board.Clock.FrameCount - operationStartFrame < (scenario == 0 ? 3 : 12)) return;
                Require(game.HasOnlyLightballMove && guide == null,
                    "Only the sparse shuffle owner changes the flag, and joint fall and swap preserves it.");
                if (scenario == 0) board.State.Operations.OperationFinish(4);
                else
                {
                    board.State.Operations.OperationFinish(2);
                    board.State.Operations.OperationFinish(1);
                    report.joint_fall_swap_preserved = true;
                }
                operationFinishedAt = board.Clock.Time;
                step++;
                return;
            }
            if (scenario == 1 && step == 4)
            {
                if (board.Clock.Time - onlyLightballAt < 3.5f) return;
                CoreFlowVerification.Click(settingsMenu.GearButton, flow.UiCamera, repeat: false);
                Require(settingsMenu.Lifecycle.IsShowing && !settingsMenu.Lifecycle.IsClosing && flow.BlocksBoardInput, "The settings menu owns board input.");
                step++;
                return;
            }
            if (step == 5)
            {
                Require(guide == null, "No lightball prompt can open over settings.");
                if (board.Clock.Time - onlyLightballAt < 5.4f) return;
                CoreFlowVerification.Click(settingsMenu.GearButton, flow.UiCamera, repeat: false);
                Require(settingsMenu.Lifecycle.IsClosing, "The gear closes the same settings menu.");
                settingsClosedAt = board.Clock.Time;
                step++;
                return;
            }
            if ((scenario == 0 && step == 4) || step == 6)
            {
                if (guide == null || !guide.IsGuiding) return;
                shownAt = board.Clock.Time;
                report.guide_delay[scenario] = shownAt - onlyLightballAt;
                if (scenario == 0)
                {
                    report.delay_after_operation = shownAt - operationFinishedAt;
                    Require(report.delay_after_operation >= 4.95f,
                        "A short rocket operation between sparse ticks restarts the full guide delay.");
                }
                else
                {
                    report.delay_after_settings = shownAt - settingsClosedAt;
                    Require(!flow.BlocksBoardInput && report.delay_after_settings < 1.5f,
                        "The prompt appears when settings finishes closing without restarting the elapsed delay.");
                }
                Require(guide.FocusCell == focus && report.guide_delay[scenario] >= 4.95f &&
                    !guide.DismissButton.interactable && flow.MatchHints.ActiveSource == null,
                    "The authored guide opens on the usable lightball after five seconds and starts non-dismissible.");
                Require(focus.IsTouchEnabled && game.IsImpossibleMoveDisabledForCell(new OriginalCellPoint(focus.X, focus.Y)),
                    "The focused ball accepts a tap and suppresses impossible swipes.");
                int enabled = 0;
                for (int y = 0; y < board.Grid.Height; y++)
                for (int x = 0; x < board.Grid.Width; x++)
                    if (board.Grid.GetCell(x, y).IsTouchEnabled) enabled++;
                var camera = flow.UiCamera;
                Require(enabled == 1 && !guide.DismissButton.targetGraphic.Raycast(camera.WorldToScreenPoint(focus.GetViewPosition()), camera),
                    "The optional Button excludes the lightball cell from its raycast.");
                report.focused_input = true;
                var instruction = guide.GetComponentInChildren<TMPro.TextMeshPro>();
                Require(instruction.text == "You can tap on power-ups to explode them!", "Use the original English tutorial text.");
                foreach (var sprite in instruction.transform.parent.GetComponentsInChildren<SpriteRenderer>())
                {
                    float left = camera.WorldToViewportPoint(sprite.bounds.min).x;
                    float right = camera.WorldToViewportPoint(sprite.bounds.max).x;
                    Require(left >= 0f && right <= 1f, "The native centered dialog remains fully within the viewport.");
                }
                report.dialog_within_viewport = true;
                step = 7;
            }

            if (scenario == 0)
            {
                if (sample == 0)
                {
                    if (board.Clock.Time - shownAt < .25f) return;
                    GrassGameplayVerification.Capture(flow.UiCamera, "core_lightball_tap_guide.png");
                }
                if (sample < 3)
                {
                    Input.Set(focus, sample++);
                    listener.ManualUpdate();
                    return;
                }
                Require(game.Moves.LeftMoves == moves - 1 && game.Session.SpecialItemCounts.TappedLightballCount == 1 &&
                    !guide.IsGuiding && !game.IsImpossibleMoveDisabledForCell(new OriginalCellPoint(focus.X, focus.Y)),
                    "The native lightball tap spends one move and immediately releases the tutorial touch lock.");
                if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.CreatedRefills == refills) return;
                report.tap_activated = true;
                report.tap_refills = game.CreatedRefills - refills;
                scenario++;
                // The next fixture needs the authored board. Retry clears the checkpoint.
                restartingAttempt = game;
                deadline = Time.realtimeSinceStartup + 40f;
                flow.RetryLevel();
                return;
            }

            if (step == 7)
            {
                if (!guide.DismissButton.interactable) return;
                report.dismiss_delay = board.Clock.Time - shownAt;
                Require(report.dismiss_delay >= 2.95f, "The background Button is enabled after the native three-second interval.");
                CoreFlowVerification.Click(guide.DismissButton, flow.UiCamera);
                dismissedAt = board.Clock.Time;
                step++;
                return;
            }
            if (guide != null && guide.IsGuiding) return;
            Require(game.Moves.LeftMoves == moves && game.Session.SpecialItemCounts.TappedLightballCount == 0 &&
                focus.Mediation.CurrentItem == ball && focus.IsTouchEnabled &&
                focus.Neighbors.Get(1).IsTouchEnabled && !game.IsImpossibleMoveDisabledForCell(new OriginalCellPoint(focus.X, focus.Y)),
                "The standard skip Button releases touch and leaves the lightball and moves untouched.");
            if (board.Clock.Time - dismissedAt < 5.5f) return;
            report.skipped_without_move = report.passed = true;
            Finish();
        }

        private static void VerifyRestoredBoard()
        {
            int width = board.Grid.Width;
            for (int y = 0; y < board.Grid.Height; y++)
            for (int x = 0; x < width; x++)
            {
                var expected = originalItems[y * width + x];
                var mediation = board.Grid.GetCell(x, y).Mediation;
                Require(mediation.CurrentItem == expected && mediation.NextItem == expected && mediation.TargetItem == expected,
                    "The failed shuffle restores all three mediation slots.");
            }
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
            public bool passed, focused_input, dialog_within_viewport, tap_activated, skipped_without_move;
            public int restored_boards, tap_refills;
            public bool joint_fall_swap_preserved;
            public float dismiss_delay, delay_after_operation, delay_after_settings;
            public float[] guide_delay = new float[2];
        }
    }
}
