using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Production-factory fixture only; all input, combo, rocket flight/damage,
    // fall, refilling, move counting and pooling use the formal scene owners.
    public static class RocketRocketComboVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static Samples samples;
        private static OriginalRocketBoardItem moving, other;
        private static OriginalGridCell source, destination;
        private static OriginalFallTargetPlanner fallQueries;
        private static bool[,] boxSlots;
        private static int test, frame, sampleFrame, movesBefore, acceptedBefore, refillsBefore, goalsBefore;
        private static float started, steadySince;
        private static string failure;
        private static readonly Vector3[] starts = new Vector3[4];
        private static Transform[] parts;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_rocket_rocket_combo_verification.json");
        public static void Run()
        {
            if (!EditorApplication.isPlaying || board != null) throw new InvalidOperationException("Fresh Play session required.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            Require(game != null && board.Moves.MadeMoves == 0, "Fresh formal board");
            boxSlots = new bool[board.Grid.Width, board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                boxSlots[x, y] = board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBoxBoardItem;
            fallQueries = new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, game, null), new OriginalFallRefillColumns());
            device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            test = 0; frame = Time.frameCount; failure = null;
            Application.logMessageReceived += ObserveLog;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(); EditorApplication.update += Tick;
        }
        private static void Begin()
        {
            int x = test == 0 ? 4 : test == 1 ? 0 : test == 2 ? 2 : 8;
            bool verticalGesture = test == 1 || test == 3;
            source = board.Grid.GetCell(x, verticalGesture ? 7 : 6);
            destination = board.Grid.GetCell(x + (verticalGesture ? 0 : 1), 6);
            moving = PlaceRocket(source, test / 2); other = PlaceRocket(destination, test % 2);
            parts = new[] { moving.ItemView.FirstPart.transform, moving.ItemView.SecondPart.transform,
                other.ItemView.FirstPart.transform, other.ItemView.SecondPart.transform };
            for (int i = 0; i < parts.Length; i++) starts[i] = parts[i].position;
            movesBefore = board.Moves.LeftMoves; acceptedBefore = game.AcceptedSwaps;
            refillsBefore = game.CreatedRefills; goalsBefore = board.Goals.GetGoalLeftCount(11);
            sampleFrame = 0; started = Time.realtimeSinceStartup; steadySince = -1f;
        }
        private static OriginalRocketBoardItem PlaceRocket(OriginalGridCell cell, int orientation)
        {
            var old = cell.Mediation.CurrentItem as OriginalMatchBoardItem;
            Require(old != null, "Fixture replaces ordinary match tile only");
            old.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)old.SwapView);
            // Original ItemModelFactory: tiled30=horizontal0, tiled20=vertical1.
            var model = (OriginalRocketBoardItem)game.CreateItemAtCell(orientation == 0 ? 30 : 20, cell);
            Require(model.Orientation == orientation, "Fixture uses original tiled-id orientation mapping");
            return model;
        }
        private static void Tick()
        {
            try { Step(); }
            catch (Exception error)
            {
                report.passed = false; report.failure = error.ToString();
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n"); Cleanup(); throw;
            }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            if (failure != null) throw new InvalidOperationException(failure);
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            Require(Time.realtimeSinceStartup - started < 20f, "Combination settles within20seconds");
            if (moving.IsExploded && other.IsExploded)
            {
                report.peak_operations[test] = Math.Max(report.peak_operations[test], board.State.Operations.Container.GetCount(4));
                report.peak_particles[test] = Math.Max(report.peak_particles[test], board.Presentation.GetParticleAmount(40) - board.Presentation.Pool.GetPoolLength(9));
                for (int i = 0; i < parts.Length; i++)
                    report.part_distances[test * 4 + i] = Mathf.Max(report.part_distances[test * 4 + i], Vector3.Distance(starts[i], parts[i].position));
                Require(moving.Orientation == 1 - test % 2 && other.Orientation == test % 2, "Native opposite orientation follows destination rocket");
                Require(moving.CurrentCell == null && other.CurrentCell == null, "Combo preparation releases both cell mediators");
                Require(!moving.SwapView.IsSwapping && !other.SwapView.IsSwapping, "Both native combo swap callbacks end");
            }
            if (sampleFrame < 3)
            {
                var p = (sampleFrame == 0 ? source : destination).GetViewPosition();
                samples.Position = new Vector2(p.x, p.y); samples.Down = sampleFrame == 0;
                samples.Held = sampleFrame < 2; samples.Up = sampleFrame == 2;
                listener.ManualUpdate(); sampleFrame++; return;
            }
            if (!moving.IsExploded || !other.IsExploded) return;
            if (board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping || !game.Fall.IsAllItemsSteady
                || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch) { steadySince = -1f; return; }
            if (steadySince < 0f) { steadySince = Time.time; return; }
            if (Time.time - steadySince < 1.6f || board.Presentation.Pool.GetPoolLength(9) != board.Presentation.GetParticleAmount(40)) return;
            Require(board.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == acceptedBefore + 1, "One combination drag spends one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(7, out int count) && count == test + 1, "One original combo7 record per drag");
            Require(report.peak_operations[test] >= 4 && report.peak_particles[test] >= 4, "Four actual rocket-part operations and particle instances");
            for (int i = 0; i < 4; i++) Require(report.part_distances[test * 4 + i] > 1f, "Every original rocket part actually flies");
            Require(game.CreatedRefills > refillsBefore && board.Goals.GetGoalLeftCount(11) < goalsBefore, "Cross flight hits real goals then refills");
            Require(board.Moves.CanUserMakeMove(), "Player control unlocks after flight/refill");
            int empty = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y); if (!cell.IsNormalCell) continue;
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No swap/rocket freeze remains");
                var item = cell.Mediation.CurrentItem;
                if (item == null)
                {
                    Require(boxSlots[x, y] && cell.Mediation.NextItem == null && cell.Mediation.TargetItem == null
                        && cell.Neighbors.Get(1)?.Mediation.CurrentItem is OriginalBoxBoardItem
                        && !fallQueries.IsFallPossibleFromCellToCell(cell.Neighbors.Get(1), cell)
                        && fallQueries.GetNextItemOfCell(cell.Neighbors.Get(1), cell) == null
                        && fallQueries.FindTargetItemFromCrossCell(cell, cell, 0) == null
                        && fallQueries.FindTargetItemFromCrossCell(cell, cell, 2) == null, "Only native blocked original-box cavities may remain");
                    empty++; continue;
                }
                Require(ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem), "Three model slots agree after refill");
                var v = board.FallMotion.GetView(item);
                if (v != null)
                {
                    var p = v.Position;
                    Require(Vector3.Distance(new Vector3(p.X, p.Y, p.Z), cell.GetViewPosition()) < .0011f, "View lands at model cell");
                }
            }
            report.completed_combos++; report.blocked_box_holes[test] = empty;
            if (++test < 4) { Begin(); return; }
            report.passed = true; report.left_moves = board.Moves.LeftMoves; report.accepted_swaps = game.AcceptedSwaps;
            report.goals_left = board.Goals.GetGoalLeftCount(11); report.created_refills = game.CreatedRefills;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("Formal rocket+rocket verification passed: four initial orientation combinations, real flight/damage/refill/moves/pools and unlock."); Cleanup();
        }
        private static void ObserveLog(string text, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failure = text; }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= ObserveLog;
            if (device != null && game != null && EditorApplication.isPlaying && device.Listener == null) device.Bind(game);
            board = null; game = null; device = null; listener = null;
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
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
            public bool passed; public string failure;
            public int completed_combos, left_moves, accepted_swaps, goals_left, created_refills;
            public int[] peak_operations = new int[4], peak_particles = new int[4], blocked_box_holes = new int[4];
            public float[] part_distances = new float[16];
            public string scope = "Explicit production-factory two-rocket fixtures in unchanged formal first-board scene, four initial HH/HV/VH/VV combinations with center/edge horizontal/vertical recorded gestures. Real source views/flight/damage/pools/goals/moves/fall/refill/input unlock. Does not establish natural two-rocket creation, physical input, rendered-original/camera/audio/charger/session/othercombos/settlement parity.";
        }
    }
}
