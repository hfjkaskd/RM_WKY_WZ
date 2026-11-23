#if BIZZA_REAL_WITHDRAW
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Whitebox.Foundation;

internal sealed partial class WkyGameplayBridge : IRoyalMatchBoosterInventory
{
    private bool boosterAdPending;
    private int boosterAdRequest;

    public event Action Changed;

    partial void OnBridgeInstalled() =>
        BizzaEventSystem.On(EventDefine.Item.ItemChangedWithData, OnBoosterStockChanged);

    partial void OnBridgeReleased()
    {
        BizzaEventSystem.Off(EventDefine.Item.ItemChangedWithData, OnBoosterStockChanged);
        boosterAdPending = false;
    }

    partial void CheckOtherAdRequest(ref bool busy)
    {
        busy |= boosterAdPending;
    }

    private void OnBoosterStockChanged(ItemEntry previous, ItemEntry current)
    {
        if (previous.Count == current.Count) return;
        if (current.Type is E_ItemType.GameProp_1 or E_ItemType.GameProp_2 or E_ItemType.GameProp_3 or E_ItemType.GameProp_4)
            Changed?.Invoke();
    }

    // This game's authored free use belongs to the unlock tutorial and its own attempt state.
    public bool HasFreeUsage(OriginalBoosterType type) => false;

    public int GetBooster(OriginalBoosterType type)
    {
        var item = ItemForBooster(type);
        return item == E_ItemType.None || SaveDataUtils.ItemData == null
            ? 0
            : Mathf.Max(0, ItemUtils.GetItemCountInt(item));
    }

    internal int AwardBoosterCoins(int amount)
    {
        int added = RoyalMatchSessionBridge.AddBoosterCoins(amount);
        if (added > 0) Changed?.Invoke();
        return added;
    }

    partial void OnWonAttemptCoinsEarned(int amount) => AwardBoosterCoins(amount);

    public bool TryConsumeBooster(OriginalBoosterType type)
    {
        var item = ItemForBooster(type);
        if (item == E_ItemType.None || !IsBoosterAttemptActive() || !ItemUtils.TryReduceItem(item))
            return false;

        NumbericalStatistics._propUseTimes.TryGetValue(item, out int uses);
        NumbericalStatistics._propUseTimes[item] = ++uses;
        StatisticModule.Instance?.UseItem(item, currentLevel, uses);
        return true;
    }

    public void RequestBooster(OriginalBoosterType type, int coinPrice, Sprite icon, Action<bool> complete,
        CancellationToken cancellationToken) =>
        RequestBoosterAsync(type, coinPrice, icon, complete, cancellationToken).Forget();

    private async UniTask RequestBoosterAsync(OriginalBoosterType type, int coinPrice, Sprite icon,
        Action<bool> complete, CancellationToken cancellationToken)
    {
        var ui = UIModule.Instance;
        UIPageBase page = null;
        bool success = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ui == null || ItemForBooster(type) == E_ItemType.None || !IsBoosterAttemptActive() ||
                boosterAdPending || HasBlockingPresentation())
                return;

            int attempt = attemptVersion;
            var choice = BoosterPurchasePanel.Choice.Cancel;
            var offer = new BoosterPurchasePanel.Offer(type, icon, coinPrice, RoyalMatchSessionBridge.BoosterCoinBalance,
                BizzaSdk.Ad.Inited, selection =>
                {
                    if (!cancellationToken.IsCancellationRequested && IsBoosterOfferAttemptActive(attempt))
                        choice = selection;
                });
            // Use the framework's cancellable open path so it can roll back an unfinished popup.
            using (var openAttempt = new UIModule.PageOpenAttempt())
            using (cancellationToken.Register(openAttempt.Cancel))
                page = await ui.OpenAutomaticPopup(UIPageIds.BoosterPurchasePanel, offer, openAttempt);
            if (page == null) return;

            // OnClose reports the choice before the framework deactivates the popup.
            await UniTask.WaitUntil(() => !IsBoosterOfferAttemptActive(attempt) ||
                page == null || !page.gameObject.activeInHierarchy, cancellationToken: cancellationToken);
            if (!IsBoosterOfferAttemptActive(attempt) || choice == BoosterPurchasePanel.Choice.Cancel)
                return;

            await UniTask.WaitUntil(() => !IsBoosterOfferAttemptActive(attempt) || !HasBlockingPresentation(),
                cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsBoosterOfferAttemptActive(attempt)) return;

