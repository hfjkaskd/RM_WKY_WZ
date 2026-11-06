using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CorePowerHintVerification
    {
        private const string ReportPath = "live_core_power_hint_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Input = new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard restartingAttempt;
        private static OriginalGameTouchListener listener;
        private static OriginalGridCell from, to, fixture;
        private static Report report;
        private static int scenario, step, sample, frame, sceneOwner, moves, refills, goalBefore;
        private static float deadline, tapPauseStarted, stopStarted, stopTravel;
        private static OriginalRootTransform hintedRoot, secondRoot;
        private static Vector3 hintedDirection;
        private static int tapPulses, tapFrame;
        private static bool tapPeakSeen, verifyPause;

        public static void Run(FoundationController owner, bool pauseAfterTap = false)
        {
            report = new Report();
            verifyPause = pauseAfterTap;
            Begin(owner);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            EditorApplication.update += Tick;
        }

        private static void Begin(FoundationController owner)
        {
            controller = owner;
            sceneOwner = owner.GetInstanceID();
            Require(owner.CurrentLevelNumber == (scenario == 0 ? 2 : 38), "Formal scene selects the expected authored level.");
            GameSceneVerification.RequireGameBackground(scenario == 0
                ? "365375d749bf4afe8c8e0bca749b1127"
                : "bec9962458314d4c8adbde84f8784f28");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, owner.Gameplay);
            moves = owner.Gameplay.Moves.LeftMoves;
            refills = owner.Gameplay.CreatedRefills;
            step = sample = 0;
            deadline = Time.realtimeSinceStartup + 60f;
            if (scenario != 0)
            {
                Require(owner.Progress.PowerHintTapCount == 1, "Accepted rocket tap count survives a scene reload.");
                report.tap_count_reopened = owner.Progress.PowerHintTapCount;
                return;
            }

            // Level 2 has no authored special at entry. Keep its board and goals,
            // and install a single vertical rocket on a match tile above boxes.
            fixture = board.Grid.GetCell(6, 4);
            var item = (OriginalMatchBoardItem)fixture.Mediation.CurrentItem;
            item.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)item.SwapView);
            var rocket = (OriginalRocketBoardItem)owner.Gameplay.CreateItemAtCell(20, fixture);
            Require(rocket.Orientation == 1, "Use the vertical rocket fixture above level 2 boxes.");
            goalBefore = board.Goals.GetGoalLeftCount(11);
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
            Require(Time.realtimeSinceStartup < deadline, $"Power hint timed out in scenario {scenario}, step {step}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.GetInstanceID() == sceneOwner || owner.Gameplay == null) return;
                Begin(owner);
            }
            if (restartingAttempt != null)
            {
                if (controller.Gameplay == null || ReferenceEquals(controller.Gameplay, restartingAttempt)) return;
                restartingAttempt = null;
                Begin(controller);
            }

            var game = controller.Gameplay;
            var hints = controller.CoreFlow.MatchHints;
            if (scenario == 2)
            {
                VerifyComboSourceTap(game, hints);
                return;
            }
            if (step == 0)
            {
                from = hints.ActiveSource;
                to = hints.ActiveDestination;
                if (from == null) return;
                if (scenario == 0)
                {
                    if (from != fixture || to != null) return;
                    var rocket = ((OriginalRocketBoardItem)from.Mediation.CurrentItem).ItemView;
                    hintedRoot = rocket.rootTransform;
                    float scale = hintedRoot.GetScale().x;
                    if (tapPulses < 2)
                    {
                        if (scale >= 1.035f) tapPeakSeen = true;
                        else if (tapPeakSeen && scale <= 1.001f)
                        {
                            tapPeakSeen = false;
                            if (++tapPulses == 2) tapPauseStarted = Time.time;
                        }
                        return;
                    }
                    if (report.tap_pause_observed == 0f && scale > 1.004f)
                    {
                        report.tap_pause_observed = Time.time - tapPauseStarted;
                        Require(report.tap_pause_observed >= .36f && report.tap_pause_observed <= .62f,
                            "The next tap-hint burst follows the native 0.4-second pause.");
                    }
                    if (scale < 1.035f) return;
                    Require(scale <= 1.051f && hintedRoot.GetPosition().sqrMagnitude < .00001f &&
                        rocket.GetSorting().Order == 7010,
                        "A tap hint scales the actual rocket without translating it.");
                    report.tap_peak_observed = scale;
                }
                else
                {
                    Require(from.Mediation.CurrentItem is OriginalPropellerBoardItem &&
                        to?.Mediation.CurrentItem is OriginalPropellerBoardItem, "The authored level 38 combo leads the hint list.");
                    var firstView = ((OriginalPropellerBoardItem)from.Mediation.CurrentItem).ItemView;
                    var secondView = ((OriginalPropellerBoardItem)to.Mediation.CurrentItem).ItemView;
                    hintedRoot = firstView.rootTransform;
                    secondRoot = secondView.rootTransform;
                    hintedDirection = new Vector3(to.X - from.X, to.Y - from.Y, 0f);
                    float forward = Vector3.Dot(hintedRoot.GetPosition(), hintedDirection);
                    if (forward < .035f) return;
                    float backward = Vector3.Dot(secondRoot.GetPosition(), -hintedDirection);
                    Require(forward <= .051f && Mathf.Abs(forward - backward) < .001f &&
                        firstView.GetSorting().Order == 7010 && secondView.GetSorting().Order != 7010,
                        "Both actual propellers move toward each other at the original hint distance.");
                    report.combo_distance_observed = forward;
                }
                int expectedParticles = scenario == 0 ? 1 : 2;
                Require(game.Moves.LeftMoves == moves && hints.ActiveParticleCount == expectedParticles &&
                    board.Presentation.Pool.GetSpawnedCount(OriginalHintParticles.PoolId) == expectedParticles,
                    "The preview uses original particles and never spends a move.");
                report.particle_counts[scenario] = hints.ActiveParticleCount;
                GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, $"core_power_hint_level{controller.CurrentLevelNumber}.png");
                step = 1;
            }

            if (sample < 3)
            {
                bool trigger = sample == (scenario == 0 ? 2 : 1);
                if (trigger)
                {
                    stopStarted = Time.time;
                    stopTravel = Vector3.Dot(hintedRoot.GetPosition(), hintedDirection);
                }
                Input.Set(sample == 0 || to == null ? from : to, sample);
                listener.ManualUpdate();
                if (trigger)
                {
                    Require(hints.ActiveSource == from && hints.ActiveParticleCount == report.particle_counts[scenario],
                        "The actual operation retains particles during the stop interval.");
                    if (scenario == 0)
                    {
                        tapFrame = Time.frameCount;
                        Require(controller.Progress.PowerHintTapCount == 1 && ReadCommittedTapCount() == 0,
                            "The actual rocket tap marks progress dirty before its save boundary.");
                        if (verifyPause)
                        {
                            controller.Progress.SetApplicationPaused(true);
                            Require(ReadCommittedTapCount() == 1, "Pausing commits the rocket tap before any later frame or scene exit.");
                            report.tap_saved_on_pause = true;
                            controller.Progress.SetApplicationPaused(false);
                        }
                    }
                }
                sample++;
                return;
            }
            if (hints.ActiveSource != null)
            {
                if (scenario == 0) report.tap_restored_at_stop |= Mathf.Abs(hintedRoot.GetScale().x - 1f) < .001f;
                else
                {
                    float first = Vector3.Dot(hintedRoot.GetPosition(), hintedDirection);
                    float second = Vector3.Dot(secondRoot.GetPosition(), -hintedDirection);
                    if (board.State.HasAnyOperation() && first > .001f && first < stopTravel - .001f &&
                        Mathf.Abs(first - second) < .001f) report.combo_intermediate_stop_frames++;
                }
                return;
            }
            if (report.stop_seconds[scenario] == 0f) report.stop_seconds[scenario] = Time.time - stopStarted;
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.CreatedRefills == refills) return;
            Require(report.stop_seconds[scenario] >= .075f, "The hint retains its native stop interval during gameplay.");
            Require(game.Moves.LeftMoves == moves - 1 && game.Session.UsedHintCount == 1 && game.Session.ShownHintCount == 1,
                "The hinted operation spends one move and records exactly one hint.");
            Require(hints.ActiveSource == null && hints.ActiveParticleCount == 0 &&
                board.Presentation.Pool.GetSpawnedCount(OriginalHintParticles.PoolId) == 0,
                "The operation releases every hint particle.");
            report.refills[scenario] = game.CreatedRefills - refills;
            if (scenario == 1)
            {
                Require(game.AcceptedSwaps == 1 && game.Session.SpecialItemCounts.Combos.TryGetValue(10, out int count) && count == 1,
                    "The actual swap executes the original double-propeller combo once.");
                Require(report.combo_intermediate_stop_frames > 0, "Both propellers return during the actual swap.");
                report.authored_combo = true;
                restartingAttempt = game;
                scenario++;
                deadline = Time.realtimeSinceStartup + 60f;
                controller.CoreFlow.RetryLevel();
                return;
            }

            Require(report.tap_restored_at_stop && board.Goals.GetGoalLeftCount(11) < goalBefore &&
                controller.Progress.PowerHintTapCount == 1,
                "The actual tap removes boxes and records the TNT/rocket tap.");
            if (!verifyPause)
            {
                int interval = OriginalDatabaseService.UserSaveInterval;
                int firstSaveFrame = tapFrame + interval - tapFrame % interval;
                if (Time.frameCount <= firstSaveFrame) return;
                Require(ReadCommittedTapCount() == 1, "The gameplay update commits the dirty tap at the native frame boundary.");
                report.tap_saved_during_play = true;
            }
            report.rocket_tap = true;
            scenario++;
            string path = controller.gameObject.scene.path;
            controller.Progress.CompleteLevel(37);
            controller = null;
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private static void VerifyComboSourceTap(OriginalPlayableBoard game, OriginalMatchHintManager hints)
        {
            if (step == 0)
            {
                from = hints.ActiveSource;
                if (from == null) return;
                Require(from.Mediation.CurrentItem is OriginalPropellerBoardItem &&
                    hints.ActiveDestination?.Mediation.CurrentItem is OriginalPropellerBoardItem,
                    "The retried authored board shows its double-propeller hint.");
                step = 1;
            }
            if (sample < 3)
            {
                Input.Set(from, sample++);
                listener.ManualUpdate();
                return;
            }
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || hints.ActiveSource != null) return;
            Require(game.Moves.LeftMoves == moves - 1 && game.AcceptedSwaps == 0 &&
                game.Session.SpecialItemCounts.Combos.Count == 0 && game.Session.UsedHintCount == 1,
                "A tap on the combo hint's source activates one power and records one hint use.");
            report.combo_source_tap = report.passed = true;
            Finish();
        }

        private static int ReadCommittedTapCount()
        {
            using var db = OriginalRuntimeStorage.Open(controller.Progress.DatabasePath);
            return int.TryParse(db.Find("CorePowerHintTapCount").Value, out int count) ? count : 0;
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
            public bool passed, rocket_tap, authored_combo, combo_source_tap, tap_restored_at_stop, tap_saved_on_pause, tap_saved_during_play;
            public int tap_count_reopened, combo_intermediate_stop_frames;
            public float[] stop_seconds = new float[2];
            public float tap_peak_observed, tap_pause_observed, combo_distance_observed;
            public int[] particle_counts = new int[2], refills = new int[2];
        }
    }
}
