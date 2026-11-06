using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // Production-factory fixture only; all input, combo, rocket flight/damage,
    // fall, refilling, move counting and pooling use the formal scene owners.
    public static class TntRocketComboVerification
    {
        public static event Action OnFinished;
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static Samples samples;
        private static OriginalSpecialBoardItem moving, other;
        private static int rocketCount;
        private static bool comboCompleted, observedMerge;
        private static float mergeStarted;
        private static OriginalGridCell source, destination;
        private static OriginalFallTargetPlanner fallQueries;
        private static OriginalGridShakeAnimation gridShake;
        private static Vector3 restPosition;
        private static bool[,] boxSlots;
        private static int test, frame, sampleFrame, movesBefore, acceptedBefore, refillsBefore, goalsBefore;
        private static float started, steadySince;
        private static string failure;
        private static readonly Vector3[] starts = new Vector3[12];
        private static Transform[] parts;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_tnt_rocket_combo_verification.json");
        public static void Run(bool afterOpening = false)
        {
            if (!EditorApplication.isPlaying || board != null) throw new InvalidOperationException("Fresh Play session required.");
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            gridShake = board.BoardRoot.GetComponent<OriginalGridShakeAnimation>();
            restPosition = board.BoardRoot.localPosition;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            Require(game != null && board.Moves.MadeMoves == (afterOpening ? 1 : 0), "Formal board has the expected starting moves");
            boxSlots = new bool[board.Grid.Width, board.Grid.Height];
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
                boxSlots[x, y] = board.Grid.GetCell(x, y).Mediation.CurrentItem is OriginalBoxBoardItem;
            fallQueries = new OriginalFallTargetPlanner(new OriginalBoardFallContext(board.Grid, board.Layout, board.InitialItemAmounts, game, null), new OriginalFallRefillColumns());
            device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            test = 0; frame = Time.frameCount; failure = null;
            game.TntRocketCombo.OnComboMade += ComboMade; game.OnComboRocketCreated += RocketCreated;
            board.Moves.OnMoveCompleted += MoveCompleted;
            Application.logMessageReceived += ObserveLog;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(); EditorApplication.update += Tick;
        }
        private static void Begin()
        {
            int[] sx = { 3, 8, 1, 0 }, sy = { 6, 6, 7, 7 }, dx = { 4, 8, 0, 0 }, dy = { 6, 7, 7, 6 };
            source = board.Grid.GetCell(sx[test], sy[test]); destination = board.Grid.GetCell(dx[test], dy[test]);
            if (!IsMatchPair(source, destination))
                FindMatchPair(dx[test] - sx[test], dy[test] - sy[test]);
            report.destinations[test] = new Vector2Int(destination.X, destination.Y);
            moving = PlaceSpecial(source, test % 2 == 0 ? 40 : (test == 1 ? 20 : 30));
            other = PlaceSpecial(destination, test % 2 == 0 ? (test == 0 ? 30 : 20) : 40);
            parts = new Transform[12]; rocketCount = 0; comboCompleted = observedMerge = false;
            movesBefore = board.Moves.LeftMoves; acceptedBefore = game.AcceptedSwaps;
            refillsBefore = game.CreatedRefills; goalsBefore = board.Goals.GetGoalLeftCount(11);
            sampleFrame = 0; started = Time.realtimeSinceStartup; steadySince = -1f;
        }
        private static bool IsMatchPair(OriginalGridCell first, OriginalGridCell second) =>
            first?.Mediation.CurrentItem is OriginalMatchBoardItem &&
            second?.Mediation.CurrentItem is OriginalMatchBoardItem;

        private static void FindMatchPair(int stepX, int stepY)
        {
            // Keep the swipe direction and edge coverage if a prior cascade
            // left a special at one of the preferred fixture positions.
            for (int y = board.Grid.Height - 1; y >= 0; y--)
            for (int x = 0; x < board.Grid.Width; x++)
            {
                bool edge = x == 0 || x == board.Grid.Width - 1 || y == 0 || y == board.Grid.Height - 1;
                if (edge != (test != 0)) continue;
                var target = board.Grid.GetCell(x, y);
                var start = board.Grid.GetCell(x - stepX, y - stepY);
                if (!IsMatchPair(start, target)) continue;
                source = start;
                destination = target;
                report.alternate_pair[test] = true;
                return;
            }
            throw new InvalidOperationException("No ordinary pair remains for the required TNT and rocket direction and edge.");
        }
        private static OriginalSpecialBoardItem PlaceSpecial(OriginalGridCell cell, int tiledId)
        {
            var old = cell.Mediation.CurrentItem as OriginalMatchBoardItem;
            Require(old != null, "Fixture replaces ordinary match tile only");
            old.Mediator.ClearFromAllRegisteredCells(); board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)old.SwapView);
            return (OriginalSpecialBoardItem)game.CreateItemAtCell(tiledId, cell);
        }
        private static void MoveCompleted(int moveType)
        {
            Require(moveType == 12 && sampleFrame == 1 && board.Moves.LeftMoves == movesBefore &&
                board.State.GetSpecialOperationCount() == 1 && !board.State.HasOperation(2),
                "The native combo starts and publishes move type 12 inside the swipe, before its one move is charged.");
            Require(moving.CurrentCell == null && other.CurrentCell == null &&
                ((Component)moving.View).transform.localPosition == source.GetViewLocalPosition() &&
                ((Component)other.View).transform.localPosition == destination.GetViewLocalPosition(),
                "The combo owns both models immediately and starts the two views in their original cells.");
            report.immediate_move_completions++;
        }
        private static void ComboMade(OriginalCellPoint point, int horizontal, int vertical)
        {
            Require(point.X == destination.X && point.Y == destination.Y && horizontal != vertical, "Native center and distinct axis explosion IDs");
            Require(observedMerge, "Merge animation precedes damage");
            Require(gridShake.IsShaking && board.BoardRoot.localPosition == restPosition,
                "The merge completion schedules the native delayed shake before launching rockets");
            report.merge_seconds[test] = Time.time - mergeStarted;
            comboCompleted = true;
        }
        private static void RocketCreated(OriginalRocketBoardItem rocket)
        {
            int i = rocketCount++;
            Require(i < 6 && rocket.CurrentCell == null, "Six detached rockets, no cell replacement");
            parts[i * 2] = rocket.ItemView.FirstPart.transform; parts[i * 2 + 1] = rocket.ItemView.SecondPart.transform;
            starts[i * 2] = parts[i * 2].position; starts[i * 2 + 1] = parts[i * 2 + 1].position;
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
            if (game.State.GetSpecialOperationCount() > 0 && !comboCompleted)
            {
                Require(!gridShake.IsShaking, "No grid shake before merge completion");
                if (!observedMerge) { observedMerge = true; mergeStarted = Time.time; }
                Require(moving.CurrentCell == null && other.CurrentCell == null, "Both original mediators are released during merge");
                Require(source.IsFallFrozen && destination.IsFallFrozen, "Both source cells frozen during merge");
                Require(!moving.IsExploded && !other.IsExploded, "Original source models wait for merge, not individual TNT explosion");
                Require(board.Presentation.Pool.GetPoolLength(21) == 0, "Original pool21 use particles active");
            }
            if (comboCompleted)
            {
                report.peak_shake[test] = Mathf.Max(report.peak_shake[test],
                    Vector3.Distance(restPosition, board.BoardRoot.localPosition));
                report.peak_operations[test] = Math.Max(report.peak_operations[test], board.State.Operations.Container.GetCount(4));
                report.peak_particles[test] = Math.Max(report.peak_particles[test], board.Presentation.GetParticleAmount(40) - board.Presentation.Pool.GetPoolLength(9));
                for (int i = 0; i < rocketCount * 2; i++)
                    report.part_distances[test * 12 + i] = Mathf.Max(report.part_distances[test * 12 + i], Vector3.Distance(starts[i], parts[i].position));
            }
            if (sampleFrame < 3)
            {
                var p = (sampleFrame == 0 ? source : destination).GetViewPosition();
                samples.Position = new Vector2(p.x, p.y); samples.Down = sampleFrame == 0;
                samples.Held = sampleFrame < 2; samples.Up = sampleFrame == 2;
                listener.ManualUpdate();
                if (sampleFrame == 1)
                    Require(report.immediate_move_completions == test + 1 && board.Moves.LeftMoves == movesBefore - 1,
                        "The accepted swipe dispatches the combo once and spends one move immediately.");
                sampleFrame++; return;
            }
            if (!comboCompleted) return;
            if (board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping || !game.Fall.IsAllItemsSteady
                || board.State.HasAnyOperation() || game.Auto.IsThereAWaitingMatch) { steadySince = -1f; return; }
            if (steadySince < 0f) { steadySince = Time.time; return; }
            if (Time.time - steadySince < 1.6f || board.Presentation.Pool.GetPoolLength(9) != board.Presentation.GetParticleAmount(40)) return;
            Require(board.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == acceptedBefore + 1, "One combination drag spends one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(6, out int count) && count == test + 1, "One original combo6 record per drag");
            Require(rocketCount == 6 && report.peak_operations[test] >= 12 && report.peak_particles[test] >= 12, "Six rockets/twelve real flight operations and particles");
            for (int i = 0; i < 12; i++) Require(report.part_distances[test * 12 + i] > 1f, "Every original rocket part actually flies");
            Require(game.CreatedRefills > refillsBefore, "Real cells refill after combination");
            if (test == 0) Require(board.Goals.GetGoalLeftCount(11) < goalsBefore, "Combo damages actual boxes");
            Require(game.State.GetSpecialOperationCount() == 0 && board.Presentation.Pool.GetPoolLength(20) == 1
                && board.Presentation.Pool.GetPoolLength(21) == 1, "Combo operation and both native particle pools released");
            Require(!moving.SwapView.IsSwapping && !other.SwapView.IsSwapping, "Both source swap states ended");
            Require(report.merge_seconds[test] > .30f && report.merge_seconds[test] < .46f, "Native .38-second merge timing");
            Require(board.Moves.CanUserMakeMove(), "Player control unlocks after flight/refill");
            Require(report.peak_shake[test] > .05f && report.peak_shake[test] <= .425f &&
                !gridShake.IsShaking && board.BoardRoot.localPosition == restPosition,
                $"The board shakes and returns to rest. Peak {report.peak_shake[test]}, active {gridShake.IsShaking}, " +
                $"rest {restPosition}, final {board.BoardRoot.localPosition}");
            int empty = 0;
            for (int x = 0; x < board.Grid.Width; x++) for (int y = 0; y < board.Grid.Height; y++)
            {
                var cell = board.Grid.GetCell(x, y); if (!cell.IsNormalCell) continue;
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No swap/rocket freeze remains");
                var item = cell.Mediation.CurrentItem;
                if (item == null)
                {
                    Require(IsBoxBlockedCavity(cell),
                        "Only native blocked original-box cavities may remain: " + DescribeHole(cell));
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
            Debug.Log("Formal TNT+rocket verification passed: four directions, both roles/orientations, edge rockets, merge/flight/damage/refill/moves/pools and unlock."); Cleanup(); OnFinished?.Invoke();
        }
        private static void ObserveLog(string text, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failure = text; }
        private static bool IsBoxBlockedCavity(OriginalGridCell cell)
        {
            // The straight search can pass several empty former box slots
            // before reaching the box that actually blocks the column.
            for (var visiting = cell; visiting != null; visiting = visiting.Neighbors.Get(1))
            {
                var slots = visiting.Mediation;
                if (slots.CurrentItem is OriginalBoxBoardItem)
                    return !fallQueries.IsFallPossibleFromCellToCell(visiting, cell) &&
                        fallQueries.GetNextItemOfCell(visiting, cell) == null;
                if (!boxSlots[visiting.X, visiting.Y] || visiting.IsFillingCell || visiting.IsFallFrozen ||
                    slots.CurrentItem != null || slots.NextItem != null || slots.TargetItem != null ||
                    fallQueries.FindTargetItemFromCrossCell(cell, visiting, 0) != null ||
                    fallQueries.FindTargetItemFromCrossCell(cell, visiting, 2) != null)
                    return false;
            }
            return false;
        }
        private static string DescribeHole(OriginalGridCell cell)
        {
            var above = cell.Neighbors.Get(1);
            return "cell=("+cell.X+","+cell.Y+") initialBox="+boxSlots[cell.X,cell.Y]
                +" above="+(above?.Mediation.CurrentItem?.TiledId.ToString()??"empty")
                +" next="+(cell.Mediation.NextItem?.TiledId.ToString()??"empty")
                +" target="+(cell.Mediation.TargetItem?.TiledId.ToString()??"empty")
                +" crossLeft="+(fallQueries.FindTargetItemFromCrossCell(cell,cell,0)?.TiledId.ToString()??"empty")
                +" crossRight="+(fallQueries.FindTargetItemFromCrossCell(cell,cell,2)?.TiledId.ToString()??"empty")
                +" state="+game.State.GetGameState()+" goals="+board.Goals.GetGoalLeftCount(11)+" combo="+test;
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= ObserveLog;
            if (game != null) { game.TntRocketCombo.OnComboMade -= ComboMade; game.OnComboRocketCreated -= RocketCreated; }
            if (board != null) board.Moves.OnMoveCompleted -= MoveCompleted;
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
            public int completed_combos, immediate_move_completions, left_moves, accepted_swaps, goals_left, created_refills;
            public int[] peak_operations = new int[4], peak_particles = new int[4], blocked_box_holes = new int[4];
            public float[] peak_shake = new float[4];
            public float[] part_distances = new float[48];
            public float[] merge_seconds = new float[4];
            public Vector2Int[] destinations = new Vector2Int[4];
            public bool[] alternate_pair = new bool[4];
            public string scope = "Explicit production-factory TNT+rocket fixtures in unchanged formal first-board scene, four directions, both moving roles and rocket orientations, edges including out-of-grid ghost rockets. Actual merge clock/particles, six rocket models/twelve flight parts, damage/goals/moves/fall/refill/pool/unlock. Does not establish natural combo creation, physical input, rendered-original/camera/grid-shake/audio/charger/session/othercombos/settlement parity.";
        }
    }
}
