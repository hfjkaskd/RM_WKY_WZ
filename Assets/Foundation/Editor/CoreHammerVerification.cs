using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreHammerVerification
    {
        private const string ReportPath = "live_core_hammer_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Samples = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGameAudioHost audio;
        private static OriginalGameTouchListener listener;
        private static OriginalInGameBoosterSlot slot;
        private static OriginalGridCell target, wrong;
        private static OriginalBoardItem targetBox;
        private static int stage, frame, startingMoves, startingGoals;
        private static float deadline, readyAt;
        private static Report report;

        public static void Run(FoundationController owner)
        {
            controller = owner;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Samples, owner.Gameplay);
            slot = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>().Boosters[0];
            target = board.Grid.GetCell(4, 6);
            wrong = board.Grid.GetCell(3, 6);
            targetBox = target.Mediation.CurrentItem;
            startingMoves = owner.Gameplay.Moves.LeftMoves;
            startingGoals = board.Goals.GetGoalLeftCount(11);
            Require(owner.CurrentLevelNumber == 8 && slot.BoosterType == OriginalBoosterType.Hammer &&
                targetBox is OriginalBoxBoardItem && targetBox.LayerCount == 1 && startingMoves == 27 && startingGoals == 34,
                "The authored level 8 hammer target is the single-layer box at (4,6).");
            deadline = Time.realtimeSinceStartup + 35f;
            readyAt = Time.realtimeSinceStartup + .3f;
            stage = 0;
            frame = -1;
            report = new Report();
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
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
            Require(Time.realtimeSinceStartup < deadline, "Hammer tutorial timed out in step " + stage);
            var guide = controller.CoreFlow.HammerTutorial;
            var game = controller.Gameplay;
            if (stage == 0)
            {
                if (guide == null || Time.realtimeSinceStartup < readyAt) return;
                Require(guide.IsGuiding && !guide.IsSelected && slot.Button.interactable && !target.IsTouchEnabled,
                    "The first prompt accepts the highlighted button and locks the board.");
                Tap(target);
                Require(!guide.IsSelected && target.Mediation.CurrentItem == targetBox && !board.State.HasOperation(7),
                    "A board tap before selecting the hammer cannot hit the target.");
                GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, "core_hammer_level8_select.png");
                CoreFlowVerification.Click(slot.Button, controller.CoreFlow.UiCamera);
                stage++;
                return;
            }
            if (stage == 1)
            {
                Require(guide.IsSelected && game.IsBoosterSelected && target.IsTouchEnabled && !wrong.IsTouchEnabled &&
                    !slot.Button.interactable, "The real button enables exactly the authored target.");
                Tap(wrong);
                Require(guide.IsSelected && !board.State.HasOperation(7) && board.Goals.GetGoalLeftCount(11) == startingGoals,
                    "The wrong cell preserves selection and does not spend the tutorial hammer.");
                GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, "core_hammer_level8_target.png");
                report.standard_button_and_target_guard = true;
                stage++;
                return;
            }
            if (stage == 2)
            {
                Tap(target);
                Require(!guide.IsGuiding && !game.IsBoosterSelected && board.State.HasOperation(7) &&
                    target.Mediation.CurrentItem == targetBox && wrong.IsTouchEnabled && game.Moves.LeftMoves == startingMoves,
                    "The real board gesture launches one delayed hammer hit and releases the guide.");
                VerifyLaunchPosition();
                stage++;
                return;
            }
            if (stage == 3)
            {
                if (target.Mediation.CurrentItem == targetBox) return;
                Require(board.Goals.GetGoalLeftCount(11) == startingGoals - 1 && board.State.HasOperation(7) &&
                    audio.Playback.GetClipLastPlayedTime(1317) > 0f && audio.Playback.GetClipLastPlayedTime(1318) > 0f,
                    "The original hit timing breaks one box and updates its goal while the animation is active.");
                GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, "core_hammer_level8_impact.png");
                report.delayed_hit_and_goal = true;
                stage++;
                return;
            }
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady) return;
            Require(game.Moves.LeftMoves == startingMoves && game.Moves.MadeMoves == 0 && game.CreatedRefills > 0 &&
                target.Mediation.CurrentItem is OriginalMatchBoardItem && !slot.Button.interactable &&
                audio.Playback.GetClipLastPlayedTime(1319) > 0f, "The free tutorial hit refills without spending a move or granting repeat stock.");
            report.remaining_moves = game.Moves.LeftMoves;
            report.refills = game.CreatedRefills;
            report.passed = true;
            Finish();
        }

        private static void VerifyLaunchPosition()
        {
            var bottom = controller.GetComponentInChildren<OriginalBottomUi>();
            var flight = UnityEngine.Object.FindObjectOfType<OriginalHammerBoosterView>();
            Require(flight != null, "The real hammer view exists immediately after the tap.");
            var animator = flight.GetComponentInChildren<Animator>(true);
            var launchOrigin = flight.transform.position + animator.transform.localPosition;
            // The original portrait frame is .075 units below the slot, before the native world offset.
            var expected = slot.transform.position + bottom.transform.TransformVector(Vector3.down * .075f) +
                new Vector3(.775f, -.478f, 0f);
            report.launch_offset_from_slot = launchOrigin - slot.transform.position;
            Require((launchOrigin - expected).sqrMagnitude < .000001f,
                "The hammer flies from the recovered portrait frame and world offset.");
            report.authored_launch_position = true;
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
            report.status = "finished";
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        [Serializable] private sealed class Report
        {
            public bool passed, standard_button_and_target_guard, delayed_hit_and_goal, authored_launch_position;
            public Vector3 launch_offset_from_slot;
            public int remaining_moves, refills;
            public string status, failure;
        }
    }
}
