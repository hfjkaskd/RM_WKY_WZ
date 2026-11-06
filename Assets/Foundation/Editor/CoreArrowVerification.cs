using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreArrowVerification
    {
        private const string ReportPath = "live_core_arrow_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Samples = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalPlayableBoard attempt;
        private static OriginalInitialBoardView board;
        private static OriginalGameAudioHost audio;
        private static OriginalInGameBoosterSlot slot;
        private static OriginalGameTouchListener listener;
        private static Animator arrowAnimator;
        private static OriginalBoardItem[] before;
        private static int scenario, stage, frame, row, refillsBefore;
        private static float deadline, arrowStartedAt, lastImpactAt, previousCaptureDelta;
        private static Report report;

        public static void Run(FoundationController owner)
        {
            scenario = 0;
            previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            report = new Report();
            Begin(owner);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            attempt = owner.Gameplay;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            slot = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>().Boosters[1];
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Samples, owner.Gameplay);
            row = scenario == 1 ? 7 : 6;
            before = new OriginalBoardItem[board.Grid.Width];
            for (int x = 0; x < before.Length; x++) before[x] = board.Grid.GetCell(x, row).Mediation.CurrentItem;
            refillsBefore = owner.Gameplay.CreatedRefills;
            Require(owner.CurrentLevelNumber == 14 && before.Length == 8 && slot.BoosterType == OriginalBoosterType.Arrow &&
                board.Goals.GetGoalLeftCount(11) == 8 && owner.Gameplay.Moves.LeftMoves == 27, "Use the actual 8-wide level 14.");
            lastImpactAt = -1f;
            deadline = Time.realtimeSinceStartup + 30f;
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
            Require(Time.realtimeSinceStartup < deadline, $"Arrow tutorial timed out in scenario {scenario}, step {stage}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.Gameplay == null || ReferenceEquals(owner.Gameplay, attempt)) return;
                Begin(owner);
            }
            var guide = controller.CoreFlow.OptionalBoosterTutorial;
            var game = controller.Gameplay;
            var camera = controller.CoreFlow.UiCamera;
            if (stage == 0)
            {
                if (guide == null || !guide.DismissButton.interactable) return;
                Require(guide.IsGuiding && !guide.IsSelected && slot.Button.interactable &&
                    !board.Grid.GetCell(0, 6).IsTouchEnabled && !board.Grid.GetCell(0, 7).IsTouchEnabled,
                    "The authored optional prompt initially locks both rows.");
                if (scenario == 0) GrassGameplayVerification.Capture(camera, "core_arrow_level14_prompt.png");
                if (scenario == 1)
                {
                    CoreFlowVerification.Click(guide.DismissButton, camera, false);
                    Require(!guide.IsGuiding && !guide.IsSelected && slot.Button.interactable && board.Grid.GetCell(0, 7).IsTouchEnabled,
                        "The visible dismissal Button releases the board and retains the free arrow.");
                    Canvas.ForceUpdateCanvases();
                }
                CoreFlowVerification.Click(slot.Button, camera, false);
                stage++;
                return;
            }
            if (stage == 1)
            {
                Require(guide.IsSelected && game.IsBoosterSelected, "The standard Arrow Button selects the free tool.");
                var otherRow = board.Grid.GetCell(0, scenario == 1 ? 6 : 7);
                if (scenario != 1)
                {
                    foreach (var item in before)
                    {
                        Require(item is OriginalBoxBoardItem && item.LayerCount == 1 && item.SwapView is OriginalBoxItemSpriteView view &&
                            view.sortingGroup.enabled && view.sortingGroup.sortAtRoot && view.sortingGroup.sortingOrder == 510,
                            "Guided aiming raises all eight single-layer boxes.");
                    }
                    Require(!otherRow.IsTouchEnabled, "Guided aiming locks the other rows.");
                    Samples.Set(otherRow, 1);
                    listener.ManualUpdate();
                    Require(guide.IsSelected, "Hovering outside the target row keeps the selection active.");
                    if (scenario == 2)
                    {
                        Tap(otherRow);
                        Require(!guide.IsGuiding && !guide.IsSelected && slot.Button.interactable && otherRow.IsTouchEnabled &&
                            !board.State.HasOperation(7) && board.Goals.GetGoalLeftCount(11) == 8, "Wrong-row tap dismisses aiming without spending the free arrow.");
                        CoreFlowVerification.Click(slot.Button, camera, false);
                        Require(guide.IsSelected && !guide.IsGuiding, "Arrow remains selectable after the optional aim is canceled.");
                        report.wrong_row_cancel_keeps_free_use = report.passed = true;
                        Finish();
                        return;
                    }
                    GrassGameplayVerification.Capture(camera, "core_arrow_level14_target.png");
                }
                else
                {
                    Require(!guide.IsGuiding && otherRow.IsTouchEnabled, "Free aiming leaves other rows enabled.");
                    controller.CoreFlow.PressBack();
                    Require(!guide.IsSelected && !game.IsBoosterSelected && slot.Button.interactable &&
                        controller.CoreFlow.LevelQuitDialog == null && game.Moves.LeftMoves == 27,
                        "Back cancels the selected Arrow before the quit route and preserves the free use and moves.");
                    CoreFlowVerification.Click(slot.Button, camera, false);
                    Require(guide.IsSelected, "The Arrow can be selected again after Back.");
                    report.back_cancels_aim = true;
                }
                Tap(board.Grid.GetCell(3, row));
                arrowStartedAt = board.Clock.Time;
                arrowAnimator = UnityEngine.Object.FindObjectOfType<OriginalArrowBoosterView>().GetComponentInChildren<Animator>(true);
                Require(!guide.IsSelected && !guide.IsGuiding && board.State.HasOperation(7) && !slot.Button.interactable &&
                    HitCount() == 0 && game.Moves.LeftMoves == 27, "The tap commits one delayed arrow without spending a move.");
                stage++;
                return;
            }
            if (stage == 2)
            {
                int hit = HitCount();
                if (hit == 0) return;
                report.first_impact_cells[scenario] = hit;
                report.first_impact_delay[scenario] = board.Clock.Time - arrowStartedAt;
                Require(hit < before.Length && board.Grid.GetCell(0, row).Mediation.CurrentItem != before[0] &&
                    board.Grid.GetCell(before.Length - 1, row).Mediation.CurrentItem == before[before.Length - 1] &&
                    board.Grid.GetCell(0, row).IsFallFrozen && board.State.HasOperation(7), "Left cells take the first hit and stay frozen.");
                Require(!arrowAnimator.enabled, "The arrow is still in flight when the first cells are struck.");
                Require(board.Goals.GetGoalLeftCount(11) == (scenario == 0 ? 8 - hit : 8), "Direct arrow damage is confined to its selected row.");
                if (scenario == 0) GrassGameplayVerification.Capture(camera, "core_arrow_level14_impact.png");
                stage++;
                return;
            }
            if (lastImpactAt < 0f && HitCount() == before.Length) lastImpactAt = board.Clock.Time;
            if (lastImpactAt >= 0f && report.last_impact_to_unfreeze[scenario] == 0f &&
                !board.Grid.GetCell(0, row).IsFallFrozen)
            {
                report.last_impact_to_unfreeze[scenario] = board.Clock.Time - lastImpactAt;
                Require(report.last_impact_to_unfreeze[scenario] >= .36f,
                    "The row stays frozen through the native impact hold and fall lead-in.");
            }
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady) return;
            Require(report.last_impact_to_unfreeze[scenario] >= .36f,
                "The full row was frozen and then released by the arrow sequence.");
            Require(HitCount() == before.Length && game.CreatedRefills > refillsBefore && game.Moves.LeftMoves == 27 &&
                game.Moves.MadeMoves == 0 && audio.Playback.GetClipLastPlayedTime(7) > 0f, "The full row resolves, refills and plays the original arrow clip.");
            if (scenario == 0)
            {
                Require(board.Goals.GetGoalLeftCount(11) == 0, "All eight original row box goals are completed.");
                report.guided_boxes_cleared = 8;
                report.guided_refills = game.CreatedRefills - refillsBefore;
                GrassGameplayVerification.Capture(camera, "core_arrow_level14_refill.png");
            }
            else report.skip_and_free_row_verified = true;
            report.remaining_moves = game.Moves.LeftMoves;
            scenario++;
            // Start the next independent scenario through the production retry.
            controller.CoreFlow.RetryLevel();
            controller = null;
        }

        private static int HitCount()
        {
            int hit = 0;
            for (int x = 0; x < before.Length; x++)
                if (board.Grid.GetCell(x, row).Mediation.CurrentItem != before[x]) hit++;
            return hit;
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
            public bool passed, skip_and_free_row_verified, wrong_row_cancel_keeps_free_use, back_cancels_aim;
            public int guided_boxes_cleared, guided_refills, remaining_moves;
            public int[] first_impact_cells = new int[2];
            public float[] first_impact_delay = new float[2], last_impact_to_unfreeze = new float[2];
            public string status, failure;
        }
    }
}
