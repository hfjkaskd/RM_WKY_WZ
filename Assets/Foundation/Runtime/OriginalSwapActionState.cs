using System;

namespace Whitebox.Foundation
{
    public interface IOriginalSwapAnimationBackend
    {
        void PlayValidSwap(OriginalSwapActionState action);
        void PlayWrongSwap(OriginalSwapActionState action);
    }

    // SwapAction.Clear / StartSwapAnimation. The LevelTouchManager selects an
    // idle action and clears it before starting; this method adds no extra gate.
    public sealed class OriginalSwapActionState
    {
        public OriginalSwapDecision Decision { get; }
        public OriginalGridCell From { get; private set; }
        public OriginalGridCell To { get; private set; }
        public bool IsSwapping { get; private set; }
        public event Action OnSwapAnimationStart;
        public event Action<int> OnSwapAnimationEnd;
        public OriginalSwapActionState(OriginalSwapDecision decision)
        {
            Decision = decision ?? throw new ArgumentNullException(nameof(decision));
        }
        // LevelTouchManager.SelectSwap 0x1ee251c: first idle, then second,
        // otherwise no action. Missing configured actions remain errors.
        public static OriginalSwapActionState Select(OriginalSwapActionState first, OriginalSwapActionState second)
        {
            if (first == null) throw new NullReferenceException();
            if (!first.IsSwapping) return first;
            if (second == null) throw new NullReferenceException();
            return second.IsSwapping ? null : second;
        }
        public void Clear()
        {
            From = null;
            To = null;
            IsSwapping = false;
            Decision.Clear();
        }
        public bool StartSwapAnimation(OriginalGridCell from, OriginalGridCell to, IOriginalSwapAnimationBackend backend)
        {
            From = from;
            To = to;
            bool valid = Decision.ShouldPlaySwapAnimation(from, to, false);
            if (valid) backend.PlayValidSwap(this);
            else backend.PlayWrongSwap(this);
            return valid;
        }
        // Called at the corresponding points in PlayValid/PlayWrong, not from
        // StartSwapAnimation: native special combo branches have other timing.
        public void MarkAnimating() { IsSwapping = true; }
        // Impossible swaps set only the source and busy flag, and do not emit
        // the normal swap start/end events (0x1ee266c / 0x1ee4d8c).
        public void BeginImpossibleAnimation(OriginalGridCell from) { From = from; IsSwapping = true; }
        public void FinishImpossibleAnimation() { IsSwapping = false; }
        public void NotifyAnimationStart() { OnSwapAnimationStart?.Invoke(); }
        public void CompleteAnimation(int moveType)
        {
            OnSwapAnimationEnd?.Invoke(moveType);
            IsSwapping = false; // Observers see the action busy during OnEnd.
        }
    }
}
