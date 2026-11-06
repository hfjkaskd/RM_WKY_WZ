using System;

namespace Whitebox.Foundation
{
    public struct OriginalSwapPoint
    {
        public float X, Y, Z;
        public OriginalSwapPoint(float x, float y, float z) { X = x; Y = y; Z = z; }
    }
    public struct OriginalSwapSorting
    {
        public int Layer, Order;
        public bool SortEverything;
        public OriginalSwapSorting(int layer, int order, bool sortEverything)
        {
            Layer = layer; Order = order; SortEverything = sortEverything;
        }
    }

    // Required concrete services for view sorting, particles, native-style local
    // tweens and resolution. No no-op/default implementation is installed.
    public interface IOriginalSwapCompletionServices
    {
        IOriginalSwapAnimationView GetView(OriginalBoardItem item);
        OriginalSwapSorting ArrangeSelfSorting(IOriginalSwapAnimationView view);
        void SetSorting(IOriginalSwapAnimationView view, OriginalSwapSorting sorting);
        void UpdateParticleSorting(object particle, OriginalSwapSorting sorting);
        void PlaySfx(int id, float delay);
        void PlayHaptic(int id, int type, float strength, float duration, bool isReplay);
        void MoveToLocal(IOriginalSwapAnimationView view, OriginalSwapPoint target, float duration, Action onComplete);
        void ExplodeMatchGroups(OriginalSwapDecision decision, OriginalBoardItem otherItem, OriginalBoardItem movingItem);
        void ExplodeSpecialItems(OriginalBoardItem otherItem, OriginalBoardItem movingItem);
    }

    // Normal swap completion closures: valid 0x1ee44b4/0x1ee45cc and wrong
    // 0x1ee4734/0x1ee48dc/0x1ee4968/0x1ee4aa8. One instance per swap,
    // retained by its delegates until both pieces finish their callbacks.
    public sealed class OriginalSwapCompletion
    {
        private readonly OriginalSwapActionState action;
        private readonly IOriginalSwapCompletionServices services;
        private readonly float returnDuration;
        private readonly float soundDelay;
        private readonly Action primaryReturnComplete;
        private readonly Action secondaryReturnComplete;
        private OriginalBoardItem otherItem, movingItem;
        private IOriginalSwapAnimationView primaryView, secondaryView;
        private bool isCombo;
        private int moveType;
        private object primaryParticle, secondaryParticle;
        private OriginalSwapPoint primaryOrigin, secondaryOrigin;
        private OriginalSwapSorting secondarySwapSorting;

        // Timings come from the animation configuration (native .135 / .04).
        public OriginalSwapCompletion(OriginalSwapActionState action, IOriginalSwapCompletionServices services,
            float returnDuration, float soundDelay)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.returnDuration = returnDuration;
            this.soundDelay = soundDelay;
            primaryReturnComplete = WrongPrimaryReturnComplete;
            secondaryReturnComplete = WrongSecondaryReturnComplete;
        }
        public void ConfigureValid(OriginalBoardItem otherItem, OriginalBoardItem movingItem,
            IOriginalSwapAnimationView primaryView, IOriginalSwapAnimationView secondaryView, bool isCombo, int moveType)
        {
            this.otherItem = otherItem;
            this.movingItem = movingItem;
            this.primaryView = primaryView;
            this.secondaryView = secondaryView;
            this.isCombo = isCombo;
            this.moveType = moveType;
        }
        public void ValidPrimaryComplete()
        {
            primaryView.SwapAnimationEnded();
            if (isCombo && otherItem != null) services.GetView(otherItem).SwapAnimationEnded();
            if (movingItem.Mediator.CurrentCell != null) services.ArrangeSelfSorting(primaryView);
            services.ExplodeMatchGroups(action.Decision, otherItem, movingItem);
            services.ExplodeSpecialItems(otherItem, movingItem);
            action.From.UnFreezeFall();
            action.To.UnFreezeFall();
            action.CompleteAnimation(moveType);
        }
        public void ValidSecondaryComplete()
        {
            if (otherItem.Mediator.CurrentCell != null) services.ArrangeSelfSorting(secondaryView);
            secondaryView.SwapAnimationEnded();
        }
        public void ConfigureWrong(OriginalBoardItem fromItem, OriginalBoardItem toItem,
            IOriginalSwapAnimationView fromView, IOriginalSwapAnimationView toView,
            object fromParticle, object toParticle, OriginalSwapPoint fromOrigin, OriginalSwapPoint toOrigin,
            OriginalSwapSorting toSwapSorting)
        {
            otherItem = fromItem;
            movingItem = toItem;
            primaryView = fromView;
            secondaryView = toView;
            primaryParticle = fromParticle;
            secondaryParticle = toParticle;
            primaryOrigin = fromOrigin;
            secondaryOrigin = toOrigin;
            secondarySwapSorting = toSwapSorting;
        }
        public void WrongPrimaryOutComplete()
        {
            if (otherItem.Mediator.CurrentCell != null)
            {
                services.PlaySfx(370, soundDelay);
                services.PlayHaptic(38, 1, 0.7f, 0f, false);
                UpdateParticleSorting(primaryParticle, services.ArrangeSelfSorting(primaryView));
            }
            services.MoveToLocal(primaryView, primaryOrigin, returnDuration, primaryReturnComplete);
        }
        public void WrongPrimaryReturnComplete()
        {
            primaryView.SwapAnimationEnded();
            action.From.UnFreezeFall();
            action.To.UnFreezeFall();
            action.CompleteAnimation(0);
        }
        public void WrongSecondaryOutComplete()
        {
            services.SetSorting(secondaryView, secondarySwapSorting);
            UpdateParticleSorting(secondaryParticle, secondarySwapSorting);
            services.MoveToLocal(secondaryView, secondaryOrigin, returnDuration, secondaryReturnComplete);
        }
        public void WrongSecondaryReturnComplete()
        {
            if (movingItem.Mediator.CurrentCell != null) services.ArrangeSelfSorting(secondaryView);
            secondaryView.SwapAnimationEnded();
        }
        private void UpdateParticleSorting(object particle, OriginalSwapSorting sorting)
        {
            services.UpdateParticleSorting(particle,
                new OriginalSwapSorting(sorting.Layer, unchecked(sorting.Order + 1), false));
        }
    }
}
