using System;
using System.Threading;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using Whitebox.Foundation;

internal sealed partial class WkyGameplayBridge : IRoyalMatchReviveBridge
{
    private Action<bool> pendingRevive;
    private bool resolvingRevive;

    internal bool CanOfferRevive => pendingRevive != null;

    partial void AddInteractionBlocks(ref bool blocked)
    {
        blocked = pendingRevive != null || resolvingRevive;
    }

    public bool TryOfferRevive(RoyalMatchLevelResult result, Action<bool> complete)
    {
        bool otherAdBusy = false;
        CheckOtherAdRequest(ref otherAdBusy);
        if (loading || attemptEnded || otherAdBusy || pendingRevive != null || resolvingRevive ||
            complete == null || result.Level != currentLevel ||
            result.Outcome != RoyalMatchLevelOutcome.OutOfMoves ||
            SaveDataUtils.GameData.currentReviveCount >= BridgingUtil.MAX_REVIVE_COUNT ||
            !BizzaSdk.Ad.Inited || BizzaSdk.Ad.IsRewardShowing || BizzaSdk.Ad.IsInterShowing)
            return false;

        pendingRevive = complete;
        FlowModule.OpenGameRevivePanel(LoseReason.Health, ToLevelInfo(result));
        WatchRevivePanel(complete).Forget();
        return true;
    }

    private async UniTask WatchRevivePanel(Action<bool> owner)
    {
        var exit = UnityEngine.Application.exitCancellationToken;
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(exit);
        using var timer = wait.CancelAfterSlim(WaitTimeout, DelayType.Realtime);
        try
        {
            await UniTask.WaitUntil(
                () => pendingRevive != owner || UIModule.Instance?.FindOpenedPage(UIPageIds.LosePanel) != null,
                cancellationToken: wait.Token);
        }
        catch (OperationCanceledException) when (exit.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
            if (pendingRevive == owner)
                ResolveReviveOffer(false);
        }
    }

    internal void ResolveReviveOffer(bool succeeded, LosePanel owner = null)
    {
        var complete = pendingRevive;
        if (complete == null)
            return;

        pendingRevive = null;
        resolvingRevive = true;
        FinishReviveOffer(succeeded, complete, owner).Forget();
    }

    private async UniTask FinishReviveOffer(bool succeeded, Action<bool> complete, LosePanel owner)
    {
        var exit = UnityEngine.Application.exitCancellationToken;
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(exit);
        using var timer = wait.CancelAfterSlim(WaitTimeout, DelayType.Realtime);
        var ui = UIModule.Instance;
        var page = owner != null ? owner : ui?.FindOpenedPage(UIPageIds.LosePanel);

        try
        {
            if (page != null && !page.IsClosing)
                ui.ClosePage(page);
            await UniTask.NextFrame(wait.Token);
            await UniTask.WaitUntil(() => page == null || !page.gameObject.activeInHierarchy,
                cancellationToken: wait.Token);
        }
        catch (OperationCanceledException) when (exit.IsCancellationRequested)
        {
            return;
        }
        catch (OperationCanceledException)
        {
            succeeded = false;
        }
        finally
        {
            resolvingRevive = false;
            lastBlockedFrame = UnityEngine.Time.frameCount;
        }

        if (!loading && !attemptEnded)
            complete(succeeded);
    }
}
