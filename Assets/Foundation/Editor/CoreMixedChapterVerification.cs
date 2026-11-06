using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;

namespace Whitebox.Foundation.Editor
{
    internal static class CoreMixedChapterVerification
    {
        internal const string BushBurstReportPath = "live_core_bush_burst_verification.json";
        internal const string MergeFirstFrameReportPath = "live_core_merge_first_frame_verification.json";
        internal const string HoneyVaseRevealReportPath = "live_core_honey_vase_reveal_verification.json";
        internal const string GuidedScoreCacheReportPath = "live_core_guided_score_cache_verification.json";

        internal static void VerifyGuidedScoreCache(FoundationController owner)
        {
            var levelBoard = owner.GetComponentInChildren<OriginalInitialBoardView>();
            var playable = owner.Gameplay;
            var point = new OriginalCellPoint(3, 4);
            var cupboard = levelBoard.Grid.GetCell(point.X, point.Y).Mediation.CurrentItem as OriginalCupboardBoardItem;
            Require(owner.CurrentLevelNumber == 99 && cupboard != null && cupboard.HasDoor,
                "The level 99 score cache uses the original closed cupboard.");
            var area = OriginalExploder.Next(13, point);
            var line = OriginalExploder.Next(11, point);
            var incoming = OriginalExploder.Next(8, point);
            var result = new GuidedScoreCacheReport();
            int phase = 0;
            bool reserved = false;
            float deadline = Time.realtimeSinceStartup + 10f;
            EditorApplication.update += Observe;

            void Observe()
            {
                try
                {
                    Require(Time.realtimeSinceStartup < deadline, "The level 99 score cache probe timed out.");
                    if (owner.CoreFlow.IsEnteringLevel) return;
                    int frame = levelBoard.Clock.FrameCount;
                    if (phase == 0)
                    {
                        result.initial_frame = frame;
                        result.initial_score = playable.AreaTargets.CalculateScore(point, area, out _);
                        cupboard.AddIncomingExploder(incoming);
                        reserved = true;
                        result.same_frame_score = playable.AreaTargets.CalculateScore(point, area, out _);
                        Require(result.same_frame_score == result.initial_score,
                            $"Same-frame score changed from {result.initial_score} to {result.same_frame_score}.");
                        playable.LineTargets.CalculateScore(false, point.Y, line, out _);
                        result.after_line_score = playable.AreaTargets.CalculateScore(point, area, out _);
                        Require(result.after_line_score - result.initial_score == 2 * 0x37c88,
                            "The different guided type replaces the two area cache slots in its row.");
                    }
                    else if (phase == 1)
                    {
                        if (frame == result.initial_frame) return;
                        result.next_frame = frame;
                        result.next_frame_score = playable.AreaTargets.CalculateScore(point, area, out _);
                        Require(result.next_frame_score - result.initial_score == 4 * 0x37c88,
                            "The next game frame recomputes the score for all four cupboard cells.");
                        cupboard.RemoveIncomingExploder(incoming);
                        reserved = false;
                        Require(playable.AreaTargets.CalculateScore(point, area, out _) == result.next_frame_score,
                            "Removing the reservation in the same frame retains the cached score.");
                    }
                    else
                    {
                        if (frame == result.next_frame) return;
                        result.restored_score = playable.AreaTargets.CalculateScore(point, area, out _);
                        Require(result.restored_score == result.initial_score, "The following frame reflects the canceled hit.");
                        result.passed = true;
                    }
                    if (++phase < 3) return;
                }
                catch (Exception error) { result.failure = error.ToString(); }
                EditorApplication.update -= Observe;
                if (reserved) cupboard.RemoveIncomingExploder(incoming);
                File.WriteAllText(GuidedScoreCacheReportPath, JsonUtility.ToJson(result, true) + "\n");
            }
        }

        [Serializable]
        private sealed class GuidedScoreCacheReport
        {
            public bool passed;
            public int initial_frame, next_frame, initial_score, same_frame_score, after_line_score, next_frame_score, restored_score;
            public string failure = "";
        }

        internal static void VerifyHoneyVaseReveal(FoundationController owner)
        {
            var report = new HoneyVaseRevealReport();
            var board = owner.GetComponentInChildren<OriginalInitialBoardView>();
            var game = owner.Gameplay;
            var device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            var audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            var center = board.Grid.GetCell(6, 2);
            var vase = center.Mediation.CurrentItem as OriginalVaseBoardItem;
            var honey = center.StaticTouch.GetStaticItem(OriginalHoneyBoardItem.StaticType) as OriginalHoneyBoardItem;
            var input = new Samples();
            var point = center.GetViewPosition();
            input.Position = new Vector2(point.x, point.y);
            device.Unbind();
            var listener = new OriginalGameTouchListener(input, game);
            float previousDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = .01f;
            float deadline = Time.realtimeSinceStartup + 15f;
            float waitUntil = 0f;
            int step = 0;
            SpriteRenderer[] renderers = null;
            EditorApplication.update += Observe;

            void Require(bool condition, string detail)
            {
                if (!condition) throw new InvalidOperationException("Level 85. " + detail);
            }

            void Tap()
            {
                input.Down = true;
                listener.ManualUpdate();
                input.Down = false;
                input.Up = true;
                listener.ManualUpdate();
                input.Up = false;
            }

            void Observe()
            {
                try
                {
                    Require(Time.realtimeSinceStartup < deadline, "The level 85 reveal probe timed out at step " + step + ".");
                    if (owner.CoreFlow.IsEnteringLevel || Time.time < waitUntil || listener.IsTouchDisabled()) return;
                    if (step == 0)
                    {
                        Require(owner.CurrentLevelNumber == 85 && honey != null && vase != null && vase.LayerCount == 2,
                            "The authored cell (6,2) starts with a two-layer vase under honey.");
                        Require(audio.GameAssets.GetClip(192) != null && audio.GameAssets.GetClip(358) != null,
                            "Both original touch clips are available.");
                        float before = audio.Playback.GetClipLastPlayedTime(192);
                        Tap();
                        report.covered_tap = audio.Playback.GetClipLastPlayedTime(192) > before &&
                            audio.Playback.GetClipLastPlayedTime(358) == 0f && game.Moves.MadeMoves == 0;
                        Require(report.covered_tap, "Pressing the cover plays only honey feedback and spends no move.");
                        renderers = honey.View.GetComponentsInChildren<SpriteRenderer>(true);
                        board.Grid.GetCell(6, 3).ExplodeCurrentItem(OriginalExploder.Next(6));
                        board.Grid.GetCell(7, 3).ExplodeCurrentItem(OriginalExploder.Next(6));
                        board.Grid.GetCell(6, 1).ExplodeCurrentItem(OriginalExploder.Next(6));
                        board.Grid.GetCell(7, 1).ExplodeCurrentItem(OriginalExploder.Next(6));
                        waitUntil = Time.time + .15f;
                    }
                    else if (step == 1)
                    {
                        var wave = OriginalExploder.Next(6);
                        center.ExplodeCurrentItem(wave);
                        board.Grid.GetCell(5, 2).ExplodeCurrentItem(wave);
                        center.ExplodeCurrentItem(wave);
                        report.honey_goals_left = board.Goals.GetGoalLeftCount(OriginalHoneyBoardItem.HoneyGoal);
                        report.same_wave_kept_vase = ReferenceEquals(center.Mediation.CurrentItem, vase) && vase.LayerCount == 2 &&
                            !center.StaticTouch.HasTouchBlockingItem() && report.honey_goals_left == 24 &&
                            board.Goals.GetGoalUiLeftCount(OriginalHoneyBoardItem.HoneyGoal) == 24;
                        Require(report.same_wave_kept_vase, "The cover absorbs the wave, updates its goal and leaves the vase intact.");
                    }
                    else if (step == 2)
                    {
                        report.extra_patches = 0;
                        report.stretched_sides = 0;
                        foreach (var renderer in renderers)
                        {
                            if (renderer.enabled && renderer.name.StartsWith("Extra", StringComparison.Ordinal)) report.extra_patches++;
                            if ((renderer.name == "TopLeft" || renderer.name == "BottomLeft") &&
                                renderer.sortingOrder == 2100 && renderer.transform.localScale.y == 1.5f &&
                                renderer.transform.localPosition.x == .25f) report.stretched_sides++;
                        }
                        if (report.stretched_sides != 2) return;
                        waitUntil = Time.time + .12f;
                    }
                    else
                    {
                        Tap();
                        report.exposed_tap = audio.Playback.GetClipLastPlayedTime(358) > 0f &&
                            vase.LayerCount == 2 && game.Moves.MadeMoves == 0;
                        Require(report.exposed_tap, "Tapping the uncovered vase plays its own feedback and spends no move.");
                        Require(report.extra_patches == 0, "A single horizontal honey neighbor stretches each side without extra fill.");
                        report.passed = true;
                    }
                    if (++step < 4) return;
                }
                catch (Exception error) { report.failure = error.ToString(); }
                EditorApplication.update -= Observe;
                Time.captureDeltaTime = previousDelta;
                device.Bind(game);
                File.WriteAllText(HoneyVaseRevealReportPath, JsonUtility.ToJson(report, true));
            }
        }

        [Serializable]
        private sealed class HoneyVaseRevealReport
        {
            public bool passed, covered_tap, exposed_tap, same_wave_kept_vase;
            public int extra_patches, stretched_sides, honey_goals_left;
            public string failure = "";
        }

        internal static void VerifyMergeFirstFrame(FoundationController owner)
        {
            var levelBoard = owner.GetComponentInChildren<OriginalInitialBoardView>();
            var presentation = levelBoard.Presentation;
            var report = new MergeFirstFrameReport();
            OriginalMatchItemSpriteView recycled = null;
            Vector3 replacementPosition = default;
            int startedAt = Time.frameCount;
            float previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = .18f;
            EditorApplication.update += Observe;

            OriginalMatchItemSpriteView SpawnView(int id, int x)
            {
                var model = new OriginalMatchBoardItem(id, 1, 1);
                model.BindViewCreationServices(levelBoard);
                model.CreateView(new OriginalSwapPoint(x, 4, 0f));
                return (OriginalMatchItemSpriteView)model.SwapView;
            }

            void Observe()
            {
                if (Time.frameCount <= startedAt) return;
                try
                {
                    if (recycled == null)
                    {
                        report.first_delta = Time.deltaTime;
                        Require(report.first_delta > .17f && report.first_delta < .2f,
                            "The first frame crosses the merge threshold before the movement duration ends.");
                        var view = SpawnView(-1, 4);
                        view.ExplodeAndMoveToPosition(view.transform.position + Vector3.right, 0, () =>
                        {
                            if (++report.completions == 1) presentation.RecycleView(view);
                        });
                        report.completions_during_start = report.completions;
                        report.merge_handle_after_start = view.HasMergeCoroutine;
                        recycled = SpawnView(-2, 6);
                        report.reused_view = ReferenceEquals(view, recycled);
                        replacementPosition = recycled.transform.position;
                        startedAt = Time.frameCount;
                        return;
                    }
                    if (Time.frameCount < startedAt + 4) return;
                    report.replacement_stayed_put = recycled.transform.position == replacementPosition;
                    Require(report.completions_during_start == 1 && report.completions == 1 && report.reused_view &&
                        report.replacement_stayed_put && !report.merge_handle_after_start,
                        "A synchronous merge returns its view once and cannot move the next owner of the same pooled view.");
                    presentation.RecycleView(recycled);
                    report.passed = true;
                }
                catch (Exception error) { report.failure = error.ToString(); }
                EditorApplication.update -= Observe;
                Time.captureDeltaTime = previousCaptureDelta;
                File.WriteAllText(MergeFirstFrameReportPath, JsonUtility.ToJson(report, true));
            }
        }

        [Serializable]
        private sealed class MergeFirstFrameReport
        {
            public bool passed, merge_handle_after_start, reused_view, replacement_stayed_put;
            public float first_delta;
            public int completions, completions_during_start;
            public string failure = "";
        }

