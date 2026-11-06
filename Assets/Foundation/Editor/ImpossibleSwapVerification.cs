using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    // Explicit diagnostic composition; production input remains unbound until
    // move/UI/booster/tap/resolution contexts have concrete implementations.
    public static class ImpossibleSwapVerification
    {
        private static OriginalInitialBoardView board;
        private static Services services;
        private static OriginalMatchItemSpriteView view;
        private static OriginalGridCell from;
        private static Vector3 origin, originalScale;
        private static int index, frame, frames, movingFrames, scaleFrames, earlyIdleFrames;
        private static float started;
        private static bool running;
        private static readonly OriginalCellPoint[] Sources = { new OriginalCellPoint(0, 7), new OriginalCellPoint(0, 4), new OriginalCellPoint(8, 7), new OriginalCellPoint(8, 4) };
        private static readonly OriginalCellPoint[] Targets = { new OriginalCellPoint(-1, 7), new OriginalCellPoint(0, 3), new OriginalCellPoint(9, 7), new OriginalCellPoint(8, 3) };
        [MenuItem("Recovery/Verify Impossible Swap (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || running) throw new InvalidOperationException("Run once in Play mode.");
            board = Object.FindObjectOfType<OriginalInitialBoardView>();
            Require(board != null && board.Grid != null && !board.FirstSwapAction.IsSwapping && !board.SecondSwapAction.IsSwapping, "Actual initialized first board and idle actions");
            services = new Services(board); index = frames = movingFrames = scaleFrames = earlyIdleFrames = 0;
            frame = Time.frameCount; started = Time.realtimeSinceStartup; running = true;
            EditorApplication.update += Tick;
        }
        private static void Tick() { try { Step(); } catch { Cleanup(); throw; } }
        private static void Step()
        {
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            Require(Time.realtimeSinceStartup - started < 20f, "Impossible-swap diagnostic timeout");
            if (frame == Time.frameCount) return; frame = Time.frameCount;
            if (view == null)
            {
                if (index == Sources.Length) { Complete(); return; }
                var source = Sources[index]; from = board.Grid.GetCell(source.X, source.Y);
                view = (OriginalMatchItemSpriteView)from.Mediation.CurrentItem.SwapView;
                origin = view.transform.localPosition; originalScale = view.transform.localScale;
                Require(!services.Router.SwapCells(from, Targets[index]), "Impossible route returns false");
                Require(board.FirstSwapAction.IsSwapping && view.IsSwapping, "Source/action busy immediately");
                Require(services.FalseSwipes == index + 1 && services.Events == index + 1 && services.EventSawBusy,
                    "Native router false-swipe and required event order");
                Require(from.FallFreezeCount == 0 && !board.GameOperations.Container.HasAny(), "Impossible move creates no normal swap operation or fall freeze");
            }
            frames++;
            if ((view.transform.localPosition - origin).sqrMagnitude > .000001f) movingFrames++;
            if ((view.transform.localScale - originalScale).sqrMagnitude > .000001f) scaleFrames++;
            if (board.FirstSwapAction.IsSwapping) return;
            if ((view.transform.localScale - originalScale).sqrMagnitude > .000001f)
            { earlyIdleFrames++; return; } // Native position callback can finish before the scale tail.
            Require((view.transform.localPosition - origin).sqrMagnitude < .000001f && !view.IsSwapping,
                "Native view position, scale and swap state restored");
            Require(view.GetSorting().Order == 1000 && view.GetSorting().Layer == SortingLayer.NameToID("Item"), "Native current-cell self sorting restored");
            Require(from.FallFreezeCount == 0 && !board.GameOperations.Container.HasAny(), "No residual freezes or operations");
            index++; view = null;
        }
        private static void Complete()
        {
            Require(services.NormalEvents == 0 && services.Sounds == 4 && services.Haptics == 4 && services.StaticRequests == 4
                && services.ExistingStaticTargets == 2, "No normal swap events, exact SFX/haptic/static requests");
            Require(movingFrames > 0 && scaleFrames > 0 && board.LocalMoves.GrowthCount == 0, "Actual intermediate motion/scale and cached tween capacity");
            var result = new Result { passed = true, actual_board_routes = index, observed_frames = frames,
                moving_frames = movingFrames, scaled_frames = scaleFrames, early_idle_scale_tail_frames = earlyIdleFrames,
                scope = "Actual first-level board, cell guards and two-action router dispatch four impossible swaps (left/right board edges and two box-blocked targets) into production impossible-swap choreography and concrete Unity match-view sorting/local-position/local-scale adapter. Player-loop .075-second OutSine/InOutSine legs and scale tail are exercised; positions, scales, view/action flags and default sorting return, no normal swap start/end event or fall freeze/operation is created, and the configured tween pool does not grow. Tutorial permission, session counters, SFX/haptic and static invalid feedback are explicitly traced diagnostic boundaries. Static targets contain no above items here; their presentation is not verified. Physical input, press/tap, full context composition, special/static view adapters, valid resolution/fall/refill/goals/settlement and original rendered parity remain pending. No permissive context is installed in production." };
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "impossible_swap_unity_verification.json"), JsonUtility.ToJson(result, true) + "\n");
            Debug.Log("Impossible swap Unity verification passed: four actual-board routes, intermediate movement/scale and native final state."); Cleanup();
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            if (services != null) services.Unsubscribe();
            running = false; view = null; board = null; services = null; from = null;
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private sealed class Services : IOriginalImpossibleSwapGameplay, IOriginalLevelSwapServices, IOriginalSwapAnimationBackend
        {
            private readonly OriginalInitialBoardView owner;
            private readonly OriginalImpossibleSwapPresentation presentation;
            public readonly OriginalLevelSwapRouter Router;
            public int FalseSwipes, Events, NormalEvents, Sounds, Haptics, StaticRequests, ExistingStaticTargets;
            public bool EventSawBusy;
            public Services(OriginalInitialBoardView owner)
            {
                this.owner = owner;
                presentation = new OriginalImpossibleSwapPresentation(owner.LocalMoves, this, SortingLayer.NameToID("Item"));
                Router = new OriginalLevelSwapRouter(owner.Grid, owner.FirstSwapAction, owner.SecondSwapAction, this, this, owner.SwapAvailability, this);
                Router.OnSwap += OnSwap;
                owner.FirstSwapAction.OnSwapAnimationStart += OnStart; owner.FirstSwapAction.OnSwapAnimationEnd += OnEnd;
                owner.SecondSwapAction.OnSwapAnimationStart += OnStart; owner.SecondSwapAction.OnSwapAnimationEnd += OnEnd;
            }
            public void Unsubscribe()
            {
                owner.FirstSwapAction.OnSwapAnimationStart -= OnStart; owner.FirstSwapAction.OnSwapAnimationEnd -= OnEnd;
                owner.SecondSwapAction.OnSwapAnimationStart -= OnStart; owner.SecondSwapAction.OnSwapAnimationEnd -= OnEnd;
            }
            private void OnStart() { NormalEvents++; } private void OnEnd(int type) { NormalEvents++; }
            private void OnSwap(OriginalGridCell source, OriginalGridCell target, bool valid)
            { Require(!valid, "Expected impossible swap"); Events++; EventSawBusy = owner.FirstSwapAction.IsSwapping; }
            public bool IsImpossibleMoveDisabledForCell(OriginalCellPoint point) => false; // Explicit diagnostic tutorial permission.
            public void StartImpossibleSwapAnimation(OriginalSwapActionState action, OriginalGridCell source, OriginalCellPoint destination) =>
                new OriginalImpossibleSwapAnimation(action, source, destination, presentation).Play();
            public void IncreaseFalseSwipeCount() { FalseSwipes++; }
            public void CheckForUsefulMove() => throw new InvalidOperationException("Unexpected valid move.");
            public void PlayValidSwap(OriginalSwapActionState action) => throw new InvalidOperationException("Unexpected normal swap.");
            public void PlayWrongSwap(OriginalSwapActionState action) => throw new InvalidOperationException("Unexpected normal swap.");
            public void PlaySfx(int id, float delay) { Require(id == 193 && delay == .04f, "Native impossible SFX"); Sounds++; }
            public void PlayHaptic(int id, int type, float strength, float duration, bool replay)
            { Require(id == 17 && type == 1 && strength == .7f && duration == 0f && !replay, "Native impossible haptic"); Haptics++; }
            public void TryPlayStaticInvalidMoveAnimation(OriginalCellPoint point)
            {
                StaticRequests++; var target = owner.Grid.GetCell(point.X, point.Y);
                if (target == null) return;
                ExistingStaticTargets++;
                Require(target.StaticTouch.AboveItems.Count == 0, "Above-item animation requires its concrete restored service");
            }
        }
        [Serializable] private sealed class Result
        { public bool passed; public int actual_board_routes, observed_frames, moving_frames, scaled_frames, early_idle_scale_tail_frames; public string scope; }
    }
}
