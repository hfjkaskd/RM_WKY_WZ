using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class LightballTntComboVerification
    {
        internal const string OverlapTest = "core-lightball-tnt-overlap";
        internal const string OverlapReportPath = "live_core_lightball_tnt_overlap_verification.json";

        internal static void RunOverlap(FoundationController owner) => owner.StartCoroutine(VerifyOverlap(owner));

        private static IEnumerator VerifyOverlap(FoundationController owner)
        {
            var board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            var game = owner.Gameplay;
            var from = board.Grid.GetCell(0, 8);
            var to = board.Grid.GetCell(1, 8);
            int goalsBefore = board.Goals.GetGoalLeftCount(11);
            int movesBefore = game.Moves.LeftMoves;
            Require(owner.CurrentLevelNumber == 3 && goalsBefore == 25,
                "Start with the original level 3 double-layer box layout.");
            var ball = (OriginalLightballBoardItem)CoreKingComboVerification.Place(game, board, from, 50);
            CoreKingComboVerification.Place(game, board, to, 40);
            UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>().Unbind();
            var samples = new CoreModalTutorialVerification.GestureSamples();
            var listener = new OriginalGameTouchListener(samples, game);
            yield return CoreKingComboVerification.Gesture(listener, samples, from, to);

            float deadline = Time.realtimeSinceStartup + 20f;
            while (ball.Strategy is not OriginalLightballTntStrategy { CreationAnimationStartedAt: >= 0f })
            {
                Require(Time.realtimeSinceStartup < deadline, "Wait for the production conversion and fuse.");
                yield return null;
            }
            var strategy = (OriginalLightballTntStrategy)ball.Strategy;
            Require(strategy.ExplosionStartedAt < 0f, "Observe the converted board before the collective blast.");
            var boxes = new List<(OriginalGridCell cell, OriginalBoxBoardItem box, int coverage)>();
            for (int x = 0; x < board.Grid.Width; x++)
            for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (cell.Mediation.CurrentItem is not OriginalBoxBoardItem { LayerCount: 2 } box) continue;
                int coverage = 0;
                foreach (var item in strategy.ReplacedItems)
                {
                    var tnt = (OriginalTntBoardItem)item;
                    var center = tnt.CurrentCell;
                    if (center != null && Mathf.Max(Mathf.Abs(center.X - x), Mathf.Abs(center.Y - y)) <= tnt.Config.Radius)
                        coverage++;
                }
                boxes.Add((cell, box, coverage));
            }
            Require(boxes.Count == goalsBefore, "Every original box has both layers before the blast.");
            while (strategy.ExplosionStartedAt < 0f || game.State.GetSpecialOperationCount() > 0)
            {
                Require(Time.realtimeSinceStartup < deadline, "Wait for all TNT waves to finish.");
                yield return null;
            }

            var report = new OverlapReport
            {
                converted_tnt = strategy.ConvertedCount,
                moves_spent = movesBefore - game.Moves.LeftMoves,
                box_goals_before = goalsBefore,
                box_goals_after = board.Goals.GetGoalLeftCount(11),
                box_goals_ui_after = board.Goals.GetGoalUiLeftCount(11),
            };
            foreach (var (cell, box, coverage) in boxes)
            {
                int expected = coverage == 0 ? 2 : 1;
                if (coverage > 1) report.overlapping_boxes++;
                if (coverage > 0) report.covered_boxes++;
                if (box.LayerCount != expected || cell.Mediation.CurrentItem != box)
                    report.unexpected_boxes.Add(new Vector2Int(cell.X, cell.Y));
            }
            report.passed = report.overlapping_boxes > 0 && report.unexpected_boxes.Count == 0 &&
                report.moves_spent == 1 && report.box_goals_after == goalsBefore && report.box_goals_ui_after == goalsBefore;
            GrassGameplayVerification.Capture(Camera.main, "core_lightball_tnt_overlap.png");
            File.WriteAllText(OverlapReportPath, JsonUtility.ToJson(report, true) + "\n");
            Require(report.passed, "The shared explosion removes exactly one layer from every covered box.");
        }

        [Serializable] private sealed class OverlapReport
        {
            public bool passed;
            public int converted_tnt, moves_spent, box_goals_before, box_goals_after, box_goals_ui_after;
            public int overlapping_boxes, covered_boxes;
            public List<Vector2Int> unexpected_boxes = new();
        }

        public static event Action OnFinished;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameAudioHost audio;
        private static OriginalGameTouchListener listener;
        private static Samples samples;
        private static OriginalLightballBoardItem lightball;
        private static OriginalTntBoardItem partner;
        private static OriginalLightballTntStrategy strategy;
        private static OriginalDefaultLightballStrategy defaultStrategy;
        private static OriginalGridCell source, destination;
        private static readonly List<OriginalTntBoardItem> converted = new List<OriginalTntBoardItem>();
        private static readonly List<OriginalMatchBoardItem> selected = new List<OriginalMatchBoardItem>();
        private static int test, sample, frame, movesBefore, acceptedBefore, refillsBefore, createdBefore, expectedColor;
        private static float started, steadyAt;
        private static bool sawFuse, checkedImmunity;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_lightball_tnt_combo_verification.json");
        public static void Run()
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            audio = UnityEngine.Object.FindObjectOfType<OriginalGameAudioHost>();
            device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(); EditorApplication.update += Tick;
        }
        private static void Begin()
        {
            if (test >= 4) { BeginDefault(); return; }
            FindFixture(test >= 2);
            Clear(source); Clear(destination);
            lightball = (OriginalLightballBoardItem)game.CreateItemAtCell(50, test % 2 == 0 ? source : destination);
            partner = (OriginalTntBoardItem)game.CreateItemAtCell(40, test % 2 == 0 ? destination : source);
            movesBefore = game.Moves.LeftMoves; acceptedBefore = game.AcceptedSwaps; refillsBefore = game.CreatedRefills;
            createdBefore = game.Session.SpecialItemCounts.Items.TryGetValue(4, out var c) ? c.LightballCreationCount : 0;
            expectedColor = new OriginalLightballTargeting(board.Grid, board.Layout, new HashSet<int>()).FindMostCommonItem();
            Require(expectedColor != 0, "Fixture contains normal tiles for color conversion");
            strategy = null; converted.Clear(); selected.Clear(); sample = 0; sawFuse = checkedImmunity = false;
            started = Time.realtimeSinceStartup; steadyAt = -1f;
        }
        private static void BeginDefault()
        {
            FindFixture(false);
            Clear(source); lightball = (OriginalLightballBoardItem)game.CreateItemAtCell(50, source);
            if (test == 4) destination = source;
            defaultStrategy = null; sample = 0;
            movesBefore = game.Moves.LeftMoves; acceptedBefore = game.AcceptedSwaps; refillsBefore = game.CreatedRefills;
            started = Time.realtimeSinceStartup; steadyAt = -1f;
        }
        private static void FindFixture(bool vertical)
        {
            // Cascades can naturally create specials at a previous fixture's location.
            // Preserve those and select two adjacent regular tiles on the stable board.
            for (int y = 7; y >= (vertical ? 5 : 4); y--) for (int x = 0; x < (vertical ? 9 : 8); x++)
            {
                var a = board.Grid.GetCell(x, y); var b = board.Grid.GetCell(vertical ? x : x + 1, vertical ? y - 1 : y);
                if (!(a.Mediation.CurrentItem is OriginalMatchBoardItem) || !(b.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                source = a; destination = b; return;
            }
            throw new InvalidOperationException("No adjacent ordinary fixture cells remain on the stable upper board.");
        }
        private static void Clear(OriginalGridCell cell)
        {
            Require(cell.Mediation.CurrentItem is OriginalMatchBoardItem, "Fixture only replaces a regular tile");
            var old = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            old.Mediator.ClearFromAllRegisteredCells(); old.RecycleView();
        }
        private static void Tick()
        {
            try { Step(); }
            catch (Exception e) { report.failure = e.ToString(); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); Cleanup(); throw; }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
            Require(Time.realtimeSinceStartup - started < 30f, "Lightball conversion, TNT damage and refill finish without forced resets");
            if (sample < 3)
            {
                var p = (sample == 0 ? source : destination).GetViewPosition();
                samples.Position = new Vector2(p.x, p.y); samples.Down = sample == 0; samples.Held = sample < 2; samples.Up = sample == 2;
                listener.ManualUpdate();
                if (sample == (test == 4 ? 2 : 1))
                {
                    Require(CountChargeSources() == 1, "Lightball use starts one pooled loop with the original charge clip.");
                    report.charge_starts++;
                }
                sample++; return;
            }
            if (test >= 4) { StepDefault(); return; }
            if (strategy == null && lightball.Strategy != null)
            {
                strategy = lightball.Strategy as OriginalLightballTntStrategy;
                Require(strategy != null && strategy.SelectedMatchType == expectedColor, "Production combo2 chooses the original most-common color");
                for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
                    if (board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalMatchBoardItem item && item.IsReserved && item.MatchType == expectedColor)
                        selected.Add(item);
            }
            if (strategy == null) return;
            for (int i = 0; i < strategy.ReplacedItems.Count; i++)
            {
                var item = (OriginalTntBoardItem)strategy.ReplacedItems[i];
                if (ReferenceEquals(item, partner) || converted.Contains(item)) continue;
                Require(item.IsCreatedByLightball && item.Config.Radius == 2, "Each ray creates a real normal-radius TNT with the native lightball flag");
                Require(item.CurrentCell != null && ReferenceEquals(item.CurrentCell.Mediation.TargetItem, item), "Converted TNT owns current and target cell slots");
                converted.Add(item);
                if (!checkedImmunity)
                {
                    item.Explode(OriginalExploder.Next(14));
                    Require(!item.IsExploded && item.HasView, "Lightball-created TNT ignores other TNT hits before trigger15");
                    checkedImmunity = true;
                }
            }
            if (!sawFuse && strategy.CreationAnimationStartedAt >= 0f && strategy.ExplosionStartedAt < 0f)
            {
                sawFuse = true;
                Require(converted.Count > 0 && strategy.SentRayCount == strategy.ReachedRayCount && strategy.SentRayCount == strategy.CompletedRayCount,
                    "Every actual ray reaches and completes before the fuse phase");
                Require(!lightball.HasView && partner.CurrentCell != null, "Original partner replaces the lightball at the merged swap location");
                if (!game.LightballAssets.speedIncrease)
                    Require(CountChargeSources() == 0, "Lightball removal ends the ordinary loop before the TNT fuse wait.");
                for (int i = 0; i < selected.Count; i++) Require(!selected[i].HasView && selected[i].Mediator.CurrentCell == null, "Color conversion removes the old model and pooled view");
                Require(converted[0].LightballExplodeData.Trigger == 14, "TNT receives the native lightball creation explode data");
                if (test == 0) Capture();
            }
            if (strategy.DidStart || game.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch ||
                board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping) { steadyAt = -1f; return; }
            if (steadyAt < 0f) { steadyAt = Time.time; return; }
            if (Time.time - steadyAt < .4f) return;
            Require(sawFuse && checkedImmunity && strategy.ConvertedCount == converted.Count, "Real conversions and fuse phase observed");
            float delay = strategy.ExplosionStartedAt - strategy.CreationAnimationStartedAt;
            report.fuse_delays[test] = delay; report.wait_elapsed[test] = strategy.ExplosionWaitElapsed;
            // Native WaitForGameplaySeconds consumes the level delta on each coroutine poll,
            // while LevelContext.Time advances in LateUpdate. Compare the wait's own elapsed
            // gameplay duration, not an assumed equality between these two clocks.
            Require(strategy.ExplosionWaitElapsed >= .5f, "Native .5 gameplay-second wait completes before collective trigger15");
            for (int i = 0; i < converted.Count; i++) Require(converted[i].IsExploded && !converted[i].HasView && converted[i].CurrentCell == null, "Every converted TNT actually explodes and releases its cell");
            Require(partner.IsExploded && !partner.HasView && partner.CurrentCell == null, "Original partner also explodes");
            Require(game.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == acceptedBefore + 1, "One recorded gesture consumes exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(2, out int comboCount) && comboCount == test + 1, "Native combo2 increments once");
            Require(game.Session.SpecialItemCounts.Items[4].LightballCreationCount > createdBefore, "Native lightball creation statistics recorded");
            Require(game.CreatedRefills > refillsBefore && game.Moves.CanUserMakeMove(), "Actual refill and player control return");
            for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "All ray and center freezes released");
                if (y >= 4 || board.Goals.GetGoalLeftCount(11) == 0)
                    Require(cell.Mediation.CurrentItem != null, "Reachable board has no holes, including all lower cells after the last box is removed");
            }
            VerifyChargeReleased();
            report.converted_counts[test] = converted.Count; report.fuse_delays[test] = delay; report.completed_combos++;
            test++; Begin();
        }
        private static void StepDefault()
        {
            if (defaultStrategy == null) defaultStrategy = lightball.Strategy;
            if (defaultStrategy == null || defaultStrategy.DidStart || game.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady ||
                game.Auto.IsThereAWaitingMatch || board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping)
            { steadyAt = -1f; return; }
            if (steadyAt < 0f) { steadyAt = Time.time; return; }
            if (Time.time - steadyAt < .4f) return;
            Require(!(defaultStrategy is OriginalLightballTntStrategy) && !lightball.HasView && lightball.CurrentCell == null,
                "Normal lightball activation uses the default strategy and releases its source cell");
            Require(defaultStrategy.SentRayCount > 0 && defaultStrategy.CompletedRayCount == defaultStrategy.SentRayCount,
                "Normal lightball rays complete after the combination strategies reused their pools");
            Require(board.Presentation.Pool.GetPoolLength(26) == 50 && board.Presentation.Pool.GetPoolLength(25) == 50,
                "All actual ray and background instances return to their configured pools");
            Require(game.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == acceptedBefore + (test == 4 ? 0 : 1),
                "Normal tap and swap each consume exactly one move");
            Require(game.CreatedRefills > refillsBefore && game.CanUserMakeMove(), "Normal lightball refills and unlocks player input");
            for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                Require(cell.Mediation.CurrentItem != null && !cell.IsFallFrozen && cell.CrossFallFreezeCount == 0,
                    "After box removal all 72 cells remain filled and unfrozen");
            }
            VerifyChargeReleased();
            report.default_ray_counts[test - 4] = defaultStrategy.SentRayCount; report.normal_lightball_activations++;
            if (++test < 6) { Begin(); return; }
            Capture("lightball_tnt_refilled.png");
            report.passed = true; report.left_moves = game.Moves.LeftMoves; report.goals_left = board.Goals.GetGoalLeftCount(11);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n"); Cleanup(); OnFinished?.Invoke();
        }
        private static int CountChargeSources()
        {
            var clip = audio.GameAssets.GetClip(219);
            Require(clip != null, "The original lightball charge clip resolves.");
            int count = 0;
            foreach (var pooled in UnityEngine.Object.FindObjectsOfType<OriginalPoolableAudioSource>())
                if (pooled.audioSource.clip == clip && pooled.audioSource.loop && pooled.audioSource.isPlaying) count++;
            return count;
        }
        private static void VerifyChargeReleased()
        {
            Require(CountChargeSources() == 0 && audio.Playback.GetLoopCount(219) == 0,
                "The lightball charge loop returns to the audio pool after the move.");
            report.charge_releases++;
        }
        private static void Capture(string name = "lightball_tnt_converted.png")
        {
            var camera = device.PointerCamera;
            var target = new RenderTexture(Mathf.RoundToInt(768 * camera.aspect), 768, 24);
            var previous = camera.targetTexture; var active = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + name), pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previous; RenderTexture.active = active;
                if (pixels != null) UnityEngine.Object.Destroy(pixels);
                UnityEngine.Object.Destroy(target);
            }
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (device != null && game != null && device.Listener == null) device.Bind(game);
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private sealed class Samples : IOriginalGameTouchInput
        {
            public Vector2 Position; public bool Down, Held, Up;
            public Vector2 GetMouseWorldPosition() => Position;
            public bool GetMouseButtonDown() => Down;
            public bool GetMouseButton() => Held;
            public bool GetMouseButtonUp() => Up;
        }
        [Serializable] private sealed class Report
        {
            public bool passed; public string failure; public int completed_combos, normal_lightball_activations, left_moves, goals_left;
            public int[] converted_counts = new int[4]; public float[] fuse_delays = new float[4], wait_elapsed = new float[4];
            public int[] default_ray_counts = new int[2];
            public int charge_starts, charge_releases;
            public string scope = "Four explicit production-factory fixtures swap lightball/TNT in both orders horizontally and vertically, then two production-factory normal-lightball tap/swap fixtures through recorded player input. Checks color/rays/model conversion, TNT immunity, fuse phase, collective explosion, one move, real damage/refill, no frozen cells, all72 cells filled after box removal, subsequent input and complete ray/background pool return. No natural special creation, physical input, original rendered comparison, super lightball, static/event/charger or other combo parity claim.";
        }
    }
}
