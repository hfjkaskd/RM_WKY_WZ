#if BIZZA_REAL_WITHDRAW
using System;
using System.Threading;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>使用当前 BizzaWZ 的画布、礼包、HTTP 与存档实现 Teach01Flow 所需操作。</summary>
public sealed class Teach01UnityServices : ITeach01Services
{
    readonly UIModule ui;
    readonly object blockReason = new();
    readonly bool singleCurrency, isUS;
    readonly Action<Action<FailHttpResponse<AccountModule.OceanShineUserReachReportResponse>>> request;
    bool preparationBlocked, rewardRequested;
    CanvasGroup frameworkGroup;
    bool savedInteractable, savedRaycasts;

    public Teach01UnityServices(
        Action<Action<FailHttpResponse<AccountModule.OceanShineUserReachReportResponse>>> requestReward = null)
    {
        ui = UIModule.Instance;
        if (ui == null || AccountModule.Instance?.Os_Current_Uso == null || ChannelConfig.Instance == null)
            throw new InvalidOperationException("Teach01 requires initialized UI, account and channel configuration.");
        singleCurrency = ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode;
        isUS = AccountModule.CountryType == AccountModule.E_CountryType.US;
        request = requestReward ?? AccountModule.Instance.Request_UserReachReportRequest;
    }

    public bool GameReady => World.Current?.GetGameMode<GameMode_GamePlay>() != null &&
                             SaveDataUtils.GameData.customTutorialCanPlay;
    public bool GameTutorialCompleted => SaveDataUtils.GameData.customTutorialEnd;
    public bool SingleCurrency => singleCurrency;
    public bool IsUS => isUS;

    public void SetPreparationBlock(bool blocked)
    {
        if (preparationBlocked == blocked) return;
        preparationBlocked = blocked;
        if (blocked) TransparentBlock.AddBlock(blockReason).Forget();
        else TransparentBlock.RemoveBlock(blockReason).Forget();
    }

    public void SetFrameworkInputBlocked(bool blocked)
    {
        if (blocked)
        {
            if (frameworkGroup != null) return;
            frameworkGroup = ui.GetUICanvas().GetComponent<CanvasGroup>();
            if (frameworkGroup == null) throw new InvalidOperationException("GameCanvas is missing CanvasGroup.");
            savedInteractable = frameworkGroup.interactable;
            savedRaycasts = frameworkGroup.blocksRaycasts;
            frameworkGroup.interactable = frameworkGroup.blocksRaycasts = false;
        }
        else if (frameworkGroup != null)
        {
            frameworkGroup.interactable = savedInteractable;
            frameworkGroup.blocksRaycasts = savedRaycasts;
            frameworkGroup = null;
        }
    }