        internal static void VerifyBushBurst(FoundationController owner)
        {
            var report = new BushBurstReport();
            try
            {
                var levelBoard = owner.GetComponentInChildren<OriginalInitialBoardView>();
                var cell = levelBoard.Grid.GetCell(1, 1);
                var bush = cell.Mediation.CurrentItem as OriginalBushBoardItem;
                Require(owner.CurrentLevelNumber == 71 && bush != null && bush.LayerCount == 5,
                    "The fresh original level starts with a five-layer bush at (1,1).");
                report.grass_pool_ready = levelBoard.Presentation.Pool.HasPool(40) &&
                    levelBoard.Presentation.Pool.GetSpawnedCount(40) == 0;
                Require(report.grass_pool_ready, "The level pools grass before any grass view exists on the board.");
                var burst = levelBoard.Presentation.Particles.GetGlobalParticle<OriginalBushBurstParticles>(63);
                var particles = burst.GetComponentsInChildren<ParticleSystem>(true);
                // Isolate the world-space origin from the particle shapes and their random offsets.
                foreach (var system in particles)
                {
                    system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var shape = system.shape;
                    shape.enabled = false;
                    var main = system.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                }
                report.expected = bush.ItemView.Center;
                report.cell_origin = bush.ItemView.transform.position;
                var seed = UnityEngine.Random.state;
                cell.ExplodeCurrentItem(OriginalExploder.Next(6));
                Require(bush.LayerCount == 4, "The authored bush takes one real hit.");
                var sample = new ParticleSystem.Particle[1];
                var observed = new Vector3?[particles.Length];
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i].GetParticles(sample, 1) != 0) observed[i] = sample[0].position;
                    particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                // Compare the same authored emitter at the native origin, including its own offset.
                var afterHit = UnityEngine.Random.state;
                UnityEngine.Random.state = seed;
                burst.Play(4, report.expected);
                UnityEngine.Random.state = afterHit;
                for (int i = 0; i < particles.Length; i++)
                {
                    if (!observed[i].HasValue) continue;
                    Require(particles[i].GetParticles(sample, 1) != 0, "The reference starts the same emitters.");
                    report.emitter_count++;
                    report.reference_particle = sample[0].position;
                    report.observed_particle = observed[i].Value;
                    report.max_distance = Mathf.Max(report.max_distance,
                        Vector3.Distance(report.reference_particle, report.observed_particle));
                }
                Require(report.emitter_count > 0 && report.max_distance < .001f,
                    "The recovered burst emits from the bush view center.");
                VerifyBushSpreadClips(levelBoard, cell, bush, report);
                ObserveBushSpreadPatches(levelBoard, report);
                return;
            }
            catch (Exception error) { report.failure = error.ToString(); }
            File.WriteAllText(BushBurstReportPath, JsonUtility.ToJson(report, true));
        }

        private static void VerifyBushSpreadClips(OriginalInitialBoardView levelBoard, OriginalGridCell cell,
            OriginalBushBoardItem bush, BushBurstReport report)
        {
            for (int layer = bush.LayerCount; layer > 0; layer--)
                cell.ExplodeCurrentItem(OriginalExploder.Next(6));
            int inner = Animator.StringToHash("Base Layer.GrassSpreadAnimation");
            int edge = Animator.StringToHash("Base Layer.GrassSpreadEdgeAnimation");
            foreach (var spread in levelBoard.BoardRoot.GetComponentsInChildren<OriginalGrassSpreadView>())
            {
                var animator = spread.GetComponentInChildren<Animator>();
                int state = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
                if (state == inner) report.inner_spread_clips++;
                else if (state == edge) report.edge_spread_clips++;
            }
            Require(!bush.HasView && levelBoard.Presentation.Pool.GetSpawnedCount(64) == 16 &&
                report.inner_spread_clips == 12 && report.edge_spread_clips == 4,
                "The first bush uses the inner animation for its four center and eight side cells, and the edge animation for its four corners.");
        }

        private static void ObserveBushSpreadPatches(OriginalInitialBoardView levelBoard, BushBurstReport report)
        {
            int edgeState = Animator.StringToHash("Base Layer.GrassSpreadEdgeAnimation");
            var patches = new List<(Animator animator, SpriteRenderer patch, bool edge)>();
            foreach (var spread in levelBoard.BoardRoot.GetComponentsInChildren<OriginalGrassSpreadView>())
            {
                var animator = spread.GetComponentInChildren<Animator>();
                bool edge = animator.GetCurrentAnimatorStateInfo(0).fullPathHash == edgeState;
                patches.Add((animator, animator.transform.Find("TopPatch").GetComponent<SpriteRenderer>(), edge));
                patches.Add((animator, animator.transform.Find("RightPatch").GetComponent<SpriteRenderer>(), edge));
            }
            int previousCaptureRate = Time.captureFramerate;
            Time.captureFramerate = 60;
            float deadline = Time.realtimeSinceStartup + 6f;
            int sampledFrame = Time.frameCount;
            int scriptUpdateCompletedFrame = -1;
            var originalLoop = PlayerLoop.GetCurrentPlayerLoop();
            var observedLoop = originalLoop;
            observedLoop.subSystemList = (PlayerLoopSystem[])originalLoop.subSystemList.Clone();
            int updateIndex = Array.FindIndex(observedLoop.subSystemList, unit => unit.type == typeof(UnityEngine.PlayerLoop.Update));
            var update = observedLoop.subSystemList[updateIndex];
            var steps = new List<PlayerLoopSystem>(update.subSystemList);
            int scriptIndex = steps.FindIndex(unit => unit.type == typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate));
            Require(scriptIndex >= 0, "The Unity loop exposes its normal script update.");
            steps.Insert(scriptIndex + 1, new PlayerLoopSystem
            {
                type = typeof(CoreMixedChapterVerification),
                updateDelegate = () => scriptUpdateCompletedFrame = Time.frameCount
            });
            update.subSystemList = steps.ToArray();
            observedLoop.subSystemList[updateIndex] = update;
            PlayerLoop.SetPlayerLoop(observedLoop);
            levelBoard.Goals.OnGoalUpdated += ObserveGrassLanding;
            EditorApplication.update += Observe;

            void ObserveGrassLanding(int goalType, int remaining)
            {
                if (goalType != GrassGoal) return;
                if (scriptUpdateCompletedFrame == Time.frameCount) report.landings_after_update++;
                else report.landings_in_update++;
            }

            void Observe()
            {
                if (sampledFrame == Time.frameCount) return;
                sampledFrame = Time.frameCount;
                try
                {
                    int waitingPatches = 0;
                    foreach (var (animator, patch, edge) in patches)
                    {
                        if (!patch.enabled || !patch.gameObject.activeInHierarchy) continue;
                        if (animator.speed == 0f) waitingPatches++;
                        else if (edge) report.edge_patch_min_alpha = Mathf.Min(report.edge_patch_min_alpha, patch.color.a);
                        else report.inner_patch_min_alpha = Mathf.Min(report.inner_patch_min_alpha, patch.color.a);
                    }
                    report.waiting_patches = Mathf.Max(report.waiting_patches, waitingPatches);
                    if (levelBoard.Presentation.Pool.GetSpawnedCount(64) != 0)
                    {
                        Require(Time.realtimeSinceStartup < deadline, "The sixteen grass flights must finish.");
                        return;
                    }
                    report.spread_finished = true;
                    Require(report.waiting_patches > 0 && report.inner_patch_min_alpha < .2f && report.edge_patch_min_alpha < .2f,
                        "Pending grass connects the patches and both authored clips keep their alpha curves.");
                    Require(report.landings_in_update == 16 && report.landings_after_update == 0 &&
                        levelBoard.Goals.GetGoalLeftCount(GrassGoal) == 16 && levelBoard.Goals.GetGoalUiLeftCount(GrassGoal) == 16,
                        "All sixteen flights update the grass goals during the original script Update phase.");
                    report.passed = true;
                }
                catch (Exception error) { report.failure = error.ToString(); }
                EditorApplication.update -= Observe;
                levelBoard.Goals.OnGoalUpdated -= ObserveGrassLanding;
                PlayerLoop.SetPlayerLoop(originalLoop);
                Time.captureFramerate = previousCaptureRate;
                File.WriteAllText(BushBurstReportPath, JsonUtility.ToJson(report, true));
            }
        }

        [Serializable]
        private sealed class BushBurstReport
        {
            public bool passed, grass_pool_ready, spread_finished;
            public int emitter_count, inner_spread_clips, edge_spread_clips, waiting_patches;
            public int landings_in_update, landings_after_update;
            public float max_distance;
            public float inner_patch_min_alpha = 1f, edge_patch_min_alpha = 1f;
            public Vector3 expected, cell_origin, reference_particle, observed_particle;
            public string failure = "";
        }

        private static string ReportPath => endLevel switch
        {
            2 or 3 => "live_core_box_chapter_verification.json",
            91 or 100 => "live_core_safe_chapter_verification.json",
            81 or 90 => "live_core_honey_chapter_verification.json",
            80 => "live_core_bush_chapter_verification.json",
            70 => "live_core_owl_chapter_verification.json",
            60 => "live_core_vase_chapter_verification.json",
            40 => "live_core_egg_chapter_verification.json",
            50 => "live_core_potion_chapter_verification.json",
            _ => "live_core_mixed_chapter_verification.json"
        };
        private const int EggGoal = 16, CoinGoal = 21;
        private const int PotionGoal = 19, VaseGoal = 12;
        private const int OwlGoal = 17;
        private const int GrassGoal = 13, BushGoal = 22;
        private static FoundationController controller;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalGoalTuple[] goals;
        private static OriginalGridCell from, to;
        private static readonly Samples input = new Samples();
        private static ChapterReport report;
        private static LevelReport current;
        private static OriginalGridShakeAnimation projectionShake;
        private static Vector3 boardRestPosition;
        private static OriginalEggBoardItem tappedEgg, fallingEgg;
        private static Transform eggPivot;
        private static OriginalPotionBoardItem openedRack, tntRack;
        private static Animator potionAnimator;
        private static int potionOpeningStep, tntRackLayers, endLevel;
        private static int eggOpeningStep;
        private static int vaseOpeningStep;
        private static OriginalVaseBoardItem tappedVase, crackedVase, fallingVase;
        private static int fallingVaseStartY;
        private static Transform vaseBase;
        private static Transform vaseShakeFirst, vaseShakeSecond;
        private static float vaseShakeFirstPeak, vaseShakePrevious;
        private static bool vaseShakeOppositeSeen;
        private static OriginalVaseVisualAssets vaseVisuals;
        private static OriginalOwlStatueVisualAssets owlVisuals;
        private static OriginalOwlStatueBoardItem tappedOwl;
        private static Transform owlPivot;
        private static int owlOpeningStep, lastAuthoredVaseId;
        private static bool checkingOwlMatch;
        private static float owlRocketRefillDeadline;
        private static RenderTexture owlRocketCapture;
        private static float cupboardDoorSeenAt = -1f;
        private static OriginalBushBoardItem firstBush;
        private static int bushOpeningStep, bushLayersBeforeMatch;
        private static float bushSpreadCaptureAt = -1f;
        private static bool bushSpreadCaptured;
        private static OriginalGrassBoardItem[,] observedGrass;
        private static int[,] observedGrassLayers;
        private static OriginalCoinBoardItem[,] shuffleCoins;
        private static OriginalKingVisual coinKing;
        private static int shuffleMadeMoves, shuffleLeftMoves, shuffleCoinGoal;
        private static float shuffleCaptureAt;
        private static bool shuffleSeen, shuffleCaptured;
        private static OriginalHoneyBoardItem honeyBefore;
        private static OriginalHoneyItemView honeyBeforeView;
        private static OriginalBoardItem honeyUnderlay;
        private static OriginalBoxBoardItem[,] coveredBoxes;
        private static int honeyOpeningStep, safeOpeningStep;
        private static int safeFirstDamageFrame = -1;
        private static OriginalSafeBoardItem openingSafe;
        private static OriginalTntBoardItem fillingTnt;
        private static bool restartAfterCap;
        private static int level, frame, sample, ownerId, goalBefore, movesBefore;
        private static float started, settledAt;
        private static bool opening, moving, reloading, sawOriginalCombo;

        public static void Run(FoundationController owner, int chapterEnd = 0)
        {
            controller = owner;
            level = owner.CurrentLevelNumber;
            Require(level == 2 || level == 22 || level == 31 || level == 41 || level == 51 || level == 61 || level == 71 || level == 80 || level == 81 || level == 91 || level == 100,
                "Start from the authored chapter entry.");
            endLevel = chapterEnd != 0 ? chapterEnd : level == 81 ? 90 : level >= 71 ? 80 : level == 61 ? 70 : level == 51 ? 60 : level == 41 ? 50 : level == 31 ? 40 : 30;
            report = new ChapterReport();
            if (endLevel == 3)
                report.scope = "Actual level 2 and 3 board, recorded first legal color swaps, controlled power placements, real wins and the standard Continue to level 3.";
            if (endLevel == 40)
                report.scope = "Actual GameScene from level 31 using real Continue buttons. Records Egg tap, rejected swap, adjacent match, fall, valid swap, controlled TNT placements, the original level 38 propellers and the level 40 coin board.";
            if (endLevel == 50)
                report.scope = "Actual GameScene from level 41 using real Continue buttons. Records potion tap, natural color hit, controlled TNT placements, the original level 46 Egg refill set and the preset level 50 rocket.";
            if (endLevel >= 60)
            {
                vaseVisuals = Resources.Load<OriginalVaseVisualAssets>("Foundation/VaseVisualAssets");
                Require(vaseVisuals != null, "Original vase visual assets are loaded.");
            }
            if (endLevel == 60)
                report.scope = "Actual GameScene from level 51 using real Continue buttons. Records vase tap, rejected swap, authored color match, accepted vase swap and two-layer damage. A controlled hit under a vase verifies the production fall. Ordinary boards also use controlled TNT placements. Level 60 uses twenty legal moves on the original coin board.";
            if (endLevel >= 70)
            {
                owlVisuals = Resources.Load<OriginalOwlStatueVisualAssets>("Foundation/OwlStatueVisualAssets");
                Require(owlVisuals != null, "Original owl visual assets are loaded.");
            }
            if (endLevel == 70)
                report.scope = "Actual GameScene from level 61 using real Continue buttons. Records owl tap, blocked drag, authored rocket and TNT, and an adjacent match. Verifies dynamic Vase and Egg refills, authored Propeller pair, and ordinary wins using controlled TNT placements.";
            if (endLevel == 80)
                report.scope = "Actual GameScene from level 71 using real Continue buttons. Records bush tap, rejected drag, an adjacent match, controlled TNT placements, real grass spread and completion. Level 80 uses twenty legal moves on the original coin board.";
            if (level >= 91) report.scope = "Actual GameScene with original Safe boards and level 100. Uses authored gestures, controlled specials on ordinary boards, and original level 100 gestures through the terminal result.";
            if (level == 81) report.scope = "Actual GameScene using the original honey boards and Continue buttons. Records an authored level 81 swap, covered-tile drag, honey reveal, and controlled placements for ordinary completion.";
            started = Time.realtimeSinceStartup;
            frame = -1;
            SceneManager.sceneLoaded += SceneLoaded;
            EditorApplication.update += Tick;
            BeginLevel();
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode) =>
            controller = UnityEngine.Object.FindObjectOfType<FoundationController>();

        private static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try
            {
                Require(Time.realtimeSinceStartup - started < (endLevel != 30 ? 750f : 500f), "Chapter timed out at level " + level);
                Advance();
            }
            catch (Exception error)
            {
                report.failure = error.ToString();
                Finish();
            }
        }

        private static void BeginLevel()
        {
            game = controller.Gameplay;
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            var config = OriginalLevelReader.Load("Levels/level_" + level);
            Require(controller.CurrentLevelNumber == level && game.Session.Name == config.Name && game.State.IsPlaying() &&
                game.Moves.LeftMoves == config.Move && game.WinFail == controller.CoreFlow.WinFail,
                "Formal scene opens the original board and normal result owner.");
            goals = board.Goals.GetFinalGoals();
            Require(goals.Length == config.Goals.Length, "All original goals appear together.");
            foreach (var goal in config.Goals)
                Require(board.Goals.GetGoalLeftCount(goal.Goal) == goal.Count &&
                    board.Goals.GetGoalUiLeftCount(goal.Goal) == goal.Count, "Original starting goal counts.");

            current = new LevelReport { level = level, width = board.Grid.Width, height = board.Grid.Height,
                target_count = TotalGoals(), move_budget = config.Move };
            fillingTnt = null;
            lastAuthoredVaseId = -1;
            report.levels.Add(current);
            coveredBoxes = level == 83 ? new OriginalBoxBoardItem[board.Grid.Width, board.Grid.Height] : null;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                int index = x + y * board.Grid.Width;
                int id = config.Grid.Items[index];
                var cell = board.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                bool grass = cell.StaticTouch.HasStaticItem(2);
                Require(grass == (config.Grid.Cells[index].Grass != 0), "Original grass cell layout.");
                if (grass) current.grass++;
                if (endLevel >= 81)
                {
                    bool hasHoney = cell.StaticTouch.GetStaticItem(4) is OriginalHoneyBoardItem cover && cover.HasView;
                    Require(hasHoney == (config.Grid.Cells[index].Honey == 33), "Original honey tile layout and views.");
                    if (hasHoney) current.honey++;
                }
                if (id == 34)
                {
                    Require(item is OriginalMailBoxBoardItem mailbox && mailbox.IsActive && mailbox.HasView,
                        "Original mailbox is active on its authored tile.");
                    current.mailboxes++;
                }
                else if (id == 51)
                {
                    Require(item is OriginalCupboardBoardItem cupboard && cupboard.HasDoor && cupboard.PlateCount == 10 && cupboard.HasView,
                        "Original cupboard master holds ten plates.");
                    current.cupboards++;
                    if (level == 45 && !report.cupboard_win_forecast_verified)
                        VerifyCupboardWinForecast(cell, (OriginalCupboardBoardItem)item);
                }
                else if (id == 41 || id == 42 || id == 52)
                    Require(item is OriginalCupboardFakeBoardItem proxy && proxy.Master != null && proxy.HasView,
                        "Original cupboard footprint is bound to its master.");
                else if (id >= 21 && id <= 24)
                {
                    Require(item is OriginalBoxBoardItem box && box.LayerCount == id - 20 && box.HasView,
                        "Original box layers are present.");
                    if (coveredBoxes != null && cell.StaticTouch.HasStaticItem(4)) coveredBoxes[x, y] = (OriginalBoxBoardItem)item;
                    current.boxes++;
                    if (level == 43 && !report.box_win_forecast_verified)
                    {
                        VerifySingleGoalForecast(item, cell);
                        report.box_win_forecast_verified = true;
                    }
                }
                else if (id == 50)
                    Require(item is OriginalLightballBoardItem && item.HasView, "The original lightball has its real view.");
                else if (id == 26)
                {
                    Require(item is OriginalEggBoardItem egg && egg.HasView && egg.LayerCount == 1,
                        "The original egg is created at its authored position.");
                    current.eggs++;
                }
                else if (id == 25)
                {
                    Require(item is OriginalVaseBoardItem vase && vase.HasView && vase.LayerCount == 2 &&
                        vase.ItemView.CurrentSprite == vaseVisuals.layerSprites[1],
                        "The original vase has two layers and its intact sprite.");
                    current.vases++;
                    lastAuthoredVaseId = Math.Max(lastAuthoredVaseId, item.Id);
                }
                else if (id == 27)
                {
                    Require(item is OriginalOwlStatueBoardItem owl && owl.HasView && owl.LayerCount == 1 &&
                        owl.ItemView.CurrentSprite == owlVisuals.layerSprites[0],
                        "The original single-layer owl is present with its correct sprite.");
                    current.owls++;
                }
                else if (id == 57)
                {
                    Require(item is OriginalSafeBoardItem safe && safe.LayerCount == 5 && safe.HasView, "The original safe master has five layers.");
                    current.safes++;
                }
                else if (id == 47 || id == 48 || id == 58)
                    Require(item is OriginalSafeFakeBoardItem safeProxy && safeProxy.Master != null && safeProxy.HasView, "Safe proxy shares the original master.");
                else if (id == 60)
                {
                    Require(item is OriginalCoinBoardItem coin && coin.HasView && coin.ItemView.baseView.sprite != null,
                        "The original coin is visible at its authored position.");
                    current.coins++;
                }
                else if (id == 10)
                {
                    Require(item is OriginalPropellerBoardItem && item.HasView, "The original propeller has its real view.");
                    current.propellers++;
                }
                else if (id == 53)
                {
                    Require(item is OriginalPotionBoardItem potion && potion.HasView && potion.LayerCount == 4 &&
                        potion.HasBottle(1) && potion.HasBottle(2) && potion.HasBottle(3) && potion.HasBottle(4),
                        "Every original potion master starts with four colored bottles.");
                    current.potion_racks++;
                }
                else if (id == 43 || id == 44 || id == 54)
                    Require(item is OriginalPotionFakeBoardItem proxy && proxy.Master != null && proxy.HasView,
                        "The other three original cells are bound to their potion master.");
                else if (id == 55)
                {
                    Require(item is OriginalBushBoardItem bush && bush.HasView && bush.LayerCount == 5 &&
                        bush.ItemView.Rig.Renderer.sharedMaterial.mainTexture.name == "bush_export",
                        "The bush starts with five layers and its original atlas on a Unity mesh.");
                    current.bushes++;
                }
                else if (id == 45 || id == 46 || id == 56)
                    Require(item is OriginalBushFakeBoardItem proxy && proxy.Master != null && proxy.HasView,
                        "Each original bush proxy shares the real master.");
            }
            var center = (board.Grid.GetCell(0, 0).GetViewPosition() +
                board.Grid.GetCell(board.Grid.Width - 1, board.Grid.Height - 1).GetViewPosition()) * .5f;
            var viewport = controller.CoreFlow.UiCamera.WorldToViewportPoint(center);
            Require(Mathf.Abs(viewport.x - .5f) < .001f && Mathf.Abs(center.y + .75f) < .001f,
                "The 720x1280 portrait board uses the native center and vertical offset.");
            current.center_y = center.y;
            if (level == 25 || level == 28 || level == 29) Capture("core_mixed_level" + level + "_start.png");
            if (level == 31 || level == 38 || level == 40) Capture("core_egg_level" + level + "_start.png");
            if (level == 41 || level == 45 || level == 50) Capture("core_potion_level" + level + "_start.png");
            if (level == 51 || level == 55 || level == 59 || level == 60) Capture("core_vase_level" + level + "_start.png");
            if (level == 61 || level == 65 || level == 68 || level == 70) Capture("core_owl_level" + level + "_start.png");
            if (level == 71 || level == 74 || level == 78 || level == 80) Capture("core_bush_level" + level + "_start.png");
            if (current.bushes != 0)
                Require(board.Goals.HasPrerequisite(GrassGoal) && board.Presentation.Pool.GetSpawnedCount(62) == current.bushes,
                    "The bush views hold the grass prerequisite before play.");
            if (endLevel == 80 && current.bushes != 0)
            {
                if (report.bush_shadow_components == 0)
                {
                    var burst = Resources.Load<GameObject>("Recovered/GameObject/BushItemExplodeParticles");
                    Require(burst != null, "The original bush debris prefab is loaded.");
                    var shadows = burst.GetComponentsInChildren<OriginalAutoShadowEmitParticles>(true);
                    Require(shadows.Length == 17 && Array.TrueForAll(shadows, shadow => shadow.system != null),
                        "All seventeen recovered bush debris emitters have their original shadow sources.");
                    report.bush_shadow_components = shadows.Length;
                }
                observedGrass = new OriginalGrassBoardItem[board.Grid.Width, board.Grid.Height];
                observedGrassLayers = new int[board.Grid.Width, board.Grid.Height];
            }
            else observedGrass = null;
            if (level == 81 || level == 83 || level == 85 || level == 90)
                Capture("core_honey_level" + level + "_start.png");
            if (current.honey != 0) Require(board.Presentation.Pool.GetSpawnedCount(43) == current.honey, "Every authored honey cell has a live pooled view.");
            if (level == 81)
            {
                VerifyPendingHoneyAreaScore();
                var burst = Resources.Load<GameObject>("Recovered/GameObject/HoneyItemExplodeParticles");
                Require(burst != null, "The original honey debris prefab is loaded.");
                var shadows = burst.GetComponentsInChildren<OriginalAutoShadowEmitParticles>(true);
                Require(shadows.Length == 3 && Array.TrueForAll(shadows, shadow => shadow.system != null),
                    "All three recovered honey debris shadow sources are connected.");
            }
            if (level == 91 || level == 95 || level == 98 || level == 100) Capture("core_safe_level" + level + "_start.png");
            if (current.safes != 0) Require(board.Presentation.Pool.GetSpawnedCount(65) == current.safes, "Each safe uses the original pooled view.");
            if (level == 80)
            {
                shuffleCoins = new OriginalCoinBoardItem[board.Grid.Width, board.Grid.Height];
                coinKing = UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().KingVisual;
                var bottom = UnityEngine.Object.FindObjectOfType<OriginalBottomUi>();
                Require(bottom != null && bottom.IsInitialized && bottom.Boosters.Length == 4,
                    "The late board restores all four original tool slots.");
                foreach (var slot in bottom.Boosters)
                    Require(slot.Presentation.Unlocked && slot.Icon.sprite != null && !slot.Presentation.ShowAdd,
                        "The unlocked tool icons display without a purchase action.");
                report.bonus_bottom_tools_verified = true;
            }
            if (level == 61)
                Require(current.owls == 85 && board.Presentation.Pool.GetSpawnedCount(53) == 85,
                    "All 85 authored owls have a live pooled view.");
            if (level == 41)
                Require(current.potion_racks == 4 && board.Presentation.Pool.GetSpawnedCount(59) == 16,
                    "The four racks use sixteen real pooled bottle views.");
            if (level == 40 || level == 60 || level == 80 || level == 100)
                Require(game.Session.IsBonus && controller.CoreFlow.IsBonus && current.coins == (level == 40 ? 70 : level == 60 ? 36 : level == 80 ? 42 : 45) &&
                    UnityEngine.Object.FindObjectOfType<OriginalGameplayHud>().TargetTitle.text == "Reward",
                    "The authored coin board displays Reward with its original coin layout.");
            if (level == 22)
            {
                projectionShake = board.BoardRoot.GetComponent<OriginalGridShakeAnimation>();
                Require(projectionShake != null, "The live board owns the original shake animation.");
                boardRestPosition = board.BoardRoot.localPosition;
                board.ShakeGrid(new OriginalTntShakeConfig
                {
                    Duration = .2f, MinVibrate = .125f, MaxVibrate = .125f, Direction = 2
                });
            }

            device.Unbind();
            listener = new OriginalGameTouchListener(input, game);
            sample = 3;
            settledAt = -1f;
            opening = true;
            moving = reloading = sawOriginalCombo = false;
            WriteReport();
        }

        private static void Advance()
        {
            if (reloading)
            {
                if (controller == null || controller.GetInstanceID() == ownerId || controller.Gameplay == null) return;
                if (restartAfterCap)
                {
                    Require(controller.CurrentLevelNumber == 1 && controller.Progress.LevelToPlay == 1 && !controller.Progress.IsComplete,
                        "The actual terminal Continue button opens the original first level again.");
                    report.level_cap_restarts = true; report.passed = true; Finish();
                    return;
                }
                BeginLevel();
                return;
            }
            var tutorial = controller.CoreFlow.ModalTutorial;
            if (tutorial != null && tutorial.BlocksBoardInput)
            {
                Require(game.IsUiTouching, "The actual modal tutorial blocks the board.");
                if (!tutorial.DismissButton.interactable) return;
                Capture("core_tutorial_level" + level + ".png");
                CoreFlowVerification.Click(tutorial.DismissButton, controller.CoreFlow.UiCamera);
                return;
            }
            if (tutorial != null)
            {
                if (level == 81) report.honey_tutorial_dismissed = true;
                if (level == 91) report.safe_tutorial_dismissed = true;
            }
            if (controller.CoreFlow.BlocksBoardInput && opening && tutorial == null) return;
            var directed = controller.CoreFlow.DirectedTutorial;
            if (level == 61 && directed != null && directed.IsGuiding)
            {
                if (directed.DismissButton.interactable)
                    CoreFlowVerification.Click(directed.DismissButton, controller.CoreFlow.UiCamera);
                return;
            }
            if (projectionShake != null && !VerifyProjectionDuringShake()) return;
            if (level == 100 && !report.tnt_fuse_unmasked_on_landing) ObserveTntRefill();
            if ((level == 28 || level == 40 || level == 100) && game.BallBallCombo.IsComboActive && !sawOriginalCombo)
            {
                sawOriginalCombo = true;
                Capture(level == 100 ? "core_safe_level100_combo.png" : level == 28 ? "core_mixed_level28_combo.png" : "core_egg_level40_combo.png");
            }
            ObserveEggOpening();
            ObservePotionFlow();
            ObserveVaseFlow();
            ObserveOwlFlow();
            ObserveBushFlow();
            if (level == 91 && safeOpeningStep == 3 && openingSafe.LayerCount == 4 && !report.safe_handle_depth_verified)
            {
                if (safeFirstDamageFrame < 0) safeFirstDamageFrame = Time.frameCount;
                else if (Time.frameCount >= safeFirstDamageFrame + 2) VerifySafeHandleParticleDepth();
            }
            if (level == 80)
            {
                ObserveCoinShuffle();
                if (game.State.IsPlaying() &&
                    (coinKing.CurrentAnimation == "idle_connect_1" || coinKing.CurrentAnimation == "idle_connect_2"))
                    report.bonus_king_idle_connector_verified = true;
                if (game.State.IsWin() && coinKing.CurrentAnimation == "k-1sevinme_6")
                {
                    Require(game.Session.State == 3 && !coinKing.IsAnimationLooping,
                        "The bonus board celebrates after its settlement starts.");
                    report.bonus_king_celebration_verified = true;
                }
            }
            if (coveredBoxes != null && !report.honey_reveals_intact_box) ObserveCoveredBoxes();
            if (sample < 3)
            {
                var point = (sample == 0 ? from : to).GetViewPosition();
                input.Position = new Vector2(point.x, point.y);
                input.Down = sample == 0;
                input.Held = sample < 2;
                input.Up = sample == 2;
                listener.ManualUpdate();
                sample++;
                return;
            }
            if (game.Session.IsBonus ? game.Moves.LeftMoves == 0 : TotalGoals() == 0)
            {
                VerifyResult();
                return;
            }
            Require(game.Moves.LeftMoves > 0 && controller.CoreFlow.FailDialog == null,
                "Level exhausted its real move budget with " + TotalGoals() + " targets left.");
            if (!game.Fall.IsAllItemsSteady || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch)
            {
                settledAt = -1f;
                return;
            }
            if (settledAt < 0f) settledAt = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - settledAt < .30f || !game.CanUserMakeMove()) return;
            if (moving)
                Require(game.Moves.LeftMoves == movesBefore - 1,
                    "The recorded gesture spends one move. Before " + movesBefore + ", after " + game.Moves.LeftMoves);
            if (level == 31 && eggOpeningStep < 5)
            {
                AdvanceEggOpening();
                return;
            }
            if (level == 41 && potionOpeningStep < 4)
            {
                AdvancePotionOpening();
                return;
            }
            if (level == 51 && vaseOpeningStep < 5)
            {
                AdvanceVaseOpening();
                return;
            }
            if (level == 61 && owlOpeningStep < 5)
            {
                if (owlOpeningStep == 3 && game.CreatedRefills == 0 && board.Clock.Time < owlRocketRefillDeadline) return;
                AdvanceOwlOpening();
                return;
            }
            if (level == 62 && checkingOwlMatch)
            {
                Require(board.Goals.GetGoalLeftCount(EggGoal) < current.eggs &&
                    board.Goals.GetGoalLeftCount(OwlGoal) == current.owls &&
                    board.Goals.GetGoalUiLeftCount(OwlGoal) == current.owls,
                    "The adjacent match breaks eggs and leaves all the owls intact.");
                report.owl_resists_adjacent_match = true;
                checkingOwlMatch = false;
            }
            if (level == 71 && bushOpeningStep < 5)
            {
                AdvanceBushOpening();
                return;
            }
            if (level == 91 && safeOpeningStep < 4)
            {
                AdvanceSafeOpening();
                return;
            }
            if (level == 81 && honeyOpeningStep < 3)
            {
                AdvanceHoneyOpening();
                return;
            }
            if (opening)
            {
                opening = false;
                if (level == 28 || level == 40 || level == 100)
                {
                    int row = level == 28 ? 5 : 4;
                    int left = level == 100 ? 6 : 3;
                    var first = board.Grid.GetCell(left, row);
                    var second = board.Grid.GetCell(left + 1, row);
                    Require(first.Mediation.CurrentItem is OriginalLightballBoardItem &&
                        second.Mediation.CurrentItem is OriginalLightballBoardItem, "Use the authored lightball pair.");
                    current.natural_actions++;
                    Gesture(first, second);
                    return;
                }
                if (level == 38)
                {
                    var first = board.Grid.GetCell(3, 6);
                    var second = board.Grid.GetCell(4, 6);
                    Require(first.Mediation.CurrentItem is OriginalPropellerBoardItem &&
                        second.Mediation.CurrentItem is OriginalPropellerBoardItem, "Use two of the original level 38 propellers.");
                    current.natural_actions++;
                    Gesture(first, second);
                    return;
                }
                if (level == 50)
                {
                    var cell = board.Grid.GetCell(8, 1);
                    Require(cell.Mediation.CurrentItem is OriginalRocketBoardItem, "Use the original level 50 rocket.");
                    current.natural_actions++;
                    Gesture(cell, cell);
                    return;
                }
                if (level == 62)
                {
                    checkingOwlMatch = true;
                    current.natural_actions++;
                    Gesture(board.Grid.GetCell(3, 4), board.Grid.GetCell(2, 4));
                    return;
                }
                if (level == 70)
                {
                    var first = board.Grid.GetCell(0, 8);
                    var second = board.Grid.GetCell(1, 8);
                    Require(first.Mediation.CurrentItem is OriginalPropellerBoardItem &&
                        second.Mediation.CurrentItem is OriginalPropellerBoardItem, "The original propeller pair opens level 70.");
                    current.natural_actions++;
                    Gesture(first, second);
                    return;
                }
                if ((level == 2 || level == 3) &&
                    CoreCupboardVerification.FindNaturalSwap(board, out var firstBoxMove, out var secondBoxMove))
                {
                    current.natural_actions++;
                    Gesture(firstBoxMove, secondBoxMove);
                    return;
                }
                if (level == 22 &&
                    CoreCupboardVerification.FindNaturalSwap(board, out var source, out var target, matchCenterFilter: BordersMailbox))
                {
                    current.natural_actions++;
                    Gesture(source, target);
                    return;
                }
            }
            if (game.Session.IsBonus)
            {
                PlayNaturalMove();
                return;
            }

            bool pair = level == 22 || level == 30 || endLevel != 30;
            ChooseTnt(pair, out var center, out var adjacent);
            if (center == null && (endLevel == 3 || level == 61 || level == 70 || endLevel >= 80))
            {
                PlayNaturalMove();
                return;
            }
            Require(center != null, "No reachable fixture center. Level " + level + ", goals " + TotalGoals() +
                ", preceding goals " + goalBefore + ", remaining moves " + game.Moves.LeftMoves);
            PlaceSpecial(center);
            if (adjacent != null) PlaceSpecial(adjacent);
            current.fixture_actions++;
            if (adjacent != null) current.fixture_combos++;
            Gesture(adjacent ?? center, center);
        }

        private static bool VerifyProjectionDuringShake()
        {
            var position = board.BoardRoot.localPosition;
            if (position == boardRestPosition) return false;
            var parent = board.BoardRoot.parent;
            var target = board.Grid.GetCell(4, 7);
            var localProbe = boardRestPosition + new Vector3(3.55f, 6.6f, 0f);
            Vector2 probe = parent.TransformPoint(localProbe);
            Require(target != null && game.GetCellFromWorldPosition(probe) == target,
                "A touch just inside the next column still selects that cell during the visual shake.");
            board.SetBoardPortraitOffset(-1f);
            Require(board.BoardRoot.localPosition == position + Vector3.down * .25f,
                "A new portrait offset keeps the current horizontal shake displacement.");
            Vector2 movedProbe = parent.TransformPoint(localProbe + Vector3.down * .25f);
            Require(game.GetCellFromWorldPosition(movedProbe) == target,
                "The touch origin follows the new portrait offset during the shake.");
            UnityEngine.Object.FindObjectOfType<OriginalGameSceneProjection>().RefreshProjection();
            Require(board.BoardRoot.localPosition == position, "Projection refresh preserves the current shake displacement.");
            Require(game.GetCellFromWorldPosition(probe) == target, "Projection refresh restores the original touch origin.");
            projectionShake.StopShaking();
            Require(board.BoardRoot.localPosition == boardRestPosition, "Shake returns to the original centered pose.");
            Require(game.GetCellFromWorldPosition(probe) == target, "The same touch still selects that cell after the shake.");
            report.touch_origin_during_shake_verified = true;
            report.projection_during_shake_verified = true;
            projectionShake = null;
            return true;
        }

        private static void ObserveEggOpening()
        {
            if (level != 31) return;
            if (eggOpeningStep == 1 && Quaternion.Angle(eggPivot.localRotation, Quaternion.identity) > 1f)
                report.egg_tap_verified = true;
            if (eggOpeningStep != 3) return;
            if (fallingEgg.HasView && fallingEgg.ItemView.LocalPosition.Y < 5.99f)
                report.egg_fall_verified = true;
            if (board.Presentation.Pool.GetSpawnedCount(52) == 1 && !report.egg_burst_verified)
            {
                report.egg_burst_verified = true;
                Capture("core_egg_level31_break.png");
            }
        }

        private static void ObservePotionFlow()
        {
            if (level == 41 && potionOpeningStep == 1 &&
                potionAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash("PotionsTapAnimation"))
                report.potion_tap_animation = true;
            if (level == 41 && potionOpeningStep == 2 && !report.potion_color_hit && openedRack.LayerCount == 3)
            {
                Require(!openedRack.HasBottle(2) && openedRack.HasBottle(1) && openedRack.HasBottle(3) && openedRack.HasBottle(4) &&
                    board.Goals.GetGoalLeftCount(PotionGoal) == board.Goals.GetGoalUiLeftCount(PotionGoal) &&
                    board.Presentation.Pool.GetSpawnedCount(60) == 1, "The green match removes only the green bottle.");
                report.potion_color_hit = true;
                Capture("core_potion_level41_bottle.png");
            }
            if (level != 46 || report.dynamic_egg_refill || game.CreatedRefills == 0) return;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                if (!(board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalEggBoardItem egg) || !egg.HasView) continue;
                report.dynamic_egg_refill = true;
                Capture("core_potion_level46_refill.png");
                return;
            }
        }

        private static void ObserveOwlFlow()
        {
            if (level == 61)
            {
                if (owlOpeningStep == 1 && Quaternion.Angle(owlPivot.localRotation, Quaternion.identity) > 1f)
                    report.owl_tap = true;
                if (owlOpeningStep == 3 && !report.owl_rocket_burst &&
                    board.Goals.GetGoalLeftCount(OwlGoal) < current.owls &&
                    board.Presentation.Pool.GetSpawnedCount(54) == 1)
                {
                    report.owl_rocket_burst = true;
                    // Preserve the burst frame and read it back after flight so
                    // PNG encoding cannot stall the next projectile update.
                    owlRocketCapture = GrassGameplayVerification.RenderCapture(controller.CoreFlow.UiCamera);
                }
                return;
            }
            if (level == 64 && !report.cupboard_door_materials && board.Presentation.Pool.GetSpawnedCount(48) > 0)
                ObserveCupboardDoorMaterials();
            bool vaseRefill = level == 66 && !report.dynamic_vase_refill;
            bool eggRefill = level == 70 && !report.dynamic_owl_chapter_egg_refill;
            if ((!vaseRefill && !eggRefill) || game.CreatedRefills == 0) return;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var item = board.Grid.GetCell(x, y).Mediation.CurrentItem;
                if (vaseRefill && item is OriginalVaseBoardItem vase && vase.Id > lastAuthoredVaseId && vase.HasView)
                {
                    report.dynamic_vase_refill = true;
                    Capture("core_owl_level66_refill.png");
                    return;
                }
                if (eggRefill && item is OriginalEggBoardItem egg && egg.HasView)
                {
                    report.dynamic_owl_chapter_egg_refill = true;
                    Capture("core_owl_level70_refill.png");
                    return;
                }
            }
        }

        private static void ObserveCupboardDoorMaterials()
        {
            if (cupboardDoorSeenAt < 0f) cupboardDoorSeenAt = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - cupboardDoorSeenAt < .12f) return;
            var effect = UnityEngine.Object.FindObjectOfType<OriginalCupboardDoorParticles>();
            Require(effect != null, "The live cupboard plays its door prefab.");
            int litDoors = 0, translucentShadows = 0;
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                var material = renderer.sharedMaterial;
                if (material.name == "Cupboard_Door_Particle")
                {
                    Require(material.shader.name == "Shader Graphs/Cupboard_Door_Particle" && material.shader.isSupported &&
                        material.GetTexture("_SampleReflectedCubemap_996c0d7a9fa646acaab13bc9c71de387_Cube_1") is Cubemap &&
                        material.GetTexture("_SampleTexture2D_fc9181a81b614b6ba6d6cbe6f6b778cb_Texture_1") is Texture2D,
                        "Each door uses its restored shader, original color map and reflection cube.");
                    litDoors++;
                }
                else if (material.name == "Object3dShadowMat")
                {
                    renderer.GetPropertyBlock(properties);
                    float alpha = properties.GetColor("_Color").a;
                    Require(material.shader.name == "Royal/Object3DShadowShader" && material.shader.isSupported &&
                        alpha > 0f && alpha < 1f, "Each moving shadow uses the original alpha shader and animated opacity.");
                    translucentShadows++;
                }
            }
            Require(litDoors == 2 && translucentShadows == 2, "Both original doors and both shadows are bound.");
            report.cupboard_door_materials = true;
            Capture("core_owl_level64_cupboard.png");
        }

        private static void AdvanceOwlOpening()
        {
            var owlCell = board.Grid.GetCell(4, 4);
            switch (owlOpeningStep++)
            {
                case 0:
                    tappedOwl = (OriginalOwlStatueBoardItem)owlCell.Mediation.CurrentItem;
                    owlPivot = tappedOwl.ItemView.transform.Find("Root");
                    Gesture(owlCell, owlCell, false);
                    return;
                case 1:
                    var audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
                    Require(report.owl_tap && audio.Playback.GetClipLastPlayedTime(265) > 0f &&
                        game.Moves.MadeMoves == 0 && TotalGoals() == 85, "Owl tap plays feedback without spending a move.");
                    Gesture(owlCell, board.Grid.GetCell(5, 4), false);
                    return;
                case 2:
                    Require(game.Moves.MadeMoves == 0 && TotalGoals() == 85 &&
                        ReferenceEquals(owlCell.Mediation.CurrentItem, tappedOwl), "A drag cannot move an owl.");
                    report.owl_drag_blocked = true;
                    var rocket = board.Grid.GetCell(0, 9);
                    Require(rocket.Mediation.CurrentItem is OriginalRocketBoardItem, "The left vertical rocket is authored.");
                    owlRocketRefillDeadline = board.Clock.Time + 2f;
                    current.natural_actions++;
                    Gesture(rocket, rocket);
                    return;
                case 3:
                    Require(report.owl_rocket_burst && TotalGoals() == 76 &&
                        board.Goals.GetGoalUiLeftCount(OwlGoal) == 76 &&
                        board.Presentation.Pool.GetSpawnedCount(53) == 76 && game.CreatedRefills > 0,
                        "The original vertical rocket breaks nine owls, updates the HUD and opens its column for filling. " +
                        "Goals " + TotalGoals() + ", HUD " + board.Goals.GetGoalUiLeftCount(OwlGoal) +
                        ", live owl views " + board.Presentation.Pool.GetSpawnedCount(53) + ", refills " + game.CreatedRefills + ".");
                    report.owl_rocket_hit = true;
                    report.owl_rocket_refills = game.CreatedRefills;
                    SaveOwlRocketCapture();
                    var tnt = board.Grid.GetCell(4, 7);
                    Require(tnt.Mediation.CurrentItem is OriginalTntBoardItem, "The center TNT is authored.");
                    current.natural_actions++;
                    Gesture(tnt, tnt);
                    return;
                default:
                    Require(TotalGoals() < 76 && board.Goals.GetGoalUiLeftCount(OwlGoal) == TotalGoals(),
                        "The authored TNT also clears owls and updates their count.");
                    report.owl_tnt_hit = true;
                    moving = false;
                    return;
            }
        }

        private static void ObserveVaseFlow()
        {
            if (level != 51) return;
            if (vaseOpeningStep == 1 && Quaternion.Angle(vaseBase.localRotation, Quaternion.identity) > 1f)
                report.vase_tap = true;
            if (vaseOpeningStep == 3 && !report.vase_first_layer && crackedVase.LayerCount == 1)
            {
                Require(crackedVase.HasView && crackedVase.ItemView.CurrentSprite == vaseVisuals.layerSprites[0] &&
                    board.Goals.GetGoalLeftCount(VaseGoal) == current.target_count &&
                    board.Presentation.Pool.GetSpawnedCount(39) == 1,
                    "The opening match cracks the vase and plays layer-one particles without collecting its goal.");
                report.vase_first_layer = true;
                var right = crackedVase.ItemView.transform.Find("Root/View/BasePivotRight");
                vaseShakeFirst = right;
                vaseShakeSecond = right.Find("BasePivotLeft");
                vaseShakeFirstPeak = vaseShakePrevious = 0f;
                vaseShakeOppositeSeen = false;
                Capture("core_vase_level51_crack.png");
            }
            if (vaseOpeningStep == 3 && report.vase_first_layer && !report.vase_shake_eased_return)
                ObserveVaseShake();
            if (report.vase_first_layer && !report.vase_final_layer && !crackedVase.HasView)
            {
                Require(board.Goals.GetGoalLeftCount(VaseGoal) < current.target_count &&
                    board.Presentation.Pool.GetSpawnedCount(39) == 1, "The last hit collects the vase goal and bursts.");
                report.vase_final_layer = true;
                Capture("core_vase_level51_break.png");
            }
            if (fallingVase != null && fallingVase.HasView && fallingVase.ItemView.LocalPosition.Y < fallingVaseStartY - .02f)
                report.vase_fall = true;
        }

        private static void ObserveVaseShake()
        {
            float first = Mathf.Abs(Mathf.DeltaAngle(0f, vaseShakeFirst.localEulerAngles.z));
            float second = Mathf.Abs(Mathf.DeltaAngle(0f, vaseShakeSecond.localEulerAngles.z));
            if (vaseShakeFirstPeak == 0f && first == 0f && second > 0f)
            {
                (vaseShakeFirst, vaseShakeSecond) = (vaseShakeSecond, vaseShakeFirst);
                (first, second) = (second, first);
            }
            if (!vaseShakeOppositeSeen)
            {
                vaseShakeFirstPeak = Mathf.Max(vaseShakeFirstPeak, first);
                vaseShakeOppositeSeen = second > .01f;
                return;
            }
            if (first > .01f && first < vaseShakePrevious - .01f)
            {
                int variation = Mathf.RoundToInt((vaseShakeFirstPeak / 10f - .85f) / .15f);
                float peak = 2f * (.85f + variation * .15f);
                Require(variation >= 0 && variation <= 2 && Mathf.Abs(vaseShakePrevious - peak) < .025f,
                    "The third vase swing reaches the original peak before returning.");
                float duration = .1f * (.9f + variation * .1f);
                float ratio = board.Clock.DeltaTime / duration;
                float expected = peak * (1f - Mathf.Sin(ratio * Mathf.PI * .5f));
                Require(ratio > 0f && ratio < 1f && Mathf.Abs(first - expected) < .025f,
                    $"Vase final return must ease with Sine.Out. Expected {expected:F4}, got {first:F4}.");
                report.vase_shake_eased_return = true;
            }
            vaseShakePrevious = first;
        }

        private static void AdvanceVaseOpening()
        {
            switch (vaseOpeningStep++)
            {
                case 0:
                    var first = board.Grid.GetCell(0, 0);
                    tappedVase = (OriginalVaseBoardItem)first.Mediation.CurrentItem;
                    vaseBase = tappedVase.ItemView.transform.Find("Root/View/BasePivotRight/BasePivotLeft/Base");
                    Gesture(first, first, false);
                    return;
                case 1:
                    var audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
                    Require(report.vase_tap && game.Moves.MadeMoves == 0 && audio.Playback.GetClipLastPlayedTime(358) > 0f,
                        "A vase tap plays its authored feedback and preserves the move count.");
                    Gesture(board.Grid.GetCell(0, 0), board.Grid.GetCell(0, 1), false);
                    return;
                case 2:
                    Require(game.Moves.MadeMoves == 0 && game.RejectedSwaps == 1 &&
                        ReferenceEquals(board.Grid.GetCell(0, 0).Mediation.CurrentItem, tappedVase),
                        "Two vases exchange and return when no match is possible.");
                    report.vase_reject = true;
                    crackedVase = (OriginalVaseBoardItem)board.Grid.GetCell(1, 3).Mediation.CurrentItem;
                    current.natural_actions++;
                    Gesture(board.Grid.GetCell(2, 5), board.Grid.GetCell(2, 4));
                    return;
                case 3:
                    Require(report.vase_first_layer && report.vase_shake_eased_return && game.AcceptedSwaps == 1,
                        "The original opening cracks a vase and plays the complete hit shake.");
                    if (CoreCupboardVerification.FindNaturalSwap(board, out var source, out var target,
                        requiredItem: item => item is OriginalVaseBoardItem))
                    {
                        report.vase_natural_swap = true;
                        current.natural_actions++;
                        Gesture(source, target);
                        return;
                    }
                    for (int x = 0; x < board.Grid.Width; x++)
                    for (int y = 0; y < board.Grid.Height; y++)
                    {
                        var cell = board.Grid.GetCell(x, y);
                        if (!(cell.Mediation.CurrentItem is OriginalVaseBoardItem)) continue;
                        foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                        {
                            var adjacent = cell.Neighbors.Get(direction);
                            if (!(adjacent?.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                            PlaceSpecial(adjacent);
                            current.fixture_actions++;
                            Gesture(adjacent, cell);
                            return;
                        }
                    }
                    throw new InvalidOperationException("No vase borders a match tile.");
                default:
                    Require(game.AcceptedSwaps == 2 && board.Goals.GetGoalUiLeftCount(VaseGoal) == TotalGoals(),
                        "The accepted exchange involving a vase spends a move and keeps the HUD synchronized.");
                    report.vase_swap = true;
                    moving = false;
                    for (int x = board.Grid.Width - 1; x >= 0; x--)
                    {
                        var bottom = board.Grid.GetCell(x, 0);
                        var top = board.Grid.GetCell(x, 1);
                        if (!(bottom.Mediation.CurrentItem is OriginalVaseBoardItem lower) ||
                            !(top.Mediation.CurrentItem is OriginalVaseBoardItem upper)) continue;
                        fallingVase = upper;
                        fallingVaseStartY = top.Y;
                        report.vase_fall_setup_hits = lower.LayerCount;
                        for (int hit = 0; hit < report.vase_fall_setup_hits; hit++)
                            bottom.ExplodeCurrentItem(OriginalExploder.Next(14));
                        Require(!lower.HasView && upper.HasView, "A controlled blast clears the vase underneath an intact vase.");
                        return;
                    }
                    throw new InvalidOperationException("No stacked vases remain for the original fall check.");
            }
        }

        private static void AdvancePotionOpening()
        {
            switch (potionOpeningStep++)
            {
                case 0:
                    openedRack = (OriginalPotionBoardItem)board.Grid.GetCell(1, 1).Mediation.CurrentItem;
                    potionAnimator = openedRack.ItemView.GetComponentInChildren<Animator>();
                    var proxy = board.Grid.GetCell(2, 1);
                    VerifyPotionTargeting(openedRack, (OriginalPotionFakeBoardItem)proxy.Mediation.CurrentItem);
                    Gesture(proxy, proxy, false);
                    return;
                case 1:
                    var audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
                    Require(game.Moves.MadeMoves == 0 && openedRack.LayerCount == 4 && report.potion_tap_animation &&
                        audio.Playback.GetClipLastPlayedTime(289) > 0f, "A proxy tap animates the rack and spends no move.");
                    current.natural_actions++;
                    Gesture(board.Grid.GetCell(3, 1), board.Grid.GetCell(3, 0));
                    return;
                case 2:
                    Require(game.AcceptedSwaps == 1 && report.potion_color_hit && openedRack.HasView &&
                        board.Grid.GetCell(1, 1).Mediation.CurrentItem == openedRack,
                        "The actual color move preserves the rack footprint while bottles remain.");
                    int before = openedRack.LayerCount;
                    openedRack.Explode(OriginalExploder.NextWithMatchType(5, 2));
                    Require(openedRack.LayerCount == before, "A second green hit cannot remove a different bottle.");
                    report.potion_repeated_color_rejected = true;
                    tntRack = (OriginalPotionBoardItem)board.Grid.GetCell(5, 5).Mediation.CurrentItem;
                    tntRackLayers = tntRack.LayerCount;
                    var first = board.Grid.GetCell(3, 4);
                    var second = board.Grid.GetCell(4, 4);
                    Require(first.Mediation.CurrentItem is OriginalMatchBoardItem && second.Mediation.CurrentItem is OriginalMatchBoardItem,
                        "Original match cells next to the potion rack accept the test combo.");
                    PlaceSpecial(first);
                    PlaceSpecial(second, 20);
                    current.fixture_actions++;
                    current.fixture_combos++;
                    Gesture(first, second);
                    return;
                default:
                    Require(tntRackLayers >= 2 && tntRack.LayerCount <= tntRackLayers - 2 &&
                        game.Session.SpecialItemCounts.Combos.ContainsKey(6),
                        "The actual TNT-rocket horizontal and vertical waves each remove one bottle. Before " +
                        tntRackLayers + ", after " + tntRack.LayerCount);
                    report.potion_tnt_rocket_verified = true;
                    moving = false;
                    return;
            }
        }

        private static void VerifyPotionTargeting(OriginalPotionBoardItem rack, OriginalPotionFakeBoardItem proxy)
        {
            const int regularScore = 456976, areaScore = 228488, lastHitScore = 913951;
            Require(ReferenceEquals(proxy.Master, rack) && rack.LayerCount == 4 && rack.GetExplodeScore(0) == regularScore &&
                proxy.GetExplodeScore(3) == areaScore, "Use the authored four-bottle rack and the original area score.");
            var rackCell = ((IOriginalExplodeTarget)rack).CurrentCell;
            var rackPoint = new OriginalCellPoint(rackCell.X, rackCell.Y);
            var blast = OriginalExploder.Next(13, rackPoint);
            var rocket = OriginalExploder.Next(11, rackPoint);
            var forecast = CreateWinForecast(board.Grid, rack.GoalType, 4, out var goal);
            Require(!forecast.IsAreaWinning(rackPoint, blast) && !forecast.IsLineWinning(false, rackCell.Y, rocket),
                "An untouched four-bottle rack does not satisfy the target in one blast.");
            var point = new OriginalCellPoint(board.Grid.Width - 1, board.Grid.Height - 1);
            var pending = new OriginalExplodeData[3];
            var first = OriginalExploder.Next(8, point);
            var second = OriginalExploder.Next(8, point);
            IOriginalExplodeTarget firstTarget = null, secondTarget = null;
            try
            {
                for (int i = 0; i < pending.Length; i++)
                {
                    pending[i] = OriginalExploder.Next(8, point);
                    rack.AddIncomingExploder(pending[i]);
                }
                Require(rack.LayerCount == 4 && rack.RemainingExplodeCount() == 1 &&
                    rack.GetExplodeScore(0) == lastHitScore && proxy.GetExplodeScore(5) == lastHitScore &&
                    rack.GetExplodeScore(1) == regularScore && rack.GetExplodeScore(3) == areaScore,
                    "Three pending hits raise only the single-hit score for the last free bottle.");
                Require(forecast.IsAreaWinning(rackPoint, blast) && forecast.IsLineWinning(false, rackCell.Y, rocket),
                    "The last unreserved hit forecasts all four bottles, including the three pending hits.");
                goal.SetGoal(5);
                Require(!forecast.IsAreaWinning(rackPoint, blast) && !forecast.IsLineWinning(false, rackCell.Y, rocket),
                    "The area and line never count a rack again through a proxy.");
                goal.SetGoal(4);
                firstTarget = game.GuidedTargets.FindForPropeller(first, null);
                Require(IsPotionTarget(firstTarget, rack) && rack.RemainingExplodeCount() == 0,
                    "The guided target finder selects the boosted footprint and reserves the last free bottle.");
                Require(!forecast.IsAreaWinning(rackPoint, blast) && !forecast.IsLineWinning(false, rackCell.Y, rocket),
                    "A fully reserved rack contributes no further goal hits.");
                secondTarget = game.GuidedTargets.FindForPropeller(second, null);
                Require(secondTarget != null && !IsPotionTarget(secondTarget, rack),
                    "The next guided target skips the fully reserved footprint.");
                report.potion_guided_scores_verified = true;
                report.potion_win_forecast_verified = true;
            }
            finally
            {
                secondTarget?.RemoveIncomingExploder(second);
                firstTarget?.RemoveIncomingExploder(first);
                foreach (var hit in pending) rack.RemoveIncomingExploder(hit);
                game.ClearSinglePropellerWinCalculationData();
            }
            Require(rack.RemainingExplodeCount() == 4 && rack.GetExplodeScore(0) == regularScore,
                "Releasing the test hits restores all four authored bottles and the original base score.");
        }

        private static bool IsPotionTarget(IOriginalExplodeTarget target, OriginalPotionBoardItem rack) =>
            ReferenceEquals(target, rack) || target is OriginalPotionFakeBoardItem proxy && ReferenceEquals(proxy.Master, rack);

        private static OriginalFirstBoardExplodeTargets CreateWinForecast(OriginalCellGrid grid, int goalType,
            int count, out OriginalGoalTuple goal)
        {
            var claims = new OriginalGoalClaims();
            var manager = new OriginalGoalManager(() => 0, board.GameOperations, claims);
            goal = new OriginalGoalTuple(goalType, count, false, manager);
            manager.InitWithGoalsAndLimits(new[] { new Dictionary<int, OriginalGoalTuple> { [goalType] = goal } },
                Array.Empty<Dictionary<int, OriginalLimitTuple>>(), false);
            var forecast = new OriginalFirstBoardExplodeTargets(manager, claims, () => board.Clock.FrameCount);
            forecast.BindAreaContext(grid, () => false);
            forecast.BindLineContext(board.Layout);
            return forecast;
        }

        private static void VerifyCupboardWinForecast(OriginalGridCell cell, OriginalCupboardBoardItem cupboard)
        {
            var grid = new OriginalCellGrid(1, 1);
            grid.SetCell(0, 0, cell);
            var forecast = CreateWinForecast(grid, cupboard.GoalType, 1, out var goal);
            var point = new OriginalCellPoint(0, 0);
            var blast = OriginalExploder.Next(13, point);
            var pending = OriginalExploder.Next(8, point);
            Require(!forecast.IsAreaWinning(point, blast), "An unreserved cupboard door blocks plate predictions.");
            cupboard.AddIncomingExploder(pending);
            try
            {
                Require(cupboard.HasDoor && forecast.IsAreaWinning(point, blast),
                    "One pending hit on the door lets the new hit forecast a plate.");
                goal.SetGoal(2);
                Require(!forecast.IsAreaWinning(point, blast), "One cupboard cell predicts at most one plate.");
            }
            finally { cupboard.RemoveIncomingExploder(pending); }
            report.cupboard_win_forecast_verified = true;
        }

        private static void VerifySingleGoalForecast(IOriginalExplodeTarget target, params OriginalGridCell[] cells)
        {
            var grid = new OriginalCellGrid(cells.Length, 1);
            for (int i = 0; i < cells.Length; i++) grid.SetCell(i, 0, cells[i]);
            var forecast = CreateWinForecast(grid, target.GoalType, 1, out var goal);
            var point = new OriginalCellPoint(0, 0);
            var blast = OriginalExploder.Next(13, point);
            int layerCount = target.RemainingExplodeCount();
            Require(layerCount > 0 && forecast.IsAreaWinning(point, blast) == (layerCount == 1),
                "Before reservations, the forecast wins only with one layer left.");
            var pending = new OriginalExplodeData[layerCount];
            try
            {
                for (int i = 0; i < layerCount - 1; i++)
                {
                    pending[i] = OriginalExploder.Next(8, point);
                    target.AddIncomingExploder(pending[i]);
                }
                Require(forecast.IsAreaWinning(point, blast), "One unreserved hit completes the target.");
                goal.SetGoal(2);
                Require(!forecast.IsAreaWinning(point, blast), "The target contributes exactly one goal.");
                goal.SetGoal(1);
                pending[layerCount - 1] = OriginalExploder.Next(8, point);
                target.AddIncomingExploder(pending[layerCount - 1]);
                Require(!forecast.IsAreaWinning(point, blast), "The fully reserved target cannot contribute again.");
            }
            finally
            {
                foreach (var hit in pending)
                    if (hit.Id != 0) target.RemoveIncomingExploder(hit);
            }
        }

        private static void AdvanceEggOpening()
        {
            switch (eggOpeningStep++)
            {
                case 0:
                    var cell = board.Grid.GetCell(0, 0);
                    tappedEgg = (OriginalEggBoardItem)cell.Mediation.CurrentItem;
                    eggPivot = tappedEgg.ItemView.transform.Find("Root/View/Base");
                    Gesture(cell, cell, false);
                    return;
                case 1:
                    var audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
                    Require(game.Moves.MadeMoves == 0 && TotalGoals() == 60 && report.egg_tap_verified &&
                        audio.Playback.GetClipLastPlayedTime(143) > 0f, "Egg tap plays its native animation and sound without consuming a move.");
                    Gesture(board.Grid.GetCell(0, 0), board.Grid.GetCell(0, 1), false);
                    return;
                case 2:
                    Require(game.Moves.MadeMoves == 0 && game.RejectedSwaps == 1 &&
                        ReferenceEquals(board.Grid.GetCell(0, 0).Mediation.CurrentItem, tappedEgg),
                        "The two eggs swap back when the move creates no match.");
                    report.egg_reject_verified = true;
                    fallingEgg = (OriginalEggBoardItem)board.Grid.GetCell(2, 6).Mediation.CurrentItem;
                    current.natural_actions++;
                    Gesture(board.Grid.GetCell(4, 3), board.Grid.GetCell(3, 3));
                    return;
                case 3:
                    report.eggs_from_opening_match = 60 - TotalGoals();
                    Require(game.AcceptedSwaps == 1 && report.eggs_from_opening_match >= 3 &&
                        board.Goals.GetGoalUiLeftCount(EggGoal) == TotalGoals() && report.egg_fall_verified &&
                        report.egg_burst_verified && game.CreatedRefills > 0,
                        "The authored opening match breaks adjacent eggs, updates the goal and refills beneath falling eggs.");
                    if (CoreCupboardVerification.FindNaturalSwap(board, out var source, out var target,
                        requiredItem: item => item is OriginalEggBoardItem))
                    {
                        report.egg_natural_swap = true;
                        current.natural_actions++;
                        Gesture(source, target);
                        return;
                    }
                    TapEggWithFixture();
                    return;
                default:
                    Require(game.AcceptedSwaps == 2 && board.Goals.GetGoalUiLeftCount(EggGoal) == TotalGoals(),
                        "A legal exchange involving an egg commits and spends one move.");
                    report.egg_swap_verified = true;
                    settledAt = -1f;
                    moving = false;
                    return;
            }
        }

        private static void TapEggWithFixture()
        {
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!(cell.Mediation.CurrentItem is OriginalEggBoardItem)) continue;
                foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                {
                    var neighbor = cell.Neighbors.Get(direction);
                    if (!(neighbor?.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                    PlaceSpecial(neighbor);
                    current.fixture_actions++;
                    Gesture(neighbor, cell);
                    return;
                }
            }
            throw new InvalidOperationException("No egg borders a color tile on the live opening board.");
        }

        private static void PlayNaturalMove()
        {
            OriginalGridCell lightball = null;
            bool hasColors = false;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                var item = cell.Mediation.CurrentItem;
                if (item is OriginalMatchBoardItem) hasColors = true;
                if (item is OriginalLightballBoardItem) lightball = cell;
                else if (item is OriginalSpecialBoardItem)
                {
                    current.natural_actions++;
                    Gesture(cell, cell);
                    return;
                }
            }
            if (hasColors && lightball != null)
            {
                current.natural_actions++;
                Gesture(lightball, lightball);
                return;
            }
            if (!CoreCupboardVerification.FindNaturalSwap(board, out var source, out var target, true))
            {
                var iterator = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
                int nativePossible = board.PossibleMoves.CheckBoard(iterator);
                var rows = new string[board.Grid.Height];
                for (int y = 0; y < board.Grid.Height; y++)
                {
                    var tiles = new int[board.Grid.Width];
                    for (int x = 0; x < board.Grid.Width; x++)
                        tiles[x] = board.Grid.GetCell(x, y).Mediation.CurrentItem?.TiledId ?? 0;
                    rows[y] = string.Join(",", tiles);
                }
                report.no_move_board = rows;
                report.no_move_native_result = nativePossible;
                Require(false, "The live board offers a legal move with " + game.Moves.LeftMoves +
                    " moves left. Original possible-move result " + nativePossible + ".");
            }
            current.natural_actions++;
            Gesture(source, target);
        }

        private static bool BordersMailbox(OriginalGridCell cell)
        {
            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                if (cell.Neighbors.Get(direction)?.Mediation.CurrentItem is OriginalMailBoxBoardItem) return true;
            return false;
        }

        private static void ObserveCoinShuffle()
        {
            if (board.State.HasOperation(6))
            {
                if (!shuffleSeen)
                {
                    shuffleSeen = true;
                    shuffleMadeMoves = game.Moves.MadeMoves;
                    shuffleLeftMoves = game.Moves.LeftMoves;
                    shuffleCoinGoal = board.Goals.GetGoalLeftCount(CoinGoal);
                    shuffleCaptureAt = board.Clock.Time + .6f;
                    for (int x = 0; x < board.Grid.Width; x++)
                    for (int y = 0; y < board.Grid.Height; y++)
                        shuffleCoins[x, y] = board.Grid.GetCell(x, y).Mediation.CurrentItem as OriginalCoinBoardItem;
                }
                Require(!game.CanUserMakeMove(), "The production shuffle locks input.");
                if (!shuffleCaptured && board.Clock.Time >= shuffleCaptureAt)
                {
                    shuffleCaptured = true;
                    Capture("core_bush_level80_shuffle.png");
                }
                return;
            }
            if (!shuffleSeen) return;
            shuffleSeen = false;
            var iterator = new OriginalGridIterator(new OriginalBottomLeftHorizontalStrategy(new OriginalBoardGridAccess(board.Grid, board.Layout)));
            Require(game.Moves.MadeMoves == shuffleMadeMoves && game.Moves.LeftMoves == shuffleLeftMoves &&
                board.Goals.GetGoalLeftCount(CoinGoal) == shuffleCoinGoal && board.PossibleMoves.CheckBoard(iterator) == 2 &&
                !board.Matches.IsThereAnyMatch(false), "Shuffle restores a real legal move without spending a move or collecting a coin.");
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
                Require(ReferenceEquals(shuffleCoins[x, y], board.Grid.GetCell(x, y).Mediation.CurrentItem as OriginalCoinBoardItem),
                    "The native shuffle leaves coin identities in place.");
            report.coin_shuffles++;
        }

        private static bool BordersBush(OriginalGridCell cell)
        {
            foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                if (cell.Neighbors.Get(direction)?.Mediation.CurrentItem?.ItemType == 13) return true;
            return false;
        }

        private static int CountBushLayers()
        {
            int layers = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
                if (board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBushBoardItem bush) layers += bush.LayerCount;
            return layers;
        }

        private static void ObserveBushFlow()
        {
            if (observedGrass != null && !report.bush_grass_layer_refill)
                ObserveGrassLayerRefill();
            if (level != 71) return;
            if (bushOpeningStep == 1 && firstBush.ItemView.Rig.NormalizedTime < .99f) report.bush_tap = true;
            if (report.bush_grass_landed && !report.bush_cavity_refilled &&
                !(board.Grid.GetCell(1, 5).Mediation.CurrentItem is OriginalBushBoardItem) &&
                board.Grid.GetCell(1, 0).Mediation.CurrentItem?.HasView == true &&
                board.Grid.GetCell(2, 0).Mediation.CurrentItem?.HasView == true)
                report.bush_cavity_refilled = true;
            if (bushOpeningStep != 4 || !board.State.HasOperation(12)) return;
            int count = board.Presentation.Pool.GetSpawnedCount(64);
            report.bush_grass_flights = Math.Max(report.bush_grass_flights, count);
            if (count == 16 && bushSpreadCaptureAt < 0f) bushSpreadCaptureAt = board.Clock.Time + .23f;
            if (bushSpreadCaptureAt >= 0f && board.Clock.Time >= bushSpreadCaptureAt && !bushSpreadCaptured)
            {
                bushSpreadCaptured = true;
                Capture("core_bush_level71_spread.png");
            }
        }

        private static void ObserveGrassLayerRefill()
        {
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var grass = board.Grid.GetCell(x, y).StaticTouch.GetStaticItem(2) as OriginalGrassBoardItem;
                if (grass != null && ReferenceEquals(observedGrass[x, y], grass) &&
                    observedGrassLayers[x, y] == 1 && grass.LayerCount == 2)
                {
                    report.bush_grass_layer_refill = true;
                    return;
                }
                observedGrass[x, y] = grass;
                observedGrassLayers[x, y] = grass?.LayerCount ?? 0;
            }
        }

        private static void AdvanceBushOpening()
        {
            var master = board.Grid.GetCell(1, 1);
            var proxy = board.Grid.GetCell(1, 2);
            switch (bushOpeningStep)
            {
                case 0:
                    firstBush = (OriginalBushBoardItem)master.Mediation.CurrentItem;
                    Require(proxy.Mediation.CurrentItem is OriginalBushFakeBoardItem fake && fake.Master == firstBush,
                        "The top-left bush tile owns the same view and hit record.");
                    Gesture(proxy, proxy, false);
                    break;
                case 1:
                    Require(report.bush_tap && game.Moves.MadeMoves == 0 && firstBush.LayerCount == 5,
                        "The bush plays the original tap pose without spending a move.");
                    Gesture(proxy, board.Grid.GetCell(0, 2), false);
                    break;
                case 2:
                    Require(game.Moves.MadeMoves == 0 && ReferenceEquals(master.Mediation.CurrentItem, firstBush),
                        "Dragging the bush proxy does not move its footprint.");
                    report.bush_drag_blocked = true;
                    Require(CoreCupboardVerification.FindNaturalSwap(board, out var source, out var destination,
                        matchCenterFilter: BordersBush), "The authored opening offers a regular match next to a bush.");
                    bushLayersBeforeMatch = CountBushLayers();
                    current.natural_actions++;
                    Gesture(source, destination);
                    break;
                case 3:
                    Require(CountBushLayers() < bushLayersBeforeMatch && board.Goals.GetGoalLeftCount(BushGoal) == 4 &&
                        board.Goals.GetGoalLeftCount(GrassGoal) == 0, "A normal adjacent match removes a bush layer.");
                    report.bush_near_match = true;
                    Capture("core_bush_level71_damage.png");
                    break;
                case 4:
                    if (firstBush.HasView)
                    {
                        var target = FindFirstBushTntCenter();
                        Require(target != null, "The first bush has a replaceable match tile in TNT range.");
                        PlaceSpecial(target);
                        current.fixture_actions++;
                        Gesture(target, target);
                        return;
                    }
                    int grassCount = 0;
                    for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 4; y++)
                    {
                        if (!(board.Grid.GetCell(x, y).StaticTouch.GetStaticItem(2) is OriginalGrassBoardItem grass)) continue;
                        Require(grass.HasView && (grass.LayerCount == 1 || grass.LayerCount == 2), "Spread lands on real layered grass.");
                        Require(grass.ItemView.rightPatch.enabled == (grass.CurrentCell.Neighbors.Get(3)?.StaticTouch.HasStaticItem(2) == true) &&
                            grass.ItemView.topPatch.enabled == (grass.CurrentCell.Neighbors.Get(1)?.StaticTouch.HasStaticItem(2) == true),
                            "Settled grass connects to adjacent grass and leaves exposed edges clear.");
                        grassCount++;
                    }
                    Require(report.bush_grass_flights == 16 && bushSpreadCaptured && grassCount > 0 &&
                        board.Goals.GetGoalLeftCount(GrassGoal) == grassCount && board.Goals.GetGoalUiLeftCount(GrassGoal) == grassCount &&
                        !board.State.HasOperation(12) && board.Goals.HasPrerequisite(GrassGoal),
                        "Sixteen flights land, reconcile the model and HUD, and preserve the remaining bushes' prerequisite. " +
                        "Flights " + report.bush_grass_flights + ", grass " + grassCount + ", goal " + board.Goals.GetGoalLeftCount(GrassGoal));
                    report.bush_grass_landed = true;
                    Capture("core_bush_level71_grass.png");
                    break;
            }
            bushOpeningStep++;
        }

        private static OriginalGridCell FindFirstBushTntCenter()
        {
            for (int index = 1; index <= 2; index++)
            {
                var below = board.Grid.GetCell(index, 0);
                if (below.Mediation.CurrentItem is OriginalMatchBoardItem) return below;
                var left = board.Grid.GetCell(0, index);
                if (left.Mediation.CurrentItem is OriginalMatchBoardItem) return left;
            }
            return null;
        }

        private static void AdvanceSafeOpening()
        {
            var master = board.Grid.GetCell(1, 5);
            var proxy = board.Grid.GetCell(1, 6);
            if (safeOpeningStep == 0)
            {
                openingSafe = master.Mediation.CurrentItem as OriginalSafeBoardItem;
                Require(openingSafe != null && openingSafe.LayerCount == 5, "The authored top-left safe starts with five layers.");
                VerifySingleGoalForecast(openingSafe, master, proxy);
                report.safe_win_forecast_verified = true;
                Gesture(proxy, board.Grid.GetCell(0, 6), false);
            }
            else if (safeOpeningStep == 1)
            {
                Require(game.Moves.LeftMoves == movesBefore && openingSafe.LayerCount == 5 &&
                    proxy.Mediation.CurrentItem is OriginalSafeFakeBoardItem fake && ReferenceEquals(fake.Master, openingSafe),
                    "The safe proxy does not move or consume a turn when dragged.");
                report.safe_blocks_drag = true;
                current.natural_actions++;
                Gesture(board.Grid.GetCell(2, 7), board.Grid.GetCell(2, 8));
            }
            else if (safeOpeningStep == 2)
            {
                Require(openingSafe.LayerCount == 5 && board.Goals.GetGoalLeftCount(20) == 4,
                    "The authored adjacent color match leaves the safe untouched.");
                report.safe_resists_match = true;
                var tnt = board.Grid.GetCell(4, 4);
                Require(tnt.Mediation.CurrentItem is OriginalTntBoardItem, "The original middle TNT is ready.");
                current.natural_actions++;
                Gesture(tnt, tnt);
            }
            else
            {
                Require(openingSafe.LayerCount == 4 && openingSafe.HasView && board.Goals.GetGoalLeftCount(20) == 4,
                    "The real TNT damages the shared safe once and leaves its goal until final destruction.");
                Require(report.safe_handle_depth_verified, "The native handle effect retained its depth while the board settled.");
                report.safe_power_damage = true;
                Capture("core_safe_level91_damage.png");
                moving = false;
            }
            safeOpeningStep++;
        }

        private static void VerifySafeHandleParticleDepth()
        {
            Vector3 origin = openingSafe.ItemView.transform.position;
            OriginalSortedParticleSystem3d handle = null, shadow = null;
            foreach (var effect in UnityEngine.Object.FindObjectsOfType<OriginalSafeDamageParticles>())
            {
                // TNT shakes the board after the effect's world-space position has been recorded.
                if (((Vector2)(effect.transform.position - origin)).sqrMagnitude > .25f) continue;
                foreach (var system in effect.GetComponentsInChildren<OriginalSortedParticleSystem3d>())
                {
                    if (!system.ParticleSystem.isPlaying) continue;
                    if (system.name == "Handle") handle = system;
                    else if (system.name == "HandleShadow") shadow = system;
                }
            }
            Require(handle != null && shadow != null && handle.ZOffset == 4 && shadow.ZOffset == 4,
                "The authored hit starts the handle fragments and their independently sorted shadow.");
            report.safe_handle_particle_z = handle.transform.position.z;
            report.safe_handle_shadow_z = shadow.transform.position.z;
            report.safe_handle_parent_z = shadow.transform.parent.position.z;
            Require(handle.Renderer.sortingLayerID == shadow.Renderer.sortingLayerID &&
                handle.Renderer.sortingOrder == shadow.Renderer.sortingOrder && report.safe_handle_particle_z >= 12f &&
                Mathf.Approximately(report.safe_handle_shadow_z, report.safe_handle_particle_z + 4f),
                "Following the fragments on X and Y preserves the shadow's next depth slot.");
            report.safe_handle_depth_verified = true;
        }

        private static void VerifyPendingHoneyAreaScore()
        {
            var cell = board.Grid.GetCell(5, 1);
            var honey = cell.StaticTouch.GetStaticItem(OriginalHoneyBoardItem.StaticType) as OriginalHoneyBoardItem;
            Require(honey != null && cell.Mediation.CurrentItem is OriginalMatchBoardItem,
                "Use the authored honey over a color tile on level 81.");
            VerifySingleGoalForecast(honey, cell);
            report.honey_win_forecast_verified = true;
            var point = new OriginalCellPoint(cell.X, cell.Y);
            var area = OriginalExploder.Next(13, point);
            var line = OriginalExploder.Next(11, point);
            var incoming = OriginalExploder.Next(8, point);
            int score = game.AreaTargets.CalculateScore(point, area, out _);
            honey.AddIncomingExploder(incoming);
            try
            {
                game.LineTargets.CalculateScore(false, point.Y, line, out _);
                int pendingScore = game.AreaTargets.CalculateScore(point, area, out _);
                Require(score - pendingScore == honey.GetExplodeScore(3) && honey.HasView,
                    "A pending final hit removes the live cover's contribution before its view disappears.");
            }
            finally { honey.RemoveIncomingExploder(incoming); }
            game.LineTargets.CalculateScore(false, point.Y, line, out _);
            Require(game.AreaTargets.CalculateScore(point, area, out _) == score,
                "Canceling the pending hit returns the authored cover score.");
            report.pending_honey_area_score_verified = true;
        }

        private static void AdvanceHoneyOpening()
        {
            var covered = board.Grid.GetCell(5, 1);
            if (honeyOpeningStep == 0)
            {
                honeyBefore = covered.StaticTouch.GetStaticItem(4) as OriginalHoneyBoardItem;
                honeyUnderlay = covered.Mediation.CurrentItem;
                Require(honeyBefore != null && honeyUnderlay is OriginalMatchBoardItem && honeyBefore.HasView &&
                    covered.StaticTouch.HasTouchBlockingItem() && !covered.IsFallBlocked(),
                    "The authored honey has a live cover over a falling tile and blocks only touch.");
                honeyBeforeView = honeyBefore.View;
                Gesture(covered, board.Grid.GetCell(5, 0), false);
            }
            else if (honeyOpeningStep == 1)
            {
                Require(game.Moves.LeftMoves == movesBefore && board.Goals.GetGoalLeftCount(14) == current.honey &&
                    ReferenceEquals(covered.Mediation.CurrentItem, honeyUnderlay) &&
                    ReferenceEquals(covered.StaticTouch.GetStaticItem(4), honeyBefore) &&
                    board.Presentation.Pool.GetSpawnedCount(45) > 0,
                    "Dragging covered honey spends no move, preserves its tile and plays native tap debris.");
                report.honey_blocks_drag = true;
                current.natural_actions++;
                Gesture(board.Grid.GetCell(5, 0), board.Grid.GetCell(6, 0));
            }
            else
            {
                int honeyLeft = board.Goals.GetGoalLeftCount(14);
                Require(!honeyBefore.HasView && !covered.StaticTouch.HasStaticItem(4) &&
                    !covered.StaticTouch.HasTouchBlockingItem() && covered.Mediation.CurrentItem is OriginalMatchBoardItem &&
                    honeyLeft == current.honey - 6 && honeyLeft == board.Goals.GetGoalUiLeftCount(14),
                    "The authored bottom swap clears neighboring honey, reveals the underlying tile, and updates the HUD.");
                report.honey_natural_reveal = true;
                report.honey_first_swap_removed = current.honey - honeyLeft;
                report.honey_recycled_min_alpha = 1f;
                foreach (var renderer in honeyBeforeView.GetComponentsInChildren<SpriteRenderer>(true))
                    report.honey_recycled_min_alpha = Mathf.Min(report.honey_recycled_min_alpha, renderer.color.a);
                Require(!honeyBeforeView.gameObject.activeSelf && report.honey_recycled_min_alpha >= .999f,
                    "The revealed honey view is pooled with every sprite's alpha restored.");
                Capture("core_honey_level81_reveal.png");
                moving = false;
            }
            honeyOpeningStep++;
        }

        private static void ObserveCoveredBoxes()
        {
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var original = coveredBoxes[x, y];
                if (original == null || original.LayerCount != 1) continue;
                var cell = board.Grid.GetCell(x, y);
                if (cell.StaticTouch.HasStaticItem(4) || !ReferenceEquals(cell.Mediation.CurrentItem, original)) continue;
                report.honey_reveals_intact_box = true;
                return;
            }
        }

        private static void ChooseTnt(bool usePair, out OriginalGridCell center, out OriginalGridCell adjacent)
        {
            center = adjacent = null;
            int best = 0;
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!(cell.Mediation.CurrentItem is OriginalMatchBoardItem) || cell.StaticTouch.HasTouchBlockingItem()) continue;
                OriginalGridCell neighbor = null;
                if (usePair)
                {
                    foreach (int direction in OriginalCellNeighbors.TouchingNeighborTypes)
                    {
                        var candidate = cell.Neighbors.Get(direction);
                        if (!(candidate?.Mediation.CurrentItem is OriginalMatchBoardItem) || candidate.StaticTouch.HasTouchBlockingItem()) continue;
                        neighbor = candidate;
                        break;
                    }
                }
                int score = ScoreBlast(x, y, neighbor != null ? 4 : 2);
                if (score <= best) continue;
                best = score;
                center = cell;
                adjacent = neighbor;
            }
        }

        private static int ScoreBlast(int x, int y, int radius)
        {
            int hits = 0;
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Math.Abs(dx) == radius && Math.Abs(dy) == radius) continue;
                var cell = board.Grid.GetCell(x + dx, y + dy);
                if (cell == null) continue;
                var item = cell.Mediation.CurrentItem;
                if (item is OriginalBoxBoardItem || item?.ItemType == 8 || item?.ItemType == 12) hits += 2;
                if (item is OriginalEggBoardItem) hits++;
                if (item is OriginalOwlStatueBoardItem) hits += 2;
                if (item?.ItemType == 13 || item?.ItemType == 14) hits += 3;
                if (item is OriginalVaseBoardItem vase) hits += vase.LayerCount == 1 ? 3 : 2;
                if (item is OriginalMailBoxBoardItem mailbox && mailbox.IsActive) hits++;
                if (cell.StaticTouch.HasStaticItem(2)) hits++;
                if (cell.StaticTouch.HasStaticItem(4)) hits += 2;
            }
            return hits;
        }

        private static void PlaceSpecial(OriginalGridCell cell, int tiledId = 40)
        {
            var tile = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            tile.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)tile.SwapView);
            game.CreateItemAtCell(tiledId, cell);
        }

        private static void ObserveTntRefill()
        {
            if (fillingTnt != null)
            {
                if (fillingTnt.HasView && fillingTnt.CurrentCell != null &&
                    ((OriginalFallComponentState)fillingTnt.FallComponent).IsItemSteady)
                {
                    var view = fillingTnt.ItemView;
                    Require(!view.IsFillMaskEnabled, "A settled refill TNT has released its fill mask.");
                    RequireFuseMask(view, SpriteMaskInteraction.None);
                    report.tnt_fuse_unmasked_on_landing = true;
                }
                if (fillingTnt.HasView) return;
                fillingTnt = null;
            }
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                if (board.Grid.GetCell(x, y) is not OriginalFillingCell cell ||
                    cell.LastCreatedItem is not OriginalTntBoardItem item || !item.ItemView.IsFillMaskEnabled) continue;
                RequireFuseMask(item.ItemView, SpriteMaskInteraction.VisibleOutsideMask);
                fillingTnt = item;
                report.tnt_fuse_masked_at_refill = true;
                return;
            }
        }

        private static void RequireFuseMask(OriginalTntItemSpriteView view, SpriteMaskInteraction expected)
        {
            var emitters = view.Fuse.Transform.GetComponentsInChildren<ParticleSystemRenderer>(true);
            Require(emitters.Length > 0, "The falling TNT has its original fuse renderers.");
            foreach (var emitter in emitters)
                Require(emitter.maskInteraction == expected, "Refill TNT fuse renderer " + emitter.name +
                    " expected mask " + expected + ", observed " + emitter.maskInteraction + ".");
        }

        private static void VerifyResult()
        {
            var dialog = controller.CoreFlow.WinDialog;
            if (dialog == null || !dialog.Lifecycle.IsShowing || dialog.IsAnimationWorking) return;
            CoreFlowVerification.RequireCurvedTitle(dialog, level == 100 ? "100 Levels Complete" : "Level " + level,
                level == 100 ? 2.3f : level >= 10 ? 2.1f : 1.9f);
            current.result_title_curve_verified = true;
            int persisted = level == 100 ? 100 : level + 1;
            Require(game.Session.State == 3 && controller.Progress.LevelToPlay == persisted &&
                controller.Progress.IsComplete == (level == 100), "Win persists its correct progress and completion state.");
            if (game.Session.IsBonus)
            {
                current.collected_coins = board.Goals.GetGoalLeftCount(CoinGoal);
                Require(current.collected_coins >= current.coins && current.collected_coins == board.Goals.GetGoalUiLeftCount(CoinGoal) &&
                    game.Moves.MadeMoves == 20 && !board.State.HasOperation(5), "The bonus level settles its coin flights after twenty moves.");
            }
            else
                foreach (var goal in goals)
                    Require(goal.LeftCount == 0 && goal.LeftCountUi == 0 && board.Goals.GetFlyingGoalCount(goal.GoalType) <= 0,
                        "Model goals and the HUD agree after the final arrival.");
            using (var db = OriginalRuntimeStorage.Open(controller.Progress.DatabasePath))
                Require(db.Find("Level").Value == persisted.ToString() && db.Find("Coins").Key == null,
                    "Core-only SQLite advances through the win.");
            if (!game.Session.IsBonus && game.Moves.MadeMoves < current.move_budget)
                Require(controller.CoreFlow.RemainingMoves.CollectionCount > 0, "Original remaining-move collection completes.");
            current.moves = game.Moves.MadeMoves;
            current.original_lightball_combo = sawOriginalCombo;
            current.won = true;
            Require(current.moves == current.natural_actions + current.fixture_actions, "Recorded actions account for each real move.");
            if (level == 28 || level == 40 || level == 100) Require(sawOriginalCombo, "Original lightball combination played.");
            if (level == 100) Require(report.tnt_fuse_masked_at_refill && report.tnt_fuse_unmasked_on_landing,
                "A naturally generated TNT applies and releases the original fuse fill mask.");
            if (level == 38) Require(game.Session.SpecialItemCounts.Combos.ContainsKey(10), "Authored propellers combined.");
            if (level == 41) Require(report.potion_tnt_rocket_verified && board.Presentation.Pool.GetSpawnedCount(58) == 0 &&
                board.Presentation.Pool.GetSpawnedCount(59) == 0 && board.Presentation.Pool.GetSpawnedCount(61) == 1,
                "Final bottle clears the four-cell rack and returns both views to their original pools.");
            if (level == 46) Require(report.dynamic_egg_refill, "The original Egg set generated live tiles during the level.");
            if (level == 51) Require(report.vase_swap && report.vase_final_layer && report.vase_fall &&
                board.Presentation.Pool.GetSpawnedCount(38) == 0, "The vases move, fall, break, count and recycle through the normal flow.");
            if (level == 61) Require(report.owl_rocket_hit && report.owl_tnt_hit && board.Presentation.Pool.GetSpawnedCount(53) == 0,
                "The authored powers hit owls and the scene recycles all 85 statue views.");
            if (level == 62) Require(report.owl_resists_adjacent_match, "Owl resistance was observed in the authored color match.");
            if (level == 64) Require(report.cupboard_door_materials, "The restored cupboard shaders were exercised.");
            if (level == 66) Require(report.dynamic_vase_refill, "The original Vase set generated live tile views.");
            if (level == 70) Require(report.dynamic_owl_chapter_egg_refill && game.Session.SpecialItemCounts.Combos.ContainsKey(10),
                "The original propellers combine and the original Egg set fills its board.");
            if (level == 71) Require(report.bush_grass_landed && report.bush_cavity_refilled &&
                !board.Goals.HasPrerequisite(GrassGoal) &&
                board.Presentation.Pool.GetSpawnedCount(62) == 0 && board.Presentation.Pool.GetSpawnedCount(64) == 0,
                "All four bushes and their grass flights recycle before the win.");
            if (level == 80)
            {
                Require(report.bonus_king_idle_connector_verified && report.bonus_king_celebration_verified,
                    "The original level 80 plays its idle connector and celebrates after twenty moves.");
                // A legal twenty-move route need not produce a dead board.
                // Mandatory dead-board coverage is CoreBonusShuffleVerification;
                // any shuffle on this natural route still has to pass its checks.
                if (report.coin_shuffles > 0) Require(shuffleCaptured,
                    "A naturally occurring bonus shuffle has its real animation capture.");
            }
            if (level == 81) Require(report.honey_tutorial_dismissed && report.honey_blocks_drag && report.honey_natural_reveal, "Verified the authored honey opening.");
            if (level == 83) Require(report.honey_reveals_intact_box, "Honey takes the first hit and reveals the original intact box.");
            if (current.honey != 0) Require(board.Presentation.Pool.GetSpawnedCount(43) == 0, "All honey views recycle before the result.");
            if (level == 91) Require(report.safe_tutorial_dismissed && report.safe_blocks_drag && report.safe_resists_match && report.safe_power_damage, "Safe opening checks passed.");
            if (current.safes != 0) Require(board.Presentation.Pool.GetSpawnedCount(65) == 0, "Every safe view recycles after its final layer.");
            if (level == endLevel)
            {
                Capture(level == 3 ? "core_box_level3_win.png" : level >= 91 ? "core_safe_level" + level + "_win.png" : level >= 81 ? "core_honey_level" + level + "_win.png" : level == 80 ? "core_bush_level80_win.png" : level == 70 ? "core_owl_level70_win.png" : level == 60 ? "core_vase_level60_win.png" : level == 50 ? "core_potion_level50_win.png" :
                    level == 40 ? "core_egg_level40_win.png" : "core_mixed_level30_win.png");
                int expectedLevels = endLevel - report.levels[0].level + 1;
                Require(report.levels.Count == expectedLevels, "The chapter ran in one production continuation.");
                if (level == 100)
                {
                    restartAfterCap = true; ownerId = controller.GetInstanceID(); reloading = true;
                    WriteReport();
                    CoreFlowVerification.Click(dialog.ContinueButton, controller.CoreFlow.UiCamera);
                    return;
                }
                report.passed = true;
                Finish();
                return;
            }
            WriteReport();
            ownerId = controller.GetInstanceID();
            CoreFlowVerification.Click(dialog.ContinueButton, controller.CoreFlow.UiCamera);
            level++;
            reloading = true;
        }

        private static int TotalGoals()
        {
            int count = 0;
            foreach (var goal in goals) count += goal.LeftCount;
            return count;
        }
        private static void Gesture(OriginalGridCell source, OriginalGridCell target, bool spendsMove = true)
        {
            from = source;
            to = target;
            movesBefore = game.Moves.LeftMoves;
            goalBefore = TotalGoals();
            moving = spendsMove;
            sample = 0;
            settledAt = -1f;
        }
        private static void SaveOwlRocketCapture()
        {
            if (owlRocketCapture == null) return;
            var capture = owlRocketCapture;
            owlRocketCapture = null;
            try { GrassGameplayVerification.SaveCapture(capture, "core_owl_level61_rocket.png"); }
            finally { UnityEngine.Object.DestroyImmediate(capture); }
        }

        private static void Capture(string file) => GrassGameplayVerification.Capture(controller.CoreFlow.UiCamera, file);
        private static void Require(bool condition, string detail)
        { if (!condition) throw new InvalidOperationException("Level " + level + ". " + detail); }
        private static void WriteReport() => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
        private static void Finish()
        {
            EditorApplication.update -= Tick;
            SaveOwlRocketCapture();
            SceneManager.sceneLoaded -= SceneLoaded;
            if (device != null && device.Listener == null) device.Bind(game);
            report.status = "finished";
            WriteReport();
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
        private sealed class LevelReport
        {
            public int honey, safes;
            public int level, width, height, target_count, mailboxes, cupboards, boxes, grass;
            public int eggs, coins, propellers, collected_coins, potion_racks, vases, owls, bushes;
            public int moves, move_budget, natural_actions, fixture_actions, fixture_combos;
            public bool original_lightball_combo, won, result_title_curve_verified;
            public float center_y;
        }

        [Serializable]
        private sealed class ChapterReport
        {
            public bool passed, projection_during_shake_verified, touch_origin_during_shake_verified;
            public bool egg_tap_verified, egg_reject_verified, egg_fall_verified, egg_burst_verified, egg_swap_verified, egg_natural_swap;
            public bool potion_tap_animation, potion_color_hit, potion_repeated_color_rejected, potion_tnt_rocket_verified, dynamic_egg_refill;
            public bool potion_guided_scores_verified, potion_win_forecast_verified, cupboard_win_forecast_verified;
            public bool box_win_forecast_verified, safe_win_forecast_verified, honey_win_forecast_verified;
            public bool vase_tap, vase_reject, vase_first_layer, vase_final_layer, vase_natural_swap, vase_swap, vase_fall;
            public bool vase_shake_eased_return;
            public bool owl_tap, owl_drag_blocked, owl_rocket_burst, owl_rocket_hit, owl_tnt_hit, owl_resists_adjacent_match;
            public bool dynamic_vase_refill, dynamic_owl_chapter_egg_refill;
            public bool cupboard_door_materials;
            public bool bush_tap, bush_drag_blocked, bush_near_match, bush_grass_landed, bush_cavity_refilled;
            public bool bush_grass_layer_refill;
            public int no_move_native_result;
            public int coin_shuffles, bush_shadow_components;
            public bool bonus_king_idle_connector_verified, bonus_king_celebration_verified, bonus_bottom_tools_verified;
            public bool honey_blocks_drag, honey_natural_reveal, honey_reveals_intact_box, pending_honey_area_score_verified;
            public int honey_first_swap_removed;
            public float honey_recycled_min_alpha;
            public bool safe_blocks_drag, safe_resists_match, safe_power_damage, safe_handle_depth_verified, level_cap_restarts;
            public float safe_handle_particle_z, safe_handle_shadow_z, safe_handle_parent_z;
            public bool tnt_fuse_masked_at_refill, tnt_fuse_unmasked_on_landing;
            public bool honey_tutorial_dismissed, safe_tutorial_dismissed;
            public string[] no_move_board;
            public int bush_grass_flights;
            public int owl_rocket_refills;
            public int vase_fall_setup_hits;
            public int eggs_from_opening_match;
            public string failure;
            public string status = "running";
            public List<LevelReport> levels = new List<LevelReport>();
            public string scope = "Actual GameScene seeded only to level 22, then original Continue buttons. Recorded player input triggers the original level 28 lightball pair and controlled production-factory TNT placements. Checks original mixed goals, ten-row board position, completion and local progression.";
        }
    }
}
