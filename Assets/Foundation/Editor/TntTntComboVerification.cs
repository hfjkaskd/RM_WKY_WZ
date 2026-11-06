using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Explicit fixture setup places two TNTs with the production item factory.
    // The unchanged formal scene then receives frame-separated device samples;
    // no substitute resolution, animation timer, fall or refill owner is used.
    public static class TntTntComboVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static Samples samples;
        private static OriginalTntBoardItem first, second;
        private static OriginalTntTntComboView view, previousView;
        private static OriginalGridCell source, destination;
        private static OriginalFallTargetPlanner fallQueries;
        private static OriginalGridShakeAnimation gridShake;
        private static Vector3 restPosition;
        private static bool[,] boxSlots;
        private static int test, frame, sampleFrame, movesBefore, swapsBefore, refillsBefore, goalsBefore;
        private static bool observedAnimation, observedDamage;
        private static float started, animationStarted, steadySince;
        private static string failure;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_tnt_tnt_combo_verification.json");
        public static void Run()
        {
            if (!EditorApplication.isPlaying || board != null) throw new InvalidOperationException("Fresh Play session required.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            gridShake = board.BoardRoot.GetComponent<OriginalGridShakeAnimation>();
            restPosition = board.BoardRoot.localPosition;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            Require(game != null && board.Moves.MadeMoves == 0, "Fresh formal board");
            boxSlots = new bool[board.Grid.Width, board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                boxSlots[x, y] = board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBoxBoardItem;
            fallQueries = new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, game, null), new OriginalFallRefillColumns());
            device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            test = 0; previousView = null; failure = null; frame = Time.frameCount;
            Application.logMessageReceived += ObserveLog;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(); EditorApplication.update += Tick;
        }
        private static void Begin()
        {
            source = board.Grid.GetCell(test == 0 ? 4 : 0, test == 0 ? 6 : 7);
            destination = board.Grid.GetCell(test == 0 ? 5 : 0, 6);
            first = PlaceTnt(source); second = PlaceTnt(destination);
            movesBefore = board.Moves.LeftMoves; swapsBefore = game.AcceptedSwaps;
            refillsBefore = game.CreatedRefills; goalsBefore = board.Goals.GetGoalLeftCount(11);
            sampleFrame = 0; observedAnimation = observedDamage = false; view = null;
            started = Time.realtimeSinceStartup; steadySince = -1f;
        }
        private static OriginalTntBoardItem PlaceTnt(OriginalGridCell cell)
        {
            var old = cell.Mediation.CurrentItem as OriginalMatchBoardItem;
            Require(old != null, "Fixture replaces an ordinary match tile only");
            old.Mediator.ClearFromAllRegisteredCells();
            board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)old.SwapView);
            return (OriginalTntBoardItem)game.CreateItemAtCell(40, cell);
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
            Require(Time.realtimeSinceStartup - started < 20f, "Combo command settles within20seconds");
            if (sampleFrame < 3)
            {
                var p = (sampleFrame == 0 ? source : destination).GetViewPosition();
                samples.Position = new Vector2(p.x, p.y); samples.Down = sampleFrame == 0;
                samples.Held = sampleFrame < 2; samples.Up = sampleFrame == 2;
                listener.ManualUpdate(); sampleFrame++; return;
            }
            if (game.TntTntCombo.IsComboActive)
            {
                Require(!gridShake.IsShaking, "No grid shake before the TNT merge completes");
                if (!observedAnimation)
                {
                    observedAnimation = true; animationStarted = Time.time;
                    view = UnityEngine.Object.FindObjectOfType<OriginalTntTntComboView>();
                    Require(view != null && (previousView == null || view == previousView), "Configured combo view is pooled/reused");
                    previousView = view;
                    Require(first.Config.Radius == 4 && second.Config.Radius == 0, "Moving/source TNT gains radius4; neighbor radius0");
                    Require(!first.IsActive && second.IsActive && !first.IsExploded && !second.IsExploded, "Damage waits for native Animator completion");
                    Require(source.FallFreezeCount > 0 && destination.FallFreezeCount > 0, "Both source cells remain frozen during merge");
                    Require(view.sortingGroup.sortingLayerID == SortingLayer.NameToID("Ui") && view.sortingGroup.sortingOrder == 8000, "Native Ui8000 sorting");
                    Require(board.Presentation.Pool.GetPoolLength(15) == 0 && board.Presentation.Pool.GetPoolLength(18) == 0, "Native combo/fuse pools spawned");
                }
                Require(!first.IsExploded && !second.IsExploded, "No early TNT damage while combination plays");
                Require(first.IsPreparedForSpecialUse && second.IsPreparedForSpecialUse, "Combo-owned TNTs suppress automatic-trigger counters");
                return;
            }
            if (!observedAnimation) return;
            report.peak_shake[test] = Mathf.Max(report.peak_shake[test],
                Vector3.Distance(restPosition, board.BoardRoot.localPosition));
            if (!observedDamage)
            {
                observedDamage = true;
                report.animation_seconds[test] = Time.time - animationStarted;
                Require(report.animation_seconds[test] > 1.4f && report.animation_seconds[test] < 1.8f, "Recovered1.5833334second animation completes naturally");
                Require(first.IsExploded && second.IsExploded && !first.HasView && !second.HasView, "Both real TNT model/view explosions complete");
                Require(board.Presentation.Pool.GetPoolLength(15) == 1 && board.Presentation.Pool.GetPoolLength(18) == 1, "Combo view/fuse recycled");
                Require(board.Presentation.Pool.GetPoolLength(22) == 0, "Original combo explosion particles spawned");
                report.particles_spawned++;
            }
            if (board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping || !game.Fall.IsAllItemsSteady
                || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch) { steadySince = -1f; return; }
            if (steadySince < 0f) { steadySince = Time.time; return; }
            if (Time.time - steadySince < 1.6f) return;
            Require(first.CurrentCell == null && second.CurrentCell == null, "Both original source cells released");
            Require(board.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == swapsBefore + 1, "One player drag spends exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(5, out int count) && count == test + 1, "One native combo-use record per drag");
            Require(game.CreatedRefills > refillsBefore, "Real fall/refill follows combination damage");
            Require(board.Goals.GetGoalLeftCount(11) < goalsBefore, "Original combo damage reaches box goals");
            Require(board.Presentation.Pool.GetPoolLength(22) == 1, "Timed combo explosion particles recycled");
            Require(board.Moves.CanUserMakeMove(), "Player control unlocks after combination and refill");
            Require(report.peak_shake[test] > .05f && report.peak_shake[test] <= .425f &&
                !gridShake.IsShaking && board.BoardRoot.localPosition == restPosition,
                $"The radius-four TNT shakes the board and returns to rest. Peak {report.peak_shake[test]}, " +
                $"active {gridShake.IsShaking}, rest {restPosition}, final {board.BoardRoot.localPosition}");
            int empty = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y); if (!cell.IsNormalCell) continue;
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No combo/swap/radius freeze remains");
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
                Require(ReferenceEquals(item, cell.Mediation.NextItem) && ReferenceEquals(item, cell.Mediation.TargetItem), "Three native model slots agree");
                var v = board.FallMotion.GetView(item);
                if (v != null)
                {
                    var p = v.Position;
                    Require(Vector3.Distance(new Vector3(p.X, p.Y, p.Z), cell.GetViewPosition()) < .0011f, "View lands at actual model cell");
                }
            }
            report.completed_combos++; report.blocked_box_holes[test] = empty;
            if (++test < 2) { Begin(); return; }
            report.passed = true; report.left_moves = board.Moves.LeftMoves; report.accepted_swaps = game.AcceptedSwaps;
            report.goals_left = board.Goals.GetGoalLeftCount(11); report.created_refills = game.CreatedRefills;
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("Formal TNT+TNT combo verification passed: two drags, original Animator, blast, moves, goals, refill, pools and input unlock.");
            Cleanup();
        }
        private static void ObserveLog(string text, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failure = text; }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= ObserveLog;
            if (device != null && game != null && EditorApplication.isPlaying && device.Listener == null) device.Bind(game);
            board = null; game = null; device = null; listener = null;
        }
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
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
            public int completed_combos, particles_spawned, left_moves, accepted_swaps, goals_left, created_refills;
            public float[] peak_shake = new float[2];
            public float[] animation_seconds = new float[2]; public int[] blocked_box_holes = new int[2];
            public string scope = "Explicit production-factory two-TNT fixture in unchanged formal first-board scene; horizontal center and vertical edge drags via recorded device samples. Real combo/Animator/pools/radius damage/move/goal/fall/refill/unlock owners. Does not prove natural two-TNT creation, physical input, rendered-original/audio/charger parity, other combinations or full settlement.";
        }
    }
}
