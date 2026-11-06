using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class LightballRocketComboVerification
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
        private static OriginalRocketBoardItem partner;
        private static OriginalLightballRocketStrategy strategy;
        private static readonly List<OriginalRocketBoardItem> converted = new List<OriginalRocketBoardItem>();
        private static readonly Dictionary<int, int> expectedOrientations = new Dictionary<int, int>();
        private static readonly Dictionary<OriginalCellPoint, int> initialCells = new Dictionary<OriginalCellPoint, int>();
        private static readonly List<OriginalCellPoint> triggered = new List<OriginalCellPoint>();
        private static bool[,] originalBoxes;
        private static int test, sample, frame, movesBefore, swapsBefore, refillsBefore, expectedColor, orientation, initialCount, peakParts;
        private static float started, steadyAt, minimumWait;
        private static bool captured;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_lightball_rocket_combo_verification.json");
        public static void Run()
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            fallQueries = new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, game, null), new OriginalFallRefillColumns());
            originalBoxes = new bool[board.Grid.Width, board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                originalBoxes[x, y] = board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBoxBoardItem;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(); EditorApplication.update += Tick;
        }
        private static void Begin()
        {
            FindFixture((test & 4) != 0); Clear(source); Clear(destination);
            bool movingLightball = (test & 1) == 0; orientation = (test >> 1) & 1;
            lightball = (OriginalLightballBoardItem)game.CreateItemAtCell(50, movingLightball ? source : destination);
            partner = (OriginalRocketBoardItem)game.CreateItemAtCell(orientation == 0 ? 30 : 20, movingLightball ? destination : source);
            movesBefore = game.Moves.LeftMoves; swapsBefore = game.AcceptedSwaps; refillsBefore = game.CreatedRefills;
            expectedColor = new OriginalLightballTargeting(board.Grid, board.Layout, new HashSet<int>()).FindMostCommonItem();
            expectedOrientations.Clear(); initialCells.Clear(); converted.Clear(); triggered.Clear();
            int next = orientation;
            for (int y = board.Layout.CurrentBoard.Top; y >= board.Layout.CurrentBoard.Bottom; y--) for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                if (!(cell.Mediation.CurrentItem is OriginalMatchBoardItem item) || item.MatchType != expectedColor || item.IsReserved || !OriginalLightballTargeting.IsReadyForLightball(cell)) continue;
                expectedOrientations.Add(item.Id, next); initialCells.Add(new OriginalCellPoint(x, y), next); next = 1 - next;
            }
            Require(expectedOrientations.Count > 1, "Fixture has a color group for alternating rocket assignment");
            initialCount = expectedOrientations.Count;
            strategy = null; sample = 0; captured = false; minimumWait = float.PositiveInfinity; peakParts = 0;
            started = Time.realtimeSinceStartup; steadyAt = -1f;
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
        {
            var old = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            old.Mediator.ClearFromAllRegisteredCells(); old.RecycleView();
        }
        private static void Triggered(OriginalSpecialBoardItem item, OriginalCellPoint point, float time)
        {
            if (triggered.Count > 0)
            {
                var before = triggered[triggered.Count - 1];
                Require(point.Y < before.Y || point.Y == before.Y && point.X >= before.X, "Native explicit rocket triggers follow top-left order");
                Require(strategy.LastTriggerWaitElapsed >= .15f, "Native .15 gameplay-second wait precedes the next explicit trigger");
                minimumWait = Mathf.Min(minimumWait, strategy.LastTriggerWaitElapsed);
            }
            triggered.Add(point);
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
            {
                string state = "case=" + test + " samples=" + sample + " combo=" + (strategy == null ? "none" : strategy.DidStart + "/converted=" + strategy.ConvertedCount
                    + "/sent=" + strategy.SentRayCount + "/reached=" + strategy.ReachedRayCount + "/complete=" + strategy.CompletedRayCount + "/triggers=" + strategy.ExplicitTriggerCount)
                    + " accepted=" + (game.AcceptedSwaps - swapsBefore) + " steady=" + game.Fall.IsAllItemsSteady + " auto=" + game.Auto.IsThereAWaitingMatch
                    + " swaps=" + board.FirstSwapAction.IsSwapping + "/" + board.SecondSwapAction.IsSwapping
                    + " parts=" + board.Presentation.Pool.GetPoolLength(9) + "/" + board.Presentation.GetParticleAmount(40)
                    + " activeParts=" + board.Presentation.Pool.GetSpawnedCount(9);
                for (int i = 1; i <= 13; i++) state += " op" + i + "=" + game.State.Operations.Container.GetCount(i);
                throw new InvalidOperationException("Rocket combo timed out without forced resets: " + state);
            }
            if (sample < 3)
            {
                var p = (sample == 0 ? source : destination).GetViewPosition();
                samples.Position = new Vector2(p.x, p.y); samples.Down = sample == 0; samples.Held = sample < 2; samples.Up = sample == 2;
                listener.ManualUpdate(); sample++; return;
            }
            if (strategy == null && lightball.Strategy != null)
            {
                strategy = lightball.Strategy as OriginalLightballRocketStrategy;
                Require(strategy != null && strategy.SelectedMatchType == expectedColor, "Formal combo4 selects the native most-common color");
                foreach (var pair in expectedOrientations)
                    Require(strategy.GetAssignedOrientation(pair.Key) == pair.Value, "Orientation alternates in target discovery order before shuffle");
                strategy.ItemTriggered += Triggered;
            }
            if (strategy == null) return;
            peakParts = Math.Max(peakParts, board.Presentation.Pool.GetSpawnedCount(9));
            for (int i = 0; i < strategy.ReplacedItems.Count; i++)
            {
                var item = (OriginalRocketBoardItem)strategy.ReplacedItems[i];
                if (ReferenceEquals(item, partner) || converted.Contains(item)) continue;
                Require(item.IsCreatedByLightball && item.TiledId == (item.Orientation == 0 ? 30 : 20), "Real converted model retains its native orientation/tiled-id/lightball flag");
                Require(item.CurrentCell != null && ReferenceEquals(item.CurrentCell.Mediation.TargetItem, item), "Converted rocket owns current/target mediation");
                var point = new OriginalCellPoint(item.CurrentCell.X, item.CurrentCell.Y);
                if (initialCells.TryGetValue(point, out int expected)) Require(item.Orientation == expected, "Ray conversion retains its assigned orientation");
                converted.Add(item);
            }
            if (!captured && converted.Count >= initialCount && strategy.SentRayCount == strategy.ReachedRayCount)
            {
                if (test == 0) Capture("lightball_rocket_converted.png"); captured = true;
            }
            if (strategy.DidStart || game.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch ||
                board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping || board.Presentation.Pool.GetSpawnedCount(9) != 0)
            { steadyAt = -1f; return; }
            if (steadyAt < 0f) { steadyAt = Time.time; return; }
            if (Time.time - steadyAt < .4f) return;
            Require(captured && converted.Count == strategy.ConvertedCount && converted.Count >= initialCount, "All initial color targets actually become rockets");
            Require(triggered.Count > 0 && triggered.Count == strategy.ExplicitTriggerCount, "Original sorted sequence triggers real rocket models");
            Require(strategy.SentRayCount == strategy.ReachedRayCount && strategy.CompletedRayCount == strategy.SentRayCount, "All actual conversion rays reach, collect and finish");
            Require(board.Presentation.Pool.GetPoolLength(25) == 50 && board.Presentation.Pool.GetPoolLength(26) == 50, "Ray/background pools return completely");
            Require(partner.IsExploded && !partner.HasView && partner.CurrentCell == null, "Original partner also explodes and releases the lightball's center freeze");
            int horizontal = 0, vertical = 0;
            for (int i = 0; i < converted.Count; i++)
            {
                var item = converted[i];
                Require(item.IsExploded && !item.HasView && item.CurrentCell == null, "Every converted rocket actually explodes and releases its source");
                if (item.Orientation == 0) horizontal++; else vertical++;
            }
            Require(horizontal > 0 && vertical > 0, "Both rocket orientations take part in real row/column damage");
            Require(game.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == swapsBefore + 1, "One combo gesture consumes exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(4, out int count) && count == test + 1, "Native combo4 increments once");
            Require(game.CreatedRefills > refillsBefore && game.CanUserMakeMove(), "Actual refill and subsequent player control return");
            int cavities = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y); var item = cell.Mediation.CurrentItem;
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No ray, partner or rocket flight freeze remains");
                if (item == null)
                {
                    var above = cell.Neighbors.Get(1);
                    Require(originalBoxes[x, y] && above?.Mediation.CurrentItem is OriginalBoxBoardItem
                        && !fallQueries.IsFallPossibleFromCellToCell(above, cell) && fallQueries.GetNextItemOfCell(above, cell) == null
                        && fallQueries.FindTargetItemFromCrossCell(cell, cell, 0) == null && fallQueries.FindTargetItemFromCrossCell(cell, cell, 2) == null,
                        "Only original-box cavities without any native straight/cross route can remain empty");
                    cavities++;
                }
                else Require(ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem), "Refilled model slots settle consistently");
            }
            report.horizontal[test] = horizontal; report.vertical[test] = vertical; report.explicit_triggers[test] = triggered.Count;
            report.wait_elapsed[test] = float.IsPositiveInfinity(minimumWait) ? 0f : minimumWait; report.native_box_cavities[test] = cavities;
            report.peak_rocket_parts[test] = peakParts; report.final_available_parts[test] = board.Presentation.Pool.GetPoolLength(9);
            strategy.ItemTriggered -= Triggered; report.completed_combos++;
            if (++test < 8) { Begin(); return; }
            Require(board.Goals.GetGoalLeftCount(11) == 0 && cavities == 0, "All36boxes removed and all72cells refilled by the completed sequence");
            Capture("lightball_rocket_refilled.png");
            report.passed = true; report.left_moves = game.Moves.LeftMoves; report.goals_left = board.Goals.GetGoalLeftCount(11);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n"); Cleanup(); OnFinished?.Invoke();
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
            public int[] horizontal = new int[8], vertical = new int[8], explicit_triggers = new int[8], native_box_cavities = new int[8];
            public int[] peak_rocket_parts = new int[8], final_available_parts = new int[8];
            public float[] wait_elapsed = new float[8];
            public string scope = "Eight formal-scene production-factory lightball/rocket fixtures: both partner orientations, both swap orders and horizontal/vertical recorded gestures. Verifies pre-shuffle orientation assignment, actual conversion rays/models, top-left explicit triggers/.15 gameplay wait, real row/column damage/chains, original flight/pool tails, one move per gesture, input release and native refill paths. After36boxes finish, all72cells must refill. No natural combination creation, physical input, super/static/event/charger/jelly, game lifecycle or rendered-original parity claim.";
        }
    }
}
