#if BIZZA_REAL_WITHDRAW
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

/// <summary>Teach_01 的顺序流程。只编排现有业务，不加载动作图或执行图节点。</summary>
public sealed class Teach01Flow : IDisposable
{
    public const string SaveKey = "Teach_01";
    public enum Stage
    {
        Ready, WaitingForGame, GameTutorial, RequestingReward, Gift,
        Currency, StarterWithdrawal, WaitingForWithdrawal, Finishing,
        Completed, Failed, Cancelled, WaitingForGiftWindow, WaitingForFirstMatch, RetryingReward
    }

    public Stage CurrentStage { get; private set; }
    public Exception LastError { get; private set; }
    public bool IsRunning { get; private set; }
    readonly ITeach01Services services;
    readonly CancellationTokenSource lifetime = new();
    object gift, mask, tip;
    bool started, disposed;

    public Teach01Flow(ITeach01Services services) =>
        this.services = services ?? throw new ArgumentNullException(nameof(services));

    public async UniTask<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        if (started || disposed) return false;
        started = IsRunning = true;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellationToken);
        var token = linked.Token;
        try
        {
            token.ThrowIfCancellationRequested();
            CurrentStage = Stage.WaitingForGame;
            services.SetPreparationBlock(true);
            await WaitUntil(() => services.GameReady, token);
            await services.Delay(.01f, token);
            // Restored offline claims resume without reopening a gift or blocking gameplay teaching.
            if (services.RewardState == NewbieGiftRequestState.Background && services.GiftShown)
            {
                services.SetPreparationBlock(false);
                return await CompleteBackgroundReward(token);
            }

            var reward = PrepareReward(token);
            await UniTask.WhenAll(reward, WaitForGameplayTutorials(token));
            ThrowIfSuspended(token);
            bool offlineGift = services.RewardState == NewbieGiftRequestState.Background;
            bool giftAlreadyShown = services.GiftShown;
            if (!giftAlreadyShown) await ShowGift(token);
            if (offlineGift) return await CompleteBackgroundReward(token);

            // A previously shown gift must never grant local currency or restart withdrawal teaching.
            if (giftAlreadyShown)
            {
                services.SaveCompleted();
                CurrentStage = Stage.Completed;
                return true;
            }

            // NewbieGiftPage still owns granting, flying money and closing the gift.
            if (services.SingleCurrency)
            {
                await GuideCurrency("Teach_AllMoney", 0, .5f, token);
                CurrentStage = Stage.WaitingForWithdrawal;
                var withdrawal = await WaitForPage(Teach01Page.RealWithdrawal, token);
                await WaitForClose(Teach01Page.RealWithdrawal, withdrawal, token);
            }
            else
            {
                CurrentStage = Stage.StarterWithdrawal;
                var starter = await WaitForPage(Teach01Page.StarterWithdrawal, token);
                if (!services.IsUS)
                {
                    mask = await services.OpenMask(false, token);
                    tip = await services.OpenTip("Teach_Withdraw", -1, .2f, token);
                    var withdrawal = await WaitForPage(Teach01Page.Withdrawal, token);
                    CloseTip();
                    await WaitForClose(Teach01Page.Withdrawal, withdrawal, token);
                }
                CloseOwned(Teach01Page.Mask, ref mask);
                await WaitForClose(Teach01Page.StarterWithdrawal, starter, token);
                await GuideCurrency("Teach_coin", -1, .8f, token);
                CurrentStage = Stage.WaitingForWithdrawal;
                await WaitForPage(Teach01Page.RealWithdrawal, token);
                // The original dual-currency branch finishes with the real withdrawal page open.
            }

            CurrentStage = Stage.Finishing;
            await services.Delay(1f, token);
            CloseTip();
            await services.Delay(1f, token);
            CloseTip();
            token.ThrowIfCancellationRequested();
            services.SaveCompleted();
            CurrentStage = Stage.Completed;
            return true;
        }
        catch (OperationCanceledException) { CurrentStage = Stage.Cancelled; return false; }
        catch (Exception error) { LastError = error; CurrentStage = Stage.Failed; return false; }
        finally
        {
            // A failed request must also cancel the concurrent tutorial/input wait.
            linked.Cancel();
            Cleanup();
            IsRunning = false;
        }
    }

    async UniTask PrepareReward(CancellationToken token)
    {
        CurrentStage = Stage.WaitingForFirstMatch;
        await WaitUntil(() => services.RewardState != NewbieGiftRequestState.NotTriggered, token);
        if (services.RewardState == NewbieGiftRequestState.Background) return;
        CurrentStage = Stage.RequestingReward;
        await WaitUntil(() => services.CanRequestReward, token);
        if (services.RewardState != NewbieGiftRequestState.Succeeded && !await services.RequestReward(token))
        {
            ThrowIfSuspended(token);
            throw new InvalidOperationException("Teach01 reward failed; its pending state will retry next login.");
        }
        ThrowIfSuspended(token);
    }

    async UniTask ShowGift(CancellationToken token)
    {
        CurrentStage = Stage.WaitingForGiftWindow;
        await WaitUntil(() => services.CanShowGift, token);
        services.ClosePopups();
        // Closing another popup may synchronously start settlement or a level transition.
        await WaitUntil(() => services.CanShowGift, token);
        CurrentStage = Stage.Gift;
        gift = await services.OpenGift(token);
        await WaitForClose(Teach01Page.Gift, gift, token);
        gift = null;
    }

    async UniTask<bool> CompleteBackgroundReward(CancellationToken token)
    {
        services.SaveCompleted();
        CurrentStage = Stage.RetryingReward;
        while (true)
        {
            await WaitUntil(() => services.CanRequestReward, token);
            bool succeeded = await services.RequestReward(token);
            ThrowIfSuspended(token);
            if (succeeded)
            {
                CurrentStage = Stage.Completed;
                return true;
            }
            // Bound immediate errors too; preserve the background state until a valid server reply.
            await services.Delay(1f, token);
        }
    }

    void ThrowIfSuspended(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (services.IsSuspended) throw new OperationCanceledException("WKY account suspended.");
    }

    async UniTask WaitUntil(Func<bool> condition, CancellationToken token, float timeoutSeconds = 0)
    {
        await services.WaitUntil(() => services.IsSuspended || condition(), token, timeoutSeconds);
        ThrowIfSuspended(token);
    }

    async UniTask WaitForGameplayTutorials(CancellationToken token)
    {
        CurrentStage = Stage.GameTutorial;
        if (!services.BasicTutorialCompleted) services.SetFrameworkInputBlocked(true);
        services.SetPreparationBlock(false);
        await WaitUntil(() => services.BasicTutorialCompleted, token);
        // Release the framework UI after the base gameplay tutorial.
        services.SetFrameworkInputBlocked(false);
        await WaitUntil(() => services.GameTutorialCompleted, token);
    }

    async UniTask GuideCurrency(string text, int position, float height, CancellationToken token)
    {
        CurrentStage = Stage.Currency;
        mask = await services.OpenMask(true, token);
        tip = await services.OpenTip(text, position, height, token);
        await WaitForClose(Teach01Page.Mask, mask, token);
        mask = null;
        CloseTip();
    }

    async UniTask<object> WaitForPage(Teach01Page page, CancellationToken token)
    {
        object owner = null;
        await WaitUntil(() => (owner = services.GetPageOwner(page)) != null, token, 15f);
        return owner;
    }

    UniTask WaitForClose(Teach01Page page, object owner, CancellationToken token) =>
        WaitUntil(() => !ReferenceEquals(services.GetPageOwner(page), owner), token);

    void CloseTip() => CloseOwned(Teach01Page.Tip, ref tip);
    void CloseOwned(Teach01Page page, ref object owner)
    {
        var previous = owner;
        owner = null;
        if (previous != null) services.CloseOwnedPage(page, previous);
    }

    void Cleanup()
    {
        // One broken close callback must not retain the other pages or our input block.
        try { CloseTip(); } catch (Exception error) { LastError ??= error; }
        try { CloseOwned(Teach01Page.Mask, ref mask); } catch (Exception error) { LastError ??= error; }
        try { CloseOwned(Teach01Page.Gift, ref gift); } catch (Exception error) { LastError ??= error; }
        try { services.SetPreparationBlock(false); } catch (Exception error) { LastError ??= error; }
        try { services.SetFrameworkInputBlocked(false); } catch (Exception error) { LastError ??= error; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        Cleanup();
        lifetime.Dispose();
    }
}

