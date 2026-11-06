using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreDynamicPowerTutorialVerification
    {
        private const string ReportPath = "live_core_dynamic_power_tutorial_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Input = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell focus, other;
        private static OriginalDirectedTutorialView shown;
        private static Report report;
        private static int scenario, phase, sample, frame, ownerId, budget, operationFrame, stableFrame;
        private static float deadline, shownAt;
        private static string fixtureCheckpoint;

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
            ownerId = owner.GetInstanceID();
            Require(owner.CurrentLevelNumber == 5, "The test uses the authored level 5 grass board.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, owner.Gameplay);
            shown = null;
            phase = sample = 0;
            deadline = Time.realtimeSinceStartup + 25f;
            budget = owner.Gameplay.Moves.LeftMoves;
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
            Require(Time.realtimeSinceStartup < deadline, $"Guide timed out in scenario {scenario}, phase {phase}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.GetInstanceID() == ownerId || owner.Gameplay == null) return;
                Begin(owner);
            }
            var game = controller.Gameplay;
            var guide = controller.CoreFlow.DirectedTutorial;
            if (shown == null && guide != null && guide.IsGuiding)
            {
                shown = guide;
                shownAt = board.Clock.Time;
            }
            if (phase == 0)
            {
                if (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation()) return;
                if (scenario == 3)
                {
                    Require(board.InitialPopulationAttempts == 0 && game.Moves.MadeMoves == 0,
                        "The final scene restores the real saved board from the skipped guide.");
                    // Re-enable the account-level combo gate so only the
                    // restored per-attempt flag can suppress the eligible guide.
                    controller.Progress.CompleteLevel(4);
                }
                if (scenario == 0)
                {
                    Place(1, 1, 10);
                    Place(7, 7, 20);
                }
                else if (scenario == 1)
                {
                    other = Place(7, 6, 40);
                    focus = Place(8, 6, 40);
                    Place(1, 5, 20);
                    Place(1, 6, 20);
                }
                else if (scenario == 2)
                {
                    other = Place(1, 5, 20);
                    focus = Place(1, 6, 40);
                    Place(7, 6, 10);
                }
                StartSettle();
                phase++;
            }
            if (phase == 1)
            {
                if (!Settled()) return;
                if (scenario == 3)
                {
                    Require(controller.CoreFlow.DirectedTutorial == null,
                        "The saved per-attempt flag suppresses the guide even after its progress gate is rearmed.");
                    report.persisted_after_reload = report.passed = true;
                    Finish();
                    return;
                }
                if (scenario == 0)
                {
                    Require(controller.CoreFlow.DirectedTutorial == null, "Two powers do not qualify for the tap guide.");
                    report.two_power_gate = true;
                    focus = Place(6, 2, 40);
                    other = null;
                    game.Moves.SetMaxMoves(5);
                    StartSettle();
                    phase = 2;
                    return;
                }
                phase = 3;
            }
            if (phase == 2)
            {
                if (!Settled()) return;
                Require(controller.CoreFlow.DirectedTutorial == null, "Exactly five moves do not qualify for the tap guide.");
                report.five_move_gate = true;
                game.Moves.SetMaxMoves(budget);
                StartSettle();
                phase++;
                return;
            }
            if (phase == 3)
            {
                if (!Settled()) return;
                if (guide == null || !guide.IsGuiding) return;
                Require(guide.FocusCell == focus && guide.SecondCell == other, "The native priority selects the expected power or pair.");
                Require(!guide.DismissButton.interactable && controller.CoreFlow.MatchHints.ActiveSource == null,
                    "The guide initially blocks skipping and replaces the idle hint.");
                int unlocked = 0;
                for (int y = 0; y < board.Grid.Height; y++)
                    for (int x = 0; x < board.Grid.Width; x++)
                        if (board.Grid.GetCell(x, y).IsTouchEnabled) unlocked++;
                Require(unlocked == (other == null ? 1 : 2) && focus.IsTouchEnabled && (other == null || other.IsTouchEnabled),
                    "The guide only opens the selected board cells.");
                var camera = controller.CoreFlow.UiCamera;
                Require(!guide.DismissButton.targetGraphic.Raycast(camera.WorldToScreenPoint(focus.GetViewPosition()), camera) &&
                    (other == null || !guide.DismissButton.targetGraphic.Raycast(camera.WorldToScreenPoint(other.GetViewPosition()), camera)),
                    "The Button leaves the highlighted cells to the gameplay listener.");
                var text = guide.GetComponentInChildren<TextMeshPro>();
                Require(text.text == (other == null ? "You can tap on power-ups to explode them!" :
                    "You can swap two power-ups to make a more powerful explosion!"), "Use the recovered English tutorial dialog.");
                phase++;
            }
            if (phase == 4)
            {
                if (board.Clock.Time - shownAt < .25f) return;
                GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, $"core_dynamic_power_guide_{scenario}.png");
                if (scenario == 2)
                {
                    File.Copy(controller.Progress.DatabasePath + ".level", "core_dynamic_checkpoint_sample.bytes", true);
                    report.sampled_during_guide = shown.IsGuiding;
                }
                phase++;
            }
            if (scenario == 2)
            {
                if (phase == 5)
                {
                    if (!shown.DismissButton.interactable) return;
                    report.skip_delay = board.Clock.Time - shownAt;
                    Require(report.skip_delay >= 2.95f, "The ordinary combo guide waits three seconds before skipping.");
                    CoreFlowVerification.Click(shown.DismissButton, controller.CoreFlow.UiCamera);
                    phase++;
                    return;
                }
                if (shown != null && shown.IsGuiding) return;
                Require(game.Moves.LeftMoves == budget && focus.IsTouchEnabled && other.IsTouchEnabled &&
                    !focus.Mediation.CurrentItem.IsTapDisabled && !other.Mediation.CurrentItem.IsTapDisabled,
                    "Skipping the vertical guide restores the powers and the input lock without a move.");
                report.vertical_skip = true;
                Reload();
                return;
            }
            if (scenario == 1 && phase == 5)
            {
                if (sample < 3) { Gesture(focus, focus); return; }
                Require(game.Moves.LeftMoves == budget && shown.IsGuiding,
                    "The horizontal combo guide rejects tapping either selected special.");
                sample = 0;
                phase++;
            }
            if (sample < 3) { Gesture(focus, other ?? focus); return; }
            if (shown != null && shown.IsGuiding) return;
            Require(game.Moves.LeftMoves == budget - 1 && game.Moves.MadeMoves == 1,
                "The accepted guide action spends exactly one move through the production listener.");
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady) return;
            if (scenario == 0) report.tap_activated = true;
            else
            {
                Require(game.Session.SpecialItemCounts.Combos.TryGetValue(5, out int count) && count == 1,
                    "The horizontal guided swap uses the actual TNT pair combo.");
                report.horizontal_combo = true;
                // Exercise the original level 4 success gate again with one prior combo.
                controller.Progress.CompleteLevel(4);
            }
            Reload();
        }

        private static void StartSettle()
        {
            operationFrame = frame;
            stableFrame = -1;
            board.State.Operations.OperationStart(2);
        }

        private static bool Settled()
        {
            if (operationFrame >= 0)
            {
                if (frame - operationFrame < 3) return false;
                board.State.Operations.OperationFinish(2);
                operationFrame = -1;
                stableFrame = frame;
            }
            return frame - stableFrame >= 6;
        }

        private static OriginalGridCell Place(int x, int y, int tiledId)
        {
            var cell = board.Grid.GetCell(x, y);
            Require(cell.Mediation.CurrentItem is OriginalMatchBoardItem, "Use an authored color tile for the power fixture.");
            var item = cell.Mediation.CurrentItem;
            item.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)item.SwapView);
            controller.Gameplay.CreateItemAtCell(tiledId, cell);
            return cell;
        }

        private static void Gesture(OriginalGridCell from, OriginalGridCell to)
        {
            Input.Set(sample == 0 ? from : to, sample++);
            listener.ManualUpdate();
        }

        private static void Reload()
        {
            scenario++;
            string path = controller.gameObject.scene.path;
            // The next two fixtures change their authored starting pieces. The
            // final reopen deliberately retains the production checkpoint.
            if (scenario < 3)
            {
                Require(Path.GetFileName(controller.Progress.DatabasePath).StartsWith("CoreLevels_verify_", StringComparison.Ordinal),
                    "Fixture resets require an isolated progress profile.");
                fixtureCheckpoint = controller.Progress.DatabasePath + ".level";
                SceneManager.sceneUnloaded += ResetFixtureCheckpoint;
            }
            controller = null;
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private static void ResetFixtureCheckpoint(Scene scene)
        {
            SceneManager.sceneUnloaded -= ResetFixtureCheckpoint;
            File.Delete(fixtureCheckpoint);
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
            public bool passed, two_power_gate, five_move_gate, tap_activated, horizontal_combo, vertical_skip,
                persisted_after_reload, sampled_during_guide;
            public float skip_delay;
        }
    }
}
