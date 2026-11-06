using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreMailVerification
    {
        private const string ReportPath = "live_core_mail_verification.json";
        internal const string BatchReportPath = "live_core_mail_batch_verification.json";
        internal const string TargetScoreReportPath = "live_core_mail_target_score_verification.json";
        private const int MailGoal = 18;
        private enum Phase { Press, Reject, Match, PressAfterCollect, FinishGoals, NextLevel }
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalGoalManager goalManager;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalGameAudioHost audio;
        private static OriginalMailBoxBoardItem mailbox;
        private static OriginalMailBoxItemView collectedMailbox;
        private static Animator nativeSequenceAnimator;
        private static OriginalGridCell from, to;
        private static readonly Samples input = new Samples();
        private static Phase phase;
        private static int frame, step, movesBefore, mailsBefore;
        private static float started, flightStartedAt, settledAt;
        private static OriginalMailGoalFlight firstLetter;
        private static Transform letterView, letterShadow;
        private static Vector3 mailGoalPosition;
        private static RenderTexture flightCapture;
        private static Report report;
        private static OriginalContextContainer flightContext;
        private static OriginalGameLateUpdateDispatcher flightLateUpdates;
        private static int flightProbeId, sampledFrame;
        private static Vector3 sampledLetterScale, sampledLetterOffset, sampledShadowOffset;

        internal static void VerifyTargetScore(FoundationController owner)
        {
            var result = new TargetScoreReport();
            try
            {
                var levelBoard = owner.GetComponentInChildren<OriginalInitialBoardView>();
                var goals = levelBoard.Goals;
                var playable = owner.Gameplay;
                var center = new OriginalCellPoint(3, 1);
                var area = OriginalExploder.Next(13, center);
                var line = OriginalExploder.Next(11, center);
                Require(owner.CurrentLevelNumber == 94 && goals.GetGoalLeftCount(MailGoal) == 60,
                    "The target score uses the original level 94 board and letter goal.");
                for (int x = 0; x < 8; x++)
                    Require(levelBoard.Grid.GetCell(x, 0).Mediation.CurrentItem is OriginalMailBoxBoardItem,
                        "The authored bottom row is full of mailboxes.");
                var mailbox = (OriginalMailBoxBoardItem)levelBoard.Grid.GetCell(3, 0).Mediation.CurrentItem;
                result.area_score_per_letter = mailbox.GetExplodeScore(3);
                result.line_score_per_letter = mailbox.GetExplodeScore(2);
                Require(result.area_score_per_letter > 0 && result.line_score_per_letter > 0,
                    "The active mailbox contributes to both combo types.");

                int fullArea = playable.AreaTargets.CalculateScore(center, area, out _);
                int fullLine = playable.LineTargets.CalculateScore(false, 0, line, out _);
                goals.UpdateGoal(MailGoal, -59);
                int lastArea = playable.AreaTargets.CalculateScore(center, area, out _);
                int lastLine = playable.LineTargets.CalculateScore(false, 0, line, out _);
                goals.UpdateGoal(MailGoal, -1);
                int noMailArea = playable.AreaTargets.CalculateScore(center, area, out _);
                int noMailLine = playable.LineTargets.CalculateScore(false, 0, line, out _);
                result.full_area_mail_score = fullArea - noMailArea;
                result.full_line_mail_score = fullLine - noMailLine;
                result.last_letter_area_score = lastArea - noMailArea;
                result.last_letter_line_score = lastLine - noMailLine;
                Require(result.full_area_mail_score == 5 * result.area_score_per_letter &&
                    result.full_line_mail_score == 8 * result.line_score_per_letter,
                    "The two scans see five mailboxes in the area and eight in the row.");
                Require(result.last_letter_area_score == result.area_score_per_letter &&
                    result.last_letter_line_score == result.line_score_per_letter,
                    "Both guided combos count one mailbox when the shared goal needs one letter.");
                result.passed = true;
            }
            catch (Exception error) { result.failure = error.ToString(); }
            File.WriteAllText(TargetScoreReportPath, JsonUtility.ToJson(result, true));
        }

        [Serializable]
        private sealed class TargetScoreReport
        {
            public bool passed;
            public int area_score_per_letter, line_score_per_letter, full_area_mail_score, full_line_mail_score;
            public int last_letter_area_score, last_letter_line_score;
            public string failure = "";
        }

        internal static void RunFlightBatch(FoundationController owner)
        {
            var levelBoard = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var goals = levelBoard.Goals;
            var updates = UnityEngine.Object.FindObjectOfType<OriginalContextUpdateHost>().LateUpdates;
            var check = new FlightBatchReport();
            int startedFrame = -1;
            OriginalMailGoalFlight lower = null, right = null, left = null;
            Require(owner.CurrentLevelNumber == 55 && goals.GetGoalLeftCount(MailGoal) == 50,
                "The flight batch starts on the original level 55.");
            updates.RegisterToLateUpdate(Observe);

            OriginalMailGoalFlight Hit(int x, int y)
            {
                var cell = levelBoard.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem as OriginalMailBoxBoardItem;
                Require(item != null && item.IsActive, "The authored mailbox can collect.");
                cell.ExplodeCurrentItem(OriginalExploder.Next(6));
                var letter = item.ItemView.GetComponentInChildren<OriginalMailGoalFlight>();
                Require(letter != null && letter.Delay == -1f, "A real letter is queued without its batch delay.");
                return letter;
            }

            void Observe()
            {
                try
                {
                    int currentFrame = levelBoard.Clock.FrameCount;
                    if (startedFrame < 0)
                    {
                        startedFrame = currentFrame;
                        lower = Hit(0, 8);
                        right = Hit(8, 10);
                        return;
                    }
                    int elapsed = currentFrame - startedFrame;
                    if (elapsed == 2)
                    {
                        Require(lower.Delay == -1f && right.Delay == -1f,
                            "Two late updates after collection, the original batch still accepts a third letter.");
                        left = Hit(0, 10);
                    }
                    if (elapsed == 3)
                    {
                        Require(left != null && left.Delay == 0f && Mathf.Approximately(right.Delay, .05f) &&
                            Mathf.Approximately(lower.Delay, .1f), "The original batch sorts by row descending and column ascending.");
                        check.assignment_frame = elapsed;
                        check.delays_top_left_to_bottom = new[] { left.Delay, right.Delay, lower.Delay };
                    }
                    Require(elapsed < 240, "The batch of three reaches the HUD.");
                    if (elapsed < 3 || levelBoard.State.HasOperation(5)) return;
                    Require(goals.GetGoalLeftCount(MailGoal) == 47 && goals.GetGoalUiLeftCount(MailGoal) == 47 &&
                        goals.GetFlyingGoalCount(MailGoal) == 0, "The three flights finish and update the HUD once per letter.");
                    updates.UnregisterFromLateUpdate(Observe);
                    owner.StartCoroutine(VerifyTracer());
                }
                catch (Exception error)
                {
                    check.failure = error.ToString();
                    updates.UnregisterFromLateUpdate(Observe);
                    FinishBatch();
                }
            }

            IEnumerator VerifyTracer()
            {
                var anchor = new GameObject("Mailbox tracer test anchor");
                var tracer = levelBoard.Presentation.Pool.Spawn<OriginalMailTracer>(57, true);
                var particles = tracer.GetComponentsInChildren<ParticleSystem>();
                tracer.Follow(anchor.transform);
                anchor.transform.SetPositionAndRotation(new Vector3(.0008f, .0008f, .01f), Quaternion.Euler(0f, 0f, 37f));
                yield return null;
                yield return null;

                Require(particles.Length == 2 && Array.TrueForAll(particles, particle => !particle.isPlaying),
                    "A sub-threshold change in both horizontal axes leaves the mailbox tracer idle even when Z changes.");
                Require(Vector3.Distance(tracer.transform.position, anchor.transform.position) < .00001f &&
                    Quaternion.Angle(tracer.transform.rotation, anchor.transform.rotation) < .01f,
                    "The tracer follows the envelope bone's world position and rotation.");
                check.tracer_ignored_subthreshold_motion = true;
                check.tracer_followed_rotation = true;

                anchor.transform.position = new Vector3(.002f, .0008f, .01f);
                yield return null;
                yield return null;
                Require(Array.TrueForAll(particles, particle => particle.isPlaying && particle.emission.enabled),
                    "A change outside one horizontal axis's tolerance starts both tracer particle systems.");
                tracer.StopFollowing();
                Require(Array.TrueForAll(particles, particle => !particle.emission.enabled),
                    "Arrival turns off both tracer emitters.");
                UnityEngine.Object.Destroy(anchor);
                check.tracer_started_and_stopped = true;
                check.passed = true;
                FinishBatch();
            }

            void FinishBatch()
            {
                File.WriteAllText(BatchReportPath, JsonUtility.ToJson(check, true) + "\n");
            }
        }

        [Serializable]
        private sealed class FlightBatchReport
        {
            public bool passed;
            public bool tracer_ignored_subthreshold_motion, tracer_followed_rotation, tracer_started_and_stopped;
            public int assignment_frame;
            public float[] delays_top_left_to_bottom;
            public string failure;
        }

        public static void Run(FoundationController owner)
        {
            controller = owner;
            game = owner.Gameplay;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            goalManager = board.Goals;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            report = new Report();
            collectedMailbox = null;
            Require(owner.CurrentLevelNumber == 21 && game.Session.Name == "00021v02_L" && game.Moves.LeftMoves == 28,
                "Formal GameScene loads original level 21.");
            var config = OriginalLevelReader.Load("Levels/level_21");
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                if (config.Grid.Items[x + y * board.Grid.Width] != 34) continue;
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem as OriginalMailBoxBoardItem;
                Require(item != null && item.IsActive && item.HasView && item.ItemView.Rig.Renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount > 0,
                    "Every authored mailbox is open and renders its original rig.");
                report.initial_mailboxes++;
            }
            Require(report.initial_mailboxes == 22 && board.Goals.GetGoalLeftCount(MailGoal) == 50 &&
                board.Goals.GetGoalUiLeftCount(MailGoal) == 50, "Level 21 starts with 22 mailboxes and 50 letters to collect.");
            var camera = controller.CoreFlow.UiCamera;
            var center = (board.Grid.GetCell(0, 0).GetViewPosition() + board.Grid.GetCell(7, 0).GetViewPosition()) * .5f;
            Require(Mathf.Abs(camera.WorldToViewportPoint(center).x - .5f) < .001f, "Original eight-column board is centered horizontally.");
            report.board_centered = true;
            Capture("core_mail_level21_start.png");
            mailbox = (OriginalMailBoxBoardItem)board.Grid.GetCell(0, 7).Mediation.CurrentItem;
            nativeSequenceAnimator = new GameObject("Mailbox reference Animator").AddComponent<Animator>();
            nativeSequenceAnimator.runtimeAnimatorController = mailbox.ItemView.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
            nativeSequenceAnimator.Update(0f);
            device.Unbind();
            listener = new OriginalGameTouchListener(input, game);
            phase = Phase.Press;
            Gesture(board.Grid.GetCell(0, 7), board.Grid.GetCell(0, 7));
            started = Time.realtimeSinceStartup;
            frame = -1;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            var host = UnityEngine.Object.FindObjectOfType<OriginalContextUpdateHost>();
            flightContext = host.Context;
            flightLateUpdates = host.LateUpdates;
            flightProbeId = flightContext.GetAllUnits().Length - 1;
            Require(!flightContext.Has(flightProbeId), "The mailbox frame observer has a free context slot.");
            flightContext.AddToFirst(new FlightFrameProbe(flightProbeId));
            flightLateUpdates.RegisterToLateUpdate(VerifyApplicationAnimationFrame);
            goalManager.OnGoalUpdated += ObserveCollectPose;
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
            Require(Time.realtimeSinceStartup - started < 245f, "Mail flow timed out in " + phase);
            if (phase == Phase.NextLevel)
            {
                var owner = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (owner == null || owner == controller || owner.Gameplay == null) return;
                Require(owner.CurrentLevelNumber == 22 && owner.Gameplay.Session.Name == "00022v01_L" && owner.Gameplay.State.IsPlaying(),
                    "The actual Continue Button opens the original playable level 22.");
                report.next_level_verified = true;
                report.passed = true;
                Finish();
                return;
            }
            if (phase == Phase.Press && controller.CoreFlow.BlocksBoardInput)
            {
                var tutorial = controller.CoreFlow.ModalTutorial;
                if (tutorial != null && tutorial.DismissButton.interactable)
                    CoreFlowVerification.Click(tutorial.DismissButton, controller.CoreFlow.UiCamera);
                return;
            }
            var guide = controller.CoreFlow.DirectedTutorial;
            if (guide != null && phase is Phase.Match or Phase.FinishGoals)
            {
                if (guide.IsGuiding && guide.DismissButton != null && guide.DismissButton.interactable)
                {
                    CoreFlowVerification.Click(guide.DismissButton, controller.CoreFlow.UiCamera);
                    report.combo_guide_dismissals++;
                }
                if (step < 3) step = 0;
                return;
            }
            if (step < 3)
            {
                var point = (step == 0 ? from : to).GetViewPosition();
                input.Position = new Vector2(point.x, point.y);
                input.Down = step == 0;
                input.Held = step < 2;
                input.Up = step == 2;
                listener.ManualUpdate();
                if (step == 0 && phase is Phase.Press or Phase.PressAfterCollect)
                {
                    Require(ReferenceEquals(listener.SelectedCell, from),
                        $"The input press selects the actual mailbox. UI touching {game.IsUiTouching}, movable {game.CanUserMakeMove()}.");
                    var view = phase == Phase.Press ? mailbox.ItemView : collectedMailbox;
                    const string tap = "mailbox_tap";
                    float expectedSpeed = phase == Phase.Press ? 1f : 1.1f;
                    float expectedPose = SampleNativePose(tap, expectedSpeed);
                    var animator = view.GetComponentInChildren<Animator>(true);
                    if (phase == Phase.Press)
                    {
                        report.initial_tap_pose = view.Rig.NormalizedTime;
                        report.native_initial_tap_pose = expectedPose;
                    }
                    else
                    {
                        report.tap_after_collect_speed = animator.speed;
                        Require(Mathf.Approximately(report.tap_after_collect_speed, 1.1f),
                            "A mailbox tap retains the collection playback speed.");
                    }
                    Require(animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(tap) &&
                        Mathf.Abs(view.Rig.NormalizedTime - expectedPose) < .0001f,
                        $"The mailbox tap samples the native Play/Update sequence. Actual {view.Rig.NormalizedTime}, expected {expectedPose}.");
                }
                step++;
                return;
            }
            if (phase == Phase.Match && board.Goals.GetGoalLeftCount(MailGoal) < board.Goals.GetGoalUiLeftCount(MailGoal) &&
                board.State.HasOperation(5))
                ObserveOpeningLetter();
            if (phase == Phase.FinishGoals && board.Goals.GetGoalLeftCount(MailGoal) == 0)
            {
                VerifyResult();
                return;
            }
            if (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            { settledAt = -1f; return; }
            if (settledAt < 0f) settledAt = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - settledAt < .35f) return;

            switch (phase)
            {
                case Phase.Press:
                    Require(game.Moves.LeftMoves == 28 && board.Goals.GetGoalLeftCount(MailGoal) == 50 &&
                        audio.Playback.GetClipLastPlayedTime(244) > 0f, "Mailbox press gives original feedback without spending a move or collecting.");
                    report.tap_verified = true;
                    phase = Phase.Reject;
                    Gesture(board.Grid.GetCell(1, 7), board.Grid.GetCell(0, 7));
                    break;
                case Phase.Reject:
                    Require(game.Moves.LeftMoves == 28 && ReferenceEquals(board.Grid.GetCell(0, 7).Mediation.CurrentItem, mailbox),
                        "Dragging toward the mailbox leaves the permanent obstacle in place.");
                    report.fixed_mailbox_verified = true;
                    phase = Phase.Match;
                    Require(CoreCupboardVerification.FindNaturalSwap(board, out var source, out var destination, matchCenterFilter: BordersMailbox),
                        "An authored opening move matches next to a mailbox.");
                    Gesture(source, destination);
                    break;
                case Phase.Match:
                    Require(game.Moves.LeftMoves == 27 && game.AcceptedSwaps == 1 && report.flight_verified,
                        "The legal opening swap spends exactly one move and sends a letter.");
                    Require(report.offset_curve_samples >= 3 && report.maximum_offset_curve_error < .0001f,
                        $"The live shadow and letter follow complementary sine eases. " +
                        $"Samples {report.offset_curve_samples}, maximum error {report.maximum_offset_curve_error}.");
                    Require(report.launch_observed_before_game_update && report.application_offset_samples >= 3,
                        "The launch and local offsets are visible when the game context updates.");
                    SaveFlightCapture();
                    int left = board.Goals.GetGoalLeftCount(MailGoal);
                    Require(left < 50 && left == board.Goals.GetGoalUiLeftCount(MailGoal) && mailbox.HasView,
                        "The arrival updates the HUD and the mailbox stays on the board.");
                    Require(report.collect_pose_matches_native && collectedMailbox != null,
                        $"The mailbox collection samples the native Play/Update sequence before the logical goal changes. " +
                        $"Actual {report.initial_collect_pose}, expected {report.native_initial_collect_pose}.");
                    report.natural_match_letters = 50 - left;
                    phase = Phase.PressAfterCollect;
                    var collectedCell = game.GetCellFromWorldPosition(collectedMailbox.transform.position);
                    Require(collectedCell?.Mediation.CurrentItem is OriginalMailBoxBoardItem collected &&
                        ReferenceEquals(collected.ItemView, collectedMailbox), "Find the source of the first letter.");
                    Gesture(collectedCell, collectedCell);
                    break;
                case Phase.PressAfterCollect:
                    Require(game.Moves.LeftMoves == 27 && board.Goals.GetGoalLeftCount(MailGoal) == 50 - report.natural_match_letters,
                        "Tapping the same mailbox again gives feedback without spending a move or collecting.");
                    phase = Phase.FinishGoals;
                    TapFixtureTnt();
                    break;
                case Phase.FinishGoals:
                    Require(game.Moves.LeftMoves == movesBefore - 1 && board.Goals.GetGoalLeftCount(MailGoal) < mailsBefore,
                        $"TNT action {report.fixture_tnt_actions} at ({from.X}, {from.Y}) must spend a move and collect mail. " +
                        $"Moves {movesBefore} -> {game.Moves.LeftMoves}, mail {mailsBefore} -> {board.Goals.GetGoalLeftCount(MailGoal)}, " +
                        $"input blocked {controller.CoreFlow.BlocksBoardInput}, cell tile {from.Mediation.CurrentItem?.TiledId}.");
                    TapFixtureTnt();
                    break;
            }
        }

        private static void ObserveCollectPose(int goal, int left)
        {
            if (phase != Phase.Match || goal != MailGoal || collectedMailbox != null) return;
            var letter = UnityEngine.Object.FindObjectOfType<OriginalMailGoalFlight>();
            if (letter == null) return;
            collectedMailbox = letter.GetComponentInParent<OriginalMailBoxItemView>();
            if (collectedMailbox == null) return;
            const string collect = "mailbox_acik";
            report.initial_collect_pose = collectedMailbox.Rig.NormalizedTime;
            report.native_initial_collect_pose = SampleNativePose(collect, 1.1f);
            var state = collectedMailbox.GetComponentInChildren<Animator>(true).GetCurrentAnimatorStateInfo(0);
            report.collect_pose_matches_native = state.shortNameHash == Animator.StringToHash(collect) &&
                Mathf.Abs(report.initial_collect_pose - report.native_initial_collect_pose) < .0001f;
        }

        private static float SampleNativePose(string state, float speed)
        {
            nativeSequenceAnimator.speed = speed;
            nativeSequenceAnimator.Play(Animator.StringToHash("Base Layer." + state), 0, 0f);
            nativeSequenceAnimator.Update(Time.deltaTime);
            return Mathf.Clamp01(nativeSequenceAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }

        private static void ObserveOpeningLetter()
        {
            if (firstLetter == null)
            {
                mailGoalPosition = UnityEngine.Object.FindObjectOfType<OriginalGoalPanelUi>().GetGoalPosition(MailGoal);
                float farthest = -1f;
                foreach (var candidate in UnityEngine.Object.FindObjectsOfType<OriginalMailGoalFlight>())
                {
                    float distanceToGoal = ((Vector2)(candidate.transform.position - mailGoalPosition)).sqrMagnitude;
                    if (distanceToGoal <= farthest) continue;
                    farthest = distanceToGoal;
                    firstLetter = candidate;
                }
                Require(firstLetter != null, "A real letter view is spawned before its goal updates.");
                letterView = firstLetter.transform.Find("Root/View");
                letterShadow = letterView.Find("ShadowBoneFollower/Shadow");
                flightStartedAt = Time.realtimeSinceStartup;
            }
            if (!report.flight_verified && Time.realtimeSinceStartup - flightStartedAt >= .62f)
            {
                Require(board.Presentation.Pool.GetSpawnedCount(56) > 0 && board.Presentation.Pool.GetSpawnedCount(57) > 0,
                    "Real letters and original tracer are in flight before the HUD count changes.");
                report.flight_verified = true;
                flightCapture = GrassGameplayVerification.RenderCapture(controller.CoreFlow.UiCamera);
            }
            if (!firstLetter.gameObject.activeInHierarchy) return;
            float distance = Vector2.Distance(firstLetter.transform.position, mailGoalPosition);
            float shadowProgress = (letterShadow.localPosition.y + .6f) / .6f;
            float bodyProgress = (.1f - letterView.localPosition.y) / 2.2f;
            if (bodyProgress >= .15f && bodyProgress <= .85f)
            {
                float remainingShadow = 1f - shadowProgress;
                float error = Mathf.Abs(remainingShadow * remainingShadow + bodyProgress * bodyProgress - 1f);
                report.maximum_offset_curve_error = Mathf.Max(report.maximum_offset_curve_error, error);
                report.offset_curve_samples++;
            }
            if (bodyProgress >= .9999f && shadowProgress >= .9999f)
                report.distance_at_offset_completion = Mathf.Max(report.distance_at_offset_completion, distance);
        }

        private static bool IsObservingLetter() => phase == Phase.Match && firstLetter != null && firstLetter.gameObject.activeInHierarchy;

        private static void VerifyApplicationAnimationFrame()
        {
            if (!IsObservingLetter() || sampledFrame != Time.frameCount) return;
            try
            {
                Require(sampledLetterScale == letterView.localScale,
                    "Mail launch must be applied before the game context update.");
                if (sampledLetterScale.x > 0f) report.launch_observed_before_game_update = true;
                Require(sampledLetterOffset == letterView.localPosition && sampledShadowOffset == letterShadow.localPosition,
                    "Mail offsets must be applied before the game context update.");
                if (sampledLetterOffset.y < .09f && sampledLetterOffset.y > -2f)
                    report.application_offset_samples++;
            }
            catch (Exception error) { report.failure = error.ToString(); Finish(); }
        }

        private sealed class FlightFrameProbe : IOriginalContextBehaviour
        {
            public int Id { get; }
            public FlightFrameProbe(int id) => Id = id;
            public void Bind() { }
            public void ManualUpdate()
            {
                if (!IsObservingLetter()) return;
                sampledFrame = Time.frameCount;
                sampledLetterScale = letterView.localScale;
                sampledLetterOffset = letterView.localPosition;
                sampledShadowOffset = letterShadow.localPosition;
            }
        }

        private static void SaveFlightCapture()
        {
            if (flightCapture == null) return;
            var capture = flightCapture;
            flightCapture = null;
            try { GrassGameplayVerification.SaveCapture(capture, "core_mail_level21_flight.png"); }
            finally { UnityEngine.Object.DestroyImmediate(capture); }
        }

        private static bool BordersMailbox(OriginalGridCell cell)
        {
            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                if (cell.Neighbors.Get(direction)?.Mediation.CurrentItem is OriginalMailBoxBoardItem) return true;
            return false;
        }

        private static void TapFixtureTnt()
        {
            OriginalGridCell source = null;
            int best = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.Mediation.CurrentItem is not OriginalMatchBoardItem ||
                    !OriginalCellTap.CanTapCurrentItem(cell, out _)) continue;
                int hits = 0;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (board.Grid.GetCell(x + dx, y + dy)?.Mediation.CurrentItem is OriginalMailBoxBoardItem) hits++;
                if (hits <= best) continue;
                source = cell;
                best = hits;
            }
            Require(source != null && report.fixture_tnt_actions < 20, "A normal tile can host the explicit TNT fixture next to mailboxes.");
            var match = (OriginalMatchBoardItem)source.Mediation.CurrentItem;
            match.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)match.SwapView);
            game.CreateItemAtCell(40, source);
            movesBefore = game.Moves.LeftMoves;
            mailsBefore = board.Goals.GetGoalLeftCount(MailGoal);
            report.fixture_tnt_actions++;
            Gesture(source, source);
        }

        private static void VerifyResult()
        {
            var dialog = controller.CoreFlow.WinDialog;
            if (dialog == null || !dialog.Lifecycle.IsShowing || dialog.IsAnimationWorking) return;
            Require(game.Session.State == 3 && controller.Progress.LevelToPlay == 22 && !board.State.HasOperation(5) &&
                board.Goals.GetFlyingGoalCount(MailGoal) == 0, "All fifty letters arrive before actual win settlement.");
            int closed = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
                if (board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalMailBoxBoardItem item &&
                    !item.IsActive && item.ItemView.IsClosed && item.HasView) closed++;
            Require(closed == 22 && audio.Playback.GetClipLastPlayedTime(243) > 0f, "All permanent mailboxes close with the original sound.");
            report.closed_mailboxes = closed;
            report.moves_played = game.Moves.MadeMoves;
            Capture("core_mail_level21_win.png");
            phase = Phase.NextLevel;
            CoreFlowVerification.Click(dialog.ContinueButton, controller.CoreFlow.UiCamera);
        }

        private static void Gesture(OriginalGridCell source, OriginalGridCell destination)
        { from = source; to = destination; step = 0; settledAt = -1f; }
        private static void Capture(string file) => GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, file);
        private static void Require(bool condition, string detail)
        { if (!condition) throw new InvalidOperationException(detail); }
        private static void Finish()
        {
            EditorApplication.update -= Tick;
            goalManager.OnGoalUpdated -= ObserveCollectPose;
            if (nativeSequenceAnimator != null) UnityEngine.Object.Destroy(nativeSequenceAnimator.gameObject);
            flightLateUpdates?.UnregisterFromLateUpdate(VerifyApplicationAnimationFrame);
            flightContext?.Remove(flightProbeId);
            SaveFlightCapture();
            if (device != null && device.Listener == null) device.Bind(game);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        }

        private sealed class Samples : IOriginalGameTouchInput
        {
            public Vector2 Position;
            public bool Down, Held, Up;
            public Vector2 GetMouseWorldPosition() => Position;
            public bool GetMouseButtonDown() => Down;
            public bool GetMouseButton() => Held;
            public bool GetMouseButtonUp() => Up;
        }

        [Serializable]
        private sealed class Report
        {
            public bool passed, board_centered, tap_verified, fixed_mailbox_verified, flight_verified, next_level_verified;
            public bool launch_observed_before_game_update, collect_pose_matches_native;
            public int offset_curve_samples, combo_guide_dismissals;
            public int application_offset_samples;
            public float maximum_offset_curve_error, distance_at_offset_completion;
            public float initial_tap_pose, native_initial_tap_pose, initial_collect_pose, native_initial_collect_pose, tap_after_collect_speed;
            public int initial_mailboxes, natural_match_letters, closed_mailboxes, fixture_tnt_actions, moves_played;
            public string failure;
            public string scope = "Formal GameScene opened with isolated progress at level 21. Recorded tap and natural adjacent match, followed by controlled TNT placement and actual tap input. The combo guide closes through its normal button when shown. The real win dialog Continue loads original level 22.";
        }
    }
}