public enum Teach01Page { Gift, Mask, Tip, StarterWithdrawal, Withdrawal, RealWithdrawal }

/// <summary>UI/HTTP 边界；页面句柄是每次打开的身份，不能用 Unity 对象的 == 判断关闭。</summary>
public interface ITeach01Services
{
    bool GameReady { get; }
    NewbieGiftRequestState RewardState { get; }
    bool GiftShown { get; }
    bool CanRequestReward { get; }
    bool IsSuspended { get; }
    bool BasicTutorialCompleted { get; }
    bool GameTutorialCompleted { get; }
    bool CanShowGift { get; }
    bool SingleCurrency { get; }
    bool IsUS { get; }
    void SetPreparationBlock(bool blocked);
    void SetFrameworkInputBlocked(bool blocked);
    UniTask WaitUntil(Func<bool> condition, CancellationToken token, float timeoutSeconds = 0);
    UniTask Delay(float seconds, CancellationToken token);
    UniTask<bool> RequestReward(CancellationToken token);
    void ClosePopups();
    UniTask<object> OpenGift(CancellationToken token);
    UniTask<object> OpenMask(bool currency, CancellationToken token);
    UniTask<object> OpenTip(string text, int position, float height, CancellationToken token);
    object GetPageOwner(Teach01Page page);
    void CloseOwnedPage(Teach01Page page, object owner);
    void SaveCompleted();
}
#endif