            if (choice == BoosterPurchasePanel.Choice.Coins)
                success = TryPurchaseBooster(type, coinPrice);
            else if (choice == BoosterPurchasePanel.Choice.Ad)
            {
                bool? adResult = null;
                RequestBoosterAd(type, result => adResult = result, cancellationToken);
                await UniTask.WaitUntil(() => adResult.HasValue || !IsBoosterOfferAttemptActive(attempt),
                    cancellationToken: cancellationToken);
                success = adResult == true;
            }
        }
        catch (Exception error)
        {
            if (!(error is OperationCanceledException && cancellationToken.IsCancellationRequested))
                Debug.LogException(error);
        }
        finally
        {
            try
            {
                if (page != null && ui != null && page.gameObject.activeInHierarchy && !page.IsClosing)
                    ui.ClosePage(page);
            }
            finally
            {
                complete(success);
            }
        }
    }

    private bool IsBoosterOfferAttemptActive(int attempt) =>
        attempt == attemptVersion && IsBoosterAttemptActive();

    private bool TryPurchaseBooster(OriginalBoosterType type, int coinPrice)
    {
        var item = ItemForBooster(type);
        if (item == E_ItemType.None || coinPrice <= 0 || !IsBoosterAttemptActive() ||
            boosterAdPending || HasBlockingPresentation())
            return false;

        int previousCount = GetBooster(type);
        if (!RoyalMatchSessionBridge.TrySpendBoosterCoins(coinPrice))
            return false;

        try
        {
            ItemUtils.AddItem(item, 1, false, false, default, false);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
        }

        if (GetBooster(type) > previousCount)
            return true;

        // Refund the game-only wallet if persisting the purchased booster failed.
        AwardBoosterCoins(coinPrice);
        return false;
    }

    private void RequestBoosterAd(OriginalBoosterType type, Action<bool> complete, CancellationToken cancellationToken)
    {
        var item = ItemForBooster(type);
        if (cancellationToken.IsCancellationRequested || item == E_ItemType.None ||
            !IsBoosterAttemptActive() || boosterAdPending || !BizzaSdk.Ad.Inited || HasBlockingPresentation())
        {
            complete(false);
            return;
        }

        string placement = PlacementForItem(item);
        int request = ++boosterAdRequest;
        int attempt = attemptVersion;
        boosterAdPending = true;
        lastBlockedFrame = Time.frameCount;
        try
        {
            float amount = WithdrawalUtil.GetCustomizedFloatByCountryType(
                WithdrawalUtil.GetDollarCountByReward());
            BizzaSdk.Ad.ShowRewardAd(placement, amount, result =>
            {
                if (!boosterAdPending || request != boosterAdRequest) return;
                boosterAdPending = false;
                if (attempt == attemptVersion) lastBlockedFrame = Time.frameCount;

                if (!result.success || cancellationToken.IsCancellationRequested || !IsBoosterOfferAttemptActive(attempt))
                {
                    complete(false);
                    return;
                }

                int before = GetBooster(type);
                try
                {
                    ItemUtils.AddItem(item, 1, false, true, default, false);
                }
                catch (Exception error)
                {
                    Debug.LogException(error);
                }

                complete(GetBooster(type) > before);
            });
            UIModule.Instance.m_curadvertistics--;
        }
        catch (Exception error)
        {
            if (!boosterAdPending || request != boosterAdRequest) throw;
            boosterAdPending = false;
            Debug.LogException(error);
            complete(false);
        }
    }

    private bool IsBoosterAttemptActive() => ReferenceEquals(this, Instance) &&
        !loading && !attemptEnded && !reloadRequested &&
        currentLevel > 0 && readyLevel == currentLevel && SaveDataUtils.ItemData != null;

    private static E_ItemType ItemForBooster(OriginalBoosterType type) => type switch
    {
        OriginalBoosterType.Hammer => E_ItemType.GameProp_1,
        OriginalBoosterType.Arrow => E_ItemType.GameProp_2,
        OriginalBoosterType.Cannon => E_ItemType.GameProp_3,
        OriginalBoosterType.JesterHat => E_ItemType.GameProp_4,
        _ => E_ItemType.None,
    };

    private static string PlacementForItem(E_ItemType item) => item switch
    {
        E_ItemType.GameProp_1 => "GameProp_1",
        E_ItemType.GameProp_2 => "GameProp_2",
        E_ItemType.GameProp_3 => "GameProp_3",
        E_ItemType.GameProp_4 => "GameProp_4",
        _ => throw new ArgumentOutOfRangeException(nameof(item)),
    };
}
#endif
