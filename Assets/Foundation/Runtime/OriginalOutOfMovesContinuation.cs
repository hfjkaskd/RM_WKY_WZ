using System;

namespace Whitebox.Foundation
{
    public interface IOriginalOutOfMovesContinuationContext
    {
        void EnableOrientation(int reason);
        bool IsLandscape { get; }
        void ResetTopUiSorting();
        void ResetBottomUiSorting();
        OriginalFlowManager GetFlowManager();
        OriginalFlowAction CreateEgoDialogAction(int origin, bool isOriginAddCoin, float backgroundFadeInDuration);
    }

    // OutOfMovesBanner.PushEgoDialog0x158a754. Called by the original banner
    // presentation timeline; the UiDialog close owner completes its own action.
    public sealed class OriginalOutOfMovesContinuation
    {
        private readonly IOriginalOutOfMovesContinuationContext context;
        public OriginalOutOfMovesContinuation(IOriginalOutOfMovesContinuationContext context) { this.context = context; }
        public void PushEgoDialog()
        {
            var owner = context ?? throw new NullReferenceException();
            owner.EnableOrientation(35);
            if (owner.IsLandscape)
            { owner.ResetTopUiSorting(); owner.ResetBottomUiSorting(); }
            var flow = owner.GetFlowManager();
            // Clear pending actions while retaining the running banner.
            (flow ?? throw new NullReferenceException()).Clear(false);
            // Native captures the owner again after Clear, before allocation.
            flow = owner.GetFlowManager();
            var action = owner.CreateEgoDialogAction(10, false, .2f);
            (flow ?? throw new NullReferenceException()).Push(action);
        }
    }
}