    public async UniTask WaitUntil(Func<bool> condition, CancellationToken token, float timeoutSeconds = 0)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        token.ThrowIfCancellationRequested();
        while (!condition())
        {
            if (timeoutSeconds > 0 && Time.realtimeSinceStartup >= deadline)
                throw new TimeoutException("Teach01 waited too long for a request or required page.");
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            token.ThrowIfCancellationRequested();
        }
    }

    public UniTask Delay(float seconds, CancellationToken token) =>
        UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);

    public async UniTask RequestReward(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (rewardRequested) throw new InvalidOperationException("Teach01 reward already requested in this run.");
        rewardRequested = true;
        bool? succeeded = null;
        bool accepting = true;
        try
        {
            request(response =>
            {
                if (!accepting || token.IsCancellationRequested || succeeded.HasValue) return;
                succeeded = response.success && response.data != null && response.data.Os_Uso != null;
            });
            await WaitUntil(() => succeeded.HasValue, token, 30f);
            if (succeeded != true) throw new InvalidOperationException("Teach01 newbie reward request failed.");
            // AccountModule updates account data after our callback. Do not read gift data inline.
            await UniTask.Yield(PlayerLoopTiming.Update, token);
            token.ThrowIfCancellationRequested();
        }
        finally { accepting = false; }
    }

    public void ClosePopups() => ui.CloseLayerAllPage((int)E_UILayer.PopupLayer);

    public async UniTask<object> OpenGift(CancellationToken token) =>
        OwnOpenedPage(await ui.OpenPage(UIPageIds.NewbieGiftPage), token);

    public async UniTask<object> OpenMask(bool currency, CancellationToken token)
    {
        Transform target = null;
        for (int i = 0; i < 5; i++)
        {
            token.ThrowIfCancellationRequested();
            if (currency)
            {
                var game = ui.GetPage<RealGamePanel>();
                if (game != null)
                    foreach (var bar in game.GetComponentsInChildren<CurrencyBar>())
                        if (bar.coinBtn != null && bar.coinBtn.gameObject.activeInHierarchy)
                        { target = bar.coinBtn.transform; break; }
            }
            else
            {
                var starter = ui.GetPage<FakeWithdrawPanel>();
                if (starter != null && starter.withdrawBtn != null && starter.withdrawBtn.gameObject.activeInHierarchy)
                    target = starter.withdrawBtn.transform;
            }
            if (target != null) break;
            await Delay(.3f, token);
        }
        if (target == null) throw new InvalidOperationException("Teach01 could not find its active guide button.");
        string path = target.name;
        for (var parent = target.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return OwnOpenedPage(await ui.OpenPage(UIPageIds.UI_TeachMask, new UITeachMaskPage.InitParam
        {
            type = UITeachMaskPage.Type.Path, path = path, target = target.gameObject,
            block = true, shape = UITeachMaskPage.Shape.Rect, bgBlock = false,
            width = -1, height = -1, alpha = 100, clickCD = 0,
            retryCount = 5, retryInterval = .3f, showHand = true
        }), token);
    }

    public async UniTask<object> OpenTip(string text, int position, float height, CancellationToken token) =>
        OwnOpenedPage(await ui.OpenPage(UIPageIds.UI_TeachTip, new UITeachTipsPage.InitParam
        { content = text, posIdx = position, heightValue = height, block = false, alpha = 0 }), token);

    object OwnOpenedPage(UIPageBase page, CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            if (page != null && ui != null && ReferenceEquals(ui.GetPage(page.PageId), page)) ui.ClosePage(page);
            token.ThrowIfCancellationRequested();
        }
        if (page == null || page.OpenOwner == null) throw new InvalidOperationException("Teach01 page failed to open.");
        return page.OpenOwner;
    }

    public object GetPageOwner(Teach01Page page)
    {
        var instance = ui != null ? ui.GetPage(PageIdFor(page)) : null;
        return instance != null ? instance.OpenOwner : null;
    }

    public void CloseOwnedPage(Teach01Page page, object owner)
    {
        var instance = ui != null ? ui.GetPage(PageIdFor(page)) : null;
        if (instance != null && ReferenceEquals(instance.OpenOwner, owner)) ui.ClosePage(instance);
    }

    static PageId PageIdFor(Teach01Page page) => page switch
    {
        Teach01Page.Gift => UIPageIds.NewbieGiftPage,
        Teach01Page.Mask => UIPageIds.UI_TeachMask,
        Teach01Page.Tip => UIPageIds.UI_TeachTip,
        Teach01Page.StarterWithdrawal => UIPageIds.FakeWithdrawPanel,
        Teach01Page.Withdrawal => UIPageIds.UIWithdrawalPanel,
        Teach01Page.RealWithdrawal => UIPageIds.RealWithdrawPanel,
        _ => throw new ArgumentOutOfRangeException(nameof(page))
    };

    public void SaveCompleted()
    {
        SaveDataUtils.TeachData.SetState(Teach01Flow.SaveKey, TeachUtil.FinishState);
        SaveDataUtils.Save();
    }
}
#endif
