#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class ItemUtils
{
    internal static RewardPresentation GrantReward(ItemEntry reward, AddItemParam parameters)
    {
        var presentation = new RewardPresentation(parameters);
        try
        {
            if ((reward.Type == E_ItemType.Dollar || reward.Type == E_ItemType.WithDrawDanDollar) &&
                Bizza.Sdk.ChannelConfig.Instance.real_CustomConfig.singleCurrencyMode)
            {
                presentation.Complete(false);
                return null;
            }
            if (float.IsNaN(reward.Count) || float.IsInfinity(reward.Count))
                throw new ArgumentException("Reward count must be finite.");

            var data = SaveDataUtils.ItemData;
            if (data == null) throw new InvalidOperationException("Item data is not loaded.");
            data.rewardReceipts ??= new List<string>();
            string receipt = string.IsNullOrEmpty(parameters.rewardId) ? null : parameters.rewardId + ":" + (int)reward.Type;
            if (receipt != null && data.rewardReceipts.Contains(receipt))
            {
                presentation.Complete(false);
                return null;
            }
            bool existed = data.itemMap.TryGetValue(reward.Type, out ItemEntry previous);
            if (!existed) previous = new ItemEntry { Type = reward.Type };
            ItemEntry current = previous;
            current.Count = Mathf.Max(0f, previous.Count + reward.Count);
            if (float.IsNaN(current.Count) || float.IsInfinity(current.Count)) throw new ArgumentException("Invalid reward total.");
            data.itemMap[reward.Type] = current;
            string evicted = null;
            if (receipt != null)
            {
                if (data.rewardReceipts.Count >= 256)
                {
                    evicted = data.rewardReceipts[0];
                    data.rewardReceipts.RemoveAt(0);
                }
                data.rewardReceipts.Add(receipt);
            }
            // Persist the amount and receipt together, before any animation or UI listener runs.
            try { SaveDataUtils.itemStrategy.SaveDataImmediately(); }
            catch
            {
                if (existed) data.itemMap[reward.Type] = previous;
                else data.itemMap.Remove(reward.Type);
                if (receipt != null) data.rewardReceipts.Remove(receipt);
                if (evicted != null) data.rewardReceipts.Insert(0, evicted);
                throw;
            }
            presentation.SetCommitted(previous, current);
            return presentation;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            presentation.Complete(false);
            return null;
        }
    }

    internal sealed class RewardPresentation
    {
        private readonly AddItemParam parameters;
        private readonly RewardFxScope.Stamp owner;
        private readonly int generation;
        private ItemEntry previous;
        private ItemEntry current;
        private bool committed;
        private bool played;
        private bool completed;
        private bool showingCurrencyBar;

        internal RewardPresentation(AddItemParam parameters)
        {
            this.parameters = parameters;
            generation = RewardItemCollectFlow.Generation;
            UnityEngine.Object callbackOwner = parameters.callbackOwner;
            if (ReferenceEquals(callbackOwner, null) && parameters.addFinishAction != null)
                callbackOwner = parameters.addFinishAction.Target as UnityEngine.Object;
            if (ReferenceEquals(callbackOwner, null)) callbackOwner = parameters.target;
            owner = RewardFxScope.Capture(callbackOwner);
        }

        internal void SetCommitted(ItemEntry before, ItemEntry after)
        {
            previous = before;
            current = after;
            committed = true;
        }

        internal void Play(Vector3 position, float delay = 0f)
        {
            if (played || completed) return;
            played = true;
            if (!parameters.playAnim || generation != RewardItemCollectFlow.Generation)
            {
                Complete(false);
                return;
            }
            if (parameters.showCurrencyBar)
            {
                showingCurrencyBar = true;
                SafeInvoke(OnMoneyFlyStart);
            }
            try
            {
                VFXUtils.PlayItemCollectFx(current.Type, position, () => Complete(true),
                    bUiPos: parameters.bUiPos, target: parameters.target,
                    animation: parameters.animation, delay: delay);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Complete(false);
            }
        }

        internal void Complete(bool animated)
        {
            if (completed) return;
            completed = true;
            if (showingCurrencyBar) SafeInvoke(OnMoneyFlyFinish);
            if (committed)
            {
                // A broken UI listener must not prevent the refresh or the caller's completion.
                SafeInvoke(() => BizzaEventSystem.Emit(EventDefine.Item.ItemChangedWithData, previous, current));
                SafeInvoke(() => BizzaEventSystem.Emit(EventDefine.Item.ItemChanged));
            }
            if (generation == RewardItemCollectFlow.Generation && owner.IsValid)
                SafeInvoke(parameters.addFinishAction);
        }
    }

    private static void SafeInvoke(Action action)
    {
        try { action?.Invoke(); }
        catch (Exception exception) { Debug.LogException(exception); }
    }
}
#endif
