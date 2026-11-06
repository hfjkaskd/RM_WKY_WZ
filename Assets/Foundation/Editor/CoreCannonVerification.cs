using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreCannonVerification
    {
        private const string ReportPath = "live_core_cannon_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Samples = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalPlayableBoard previousAttempt;
        private static OriginalInitialBoardView board;
        private static OriginalGameAudioHost audio;
        private static OriginalBottomUi toolbar;
        private static OriginalInGameBoosterSlot slot;
        private static OriginalGameTouchListener listener;
        private static OriginalBoardItem[] before;
        private static Vector3 toolbarHome;
        private static int attempt, stage, frame, column, refills;
        private static float deadline, launchedAt, steadySince, previousCaptureDelta, toolbarReturnedAt;
        private static bool capturedArrival, directHitVerified;
        private static Report report;

        public static void Run(FoundationController owner)
        {
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
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
            audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            toolbar = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>();
            slot = toolbar.Boosters[2];
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Samples, owner.Gameplay);
            column = attempt == 1 ? 1 : 6;
            before = new OriginalBoardItem[board.Grid.Height];
            for (int y = 0; y < before.Length; y++) before[y] = board.Grid.GetCell(column, y).Mediation.CurrentItem;
            refills = owner.Gameplay.CreatedRefills;
            capturedArrival = directHitVerified = false;
            steadySince = -1f;
            toolbarReturnedAt = -1f;
            toolbarHome = toolbar.transform.position;
            Require(owner.CurrentLevelNumber == 17 && before.Length == 9 && slot.BoosterType == OriginalBoosterType.Cannon &&
                board.Goals.GetGoalLeftCount(11) == 63 && board.Goals.GetGoalLeftCount(15) == 20 && owner.Gameplay.Moves.LeftMoves == 23,
                "Use the original level 17 boxes and cupboards.");
            deadline = Time.realtimeSinceStartup + 35f;
            stage = 0;
            frame = -1;
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
            Require(Time.realtimeSinceStartup < deadline, $"Cannon timed out in attempt {attempt}, step {stage}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.Gameplay == null || ReferenceEquals(owner.Gameplay, previousAttempt)) return;
                Begin(owner);
            }
            var game = controller.Gameplay;
            var guide = controller.CoreFlow.OptionalBoosterTutorial;
            var camera = controller.CoreFlow.UiCamera;
            var outside = board.Grid.GetCell(7, 0);
            if (stage == 0)
            {
                if (guide == null || !guide.DismissButton.interactable) return;
                Require(guide.IsGuiding && !guide.IsSelected && slot.Button.interactable && !outside.IsTouchEnabled &&
                    !board.Grid.GetCell(column, 0).IsTouchEnabled, "The optional prompt initially locks the board.");
                if (attempt == 0) GrassGameplayVerification.Capture(camera, "core_cannon_level17_prompt.png");
                if (attempt == 1)
                {
                    CoreFlowVerification.Click(guide.DismissButton, camera, false);
                    Require(!guide.IsGuiding && !guide.IsSelected && slot.Button.interactable && outside.IsTouchEnabled,
                        "The real dismiss surface keeps the free cannon.");
                    Canvas.ForceUpdateCanvases();
                }
                CoreFlowVerification.Click(slot.Button, camera, false);
                stage++;
                return;
            }
            if (stage == 1)
            {
                Require(guide.IsSelected && game.IsBoosterSelected, "The real Cannon Button enters aiming.");
                if (attempt != 1)
                {
                    int highlighted = 0;
                    for (int y = 0; y < board.Grid.Height; y++)
                    for (int x = 0; x < 7; x++)
                    {
                        var cell = board.Grid.GetCell(x, y);
                        Require(cell.IsTouchEnabled && cell.Mediation.CurrentItem.SwapView is OriginalBoxItemSpriteView view &&
                            view.sortingGroup.enabled && view.sortingGroup.sortAtRoot && view.sortingGroup.sortingOrder == 510,
                            "All box cells in the seven-column guide region are raised.");
                        highlighted++;
                    }
                    Require(!outside.IsTouchEnabled, "The right two columns are locked in guided aiming.");
                    report.guided_highlights = highlighted;
                    if (attempt == 2)
                    {
                        Tap(outside);
                        Require(!guide.IsGuiding && !guide.IsSelected && slot.Button.interactable && outside.IsTouchEnabled &&
                            !board.State.HasOperation(7) && board.Goals.GetGoalLeftCount(11) == 63,
                            "A tap outside the guide cancels aiming and keeps the free shot.");
                        CoreFlowVerification.Click(slot.Button, camera, false);
                        Require(guide.IsSelected && !guide.IsGuiding, "The free shot can be selected after cancellation.");
                        report.wrong_column_cancel_keeps_free_use = report.passed = true;
                        Finish();
                        return;
                    }
                    GrassGameplayVerification.Capture(camera, "core_cannon_level17_target.png");
                }
                else Require(outside.IsTouchEnabled && !guide.IsGuiding, "Aiming after skipping leaves the entire board available.");
                Tap(board.Grid.GetCell(column, 4));
                launchedAt = board.Clock.Time;
                Require(!guide.IsSelected && !slot.Button.interactable && board.State.HasOperation(7) &&
                    HitCount() == 0 && board.Grid.GetCell(column, 0).CrossFallFreezeCount == 1 &&
                    board.Grid.GetCell(column, 8).CrossFallFreezeCount == 1 && outside.CrossFallFreezeCount == 0,
                    "The cannon reserves only the selected column and hits after its entrance animation.");
                stage++;
                return;
            }
            if (stage == 2)
            {
                if (attempt == 0 && !report.toolbar_hide_verified && board.Clock.Time - launchedAt >= .24f)
                {
                    report.toolbar_hide_sample_seconds = board.Clock.Time - launchedAt;
                    report.toolbar_hide_fraction = ToolbarProgress(camera);
                    Require(report.toolbar_hide_fraction > .21f && report.toolbar_hide_fraction < .65f,
                        "The toolbar is still easing toward its hidden position during the native 0.3-second retreat.");
                    report.toolbar_hide_verified = true;
                }
                int hit = HitCount();
                if (hit == 0)
                {
                    if (attempt == 0 && !capturedArrival && board.Clock.Time - launchedAt >= .48f)
                    {
                        GrassGameplayVerification.Capture(camera, "core_cannon_level17_arrival.png");
                        capturedArrival = true;
                    }
                    return;
                }
                Require(hit < before.Length && board.Grid.GetCell(column, 0).Mediation.CurrentItem != before[0] &&
                    board.Grid.GetCell(column, 8).Mediation.CurrentItem == before[8] && board.State.HasOperation(7) &&
                    toolbar.transform.position.y < toolbarHome.y &&
                    board.Goals.GetGoalLeftCount(11) == 63 - hit && board.Goals.GetGoalLeftCount(15) == 20,
                    "The projectile clears from bottom to top, while the toolbar is out of the way.");
                if (attempt == 0) GrassGameplayVerification.Capture(camera, "core_cannon_level17_impact.png");
                stage++;
                return;
            }
            if (!directHitVerified)
            {
                if (HitCount() < before.Length) return;
                Require(board.Goals.GetGoalLeftCount(11) == 54 && board.Goals.GetGoalLeftCount(15) == 20,
                    "The direct shot clears exactly nine boxes before subsequent cascades.");
                var projectile = UnityEngine.Object.FindObjectOfType<OriginalCannonBallView>();
                var top = board.Grid.GetCell(column, 8);
                float distance = Mathf.Abs(projectile.transform.position.y - top.GetViewPosition().y);
                report.top_held_at_impact = top.FallFreezeCount - (top.FreezeCountdown.IsCounting ? 1 : 0) == 1;
                report.top_impact_distance = distance;
                Require(report.top_held_at_impact && distance < .8f && top.CrossFallFreezeCount == 1,
                    "The projectile holds the top cell while it is inside the original hit radius.");
                directHitVerified = true;
            }
            if (attempt == 0 && !report.toolbar_return_verified && board.Grid.GetCell(column, 0).CrossFallFreezeCount == 0)
            {
                if (toolbarReturnedAt < 0f) toolbarReturnedAt = board.Clock.Time;
                if (board.Clock.Time - toolbarReturnedAt >= .32f)
                {
                    report.toolbar_return_sample_seconds = board.Clock.Time - toolbarReturnedAt;
                    report.toolbar_return_fraction = 1f - ToolbarProgress(camera);
                    Require(report.toolbar_return_fraction > 1.015f && report.toolbar_return_fraction < 1.07f,
                        "The toolbar keeps the native OutBack overshoot until its 0.4-second return settles.");
                    report.toolbar_return_verified = true;
                }
            }
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch)
            {
                steadySince = -1f;
                return;
            }
            if (steadySince < 0f) steadySince = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - steadySince < .55f) return;
            if (attempt == 1) GrassGameplayVerification.Capture(camera, "core_cannon_level17_free_refill.png");
            for (int y = 0; y < before.Length; y++)
            {
                var cell = board.Grid.GetCell(column, y);
                var item = cell.Mediation.CurrentItem;
                Require(cell.CrossFallFreezeCount == 0 && !cell.IsFallFrozen &&
                    (item is OriginalMatchBoardItem || item is OriginalSpecialBoardItem),
                    $"Cell ({column},{y}) must release both holds and refill. Item {item?.TiledId}, cross freeze {cell.CrossFallFreezeCount}, regular freeze {cell.FallFreezeCount}.");
            }
            int boxesLeft = board.Goals.GetGoalLeftCount(11);
            Require(boxesLeft <= 54 && board.Goals.GetGoalLeftCount(15) == 20 &&
                game.Moves.LeftMoves == 23 && game.Moves.MadeMoves == 0 && game.CreatedRefills > refills &&
                toolbar.transform.position == toolbarHome && audio.Playback.GetClipLastPlayedTime(1320) > 0f &&
                audio.Playback.GetClipLastPlayedTime(1321) > 0f, "The full column resolves and the toolbar returns without consuming a move.");
            if (attempt == 0)
            {
                Require(report.toolbar_hide_verified && report.toolbar_return_verified,
                    "The guided shot samples both original toolbar animations.");
                report.guided_boxes_cleared = 9;
                report.guided_refills = game.CreatedRefills - refills;
                GrassGameplayVerification.Capture(camera, "core_cannon_level17_refill.png");
            }
            else
            {
                report.skip_and_free_column_verified = true;
                report.free_cascade_extra_boxes = 54 - boxesLeft;
            }
            report.remaining_moves = game.Moves.LeftMoves;
            attempt++;
            // Each targeting case needs the authored board, not the just-spent checkpoint.
            previousAttempt = game;
            controller.CoreFlow.RetryLevel();
            controller = null;
        }

        private static int HitCount()
        {
            int count = 0;
            for (int y = 0; y < before.Length; y++)
                if (board.Grid.GetCell(column, y).Mediation.CurrentItem != before[y]) count++;
            return count;
        }

        private static float ToolbarProgress(Camera camera)
        {
            float hiddenY = camera.transform.position.y - camera.orthographicSize - 1.1f;
            return (toolbar.transform.position.y - toolbarHome.y) / (hiddenY - toolbarHome.y);
        }

        private static void Tap(OriginalGridCell cell)
        {
            Samples.Set(cell, 0);
            listener.ManualUpdate();
            Samples.Set(cell, 2);
            listener.ManualUpdate();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            EditorApplication.update -= Tick;
            Time.captureDeltaTime = previousCaptureDelta;
            report.status = "finished";
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable] private sealed class Report
        {
            public bool passed, skip_and_free_column_verified, wrong_column_cancel_keeps_free_use, top_held_at_impact;
            public float top_impact_distance;
            public bool toolbar_hide_verified, toolbar_return_verified;
            public float toolbar_hide_sample_seconds, toolbar_hide_fraction, toolbar_return_sample_seconds, toolbar_return_fraction;
            public int guided_highlights, guided_boxes_cleared, guided_refills, free_cascade_extra_boxes, remaining_moves;
            public string status, failure;
        }
    }
}
