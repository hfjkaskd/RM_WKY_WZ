using System;
using System.Collections.Generic;
using System.Threading;
using Bizza.GameAnalytics;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Whitebox.Foundation;

internal sealed partial class WkyGameplayBridge : IRoyalMatchSessionBridge, IRoyalMatchTutorialBridge, IRoyalMatchWinPresentation
{
    partial void OnWonAttemptCoinsEarned(int amount);

    internal const string SceneName = "GameScene";
    internal const float BootProgressWeight = 0.75f;
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

    internal static WkyGameplayBridge Instance { get; private set; }

    private readonly List<UIPageBase> blockingPages = new();
    private int currentLevel;
    private int attemptVersion;
    private int readyLevel;
    private int lastBlockedFrame = -2;
    private bool loading = true;
    private bool attemptEnded;
    private bool reloadRequested;
    private bool initialLoad = true;
    private bool openingTutorialCompleted;
    private WinSettlement winSettlement;
    private UniTaskCompletionSource handoffCompletion;

    public bool BlocksGameplayInput
    {
        get
        {
            if (loading || attemptEnded || HasBlockingPresentation())
            {
                lastBlockedFrame = Time.frameCount;
                return true;
            }

            return Time.frameCount - lastBlockedFrame <= 1;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        Instance?.Release();
        Instance = new WkyGameplayBridge();
        RoyalMatchSessionBridge.Current = Instance;
        Instance.Register();
    }

    private void Register()
    {
        BizzaEventSystem.On(EventDefine.Frame.OpenPage, ObservePageOpened);
        BizzaEventSystem.On(EventDefine.Frame.ClosePage, ObservePageClosed);
        BizzaEventSystem.On(EventDefine.Item.ItemChanged, ObserveAccountLevel);
        OnBridgeInstalled();
    }

    private void Release()
    {
        BizzaEventSystem.Off(EventDefine.Frame.OpenPage, ObservePageOpened);
        BizzaEventSystem.Off(EventDefine.Frame.ClosePage, ObservePageClosed);
        BizzaEventSystem.Off(EventDefine.Item.ItemChanged, ObserveAccountLevel);
        OnBridgeReleased();
    }

    partial void OnBridgeInstalled();
    partial void OnBridgeReleased();
    partial void AddInteractionBlocks(ref bool blocked);
    partial void CheckOtherAdRequest(ref bool busy);

    private void ObservePageOpened(PageId pageId)
    {
        var page = UIModule.Instance?.FindOpenedPage(pageId);
        if (page != null && (page.Layer is E_UILayer.PopupLayer or E_UILayer.SystemLayer or E_UILayer.TeachLayer) &&
            !blockingPages.Contains(page))
            blockingPages.Add(page);
    }

    private void ObservePageClosed(PageId pageId)
    {
        lastBlockedFrame = Time.frameCount;
    }

    private bool HasBlockingPresentation()
    {
        bool hasModalPage = false;
        for (int i = blockingPages.Count - 1; i >= 0; i--)
        {
            var page = blockingPages[i];
            if (page == null || !page.gameObject.activeInHierarchy)
                blockingPages.RemoveAt(i);
            else
                hasModalPage = true;
        }

        bool extensionBlocked = false;
        AddInteractionBlocks(ref extensionBlocked);
        return hasModalPage || extensionBlocked || UIModule.Instance?.Opening == true ||
            TransparentBlock.IsBlock || LoadingBlock.IsBlock || BizzaSdk.Ad.IsRewardShowing ||
            BizzaSdk.Ad.IsInterShowing || World.Current?.IsPause == true;
    }

    private void ObserveAccountLevel()
    {
        var account = AccountModule.Instance;
        var serverUser = account?.Os_Current_Uso;
        var settlement = winSettlement;
        if (settlement != null && serverUser != null && ReferenceEquals(settlement.Account, account) &&
            ReferenceEquals(settlement.Receipt, serverUser))
        {
            settlement.Report.TrySetResult(true);
        }

        // AccountModule updates the saved next-entry progress. Never replace a
        // board that is loading or in play when a recovered account arrives.
    }

    public bool TryGetCurrentLevel(out int level)
    {
        level = currentLevel;
        return level > 0;
    }

    internal void PrepareInitialAttempt()
    {
        var account = AccountModule.Instance;
        if (account?.AccountReady == true)
            FlowModule.ApplyServerProgress(account.Os_Current_Uso.Os_Rts);
        PrepareNextAttempt();
    }

    public void OnAttemptReady(int level)
    {
        if (loading && level == currentLevel)
            readyLevel = level;
    }

    internal async UniTask CompleteSceneHandoff(CancellationToken token)
    {
        if (handoffCompletion != null)
        {
            await handoffCompletion.Task;
            return;
        }
        var completion = handoffCompletion = new UniTaskCompletionSource();
        int expectedAttempt = attemptVersion;
        try
        {
            await CompleteSceneHandoffOnce(token, expectedAttempt);
            completion.TrySetResult();
        }
        catch (Exception error)
        {
            completion.TrySetException(error);
            throw;
        }
    }

    private async UniTask CompleteSceneHandoffOnce(CancellationToken token, int expectedAttempt)
    {
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var timer = wait.CancelAfterSlim(WaitTimeout, DelayType.Realtime);

        var ui = UIModule.Instance;
        var loadingPage = ui.FindOpenedPage(UIPageIds.LoadingPanel);
        if (loadingPage == null)
            throw new InvalidOperationException("The active loading page was lost during the gameplay handoff.");

        ReportProgress(initialLoad ? BootProgressWeight : 0.4f);
        var gamePage = await ui.OpenPage(UIPageIds.RealGamePanel);
        EnsureCurrentAttempt(expectedAttempt);
        if (gamePage == null)
            throw new InvalidOperationException("The framework gameplay page failed to open.");

        ReportProgress(initialLoad ? 0.9f : 0.7f);
        await UniTask.WaitUntil(() => readyLevel == currentLevel, cancellationToken: wait.Token);
        EnsureCurrentAttempt(expectedAttempt);
        ReportProgress(1f);
        // Render the final completed state before closing the same page.
        await UniTask.NextFrame(wait.Token);
        EnsureCurrentAttempt(expectedAttempt);
        ui.ClosePage(loadingPage);
        await UniTask.WaitUntil(() => loadingPage == null || !loadingPage.gameObject.activeInHierarchy,
            cancellationToken: wait.Token);
        EnsureCurrentAttempt(expectedAttempt);

        FlowModule.CanShowGuide();
        if (initialLoad)
        {
            initialLoad = false;
            BizzaGameAnalytics.TrackGameLoadComplete();
            BizzaEventSystem.Emit(EventDefine.Frame.LoadingStageComplete);
        }

        FlowModule.GameStart();
        attemptEnded = false;
        reloadRequested = false;
        lastBlockedFrame = Time.frameCount;
        loading = false;
        ReportTutorialCompletion();
    }

    private void EnsureCurrentAttempt(int expectedAttempt)
    {
        if (!ReferenceEquals(Instance, this) || expectedAttempt != attemptVersion)
            throw new OperationCanceledException("The gameplay handoff belongs to a previous attempt.");
    }

    public void OnOpeningTutorialCompleted()
    {
        openingTutorialCompleted = true;
        ReportTutorialCompletion();
    }

    private void ReportTutorialCompletion()
    {
        if (!loading && openingTutorialCompleted && !SaveDataUtils.GameData.customTutorialEnd)
            FlowModule.NewPlayerGuideEnd();
    }

    public void SetWinLogoHudCamera(Camera camera)
    {
        var page = UIModule.Instance?.FindOpenedPage(UIPageIds.RealGamePanel) as RealGamePanel;
        if (page != null) page.SetWinLogoHudCamera(camera);
    }

    public void OnAttemptEnded(RoyalMatchLevelResult result)
    {
        if (loading || attemptEnded || result.Level != currentLevel)
            return;

        attemptEnded = true;
        if (result.Outcome == RoyalMatchLevelOutcome.Quit)
        {
            FlowModule.LoadGameLevel();
            return;
        }

        var levelInfo = ToLevelInfo(result);

        if (result.Outcome == RoyalMatchLevelOutcome.Won)
        {
            OnWonAttemptCoinsEarned(result.CoinsEarned);
            winSettlement = new WinSettlement(AccountModule.Instance);
            FlowModule.OpenGameWinPanel(BizzaLevelResultType.Win, levelInfo);
            return;
        }

        // Health is the existing non-timeout category; the actual gameplay result is above.
        FlowModule.OpenGameLosePanel(BizzaLevelResultType.Fail, LoseReason.Health, levelInfo);
    }

    private static LevelInfo ToLevelInfo(RoyalMatchLevelResult result)
    {
        return new LevelInfo
        {
            levelTotalTarget = result.TotalTargets,
            levelRemainingTarget = result.RemainingTargets,
            levelAchieveTarget = result.TotalTargets - result.RemainingTargets,
            levelProgress = result.TotalTargets > 0
                ? (double)(result.TotalTargets - result.RemainingTargets) / result.TotalTargets
                : 0
        };
    }

    // The win panel owns the request. Each actual gameplay settlement can claim it once.
    internal bool TryCaptureWinReport(out Action<FailHttpResponse<AccountModule.OceanShineUserReachReportResponse>> onReport)
    {
        onReport = null;
        var settlement = winSettlement;
        if (settlement == null || settlement.ReportStarted || settlement.Account == null ||
            !ReferenceEquals(settlement.Account, AccountModule.Instance))
            return false;

        settlement.ReportStarted = true;
        if (!settlement.Account.AccountReady ||
            Application.internetReachability == NetworkReachability.NotReachable)
        {
            settlement.ReplyReceived = true;
            settlement.Report.TrySetResult(false);
            return false;
        }
        onReport = response =>
        {
            if (!ReferenceEquals(Instance, this) || !ReferenceEquals(winSettlement, settlement) ||
                !ReferenceEquals(AccountModule.Instance, settlement.Account) || settlement.ReplyReceived)
                return;

            settlement.ReplyReceived = true;
            settlement.Receipt = response.success ? response.data?.Os_Uso : null;
            if (settlement.Receipt == null)
                settlement.Report.TrySetResult(false);

            // AccountModule applies the receipt after this callback. ObserveAccountLevel confirms it.
        };
        return true;
    }

    internal async UniTask ReloadAttempt()
    {
        if (loading || reloadRequested)
            return;

        reloadRequested = true;
        loading = true;
        readyLevel = 0;
        openingTutorialCompleted = false;
        try
        {
            var ui = UIModule.Instance;
            ui.ClosePage(UIPageIds.LosePanel);
            if (await ui.OpenPage(UIPageIds.LoadingPanel) == null)
                throw new InvalidOperationException("The framework loading page failed to open.");

            var settlement = winSettlement;
            if (settlement != null)
            {
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
                using var timer = wait.CancelAfterSlim(WaitTimeout, DelayType.Realtime);
                // Offline progress is local only; the reward page still owns the
                // sole server report, and only a real response can update money.
                if (!settlement.ReportStarted) settlement.Report.TrySetResult(false);
                bool confirmed = false;
                try
                {
                    confirmed = await settlement.Report.Task.AttachExternalCancellation(wait.Token);
                }
                catch (OperationCanceledException) when (!Application.exitCancellationToken.IsCancellationRequested)
                {
                }
                if (AccountModule.Instance?.IsSuspended == true)
                    return;
                if (!confirmed) UIUtils.ShowNetworkErrorTip();

                // The account update event runs before the SDK finishes persisting the receipt.
                await UniTask.NextFrame(Application.exitCancellationToken);
                if (!ReferenceEquals(Instance, this) || !ReferenceEquals(winSettlement, settlement) ||
                    !ReferenceEquals(AccountModule.Instance, settlement.Account))
                    throw new InvalidOperationException("The account changed during level settlement.");

                // The receipt was already applied by AccountModule. Reapplying
                // an older receipt here would erase a newer server revision.
                winSettlement = null;
            }

            PrepareNextAttempt();
            ReportProgress(0.2f);
            TransitionBlock.ToGamePlay();
        }
        catch (OperationCanceledException) when (Application.exitCancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            await ShowLoadFailure(error);
        }
    }

    internal static UniTask ShowLoadFailure(Exception error)
    {
        Debug.LogException(error);
        return SDKAssetHandler.OpenCommonConfirmTipsPanel(new CommonConfirmTipsPanel.Args
        {
            isLanguage = true,
            des = "HTTPNetworkProblem",
            onFinish = Application.Quit
        });
    }

    internal static void ReportProgress(float progress)
    {
        BizzaEventSystem.Emit(EventDefine.Frame.LoadingProgress, progress);
    }

    private void PrepareNextAttempt()
    {
        int level = FlowModule.BeginLevelLoading();
        if (level < 1 || level > CoreLevelCatalog.SupportedLevelCount)
            throw new InvalidOperationException($"Level {level} is outside the recovered gameplay catalog.");

        currentLevel = level;
        attemptVersion++;
        handoffCompletion = null;
    }

    private sealed class WinSettlement
    {
        internal readonly AccountModule Account;
        internal readonly UniTaskCompletionSource<bool> Report = new();
        internal bool ReportStarted;
        internal bool ReplyReceived;
        internal AccountModule.UserInfo Receipt;

        internal WinSettlement(AccountModule account)
        {
            Account = account;
        }
    }
}
