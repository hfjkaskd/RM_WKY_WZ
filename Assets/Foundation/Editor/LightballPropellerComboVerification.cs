using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class LightballPropellerComboVerification
    {
        public static event Action OnFinished;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static OriginalFallTargetPlanner fallQueries;
        private static Samples samples;
        private static OriginalGridCell source, destination;
        private static OriginalLightballBoardItem lightball;
        private static OriginalPropellerBoardItem partner;
        private static OriginalLightballPropellerStrategy strategy;
        private static readonly List<OriginalPropellerBoardItem> converted = new List<OriginalPropellerBoardItem>();
        private static readonly HashSet<int> observedFlights = new HashSet<int>();
        private static readonly List<OriginalCellPoint> triggers = new List<OriginalCellPoint>();
        private static bool[,] originalBoxes;
        private static int test, sample, frame, movesBefore, swapsBefore, refillsBefore, expectedColor, peakFlights, peakViews;
        private static float started, steadyAt, minimumWait;
        private static bool captured;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_lightball_propeller_combo_verification.json");
        public static void Run()
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>(); game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>(); device.Unbind();
            samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            fallQueries = new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, game, null), new OriginalFallRefillColumns());
            originalBoxes = new bool[board.Grid.Width, board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                originalBoxes[x, y] = board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBoxBoardItem;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n"); Begin(); EditorApplication.update += Tick;
        }
        private static void Begin()
        {
            FindFixture(test >= 2); Clear(source); Clear(destination);
            lightball = (OriginalLightballBoardItem)game.CreateItemAtCell(50, test % 2 == 0 ? source : destination);
            partner = (OriginalPropellerBoardItem)game.CreateItemAtCell(10, test % 2 == 0 ? destination : source);
            expectedColor = new OriginalLightballTargeting(board.Grid, board.Layout, new HashSet<int>()).FindMostCommonItem();
            Require(expectedColor != 0, "Fixture contains an available color");
            movesBefore = game.Moves.LeftMoves; swapsBefore = game.AcceptedSwaps; refillsBefore = game.CreatedRefills;
            strategy = null; converted.Clear(); observedFlights.Clear(); triggers.Clear(); sample = 0; peakFlights = peakViews = 0;
            captured = false; minimumWait = float.PositiveInfinity; started = Time.realtimeSinceStartup; steadyAt = -1f;
        }
        private static void FindFixture(bool vertical)
        {
            for (int y = 7; y >= (vertical ? 5 : 4); y--) for (int x = 0; x < (vertical ? 9 : 8); x++)
            {
                var a = board.Grid.GetCell(x, y); var b = board.Grid.GetCell(vertical ? x : x + 1, vertical ? y - 1 : y);
                if (!(a.Mediation.CurrentItem is OriginalMatchBoardItem) || !(b.Mediation.CurrentItem is OriginalMatchBoardItem)) continue;
                source = a; destination = b; return;
            }
            throw new InvalidOperationException("No adjacent ordinary fixture cells remain.");
        }
        private static void Clear(OriginalGridCell cell)
        { var old = (OriginalMatchBoardItem)cell.Mediation.CurrentItem; old.Mediator.ClearFromAllRegisteredCells(); old.RecycleView(); }
        private static void Triggered(OriginalSpecialBoardItem item, OriginalCellPoint point, float time)
        {
            Require(item.IsActive, "Hidden original partner is active again before the native trigger15 sequence");
            if (triggers.Count > 0)
            {
                var previous = triggers[triggers.Count - 1];
                Require(point.Y < previous.Y || point.Y == previous.Y && point.X >= previous.X, "Explicit propeller activation follows native top-left order");
                Require(strategy.LastTriggerWaitElapsed >= .07f, "Native .07 gameplay-second wait precedes the next explicit activation");
                minimumWait = Mathf.Min(minimumWait, strategy.LastTriggerWaitElapsed);
            }
            triggers.Add(point);
        }
        private static void Tick()
        {
            try { Step(); }
            catch (Exception e) { report.failure = e.ToString(); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); Cleanup(); throw; }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
            if (Time.realtimeSinceStartup - started >= 40f)
                throw new InvalidOperationException("Propeller combo timed out: case=" + test + " strategy=" + (strategy == null ? "none" : strategy.DidStart + "/" + strategy.ConvertedCount)
                    + " flights=" + game.State.Operations.Container.GetCount(3) + " steady=" + game.Fall.IsAllItemsSteady + " auto=" + game.Auto.IsThereAWaitingMatch);
            if (sample < 3)
            {
                var p = (sample == 0 ? source : destination).GetViewPosition(); samples.Position = new Vector2(p.x, p.y);
                samples.Down = sample == 0; samples.Held = sample < 2; samples.Up = sample == 2; listener.ManualUpdate(); sample++; return;
            }
            if (strategy == null && lightball.Strategy != null)
            {
                strategy = lightball.Strategy as OriginalLightballPropellerStrategy;
                Require(strategy != null && strategy.SelectedMatchType == expectedColor, "Formal combo3 selects the native most-common color");
                Require(!partner.IsActive && partner.ItemView.rootTransform.IsHidden, "Original partner is inactive and its prefab view is hidden during conversion");
                strategy.ItemTriggered += Triggered;
            }
            if (strategy == null) return;
            peakFlights = Math.Max(peakFlights, game.State.Operations.Container.GetCount(3));
            peakViews = Math.Max(peakViews, board.Presentation.Pool.GetSpawnedCount(10));
            for (int i = 0; i < strategy.ReplacedItems.Count; i++)
            {
                var item = (OriginalPropellerBoardItem)strategy.ReplacedItems[i];
                if (ReferenceEquals(item, partner) || converted.Contains(item)) continue;
                Require(item.IsCreatedByLightball && item.TiledId == 10 && item.CurrentCell != null
                    && ReferenceEquals(item.CurrentCell.Mediation.TargetItem, item), "Each real converted propeller keeps its native flag, tile and current/target ownership");
                converted.Add(item);
            }
            for (int i = 0; i < converted.Count; i++) if (converted[i].IsInFlight) observedFlights.Add(converted[i].Id);
            if (!captured && converted.Count > 0 && strategy.SentRayCount == strategy.ReachedRayCount)
            { if (test == 0) Capture("lightball_propeller_converted.png"); captured = true; }
            if (strategy.DidStart || game.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch ||
                board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping) { steadyAt = -1f; return; }
            if (steadyAt < 0f) { steadyAt = Time.time; return; }
            if (Time.time - steadyAt < .4f) return;
            Require(captured && converted.Count > 1 && converted.Count == strategy.ConvertedCount, "Actual color conversion completes for multiple propellers");
            Require(triggers.Count == strategy.ExplicitTriggerCount && triggers.Count > 0 && peakFlights > 1 && observedFlights.Count > 1,
                "Sorted activations start multiple actual concurrent default-pilot flights");
            Require(strategy.SentRayCount == strategy.ReachedRayCount && strategy.CompletedRayCount == strategy.SentRayCount,
                "All real conversion rays reach, collect and finish");
            Require(board.Presentation.Pool.GetSpawnedCount(25) == 0 && board.Presentation.Pool.GetSpawnedCount(26) == 0, "All ray/background lifetimes release");
            for (int i = 0; i < converted.Count; i++)
            {
                var item = converted[i];
                Require(item.IsExploded && !item.HasView && item.CurrentCell == null && !item.IsInFlight && item.TargetItem != null
                    && item.TargetExplodeData.Trigger == 30, "Converted propeller actually resolves a native trigger30 target and releases its flight");
            }
            Require(partner.IsExploded && !partner.HasView && partner.CurrentCell == null && !partner.IsInFlight && partner.TargetExplodeData.Trigger == 8,
                "Original partner preserves normal trigger8 behavior and releases its merged cell");
            Require(game.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == swapsBefore + 1, "One gesture consumes exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(3, out int comboCount) && comboCount == test + 1, "Native combo3 increments once");
            Require(game.CreatedRefills > refillsBefore && game.CanUserMakeMove(), "Real target damage/refill and subsequent player input complete");
            int cavities = 0, boardPropellers = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y); var item = cell.Mediation.CurrentItem;
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No ray/partner/flight fall freeze remains");
                if (item == null)
                {
                    var above = cell.Neighbors.Get(1);
                    while (above != null && above.Mediation.CurrentItem == null) above = above.Neighbors.Get(1);
                    Require(originalBoxes[x, y] && above?.Mediation.CurrentItem is OriginalBoxBoardItem
                        && !fallQueries.IsFallPossibleFromCellToCell(above, cell) && fallQueries.GetNextItemOfCell(above, cell) == null
                        && fallQueries.FindTargetItemFromCrossCell(cell, cell, 0) == null && fallQueries.FindTargetItemFromCrossCell(cell, cell, 2) == null,
                        "Only destroyed original-box cavities without a native straight/cross source may remain empty");
                    cavities++;
                }
                else
                {
                    Require(ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem), "Refilled cell slots settle consistently");
                    if (item is OriginalPropellerBoardItem) boardPropellers++;
                }
            }
            Require(board.Presentation.Pool.GetSpawnedCount(10) == boardPropellers, "Every borrowed propeller view belongs to a remaining real board model");
            report.converted[test] = converted.Count; report.observed_flights[test] = observedFlights.Count; report.peak_flights[test] = peakFlights;
            report.explicit_triggers[test] = triggers.Count; report.native_box_cavities[test] = cavities; report.peak_propeller_views[test] = peakViews;
            report.wait_elapsed[test] = float.IsPositiveInfinity(minimumWait) ? 0f : minimumWait;
            strategy.ItemTriggered -= Triggered; report.completed_combos++;
            if (++test < 4) { Begin(); return; }
            report.passed = true; report.left_moves = game.Moves.LeftMoves; report.goals_left = board.Goals.GetGoalLeftCount(11);
            Capture("lightball_propeller_refilled.png"); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n"); Cleanup(); OnFinished?.Invoke();
        }
        private static void Capture(string name)
        {
            var camera = device.PointerCamera; var target = new RenderTexture(Mathf.RoundToInt(768 * camera.aspect), 768, 24);
            var previous = camera.targetTexture; var active = RenderTexture.active; Texture2D pixels = null;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + name), pixels.EncodeToPNG());
            }
            finally { camera.targetTexture = previous; RenderTexture.active = active; if (pixels != null) UnityEngine.Object.Destroy(pixels); UnityEngine.Object.Destroy(target); }
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick; if (strategy != null) strategy.ItemTriggered -= Triggered;
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
            public bool passed; public string failure; public int completed_combos, left_moves, goals_left;
            public int[] converted = new int[4], observed_flights = new int[4], peak_flights = new int[4], explicit_triggers = new int[4], native_box_cavities = new int[4], peak_propeller_views = new int[4];
            public float[] wait_elapsed = new float[4];
            public string scope = "Four formal production-factory lightball/propeller fixtures exercise both swap orders and both axes via recorded input. Checks actual color conversion, native hidden/active partner state, sorted0.07-second activation, true concurrent single-target flights, created trigger30/original trigger8, actual target damage/refill, one move, input release and complete view/ray/background ownership. Only original-box cavities proven unreachable by native straight/cross queries are permitted. Natural creation, physical input, super/static/event/charger/jelly, full lifecycle and rendered-original parity remain unverified.";
        }
    }
}
