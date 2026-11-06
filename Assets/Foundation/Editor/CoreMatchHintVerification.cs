using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreMatchHintVerification
    {
        private const string ReportPath = "live_core_match_hint_verification.json";
        private static readonly CoreModalTutorialVerification.GestureSamples Input =
            new CoreModalTutorialVerification.GestureSamples();
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGameTouchListener listener;
        private static OriginalBoardItem[] initialItems;
        private static OriginalGridCell from, to;
        private static Report report;
        private static int scenario, step, sample, frame, beganAt, sceneOwner, moves, refills;
        private static float deadline, stopStarted, stopTravel;
        private static OriginalRootTransform hintedRoot;
        private static Vector3 hintedDirection;

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
            sceneOwner = owner.GetInstanceID();
            Require(owner.CurrentLevelNumber == (scenario == 0 ? 2 : 4), "Actual GameScene and original board are selected.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            listener = new OriginalGameTouchListener(Input, owner.Gameplay);
            moves = owner.Gameplay.Moves.LeftMoves;
            refills = owner.Gameplay.CreatedRefills;
            beganAt = board.Clock.FrameCount;
            step = sample = 0;
            initialItems = new OriginalBoardItem[board.Grid.Width * board.Grid.Height];
            foreach (var cell in AllCells())
                initialItems[cell.Y * board.Grid.Width + cell.X] = cell.Mediation.CurrentItem;
            deadline = Time.realtimeSinceStartup + 30f;
        }

        private static System.Collections.Generic.IEnumerable<OriginalGridCell> AllCells()
        {
            for (int y = 0; y < board.Grid.Height; y++)
                for (int x = 0; x < board.Grid.Width; x++)
                    yield return board.Grid.GetCell(x, y);
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
            Require(Time.realtimeSinceStartup < deadline, $"Match hint timed out, level case {scenario}, step {step}.");
            if (controller == null)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner.GetInstanceID() == sceneOwner || owner.Gameplay == null) return;
                Begin(owner);
            }
            var game = controller.Gameplay;
            var flow = controller.CoreFlow;
            var hints = flow.MatchHints;
            if (step == 4)
            {
                var cell = hints.ActiveSource;
                var moved = cell?.Mediation.CurrentItem?.SwapView as OriginalMatchItemSpriteView;
                if (moved == null || moved.RootTransform.GetPosition().sqrMagnitude < .001f) return;
                int shown = game.Session.ShownHintCount;
                flow.Release();
                Require(hints.ActiveSource == null && hints.ActiveParticleCount == 0 &&
                    moved.RootTransform.GetPosition().sqrMagnitude < .00001f && !moved.IsHighlightActive &&
                    moved.GetSorting().Order == 7000 - 200 * cell.Y &&
                    game.Session.ShownHintCount == shown + 1,
                    "Releasing the scene hint owner immediately restores the live tile and its row sorting.");
                report.forced_release = report.passed = true;
                Finish();
                return;
            }
            if (step == 0)
            {
                if (scenario == 1)
                {
                    var tutorial = flow.DirectedTutorial;
                    if (tutorial == null || !tutorial.IsGuiding) return;
                    Require(hints.ActiveSource == null && game.Session.ShownHintCount == 0,
                        "Idle hints stay suppressed while the fixed grass tutorial owns guidance.");
                    if (board.Clock.FrameCount - beganAt < 200 || !tutorial.DismissButton.interactable) return;
                    CoreFlowVerification.Click(tutorial.DismissButton, flow.UiCamera);
                    report.suppressed_during_fixed_tutorial = true;
                    beganAt = board.Clock.FrameCount;
                }
                step = 1;
            }
            if (step == 1)
            {
                from = hints.ActiveSource;
                to = hints.ActiveDestination;
                if (from == null || to == null) return;
                int delay = board.Clock.FrameCount - beganAt;
                Require(scenario == 0 ? delay < 90 : delay >= 60,
                    "The authored level-two startup delay differs from a normal stable board.");
                report.hint_frame_delay[scenario] = delay;
                foreach (var cell in AllCells())
                {
                    var item = initialItems[cell.Y * board.Grid.Width + cell.X];
                    Require(ReferenceEquals(item, cell.Mediation.CurrentItem) &&
                        ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem),
                        "Native hint candidate probes restore all three mediation slots.");
                }
                step = 2;
            }
            if (step == 2)
            {
                Require(from == hints.ActiveSource && to == hints.ActiveDestination,
                    "The candidate stays selected during the preview.");
                var moved = (OriginalMatchItemSpriteView)from.Mediation.CurrentItem.SwapView;
                var direction = new Vector3(to.X - from.X, to.Y - from.Y, 0f);
                float movement = Vector3.Dot(moved.RootTransform.GetPosition(), direction);
                if (movement < .07f) return;
                Require(movement <= .101f && moved.IsHighlightActive && game.Moves.LeftMoves == moves &&
                    moved.GetSorting().Order == 7010,
                    "The real source tile pulses toward the destination without spending a move.");
                hintedRoot = moved.RootTransform;
                hintedDirection = direction;
                report.particle_counts[scenario] = hints.ActiveParticleCount;
                if (hints.ActiveParticleCount > 0)
                {
                    Require(board.Presentation.Pool.GetSpawnedCount(OriginalHintParticles.PoolId) == hints.ActiveParticleCount,
                        "The original board edge particles use pool 46.");
                    report.used_native_particles = true;
                }
                GrassGameplayVerification.Capture(flow.UiCamera, $"core_match_hint_level{controller.CurrentLevelNumber}.png");
                step = 3;
            }
            if (sample < 3)
            {
                if (sample == 1)
                {
                    stopStarted = Time.time;
                    stopTravel = Vector3.Dot(hintedRoot.GetPosition(), hintedDirection);
                }
                Input.Set(sample == 0 ? from : to, sample);
                listener.ManualUpdate();
                if (sample == 1)
                    Require(hints.ActiveSource == from && hints.ActiveParticleCount == report.particle_counts[scenario],
                        "A real swipe keeps the hint alive for its stopping interval.");
                sample++;
                return;
            }
            if (hints.ActiveSource != null)
            {
                float travel = Vector3.Dot(hintedRoot.GetPosition(), hintedDirection);
                if (board.State.HasAnyOperation() && travel > .001f && travel < stopTravel - .001f)
                    report.intermediate_stop_frames[scenario]++;
                return;
            }
            if (report.stop_seconds[scenario] == 0f) report.stop_seconds[scenario] = Time.time - stopStarted;
            if (board.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.CreatedRefills == refills) return;
            Require(report.intermediate_stop_frames[scenario] > 0 && report.stop_seconds[scenario] >= .075f,
                "The hint interpolates toward rest while the actual board action continues.");
            Require(game.AcceptedSwaps == 1 && game.Moves.LeftMoves == moves - 1 &&
                game.Session.UsedHintCount == 1 && game.Session.ShownHintCount == 1,
                "The original hinted gesture resolves once, spends one move, and records the hint.");
            Require(hints.ActiveSource == null && hints.ActiveParticleCount == 0 &&
                board.Presentation.Pool.GetSpawnedCount(OriginalHintParticles.PoolId) == 0,
                "Input releases the highlight and all particles.");
            report.successful_hinted_swaps++;
            if (scenario == 1)
            {
                Require(report.used_native_particles, "A higher-scored pattern uses the restored particles.");
                step = 4;
                return;
            }
            scenario++;
            string path = controller.gameObject.scene.path;
            controller.Progress.CompleteLevel(3);
            controller = null;
            SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
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
            public bool passed, suppressed_during_fixed_tutorial, used_native_particles, forced_release;
            public int successful_hinted_swaps;
            public int[] hint_frame_delay = new int[2], particle_counts = new int[2], intermediate_stop_frames = new int[2];
            public float[] stop_seconds = new float[2];
        }
    }
}
