using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Scoped physical-click and framed-input diagnostic. No game services are installed
    // outside this command; valid resolution/impossible moves deliberately fail
    // here because those concrete scene services are not restored yet.
    public static class GameTouchVerification
    {
        private static OriginalGameTouchInput input;
        private static OriginalInitialBoardView board;
        private static Services services;
        private static int phase, frame;
        private static float started;
        private static OriginalGameTouchListener replay;
        private static RecordedInput samples;
        private static int replayFrame;
        private static Vector2 origin;
        private static bool diagonalPreserved;
        [MenuItem("Recovery/Verify Game Touch (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || input != null) throw new InvalidOperationException("Run once in Play mode.");
            input = Object.FindObjectOfType<OriginalGameTouchInput>();
            board = Object.FindObjectOfType<OriginalInitialBoardView>();
            try
            {
                Require(input != null && input.PointerCamera != null && input.Listener == null && board.Grid != null,
                    "Configured scene camera/input and actual first board");
                services = new Services(board); input.Bind(services);
                phase = 0; frame = Time.frameCount; started = Time.realtimeSinceStartup; diagonalPreserved = false;
                EditorApplication.update += Tick;
                Debug.Log("Game touch verification ready: tap cell (0,7), then run frame-separated diagonal and horizontal input sequences.");
            }
            catch { Cleanup(); throw; }
        }
        private static void Tick() { try { Step(); } catch { Cleanup(); throw; } }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Require(Time.realtimeSinceStartup - started < 90f, "Real-input diagnostic timeout");
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            if (phase == 0)
            {
                if (services.Taps == 0) return;
                Require(services.Taps == 1 && services.LastTap == board.Grid.GetCell(0, 7) && !input.Listener.IsTouching,
                    "Real native mouse down/up reaches tap on the selected cell");
                Require(services.Swaps == 0, "Tap creates no swap"); phase = 1;
                input.Unbind(); samples = new RecordedInput(); replay = new OriginalGameTouchListener(samples, services);
                var world = services.FromView.transform.position; origin = new Vector2(world.x, world.y); replayFrame = 0;
                Debug.Log("Game touch verification physical tap passed; frame-separated handoff checks are running."); return;
            }
            if (phase == 1)
            {
                if (replayFrame == 0) samples.Set(origin, true, true, false);
                else if (replayFrame == 1) samples.Set(origin + new Vector2(.6f, -.6f), false, true, false);
                else samples.Set(origin + new Vector2(1f, -1f), false, false, true);
                replay.ManualUpdate(); replayFrame++;
                if (replayFrame < 3) Require(services.FromView.IsHighlighted && services.FromView.IsHighlightActive
                    && services.FromView.GetHighlightRatio() == services.FromView.GetMaxHighlightRatio(), "Native press highlight stays active in the diagonal dead zone");
                if (replayFrame < 3) return;
                Require(services.Swaps == 0 && services.Taps == 1 && replay.IsTouching
                    && replay.SelectedCell == board.Grid.GetCell(0, 7), "45-degree dead zone and native large-release selection preservation");
                diagonalPreserved = true; phase = 2; replayFrame = 0; return;
            }
            if (replayFrame < 3)
            {
                if (replayFrame == 0) samples.Set(origin, true, true, false);
                else if (replayFrame == 1) samples.Set(origin + new Vector2(.6f, 0f), false, true, false);
                else samples.Set(origin + new Vector2(1f, 0f), false, false, true);
                replay.ManualUpdate(); replayFrame++;
            }
            if (services.Swaps == 0) return;
            Require(services.Swaps == 1 && services.FalseSwipes == 1 && services.SwapFrom == board.Grid.GetCell(0, 7)
                && services.SwapTo == board.Grid.GetCell(1, 7) && !services.ValidSwap && services.BusyAtEvent,
                "Framed input to native guarded router to actual wrong-swap backend");
            if (board.FirstSwapAction.IsSwapping || board.SecondSwapAction.IsSwapping || Time.time - services.SwapTime < 1.08f) return;
            Require(services.Sounds == 2 && services.Haptics == 1 && !replay.IsTouching, "Swap sound/haptic boundaries and touch stop");
            Require(services.HighlightPresses == 3 && services.CanceledHighlights == 3 && !services.FromView.IsHighlighted
                && !services.FromView.IsHighlightActive, "Physical tap and both framed presses cancel the real highlight layer");
            Require(services.RejectedNormalTaps == 1, "Actual native tap router rejects an ordinary match without move-completed events");
            Require(services.Moves.LeftMoves == 30 && services.Moves.MadeMoves == 0 && services.MoveChanges == 0
                && services.MoveCompletions == 1, "Actual move subscriptions retain budget after ordinary rejected tap and invalid swap; animation emits completion separately");
            Require(services.FromView.transform.localPosition == new Vector3(0, 7, 0)
                && services.ToView.transform.localPosition == new Vector3(1, 7, 0)
                && !services.FromView.IsSwapping && !services.ToView.IsSwapping, "Real board views return to their native cell coordinates");
            Require(services.SwapFrom.FallFreezeCount == 0 && services.SwapTo.FallFreezeCount == 0
                && !board.GameOperations.Container.HasAny(), "Native input/swap operations and freezes settle");
            Require(board.Presentation.Pool.GetPoolLength(3) == 10 && board.LocalMoves.GrowthCount == 0,
                "Trail recycling and cached local move capacity");
            var result = new Result { passed = true, real_taps = services.Taps, replayed_swipes = services.Swaps, physical_drag_verified = false,
                diagonal_dead_zone_preserved_selection = diagonalPreserved, false_swipes = services.FalseSwipes,
                actual_highlight_presses = services.HighlightPresses, canceled_highlights = services.CanceledHighlights,
                concrete_normal_tap_rejections = services.RejectedNormalTaps,
                actual_move_gate_and_subscriptions_checked = true,
                scope = "Physical Windows mouse click and framed diagonal/invalid horizontal swap through actual first-board input, hit test, touch/press/tap routers, pooled highlights/trails/flashes, animation and operation/freeze cleanup. Uses actual MoveManager eligibility and event subscriptions against concrete game state/shared operation container, with explicit diagnostic play state. Fixture budget now passes through full native LevelManager.LevelLoad routing, actual episode and welcome-back move providers, active Load/name selection, SuperDuke/SLB adjustment and fresh LevelParser getters. Checks invalid swap and rejected ordinary tap spend nothing; animation completion is forwarded exactly once, independently of spending. Physical drag remains unverified. Application data/resource/parser transport, UI/booster/session/audio/haptic and valid/impossible resolution remain diagnostic boundaries. Formal board starts in NoGame with uninitialized moves until actual application/LevelTouch/GameContext startup is composed. Full constructor/session Start/Restart/save, special taps, resolution/fall/refill/goals/settlement and rendered parity remain pending." };
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "game_touch_unity_verification.json"), JsonUtility.ToJson(result, true) + "\n");
            Debug.Log("Game touch Unity physical-click and framed-input verification passed."); Cleanup();
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (input != null) input.Unbind();
            if (services != null) services.ReleaseMoveBindings();
            if (board != null && board.Presentation != null && board.Presentation.Pool != null && board.Presentation.Pool.IsPoolAlive())
                board.Presentation.Particles.Clear(false);
            input = null; services = null; board = null; replay = null; samples = null;
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private sealed class Services : IOriginalGameTouchServices, IOriginalNormalSwapGameplay, IOriginalLevelSwapServices, IOriginalLevelTapServices, IOriginalMoveTouchEvents
        {
            private readonly OriginalInitialBoardView owner;
            private readonly OriginalCellGridHitTest hit;
            private readonly OriginalLevelSwapRouter router;
            private readonly OriginalLevelPressRouter presses;
            private readonly OriginalLevelTapRouter taps;
            private readonly OriginalGameStateManager state;
            private Action<int> completionHandlers;
            public OriginalMoveManager Moves { get; }
            public int MoveChanges, MoveCompletions;
            public event Action<OriginalGridCell> OnTap { add { taps.OnTap += value; } remove { taps.OnTap -= value; } }
            public event Action<OriginalGridCell, OriginalGridCell, bool> OnSwap
            { add { router.OnSwap += value; } remove { router.OnSwap -= value; } }
            public event Action<int> OnMoveCompleted
            {
                add { taps.OnMoveCompleted += value; owner.OnSwapEnded += value; completionHandlers += value; }
                remove { taps.OnMoveCompleted -= value; owner.OnSwapEnded -= value; completionHandlers -= value; }
            }
            public int Touches, Taps, Swaps, FalseSwipes, Sounds, Haptics, Cancels;
            public int HighlightPresses, CanceledHighlights;
            public int RejectedNormalTaps;
            public OriginalGridCell LastTap, SwapFrom, SwapTo;
            public OriginalMatchItemSpriteView FromView, ToView;
            public bool ValidSwap, BusyAtEvent;
            public float SwapTime;
            public Services(OriginalInitialBoardView owner)
            {
                this.owner = owner;
                presses = new OriginalLevelPressRouter(this);
                taps = new OriginalLevelTapRouter(this);
                taps.OnTap += cell => throw new InvalidOperationException("Ordinary match tap must not publish OnTap.");
                taps.OnMoveCompleted += move => throw new InvalidOperationException("Ordinary match tap must not complete a move.");
                FromView = (OriginalMatchItemSpriteView)owner.Grid.GetCell(0, 7).Mediation.CurrentItem.SwapView;
                ToView = (OriginalMatchItemSpriteView)owner.Grid.GetCell(1, 7).Mediation.CurrentItem.SwapView;
                var root = FromView.transform.parent;
                hit = new OriginalCellGridHitTest(owner.Grid, new Vector2(root.position.x, root.position.y), false, false);
                var presentation = new OriginalNormalSwapPresentation(root, owner.Presentation, owner.LocalMoves, this, SortingLayer.NameToID("Item"));
                router = new OriginalLevelSwapRouter(owner.Grid, owner.FirstSwapAction, owner.SecondSwapAction,
                    new OriginalNormalSwapAnimationBackend(owner.FirstSwapAction, presentation, .135f, .04f),
                    new OriginalNormalSwapAnimationBackend(owner.SecondSwapAction, presentation, .135f, .04f), owner.SwapAvailability, this);
                state = owner.State;
                Require(state != null && ReferenceEquals(state.Operations, owner.GameOperations) && state.IsNoGame(),
                    "Formal board owns native NoGame state and shared operation container before full context startup");
                state.SetStateToPlay();
                Moves = owner.Moves; Moves.Bind(state, this);
                var freshMoves = MoveBudgetVerification.FirstLevelMoves();
                Moves.SetMovesForStart(freshMoves.GetMaxMoves(), freshMoves.GetLeftMoves(), freshMoves.GetMadeMoves());
                Moves.OnMoveChanged += (left, reason) => MoveChanges++;
                Moves.OnMoveCompleted += type => MoveCompletions++;
                router.OnSwap += ObserveSwap;
            }
            public bool IsUiTouching => false; // Explicit diagnostic service boundary.
            public bool IsBoosterSelected => false;
            public bool CanUserMakeMove() => Moves.CanUserMakeMove();
            public OriginalGridCell GetCellFromWorldPosition(Vector2 position) => hit.GetCellFromWorldPosition(position);
            public bool ShouldDeselectBoosterOnTouch(OriginalGridCell cell) => throw new InvalidOperationException("Unexpected selected booster.");
            public void DeselectBooster() => throw new InvalidOperationException("Unexpected booster deselection.");
            public void CancelTouchedItemAnimation()
            { Cancels++; if (presses.TouchedItem != null) CanceledHighlights++; presses.CancelTouchedItemAnimation(); }
            public void TouchCell(OriginalGridCell cell)
            {
                Require(cell == owner.Grid.GetCell(0, 7), "Expected diagnostic touch cell"); Touches++;
                presses.TouchCell(cell);
                Require(FromView.IsHighlighted && FromView.IsHighlightActive && FromView.GetHighlightRatio() == FromView.GetMaxHighlightRatio(), "Actual model press enables the configured highlight renderer");
                HighlightPresses++;
            }
            public bool TapCell(OriginalGridCell cell)
            {
                LastTap = cell; Taps++;
                bool accepted = taps.TapCell(cell);
                Require(!accepted, "Actual ordinary match Tap returns false");
                RejectedNormalTaps++; return accepted;
            }
            public void UseBooster(OriginalGridCell cell) => throw new InvalidOperationException("Unexpected booster use.");
            public void IncrementTappedLightball() => throw new InvalidOperationException("Unexpected lightball tap.");
            public void OnSpecialItemTapped() => throw new InvalidOperationException("Unexpected special tap.");
            public void IncrementItemUse(int itemType, bool isCombo) => throw new InvalidOperationException("Unexpected special use.");
            public void OnTntOrRocketItemTapped() => throw new InvalidOperationException("Unexpected rocket/TNT tap.");
            public bool SwapCells(OriginalGridCell from, OriginalCellPoint point) => router.SwapCells(from, point);
            private void ObserveSwap(OriginalGridCell from, OriginalGridCell to, bool valid)
            { SwapFrom = from; SwapTo = to; ValidSwap = valid; Swaps++; SwapTime = Time.time; BusyAtEvent = owner.FirstSwapAction.IsSwapping; }
            public bool IsImpossibleMoveDisabledForCell(OriginalCellPoint point) => throw new InvalidOperationException("Unexpected impossible move.");
            public void StartImpossibleSwapAnimation(OriginalSwapActionState action, OriginalGridCell from, OriginalCellPoint to) =>
                throw new InvalidOperationException("Impossible animation is outside this real-input check.");
            public void IncreaseFalseSwipeCount() { FalseSwipes++; }
            public void CheckForUsefulMove() => throw new InvalidOperationException("Unexpected valid swap.");
            public void ClearSinglePropellerWinCalculationData() => throw new InvalidOperationException("Unexpected valid swap.");
            public void PlaySfx(int id, float delay) { Require((id == 254 || id == 370) && delay == .04f, "Native SFX boundary"); Sounds++; }
            public void PlayHaptic(int id, int type, float strength, float duration, bool replay)
            { Require(id == 38 && type == 1 && strength == .7f && duration == 0 && !replay, "Native haptic boundary"); Haptics++; }
            public void ExplodeMatchGroups(OriginalSwapDecision decision, OriginalBoardItem other, OriginalBoardItem moving) =>
                throw new InvalidOperationException("Valid resolution is outside this real-input check.");
            public void ExplodeSpecialItems(OriginalBoardItem other, OriginalBoardItem moving) =>
                throw new InvalidOperationException("Special resolution is outside this real-input check.");
            public void ReleaseMoveBindings()
            {
                owner.OnSwapEnded -= completionHandlers; completionHandlers = null;
                Moves.Clear(true);
                state.Clear(true);
            }
        }
        private sealed class RecordedInput : IOriginalGameTouchInput
        {
            private Vector2 position; private bool down, held, up;
            public void Set(Vector2 position, bool down, bool held, bool up)
            { this.position = position; this.down = down; this.held = held; this.up = up; }
            public Vector2 GetMouseWorldPosition() => position;
            public bool GetMouseButtonDown() => down;
            public bool GetMouseButton() => held;
            public bool GetMouseButtonUp() => up;
        }
        [Serializable] private sealed class Result
        { public bool passed, physical_drag_verified, diagonal_dead_zone_preserved_selection, actual_move_gate_and_subscriptions_checked; public int real_taps, replayed_swipes, false_swipes, actual_highlight_presses, canceled_highlights, concrete_normal_tap_rejections; public string scope; }
    }
}
