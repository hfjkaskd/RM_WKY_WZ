#if BIZZA_REAL_WITHDRAW
using System;
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

    public void RequestBooster(OriginalBoosterType type, Action<bool> complete)
    {
        var item = ItemForBooster(type);
        if (item == E_ItemType.None || !IsBoosterAttemptActive() || boosterAdPending ||
            !BizzaSdk.Ad.Inited || HasBlockingPresentation())
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

                if (!result.success || attempt != attemptVersion || !IsBoosterAttemptActive())
                {
                    complete(false);
                    return;
                }

                int before = GetBooster(type);
                ItemUtils.AddItem(item, 1, false, true, default, false);
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
        !loading && !attemptEnded && !reloadRequested && !correctionPending &&
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
