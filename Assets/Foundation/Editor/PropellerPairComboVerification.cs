using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class PropellerPairComboVerification
    {
        private static OriginalInitialBoardView board;
        private static OriginalPlayableBoard game;
        private static OriginalGameTouchInput device;
        private static OriginalGameTouchListener listener;
        private static Samples samples;
        private static OriginalGridCell source, destination;
        private static OriginalPropellerBoardItem moving, other, extra;
        private static int test, frame, sample, movesBefore, acceptedBefore, refillsBefore, peakFlights;
        private static float started, steadyAt;
        private static bool sawExpand;
        private static readonly Report report = new Report();
        private static string ReportPath => Path.Combine(Application.dataPath, "../live_propeller_pair_combo_verification.json");
        public static void Run()
        {
            board = UnityEngine.Object.FindObjectOfType<OriginalInitialBoardView>();
            game = UnityEngine.Object.FindObjectOfType<FoundationController>().Gameplay;
            device = UnityEngine.Object.FindObjectOfType<OriginalGameTouchInput>();
            device.Unbind(); samples = new Samples(); listener = new OriginalGameTouchListener(samples, game);
            game.OnComboPropellerCreated += Created;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            Begin(); EditorApplication.update += Tick;
        }
        private static void Created(OriginalPropellerBoardItem value) { Require(extra == null, "Exactly one extra propeller"); extra = value; }
        private static void Begin()
        {
            int x = test == 0 ? 4 : test == 1 ? 0 : test == 2 ? 7 : 8;
            source = board.Grid.GetCell(x, 7); destination = board.Grid.GetCell(test == 3 ? x : x + 1, test == 3 ? 6 : 7);
            moving = Place(source); other = Place(destination); extra = null;
            movesBefore = game.Moves.LeftMoves; acceptedBefore = game.AcceptedSwaps; refillsBefore = game.CreatedRefills;
            sample = 0; peakFlights = 0; sawExpand = false; started = Time.realtimeSinceStartup; steadyAt = -1f;
        }
        private static OriginalPropellerBoardItem Place(OriginalGridCell cell)
        {
            Require(cell.Mediation.CurrentItem is OriginalMatchBoardItem, "Fixture only replaces a regular tile");
            var old = (OriginalMatchBoardItem)cell.Mediation.CurrentItem;
            old.Mediator.ClearFromAllRegisteredCells(); board.Presentation.Pool.Recycle((OriginalMatchItemSpriteView)old.SwapView);
            return (OriginalPropellerBoardItem)game.CreateItemAtCell(10, cell);
        }
        private static void Tick()
        {
            try { Step(); }
            catch (Exception e) { report.failure = e.ToString(); File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true)); Cleanup(); throw; }
        }
        private static void Step()
        {
            if (!EditorApplication.isPlaying || frame == Time.frameCount) return; frame = Time.frameCount;
            Require(Time.realtimeSinceStartup - started < 25f, "Three flights and refill finish without forced operation resets");
            if (sample < 3)
            {
                var p = (sample == 0 ? source : destination).GetViewPosition();
                samples.Position = new Vector2(p.x, p.y); samples.Down = sample == 0; samples.Held = sample < 2; samples.Up = sample == 2;
                listener.ManualUpdate(); sample++; return;
            }
            peakFlights = Math.Max(peakFlights, game.State.Operations.Container.GetCount(3));
            if (moving.ItemView.FlyState == OriginalGuidedExploderFlyState.ComboExpand || other.ItemView.FlyState == OriginalGuidedExploderFlyState.ComboExpand)
                sawExpand = true;
            if (extra == null || !moving.IsExploded || !other.IsExploded || !extra.IsExploded || moving.IsInFlight || other.IsInFlight || extra.IsInFlight ||
                game.State.HasAnyOperation() || !game.Fall.IsAllItemsSteady || game.Auto.IsThereAWaitingMatch ||
                board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping) { steadyAt = -1f; return; }
            if (steadyAt < 0f) { steadyAt = Time.time; return; }
            if (Time.time - steadyAt < .3f) return;
            Require(sawExpand && peakFlights >= 3, "Original radial expansion and three actual concurrent flight operations");
            Require(moving.TargetItem != null && other.TargetItem != null && extra.TargetItem != null, "All three selected real board targets");
            Require(moving.CurrentCell == null && other.CurrentCell == null && extra.CurrentCell == null, "Both swapped cells released; extra remains detached");
            Require(!moving.SwapView.IsSwapping && !other.SwapView.IsSwapping && game.Moves.CanUserMakeMove(), "Swap and flight complete; player control restored");
            Require(game.Moves.LeftMoves == movesBefore - 1 && game.AcceptedSwaps == acceptedBefore + 1, "One gesture spends exactly one move");
            Require(game.Session.SpecialItemCounts.Combos.TryGetValue(10, out int count) && count == test + 1, "Original combo10 recorded once");
            Require(game.CreatedRefills > refillsBefore, "Actual board refill follows the combo");
            for (int y = 0; y < board.Grid.Height; y++) for (int x = 0; x < board.Grid.Width; x++)
            {
                var cell = board.Grid.GetCell(x, y);
                Require(!cell.IsFallFrozen && cell.CrossFallFreezeCount == 0, "No leaked combo freeze");
                if (y >= 4) Require(cell.Mediation.CurrentItem != null, "Reachable upper board has no holes");
            }
            report.peak_flights[test] = peakFlights; report.completed_combos++;
            if (++test < 4) { Begin(); return; }
            report.passed = true; report.left_moves = game.Moves.LeftMoves; report.goals_left = board.Goals.GetGoalLeftCount(11);
            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true) + "\n"); Cleanup();
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (game != null) game.OnComboPropellerCreated -= Created;
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
            public int[] peak_flights = new int[4];
            public string scope = "Four explicit two-propeller production-factory fixtures: center/left/right horizontal and edge vertical recorded input, actual combo dispatch, three pilots/target reservations/damage/refill, model move budget, operation/freeze release and subsequent input. No natural special creation, physical input, original rendered comparison, inactive event/charger/jelly or other combos parity claim.";
        }
    }
}
